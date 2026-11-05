import { CheckOutlined, CloseOutlined, InboxOutlined, PlusOutlined, PrinterOutlined } from '@ant-design/icons';
import { useQueries, useQuery, useQueryClient } from '@tanstack/react-query';
import { Alert, App as AntApp, Button, Checkbox, Input, InputNumber, Modal, Radio, Segmented, Select, Skeleton, Table, Tag } from 'antd';
import { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { get, post, taiFile } from '../api/client';
import type { ChiTietDoiTra, DoiTra, HoaDon, SanPham } from '../api/types';
import { ChuaChon, loiThanhChu, ThongTin, TieuDeTrang } from '../components/Chung';
import { MainSub } from '../components/MainSub';
import { bienThe, ngay, tien } from '../lib/dinhDang';
import { The, TRANG_THAI_DT } from '../lib/nhan';
import { usePhimTat } from '../lib/phimTat';

export default function DoiTraPage() {
  const [params, setParams] = useSearchParams();
  const loc = params.get('loc') ?? 'CHO_DUYET';
  const dangChon = params.get('ma');
  const lapTu = params.get('lap');
  const [moLap, setMoLap] = useState(!!lapTu);
  const ds = useQuery({ queryKey: ['doi-tra', 'ds', loc], queryFn: () => get<DoiTra[]>('/api/doi-tra', { trangThai: loc || undefined }), refetchInterval: 30_000 });
  const datParam = (k: string, v: string | null) =>
    setParams((p) => {
      const n = new URLSearchParams(p);
      if (v) n.set(k, v);
      else n.delete(k);
      return n;
    }, { replace: true });

  usePhimTat([
    { phim: 'Alt+N', chay: () => setMoLap(true) },
    { phim: 'Alt+L', chay: () => ds.refetch() },
  ]);

  return (
    <>
      <TieuDeTrang
        tieuDe="Đổi trả"
        moTa="Yêu cầu khách gửi từ web và phiếu lập tại quầy. Hoàn tất = đã nhận hàng về kho (đổi thì xuất hàng mới, trả thì hoàn tiền)."
        phai={
          <Button type="primary" icon={<PlusOutlined />} onClick={() => setMoLap(true)}>
            Lập phiếu tại quầy <kbd className="kbd-toi">Alt N</kbd>
          </Button>
        }
      />
      <div style={{ marginBottom: 12 }}>
        <Segmented
          value={loc}
          onChange={(v) => setParams({ loc: String(v) }, { replace: true })}
          options={[
            { value: 'CHO_DUYET', label: 'Chờ duyệt' },
            { value: 'DA_DUYET', label: 'Đã duyệt, chờ nhận hàng' },
            { value: 'HOAN_TAT', label: 'Hoàn tất' },
            { value: 'TU_CHOI', label: 'Từ chối' },
            { value: '', label: 'Tất cả' },
          ]}
        />
      </div>
      <MainSub<DoiTra>
        duLieu={ds.data ?? []}
        khoa="maDoiTra"
        dangChon={dangChon}
        chon={(m) => datParam('ma', m)}
        dangTai={ds.isLoading}
        cot={[
          { title: 'Mã phiếu', dataIndex: 'maDoiTra', width: 90, render: (v) => <b>{v}</b> },
          { title: 'Loại', dataIndex: 'loai', width: 64, render: (v) => <Tag bordered={false} color={v === 'DOI' ? 'blue' : 'magenta'}>{v === 'DOI' ? 'Đổi' : 'Trả'}</Tag> },
          { title: 'Hóa đơn', dataIndex: 'maHoaDon', width: 90 },
          { title: 'Khách', ellipsis: true, render: (_, d) => d.tenKhachHang ?? <span style={{ color: '#737373' }}>Khách lẻ</span> },
          { title: 'Ngày', dataIndex: 'ngayDoiTra', width: 92, render: ngay },
          { title: 'Trạng thái', dataIndex: 'trangThai', width: 104, render: (v) => <The bang={TRANG_THAI_DT} ma={v} /> },
        ]}
        chiTiet={dangChon ? <ChiTiet key={dangChon} ma={dangChon} dt={ds.data?.find((d) => d.maDoiTra === dangChon)} /> : <ChuaChon />}
      />
      <LapPhieu
        mo={moLap}
        maHdBanDau={lapTu ?? ''}
        dong={() => {
          setMoLap(false);
          datParam('lap', null);
        }}
        xong={(ma) => {
          setMoLap(false);
          setParams({ loc: '', ma }, { replace: true });
        }}
      />
    </>
  );
}

function ChiTiet({ ma, dt: dtDs }: { ma: string; dt?: DoiTra }) {
  const { message, modal } = AntApp.useApp();
  const qc = useQueryClient();
  const [dangLam, setDangLam] = useState<string | null>(null);
  const q = useQuery({ queryKey: ['doi-tra', 'ct', ma], queryFn: () => get<DoiTra>(`/api/doi-tra/${ma}`), initialData: dtDs });
  const d = q.data;

  const lam = async (ten: string, goi: () => Promise<unknown>, tb: string) => {
    setDangLam(ten);
    try {
      await goi();
      message.success(tb);
      await qc.invalidateQueries({ queryKey: ['doi-tra'] });
      qc.invalidateQueries({ queryKey: ['san-pham'] });
    } catch (e) {
      message.error(loiThanhChu(e));
    } finally {
      setDangLam(null);
    }
  };
  const inPdf = () => taiFile(`/api/doi-tra/${ma}/pdf`).catch((e) => message.error(loiThanhChu(e)));
  usePhimTat([{ phim: 'Alt+P', chay: inPdf }]);

  if (!d) return q.isError ? <Alert type="error" message={loiThanhChu(q.error)} style={{ margin: 16 }} /> : <div style={{ padding: 16 }}><Skeleton active /></div>;

  const hoanTat = () =>
    modal.confirm({
      title: d.loai === 'DOI' ? 'Đã nhận hàng cũ và giao hàng mới cho khách?' : 'Đã nhận hàng và hoàn tiền cho khách?',
      content: d.loai === 'DOI' ? 'Hàng cũ cộng lại vào kho, hàng mới trừ khỏi kho.' : 'Hàng cộng lại vào kho. Tiền hoàn tính theo giá khách đã trả (đã trừ giảm giá hóa đơn).',
      okText: 'Hoàn tất',
      cancelText: 'Quay lại',
      onOk: () =>
        lam('ht', async () => {
          const kq = await post<DoiTra>(`/api/doi-tra/${ma}/hoan-tat`);
          if (kq.loai === 'TRA') message.info(`Hoàn cho khách ${tien(kq.tienHoan)}`);
        }, `Phiếu ${ma} đã hoàn tất`),
    });

  return (
    <>
      <div className="ct-dau">
        <div>
          <div className="ct-ma">
            {d.maDoiTra} <The bang={TRANG_THAI_DT} ma={d.trangThai} />
            <Tag bordered={false} color={d.loai === 'DOI' ? 'blue' : 'magenta'}>{d.loai === 'DOI' ? 'Đổi hàng' : 'Trả hàng'}</Tag>
          </div>
          <div className="ct-phu">
            Lập ngày {ngay(d.ngayDoiTra)} · hóa đơn {d.maHoaDon}
            {d.tenNhanVien && ` · xử lý: ${d.tenNhanVien}`}
          </div>
        </div>
        <div className="ct-hanh-dong">
          {d.trangThai === 'CHO_DUYET' && (
            <>
              <Button danger icon={<CloseOutlined />} loading={dangLam === 'tc'} onClick={() => modal.confirm({ title: `Từ chối phiếu ${ma}?`, okText: 'Từ chối', okButtonProps: { danger: true }, cancelText: 'Quay lại', onOk: () => lam('tc', () => post(`/api/doi-tra/${ma}/tu-choi`), `Đã từ chối ${ma}`) })}>
                Từ chối
              </Button>
              <Button icon={<CheckOutlined />} loading={dangLam === 'd'} onClick={() => lam('d', () => post(`/api/doi-tra/${ma}/duyet`), `Đã duyệt ${ma}, chờ khách mang hàng tới`)}>
                Duyệt
              </Button>
            </>
          )}
          {(d.trangThai === 'CHO_DUYET' || d.trangThai === 'DA_DUYET') && (
            <Button type="primary" icon={<InboxOutlined />} loading={dangLam === 'ht'} onClick={hoanTat}>
              Nhận hàng &amp; hoàn tất
            </Button>
          )}
          <Button icon={<PrinterOutlined />} onClick={inPdf} aria-label="In PDF phiếu đổi trả" />
        </div>
      </div>
      <div className="ct-phan">
        <ThongTin
          items={[
            { nhan: 'Khách hàng', gt: d.tenKhachHang ? `${d.tenKhachHang} · ${d.maKh}` : 'Khách lẻ' },
            { nhan: 'Hóa đơn gốc', gt: d.maHoaDon },
            { nhan: 'Lý do', gt: d.lyDo ?? 'Không ghi', span: 2 },
            ...(d.trangThai === 'HOAN_TAT' && d.loai === 'TRA' ? [{ nhan: 'Đã hoàn', gt: <b>{tien(d.tienHoan)}</b> }, { nhan: 'Ngày hoàn', gt: ngay(d.ngayHoan) }] : []),
          ]}
        />
      </div>
      <div className="ct-phan">
        <h3>Sản phẩm</h3>
        <Table<ChiTietDoiTra>
          size="small"
          rowKey="maSp"
          pagination={false}
          dataSource={d.chiTiets}
          columns={[
            { title: 'Sản phẩm trả lại', render: (_, c) => <><b>{c.maSp}</b> {c.tenSp}</> },
            { title: 'SL', dataIndex: 'soLuong', width: 50, align: 'right', className: 'so' },
            ...(d.loai === 'DOI' ? [{ title: 'Đổi sang', render: (_: unknown, c: ChiTietDoiTra) => <><b>{c.maSpDoi}</b> {c.tenSpDoi}</> }] : []),
          ]}
        />
      </div>
    </>
  );
}

interface DongChon {
  maSp: string;
  chon: boolean;
  soLuong: number;
  maSpDoi?: string;
}

function LapPhieu({ mo, maHdBanDau, dong, xong }: { mo: boolean; maHdBanDau: string; dong: () => void; xong: (ma: string) => void }) {
  const { message } = AntApp.useApp();
  const qc = useQueryClient();
  const [maHd, setMaHd] = useState(maHdBanDau);
  const [maTim, setMaTim] = useState(maHdBanDau);
  const [loai, setLoai] = useState<'DOI' | 'TRA'>('DOI');
  const [lyDo, setLyDo] = useState('');
  const [dongs, setDongs] = useState<DongChon[]>([]);
  const [hoanTatNgay, setHoanTatNgay] = useState(true);
  const [dangGui, setDangGui] = useState(false);

  useEffect(() => {
    if (mo) {
      setMaHd(maHdBanDau);
      setMaTim(maHdBanDau);
      setLyDo('');
      // Mở lại hộp thoại thì về mặc định, không giữ lựa chọn "Trả hàng" / bỏ tick của phiếu trước
      setLoai('DOI');
      setHoanTatNgay(true);
    }
  }, [mo, maHdBanDau]);

  const hd = useQuery({ queryKey: ['hoa-don', 'dau', maTim], queryFn: () => get<HoaDon>(`/api/hoa-don/${maTim}`), enabled: mo && !!maTim, retry: false });
  const cacPhieu = useQuery({ queryKey: ['doi-tra', 'ds', ''], queryFn: () => get<DoiTra[]>('/api/doi-tra'), enabled: mo && !!hd.data });

  const daDoiTra = useMemo(() => {
    const m: Record<string, number> = {};
    for (const p of cacPhieu.data ?? []) if (p.maHoaDon === maTim && p.trangThai !== 'TU_CHOI') for (const c of p.chiTiets) m[c.maSp] = (m[c.maSp] ?? 0) + c.soLuong;
    return m;
  }, [cacPhieu.data, maTim]);

  useEffect(() => {
    setDongs((hd.data?.chiTiets ?? []).map((c) => ({ maSp: c.maSp, chon: false, soLuong: 1 })));
  }, [hd.data]);

  const bienThes = useQueries({
    queries: (loai === 'DOI' ? dongs.filter((d) => d.chon) : []).map((d) => ({
      queryKey: ['san-pham', 'bien-the', d.maSp],
      queryFn: () => get<SanPham[]>(`/api/san-pham/${d.maSp}/bien-the`),
      staleTime: 60_000,
    })),
  });
  const bienTheCua = (maSp: string) => {
    const i = dongs.filter((d) => d.chon).findIndex((d) => d.maSp === maSp);
    return (bienThes[i]?.data ?? []).filter((s) => s.maSp !== maSp && s.trangThai === 'DANG_BAN');
  };

  const sua = (maSp: string, p: Partial<DongChon>) => setDongs((ds) => ds.map((d) => (d.maSp === maSp ? { ...d, ...p } : d)));
  const daChon = dongs.filter((d) => d.chon);
  const quaHan = hd.data && (Date.now() - new Date(hd.data.ngayLap).getTime()) / 86_400_000 > 31;

  const gui = async () => {
    if (!hd.data) return;
    if (daChon.length === 0) return message.warning('Chọn ít nhất 1 sản phẩm');
    if (loai === 'DOI' && daChon.some((d) => !d.maSpDoi)) return message.warning('Chọn size / màu muốn đổi sang cho từng sản phẩm');
    setDangGui(true);
    try {
      const p = await post<DoiTra>('/api/doi-tra', {
        maHoaDon: hd.data.maHoaDon,
        loai,
        lyDo: lyDo || null,
        chiTiets: daChon.map((d) => ({ maSp: d.maSp, soLuong: d.soLuong, maSpDoi: loai === 'DOI' ? d.maSpDoi : null })),
      });
      let tb = `Đã lập phiếu ${p.maDoiTra}`;
      if (hoanTatNgay) {
        try {
          const kq = await post<DoiTra>(`/api/doi-tra/${p.maDoiTra}/hoan-tat`);
          tb = kq.loai === 'TRA' ? `Phiếu ${p.maDoiTra} hoàn tất. Hoàn cho khách ${tien(kq.tienHoan)}` : `Phiếu ${p.maDoiTra} hoàn tất, đã đổi hàng`;
        } catch (e) {
          // Phiếu đã lưu: đóng hộp thoại để không bấm lại tạo phiếu trùng, hoàn tất sau ở danh sách
          message.warning(`Đã lập phiếu ${p.maDoiTra} nhưng chưa hoàn tất được: ${loiThanhChu(e)}. Bấm "Nhận hàng & hoàn tất" ở danh sách để làm lại.`);
          qc.invalidateQueries({ queryKey: ['doi-tra'] });
          xong(p.maDoiTra);
          return;
        }
      }
      message.success(tb);
      qc.invalidateQueries({ queryKey: ['doi-tra'] });
      qc.invalidateQueries({ queryKey: ['san-pham'] });
      xong(p.maDoiTra);
    } catch (e) {
      message.error(loiThanhChu(e));
    } finally {
      setDangGui(false);
    }
  };

  return (
    <Modal open={mo} onCancel={dong} width={760} title="Lập phiếu đổi trả tại quầy" okText={hoanTatNgay ? 'Lập và hoàn tất' : 'Lập phiếu'} cancelText="Hủy" onOk={gui} confirmLoading={dangGui} okButtonProps={{ disabled: !hd.data || daChon.length === 0 }} destroyOnClose>
      <Input.Search
        autoFocus
        placeholder="Nhập mã hóa đơn, vd HD00012"
        aria-label="Mã hóa đơn"
        enterButton="Tìm hóa đơn"
        value={maHd}
        onChange={(e) => setMaHd(e.target.value.toUpperCase())}
        onSearch={(v) => setMaTim(v.trim().toUpperCase())}
        loading={hd.isFetching}
        style={{ marginBottom: 12 }}
      />
      {hd.isError && <Alert type="error" showIcon message={loiThanhChu(hd.error)} />}
      {hd.data && (
        <>
          <div style={{ marginBottom: 12, color: '#525252' }}>
            {hd.data.maHoaDon} · ngày {ngay(hd.data.ngayLap)} · {hd.data.tenKhachHang ?? 'Khách lẻ'} · {tien(hd.data.tongTien)}
          </div>
          {hd.data.trangThai !== 'DA_THANH_TOAN' && <Alert type="warning" showIcon message="Chỉ đổi trả được hóa đơn đã thanh toán" style={{ marginBottom: 12 }} />}
          {quaHan && <Alert type="warning" showIcon message="Hóa đơn đã quá 30 ngày, hệ thống sẽ không cho đổi trả" style={{ marginBottom: 12 }} />}
          <Radio.Group value={loai} onChange={(e) => setLoai(e.target.value)} optionType="button" buttonStyle="solid" style={{ marginBottom: 12 }}>
            <Radio.Button value="DOI">Đổi size / màu</Radio.Button>
            <Radio.Button value="TRA">Trả hàng, hoàn tiền</Radio.Button>
          </Radio.Group>
          <Table
            size="small"
            rowKey="maSp"
            pagination={false}
            dataSource={hd.data.chiTiets}
            columns={[
              {
                title: '',
                width: 36,
                render: (_, c) => {
                  const con = c.soLuong - (daDoiTra[c.maSp] ?? 0);
                  const d = dongs.find((x) => x.maSp === c.maSp);
                  return <Checkbox disabled={con <= 0} checked={d?.chon} onChange={(e) => sua(c.maSp, { chon: e.target.checked, soLuong: 1 })} aria-label={`Chọn ${c.maSp}`} />;
                },
              },
              { title: 'Sản phẩm', render: (_, c) => <><b>{c.maSp}</b> {c.tenSp} <span style={{ color: '#737373' }}>{bienThe(c.kichCo, c.mauSac)}</span></> },
              {
                title: 'Còn đổi được',
                width: 100,
                align: 'right',
                render: (_, c) => {
                  const con = c.soLuong - (daDoiTra[c.maSp] ?? 0);
                  return con <= 0 ? <Tag bordered={false}>Đã yêu cầu hết</Tag> : `${con} / ${c.soLuong}`;
                },
              },
              {
                title: 'SL',
                width: 80,
                render: (_, c) => {
                  const d = dongs.find((x) => x.maSp === c.maSp);
                  const con = c.soLuong - (daDoiTra[c.maSp] ?? 0);
                  return <InputNumber size="small" min={1} max={Math.max(1, con)} value={d?.soLuong} disabled={!d?.chon} onChange={(v) => v && sua(c.maSp, { soLuong: v })} style={{ width: 64 }} aria-label="Số lượng" />;
                },
              },
              ...(loai === 'DOI'
                ? [
                    {
                      title: 'Đổi sang',
                      width: 200,
                      render: (_: unknown, c: { maSp: string }) => {
                        const d = dongs.find((x) => x.maSp === c.maSp);
                        if (!d?.chon) return null;
                        return (
                          <Select
                            size="small"
                            style={{ width: '100%' }}
                            placeholder="Chọn size / màu"
                            value={d.maSpDoi}
                            onChange={(v) => sua(c.maSp, { maSpDoi: v })}
                            options={bienTheCua(c.maSp).map((s) => ({ value: s.maSp, label: `${bienThe(s.kichCo, s.mauSac)} · tồn ${s.soLuong}`, disabled: s.soLuong < (d.soLuong ?? 1) }))}
                          />
                        );
                      },
                    },
                  ]
                : []),
            ]}
          />
          <Input.TextArea style={{ marginTop: 12 }} rows={2} maxLength={255} placeholder="Lý do (vd: chật, không vừa màu)" aria-label="Lý do đổi trả" value={lyDo} onChange={(e) => setLyDo(e.target.value)} />
          <Checkbox style={{ marginTop: 12 }} checked={hoanTatNgay} onChange={(e) => setHoanTatNgay(e.target.checked)}>
            Khách đang ở quầy: nhận hàng và hoàn tất ngay ({loai === 'DOI' ? 'xuất hàng mới' : 'hoàn tiền'})
          </Checkbox>
          {cacPhieu.data && Object.keys(daDoiTra).length > 0 && (
            <div style={{ fontSize: 12, color: '#737373', marginTop: 8 }}>Hóa đơn này đã có phiếu đổi trả trước đó; cột “Còn đổi được” đã trừ phần đã yêu cầu.</div>
          )}
        </>
      )}
    </Modal>
  );
}
