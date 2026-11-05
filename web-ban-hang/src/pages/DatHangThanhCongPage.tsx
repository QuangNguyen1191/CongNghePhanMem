// v3: Sau khi khach vang lai dat hang: bao thanh cong + tao tai khoan nhanh (SDT da xac thuc OTP, chi can ten dang nhap + mat khau).
// Tao tai khoan -> cac don vua dat (va don cu cung SDT) tu gan vao tai khoan, duoc uu dai thanh vien moi cho nhung don sau.
import { useRef, useState, type FormEvent } from 'react';
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { toast } from 'sonner';
import { CheckCircle2, Gift } from 'lucide-react';
import { vangLaiApi } from '../api/vangLaiApi';
import { qk } from '../api/queryKeys';
import type { DonDatHang } from '../api/types';
import { Button, ButtonLink, PasswordField, Skeleton, TextField } from '../components';
import { PHUONG_THUC } from '../features/account/nhan';
import { useDangKy } from '../features/auth/useAuth';
import { useSession } from '../features/auth/useSession';
import { moTaUuDai, useUuDaiTvm } from '../features/vangLai/useUuDai';
import { coLoi, kiemTra, type Loi } from '../lib/kiemTra';
import { formatTien } from '../lib/format';
import { usePhienVangLai } from '../lib/phienVangLai';
import { useTitle } from '../lib/useTitle';
import f from './Form.module.css';
import a from './account/Account.module.css';
import s from './CartPage.module.css';

type Form = { tenDangNhap: string; matKhau: string; xacNhan: string };

export default function DatHangThanhCongPage() {
  const { ma = '' } = useParams();
  useTitle('Đặt hàng thành công');
  const tuState = (useLocation().state as { don?: DonDatHang } | null)?.don;
  const pv = usePhienVangLai();
  const phien = useSession();
  const navigate = useNavigate();
  const uuDai = useUuDaiTvm();
  const dangKy = useDangKy();
  const q = useQuery({
    queryKey: qk.vangLaiDon(pv?.sdt ?? '', ma), queryFn: () => vangLaiApi.don(ma),
    enabled: !!pv && !tuState, initialData: tuState?.maDon === ma ? tuState : undefined, retry: false,
  });
  const don = q.data;
  const [v, setV] = useState<Form>({ tenDangNhap: '', matKhau: '', xacNhan: '' });
  const [loi, setLoi] = useState<Loi<Form>>({});
  const dangGui = useRef(false);

  const doi = (k: keyof Form) => (e: { target: { value: string } }) => {
    setV((x) => ({ ...x, [k]: e.target.value }));
    if (loi[k]) setLoi((l) => ({ ...l, [k]: '' }));
  };

  const taoTaiKhoan = async (e: FormEvent) => {
    e.preventDefault();
    if (dangGui.current || !pv) return;
    const l: Loi<Form> = {
      tenDangNhap: kiemTra.tenDangNhap(v.tenDangNhap),
      matKhau: kiemTra.matKhau(v.matKhau),
      xacNhan: v.xacNhan !== v.matKhau ? 'Mật khẩu nhập lại không khớp' : '',
    };
    setLoi(l);
    if (coLoi(l)) { document.querySelector<HTMLInputElement>(`[name="${Object.keys(l).find((k) => l[k as keyof Form])}"]`)?.focus(); return; }
    dangGui.current = true;
    try {
      const p = await dangKy.mutateAsync({
        tenDangNhap: v.tenDangNhap.trim(), matKhau: v.matKhau, phienXacThuc: pv.token,
        khachHang: { hoTen: don?.tenNguoiNhan || pv.hoTen || 'Khách hàng', sdt: pv.sdt, email: don?.emailNhan || pv.email || null, diaChi: don?.diaChiGiao || pv.diaChi || null },
      });
      toast.success(p.soDonDaGan ? `Đã tạo tài khoản. ${p.soDonDaGan} đơn mua trước đó đã được gắn vào tài khoản.` : 'Đã tạo tài khoản.');
      navigate(`/tai-khoan/don-hang/${ma}`, { replace: true });
    } catch { /* hien o alert */ } finally {
      dangGui.current = false;
    }
  };

  const duocTao = !phien && pv && !pv.coTaiKhoan;

  return (
    <div className={`container ${s.page}`}>
      <div style={{ maxWidth: 720, marginInline: 'auto', display: 'grid', gap: 'var(--s-5)', marginTop: 'var(--s-4)' }}>
        <p className={f.success} role="status">
          <CheckCircle2 size={16} aria-hidden style={{ verticalAlign: '-3px', marginRight: 6 }} />
          Đặt hàng thành công! Mã đơn của bạn là <strong>{ma}</strong>. Cửa hàng sẽ gọi xác nhận trước khi giao.
        </p>

        {don ? (
          <section className={a.panel} aria-labelledby="tom-tat">
            <h2 id="tom-tat" className={a.panelTitle}>Đơn hàng {don.maDon}</h2>
            <dl className={a.kv}>
              <dt>Người nhận</dt><dd>{don.tenNguoiNhan} · <span className="num">{don.sdtNhan}</span></dd>
              <dt>Địa chỉ</dt><dd>{don.diaChiGiao}</dd>
              <dt>Thanh toán</dt><dd>{PHUONG_THUC[don.phuongThucTt] ?? don.phuongThucTt}</dd>
              <dt>Sản phẩm</dt><dd>{don.chiTiets.reduce((t, c) => t + c.soLuong, 0)}</dd>
              {don.tienGiam > 0 && <><dt>Giảm giá</dt><dd className="num">−{formatTien(don.tienGiam)}</dd></>}
              <dt>Tổng tiền</dt><dd><strong className="num">{formatTien(don.tongTien)}</strong></dd>
            </dl>
          </section>
        ) : pv && q.isLoading ? <Skeleton h={160} /> : null}

        <div className={f.actions}>
          <ButtonLink to={`/tra-cuu-don/${ma}`} variant="secondary">Xem / hủy đơn</ButtonLink>
          <ButtonLink to="/san-pham" variant="ghost">Tiếp tục mua sắm</ButtonLink>
        </div>
        <p className="small muted">
          Bạn có thể theo dõi đơn bất cứ lúc nào ở trang <Link to="/tra-cuu-don" className="link">Tra cứu đơn hàng</Link> bằng số điện thoại đã đặt.
        </p>

        {duocTao && (
          <section className={a.panel} aria-labelledby="tao-tk">
            <div>
              <h2 id="tao-tk" className="h2"><Gift size={20} aria-hidden style={{ verticalAlign: '-3px', marginRight: 6 }} />Tạo tài khoản trong 10 giây</h2>
              <p className="small muted" style={{ marginTop: 'var(--s-2)' }}>
                Số {pv.sdt} đã được xác thực, chỉ cần đặt tên đăng nhập và mật khẩu. Đơn {ma}{pv.soDonCu > 0 ? ' và các đơn trước đó' : ''} sẽ được gắn vào tài khoản
                {uuDai.data ? <>, và bạn được <strong>{moTaUuDai(uuDai.data)}</strong> đặt bằng tài khoản.</> : ', tích điểm cho các lần mua sau.'}
              </p>
            </div>
            {dangKy.error && <p className={f.alert} role="alert">{dangKy.error.message}</p>}
            <form className={f.form} onSubmit={taoTaiKhoan} noValidate>
              <TextField label="Tên đăng nhập" name="tenDangNhap" autoComplete="username" value={v.tenDangNhap} onChange={doi('tenDangNhap')} error={loi.tenDangNhap} hint="Ít nhất 4 ký tự, không có khoảng trắng" required />
              <div className={f.grid2}>
                <PasswordField label="Mật khẩu" name="matKhau" autoComplete="new-password" value={v.matKhau} onChange={doi('matKhau')} error={loi.matKhau} hint="Ít nhất 6 ký tự" required />
                <PasswordField label="Nhập lại mật khẩu" name="xacNhan" autoComplete="new-password" value={v.xacNhan} onChange={doi('xacNhan')} error={loi.xacNhan} required />
              </div>
              <Button type="submit" loading={dangKy.isPending}>Tạo tài khoản</Button>
            </form>
          </section>
        )}
        {!phien && pv?.coTaiKhoan && (
          <p className={f.info}>Số {pv.sdt} đã có tài khoản. <Link to="/dang-nhap?tiep=%2Ftai-khoan%2Fdon-hang" className="link">Đăng nhập</Link> để xem đơn cùng điểm tích lũy.</p>
        )}
      </div>
    </div>
  );
}
