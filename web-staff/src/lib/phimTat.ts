import { useEffect, useRef } from 'react';

// Yêu cầu 9: hỗ trợ thao tác bằng bàn phím.
// Phím F1–F12 và Alt+phím chạy được cả khi đang gõ trong ô nhập; phím đơn (vd "?", "/") chỉ chạy khi không gõ chữ.

export interface PhimTat {
  phim: string; // vd 'F2', 'Alt+1', '?', '/', 'Escape', 'Ctrl+Enter'
  chay: (e: KeyboardEvent) => void;
  moTa?: string;
  /** Cho phép chạy cả khi đang gõ trong ô nhập (mặc định: true với F-key, Alt, Ctrl; false với phím đơn). */
  trongONhap?: boolean;
}

export function tenPhim(e: KeyboardEvent) {
  const k = e.key.length === 1 ? e.key.toUpperCase() : e.key;
  const ds: string[] = [];
  if (e.ctrlKey || e.metaKey) ds.push('Ctrl');
  if (e.altKey) ds.push('Alt');
  if (e.shiftKey && k.length > 1 && !/^F\d+$/.test(k)) ds.push('Shift');
  ds.push(e.key === '?' ? '?' : k);
  return ds.join('+');
}

export function dangGo(e: KeyboardEvent) {
  const t = e.target as HTMLElement | null;
  if (!t) return false;
  const tag = t.tagName;
  return tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT' || t.isContentEditable;
}

export function usePhimTat(ds: PhimTat[], batDung = true) {
  const ref = useRef(ds);
  ref.current = ds;
  useEffect(() => {
    if (!batDung) return;
    const nghe = (e: KeyboardEvent) => {
      if (e.isComposing) return;
      const ten = tenPhim(e);
      for (const p of ref.current) {
        if (p.phim.toUpperCase() !== ten.toUpperCase()) continue;
        const choPhepKhiGo = p.trongONhap ?? (/^F\d+$/.test(p.phim) || p.phim.includes('+') || p.phim === 'Escape');
        if (dangGo(e) && !choPhepKhiGo) continue;
        e.preventDefault();
        p.chay(e);
        return;
      }
    };
    window.addEventListener('keydown', nghe);
    return () => window.removeEventListener('keydown', nghe);
  }, [batDung]);
}

/** Danh sách hiển thị trong bảng phím tắt (phím ?). */
export const BANG_PHIM_TAT: { nhom: string; ds: [string, string][] }[] = [
  {
    nhom: 'Toàn trang',
    ds: [
      ['Alt + 1…9', 'Chuyển nhanh giữa các màn hình theo thứ tự menu'],
      ['?', 'Mở bảng phím tắt này'],
      ['/', 'Đưa con trỏ vào ô tìm kiếm của màn hình'],
      ['Alt + L', 'Làm mới dữ liệu màn hình'],
      ['Esc', 'Đóng hộp thoại'],
    ],
  },
  {
    nhom: 'Danh sách main-sub (đơn, hóa đơn, đổi trả, phiếu nhập)',
    ds: [
      ['↑ / ↓', 'Chọn dòng trước / sau, chi tiết hiện ngay bên phải'],
      ['Home / End', 'Về dòng đầu / dòng cuối'],
    ],
  },
  {
    nhom: 'Bán tại quầy',
    ds: [
      ['F2', 'Ô tìm sản phẩm (gõ mã hoặc tên, Enter để thêm)'],
      ['F3', 'Đổi chế độ tìm: tái sử dụng / truy vấn mới'],
      ['F4', 'Ô số điện thoại khách'],
      ['F6', 'Ô mã voucher'],
      ['F7', 'Đổi phương thức thanh toán'],
      ['F9', 'Thanh toán và lập hóa đơn'],
      ['Alt + Delete', 'Xóa hết giỏ'],
      ['+ / −', 'Tăng / giảm số lượng dòng đang chọn'],
    ],
  },
  {
    nhom: 'Đơn online',
    ds: [
      ['Alt + X', 'Xác nhận đơn / duyệt yêu cầu hủy'],
      ['Alt + G', 'Chuyển sang đang giao'],
      ['Alt + H', 'Hoàn tất đơn đang giao'],
      ['Alt + P', 'In PDF chứng từ đang chọn'],
    ],
  },
];
