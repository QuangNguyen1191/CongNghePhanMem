import { App as AntApp, Descriptions, Empty, Typography } from 'antd';
import type { ReactNode } from 'react';
import { LoiApi } from '../api/client';

export function TieuDeTrang({ tieuDe, moTa, phai }: { tieuDe: string; moTa?: ReactNode; phai?: ReactNode }) {
  return (
    <header className="tieu-de-trang">
      <div>
        <Typography.Title level={1} className="tieu-de-h1">
          {tieuDe}
        </Typography.Title>
        {moTa && <div className="mo-ta">{moTa}</div>}
      </div>
      {phai && <div className="tieu-de-phai">{phai}</div>}
    </header>
  );
}

export function ChuaChon({ chu = 'Chọn một dòng bên trái để xem chi tiết' }: { chu?: string }) {
  return (
    <div className="chua-chon">
      <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description={chu} />
    </div>
  );
}

export function ThongTin({ items, cot = 2 }: { items: { nhan: string; gt: ReactNode; span?: number }[]; cot?: number }) {
  return (
    <Descriptions
      size="small"
      column={cot}
      colon={false}
      className="thong-tin"
      items={items.map((i, k) => ({ key: k, label: i.nhan, children: i.gt ?? '—', span: i.span }))}
    />
  );
}

export const loiThanhChu = (e: unknown) => (e instanceof LoiApi || e instanceof Error ? e.message : 'Có lỗi xảy ra');

/** Hook nhỏ: chạy thao tác, báo thành công / lỗi bằng message. */
export function useThaoTac() {
  const { message } = AntApp.useApp();
  return async <T,>(lam: () => Promise<T>, thanhCong?: string): Promise<T | undefined> => {
    try {
      const kq = await lam();
      if (thanhCong) message.success(thanhCong);
      return kq;
    } catch (e) {
      message.error(loiThanhChu(e));
      return undefined;
    }
  };
}
