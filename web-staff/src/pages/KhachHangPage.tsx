import { EditOutlined, FileExcelOutlined, SearchOutlined, UserAddOutlined } from '@ant-design/icons';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { App as AntApp, Button, Form, Input, Modal, Segmented, Select, Skeleton, Table, Tag, Tooltip } from 'antd';
import type { InputRef } from 'antd';
import { useEffect, useMemo, useRef, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { get, put, taiFile } from '../api/client';
import type { HoaDon, KhachHang, RuiRoKhach } from '../api/types';
import { ChuaChon, loiThanhChu, ThongTin, TieuDeTrang } from '../components/Chung';
import { MainSub } from '../components/MainSub';
import { boDau, ngay, so, tien } from '../lib/dinhDang';
import { HANG_TV, RUI_RO, The, TRANG_THAI_HD } from '../lib/nhan';
import { usePhimTat } from '../lib/phimTat';
import { ThemKhachModal } from './BanHangPage';

type Nhom = 'TAT_CA' | 'HOP' | 'GIAO';

export default function KhachHangPage() {
  const { message } = AntApp.useApp();
  const qc = useQueryClient();
  const [params, setParams] = useSearchParams();
  const dangChon = params.get('ma');
  const [tuKhoa, setTuKhoa] = useState('');
  const [tinh, setTinh] = useState('');
  const [nhom, setNhom] = useState<Nhom>('TAT_CA');
  const [moThem, setMoThem] = useState(false);
  const oTim = useRef<InputRef>(null);

  const ds = useQuery({
    queryKey: ['khach-hang', 'ds', nhom],
    queryFn: () =>
      get<KhachHang[]>(nhom === 'HOP' ? '/api/khach-hang/hai-tinh-hop' : nhom === 'GIAO' ? '/api/khach-hang/vua-tai-quay-vua-online' : '/api/khach-hang'),
  });
  const cacTinh = useMemo(() => [...new Set((ds.data ?? []).map((k) => k.tinhThanh).filter(Boolean))].sort() as string[], [ds.data]);
  const loc = useMemo(() => {
    const k = boDau(tuKhoa.trim());
    return (ds.data ?? []).filter((x) => (!tinh || x.tinhThanh === tinh) && (!k || boDau(`${x.maKh} ${x.hoTen} ${x.sdt ?? ''} ${x.email ?? ''}`).includes(k)));
  }, [ds.data, tuKhoa, tinh]);

  usePhimTat([
    { phim: '/', chay: () => oTim.current?.focus() },
    { phim: 'Alt+N', chay: () => setMoThem(true) },
  ]);

  return (
    <>
      <TieuDeTrang
        tieuDe="Khách hàng"
        moTa="Tìm khách theo SĐT, xem lịch sử mua và độ tin cậy trước khi xác nhận đơn."
        phai={
          <>
            <Button icon={<FileExcelOutlined />} onClick={() => taiFile('/api/excel/khach-hang', 'luu').catch((e) => message.error(loiThanhChu(e)))}>
              Xuất Excel
            </Button>
            <Button type="primary" icon={<UserAddOutlined />} onClick={() => setMoThem(true)}>
              Thêm khách <kbd className="kbd-toi">Alt N</kbd>
            </Button>
          </>
        }
      />
      <div style={{ marginBottom: 12 }}>
        <Segmented
          value={nhom}
          onChange={(v) => setNhom(v as Nhom)}
          options={[
            { value: 'TAT_CA', label: 'Tất cả' },
            { value: 'HOP', label: 'TP HCM ∪ Hà Nội (hợp)' },
            { value: 'GIAO', label: 'Mua tại quầy ∩ đặt online (giao)' },
          ]}
        />
      </div>
      <MainSub<KhachHang>
        duLieu={loc}
        khoa="maKh"
        dangChon={dangChon}
        chon={(m) => setParams(m ? { ma: m } : {}, { replace: true })}
        dangTai={ds.isLoading}
        rongCot={600}
        boLoc={
          <>
            <Input ref={oTim} allowClear prefix={<SearchOutlined aria-hidden />} placeholder="Tên, SĐT, email, mã KH" aria-label="Tìm khách" value={tuKhoa} onChange={(e) => setTuKhoa(e.target.value)} style={{ flex: '1 1 200px' }} />
            <Select value={tinh} onChange={setTinh} style={{ width: 170 }} aria-label="Tỉnh thành" options={[{ value: '', label: 'Mọi tỉnh thành' }, ...cacTinh.map((t) => ({ value: t, label: t }))]} />
          </>
        }
        cot={[
          { title: 'Mã KH', dataIndex: 'maKh', width: 86, render: (v) => <b>{v}</b> },
          { title: 'Họ tên', dataIndex: 'hoTen', ellipsis: true, render: (v, k) => <>{v}{!k.maTk && <div style={{ fontSize: 11, color: '#737373' }}>Khách tại quầy</div>}</> },
          { title: 'SĐT', dataIndex: 'sdt', width: 110 },
          { title: 'Tỉnh', dataIndex: 'tinhThanh', width: 120, ellipsis: true },
          { title: 'Hạng', dataIndex: 'hangThanhVien', width: 96, render: (v) => <The bang={HANG_TV} ma={v} /> },
        ]}
        chiTiet={dangChon ? <ChiTiet key={dangChon} ma={dangChon} /> : <ChuaChon />}
      />
      <ThemKhachModal
        mo={moThem}
        dong={() => setMoThem(false)}
        xong={(k) => {
          setMoThem(false);
          qc.invalidateQueries({ queryKey: ['khach-hang'] });
          setParams({ ma: k.maKh }, { replace: true });
        }}
      />
    </>
  );
}

function ChiTiet({ ma }: { ma: string }) {
  const kh = useQuery({ queryKey: ['khach-hang', 'ct', ma], queryFn: () => get<KhachHang>(`/api/khach-hang/${ma}`) });
  const k = kh.data;
  const ruiRo = useQuery({ queryKey: ['rui-ro', k?.sdt], queryFn: () => get<RuiRoKhach>(`/api/khach-hang/rui-ro/${k!.sdt}`), enabled: !!k?.sdt });
  const hd = useQuery({ queryKey: ['hoa-don', 'khach', ma], queryFn: () => get<HoaDon[]>('/api/hoa-don', { maKh: ma }) });
  const [moSua, setMoSua] = useState(false);
  if (!k) return <div style={{ padding: 16 }}><Skeleton active /></div>;
  const r = ruiRo.data;
  const daMua = (hd.data ?? []).filter((h) => h.trangThai === 'DA_THANH_TOAN');
  return (
    <>
      <div className="ct-dau">
        <div>
          <div className="ct-ma">
            {k.hoTen} <The bang={HANG_TV} ma={k.hangThanhVien} />
            {k.sdtDaXacThuc && <Tag color="green" bordered={false}>SĐT đã xác thực</Tag>}
          </div>
          <div className="ct-phu">
            {k.maKh} · {k.maTk ? 'Có tài khoản web' : 'Khách tại quầy (chưa có tài khoản)'} · {so(k.diemTichLuy)} điểm
          </div>
        </div>
        <Button icon={<EditOutlined />} onClick={() => setMoSua(true)}>
          Sửa thông tin
        </Button>
      </div>
      <div className="ct-phan">
        <ThongTin
          items={[
            { nhan: 'SĐT', gt: k.sdt },
            { nhan: 'Email', gt: k.email },
            { nhan: 'Ngày sinh', gt: ngay(k.ngaySinh) },
            { nhan: 'Tỉnh thành', gt: k.tinhThanh },
            { nhan: 'Địa chỉ', gt: k.diaChi, span: 2 },
          ]}
        />
      </div>
      {r && (
        <div className="ct-phan">
          <h3>Độ tin cậy đơn online</h3>
          <div style={{ display: 'flex', gap: 16, flexWrap: 'wrap', alignItems: 'center' }}>
            <The bang={RUI_RO} ma={r.mucRuiRo} />
            <span>
              Bom hàng <b className="so" style={{ color: r.soLanBomHang ? '#c8102e' : undefined }}>{r.soLanBomHang}</b>
            </span>
            <span>Hoàn tất <b className="so">{r.soDonHoanTat}</b></span>
            <span>Bị từ chối <b className="so">{r.soDonBiTuChoi}</b></span>
            {!r.duocDatCod && <Tag color="red" bordered={false}>Không được đặt COD</Tag>}
          </div>
        </div>
      )}
      <div className="ct-phan">
        <h3>
          Hóa đơn ({hd.data?.length ?? 0}) · đã mua {tien(daMua.reduce((s, h) => s + h.tongTien, 0))}
        </h3>
        <Table<HoaDon>
          size="small"
          rowKey="maHoaDon"
          loading={hd.isLoading}
          pagination={{ pageSize: 8, size: 'small' }}
          dataSource={hd.data}
          columns={[
            { title: 'Mã HĐ', dataIndex: 'maHoaDon', render: (v) => <Link to={`/hoa-don?ma=${v}`}>{v}</Link> },
            { title: 'Ngày', dataIndex: 'ngayLap', render: ngay },
            { title: 'Nguồn', render: (_, h) => (h.maDon ? `Online ${h.maDon}` : 'Tại quầy') },
            { title: 'Trạng thái', dataIndex: 'trangThai', render: (v) => <The bang={TRANG_THAI_HD} ma={v} /> },
            { title: 'Tổng', dataIndex: 'tongTien', align: 'right', className: 'so', render: tien },
          ]}
        />
      </div>
      <SuaKhach mo={moSua} k={k} dong={() => setMoSua(false)} />
    </>
  );
}

function SuaKhach({ mo, k, dong }: { mo: boolean; k: KhachHang; dong: () => void }) {
  const [form] = Form.useForm();
  const { message } = AntApp.useApp();
  const qc = useQueryClient();
  const [dangGui, setDangGui] = useState(false);
  useEffect(() => {
    if (mo) form.setFieldsValue(k);
  }, [mo, k, form]);
  const gui = async () => {
    const v = await form.validateFields();
    setDangGui(true);
    try {
      await put(`/api/khach-hang/${k.maKh}`, { ...k, ...v, email: v.email || null });
      message.success('Đã lưu thông tin khách');
      qc.invalidateQueries({ queryKey: ['khach-hang'] });
      dong();
    } catch (e) {
      message.error(loiThanhChu(e));
    } finally {
      setDangGui(false);
    }
  };
  return (
    <Modal open={mo} title={`Sửa khách ${k.maKh}`} onCancel={dong} onOk={gui} okText="Lưu" cancelText="Hủy" confirmLoading={dangGui} destroyOnClose>
      <Form form={form} layout="vertical" onFinish={gui}>
        <Form.Item name="hoTen" label="Họ tên" rules={[{ required: true, message: 'Nhập họ tên' }]}>
          <Input autoFocus />
        </Form.Item>
        <Tooltip title={k.maTk ? 'Khách có tài khoản tự đổi SĐT trên web (cần xác thực OTP)' : undefined}>
          <Form.Item name="sdt" label="Số điện thoại" rules={[{ pattern: /^0\d{9}$/, message: '10 số, bắt đầu bằng 0' }]}>
            <Input disabled={!!k.maTk} inputMode="numeric" maxLength={10} />
          </Form.Item>
        </Tooltip>
        <Form.Item name="email" label="Email" rules={[{ type: 'email', message: 'Email chưa đúng' }]}>
          <Input />
        </Form.Item>
        <Form.Item name="tinhThanh" label="Tỉnh / thành">
          <Input />
        </Form.Item>
        <Form.Item name="diaChi" label="Địa chỉ">
          <Input />
        </Form.Item>
        <button type="submit" hidden />
      </Form>
    </Modal>
  );
}
