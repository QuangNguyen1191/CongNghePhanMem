import { api } from './apiClient';
import type { GiaKhuyenMai, KhuyenMai, LoaiSp, SanPham } from './types';

export const loaiSpApi = {
  layTatCa: () => api.get<LoaiSp[]>('/api/loai-sp'),
};

export const sanPhamApi = {
  /** Toan bo SKU dang ban (tai 1 lan, loc tai trinh duyet) */
  layDangBan: () => api.get<SanPham[]>('/api/san-pham', { query: { chiDangBan: true } }),
  timKiem: (tuKhoa: string, maLoaiSp?: string) =>
    api.get<SanPham[]>('/api/san-pham', { query: { tuKhoa, maLoaiSp, chiDangBan: true } }),
  layTheoMa: (maSp: string) => api.get<SanPham>(`/api/san-pham/${encodeURIComponent(maSp)}`),
  layBienThe: (maSp: string) => api.get<SanPham[]>(`/api/san-pham/${encodeURIComponent(maSp)}/bien-the`),
  giaKhuyenMai: () => api.get<GiaKhuyenMai>('/api/san-pham/gia-khuyen-mai'),
};

export const khuyenMaiApi = {
  dangChay: () => api.get<KhuyenMai[]>('/api/khuyen-mai/dang-chay'),
};
