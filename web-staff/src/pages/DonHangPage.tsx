import { CarOutlined, CheckOutlined, CloseOutlined, PrinterOutlined, ReloadOutlined, SearchOutlined, StopOutlined, WarningOutlined } from '@ant-design/icons';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Alert, App as AntApp, Button, DatePicker, Input, Segmented, Select, Skeleton, Space, Steps, Table, Tag, Tooltip } from 'antd';
import type { InputRef } from 'antd';
import dayjs, { type Dayjs } from 'dayjs';
import { useMemo, useRef, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { get, post, taiFile } from '../api/client';
import type { DonDatHang, HoaDon, RuiRoKhach } from '../api/types';
import { ChuaChon, loiThanhChu, ThongTin, TieuDeTrang } from '../components/Chung';
import { useHoiLyDo } from '../components/HoiLyDo';
import { MainSub } from '../components/MainSub';
import { bienThe, boDau, ngayGio, ngayIso, tien } from '../lib/dinhDang';
import { PHUONG_THUC, RUI_RO, The, TRANG_THAI_DON, TRANG_THAI_HT, YEU_CAU_HUY } from '../lib/nhan';
import { usePhimTat } from '../lib/phimTat';

type Loc = 'CHO_XAC_NHAN' | 'XIN_HUY' | 'DA_XAC_NHAN' | 'DANG_GIAO' | 'HOAN_TAT' | 'DA_HUY' | 'TU_CHOI' | 'TAT_CA';

const CAC_LOC: { value: Loc; label: string }[] = [
  { value: 'CHO_XAC_NHAN', label: 'Chờ xác nhận' },
  { value: 'XIN_HUY', label: 'Khách xin hủy' },
  { value: 'DA_XAC_NHAN', label: 'Đã xác nhận' },
  { value: 'DANG_GIAO', label: 'Đang giao' },
  { value: 'HOAN_TAT', label: 'Hoàn tất' },
  { value: 'DA_HUY', label: 'Đã hủy' },
  { value: 'TU_CHOI', label: 'Từ chối' },
  { value: 'TAT_CA', label: 'Tất cả' },
];

export default function DonHangPage() {
  const [params, setParams] = useSearchParams();
  const loc = (params.get('loc') as Loc) || 'CHO_XAC_NHAN';
  const dangChon = params.get('ma');
  const [nguon, setNguon] = useState<string>('');
  const [khoang, setKhoang] = useState<[Dayjs, Dayjs] | null>(null);
  const [tuKhoa, setTuKhoa] = useState('');
  const oTim = useRef<InputRef>(null);

  const datParam = (k: string, v: string | null) =>
    setParams(
      (p) => {
        const n = new URLSearchParams(p);
        if (v) n.set(k, v);
        else n.delete(k);
        return n;
      },
      { replace: true },
    );

  const ds = useQuery({
    queryKey: ['don-hang', 'ds', loc, nguon, khoang?.[0]?.valueOf(), khoang?.[1]?.valueOf()],
    queryFn: () =>
      get<DonDatHang[]>('/api/don-hang', {
        trangThai: loc === 'XIN_HUY' || loc === 'TAT_CA' ? undefined : loc,
        yeuCauHuy: loc === 'XIN_HUY' ? 'CHO_DUYET' : undefined,
        nguon: nguon || undefined,
        tuNgay: khoang ? ngayIso(khoang[0]) : undefined,
        denNgay: khoang ? ngayIso(khoang[1]) : undefined,
      }),
    refetchInterval: 30_000, // danh sách đơn chờ tự làm mới
  });

  const loc2 = useMemo(() => {
    const k = boDau(tuKhoa.trim());
    const d = ds.data ?? [];
    if (!k) return d;
    return d.filter((x) => boDau(`${x.maDon} ${x.sdtNhan} ${x.tenNguoiNhan ?? ''} ${x.tenKhachHang ?? ''}`).includes(k));
  }, [ds.data, tuKhoa]);

  usePhimTat([
    { phim: '/', chay: () => oTim.current?.focus() },
    { phim: 'Alt+L', chay: () => ds.refetch() },
  ]);

  return (
    <>
      <TieuDeTrang
        tieuDe="Đơn online"
        moTa="Xác nhận đơn, theo dõi giao hàng và duyệt yêu cầu hủy. Danh sách tự làm mới mỗi 30 giây."
        phai={
          <Tooltip title="Làm mới (Alt+L)">
            <Button icon={<ReloadOutlined />} onClick={() => ds.refetch()} loading={ds.isFetching}>
              Làm mới
            </Button>
          </Tooltip>
        }
      />
      <div style={{ marginBottom: 12, overflowX: 'auto' }}>
        <Segmented value={loc} onChange={(v) => setParams({ loc: String(v) }, { replace: true })} options={CAC_LOC} />
      </div>
      <MainSub<DonDatHang>
        duLieu={loc2}
        khoa="maDon"
        dangChon={dangChon}
        chon={(m) => datParam('ma', m)}
        dangTai={ds.isLoading}
        boLoc={
          <>
            <Input
              ref={oTim}
              allowClear
              prefix={<SearchOutlined aria-hidden />}
              placeholder="Mã đơn, SĐT, tên"
              aria-label="Lọc đơn"
              value={tuKhoa}
              onChange={(e) => setTuKhoa(e.target.value)}
              style={{ flex: '1 1 160px' }}
            />
            <Select
              value={nguon}
              onChange={setNguon}
              style={{ width: 150 }}
              aria-label="Nguồn đơn"
              options={[
                { value: '', label: 'Mọi nguồn' },
                { value: 'TAI_KHOAN', label: 'Khách có tài khoản' },
                { value: 'VANG_LAI', label: 'Khách vãng lai' },
              ]}
            />
            <DatePicker.RangePicker value={khoang} onChange={(v) => setKhoang(v as [Dayjs, Dayjs] | null)} format="DD/MM/YYYY" style={{ flex: '1 1 220px' }} aria-label="Khoảng ngày đặt" />
          </>
        }
        cot={[
          {
            title: 'Mã đơn',
            dataIndex: 'maDon',
            width: 96,
            render: (v, d) => (
              <span>
                <b>{v}</b>
                {d.nguon === 'VANG_LAI' && <div style={{ fontSize: 11, color: '#737373' }}>Vãng lai</div>}
              </span>
            ),
          },
          { title: 'Người nhận', ellipsis: true, render: (_, d) => <span>{d.tenNguoiNhan ?? d.tenKhachHang}<div style={{ fontSize: 12, color: '#737373' }}>{d.sdtNhan}</div></span> },
          { title: 'Đặt lúc', dataIndex: 'ngayDat', width: 96, render: (v) => { const [d, g] = ngayGio(v).split(' '); return <span style={{ fontSize: 12, lineHeight: 1.35, display: 'block' }}>{d}<br /><span style={{ color: '#6b6b6b' }}>{g}</span></span>; } },
          {
            title: 'Trạng thái',
            width: 118,
            render: (_, d) => (
              <Space direction="vertical" size={2}>
                <The bang={TRANG_THAI_DON} ma={d.trangThai} />
                {d.yeuCauHuy === 'CHO_DUYET' && <The bang={YEU_CAU_HUY} ma="CHO_DUYET" />}
              </Space>
            ),
          },
          { title: 'Tổng', dataIndex: 'tongTien', width: 100, align: 'right', className: 'so', render: tien },
        ]}
        chiTiet={dangChon ? <ChiTietDon key={dangChon} ma={dangChon} /> : <ChuaChon chu={loc2.length ? undefined : 'Không có đơn nào ở mục này'} />}
      />
    </>
  );
}

function ChiTietDon({ ma }: { ma: string }) {
  const { message, modal } = AntApp.useApp();
  const qc = useQueryClient();
  const [hoiLyDo, hopLyDo] = useHoiLyDo();
  const [dangLam, setDangLam] = useState<string | null>(null);
  const don = useQuery({ queryKey: ['don-hang', 'ct', ma], queryFn: () => get<DonDatHang>(`/api/don-hang/${ma}`) });
  const d = don.data;
  const ruiRo = useQuery({
    queryKey: ['rui-ro', d?.sdtNhan, d?.maKh],
    queryFn: () => get<RuiRoKhach>(`/api/khach-hang/rui-ro/${d!.sdtNhan}`, { maKh: d!.maKh }),
    enabled: !!d?.sdtNhan,
  });

  const lam = async (ten: string, goi: () => Promise<unknown>, thongBao: string) => {
    setDangLam(ten);
    try {
      await goi();
      message.success(thongBao);
      await Promise.all([qc.invalidateQueries({ queryKey: ['don-hang'] }), qc.invalidateQueries({ queryKey: ['hoan-tien'] }), qc.invalidateQueries({ queryKey: ['rui-ro'] }), qc.invalidateQueries({ queryKey: ['hoa-don'] })]);
    } catch (e) {
      message.error(loiThanhChu(e));
      don.refetch();
    } finally {
      setDangLam(null);
    }
  };

  const xacNhan = () =>
    lam('xn', async () => {
      const hd = await post<HoaDon>(`/api/don-hang/${ma}/xac-nhan`);
      message.info(`Đã trừ kho và lập hóa đơn ${hd.maHoaDon}`);
    }, `Đã xác nhận đơn ${ma}`);
  const tuChoi = () =>
    modal.confirm({
      title: `Từ chối đơn ${ma}?`,
      content: 'Đơn chuyển sang Từ chối, không trừ kho. Lần từ chối này được tính vào lịch sử của số điện thoại.',
      okText: 'Từ chối đơn',
      okButtonProps: { danger: true },
      cancelText: 'Quay lại',
      onOk: () => lam('tc', () => post(`/api/don-hang/${ma}/tu-choi`), `Đã từ chối đơn ${ma}`),
    });
  const giaoHang = () => lam('gh', () => post(`/api/don-hang/${ma}/giao-hang`), `Đơn ${ma} đã chuyển sang đang giao`);
  const hoanTat = () => lam('ht', () => post(`/api/don-hang/${ma}/hoan-tat`), `Đơn ${ma} đã hoàn tất`);
  const duyetHuy = () =>
    modal.confirm({
      title: `Đồng ý hủy đơn ${ma}?`,
      content: (
        <div>
          Hàng được trả về kho và hóa đơn bị hủy.
          {d?.phuongThucTt !== 'COD' && ' Khách đã trả trước nên hệ thống tạo phiếu chờ hoàn tiền.'} Lần hủy này <b>không</b> tính là bom hàng.
        </div>
      ),
      okText: 'Duyệt hủy',
      cancelText: 'Quay lại',
      onOk: () => lam('dh', () => post(`/api/don-hang/${ma}/duyet-huy`), `Đã duyệt yêu cầu hủy ${ma}`),
    });
  const tuChoiHuy = async () => {
    const l = await hoiLyDo({
      tieuDe: `Từ chối yêu cầu hủy ${ma}`,
      moTa: 'Khách sẽ thấy lý do này ở trang đơn hàng. Đơn tiếp tục được giao.',
      batBuoc: true,
      nutOk: 'Từ chối yêu cầu',
      goiY: ['Hàng đã đóng gói, đang bàn giao cho đơn vị vận chuyển', 'Đơn đã được giao cho đơn vị vận chuyển'],
    });
    if (l) lam('tch', () => post(`/api/don-hang/${ma}/tu-choi-huy`, { lyDo: l }), 'Đã từ chối yêu cầu hủy');
  };
  const huyDon = async (bomHang: boolean) => {
    const l = await hoiLyDo({
      tieuDe: bomHang ? `Khách không nhận hàng (${ma})` : `Hủy đơn ${ma}`,
      moTa: (
        <Alert
          type="warning"
          showIcon
          message={bomHang ? 'Đơn bị hủy, hàng trả về kho và tính 1 lần bom hàng cho số điện thoại này.' : 'Đơn bị hủy, hàng trả về kho. Cửa hàng chủ động hủy sẽ được tính là 1 lần bom hàng của số điện thoại.'}
        />
      ),
      batBuoc: bomHang,
      nguyHiem: true,
      nutOk: 'Hủy đơn',
      goiY: bomHang ? ['Khách từ chối nhận hàng', 'Không liên lạc được khách', 'Sai địa chỉ, khách không xác nhận'] : ['Khách gọi hotline xin hủy', 'Hết hàng'],
    });
    if (l !== null) lam('huy', () => post(`/api/don-hang/${ma}/huy`, { lyDo: l || null }), `Đã hủy đơn ${ma}`);
  };
  const inPdf = () => taiFile(`/api/don-hang/${ma}/pdf`).catch((e) => message.error(loiThanhChu(e)));

  const xinHuy = d?.yeuCauHuy === 'CHO_DUYET';
  usePhimTat(
    [
      { phim: 'Alt+X', chay: () => (d?.trangThai === 'CHO_XAC_NHAN' ? xacNhan() : xinHuy ? duyetHuy() : undefined) },
      { phim: 'Alt+G', chay: () => d?.trangThai === 'DA_XAC_NHAN' && !xinHuy && giaoHang() },
      { phim: 'Alt+H', chay: () => d?.trangThai === 'DANG_GIAO' && hoanTat() },
      { phim: 'Alt+P', chay: inPdf },
    ],
    !!d && !dangLam,
  );

  if (don.isLoading) return <div style={{ padding: 16 }}><Skeleton active /></div>;
  if (don.isError || !d) return <Alert type="error" message={loiThanhChu(don.error)} style={{ margin: 16 }} />;

  const buoc = ['CHO_XAC_NHAN', 'DA_XAC_NHAN', 'DANG_GIAO', 'HOAN_TAT'];
  const ketThucXau = d.trangThai === 'DA_HUY' || d.trangThai === 'TU_CHOI';
  const r = ruiRo.data;

  return (
    <>
      <div className="ct-dau">
        <div>
          <div className="ct-ma">
            {d.maDon} <The bang={TRANG_THAI_DON} ma={d.trangThai} />
            {xinHuy && <The bang={YEU_CAU_HUY} ma="CHO_DUYET" />}
            {d.nguon === 'VANG_LAI' && <Tag bordered={false}>Khách vãng lai</Tag>}
          </div>
          <div className="ct-phu">
            Đặt lúc {ngayGio(d.ngayDat)}
            {d.tenNhanVien && ` · Xử lý: ${d.tenNhanVien}`}
            {d.maHoaDon && ` · Hóa đơn ${d.maHoaDon}`}
          </div>
        </div>
        <div className="ct-hanh-dong">
          {d.trangThai === 'CHO_XAC_NHAN' && (
            <>
              <Button danger icon={<CloseOutlined />} onClick={tuChoi} loading={dangLam === 'tc'}>
                Từ chối
              </Button>
              <Button type="primary" icon={<CheckOutlined />} onClick={xacNhan} loading={dangLam === 'xn'}>
                Xác nhận <kbd className="kbd-toi">Alt X</kbd>
              </Button>
            </>
          )}
          {xinHuy && (
            <>
              <Button onClick={tuChoiHuy} loading={dangLam === 'tch'}>
                Từ chối hủy
              </Button>
              <Button type="primary" danger icon={<StopOutlined />} onClick={duyetHuy} loading={dangLam === 'dh'}>
                Duyệt hủy <kbd className="kbd-toi">Alt X</kbd>
              </Button>
            </>
          )}
          {d.trangThai === 'DA_XAC_NHAN' && !xinHuy && (
            <>
              <Button danger onClick={() => huyDon(false)} loading={dangLam === 'huy'}>
                Hủy đơn
              </Button>
              <Button type="primary" icon={<CarOutlined />} onClick={giaoHang} loading={dangLam === 'gh'}>
                Giao hàng <kbd className="kbd-toi">Alt G</kbd>
              </Button>
            </>
          )}
          {d.trangThai === 'DANG_GIAO' && (
            <>
              <Button danger icon={<WarningOutlined />} onClick={() => huyDon(true)} loading={dangLam === 'huy'}>
                Khách không nhận
              </Button>
              <Button type="primary" icon={<CheckOutlined />} onClick={hoanTat} loading={dangLam === 'ht'}>
                Hoàn tất <kbd className="kbd-toi">Alt H</kbd>
              </Button>
            </>
          )}
          <Tooltip title="In PDF (Alt+P)">
            <Button icon={<PrinterOutlined />} onClick={inPdf} aria-label="In PDF đơn hàng" />
          </Tooltip>
        </div>
      </div>

      {xinHuy && (
        <div className="ct-phan">
          <div className="canh-bao">
            <b>Khách xin hủy đơn</b> lúc {ngayGio(d.ngayYeuCauHuy)}: “{d.lyDoHuy}”. Đơn chưa giao được tới khi duyệt hoặc từ chối yêu cầu.
          </div>
        </div>
      )}

      <div className="ct-phan">
        {ketThucXau ? (
          <Alert
            type={d.trangThai === 'DA_HUY' ? 'warning' : 'error'}
            showIcon
            message={d.trangThai === 'DA_HUY' ? `Đơn đã hủy${d.nguoiHuy ? (d.nguoiHuy === 'KHACH' ? ' theo yêu cầu của khách' : ' bởi cửa hàng') : ''}${d.ngayHuy ? ` lúc ${ngayGio(d.ngayHuy)}` : ''}` : 'Cửa hàng đã từ chối đơn này'}
            description={
              <>
                {d.lyDoHuy && <div>Lý do: {d.lyDoHuy}</div>}
                {d.trangThaiHoanTien && (
                  <div>
                    Hoàn tiền {tien(d.soTienHoan)}: <The bang={TRANG_THAI_HT} ma={d.trangThaiHoanTien} />
                  </div>
                )}
              </>
            }
          />
        ) : (
          <Steps size="small" current={buoc.indexOf(d.trangThai)} items={[{ title: 'Chờ xác nhận' }, { title: 'Đã xác nhận' }, { title: 'Đang giao' }, { title: 'Hoàn tất' }]} />
        )}
        {d.yeuCauHuy === 'TU_CHOI' && (
          <Alert style={{ marginTop: 10 }} type="info" showIcon message={`Đã từ chối yêu cầu hủy của khách: ${d.phanHoiHuy ?? ''}`} description={d.lyDoHuy ? `Lý do khách đưa: ${d.lyDoHuy}` : undefined} />
        )}
      </div>

      <div className="ct-phan">
        <h3>Giao hàng</h3>
        <ThongTin
          items={[
            { nhan: 'Người nhận', gt: d.tenNguoiNhan ?? d.tenKhachHang },
            { nhan: 'SĐT', gt: <a href={`tel:${d.sdtNhan}`}>{d.sdtNhan}</a> },
            { nhan: 'Địa chỉ', gt: d.diaChiGiao, span: 2 },
            { nhan: 'Thanh toán', gt: PHUONG_THUC[d.phuongThucTt] ?? d.phuongThucTt },
            { nhan: 'Khách hàng', gt: d.nguon === 'VANG_LAI' ? 'Vãng lai (đã xác thực OTP)' : `${d.tenKhachHang ?? ''} · ${d.maKh}` },
            ...(d.emailNhan ? [{ nhan: 'Email', gt: d.emailNhan }] : []),
          ]}
        />
      </div>

      <div className="ct-phan">
        <h3>Độ tin cậy của số điện thoại</h3>
        {ruiRo.isLoading ? (
          <Skeleton active paragraph={{ rows: 1 }} />
        ) : r ? (
          <div style={{ display: 'flex', gap: 16, flexWrap: 'wrap', alignItems: 'center' }}>
            <The bang={RUI_RO} ma={r.mucRuiRo} />
            <span>
              Bom hàng <b className="so" style={{ color: r.soLanBomHang > 0 ? '#c8102e' : undefined }}>{r.soLanBomHang}</b> lần
            </span>
            <span>Hoàn tất <b className="so">{r.soDonHoanTat}</b></span>
            <span>Bị từ chối <b className="so">{r.soDonBiTuChoi}</b></span>
            <span>Đang chờ <b className="so">{r.soDonDangCho}</b></span>
            {!r.duocDatCod && <Tag color="red" bordered={false}>Không được đặt COD</Tag>}
            {r.ghiChu && <div style={{ width: '100%', color: '#525252', fontSize: 13 }}>{r.ghiChu}</div>}
          </div>
        ) : (
          <span style={{ color: '#737373' }}>Không lấy được thông tin</span>
        )}
      </div>

      <div className="ct-phan">
        <h3>Sản phẩm ({d.chiTiets.length})</h3>
        <Table
          size="small"
          rowKey="maSp"
          pagination={false}
          dataSource={d.chiTiets}
          columns={[
            { title: 'Mã SP', dataIndex: 'maSp', width: 100 },
            { title: 'Sản phẩm', render: (_, c) => <>{c.tenSp} <span style={{ color: '#737373' }}>{bienThe(c.kichCo, c.mauSac)}</span></> },
            { title: 'SL', dataIndex: 'soLuong', width: 50, align: 'right', className: 'so' },
            { title: 'Đơn giá', dataIndex: 'donGia', width: 110, align: 'right', className: 'so', render: tien },
            { title: 'Thành tiền', dataIndex: 'thanhTien', width: 120, align: 'right', className: 'so', render: tien },
          ]}
        />
        <div className="tong-cong" style={{ marginTop: 12 }}>
          <span>Tạm tính</span>
          <span style={{ textAlign: 'right' }}>{tien(d.tongTien + d.tienGiam)}</span>
          {d.tienGiam > 0 && (<><span>Giảm{d.maVoucher ? ` (voucher ${d.maVoucher})` : ''}</span><span style={{ textAlign: 'right' }}>−{tien(d.tienGiam)}</span></>)}
          <span className="lon">Tổng</span>
          <span className="lon" style={{ textAlign: 'right' }}>{tien(d.tongTien)}</span>
        </div>
        {d.trangThai === 'CHO_XAC_NHAN' && dayjs().diff(dayjs(d.ngayDat), 'hour') >= 24 && (
          <Alert style={{ marginTop: 12 }} type="warning" showIcon message="Đơn đã chờ quá 24 giờ" />
        )}
      </div>
      {hopLyDo}
    </>
  );
}
