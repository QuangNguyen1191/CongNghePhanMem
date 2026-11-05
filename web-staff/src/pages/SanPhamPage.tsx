import { FileExcelOutlined, SearchOutlined } from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import { App as AntApp, Button, Input, Segmented, Select, Table, Tag, Tooltip } from 'antd';
import type { InputRef } from 'antd';
import { useRef, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { get, taiFile } from '../api/client';
import type { LoaiSp, SanPham } from '../api/types';
import { ChuaChon, loiThanhChu, ThongTin, TieuDeTrang } from '../components/Chung';
import { MainSub } from '../components/MainSub';
import { bienThe, tien } from '../lib/dinhDang';
import { usePhimTat } from '../lib/phimTat';
import { useCheDoTim, useTimSanPham } from '../lib/timSanPham';

const NGUONG = 5;
// Ảnh sản phẩm nằm trong web bán hàng (web-ban-hang/public/images) nên lấy từ địa chỉ của web đó
const ANH_URL = ((import.meta.env.VITE_ANH_URL as string | undefined) ?? 'http://localhost:5173').replace(/\/$/, '');

export default function SanPhamPage() {
  const { message } = AntApp.useApp();
  const [params, setParams] = useSearchParams();
  const tonThap = params.get('loc') === 'ton-thap';
  const dangChon = params.get('ma');
  const [cheDo, setCheDo] = useCheDoTim();
  const [tuKhoa, setTuKhoa] = useState('');
  const [maLoai, setMaLoai] = useState<string>('');
  const oTim = useRef<InputRef>(null);
  const loai = useQuery({ queryKey: ['loai-sp'], queryFn: () => get<LoaiSp[]>('/api/loai-sp'), staleTime: 10 * 60_000 });
  const tim = useTimSanPham(tuKhoa, cheDo, { maLoaiSp: maLoai || undefined });
  const ds = tonThap ? tim.ketQua.filter((s) => s.trangThai === 'DANG_BAN' && s.soLuong < NGUONG) : tim.ketQua;

  const datParam = (k: string, v: string | null) =>
    setParams((p) => {
      const n = new URLSearchParams(p);
      if (v) n.set(k, v);
      else n.delete(k);
      return n;
    }, { replace: true });

  usePhimTat([
    { phim: '/', chay: () => oTim.current?.focus() },
    { phim: 'F3', chay: () => setCheDo(cheDo === 'TAI_SU_DUNG' ? 'TRUY_VAN_MOI' : 'TAI_SU_DUNG') },
  ]);

  return (
    <>
      <TieuDeTrang
        tieuDe="Sản phẩm & tồn kho"
        moTa="Tra cứu giá, tồn kho và các size / màu còn hàng. Thêm, sửa sản phẩm làm ở app admin."
        phai={
          <Button icon={<FileExcelOutlined />} onClick={() => taiFile('/api/excel/san-pham', 'luu').catch((e) => message.error(loiThanhChu(e)))}>
            Xuất Excel
          </Button>
        }
      />
      <div style={{ marginBottom: 12, display: 'flex', gap: 12, flexWrap: 'wrap', alignItems: 'center' }}>
        <Segmented value={tonThap ? 'ton-thap' : ''} onChange={(v) => datParam('loc', v ? String(v) : null)} options={[{ value: '', label: 'Tất cả' }, { value: 'ton-thap', label: `Tồn thấp (< ${NGUONG})` }]} />
        <span className="che-do-tim">
          <Segmented
            size="small"
            value={cheDo}
            onChange={(v) => setCheDo(v as typeof cheDo)}
            options={[
              { value: 'TAI_SU_DUNG', label: 'Tái sử dụng danh sách' },
              { value: 'TRUY_VAN_MOI', label: 'Truy vấn mới' },
            ]}
          />
          <Tooltip title="Yêu cầu 2. F3 để đổi chế độ.">
            <span>
              <b className="so">{tim.thongKe.soCauSql}</b> câu SQL · {tim.thongKe.thoiGianMs} ms
            </span>
          </Tooltip>
        </span>
      </div>
      <MainSub<SanPham>
        duLieu={ds}
        khoa="maSp"
        dangChon={dangChon}
        chon={(m) => datParam('ma', m)}
        dangTai={tim.dangTai && ds.length === 0}
        rongCot={680}
        boLoc={
          <>
            <Input ref={oTim} allowClear prefix={<SearchOutlined aria-hidden />} placeholder="Mã, tên, màu, size" aria-label="Tìm sản phẩm" value={tuKhoa} onChange={(e) => setTuKhoa(e.target.value)} style={{ flex: '1 1 200px' }} />
            <Select value={maLoai} onChange={setMaLoai} style={{ width: 170 }} aria-label="Loại sản phẩm" options={[{ value: '', label: 'Mọi loại' }, ...(loai.data ?? []).map((l) => ({ value: l.maLoaiSp, label: l.tenLoaiSp }))]} />
          </>
        }
        cot={[
          { title: 'Mã SP', dataIndex: 'maSp', width: 100, render: (v) => <b>{v}</b> },
          { title: 'Sản phẩm', dataIndex: 'tenSp', ellipsis: true },
          { title: 'Màu / Size', width: 130, render: (_, s) => bienThe(s.kichCo, s.mauSac) },
          { title: 'Giá', dataIndex: 'donGia', width: 100, align: 'right', className: 'so', render: tien },
          {
            title: 'Tồn',
            dataIndex: 'soLuong',
            width: 80,
            align: 'right',
            sorter: (a, b) => a.soLuong - b.soLuong,
            render: (v: number, s) =>
              s.trangThai === 'NGUNG_BAN' ? <Tag bordered={false}>Ngừng bán</Tag> : v === 0 ? <Tag color="red" bordered={false}>Hết</Tag> : <b className="so" style={{ color: v < NGUONG ? '#b45309' : undefined }}>{v}</b>,
          },
        ]}
        chiTiet={dangChon ? <ChiTiet key={dangChon} sp={ds.find((s) => s.maSp === dangChon)} ma={dangChon} /> : <ChuaChon />}
      />
    </>
  );
}

function anh(h?: string | null) {
  if (!h) return null;
  if (/^https?:/.test(h)) return h;
  return `${ANH_URL}/${h.replace(/^\//, '')}`;
}

function ChiTiet({ ma, sp }: { ma: string; sp?: SanPham }) {
  const bt = useQuery({ queryKey: ['san-pham', 'bien-the', ma], queryFn: () => get<SanPham[]>(`/api/san-pham/${ma}/bien-the`) });
  const s = sp ?? bt.data?.find((x) => x.maSp === ma);
  if (!s) return <ChuaChon chu="Đang tải…" />;
  const src = anh(s.hinhAnh);
  const tongTon = (bt.data ?? []).reduce((t, x) => t + x.soLuong, 0);
  return (
    <>
      <div className="ct-dau">
        <div style={{ display: 'flex', gap: 12 }}>
          {src && <img src={src} alt="" width={72} height={72} style={{ objectFit: 'cover', borderRadius: 4, background: '#f0f0f0' }} onError={(e) => (e.currentTarget.style.display = 'none')} />}
          <div>
            <div className="ct-ma">{s.tenSp}</div>
            <div className="ct-phu">
              {s.maSp} · {s.tenLoaiSp} · {bienThe(s.kichCo, s.mauSac)}
            </div>
          </div>
        </div>
        <div style={{ textAlign: 'right' }}>
          <div style={{ fontSize: 20, fontWeight: 700 }} className="so">{tien(s.donGia)}</div>
          <div className="ct-phu">Tồn {s.soLuong}</div>
        </div>
      </div>
      <div className="ct-phan">
        <ThongTin cot={1} items={[{ nhan: 'Chất liệu', gt: s.chatLieu }, { nhan: 'Trạng thái', gt: s.trangThai === 'DANG_BAN' ? 'Đang bán' : 'Ngừng bán' }, { nhan: 'Mô tả', gt: s.moTa }]} />
      </div>
      <div className="ct-phan">
        <h3>Các size / màu của mẫu này · tổng tồn {tongTon}</h3>
        <Table<SanPham>
          size="small"
          rowKey="maSp"
          loading={bt.isLoading}
          pagination={false}
          dataSource={bt.data}
          rowClassName={(r) => (r.maSp === ma ? 'dong-dang-chon' : '')}
          columns={[
            { title: 'Mã SP', dataIndex: 'maSp', width: 100 },
            { title: 'Màu', dataIndex: 'mauSac' },
            { title: 'Size', dataIndex: 'kichCo', width: 70 },
            { title: 'Giá', dataIndex: 'donGia', width: 100, align: 'right', className: 'so', render: tien },
            { title: 'Tồn', dataIndex: 'soLuong', width: 70, align: 'right', render: (v: number) => (v === 0 ? <Tag color="red" bordered={false}>Hết</Tag> : <b className="so">{v}</b>) },
          ]}
        />
      </div>
    </>
  );
}
