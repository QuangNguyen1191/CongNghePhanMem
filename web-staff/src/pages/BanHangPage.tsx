import { DeleteOutlined, MinusOutlined, PlusOutlined, PrinterOutlined, SearchOutlined, UserAddOutlined, UserOutlined } from '@ant-design/icons';
import { keepPreviousData, useQuery, useQueryClient } from '@tanstack/react-query';
import { Alert, App as AntApp, Button, Empty, Form, Input, InputNumber, Modal, Segmented, Select, Space, Table, Tag, Tooltip } from 'antd';
import type { InputRef } from 'antd';
import { useEffect, useMemo, useRef, useState } from 'react';
import { get, LoiApi, post, taiFile } from '../api/client';
import type { BangTinhTien, HoaDon, KhachHang, SanPham } from '../api/types';
import { loiThanhChu, TieuDeTrang } from '../components/Chung';
import { bienThe, so, tien } from '../lib/dinhDang';
import { HANG_TV, The } from '../lib/nhan';
import { usePhimTat } from '../lib/phimTat';
import { useCheDoTim, useTimSanPham } from '../lib/timSanPham';

interface DongGio {
  sp: SanPham;
  soLuong: number;
}

const KHOA_GIO = 'web-staff.gio-quay';

function docGio(): DongGio[] {
  try {
    return JSON.parse(sessionStorage.getItem(KHOA_GIO) ?? '[]');
  } catch {
    return [];
  }
}

const TINH_THANH = ['TP Hồ Chí Minh', 'Hà Nội', 'Đà Nẵng', 'Cần Thơ', 'Hải Phòng', 'Bình Dương', 'Đồng Nai', 'Khánh Hòa'];

export default function BanHangPage() {
  const { message, modal } = AntApp.useApp();
  const qc = useQueryClient();
  const [cheDo, setCheDo] = useCheDoTim();
  const [tuKhoa, setTuKhoa] = useState('');
  const [gio, setGio] = useState<DongGio[]>(docGio);
  const [dongChon, setDongChon] = useState(0);
  const [sdt, setSdt] = useState('');
  const [khach, setKhach] = useState<KhachHang | null>(null);
  const [khongThayKhach, setKhongThayKhach] = useState(false);
  const [moThemKhach, setMoThemKhach] = useState(false);
  const [voucher, setVoucher] = useState('');
  const [voucherApDung, setVoucherApDung] = useState<string | null>(null);
  const [phuongThuc, setPhuongThuc] = useState<'TIEN_MAT' | 'CHUYEN_KHOAN'>('TIEN_MAT');
  const [khachDua, setKhachDua] = useState<number | null>(null);
  const [dangThanhToan, setDangThanhToan] = useState(false);
  const [hoaDonVua, setHoaDonVua] = useState<HoaDon | null>(null);

  const oTim = useRef<InputRef>(null);
  const oSdt = useRef<InputRef>(null);
  const oVoucher = useRef<InputRef>(null);

  const { ketQua, dangTai, loi, thongKe } = useTimSanPham(tuKhoa, cheDo, { chiDangBan: true });
  const giaKm = useQuery({ queryKey: ['san-pham', 'gia-km'], queryFn: () => get<Record<string, number>>('/api/san-pham/gia-khuyen-mai'), staleTime: 5 * 60_000 });

  useEffect(() => {
    try {
      sessionStorage.setItem(KHOA_GIO, JSON.stringify(gio));
    } catch {
      /* bỏ qua */
    }
  }, [gio]);
  useEffect(() => oTim.current?.focus(), []);

  const dongHangs = useMemo(() => gio.map((d) => ({ maSp: d.sp.maSp, soLuong: d.soLuong })), [gio]);
  const tinh = useQuery({
    queryKey: ['tinh-truoc', dongHangs, voucherApDung],
    queryFn: () => post<BangTinhTien>('/api/hoa-don/tinh-truoc', { dongHangs, maVoucher: voucherApDung, phuongThucTt: phuongThuc }),
    enabled: dongHangs.length > 0,
    placeholderData: keepPreviousData,
    retry: false,
    staleTime: 0,
  });
  const bang = dongHangs.length > 0 ? tinh.data : undefined;
  const tongTien = bang?.tongTien ?? 0;

  // ---- giỏ
  const themVaoGio = (sp: SanPham, sl = 1) => {
    if (sp.soLuong <= 0) {
      message.warning(`${sp.maSp} đã hết hàng`);
      return;
    }
    setGio((g) => {
      const i = g.findIndex((d) => d.sp.maSp === sp.maSp);
      if (i >= 0) {
        const moi = [...g];
        const slMoi = Math.min(sp.soLuong, moi[i].soLuong + sl);
        if (slMoi === moi[i].soLuong) message.warning(`Chỉ còn ${sp.soLuong} sản phẩm ${sp.maSp}`);
        moi[i] = { ...moi[i], soLuong: slMoi };
        setDongChon(i);
        return moi;
      }
      setDongChon(g.length);
      return [...g, { sp, soLuong: sl }];
    });
  };
  const doiSl = (i: number, sl: number) => setGio((g) => g.map((d, k) => (k === i ? { ...d, soLuong: Math.max(1, Math.min(d.sp.soLuong, sl)) } : d)));
  const xoaDong = (i: number) => {
    setGio((g) => g.filter((_, k) => k !== i));
    setDongChon((c) => Math.max(0, Math.min(c, gio.length - 2)));
  };
  const lamMoi = () => {
    setGio([]);
    setKhach(null);
    setSdt('');
    setKhongThayKhach(false);
    setVoucher('');
    setVoucherApDung(null);
    setKhachDua(null);
    setPhuongThuc('TIEN_MAT');
    setTuKhoa('');
    setTimeout(() => oTim.current?.focus(), 0);
  };

  const enterTim = () => {
    const k = tuKhoa.trim().toUpperCase();
    if (!k) return;
    const dung = ketQua.find((s) => s.maSp.toUpperCase() === k) ?? (ketQua.length === 1 ? ketQua[0] : undefined) ?? ketQua[0];
    if (!dung) {
      message.info('Không tìm thấy sản phẩm phù hợp');
      return;
    }
    themVaoGio(dung);
    setTuKhoa('');
  };

  // ---- khách
  const timKhach = async () => {
    const s = sdt.trim();
    if (!/^0\d{9}$/.test(s)) {
      message.warning('Số điện thoại gồm 10 số, bắt đầu bằng 0');
      return;
    }
    try {
      const k = await get<KhachHang>(`/api/khach-hang/theo-sdt/${s}`);
      setKhach(k);
      setKhongThayKhach(false);
      oTim.current?.focus();
    } catch (e) {
      if (e instanceof LoiApi && e.status === 404) {
        setKhach(null);
        setKhongThayKhach(true);
      } else message.error(loiThanhChu(e));
    }
  };

  // ---- thanh toán
  const thanhToan = async () => {
    if (gio.length === 0) {
      message.warning('Giỏ đang trống');
      oTim.current?.focus();
      return;
    }
    if (tinh.isError) {
      message.error(loiThanhChu(tinh.error));
      return;
    }
    if (phuongThuc === 'TIEN_MAT' && khachDua != null && khachDua < tongTien) {
      message.warning('Tiền khách đưa chưa đủ');
      return;
    }
    const ok = await modal.confirm({
      title: `Thu ${tien(tongTien)}?`,
      content: (
        <div>
          {gio.length} dòng hàng · {PHUONG_THUC_NHAN[phuongThuc]} · {khach ? `${khach.hoTen} (${khach.sdt})` : 'Khách lẻ'}
          {phuongThuc === 'TIEN_MAT' && khachDua != null && <div>Tiền thừa trả khách: <b>{tien(khachDua - tongTien)}</b></div>}
        </div>
      ),
      okText: 'Lập hóa đơn',
      cancelText: 'Quay lại',
      autoFocusButton: 'ok',
    });
    if (!ok) return;
    setDangThanhToan(true);
    try {
      const hd = await post<HoaDon>('/api/hoa-don/ban-tai-quay', { maKh: khach?.maKh ?? null, phuongThucTt: phuongThuc, maVoucher: voucherApDung, dongHangs });
      setHoaDonVua(hd);
      qc.invalidateQueries({ queryKey: ['san-pham'] });
      qc.invalidateQueries({ queryKey: ['hoa-don'] });
      lamMoi();
    } catch (e) {
      message.error(loiThanhChu(e));
    } finally {
      setDangThanhToan(false);
    }
  };

  usePhimTat(
    [
      { phim: 'F2', chay: () => oTim.current?.focus() },
      { phim: '/', chay: () => oTim.current?.focus() },
      { phim: 'F3', chay: () => setCheDo(cheDo === 'TAI_SU_DUNG' ? 'TRUY_VAN_MOI' : 'TAI_SU_DUNG') },
      { phim: 'F4', chay: () => oSdt.current?.focus() },
      { phim: 'F6', chay: () => oVoucher.current?.focus() },
      { phim: 'F7', chay: () => setPhuongThuc((p) => (p === 'TIEN_MAT' ? 'CHUYEN_KHOAN' : 'TIEN_MAT')) },
      { phim: 'F9', chay: () => !dangThanhToan && thanhToan() },
      { phim: 'Alt+Delete', chay: () => gio.length && modal.confirm({ title: 'Xóa hết giỏ hàng?', okText: 'Xóa hết', okButtonProps: { danger: true }, cancelText: 'Không', onOk: lamMoi }) },
      { phim: '+', chay: () => gio[dongChon] && doiSl(dongChon, gio[dongChon].soLuong + 1) },
      { phim: '-', chay: () => gio[dongChon] && doiSl(dongChon, gio[dongChon].soLuong - 1) },
      { phim: 'Delete', chay: () => gio[dongChon] && xoaDong(dongChon), trongONhap: false },
    ],
    !hoaDonVua && !moThemKhach,
  );

  const goiYTien = useMemo(() => {
    if (!tongTien) return [];
    const tron = [10_000, 50_000, 100_000, 500_000].map((m) => Math.ceil(tongTien / m) * m);
    return [...new Set([tongTien, ...tron])].slice(0, 4);
  }, [tongTien]);

  return (
    <>
      <TieuDeTrang
        tieuDe="Bán tại quầy"
        moTa={
          <>
            <kbd>F2</kbd> tìm SP · <kbd>F4</kbd> khách · <kbd>F6</kbd> voucher · <kbd>F7</kbd> đổi cách trả · <kbd>F9</kbd> thanh toán
          </>
        }
        phai={
          <Button onClick={lamMoi} disabled={gio.length === 0 && !khach}>
            Đơn mới
          </Button>
        }
      />
      <div className="pos">
        <div className="pos-trai">
          <section className="khung">
            <div className="khung-dau" style={{ flexDirection: 'column', alignItems: 'stretch' }}>
              <Input
                ref={oTim}
                size="large"
                allowClear
                prefix={<SearchOutlined aria-hidden />}
                placeholder="Quét / gõ mã SP hoặc tên, Enter để thêm vào giỏ"
                aria-label="Tìm sản phẩm"
                value={tuKhoa}
                onChange={(e) => setTuKhoa(e.target.value)}
                onPressEnter={enterTim}
                suffix={<kbd>F2</kbd>}
              />
              <div className="che-do-tim">
                <Segmented
                  size="small"
                  value={cheDo}
                  onChange={(v) => setCheDo(v as typeof cheDo)}
                  options={[
                    { value: 'TAI_SU_DUNG', label: 'Tái sử dụng danh sách' },
                    { value: 'TRUY_VAN_MOI', label: 'Truy vấn mới mỗi lần gõ' },
                  ]}
                />
                <Tooltip title="Yêu cầu 2: so sánh tái sử dụng danh sách và chạy câu truy vấn mới. F3 để đổi.">
                  <span>
                    <b className="so">{thongKe.soCauSql}</b> câu SQL · {thongKe.thoiGianMs} ms · {thongKe.nguon}
                  </span>
                </Tooltip>
              </div>
            </div>
            {loi && <Alert type="error" message={loi.message} banner />}
            <Table<SanPham>
              size="small"
              rowKey="maSp"
              loading={dangTai}
              dataSource={ketQua}
              pagination={{ pageSize: 12, size: 'small', showSizeChanger: false }}
              locale={{ emptyText: <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="Không có sản phẩm phù hợp" /> }}
              onRow={(s) => ({ onClick: () => themVaoGio(s), style: { cursor: s.soLuong > 0 ? 'pointer' : 'not-allowed', opacity: s.soLuong > 0 ? 1 : 0.5 } })}
              columns={[
                { title: 'Mã SP', dataIndex: 'maSp', width: 110, render: (v) => <b>{v}</b> },
                { title: 'Sản phẩm', dataIndex: 'tenSp', ellipsis: true },
                { title: 'Màu / Size', width: 140, render: (_, s) => bienThe(s.kichCo, s.mauSac) },
                {
                  title: 'Giá',
                  width: 130,
                  align: 'right',
                  className: 'so',
                  render: (_, s) => {
                    const km = giaKm.data?.[s.maSp];
                    return km != null && km < s.donGia ? (
                      <span>
                        <s style={{ color: '#a3a3a3', fontSize: 12 }}>{tien(s.donGia)}</s> <b style={{ color: '#c8102e' }}>{tien(km)}</b>
                      </span>
                    ) : (
                      tien(s.donGia)
                    );
                  },
                },
                { title: 'Tồn', dataIndex: 'soLuong', width: 70, align: 'right', className: 'so', render: (v: number) => (v <= 0 ? <Tag color="default">Hết</Tag> : v < 5 ? <b style={{ color: '#b45309' }}>{v}</b> : v) },
              ]}
            />
          </section>
        </div>

        <aside className="khung pos-phai" aria-label="Hóa đơn đang lập">
          <div className="khung-dau" style={{ flexDirection: 'column', alignItems: 'stretch' }}>
            <Space.Compact style={{ width: '100%' }}>
              <Input
                ref={oSdt}
                prefix={<UserOutlined aria-hidden />}
                placeholder="SĐT khách (bỏ trống = khách lẻ)"
                aria-label="Số điện thoại khách"
                value={sdt}
                inputMode="numeric"
                maxLength={10}
                onChange={(e) => {
                  setSdt(e.target.value.replace(/\D/g, ''));
                  setKhongThayKhach(false);
                }}
                onPressEnter={timKhach}
                suffix={<kbd>F4</kbd>}
              />
              <Button onClick={timKhach}>Tìm</Button>
            </Space.Compact>
            {khach && (
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 8 }}>
                <span>
                  <b>{khach.hoTen}</b> · {khach.maKh} <The bang={HANG_TV} ma={khach.hangThanhVien} /> <span style={{ color: '#525252', fontSize: 12 }}>{so(khach.diemTichLuy)} điểm</span>
                </span>
                <Button size="small" type="text" onClick={() => { setKhach(null); setSdt(''); }} aria-label="Bỏ chọn khách">
                  Bỏ
                </Button>
              </div>
            )}
            {khongThayKhach && (
              <Alert
                type="info"
                showIcon
                message="Chưa có khách với SĐT này"
                action={
                  <Button size="small" icon={<UserAddOutlined />} onClick={() => setMoThemKhach(true)}>
                    Thêm khách
                  </Button>
                }
              />
            )}
          </div>

          <div className="pos-gio" role="list" aria-label="Giỏ hàng">
            {gio.length === 0 ? (
              <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="Chưa có sản phẩm. Gõ mã SP rồi Enter." style={{ padding: 24 }} />
            ) : (
              gio.map((d, i) => {
                const dongTinh = bang?.dongs.find((x) => x.maSp === d.sp.maSp);
                return (
                  <div key={d.sp.maSp} role="listitem" className={`dong-gio${i === dongChon ? ' dang-chon' : ''}`} onClick={() => setDongChon(i)}>
                    <div style={{ minWidth: 0 }}>
                      <div className="ten">{d.sp.tenSp}</div>
                      <div className="phu">
                        {d.sp.maSp} · {bienThe(d.sp.kichCo, d.sp.mauSac)} · {tien(dongTinh?.donGiaBan ?? d.sp.donGia)}
                      </div>
                    </div>
                    <Space.Compact size="small">
                      <Button icon={<MinusOutlined />} onClick={() => doiSl(i, d.soLuong - 1)} disabled={d.soLuong <= 1} aria-label="Giảm số lượng" />
                      <InputNumber size="small" min={1} max={d.sp.soLuong} value={d.soLuong} onChange={(v) => v && doiSl(i, v)} style={{ width: 52 }} aria-label="Số lượng" controls={false} />
                      <Button icon={<PlusOutlined />} onClick={() => doiSl(i, d.soLuong + 1)} disabled={d.soLuong >= d.sp.soLuong} aria-label="Tăng số lượng" />
                    </Space.Compact>
                    <div style={{ textAlign: 'right', minWidth: 90 }}>
                      <div className="so" style={{ fontWeight: 600 }}>{tien(dongTinh?.thanhTien ?? d.sp.donGia * d.soLuong)}</div>
                      <Button type="text" size="small" danger icon={<DeleteOutlined />} onClick={(e) => { e.stopPropagation(); xoaDong(i); }} aria-label={`Xóa ${d.sp.maSp}`} />
                    </div>
                  </div>
                );
              })
            )}
          </div>

          <div className="pos-tong">
            <Space.Compact style={{ width: '100%', marginBottom: 10 }}>
              <Input
                ref={oVoucher}
                placeholder="Mã voucher"
                aria-label="Mã voucher"
                value={voucher}
                disabled={!!voucherApDung}
                onChange={(e) => setVoucher(e.target.value.toUpperCase())}
                onPressEnter={() => voucher.trim() && setVoucherApDung(voucher.trim())}
                suffix={<kbd>F6</kbd>}
              />
              {voucherApDung ? (
                <Button onClick={() => { setVoucherApDung(null); setVoucher(''); }}>Bỏ mã</Button>
              ) : (
                <Button onClick={() => voucher.trim() && setVoucherApDung(voucher.trim())} disabled={!voucher.trim()}>
                  Áp dụng
                </Button>
              )}
            </Space.Compact>
            {tinh.isError && <Alert type="error" showIcon message={loiThanhChu(tinh.error)} style={{ marginBottom: 10 }} />}
            <div className="tong-cong" style={{ maxWidth: 'none' }}>
              <span>Tạm tính</span>
              <span style={{ textAlign: 'right' }}>{tien(bang?.tamTinh ?? 0)}</span>
              <span>
                Giảm giá{bang?.ghiChuKm ? <span style={{ color: '#525252', fontSize: 12 }}> · {bang.ghiChuKm}</span> : null}
              </span>
              <span style={{ textAlign: 'right', color: bang?.tienGiam ? '#15803d' : undefined }}>{bang?.tienGiam ? `−${tien(bang.tienGiam)}` : tien(0)}</span>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', margin: '8px 0 10px' }}>
              <span style={{ fontWeight: 600 }}>Khách cần trả</span>
              <span className="tong-lon" aria-live="polite">{tien(tongTien)}</span>
            </div>
            <Segmented
              block
              value={phuongThuc}
              onChange={(v) => setPhuongThuc(v as typeof phuongThuc)}
              options={[
                { value: 'TIEN_MAT', label: 'Tiền mặt' },
                { value: 'CHUYEN_KHOAN', label: 'Chuyển khoản' },
              ]}
              style={{ marginBottom: 10 }}
            />
            {phuongThuc === 'TIEN_MAT' && tongTien > 0 && (
              <div style={{ marginBottom: 10 }}>
                <InputNumber
                  style={{ width: '100%' }}
                  placeholder="Tiền khách đưa"
                  aria-label="Tiền khách đưa"
                  min={0}
                  step={10000}
                  value={khachDua}
                  onChange={(v) => setKhachDua(v)}
                  formatter={(v) => (v ? `${so(Number(v))}` : '')}
                  parser={(v) => Number((v ?? '').replace(/\D/g, ''))}
                  addonAfter="đ"
                />
                <div style={{ display: 'flex', gap: 6, marginTop: 6, flexWrap: 'wrap' }}>
                  {goiYTien.map((g) => (
                    <Button key={g} size="small" onClick={() => setKhachDua(g)}>
                      {so(g)}
                    </Button>
                  ))}
                </div>
                {khachDua != null && khachDua >= tongTien && (
                  <div style={{ marginTop: 6 }}>
                    Tiền thừa: <b className="so">{tien(khachDua - tongTien)}</b>
                  </div>
                )}
              </div>
            )}
            <Button type="primary" block className="nut-thanh-toan" onClick={thanhToan} loading={dangThanhToan} disabled={gio.length === 0}>
              Thanh toán {tongTien ? tien(tongTien) : ''} <kbd style={{ marginLeft: 8, background: '#333', color: '#fff', borderColor: '#555' }}>F9</kbd>
            </Button>
          </div>
        </aside>
      </div>

      <ThemKhachModal
        mo={moThemKhach}
        sdt={sdt}
        dong={() => setMoThemKhach(false)}
        xong={(k) => {
          setKhach(k);
          setKhongThayKhach(false);
          setMoThemKhach(false);
          oTim.current?.focus();
        }}
      />

      <Modal
        open={!!hoaDonVua}
        title="Đã lập hóa đơn"
        onCancel={() => setHoaDonVua(null)}
        afterClose={() => oTim.current?.focus()}
        footer={[
          <Button key="moi" onClick={() => setHoaDonVua(null)}>
            Đơn mới (Esc)
          </Button>,
          <Button key="in" type="primary" icon={<PrinterOutlined />} autoFocus onClick={() => hoaDonVua && taiFile(`/api/hoa-don/${hoaDonVua.maHoaDon}/pdf`).catch((e) => message.error(loiThanhChu(e)))}>
            In hóa đơn (Enter)
          </Button>,
        ]}
      >
        {hoaDonVua && (
          <div style={{ fontSize: 15 }}>
            <p style={{ margin: '0 0 6px' }}>
              Mã hóa đơn <b>{hoaDonVua.maHoaDon}</b> · {PHUONG_THUC_NHAN[hoaDonVua.phuongThucTt as 'TIEN_MAT'] ?? hoaDonVua.phuongThucTt}
            </p>
            <p style={{ margin: 0 }}>
              Tổng thu <b className="so" style={{ fontSize: 22 }}>{tien(hoaDonVua.tongTien)}</b>
              {hoaDonVua.tienGiam > 0 && <span style={{ color: '#525252' }}> (đã giảm {tien(hoaDonVua.tienGiam)})</span>}
            </p>
          </div>
        )}
      </Modal>
    </>
  );
}

const PHUONG_THUC_NHAN = { TIEN_MAT: 'Tiền mặt', CHUYEN_KHOAN: 'Chuyển khoản' } as const;

export function ThemKhachModal({ mo, sdt, dong, xong }: { mo: boolean; sdt?: string; dong: () => void; xong: (k: KhachHang) => void }) {
  const [form] = Form.useForm();
  const [dangGui, setDangGui] = useState(false);
  const { message } = AntApp.useApp();
  useEffect(() => {
    if (mo) form.setFieldsValue({ sdt: sdt ?? '', hoTen: '', email: '', tinhThanh: undefined, diaChi: '' });
  }, [mo, sdt, form]);

  const gui = async () => {
    const v = await form.validateFields();
    setDangGui(true);
    try {
      const k = await post<KhachHang>('/api/khach-hang', { ...v, email: v.email || null });
      message.success(`Đã thêm khách ${k.maKh}`);
      xong(k);
    } catch (e) {
      message.error(loiThanhChu(e));
    } finally {
      setDangGui(false);
    }
  };

  return (
    <Modal open={mo} title="Thêm khách hàng tại quầy" onCancel={dong} onOk={gui} okText="Lưu khách" cancelText="Hủy" confirmLoading={dangGui} destroyOnClose>
      <Form form={form} layout="vertical" requiredMark="optional" onFinish={gui}>
        <Form.Item name="hoTen" label="Họ tên" rules={[{ required: true, message: 'Nhập họ tên' }]}>
          <Input autoFocus autoComplete="off" />
        </Form.Item>
        <Form.Item name="sdt" label="Số điện thoại" rules={[{ required: true, pattern: /^0\d{9}$/, message: '10 số, bắt đầu bằng 0' }]}>
          <Input inputMode="numeric" maxLength={10} />
        </Form.Item>
        <Form.Item name="email" label="Email" rules={[{ type: 'email', message: 'Email chưa đúng' }]}>
          <Input />
        </Form.Item>
        <Form.Item name="tinhThanh" label="Tỉnh / thành">
          <Select allowClear showSearch options={TINH_THANH.map((t) => ({ value: t, label: t }))} />
        </Form.Item>
        <Form.Item name="diaChi" label="Địa chỉ">
          <Input />
        </Form.Item>
        <button type="submit" hidden />
      </Form>
    </Modal>
  );
}
