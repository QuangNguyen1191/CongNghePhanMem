// Kiem tra form o trinh duyet, cung quy tac voi backend (TaiKhoanBLL, KhachHangBLL, BanHangBLL).
// Backend van kiem tra lai; day chi de bao loi som cho khach.
export const kiemTra = {
  tenDangNhap: (v: string) => (v.trim().length < 4 ? 'Tên đăng nhập phải có ít nhất 4 ký tự' : /\s/.test(v.trim()) ? 'Tên đăng nhập không được có khoảng trắng' : ''),
  matKhau: (v: string) => (v.length < 6 ? 'Mật khẩu phải có ít nhất 6 ký tự' : ''),
  hoTen: (v: string) => (v.trim() ? '' : 'Vui lòng nhập họ tên'),
  /** khong bat buoc; neu co: 9-11 chu so */
  sdt: (v: string, batBuoc = false) => {
    const t = v.trim();
    if (!t) return batBuoc ? 'Vui lòng nhập số điện thoại' : '';
    return /^\d{9,11}$/.test(t) ? '' : 'Số điện thoại gồm 9 đến 11 chữ số';
  },
  email: (v: string) => (!v.trim() || /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v.trim()) ? '' : 'Email không hợp lệ'),
  ngaySinh: (v: string) => {
    if (!v) return '';
    const d = new Date(v);
    if (Number.isNaN(d.getTime())) return 'Ngày sinh không hợp lệ';
    return d > new Date() ? 'Ngày sinh không được ở tương lai' : '';
  },
};

export type Loi<T> = Partial<Record<keyof T, string>>;
export const coLoi = <T,>(l: Loi<T>) => Object.values(l).some(Boolean);
