import { useRef, useState, type FormEvent } from 'react';
import { Link, Navigate, useNavigate, useSearchParams } from 'react-router-dom';
import { toast } from 'sonner';
import { Button, PasswordField, TextField } from '../components';
import { useDangNhap } from '../features/auth/useAuth';
import { duongDanTiep } from '../features/auth/RequireAuth';
import { useSession } from '../features/auth/useSession';
import { useGioTam } from '../features/cart/gioTam';
import { useTitle } from '../lib/useTitle';
import f from './Form.module.css';

export default function DangNhapPage() {
  useTitle('Đăng nhập');
  const [params] = useSearchParams();
  const tiep = duongDanTiep(params.get('tiep'));
  const phien = useSession();
  const navigate = useNavigate();
  const dangNhap = useDangNhap();
  const gioTam = useGioTam();
  const [ten, setTen] = useState('');
  const [mk, setMk] = useState('');
  const [loi, setLoi] = useState<{ ten?: string; mk?: string }>({});
  const dangGui = useRef(false);

  if (phien && !dangNhap.isPending && !dangNhap.isSuccess) return <Navigate to={tiep} replace />;

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    if (dangGui.current) return;
    const l = { ten: ten.trim() ? '' : 'Vui lòng nhập tên đăng nhập', mk: mk ? '' : 'Vui lòng nhập mật khẩu' };
    setLoi(l);
    if (l.ten || l.mk) return;
    dangGui.current = true;
    try {
      const p = await dangNhap.mutateAsync({ tenDangNhap: ten, matKhau: mk });
      toast.success(`Xin chào, ${p.hoTen}`);
      navigate(tiep, { replace: true });
    } catch {
      setMk('');
    } finally {
      dangGui.current = false;
    }
  };

  return (
    <div className={`container ${f.auth}`}>
      <div className={f.card}>
        <div className={f.head}>
          <h1 className="h1">Đăng nhập</h1>
          <p className="muted small">Đăng nhập để đặt hàng, theo dõi đơn và tích điểm thành viên.</p>
        </div>
        {tiep === '/thanh-toan' && gioTam.length > 0 && (
          <p className={f.info}>Giỏ hàng tạm của bạn sẽ được giữ lại và gộp vào tài khoản sau khi đăng nhập.</p>
        )}
        {dangNhap.error && <p className={f.alert} role="alert">{dangNhap.error.message}</p>}
        <form className={f.form} onSubmit={submit} noValidate>
          <TextField label="Tên đăng nhập" name="username" autoComplete="username" value={ten} onChange={(e) => setTen(e.target.value)} error={loi.ten} required autoFocus />
          <PasswordField label="Mật khẩu" name="password" autoComplete="current-password" value={mk} onChange={(e) => setMk(e.target.value)} error={loi.mk} required />
          <Button type="submit" size="lg" block loading={dangNhap.isPending}>Đăng nhập</Button>
        </form>
        <p className={f.info}>Quên mật khẩu? Vui lòng liên hệ cửa hàng để được cấp lại, hệ thống chưa hỗ trợ tự đặt lại mật khẩu.</p>
        <p className={f.foot}>Chưa có tài khoản? <Link to={`/dang-ky${params.get('tiep') ? `?tiep=${encodeURIComponent(tiep)}` : ''}`} className="link">Đăng ký ngay</Link></p>
      </div>
    </div>
  );
}
