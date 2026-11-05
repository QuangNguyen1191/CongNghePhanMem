import { useEffect, useMemo, useRef, useState, type FormEvent } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { Gift, Lock } from 'lucide-react';
import { thanhToanApi } from '../api/khachApi';
import { vangLaiApi } from '../api/vangLaiApi';
import { qk } from '../api/queryKeys';
import type { BangTinhTien, DatHangYeuCau, DatHangVangLaiYeuCau, DonDatHang, PhuongThucTt } from '../api/types';
import { Breadcrumb, Button, ButtonLink, EmptyState, ErrorState, LoadingRegion, ProductImage, Skeleton, TextAreaField, TextField } from '../components';
import { useGioHang } from '../features/cart/useGioHang';
import { useHoSo } from '../features/account/useTaiKhoan';
import { useSession } from '../features/auth/useSession';
import { gioTam } from '../features/cart/gioTam';
import { XacThucSdt } from '../features/vangLai/XacThucSdt';
import { moTaUuDai, useUuDaiTvm } from '../features/vangLai/useUuDai';
import { usePhienVangLai } from '../lib/phienVangLai';
import { nhanBienThe } from '../features/account/nhan';
import { kiemTra } from '../lib/kiemTra';
import { formatTien } from '../lib/format';
import { useTitle } from '../lib/useTitle';
import f from './Form.module.css';
import s from './CartPage.module.css';

const PHUONG_THUC: { ma: PhuongThucTt; nhan: string; moTa: string }[] = [
  { ma: 'COD', nhan: 'Thanh toán khi nhận hàng (COD)', moTa: 'Trả tiền mặt cho nhân viên giao hàng.' },
  { ma: 'CHUYEN_KHOAN', nhan: 'Chuyển khoản ngân hàng', moTa: 'Cửa hàng gửi thông tin chuyển khoản khi xác nhận đơn.' },
  { ma: 'ONLINE', nhan: 'Thanh toán online', moTa: 'Ghi nhận phương thức; cửa hàng xác nhận thanh toán khi duyệt đơn.' },
];

type Form = { tenNguoiNhan: string; sdtNhan: string; diaChiGiao: string; email: string };

/** Nhan dong giam gia theo loai backend da ap (v3: co them uu dai thanh vien moi) */
function nhanGiam(bang: BangTinhTien) {
  if (bang.loaiGiam === 'THANH_VIEN_MOI') return 'Ưu đãi thành viên mới';
  if (bang.loaiGiam === 'VOUCHER' && bang.maVoucher) return `Mã ${bang.maVoucher}`;
  return 'Khuyến mãi hóa đơn';
}

export default function CheckoutPage() {
  useTitle('Thanh toán');
  // v3: chua dang nhap van thanh toan duoc (khach vang lai, xac thuc SDT bang OTP)
  const phien = useSession();
  const laVangLai = !phien;
  const pv = usePhienVangLai();
  const { pathname } = useLocation();
  const gio = useGioHang();
  const hoSo = useHoSo(!laVangLai);
  const uuDai = useUuDaiTvm();
  const qc = useQueryClient();
  const navigate = useNavigate();
  const [v, setV] = useState<Form | null>(null);
  const [loi, setLoi] = useState<Partial<Form>>({});
  const [pt, setPt] = useState<PhuongThucTt>('COD');
  const [maNhap, setMaNhap] = useState('');
  const [voucher, setVoucher] = useState<{ ma: string; bang: BangTinhTien } | null>(null);
  const [loiVoucher, setLoiVoucher] = useState('');
  const dangGui = useRef(false);

  // dien san tu ho so (backend chi luu 1 dia chi)
  useEffect(() => {
    if (v || !hoSo.data) return;
    const k = hoSo.data;
    const diaChi = [k.diaChi, k.tinhThanh && !k.diaChi?.includes(k.tinhThanh) ? k.tinhThanh : null].filter(Boolean).join(', ');
    setV({ tenNguoiNhan: k.hoTen ?? '', sdtNhan: k.sdt ?? '', diaChiGiao: diaChi, email: '' });
  }, [hoSo.data, v]);
  useEffect(() => { if (hoSo.error && !v) setV({ tenNguoiNhan: '', sdtNhan: '', diaChiGiao: '', email: '' }); }, [hoSo.error, v]);
  // khach vang lai: dien san tu lan mua truoc cua SDT da xac thuc (neu co)
  useEffect(() => {
    if (!laVangLai) return;
    setV((x) => {
      const cu = x ?? { tenNguoiNhan: '', sdtNhan: '', diaChiGiao: '', email: '' };
      if (!pv) return x ?? cu;
      return {
        ...cu, sdtNhan: pv.sdt,
        tenNguoiNhan: cu.tenNguoiNhan || pv.hoTen || '', diaChiGiao: cu.diaChiGiao || pv.diaChi || '', email: cu.email || pv.email || '',
      };
    });
  }, [laVangLai, pv]);

  const dongHangs = useMemo(() => gio.dongs.filter((d) => d.conBan).map((d) => ({ maSp: d.maSp, soLuong: d.soLuong })), [gio.dongs]);
  const khoaGio = JSON.stringify(dongHangs);
  // xem truoc tien (KM san pham + KM hoa don), server tinh lai khi dat
  const tinhQ = useQuery({
    queryKey: ['toi', 'tinh-tien', khoaGio],
    queryFn: () => thanhToanApi.tinhTien(dongHangs),
    enabled: dongHangs.length > 0,
  });
  // gio doi -> voucher da ap phai tinh lai
  useEffect(() => { setVoucher(null); }, [khoaGio]);

  const apVoucher = useMutation({
    mutationFn: (ma: string) => thanhToanApi.tinhTien(dongHangs, ma),
    onSuccess: (bang, ma) => {
      if (bang.loaiGiam === 'THANH_VIEN_MOI' && bang.maVoucher !== ma) {
        setLoiVoucher(`Ưu đãi thành viên mới đang giảm nhiều hơn mã ${ma}, hệ thống giữ ưu đãi tốt hơn cho bạn.`); return;
      }
      if (bang.maKm == null || bang.tienGiam <= 0 || bang.maVoucher !== ma) { setLoiVoucher('Mã không mang lại ưu đãi cho đơn này.'); return; }
      setVoucher({ ma, bang }); setLoiVoucher(''); toast.success(`Đã áp dụng mã ${ma}`);
    },
    onError: (e) => { setVoucher(null); setLoiVoucher(e.message); },
    meta: { tuXuLyLoi: true },
  });

  const apMa = () => {
    const ma = maNhap.trim();
    if (!ma || apVoucher.isPending || dongHangs.length === 0) return;
    apVoucher.mutate(ma);
  };

  const datHang = useMutation({
    mutationFn: (yc: DatHangYeuCau) => thanhToanApi.datHang(yc),
    onSuccess: (don) => {
      qc.invalidateQueries({ queryKey: qk.gioHang });
      qc.invalidateQueries({ queryKey: qk.donHangs });
      qc.invalidateQueries({ queryKey: qk.uuDaiTvm(true) });
      qc.setQueryData(qk.donHang(don.maDon), don);
      navigate(`/tai-khoan/don-hang/${don.maDon}`, { replace: true, state: { moiDat: true } });
    },
    onError: () => { qc.invalidateQueries({ queryKey: qk.gioHang }); },
  });

  // v3: dat hang khong can tai khoan -> gio nam o trinh duyet, gui kem dong hang
  const datVangLai = useMutation({
    mutationFn: (yc: DatHangVangLaiYeuCau) => vangLaiApi.datHang(yc),
    onSuccess: (don: DonDatHang) => {
      gioTam.xoaHet();
      if (pv) qc.setQueryData(qk.vangLaiDon(pv.sdt, don.maDon), don);
      if (pv) qc.invalidateQueries({ queryKey: qk.vangLaiDons(pv.sdt) });
      navigate(`/dat-hang-thanh-cong/${don.maDon}`, { replace: true, state: { don } });
    },
  });
  const dangDat = datHang.isPending || datVangLai.isPending;

  if (gio.error) return <div className="container"><ErrorState error={gio.error} onRetry={gio.refetch} /></div>;
  if (gio.isLoading || !v || (!laVangLai && !hoSo.data && !hoSo.error)) return <div className={`container ${s.page}`}><LoadingRegion><Skeleton h={400} /></LoadingRegion></div>;
  if (gio.dongs.length === 0 && !datHang.isSuccess && !datVangLai.isSuccess) {
    return (
      <div className={`container ${s.page}`}>
        <EmptyState title="Giỏ hàng đang trống" description="Thêm sản phẩm vào giỏ trước khi thanh toán." action={<ButtonLink to="/san-pham">Mua sắm ngay</ButtonLink>} />
      </div>
    );
  }

  const bang = voucher?.bang ?? tinhQ.data;
  const doi = (k: keyof Form) => (e: { target: { value: string } }) => {
    setV((x) => x && { ...x, [k]: e.target.value });
    if (loi[k]) setLoi((l) => ({ ...l, [k]: '' }));
  };

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    if (dangGui.current || dangDat) return; // chong bam 2 lan
    const l: Partial<Form> = {
      tenNguoiNhan: kiemTra.hoTen(v.tenNguoiNhan) && (laVangLai ? 'Vui lòng nhập họ tên' : 'Vui lòng nhập tên người nhận'),
      sdtNhan: laVangLai ? (pv ? '' : 'Vui lòng xác thực số điện thoại bằng mã OTP') : kiemTra.sdt(v.sdtNhan, true),
      diaChiGiao: v.diaChiGiao.trim().length < 10 ? 'Vui lòng nhập địa chỉ đầy đủ (số nhà, đường, quận/huyện, tỉnh/thành)' : '',
      email: laVangLai ? kiemTra.email(v.email) : '',
    };
    setLoi(l);
    if (Object.values(l).some(Boolean)) {
      const dau = (Object.keys(l) as (keyof Form)[]).find((k) => l[k]);
      document.querySelector<HTMLElement>(`[name="${dau === 'sdtNhan' && laVangLai ? 'sdtOtp' : dau}"]`)?.focus();
      return;
    }
    if (gio.coLoi) { toast.error('Giỏ hàng có sản phẩm hết hàng hoặc vượt tồn kho, vui lòng kiểm tra lại.'); return; }
    dangGui.current = true;
    try {
      if (laVangLai) {
        await datVangLai.mutateAsync({
          hoTen: v.tenNguoiNhan.trim(), email: v.email.trim() || undefined, diaChiGiao: v.diaChiGiao.trim(),
          phuongThucTt: pt, maVoucher: voucher?.ma, dongHangs,
        });
      } else {
        await datHang.mutateAsync({
          tenNguoiNhan: v.tenNguoiNhan.trim(), sdtNhan: v.sdtNhan.trim(), diaChiGiao: v.diaChiGiao.trim(),
          phuongThucTt: pt, maVoucher: voucher?.ma,
        });
      }
    } catch { /* toast tu MutationCache */ } finally {
      dangGui.current = false;
    }
  };

  return (
    <div className={`container ${s.page}`}>
      <Breadcrumb items={[{ label: 'Giỏ hàng', to: '/gio-hang' }, { label: 'Thanh toán' }]} />
      <h1 className="h1" style={{ margin: 'var(--s-4) 0 var(--s-5)' }}>Thanh toán</h1>
      {laVangLai && (
        <div className={f.info} style={{ marginBottom: 'var(--s-5)', display: 'grid', gap: 'var(--s-1)' }}>
          <span>
            Bạn đang <strong>mua không cần tài khoản</strong>: chỉ cần xác thực số điện thoại bằng mã OTP. Đã có tài khoản?{' '}
            <Link to={`/dang-nhap?tiep=${encodeURIComponent(pathname)}`} className="link">Đăng nhập</Link> để tích điểm.
          </span>
          {uuDai.data && (
            <span><Gift size={14} aria-hidden style={{ verticalAlign: '-2px', marginRight: 4 }} />
              Tạo tài khoản sau khi đặt để được {moTaUuDai(uuDai.data)}.</span>
          )}
        </div>
      )}

      <form
        className={s.layout}
        onSubmit={submit}
        noValidate
        // Ctrl + Enter (Cmd + Enter tren Mac) = dat hang
        onKeyDown={(e) => { if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) { e.preventDefault(); e.currentTarget.requestSubmit(); } }}
      >
        <div className={f.form} style={{ gap: 'var(--s-6)' }}>
          <section className={s.section} aria-labelledby="tt-giao">
            <h2 id="tt-giao" className={s.sectionTitle}>Thông tin giao hàng</h2>
            {laVangLai ? (
              <>
                <XacThucSdt mucDich="MUA_HANG" loi={loi.sdtNhan} onXong={() => setLoi((x) => ({ ...x, sdtNhan: '' }))}
                  goiY="Nhận mã OTP để xác nhận đơn; dùng số này để tra cứu đơn sau khi đặt" />
                {pv?.coTaiKhoan && (
                  <p className={f.info}>Số {pv.sdt} đã có tài khoản. <Link to={`/dang-nhap?tiep=${encodeURIComponent(pathname)}`} className="link">Đăng nhập</Link> để được tích điểm cho đơn này, hoặc tiếp tục đặt không cần đăng nhập.</p>
                )}
                <div className={f.grid2}>
                  <TextField label="Họ và tên" name="tenNguoiNhan" autoComplete="name" value={v.tenNguoiNhan} onChange={doi('tenNguoiNhan')} error={loi.tenNguoiNhan} required />
                  <TextField label="Email (không bắt buộc)" name="email" type="email" autoComplete="email" value={v.email} onChange={doi('email')} error={loi.email} />
                </div>
              </>
            ) : (
              <div className={f.grid2}>
                <TextField label="Người nhận" name="tenNguoiNhan" autoComplete="name" value={v.tenNguoiNhan} onChange={doi('tenNguoiNhan')} error={loi.tenNguoiNhan} required />
                <TextField label="Số điện thoại" name="sdtNhan" type="tel" inputMode="numeric" autoComplete="tel" value={v.sdtNhan} onChange={doi('sdtNhan')} error={loi.sdtNhan} required />
              </div>
            )}
            <TextAreaField label="Địa chỉ giao hàng" name="diaChiGiao" autoComplete="street-address" value={v.diaChiGiao} onChange={doi('diaChiGiao')} error={loi.diaChiGiao} required rows={3} hint={laVangLai ? 'Số nhà, đường, phường/xã, quận/huyện, tỉnh/thành' : 'Lấy sẵn từ hồ sơ, bạn có thể sửa cho đơn này'} />
          </section>

          <section className={s.section} aria-labelledby="tt-pt">
            <h2 id="tt-pt" className={s.sectionTitle}>Phương thức thanh toán</h2>
            <fieldset className={s.pay}>
              <legend className="sr-only">Phương thức thanh toán</legend>
              {PHUONG_THUC.map((p) => (
                <label key={p.ma} className={s.payOpt}>
                  <input type="radio" name="pt" value={p.ma} checked={pt === p.ma} onChange={() => setPt(p.ma)} />
                  <span><strong>{p.nhan}</strong><span className="small muted">{p.moTa}</span></span>
                </label>
              ))}
            </fieldset>
          </section>
        </div>

        <aside className={s.summary} aria-label="Đơn hàng">
          <h2 className="h2">Đơn hàng ({gio.tongSoLuong})</h2>
          <ul className={s.mini}>
            {gio.dongs.map((d) => (
              <li key={d.maSp} className={s.miniItem}>
                <ProductImage src={d.hinhAnh} alt="" sizes="56px" />
                <span><strong>{d.tenSp}</strong><br /><span className="muted">{nhanBienThe(d.mauSac, d.kichCo)} · SL {d.soLuong}</span>
                  {(!d.conBan || d.soLuong > d.tonKho) && <span className={s.warn} style={{ display: 'block' }}>Không đủ hàng</span>}
                </span>
                <span className="num">{formatTien(d.thanhTien)}</span>
              </li>
            ))}
          </ul>

          <div className={s.voucher}>
            <TextField
              label="Mã giảm giá"
              value={maNhap}
              onChange={(e) => { setMaNhap(e.target.value.toUpperCase()); setLoiVoucher(''); }}
              // Enter trong o ma = ap ma (khong de trinh duyet submit form dat hang)
              onKeyDown={(e) => { if (e.key === 'Enter' && !e.ctrlKey && !e.metaKey) { e.preventDefault(); e.stopPropagation(); if (!voucher) apMa(); } }}
              readOnly={!!voucher}
              error={loiVoucher}
              hint={voucher ? `Đang áp dụng mã ${voucher.ma}` : undefined}
              autoComplete="off"
              placeholder="VD: WELCOME10"
            />
            {voucher
              ? <Button variant="secondary" onClick={() => { setVoucher(null); setMaNhap(''); }}>Bỏ mã</Button>
              : <Button variant="secondary" loading={apVoucher.isPending} disabled={!maNhap.trim()} onClick={apMa}>Áp dụng</Button>}
          </div>

          {tinhQ.error ? <p className={s.warn}>Chưa tính được tổng tiền: {(tinhQ.error as Error).message}</p> : !bang ? <Skeleton h={96} /> : (
            <>
              <div className={s.rows}>
                <div><span>Tạm tính</span><span className="num">{formatTien(bang.tamTinh)}</span></div>
                {bang.tienGiam > 0 && <div className={s.saving}><span>{nhanGiam(bang)}</span><span className="num">−{formatTien(bang.tienGiam)}</span></div>}
                <div><span>Phí vận chuyển</span><span>Miễn phí</span></div>
              </div>
              {bang.ghiChuKm && <p className={s.note}>{bang.ghiChuKm}</p>}
              {!laVangLai && uuDai.data?.conLai != null && uuDai.data.conLai > 0 && (
                <p className={s.note}><Gift size={13} aria-hidden style={{ verticalAlign: '-2px', marginRight: 4 }} />
                  Bạn còn {uuDai.data.conLai}/{uuDai.data.soDonDau} lượt ưu đãi thành viên mới, tự áp dụng khi giảm nhiều hơn khuyến mãi khác.</p>
              )}
              <div className={s.total}><span>Tổng thanh toán</span><span className="num">{formatTien(bang.tongTien)}</span></div>
            </>
          )}

          <Button type="submit" size="lg" block loading={dangDat} disabled={gio.coLoi || !bang} iconLeft={<Lock size={16} aria-hidden />} aria-keyshortcuts="Control+Enter" title="Ctrl + Enter">Đặt hàng</Button>
          <p className={s.note}>Bằng việc đặt hàng, bạn đồng ý với <Link to="/chinh-sach-doi-tra" className="link">chính sách đổi trả</Link> của cửa hàng. Tổng tiền cuối cùng do hệ thống tính lại khi đặt.</p>
        </aside>
      </form>
    </div>
  );
}
