import dayjs from 'dayjs';

const vnd = new Intl.NumberFormat('vi-VN');
export const tien = (n: number | null | undefined) => `${vnd.format(Math.round(n ?? 0))}đ`;
export const so = (n: number | null | undefined) => vnd.format(n ?? 0);
export const ngay = (s?: string | null) => (s ? dayjs(s).format('DD/MM/YYYY') : '');
export const ngayGio = (s?: string | null) => (s ? dayjs(s).format('DD/MM/YYYY HH:mm') : '');
export const ngayIso = (d: dayjs.Dayjs) => d.format('YYYY-MM-DD');

/** Bỏ dấu tiếng Việt để tìm kiếm không phân biệt dấu. */
export const boDau = (s: string) =>
  s
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .replace(/đ/g, 'd')
    .replace(/Đ/g, 'D')
    .toLowerCase();

export const bienThe = (kichCo?: string | null, mauSac?: string | null) => [mauSac, kichCo].filter(Boolean).join(' / ');
