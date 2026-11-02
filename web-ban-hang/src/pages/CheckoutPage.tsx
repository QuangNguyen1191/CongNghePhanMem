import { useEffect, useMemo, useRef, useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { Lock } from 'lucide-react';
import { thanhToanApi } from '../api/khachApi';
import { qk } from '../api/queryKeys';
import type { BangTinhTien, DatHangYeuCau, PhuongThucTt } from '../api/types';
import { Breadcrumb, Button, ButtonLink, EmptyState, ErrorState, LoadingRegion, ProductImage, Skeleton, TextAreaField, TextField } from '../components';
import { useGioHang } from '../features/cart/useGioHang';
import { useHoSo } from '../features/account/useTaiKhoan';
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

type Form = { tenNguoiNhan: string; sdtNhan: string; diaChiGiao: string };

export default function CheckoutPage() {
  useTitle('Thanh toán');
  const gio = useGioHang();
  const hoSo = useHoSo();
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
    setV({ tenNguoiNhan: k.hoTen ?? '', sdtNhan: k.sdt ?? '', diaChiGiao: diaChi });
  }, [hoSo.data, v]);
  useEffect(() => { if (hoSo.error && !v) setV({ tenNguoiNhan: '', sdtNhan: '', diaChiGiao: '' }); }, [hoSo.error, v]);

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
      if (bang.maKm == null || bang.tienGiam <= 0) { setLoiVoucher('Mã không mang lại ưu đãi cho đơn này.'); return; }
      setVoucher({ ma, bang }); setLoiVoucher(''); toast.success(`Đã áp dụng mã ${ma}`);
    },
    onError: (e) => { setVoucher(null); setLoiVoucher(e.message); },
    meta: { tuXuLyLoi: true },
  });

  const datHang = useMutation({
    mutationFn: (yc: DatHangYeuCau) => thanhToanApi.datHang(yc),
    onSuccess: (don) => {
      qc.invalidateQueries({ queryKey: qk.gioHang });
      qc.invalidateQueries({ queryKey: qk.donHangs });
      qc.setQueryData(qk.donHang(don.maDon), don);
      navigate(`/tai-khoan/don-hang/${don.maDon}`, { replace: true, state: { moiDat: true } });
    },
    onError: () => { qc.invalidateQueries({ queryKey: qk.gioHang }); },
  });

  if (gio.error) return <div className="container"><ErrorState error={gio.error} onRetry={gio.refetch} /></div>;
  if (gio.isLoading || !v) return <div className={`container ${s.page}`}><LoadingRegion><Skeleton h={400} /></LoadingRegion></div>;
  if (gio.dongs.length === 0 && !datHang.isSuccess) {
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
    if (dangGui.current || datHang.isPending) return; // chong bam 2 lan
    const l: Partial<Form> = {
      tenNguoiNhan: kiemTra.hoTen(v.tenNguoiNhan) && 'Vui lòng nhập tên người nhận',
      sdtNhan: kiemTra.sdt(v.sdtNhan, true),
      diaChiGiao: v.diaChiGiao.trim().length < 10 ? 'Vui lòng nhập địa chỉ đầy đủ (số nhà, đường, quận/huyện, tỉnh/thành)' : '',
    };
    setLoi(l);
    if (Object.values(l).some(Boolean)) {
      const dau = (Object.keys(l) as (keyof Form)[]).find((k) => l[k]);
      document.querySelector<HTMLElement>(`[name="${dau}"]`)?.focus();
      return;
    }
    if (gio.coLoi) { toast.error('Giỏ hàng có sản phẩm hết hàng hoặc vượt tồn kho, vui lòng kiểm tra lại.'); return; }
    dangGui.current = true;
    try {
      await datHang.mutateAsync({
        tenNguoiNhan: v.tenNguoiNhan.trim(), sdtNhan: v.sdtNhan.trim(), diaChiGiao: v.diaChiGiao.trim(),
        phuongThucTt: pt, maVoucher: voucher?.ma,
      });
    } catch { /* toast tu MutationCache */ } finally {
      dangGui.current = false;
    }
  };

  return (
    <div className={`container ${s.page}`}>
      <Breadcrumb items={[{ label: 'Giỏ hàng', to: '/gio-hang' }, { label: 'Thanh toán' }]} />
      <h1 className="h1" style={{ margin: 'var(--s-4) 0 var(--s-5)' }}>Thanh toán</h1>

      <form className={s.layout} onSubmit={submit} noValidate>
        <div className={f.form} style={{ gap: 'var(--s-6)' }}>
          <section className={s.section} aria-labelledby="tt-giao">
            <h2 id="tt-giao" className={s.sectionTitle}>Thông tin giao hàng</h2>
            <div className={f.grid2}>
              <TextField label="Người nhận" name="tenNguoiNhan" autoComplete="name" value={v.tenNguoiNhan} onChange={doi('tenNguoiNhan')} error={loi.tenNguoiNhan} required />
              <TextField label="Số điện thoại" name="sdtNhan" type="tel" inputMode="numeric" autoComplete="tel" value={v.sdtNhan} onChange={doi('sdtNhan')} error={loi.sdtNhan} required />
            </div>
            <TextAreaField label="Địa chỉ giao hàng" name="diaChiGiao" autoComplete="street-address" value={v.diaChiGiao} onChange={doi('diaChiGiao')} error={loi.diaChiGiao} required rows={3} hint="Lấy sẵn từ hồ sơ, bạn có thể sửa cho đơn này" />
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
            <TextField label="Mã giảm giá" value={maNhap} onChange={(e) => { setMaNhap(e.target.value.toUpperCase()); setLoiVoucher(''); }} error={loiVoucher} autoComplete="off" placeholder="VD: WELCOME10" />
            {voucher
              ? <Button variant="secondary" onClick={() => { setVoucher(null); setMaNhap(''); }}>Bỏ mã</Button>
              : <Button variant="secondary" loading={apVoucher.isPending} disabled={!maNhap.trim()} onClick={() => apVoucher.mutate(maNhap.trim())}>Áp dụng</Button>}
          </div>

          {tinhQ.error ? <p className={s.warn}>Chưa tính được tổng tiền: {(tinhQ.error as Error).message}</p> : !bang ? <Skeleton h={96} /> : (
            <>
              <div className={s.rows}>
                <div><span>Tạm tính</span><span className="num">{formatTien(bang.tamTinh)}</span></div>
                {bang.tienGiam > 0 && <div className={s.saving}><span>{voucher ? `Mã ${voucher.ma}` : 'Khuyến mãi hóa đơn'}</span><span className="num">−{formatTien(bang.tienGiam)}</span></div>}
                <div><span>Phí vận chuyển</span><span>Miễn phí</span></div>
              </div>
              {bang.ghiChuKm && <p className={s.note}>{bang.ghiChuKm}</p>}
              <div className={s.total}><span>Tổng thanh toán</span><span className="num">{formatTien(bang.tongTien)}</span></div>
            </>
          )}

          <Button type="submit" size="lg" block loading={datHang.isPending} disabled={gio.coLoi || !bang} iconLeft={<Lock size={16} aria-hidden />}>Đặt hàng</Button>
          <p className={s.note}>Bằng việc đặt hàng, bạn đồng ý với <Link to="/chinh-sach-doi-tra" className="link">chính sách đổi trả</Link> của cửa hàng. Tổng tiền cuối cùng do hệ thống tính lại khi đặt.</p>
        </aside>
      </form>
    </div>
  );
}
