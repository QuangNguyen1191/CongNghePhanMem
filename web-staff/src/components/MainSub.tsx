import { Empty, Table } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useEffect, useRef, type ReactNode } from 'react';
import { usePhimTat } from '../lib/phimTat';

// Yêu cầu 4: form main-sub. Bấm bất kỳ chỗ nào trên 1 dòng (hoặc dùng ↑ ↓) là chi tiết hiện ngay bên phải,
// không có nút "Chi tiết".
interface Props<T> {
  duLieu: T[];
  khoa: keyof T & string;
  cot: ColumnsType<T>;
  dangChon: string | null;
  chon: (k: string | null) => void;
  dangTai?: boolean;
  chiTiet: ReactNode;
  boLoc?: ReactNode;
  tieuDeDs?: ReactNode;
  rongCot?: number;
}

export function MainSub<T extends object>({ duLieu, khoa, cot, dangChon, chon, dangTai, chiTiet, boLoc, tieuDeDs, rongCot = 560 }: Props<T>) {
  const vung = useRef<HTMLDivElement>(null);
  const viTri = duLieu.findIndex((d) => String(d[khoa]) === dangChon);

  const diToi = (i: number) => {
    if (duLieu.length === 0) return;
    const j = Math.max(0, Math.min(duLieu.length - 1, i));
    chon(String(duLieu[j][khoa]));
  };

  usePhimTat([
    { phim: 'ArrowDown', chay: () => diToi(viTri < 0 ? 0 : viTri + 1), trongONhap: false },
    { phim: 'ArrowUp', chay: () => diToi(viTri < 0 ? 0 : viTri - 1), trongONhap: false },
    { phim: 'Home', chay: () => diToi(0), trongONhap: false },
    { phim: 'End', chay: () => diToi(duLieu.length - 1), trongONhap: false },
  ]);

  // Tự chọn dòng đầu khi danh sách đổi mà dòng đang chọn không còn
  useEffect(() => {
    if (dangTai) return;
    if (duLieu.length === 0) {
      if (dangChon !== null) chon(null);
    } else if (viTri < 0 && !dangChon) chon(String(duLieu[0][khoa]));
  }, [duLieu, viTri, dangTai]); // eslint-disable-line react-hooks/exhaustive-deps

  // Cuộn dòng đang chọn vào tầm nhìn khi dùng phím
  useEffect(() => {
    vung.current?.querySelector('.dong-dang-chon')?.scrollIntoView({ block: 'nearest' });
  }, [dangChon]);

  return (
    <div className="main-sub" style={{ gridTemplateColumns: `minmax(380px, ${rongCot}px) minmax(0, 1fr)` }}>
      <section className="khung main-sub-ds" aria-label="Danh sách">
        {(tieuDeDs || boLoc) && (
          <div className="khung-dau">
            {tieuDeDs}
            {boLoc}
          </div>
        )}
        <div ref={vung} className="main-sub-bang">
          <Table<T>
            size="small"
            rowKey={khoa}
            columns={cot}
            dataSource={duLieu}
            loading={dangTai}
            pagination={false}
            sticky
            locale={{ emptyText: <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="Không có dữ liệu" /> }}
            rowClassName={(r) => (String(r[khoa]) === dangChon ? 'dong-dang-chon' : '')}
            onRow={(r) => ({ onClick: () => chon(String(r[khoa])), style: { cursor: 'pointer' } })}
          />
        </div>
        <div className="khung-chan">
          {duLieu.length} dòng · <kbd>↑</kbd> <kbd>↓</kbd> để chọn
        </div>
      </section>
      <section className="khung main-sub-ct" aria-label="Chi tiết" aria-live="polite">
        {chiTiet}
      </section>
    </div>
  );
}
