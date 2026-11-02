import { Link, useNavigate } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { dangXuat } from '../features/auth/useAuth';
import { ChevronRight } from 'lucide-react';
import type { LoaiSp } from '../api/types';
import { Drawer } from '../components';
import s from './MobileMenu.module.css';

interface Props { open: boolean; onClose: () => void; loaiSps: LoaiSp[]; daDangNhap: boolean }

export function MobileMenu({ open, onClose, loaiSps, daDangNhap }: Props) {
  const qc = useQueryClient();
  const navigate = useNavigate();
  const thoat = () => { dangXuat(qc); onClose(); toast.success('Đã đăng xuất'); navigate('/'); };
  const Muc = ({ to, children }: { to: string; children: React.ReactNode }) => (
    <li><Link to={to} className={s.item} onClick={onClose}>{children}<ChevronRight size={16} aria-hidden /></Link></li>
  );
  return (
    <Drawer open={open} onClose={onClose} title="Menu" side="left">
      <p className={`label ${s.heading}`}>Danh mục</p>
      <ul>
        <Muc to="/san-pham">Tất cả sản phẩm</Muc>
        {loaiSps.map((l) => <Muc key={l.maLoaiSp} to={`/danh-muc/${l.maLoaiSp}`}>{l.tenLoaiSp}</Muc>)}
        <Muc to="/san-pham?giamGia=1">Đang giảm giá</Muc>
      </ul>
      <p className={`label ${s.heading}`}>Tài khoản</p>
      <ul>
        {daDangNhap ? (
          <>
            <Muc to="/tai-khoan">Thông tin tài khoản</Muc>
            <Muc to="/tai-khoan/don-hang">Đơn hàng của tôi</Muc>
            <li><button type="button" className={s.item} onClick={thoat}>Đăng xuất</button></li>
          </>
        ) : (
          <>
            <Muc to="/dang-nhap">Đăng nhập</Muc>
            <Muc to="/dang-ky">Tạo tài khoản</Muc>
          </>
        )}
        <Muc to="/gio-hang">Giỏ hàng</Muc>
      </ul>
      <p className={`label ${s.heading}`}>Hỗ trợ</p>
      <ul>
        <Muc to="/huong-dan-chon-size">Hướng dẫn chọn size</Muc>
        <Muc to="/chinh-sach-doi-tra">Chính sách đổi trả</Muc>
      </ul>
    </Drawer>
  );
}
