const dinhDangTien = new Intl.NumberFormat('vi-VN');

/** 249000 -> "249.000₫" */
export const formatTien = (n: number) => `${dinhDangTien.format(Math.round(n))}₫`;

/** ISO -> "02/11/2026" */
export const formatNgay = (iso: string) => {
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? '' : d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
};

/** Duong dan anh SP: hinh_anh trong CSDL la /images/spXXX.jpg, nam trong public/ cua web */
export const urlAnh = (hinhAnh?: string | null) => (hinhAnh ? hinhAnh : null);
