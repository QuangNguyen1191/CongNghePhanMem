import { useEffect, useRef, useState, type FormEvent } from 'react';
import { Link, Navigate, useNavigate, useSearchParams } from 'react-router-dom';
import { toast } from 'sonner';
import { Gift } from 'lucide-react';
import { Button, PasswordField, TextField } from '../components';
import { useDangKy } from '../features/auth/useAuth';
import { duongDanTiep } from '../features/auth/RequireAuth';
import { useSession } from '../features/auth/useSession';
import { coLoi, kiemTra, type Loi } from '../lib/kiemTra';
import { usePhienVangLai } from '../lib/phienVangLai';
import { XacThucSdt } from '../features/vangLai/XacThucSdt';
import { moTaUuDai, useUuDaiTvm } from '../features/vangLai/useUuDai';
import { useTitle } from '../lib/useTitle';
import f from './Form.module.css';

const RONG = { tenDangNhap: '', matKhau: '', xacNhan: '', hoTen: '', sdt: '', email: '', ngaySinh: '', diaChi: '', tinhThanh: '' };
type Form = typeof RONG;

function kiemTraForm(v: Form): Loi<Form> {
  return {
    tenDangNhap: kiemTra.tenDangNhap(v.tenDangNhap),
    matKhau: kiemTra.matKhau(v.matKhau),
    xacNhan: v.xacNhan !== v.matKhau ? 'Mật khẩu nhập lại không khớp' : '',
    hoTen: kiemTra.hoTen(v.hoTen),
    email: kiemTra.email(v.email),
    ngaySinh: kiemTra.ngaySinh(v.ngaySinh),
  };
}

export default function DangKyPage() {
  useTitle('Đăng ký');
  const [params] = useSearchParams();
  const tiep = duongDanTiep(params.get('tiep'));
  const phien = useSession();
  const navigate = useNavigate();
  const dangKy = useDangKy();
  const [v, setV] = useState<Form>(RONG);
  const [loi, setLoi] = useState<Loi<Form>>({});
  const dangGui = useRef(false);
  // v3: SDT bat buoc va phai xac thuc OTP (chong dang ky bang SDT nguoi khac de xem don cua ho)
  const pv = usePhienVangLai();
  const uuDai = useUuDaiTvm();
  // da tung mua khong can tai khoan -> dien san thong tin lan mua truoc
  useEffect(() => {
    if (!pv) return;
    setV((x) => ({ ...x, sdt: pv.sdt, hoTen: x.hoTen || pv.hoTen || '', email: x.email || pv.email || '', diaChi: x.diaChi || pv.diaChi || '' }));
    setLoi((l) => ({ ...l, sdt: '' }));
  }, [pv]);

  if (phien && !dangKy.isPending && !dangKy.isSuccess) return <Navigate to={tiep} replace />;

  const doi = (k: keyof Form) => (e: { target: { value: string } }) => {
    setV((x) => ({ ...x, [k]: e.target.value }));
    if (loi[k]) setLoi((l) => ({ ...l, [k]: '' }));
  };

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    if (dangGui.current) return;
    const l = { ...kiemTraForm(v), sdt: pv ? '' : 'Vui lòng xác thực số điện thoại bằng mã OTP' };
    setLoi(l);
    if (coLoi(l) || !pv) {
      const dau = Object.keys(l).find((k) => l[k as keyof Form]);
      if (dau === 'sdt') { document.querySelector<HTMLInputElement>('[name="sdtOtp"], [name="maOtp"]')?.focus(); return; }
      document.querySelector<HTMLInputElement>(`[name="${dau}"]`)?.focus();
      return;
    }
    dangGui.current = true;
    try {
      const p = await dangKy.mutateAsync({
        tenDangNhap: v.tenDangNhap.trim(),
        matKhau: v.matKhau,
        phienXacThuc: pv.token,
        khachHang: {
          hoTen: v.hoTen.trim(), sdt: pv.sdt, email: v.email.trim() || null,
          ngaySinh: v.ngaySinh || null, diaChi: v.diaChi.trim() || null, tinhThanh: v.tinhThanh.trim() || null,
        },
      });
      toast.success(`Chào mừng ${p.hoTen} đến với UNIQLO!`);
      if (p.soDonDaGan) toast.success(`${p.soDonDaGan} đơn bạn đã mua bằng số ${pv.sdt} được gắn vào tài khoản.`);
      navigate(tiep, { replace: true });
    } catch { /* hien o alert */ } finally {
      dangGui.current = false;
    }
  };

  return (
    <div className={`container ${f.auth}`}>
      <div className={`${f.card} ${f.cardWide}`}>
        <div className={f.head}>
          <h1 className="h1">Tạo tài khoản</h1>
          <p className="muted small">Thành viên được tích 1 điểm cho mỗi 10.000₫ và lên hạng Bạc, Vàng, Kim cương.</p>
          {uuDai.data && (
            <p className={f.info}><Gift size={14} aria-hidden style={{ verticalAlign: '-2px', marginRight: 4 }} />
              Thành viên mới được <strong>{moTaUuDai(uuDai.data)}</strong>.</p>
          )}
        </div>
        {dangKy.error && <p className={f.alert} role="alert">{dangKy.error.message}</p>}
        <form className={f.form} onSubmit={submit} noValidate>
          <TextField label="Tên đăng nhập" name="tenDangNhap" autoComplete="username" value={v.tenDangNhap} onChange={doi('tenDangNhap')} error={loi.tenDangNhap} hint="Ít nhất 4 ký tự, không có khoảng trắng" required />
          <div className={f.grid2}>
            <PasswordField label="Mật khẩu" name="matKhau" autoComplete="new-password" value={v.matKhau} onChange={doi('matKhau')} error={loi.matKhau} hint="Ít nhất 6 ký tự" required />
            <PasswordField label="Nhập lại mật khẩu" name="xacNhan" autoComplete="new-password" value={v.xacNhan} onChange={doi('xacNhan')} error={loi.xacNhan} required />
          </div>
          <hr className={f.divider} />
          <TextField label="Họ và tên" name="hoTen" autoComplete="name" value={v.hoTen} onChange={doi('hoTen')} error={loi.hoTen} required />
          <XacThucSdt mucDich="DANG_KY" loi={loi.sdt}
            goiY="Bắt buộc. Nếu đã mua tại cửa hàng hoặc mua không cần tài khoản, dùng đúng số đó để giữ lịch sử mua và điểm" />
          {pv && pv.soDonCu > 0 && !pv.coTaiKhoan && (
            <p className={f.info}>Số {pv.sdt} có {pv.soDonCu} đơn mua trước đây, các đơn sẽ được gắn vào tài khoản mới.</p>
          )}
          {pv?.coTaiKhoan && (
            <p className={f.alert} role="alert">Số {pv.sdt} đã có tài khoản. Vui lòng <Link to="/dang-nhap" className="link">đăng nhập</Link> hoặc dùng số khác.</p>
          )}
          <div className={f.grid2}>
            <TextField label="Email" name="email" type="email" autoComplete="email" value={v.email} onChange={doi('email')} error={loi.email} />
            <TextField label="Ngày sinh" name="ngaySinh" type="date" autoComplete="bday" value={v.ngaySinh} onChange={doi('ngaySinh')} error={loi.ngaySinh} max={new Date().toISOString().slice(0, 10)} />
          </div>
          <TextField label="Tỉnh / thành phố" name="tinhThanh" autoComplete="address-level1" value={v.tinhThanh} onChange={doi('tinhThanh')} />
          <TextField label="Địa chỉ" name="diaChi" autoComplete="street-address" value={v.diaChi} onChange={doi('diaChi')} hint="Dùng làm địa chỉ giao hàng mặc định" />
          <Button type="submit" size="lg" block loading={dangKy.isPending}>Đăng ký</Button>
        </form>
        <p className={f.foot}>Đã có tài khoản? <Link to={`/dang-nhap${params.get('tiep') ? `?tiep=${encodeURIComponent(tiep)}` : ''}`} className="link">Đăng nhập</Link></p>
      </div>
    </div>
  );
}
