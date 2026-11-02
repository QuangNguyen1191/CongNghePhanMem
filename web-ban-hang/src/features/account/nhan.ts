// Nhan tieng Viet cho cac ma trang thai trong CSDL
import type { TrangThaiDon } from '../../api/types';

type Tone = 'neutral' | 'sale' | 'success' | 'warning' | 'dark';

export const TRANG_THAI_DON: Record<TrangThaiDon, { nhan: string; tone: Tone }> = {
  CHO_XAC_NHAN: { nhan: 'Chờ xác nhận', tone: 'warning' },
  DA_XAC_NHAN: { nhan: 'Đã xác nhận', tone: 'neutral' },
  DANG_GIAO: { nhan: 'Đang giao', tone: 'dark' },
  HOAN_TAT: { nhan: 'Hoàn tất', tone: 'success' },
  DA_HUY: { nhan: 'Đã hủy', tone: 'sale' },
  TU_CHOI: { nhan: 'Bị từ chối', tone: 'sale' },
};

export const BUOC_DON: TrangThaiDon[] = ['CHO_XAC_NHAN', 'DA_XAC_NHAN', 'DANG_GIAO', 'HOAN_TAT'];

export const TRANG_THAI_HOA_DON: Record<string, { nhan: string; tone: Tone }> = {
  CHO_THANH_TOAN: { nhan: 'Chờ thanh toán', tone: 'warning' },
  DA_THANH_TOAN: { nhan: 'Đã thanh toán', tone: 'success' },
  DA_HUY: { nhan: 'Đã hủy', tone: 'sale' },
};

export const TRANG_THAI_DOI_TRA: Record<string, { nhan: string; tone: Tone }> = {
  CHO_DUYET: { nhan: 'Chờ duyệt', tone: 'warning' },
  DA_DUYET: { nhan: 'Đã duyệt', tone: 'neutral' },
  TU_CHOI: { nhan: 'Bị từ chối', tone: 'sale' },
  HOAN_TAT: { nhan: 'Hoàn tất', tone: 'success' },
};

export const PHUONG_THUC: Record<string, string> = {
  COD: 'Thanh toán khi nhận hàng (COD)',
  CHUYEN_KHOAN: 'Chuyển khoản ngân hàng',
  ONLINE: 'Thanh toán online',
  TIEN_MAT: 'Tiền mặt tại cửa hàng',
};

export const HANG_THANH_VIEN: Record<string, string> = {
  DONG: 'Đồng', BAC: 'Bạc', VANG: 'Vàng', KIM_CUONG: 'Kim cương',
};

export const nhanBienThe = (mau?: string | null, size?: string | null) => [mau, size].filter(Boolean).join(' / ');
