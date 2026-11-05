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
  /** v3: SDT nhan OTP: di dong Viet Nam 10 so (03/05/07/08/09), nhan ca +84 / co dau cach (giong QuyTac.LaSdtDiDong) */
  sdtDiDong: (v: string) => {
    const t = chuanHoaSdt(v);
    if (!t) return 'Vui lòng nhập số điện thoại';
    return /^0[35789]\d{8}$/.test(t) ? '' : 'Số điện thoại di động gồm 10 chữ số, bắt đầu bằng 03, 05, 07, 08 hoặc 09';
  },
  email: (v: string) => (!v.trim() || /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v.trim()) ? '' : 'Email không hợp lệ'),
  ngaySinh: (v: string) => {
    if (!v) return '';
    const d = new Date(v);
    if (Number.isNaN(d.getTime())) return 'Ngày sinh không hợp lệ';
    return d > new Date() ? 'Ngày sinh không được ở tương lai' : '';
  },
};

/** Bo dau cach / cham / gach, doi +84 / 84 thanh 0 (giong QuyTac.ChuanHoaSdt o backend) */
export function chuanHoaSdt(v: string) {
  let t = v.trim().replace(/[\s.\-()]/g, '');
  if (t.startsWith('+84')) t = '0' + t.slice(3);
  else if (t.startsWith('84') && t.length === 11) t = '0' + t.slice(2);
  return t;
}

export type Loi<T> = Partial<Record<keyof T, string>>;
export const coLoi = <T,>(l: Loi<T>) => Object.values(l).some(Boolean);
