import { FileExcelOutlined, PrinterOutlined, RetweetOutlined, SearchOutlined } from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import { Alert, App as AntApp, Button, Checkbox, DatePicker, Input, Select, Skeleton, Table, Tooltip } from 'antd';
import type { InputRef } from 'antd';
import dayjs, { type Dayjs } from 'dayjs';
import { useMemo, useRef, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { get, taiFile } from '../api/client';
import type { ChiTietHoaDon, HoaDon } from '../api/types';
import { ChuaChon, loiThanhChu, ThongTin, TieuDeTrang } from '../components/Chung';
import { MainSub } from '../components/MainSub';
import { bienThe, boDau, ngay, ngayIso, tien } from '../lib/dinhDang';
import { PHUONG_THUC, The, TRANG_THAI_HD } from '../lib/nhan';
import { usePhien } from '../lib/phien';
import { usePhimTat } from '../lib/phimTat';

export default function HoaDonPage() {
  const { phien } = usePhien();
  const { message } = AntApp.useApp();
  const [params, setParams] = useSearchParams();
  const dangChon = params.get('ma');
  const [khoang, setKhoang] = useState<[Dayjs, Dayjs] | null>([dayjs().subtract(30, 'day'), dayjs()]);
  const [trangThai, setTrangThai] = useState('');
  const [cuaToi, setCuaToi] = useState(false);
  const [tuKhoa, setTuKhoa] = useState('');
  const oTim = useRef<InputRef>(null);

  const query = {
    tuNgay: khoang ? ngayIso(khoang[0]) : undefined,
    denNgay: khoang ? ngayIso(khoang[1]) : undefined,
    trangThai: trangThai || undefined,
  };
  const ds = useQuery({
    queryKey: ['hoa-don', 'ds', query, cuaToi],
    queryFn: () => get<HoaDon[]>('/api/hoa-don', { ...query, maNv: cuaToi ? phien?.maNv : undefined }),
  });
  const loc = useMemo(() => {
    const k = boDau(tuKhoa.trim());
    return k ? (ds.data ?? []).filter((h) => boDau(`${h.maHoaDon} ${h.maDon ?? ''} ${h.tenKhachHang ?? ''} ${h.maKh ?? ''}`).includes(k)) : ds.data ?? [];
  }, [ds.data, tuKhoa]);
  const tong = loc.filter((h) => h.trangThai !== 'DA_HUY').reduce((s, h) => s + h.tongTien, 0);

  usePhimTat([
    { phim: '/', chay: () => oTim.current?.focus() },
    { phim: 'Alt+L', chay: () => ds.refetch() },
  ]);

  return (
    <>
      <TieuDeTrang
        tieuDe="Hóa đơn"
        moTa="Bấm vào một hóa đơn là chi tiết hiện ngay bên phải (form main-sub)."
        phai={
          <Button icon={<FileExcelOutlined />} onClick={() => taiFile('/api/excel/hoa-don', 'luu', query).catch((e) => message.error(loiThanhChu(e)))}>
            Xuất Excel
          </Button>
        }
      />
      <MainSub<HoaDon>
        duLieu={loc}
        khoa="maHoaDon"
        dangChon={dangChon}
        chon={(m) => setParams(m ? { ma: m } : {}, { replace: true })}
        dangTai={ds.isLoading}
        rongCot={600}
        boLoc={
          <>
            <Input ref={oTim} allowClear prefix={<SearchOutlined aria-hidden />} placeholder="Mã HĐ, mã đơn, khách" aria-label="Lọc hóa đơn" value={tuKhoa} onChange={(e) => setTuKhoa(e.target.value)} style={{ flex: '1 1 160px' }} />
            <DatePicker.RangePicker value={khoang} onChange={(v) => setKhoang(v as [Dayjs, Dayjs] | null)} format="DD/MM/YYYY" aria-label="Khoảng ngày lập" style={{ flex: '1 1 220px' }} />
            <Select
              value={trangThai}
              onChange={setTrangThai}
              style={{ width: 150 }}
              aria-label="Trạng thái"
              options={[{ value: '', label: 'Mọi trạng thái' }, ...Object.entries(TRANG_THAI_HD).map(([k, v]) => ({ value: k, label: v.chu }))]}
            />
            <Checkbox checked={cuaToi} onChange={(e) => setCuaToi(e.target.checked)}>
              Do tôi lập
            </Checkbox>
          </>
        }
        tieuDeDs={
          <div style={{ width: '100%', fontSize: 13, color: '#525252' }}>
            {loc.length} hóa đơn · doanh thu (trừ đã hủy) <b className="so" style={{ color: '#111' }}>{tien(tong)}</b>
          </div>
        }
        cot={[
          { title: 'Mã HĐ', dataIndex: 'maHoaDon', width: 92, render: (v, h) => <span><b>{v}</b>{h.maDon && <div style={{ fontSize: 11, color: '#737373' }}>Đơn {h.maDon}</div>}</span> },
          { title: 'Ngày', dataIndex: 'ngayLap', width: 92, render: ngay },
          { title: 'Khách', ellipsis: true, render: (_, h) => h.tenKhachHang ?? <span style={{ color: '#737373' }}>Khách lẻ</span> },
          { title: 'Trạng thái', dataIndex: 'trangThai', width: 124, render: (v) => <The bang={TRANG_THAI_HD} ma={v} /> },
          { title: 'Tổng', dataIndex: 'tongTien', width: 104, align: 'right', className: 'so', render: tien },
        ]}
        chiTiet={dangChon ? <ChiTiet key={dangChon} hd={loc.find((h) => h.maHoaDon === dangChon)} ma={dangChon} /> : <ChuaChon />}
      />
    </>
  );
}

function ChiTiet({ ma, hd: hdDs }: { ma: string; hd?: HoaDon }) {
  const { message } = AntApp.useApp();
  const nav = useNavigate();
  // Thông tin đầu phiếu lấy lại từ danh sách (đã có sẵn) — chỉ gọi thêm 1 API lấy chi tiết (yêu cầu 4)
  const dau = useQuery({ queryKey: ['hoa-don', 'dau', ma], queryFn: () => get<HoaDon>(`/api/hoa-don/${ma}`), enabled: !hdDs });
  const ct = useQuery({ queryKey: ['hoa-don', 'ct', ma], queryFn: () => get<ChiTietHoaDon[]>(`/api/hoa-don/${ma}/chi-tiet`) });
  const hd = hdDs ?? dau.data;
  const inPdf = () => taiFile(`/api/hoa-don/${ma}/pdf`).catch((e) => message.error(loiThanhChu(e)));
  usePhimTat([{ phim: 'Alt+P', chay: inPdf }]);

  if (!hd) return dau.isError ? <Alert type="error" message={loiThanhChu(dau.error)} style={{ margin: 16 }} /> : <div style={{ padding: 16 }}><Skeleton active /></div>;
  const conDoiTra = hd.trangThai === 'DA_THANH_TOAN' && dayjs().diff(dayjs(hd.ngayLap), 'day') <= 30;

  return (
    <>
      <div className="ct-dau">
        <div>
          <div className="ct-ma">
            {hd.maHoaDon} <The bang={TRANG_THAI_HD} ma={hd.trangThai} />
          </div>
          <div className="ct-phu">
            Lập ngày {ngay(hd.ngayLap)} · {hd.tenNhanVien ?? hd.maNv}
            {hd.maDon ? ` · từ đơn online ${hd.maDon}` : ' · bán tại quầy'}
          </div>
        </div>
        <div className="ct-hanh-dong">
          {conDoiTra && (
            <Button icon={<RetweetOutlined />} onClick={() => nav(`/doi-tra?lap=${hd.maHoaDon}`)}>
              Lập đổi trả
            </Button>
          )}
          <Tooltip title="Alt+P">
            <Button type="primary" icon={<PrinterOutlined />} onClick={inPdf}>
              In hóa đơn
            </Button>
          </Tooltip>
        </div>
      </div>
      <div className="ct-phan">
        <ThongTin
          items={[
            { nhan: 'Khách hàng', gt: hd.tenKhachHang ? `${hd.tenKhachHang} · ${hd.maKh}` : 'Khách lẻ' },
            { nhan: 'Thanh toán', gt: PHUONG_THUC[hd.phuongThucTt] ?? hd.phuongThucTt },
            { nhan: 'Khuyến mãi', gt: hd.tenKm ?? hd.maKm ?? 'Không' },
            { nhan: 'Nhân viên', gt: `${hd.tenNhanVien ?? ''} · ${hd.maNv}` },
          ]}
        />
      </div>
      <div className="ct-phan">
        <h3>Chi tiết hóa đơn</h3>
        <Table<ChiTietHoaDon>
          size="small"
          rowKey="maSp"
          loading={ct.isLoading}
          pagination={false}
          dataSource={ct.data}
          columns={[
            { title: 'Mã SP', dataIndex: 'maSp', width: 100 },
            { title: 'Sản phẩm', render: (_, c) => <>{c.tenSp} <span style={{ color: '#737373' }}>{bienThe(c.kichCo, c.mauSac)}</span></> },
            { title: 'SL', dataIndex: 'soLuong', width: 50, align: 'right', className: 'so' },
            { title: 'Đơn giá', dataIndex: 'donGiaBan', width: 110, align: 'right', className: 'so', render: tien },
            { title: 'Thành tiền', dataIndex: 'thanhTien', width: 120, align: 'right', className: 'so', render: tien },
          ]}
        />
        <div className="tong-cong" style={{ marginTop: 12 }}>
          <span>Tạm tính</span>
          <span style={{ textAlign: 'right' }}>{tien(hd.tongTien + hd.tienGiam)}</span>
          {hd.tienGiam > 0 && (<><span>Giảm giá</span><span style={{ textAlign: 'right' }}>−{tien(hd.tienGiam)}</span></>)}
          <span className="lon">Tổng cộng</span>
          <span className="lon" style={{ textAlign: 'right' }}>{tien(hd.tongTien)}</span>
        </div>
      </div>
    </>
  );
}
