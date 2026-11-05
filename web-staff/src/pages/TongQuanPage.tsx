import { ExclamationCircleOutlined, InboxOutlined, RetweetOutlined, ShoppingOutlined, WalletOutlined } from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import { Table } from 'antd';
import dayjs from 'dayjs';
import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { get } from '../api/client';
import type { DoiTra, DonDatHang, HoanTien, SanPham } from '../api/types';
import { TieuDeTrang } from '../components/Chung';
import { bienThe, ngayGio, tien } from '../lib/dinhDang';
import { The, TRANG_THAI_DON } from '../lib/nhan';
import { usePhien } from '../lib/phien';

function TheSo({ nhan, giaTri, goiY, den, icon, nhanManh }: { nhan: string; giaTri: number | string; goiY: string; den: string; icon: ReactNode; nhanManh?: boolean }) {
  return (
    <Link to={den} className={`khung the-so${nhanManh ? ' nhan-manh' : ''}`}>
      <div className="nhan">
        <span aria-hidden>{icon}</span>
        {nhan}
      </div>
      <div className="gia-tri">{giaTri}</div>
      <div className="goi-y">{goiY}</div>
    </Link>
  );
}

export default function TongQuanPage() {
  const { phien, coQuyen } = usePhien();
  const opt = { refetchInterval: 30_000 };
  const don = useQuery({ queryKey: ['don-hang', 'cho'], queryFn: () => get<DonDatHang[]>('/api/don-hang', { trangThai: 'CHO_XAC_NHAN' }), ...opt });
  const huy = useQuery({ queryKey: ['don-hang', 'xin-huy'], queryFn: () => get<DonDatHang[]>('/api/don-hang', { yeuCauHuy: 'CHO_DUYET' }), ...opt });
  const dt = useQuery({ queryKey: ['doi-tra', 'cho'], queryFn: () => get<DoiTra[]>('/api/doi-tra', { trangThai: 'CHO_DUYET' }), ...opt });
  const ht = useQuery({ queryKey: ['hoan-tien', 'cho'], queryFn: () => get<HoanTien[]>('/api/hoan-tien', { trangThai: 'CHO_HOAN' }), ...opt });
  const tonThap = useQuery({ queryKey: ['san-pham', 'ton-thap'], queryFn: () => get<SanPham[]>('/api/san-pham/ton-thap') });

  const gio = dayjs().hour();
  const chao = gio < 11 ? 'Chào buổi sáng' : gio < 14 ? 'Chào buổi trưa' : gio < 18 ? 'Chào buổi chiều' : 'Chào buổi tối';
  const choLau = (don.data ?? []).filter((d) => dayjs().diff(dayjs(d.ngayDat), 'hour') >= 24).length;

  return (
    <>
      <TieuDeTrang tieuDe={`${chao}, ${phien?.hoTen.split(' ').pop()}`} moTa={`Hôm nay ${dayjs().format('dddd, DD/MM/YYYY')}. Số liệu tự làm mới mỗi 30 giây.`} />
      <div className="luoi-the">
        {coQuyen(['don-online']) && (
          <TheSo nhan="Đơn chờ xác nhận" giaTri={don.data?.length ?? '…'} goiY={choLau ? `${choLau} đơn đã chờ quá 24 giờ` : 'Xác nhận để trừ kho, lập hóa đơn'} den="/don-hang?loc=CHO_XAC_NHAN" icon={<ShoppingOutlined />} nhanManh={(don.data?.length ?? 0) > 0} />
        )}
        {coQuyen(['don-online']) && (
          <TheSo nhan="Khách xin hủy đơn" giaTri={huy.data?.length ?? '…'} goiY="Duyệt hoặc từ chối trước khi giao" den="/don-hang?loc=XIN_HUY" icon={<ExclamationCircleOutlined />} nhanManh={(huy.data?.length ?? 0) > 0} />
        )}
        {coQuyen(['ban-hang', 'don-online']) && (
          <TheSo nhan="Đổi trả chờ duyệt" giaTri={dt.data?.length ?? '…'} goiY="Yêu cầu từ web và tại quầy" den="/doi-tra" icon={<RetweetOutlined />} nhanManh={(dt.data?.length ?? 0) > 0} />
        )}
        {coQuyen(['ban-hang', 'don-online']) && (
          <TheSo nhan="Chờ hoàn tiền" giaTri={ht.data?.length ?? '…'} goiY={ht.data?.length ? `Tổng ${tien(ht.data.reduce((s, h) => s + h.soTien, 0))}` : 'Đơn đã trả trước bị hủy'} den="/hoan-tien" icon={<WalletOutlined />} nhanManh={(ht.data?.length ?? 0) > 0} />
        )}
        <TheSo nhan="Sản phẩm sắp hết" giaTri={tonThap.data?.length ?? '…'} goiY="Tồn kho dưới 5" den="/san-pham?loc=ton-thap" icon={<InboxOutlined />} />
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(420px, 1fr))', gap: 16 }}>
        {coQuyen(['don-online']) && (
          <section className="khung">
            <div className="khung-dau" style={{ justifyContent: 'space-between' }}>
              <h2>Đơn mới cần xử lý</h2>
              <Link to="/don-hang">Xem tất cả</Link>
            </div>
            <Table<DonDatHang>
              size="small"
              rowKey="maDon"
              loading={don.isLoading}
              pagination={false}
              dataSource={[...(huy.data ?? []), ...(don.data ?? [])].slice(0, 8)}
              columns={[
                { title: 'Mã đơn', dataIndex: 'maDon', render: (v) => <Link to={`/don-hang?ma=${v}`}>{v}</Link> },
                { title: 'Khách', render: (_, d) => d.tenNguoiNhan ?? d.tenKhachHang },
                { title: 'Đặt lúc', dataIndex: 'ngayDat', render: ngayGio },
                { title: 'Trạng thái', render: (_, d) => (d.yeuCauHuy === 'CHO_DUYET' ? <The bang={{ X: { chu: 'Khách xin hủy', mau: 'volcano' } }} ma="X" /> : <The bang={TRANG_THAI_DON} ma={d.trangThai} />) },
                { title: 'Tổng', dataIndex: 'tongTien', align: 'right', className: 'so', render: tien },
              ]}
            />
          </section>
        )}
        <section className="khung">
          <div className="khung-dau" style={{ justifyContent: 'space-between' }}>
            <h2>Sản phẩm tồn thấp</h2>
            {coQuyen(['kho']) && <Link to="/nhap-hang?lap=1">Lập phiếu nhập</Link>}
          </div>
          <Table<SanPham>
            size="small"
            rowKey="maSp"
            loading={tonThap.isLoading}
            pagination={false}
            dataSource={(tonThap.data ?? []).slice(0, 8)}
            columns={[
              { title: 'Mã SP', dataIndex: 'maSp' },
              { title: 'Sản phẩm', render: (_, s) => <>{s.tenSp} <span style={{ color: '#737373' }}>{bienThe(s.kichCo, s.mauSac)}</span></> },
              { title: 'Tồn', dataIndex: 'soLuong', align: 'right', className: 'so', render: (v: number) => <b style={{ color: v === 0 ? '#c8102e' : undefined }}>{v}</b> },
            ]}
          />
        </section>
      </div>
    </>
  );
}
