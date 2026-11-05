import { CheckOutlined, ReloadOutlined } from '@ant-design/icons';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { App as AntApp, Button, Segmented, Table } from 'antd';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { get, post } from '../api/client';
import type { HoanTien } from '../api/types';
import { loiThanhChu, TieuDeTrang } from '../components/Chung';
import { useHoiLyDo } from '../components/HoiLyDo';
import { ngayGio, tien } from '../lib/dinhDang';
import { PHUONG_THUC, The, TRANG_THAI_HT } from '../lib/nhan';
import { usePhimTat } from '../lib/phimTat';

export default function HoanTienPage() {
  const { message } = AntApp.useApp();
  const qc = useQueryClient();
  const [loc, setLoc] = useState<'CHO_HOAN' | 'DA_HOAN' | ''>('CHO_HOAN');
  const [hoi, hop] = useHoiLyDo();
  const [dangLam, setDangLam] = useState<string | null>(null);
  const ds = useQuery({ queryKey: ['hoan-tien', 'ds', loc], queryFn: () => get<HoanTien[]>('/api/hoan-tien', { trangThai: loc || undefined }) });
  usePhimTat([{ phim: 'Alt+L', chay: () => ds.refetch() }]);

  const daHoan = async (h: HoanTien) => {
    const gc = await hoi({
      tieuDe: `Xác nhận đã hoàn ${tien(h.soTien)} cho đơn ${h.maDon}`,
      moTa: `Khách ${h.tenKhachHang ?? ''} · ${h.sdtNhan ?? ''} · đã trả bằng ${PHUONG_THUC[h.phuongThuc] ?? h.phuongThuc}. Ghi mã giao dịch hoặc cách hoàn để đối soát.`,
      nutOk: 'Đã hoàn tiền',
      goiY: ['Chuyển khoản lại tài khoản khách', 'Hoàn qua cổng thanh toán'],
    });
    if (gc === null) return;
    setDangLam(h.maHoanTien);
    try {
      await post(`/api/hoan-tien/${h.maHoanTien}/da-hoan`, { lyDo: gc || null });
      message.success(`Đã ghi nhận hoàn tiền ${h.maHoanTien}`);
      qc.invalidateQueries({ queryKey: ['hoan-tien'] });
      qc.invalidateQueries({ queryKey: ['don-hang'] });
    } catch (e) {
      message.error(loiThanhChu(e));
    } finally {
      setDangLam(null);
    }
  };

  const tongCho = (ds.data ?? []).filter((h) => h.trangThai === 'CHO_HOAN').reduce((s, h) => s + h.soTien, 0);

  return (
    <>
      <TieuDeTrang
        tieuDe="Hoàn tiền"
        moTa="Đơn chuyển khoản / thanh toán online bị hủy sau khi khách đã trả tiền. Hoàn xong thì đánh dấu để khách thấy trên web."
        phai={<Button icon={<ReloadOutlined />} onClick={() => ds.refetch()} loading={ds.isFetching}>Làm mới</Button>}
      />
      <section className="khung">
        <div className="khung-dau" style={{ justifyContent: 'space-between' }}>
          <Segmented
            value={loc}
            onChange={(v) => setLoc(v as typeof loc)}
            options={[
              { value: 'CHO_HOAN', label: 'Chờ hoàn' },
              { value: 'DA_HOAN', label: 'Đã hoàn' },
              { value: '', label: 'Tất cả' },
            ]}
          />
          {tongCho > 0 && (
            <span>
              Cần hoàn: <b className="so">{tien(tongCho)}</b>
            </span>
          )}
        </div>
        <Table<HoanTien>
          size="small"
          rowKey="maHoanTien"
          loading={ds.isLoading}
          dataSource={ds.data}
          pagination={{ pageSize: 20, size: 'small' }}
          columns={[
            { title: 'Mã', dataIndex: 'maHoanTien', width: 90, render: (v) => <b>{v}</b> },
            { title: 'Đơn', dataIndex: 'maDon', width: 90, render: (v) => <Link to={`/don-hang?loc=TAT_CA&ma=${v}`}>{v}</Link> },
            { title: 'Hóa đơn', dataIndex: 'maHoaDon', width: 90 },
            { title: 'Khách', render: (_, h) => <>{h.tenKhachHang}<div style={{ fontSize: 12, color: '#737373' }}>{h.sdtNhan}</div></> },
            { title: 'Đã trả qua', dataIndex: 'phuongThuc', width: 120, render: (v) => PHUONG_THUC[v] ?? v },
            { title: 'Số tiền', dataIndex: 'soTien', width: 120, align: 'right', className: 'so', render: (v) => <b>{tien(v)}</b> },
            { title: 'Tạo lúc', dataIndex: 'ngayTao', width: 130, render: ngayGio },
            { title: 'Trạng thái', dataIndex: 'trangThai', width: 120, render: (v) => <The bang={TRANG_THAI_HT} ma={v} /> },
            {
              title: 'Ghi chú',
              render: (_, h) => (
                <span style={{ fontSize: 12 }}>
                  {h.ghiChu}
                  {h.ngayHoan && <div style={{ color: '#737373' }}>{ngayGio(h.ngayHoan)} · {h.tenNhanVien}</div>}
                </span>
              ),
            },
            {
              title: '',
              width: 140,
              render: (_, h) =>
                h.trangThai === 'CHO_HOAN' && (
                  <Button size="small" type="primary" icon={<CheckOutlined />} loading={dangLam === h.maHoanTien} onClick={() => daHoan(h)}>
                    Đã hoàn tiền
                  </Button>
                ),
            },
          ]}
        />
      </section>
      {hop}
    </>
  );
}
