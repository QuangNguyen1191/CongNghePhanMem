// API cua khach da dang nhap: tai khoan, gio hang, don hang, hoa don, doi tra.
// Moi duong dan deu co that trong backend/WebApi/Controllers (xem API_MAPPING.md).
import { api } from './apiClient';
import type {
  BangTinhTien, DatHangYeuCau, DoiTra, DongHang, DonDatHang, GioHang, HoaDon, KhachHang, PhienDangNhap,
} from './types';

export interface DangKyYeuCau {
  tenDangNhap: string;
  matKhau: string;
  khachHang: Partial<KhachHang> & { hoTen: string };
}

export interface YeuCauDoiTra {
  maHoaDon: string;
  loai: 'DOI' | 'TRA';
  lyDo?: string;
  chiTiets: { maSp: string; soLuong: number; maSpDoi?: string | null }[];
}

const ma = encodeURIComponent;

export const taiKhoanApi = {
  dangNhap: (tenDangNhap: string, matKhau: string) =>
    api.post<PhienDangNhap>('/api/tai-khoan/dang-nhap/web-ban-hang', { tenDangNhap, matKhau }),
  dangKy: (yc: DangKyYeuCau) => api.post<PhienDangNhap>('/api/tai-khoan/dang-ky', yc),
  doiMatKhau: (matKhauCu: string, matKhauMoi: string) =>
    api.post<{ thongBao: string }>('/api/tai-khoan/doi-mat-khau', { matKhauCu, matKhauMoi }),
};

export const khachHangApi = {
  toi: () => api.get<KhachHang>('/api/khach-hang/toi'),
  /** Backend ghi de toan bo ho so -> luon gui du cac truong */
  capNhat: (k: KhachHang) => api.put<void>('/api/khach-hang/toi', k),
};

export const gioHangApi = {
  xem: () => api.get<GioHang>('/api/gio-hang'),
  them: (d: DongHang) => api.post<GioHang>('/api/gio-hang', d),
  capNhat: (d: DongHang) => api.put<GioHang>('/api/gio-hang', d),
  xoaDong: (maSp: string) => api.del<GioHang>(`/api/gio-hang/${ma(maSp)}`),
  xoaHet: () => api.del<void>('/api/gio-hang'),
  gop: (ds: DongHang[]) => api.post<GioHang>('/api/gio-hang/gop', ds),
};

export const thanhToanApi = {
  /** Xem truoc tien phai tra (KM san pham + KM hoa don hoac voucher). Khong can dang nhap. */
  tinhTien: (dongHangs: DongHang[], maVoucher?: string) =>
    api.post<BangTinhTien>('/api/khuyen-mai/tinh-tien', { dongHangs, maVoucher: maVoucher || undefined }),
  datHang: (yc: DatHangYeuCau) => api.post<DonDatHang>('/api/don-hang', yc),
};

export const donHangApi = {
  cuaToi: () => api.get<DonDatHang[]>('/api/don-hang/cua-toi'),
  layTheoMa: (m: string) => api.get<DonDatHang>(`/api/don-hang/${ma(m)}`),
  huy: (m: string) => api.post<void>(`/api/don-hang/${ma(m)}/khach-huy`),
  pdf: (m: string) => api.taiFile(`/api/don-hang/${ma(m)}/pdf`),
};

export const hoaDonApi = {
  cuaToi: () => api.get<HoaDon[]>('/api/hoa-don/cua-toi'),
  layTheoMa: (m: string) => api.get<HoaDon>(`/api/hoa-don/${ma(m)}`),
  pdf: (m: string) => api.taiFile(`/api/hoa-don/${ma(m)}/pdf`),
};

export const doiTraApi = {
  cuaToi: () => api.get<DoiTra[]>('/api/doi-tra/cua-toi'),
  layTheoMa: (m: string) => api.get<DoiTra>(`/api/doi-tra/${ma(m)}`),
  gui: (yc: YeuCauDoiTra) => api.post<DoiTra>('/api/doi-tra', yc),
  pdf: (m: string) => api.taiFile(`/api/doi-tra/${ma(m)}/pdf`),
};
