import { Tag } from 'antd';

// Nhãn tiếng Việt + màu cho các mã trạng thái. Màu luôn đi kèm chữ (không chỉ dựa vào màu).
type Nhan = { chu: string; mau: string };

export const TRANG_THAI_DON: Record<string, Nhan> = {
  CHO_XAC_NHAN: { chu: 'Chờ xác nhận', mau: 'gold' },
  DA_XAC_NHAN: { chu: 'Đã xác nhận', mau: 'blue' },
  DANG_GIAO: { chu: 'Đang giao', mau: 'geekblue' },
  HOAN_TAT: { chu: 'Hoàn tất', mau: 'green' },
  DA_HUY: { chu: 'Đã hủy', mau: 'default' },
  TU_CHOI: { chu: 'Từ chối', mau: 'red' },
};

export const TRANG_THAI_HD: Record<string, Nhan> = {
  CHO_THANH_TOAN: { chu: 'Chờ thanh toán', mau: 'gold' },
  DA_THANH_TOAN: { chu: 'Đã thanh toán', mau: 'green' },
  DA_HUY: { chu: 'Đã hủy', mau: 'default' },
};

export const TRANG_THAI_DT: Record<string, Nhan> = {
  CHO_DUYET: { chu: 'Chờ duyệt', mau: 'gold' },
  DA_DUYET: { chu: 'Đã duyệt', mau: 'blue' },
  TU_CHOI: { chu: 'Từ chối', mau: 'red' },
  HOAN_TAT: { chu: 'Hoàn tất', mau: 'green' },
};

export const TRANG_THAI_HT: Record<string, Nhan> = {
  CHO_HOAN: { chu: 'Chờ hoàn tiền', mau: 'gold' },
  DA_HOAN: { chu: 'Đã hoàn', mau: 'green' },
};

export const YEU_CAU_HUY: Record<string, Nhan> = {
  CHO_DUYET: { chu: 'Khách xin hủy', mau: 'volcano' },
  DA_DUYET: { chu: 'Đã duyệt hủy', mau: 'default' },
  TU_CHOI: { chu: 'Từ chối hủy', mau: 'purple' },
};

export const RUI_RO: Record<string, Nhan> = {
  THAP: { chu: 'Rủi ro thấp', mau: 'green' },
  TRUNG_BINH: { chu: 'Rủi ro trung bình', mau: 'gold' },
  CAO: { chu: 'Rủi ro cao', mau: 'red' },
};

export const PHUONG_THUC: Record<string, string> = {
  TIEN_MAT: 'Tiền mặt',
  COD: 'COD',
  CHUYEN_KHOAN: 'Chuyển khoản',
  ONLINE: 'Thanh toán online',
};

export const HANG_TV: Record<string, Nhan> = {
  DONG: { chu: 'Đồng', mau: 'orange' },
  BAC: { chu: 'Bạc', mau: 'default' },
  VANG: { chu: 'Vàng', mau: 'gold' },
  KIM_CUONG: { chu: 'Kim cương', mau: 'cyan' },
};

export function The({ bang, ma }: { bang: Record<string, Nhan>; ma?: string | null }) {
  if (!ma) return null;
  const n = bang[ma] ?? { chu: ma, mau: 'default' };
  return (
    <Tag color={n.mau} bordered={false} style={{ marginInlineEnd: 0 }}>
      {n.chu}
    </Tag>
  );
}
