import { DeleteOutlined, DownloadOutlined, FileExcelOutlined, PlusOutlined, PrinterOutlined, UploadOutlined } from '@ant-design/icons';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Alert, App as AntApp, Button, DatePicker, InputNumber, Modal, Select, Skeleton, Table, Upload } from 'antd';
import dayjs, { type Dayjs } from 'dayjs';
import { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { api, get, post, taiFile } from '../api/client';
import type { ChiTietPhieuNhap, KetQuaDocExcel, NhaCungCap, PhieuNhap, SanPham } from '../api/types';
import { ChuaChon, loiThanhChu, ThongTin, TieuDeTrang } from '../components/Chung';
import { MainSub } from '../components/MainSub';
import { bienThe, boDau, ngay, ngayIso, so, tien } from '../lib/dinhDang';
import { usePhimTat } from '../lib/phimTat';

export default function NhapHangPage() {
  const { message } = AntApp.useApp();
  const [params, setParams] = useSearchParams();
  const dangChon = params.get('ma');
  const [moLap, setMoLap] = useState(params.get('lap') === '1');
  const [khoang, setKhoang] = useState<[Dayjs, Dayjs] | null>([dayjs().subtract(90, 'day'), dayjs()]);
  const [maNcc, setMaNcc] = useState('');
  const ncc = useQuery({ queryKey: ['ncc'], queryFn: () => get<NhaCungCap[]>('/api/nha-cung-cap'), staleTime: 5 * 60_000 });
  const query = { tuNgay: khoang ? ngayIso(khoang[0]) : undefined, denNgay: khoang ? ngayIso(khoang[1]) : undefined };
  const ds = useQuery({ queryKey: ['phieu-nhap', 'ds', query, maNcc], queryFn: () => get<PhieuNhap[]>('/api/phieu-nhap', { ...query, maNcc: maNcc || undefined }) });

  usePhimTat([
    { phim: 'Alt+N', chay: () => setMoLap(true) },
    { phim: 'Alt+L', chay: () => ds.refetch() },
  ]);

  return (
    <>
      <TieuDeTrang
        tieuDe="Nhập hàng"
        moTa="Lập phiếu nhập từ nhà cung cấp (gõ tay hoặc đọc file Excel). Lưu phiếu là cộng tồn kho ngay."
        phai={
          <>
            <Button icon={<FileExcelOutlined />} onClick={() => taiFile('/api/excel/phieu-nhap', 'luu', query).catch((e) => message.error(loiThanhChu(e)))}>
              Xuất Excel
            </Button>
            <Button type="primary" icon={<PlusOutlined />} onClick={() => setMoLap(true)}>
              Lập phiếu nhập <kbd className="kbd-toi">Alt N</kbd>
            </Button>
          </>
        }
      />
      <MainSub<PhieuNhap>
        duLieu={ds.data ?? []}
        khoa="maPhieuNhap"
        dangChon={dangChon}
        chon={(m) => setParams(m ? { ma: m } : {}, { replace: true })}
        dangTai={ds.isLoading}
        boLoc={
          <>
            <DatePicker.RangePicker value={khoang} onChange={(v) => setKhoang(v as [Dayjs, Dayjs] | null)} format="DD/MM/YYYY" aria-label="Khoảng ngày nhập" style={{ flex: '1 1 220px' }} />
            <Select value={maNcc} onChange={setMaNcc} style={{ flex: '1 1 180px' }} aria-label="Nhà cung cấp" options={[{ value: '', label: 'Mọi nhà cung cấp' }, ...(ncc.data ?? []).map((n) => ({ value: n.maNcc, label: n.tenNcc }))]} />
          </>
        }
        cot={[
          { title: 'Mã phiếu', dataIndex: 'maPhieuNhap', width: 96, render: (v) => <b>{v}</b> },
          { title: 'Ngày', dataIndex: 'ngayNhap', width: 96, render: ngay },
          { title: 'Nhà cung cấp', dataIndex: 'tenNcc', ellipsis: true },
          { title: 'Tổng tiền', dataIndex: 'tongTien', width: 120, align: 'right', className: 'so', render: tien },
        ]}
        chiTiet={dangChon ? <ChiTiet key={dangChon} ma={dangChon} pn={ds.data?.find((p) => p.maPhieuNhap === dangChon)} /> : <ChuaChon />}
      />
      <LapPhieuNhap
        mo={moLap}
        ncc={(ncc.data ?? []).filter((n) => n.trangThai === 'HOP_TAC')}
        dong={() => setMoLap(false)}
        xong={(ma) => {
          setMoLap(false);
          setParams({ ma }, { replace: true });
        }}
      />
    </>
  );
}

function ChiTiet({ ma, pn }: { ma: string; pn?: PhieuNhap }) {
  const { message } = AntApp.useApp();
  const ct = useQuery({ queryKey: ['phieu-nhap', 'ct', ma], queryFn: () => get<ChiTietPhieuNhap[]>(`/api/phieu-nhap/${ma}/chi-tiet`) });
  const dau = useQuery({ queryKey: ['phieu-nhap', 'dau', ma], queryFn: () => get<PhieuNhap>(`/api/phieu-nhap/${ma}`), enabled: !pn });
  const p = pn ?? dau.data;
  const inPdf = () => taiFile(`/api/phieu-nhap/${ma}/pdf`).catch((e) => message.error(loiThanhChu(e)));
  usePhimTat([{ phim: 'Alt+P', chay: inPdf }]);
  if (!p) return <div style={{ padding: 16 }}><Skeleton active /></div>;
  const tongSl = (ct.data ?? []).reduce((s, c) => s + c.soLuong, 0);
  return (
    <>
      <div className="ct-dau">
        <div>
          <div className="ct-ma">{p.maPhieuNhap}</div>
          <div className="ct-phu">
            Ngày {ngay(p.ngayNhap)} · lập bởi {p.tenNhanVien ?? p.maNv}
          </div>
        </div>
        <Button type="primary" icon={<PrinterOutlined />} onClick={inPdf}>
          In phiếu nhập
        </Button>
      </div>
      <div className="ct-phan">
        <ThongTin items={[{ nhan: 'Nhà cung cấp', gt: `${p.tenNcc ?? ''} · ${p.maNcc}` }, { nhan: 'Tổng số lượng', gt: so(tongSl) }]} />
      </div>
      <div className="ct-phan">
        <h3>Chi tiết phiếu nhập</h3>
        <Table<ChiTietPhieuNhap>
          size="small"
          rowKey="maSp"
          loading={ct.isLoading}
          pagination={false}
          dataSource={ct.data}
          columns={[
            { title: 'Mã SP', dataIndex: 'maSp', width: 100 },
            { title: 'Sản phẩm', render: (_, c) => <>{c.tenSp} <span style={{ color: '#737373' }}>{bienThe(c.kichCo, c.mauSac)}</span></> },
            { title: 'SL', dataIndex: 'soLuong', width: 60, align: 'right', className: 'so' },
            { title: 'Giá nhập', dataIndex: 'donGiaNhap', width: 110, align: 'right', className: 'so', render: tien },
            { title: 'Thành tiền', dataIndex: 'thanhTien', width: 120, align: 'right', className: 'so', render: tien },
          ]}
        />
        <div className="tong-cong" style={{ marginTop: 12 }}>
          <span className="lon">Tổng tiền</span>
          <span className="lon" style={{ textAlign: 'right' }}>{tien(p.tongTien)}</span>
        </div>
      </div>
    </>
  );
}

interface Dong {
  khoa: number;
  maSp?: string;
  soLuong: number;
  donGiaNhap: number | null;
}

let dem = 1;

function LapPhieuNhap({ mo, ncc, dong, xong }: { mo: boolean; ncc: NhaCungCap[]; dong: () => void; xong: (ma: string) => void }) {
  const { message, modal } = AntApp.useApp();
  const qc = useQueryClient();
  const [maNcc, setMaNcc] = useState<string>();
  const [dongs, setDongs] = useState<Dong[]>([]);
  const [loiExcel, setLoiExcel] = useState<KetQuaDocExcel<ChiTietPhieuNhap>['lois']>([]);
  const [dangDoc, setDangDoc] = useState(false);
  const [dangGui, setDangGui] = useState(false);
  const sp = useQuery({ queryKey: ['san-pham', 'tat-ca', false], queryFn: () => get<SanPham[]>('/api/san-pham', { chiDangBan: false }), enabled: mo, staleTime: 5 * 60_000 });
  const theoMa = useMemo(() => new Map((sp.data ?? []).map((s) => [s.maSp, s])), [sp.data]);

  useEffect(() => {
    if (mo) {
      setDongs([{ khoa: dem++, soLuong: 1, donGiaNhap: null }]);
      setLoiExcel([]);
      setMaNcc(undefined);
    }
  }, [mo]);

  const sua = (k: number, p: Partial<Dong>) => setDongs((ds) => ds.map((d) => (d.khoa === k ? { ...d, ...p } : d)));
  const hopLe = dongs.filter((d) => d.maSp && d.soLuong > 0 && (d.donGiaNhap ?? 0) > 0);
  const tong = hopLe.reduce((s, d) => s + d.soLuong * (d.donGiaNhap ?? 0), 0);
  const trung = new Set(hopLe.map((d) => d.maSp)).size !== hopLe.length;

  const docExcel = async (file: File) => {
    setDangDoc(true);
    try {
      const fd = new FormData();
      fd.append('file', file);
      const kq = await api<KetQuaDocExcel<ChiTietPhieuNhap>>('/api/excel/doc/chi-tiet-phieu-nhap', { method: 'POST', form: fd });
      setLoiExcel(kq.lois);
      const moi = kq.hopLe.map((c) => ({ khoa: dem++, maSp: c.maSp, soLuong: c.soLuong, donGiaNhap: c.donGiaNhap }));
      setDongs((ds) => [...ds.filter((d) => d.maSp), ...moi]);
      message.success(`Đọc ${kq.tongSoDong} dòng: ${kq.hopLe.length} hợp lệ, ${kq.lois.length} lỗi`);
    } catch (e) {
      message.error(loiThanhChu(e));
    } finally {
      setDangDoc(false);
    }
    return false;
  };

  const gui = async () => {
    if (!maNcc) return message.warning('Chọn nhà cung cấp');
    if (hopLe.length === 0) return message.warning('Thêm ít nhất 1 dòng có sản phẩm, số lượng và giá nhập');
    if (trung) return message.warning('Có sản phẩm bị lặp, gộp lại thành 1 dòng');
    const ok = await modal.confirm({ title: `Lưu phiếu nhập ${tien(tong)}?`, content: `${hopLe.length} sản phẩm, tồn kho được cộng ngay sau khi lưu.`, okText: 'Lưu phiếu', cancelText: 'Quay lại' });
    if (!ok) return;
    setDangGui(true);
    try {
      const p = await post<PhieuNhap>('/api/phieu-nhap', { maNcc, chiTiets: hopLe.map((d) => ({ maSp: d.maSp, soLuong: d.soLuong, donGiaNhap: d.donGiaNhap })) });
      message.success(`Đã lưu phiếu ${p.maPhieuNhap}`);
      qc.invalidateQueries({ queryKey: ['phieu-nhap'] });
      qc.invalidateQueries({ queryKey: ['san-pham'] });
      xong(p.maPhieuNhap);
    } catch (e) {
      message.error(loiThanhChu(e));
    } finally {
      setDangGui(false);
    }
  };

  const tuyChonSp = useMemo(
    () => (sp.data ?? []).map((s) => ({ value: s.maSp, label: `${s.maSp} · ${s.tenSp} · ${bienThe(s.kichCo, s.mauSac)} (tồn ${s.soLuong})`, tim: boDau(`${s.maSp} ${s.tenSp} ${s.mauSac ?? ''} ${s.kichCo ?? ''}`) })),
    [sp.data],
  );

  return (
    <Modal open={mo} onCancel={dong} width={920} title="Lập phiếu nhập hàng" okText="Lưu phiếu nhập" cancelText="Hủy" onOk={gui} confirmLoading={dangGui} destroyOnClose maskClosable={false}>
      <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', marginBottom: 12 }}>
        <Select autoFocus showSearch optionFilterProp="label" placeholder="Chọn nhà cung cấp" aria-label="Nhà cung cấp" value={maNcc} onChange={setMaNcc} style={{ flex: '1 1 280px' }} options={ncc.map((n) => ({ value: n.maNcc, label: `${n.tenNcc} (${n.maNcc})` }))} />
        <Button icon={<DownloadOutlined />} onClick={() => taiFile('/api/excel/mau/chi-tiet-phieu-nhap', 'luu').catch((e) => message.error(loiThanhChu(e)))}>
          File Excel mẫu
        </Button>
        <Upload accept=".xlsx" showUploadList={false} beforeUpload={docExcel}>
          <Button icon={<UploadOutlined />} loading={dangDoc}>
            Đọc từ Excel
          </Button>
        </Upload>
      </div>
      {loiExcel.length > 0 && (
        <Alert
          type="warning"
          showIcon
          closable
          onClose={() => setLoiExcel([])}
          style={{ marginBottom: 12 }}
          message={`${loiExcel.length} dòng trong file bị bỏ qua`}
          description={
            <ul style={{ margin: 0, paddingLeft: 18, maxHeight: 120, overflow: 'auto' }}>
              {loiExcel.map((l, i) => (
                <li key={i}>
                  Dòng {l.dong}: {l.thongBao}
                </li>
              ))}
            </ul>
          }
        />
      )}
      <Table<Dong>
        size="small"
        rowKey="khoa"
        pagination={false}
        dataSource={dongs}
        scroll={{ y: 360 }}
        columns={[
          {
            title: 'Sản phẩm',
            render: (_, d) => (
              <Select
                showSearch
                style={{ width: '100%' }}
                placeholder="Gõ mã hoặc tên sản phẩm"
                aria-label="Sản phẩm"
                loading={sp.isLoading}
                value={d.maSp}
                status={d.maSp && dongs.filter((x) => x.maSp === d.maSp).length > 1 ? 'warning' : undefined}
                onChange={(v) => sua(d.khoa, { maSp: v })}
                filterOption={(input, o) => (o as { tim: string }).tim.includes(boDau(input))}
                options={tuyChonSp}
              />
            ),
          },
          { title: 'Tồn', width: 60, align: 'right', render: (_, d) => (d.maSp ? theoMa.get(d.maSp)?.soLuong : '') },
          { title: 'Số lượng', width: 100, render: (_, d) => <InputNumber min={1} value={d.soLuong} onChange={(v) => sua(d.khoa, { soLuong: v ?? 1 })} style={{ width: '100%' }} aria-label="Số lượng" /> },
          {
            title: 'Giá nhập',
            width: 150,
            render: (_, d) => (
              <InputNumber
                min={0}
                step={1000}
                value={d.donGiaNhap}
                onChange={(v) => sua(d.khoa, { donGiaNhap: v })}
                formatter={(v) => (v ? so(Number(v)) : '')}
                parser={(v) => Number((v ?? '').replace(/\D/g, ''))}
                style={{ width: '100%' }}
                aria-label="Giá nhập"
                placeholder={d.maSp ? `Bán ${so(theoMa.get(d.maSp)?.donGia)}` : ''}
              />
            ),
          },
          { title: 'Thành tiền', width: 120, align: 'right', className: 'so', render: (_, d) => tien(d.soLuong * (d.donGiaNhap ?? 0)) },
          { title: '', width: 44, render: (_, d) => <Button type="text" danger icon={<DeleteOutlined />} onClick={() => setDongs((ds) => ds.filter((x) => x.khoa !== d.khoa))} aria-label="Xóa dòng" /> },
        ]}
      />
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: 12 }}>
        <Button icon={<PlusOutlined />} onClick={() => setDongs((ds) => [...ds, { khoa: dem++, soLuong: 1, donGiaNhap: null }])}>
          Thêm dòng
        </Button>
        <span>
          {hopLe.length} dòng hợp lệ · Tổng <b className="so" style={{ fontSize: 18 }}>{tien(tong)}</b>
        </span>
      </div>
      {trung && <Alert style={{ marginTop: 8 }} type="warning" showIcon message="Có sản phẩm xuất hiện nhiều lần, gộp lại thành 1 dòng trước khi lưu" />}
    </Modal>
  );
}
