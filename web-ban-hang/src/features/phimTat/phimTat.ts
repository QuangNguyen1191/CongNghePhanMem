// Yeu cau 9 (ban phim): phim tat dung chung toan web. Danh sach nay vua dung de xu ly, vua hien trong bang huong dan (phim ?).

export interface PhimTat {
  phim: string[];       // cac phim bam lien tiep, vd ['g', 'c']
  moTa: string;
  nhom: 'Chung' | 'Đi tới trang' | 'Trang chi tiết sản phẩm' | 'Danh sách sản phẩm' | 'Thanh toán';
}

export const DANH_SACH_PHIM_TAT: PhimTat[] = [
  { phim: ['/'], moTa: 'Đưa con trỏ vào ô tìm kiếm', nhom: 'Chung' },
  { phim: ['?'], moTa: 'Mở bảng phím tắt này', nhom: 'Chung' },
  { phim: ['Esc'], moTa: 'Đóng menu, bộ lọc, hộp thoại', nhom: 'Chung' },
  { phim: ['Tab'], moTa: 'Chuyển qua lại các nút, ô nhập (Shift+Tab để lùi)', nhom: 'Chung' },
  { phim: ['g', 'h'], moTa: 'Trang chủ', nhom: 'Đi tới trang' },
  { phim: ['g', 's'], moTa: 'Tất cả sản phẩm', nhom: 'Đi tới trang' },
  { phim: ['g', 'c'], moTa: 'Giỏ hàng', nhom: 'Đi tới trang' },
  { phim: ['g', 't'], moTa: 'Tài khoản', nhom: 'Đi tới trang' },
  { phim: ['g', 'd'], moTa: 'Đơn hàng của tôi', nhom: 'Đi tới trang' },
  { phim: ['←', '→'], moTa: 'Ảnh trước / ảnh sau', nhom: 'Trang chi tiết sản phẩm' },
  { phim: ['a'], moTa: 'Thêm vào giỏ hàng (sau khi chọn size)', nhom: 'Trang chi tiết sản phẩm' },
  { phim: ['f'], moTa: 'Mở bộ lọc', nhom: 'Danh sách sản phẩm' },
  { phim: ['Enter'], moTa: 'Trong ô mã giảm giá: áp dụng mã', nhom: 'Thanh toán' },
  { phim: ['Ctrl', 'Enter'], moTa: 'Đặt hàng', nhom: 'Thanh toán' },
];

/** Dang go chu trong o nhap thi khong bat phim tat (tru khi phim tat cho phep). */
export function dangNhapLieu(e: KeyboardEvent) {
  const el = e.target as HTMLElement | null;
  if (!el) return false;
  const tag = el.tagName;
  return tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT' || el.isContentEditable;
}

/** Co hop thoai (<dialog> modal) dang mo -> nhuong phim cho hop thoai */
export const coHopThoaiMo = () => !!document.querySelector('dialog[open]');

/** Phim don gian khong kem Ctrl/Alt/Meta (Shift duoc phep vi '?' can Shift) */
export const phimTron = (e: KeyboardEvent) => !e.ctrlKey && !e.altKey && !e.metaKey;
