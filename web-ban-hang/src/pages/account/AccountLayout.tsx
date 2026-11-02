import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { FileText, KeyRound, LogOut, Package, RefreshCcw, User } from 'lucide-react';
import { Breadcrumb, Button } from '../../components';
import { dangXuat } from '../../features/auth/useAuth';
import { useSession } from '../../features/auth/useSession';
import s from './Account.module.css';

const MUC = [
  { to: '/tai-khoan', end: true, nhan: 'Hồ sơ & điểm', icon: User },
  { to: '/tai-khoan/don-hang', nhan: 'Đơn hàng', icon: Package },
  { to: '/tai-khoan/hoa-don', nhan: 'Hóa đơn', icon: FileText },
  { to: '/tai-khoan/doi-tra', nhan: 'Đổi / trả hàng', icon: RefreshCcw },
  { to: '/tai-khoan/mat-khau', nhan: 'Đổi mật khẩu', icon: KeyRound },
];

export default function AccountLayout() {
  const phien = useSession();
  const qc = useQueryClient();
  const navigate = useNavigate();
  const thoat = () => {
    dangXuat(qc);
    toast.success('Đã đăng xuất');
    navigate('/', { replace: true });
  };
  return (
    <div className={`container ${s.page}`}>
      <Breadcrumb items={[{ label: 'Trang chủ', to: '/' }, { label: 'Tài khoản' }]} />
      <div className={s.layout}>
        <aside className={s.side}>
          <div className={s.hello}>
            <span className="small muted">Xin chào</span>
            <strong>{phien?.hoTen}</strong>
          </div>
          <nav aria-label="Tài khoản" className={s.nav}>
            {MUC.map(({ to, end, nhan, icon: Icon }) => (
              <NavLink key={to} to={to} end={end} className={({ isActive }) => `${s.navLink} ${isActive ? s.navOn : ''}`}>
                <Icon size={16} aria-hidden />{nhan}
              </NavLink>
            ))}
          </nav>
          <Button variant="ghost" className={s.logout} iconLeft={<LogOut size={16} aria-hidden />} onClick={thoat}>Đăng xuất</Button>
        </aside>
        <div className={s.content}><Outlet /></div>
      </div>
    </div>
  );
}
