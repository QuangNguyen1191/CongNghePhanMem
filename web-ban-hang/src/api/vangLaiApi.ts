// v3: API cho khach mua KHONG can tai khoan (backend: Controllers/VangLaiController.cs).
// Xac thuc SDT bang OTP -> token phien VANG_LAI (30 phut), chi gui token nay cho /api/vang-lai/*.
import { api } from './apiClient';
import type { DatHangVangLaiYeuCau, DonDatHang, KetQuaGuiOtp, MucDichOtp, PhienVangLai, UuDaiThanhVienMoi } from './types';
import { phienVangLai } from '../lib/phienVangLai';

const ma = encodeURIComponent;
// khong gui token dang nhap cho cac API nay; phien het han -> xoa de khach nhan OTP lai
const tk = () => ({ token: phienVangLai.token(), khiHetHan: phienVangLai.xoa });

export const vangLaiApi = {
  guiOtp: (sdt: string, mucDich: MucDichOtp) => api.post<KetQuaGuiOtp>('/api/vang-lai/otp', { sdt, mucDich }, { token: null }),
  xacThuc: (sdt: string, maOtp: string) => api.post<PhienVangLai>('/api/vang-lai/xac-thuc', { sdt, ma: maOtp }, { token: null }),
  datHang: (yc: DatHangVangLaiYeuCau) => api.post<DonDatHang>('/api/vang-lai/don-hang', yc, tk()),
  donHangs: () => api.get<DonDatHang[]>('/api/vang-lai/don-hang', tk()),
  don: (m: string) => api.get<DonDatHang>(`/api/vang-lai/don-hang/${ma(m)}`, tk()),
  huy: (m: string) => api.post<void>(`/api/vang-lai/don-hang/${ma(m)}/huy`, undefined, tk()),
  yeuCauHuy: (m: string, lyDo: string) => api.post<DonDatHang>(`/api/vang-lai/don-hang/${ma(m)}/yeu-cau-huy`, { lyDo }, tk()),
  pdf: (m: string) => api.taiFile(`/api/vang-lai/don-hang/${ma(m)}/pdf`, tk()),
};

export const uuDaiApi = {
  /** Chuong trinh uu dai thanh vien moi dang chay (204 -> undefined). Da dang nhap: kem so luot con lai. */
  thanhVienMoi: async () => (await api.get<UuDaiThanhVienMoi | undefined>('/api/khuyen-mai/thanh-vien-moi')) ?? null,
};
