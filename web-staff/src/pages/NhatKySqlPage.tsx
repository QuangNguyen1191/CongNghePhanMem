import { ReloadOutlined } from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import { Button, Input, Table } from 'antd';
import { useMemo, useState } from 'react';
import { get } from '../api/client';
import type { NhatKySql } from '../api/types';
import { TieuDeTrang } from '../components/Chung';
import { usePhimTat } from '../lib/phimTat';

// Yêu cầu 1: chỉ ra dòng code chạy câu truy vấn. Mọi câu SQL đều nằm ở lớp DAL và được ghi lại kèm file + số dòng + hàm gọi.
export default function NhatKySqlPage() {
  const [loc, setLoc] = useState('');
  const ds = useQuery({ queryKey: ['nhat-ky-sql'], queryFn: () => get<NhatKySql[]>('/api/nhat-ky-sql', { soLuong: 200 }), staleTime: 0 });
  usePhimTat([{ phim: 'Alt+L', chay: () => ds.refetch() }]);
  const dl = useMemo(() => {
    const k = loc.trim().toLowerCase();
    return k ? (ds.data ?? []).filter((n) => `${n.cauSql} ${n.file} ${n.hamGoi}`.toLowerCase().includes(k)) : ds.data ?? [];
  }, [ds.data, loc]);

  return (
    <>
      <TieuDeTrang
        tieuDe="Nhật ký SQL"
        moTa="200 câu SQL gần nhất backend đã chạy, kèm file và dòng code trong lớp DAL (yêu cầu 1). Góc trên màn hình luôn hiện số câu SQL của thao tác vừa làm."
        phai={<Button icon={<ReloadOutlined />} onClick={() => ds.refetch()} loading={ds.isFetching}>Làm mới</Button>}
      />
      <section className="khung">
        <div className="khung-dau">
          <Input allowClear placeholder="Lọc theo câu SQL, file, hàm" aria-label="Lọc nhật ký" value={loc} onChange={(e) => setLoc(e.target.value)} style={{ maxWidth: 360 }} />
        </div>
        <Table<NhatKySql>
          size="small"
          rowKey={(r) => `${r.thoiDiem}|${r.file}|${r.dong}|${r.cauSql.length}|${r.thoiGianMs}`}
          loading={ds.isLoading}
          dataSource={dl}
          pagination={{ pageSize: 25, size: 'small' }}
          expandable={{ expandedRowRender: (r) => <pre className="sql-code">{r.cauSql}{r.thamSo ? `\n\n-- Tham số: ${r.thamSo}` : ''}</pre> }}
          columns={[
            { title: 'Lúc', dataIndex: 'thoiDiem', width: 90, render: (v) => new Date(v).toLocaleTimeString('vi-VN') },
            { title: 'Vị trí trong code', width: 280, render: (_, r) => <code style={{ fontSize: 12 }}>{r.file}:{r.dong}<div style={{ color: '#737373' }}>{r.hamGoi}()</div></code> },
            { title: 'Câu SQL', dataIndex: 'cauSql', ellipsis: true, render: (v: string) => <code style={{ fontSize: 12 }}>{v.replace(/\s+/g, ' ')}</code> },
            { title: 'Số dòng', dataIndex: 'soDong', width: 80, align: 'right', className: 'so' },
            { title: 'ms', dataIndex: 'thoiGianMs', width: 60, align: 'right', className: 'so' },
          ]}
        />
      </section>
    </>
  );
}
