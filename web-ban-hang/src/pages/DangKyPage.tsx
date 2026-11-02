import { useRef, useState, type FormEvent } from 'react';
import { Link, Navigate, useNavigate, useSearchParams } from 'react-router-dom';
import { toast } from 'sonner';
import { Button, PasswordField, TextField } from '../components';
import { useDangKy } from '../features/auth/useAuth';
import { duongDanTiep } from '../features/auth/RequireAuth';
import { useSession } from '../features/auth/useSession';
import { coLoi, kiemTra, type Loi } from '../lib/kiemTra';
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
    sdt: kiemTra.sdt(v.sdt),
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

  if (phien && !dangKy.isPending && !dangKy.isSuccess) return <Navigate to={tiep} replace />;

  const doi = (k: keyof Form) => (e: { target: { value: string } }) => {
    setV((x) => ({ ...x, [k]: e.target.value }));
    if (loi[k]) setLoi((l) => ({ ...l, [k]: '' }));
  };

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    if (dangGui.current) return;
    const l = kiemTraForm(v);
    setLoi(l);
    if (coLoi(l)) {
      const dau = Object.keys(l).find((k) => l[k as keyof Form]);
      document.querySelector<HTMLInputElement>(`[name="${dau}"]`)?.focus();
      return;
    }
    dangGui.current = true;
    try {
      const p = await dangKy.mutateAsync({
        tenDangNhap: v.tenDangNhap.trim(),
        matKhau: v.matKhau,
        khachHang: {
          hoTen: v.hoTen.trim(), sdt: v.sdt.trim() || null, email: v.email.trim() || null,
          ngaySinh: v.ngaySinh || null, diaChi: v.diaChi.trim() || null, tinhThanh: v.tinhThanh.trim() || null,
        },
      });
      toast.success(`Chào mừng ${p.hoTen} đến với UNIQLO!`);
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
          <div className={f.grid2}>
            <TextField label="Số điện thoại" name="sdt" type="tel" inputMode="numeric" autoComplete="tel" value={v.sdt} onChange={doi('sdt')} error={loi.sdt} hint="Nếu đã mua tại cửa hàng, nhập đúng SĐT để giữ lịch sử mua và điểm" />
            <TextField label="Email" name="email" type="email" autoComplete="email" value={v.email} onChange={doi('email')} error={loi.email} />
          </div>
          <div className={f.grid2}>
            <TextField label="Ngày sinh" name="ngaySinh" type="date" autoComplete="bday" value={v.ngaySinh} onChange={doi('ngaySinh')} error={loi.ngaySinh} max={new Date().toISOString().slice(0, 10)} />
            <TextField label="Tỉnh / thành phố" name="tinhThanh" autoComplete="address-level1" value={v.tinhThanh} onChange={doi('tinhThanh')} />
          </div>
          <TextField label="Địa chỉ" name="diaChi" autoComplete="street-address" value={v.diaChi} onChange={doi('diaChi')} hint="Dùng làm địa chỉ giao hàng mặc định" />
          <Button type="submit" size="lg" block loading={dangKy.isPending}>Đăng ký</Button>
        </form>
        <p className={f.foot}>Đã có tài khoản? <Link to={`/dang-nhap${params.get('tiep') ? `?tiep=${encodeURIComponent(tiep)}` : ''}`} className="link">Đăng nhập</Link></p>
      </div>
    </div>
  );
}
