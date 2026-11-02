import { useMemo, useState, type FormEvent } from 'react';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { useMutation, useQueries, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { Download, Plus } from 'lucide-react';
import { doiTraApi, type YeuCauDoiTra } from '../../api/khachApi';
import { sanPhamApi } from '../../api/catalogApi';
import { ApiError } from '../../api/apiClient';
import { qk } from '../../api/queryKeys';
import { Badge, Button, ButtonLink, EmptyState, ErrorState, LoadingRegion, QuantityStepper, SelectField, Skeleton, TextAreaField } from '../../components';
import { useDoiTra, useDoiTras, useHoaDon, useHoaDons } from '../../features/account/useTaiKhoan';
import { nhanBienThe, TRANG_THAI_DOI_TRA } from '../../features/account/nhan';
import { useTaiPdf } from '../../features/account/taiPdf';
import { quyTac } from '../../config/cuaHang';
import { formatNgay, formatTien } from '../../lib/format';
import { useTitle } from '../../lib/useTitle';
import { duocDoiTra } from './HoaDonPages';
import f from '../Form.module.css';
import s from './Account.module.css';

const LOAI = { DOI: 'Đổi hàng', TRA: 'Trả hàng' } as const;

export function DoiTraListPage() {
  useTitle('Đổi / trả hàng');
  const q = useDoiTras();
  return (
    <>
      <div className={s.titleRow}>
        <div>
          <h1 className="h1">Đổi / trả hàng</h1>
          <p className="small muted">Đổi size, đổi màu hoặc trả hàng trong {quyTac.soNgayDoiTra} ngày kể từ ngày mua.</p>
        </div>
        <ButtonLink to="/tai-khoan/doi-tra/moi" iconLeft={<Plus size={16} aria-hidden />}>Tạo yêu cầu</ButtonLink>
      </div>
      {q.error ? <ErrorState error={q.error} onRetry={() => q.refetch()} />
        : q.isLoading ? <LoadingRegion><div className={s.list}>{[0, 1].map((i) => <Skeleton key={i} h={88} />)}</div></LoadingRegion>
        : !q.data?.length ? <EmptyState title="Chưa có yêu cầu đổi / trả" description="Chọn một hóa đơn đã thanh toán để gửi yêu cầu." />
        : (
          <ul className={s.list}>
            {q.data.map((d) => {
              const tt = TRANG_THAI_DOI_TRA[d.trangThai];
              return (
                <li key={d.maDoiTra}>
                  <Link to={`/tai-khoan/doi-tra/${d.maDoiTra}`} className={s.row}>
                    <div className={s.rowTop}>
                      <span className={s.code}>{d.maDoiTra} · {LOAI[d.loai]}</span>
                      <Badge tone={tt?.tone}>{tt?.nhan ?? d.trangThai}</Badge>
                    </div>
                    <div className={s.rowMeta}>
                      <span>{formatNgay(d.ngayDoiTra)}</span>
                      <span>Hóa đơn {d.maHoaDon}</span>
                      {d.tienHoan > 0 && <span>Hoàn <strong className="num">{formatTien(d.tienHoan)}</strong></span>}
                    </div>
                  </Link>
                </li>
              );
            })}
          </ul>
        )}
    </>
  );
}

export function DoiTraDetailPage() {
  const { ma = '' } = useParams();
  useTitle(`Phiếu đổi trả ${ma}`);
  const q = useDoiTra(ma);
  const pdf = useTaiPdf(doiTraApi.pdf, (m) => `DoiTra_${m}.pdf`);
  if (q.error) {
    return q.error instanceof ApiError && (q.error.khongTimThay || q.error.khongCoQuyen)
      ? <EmptyState title="Không tìm thấy phiếu đổi / trả" action={<Link to="/tai-khoan/doi-tra" className="link">Về danh sách</Link>} />
      : <ErrorState error={q.error} onRetry={() => q.refetch()} />;
  }
  const d = q.data;
  if (!d) return <LoadingRegion><Skeleton h={40} w="50%" /><Skeleton h={240} /></LoadingRegion>;
  const tt = TRANG_THAI_DOI_TRA[d.trangThai];
  return (
    <>
      <div className={s.titleRow}>
        <div>
          <p className="small muted"><Link to="/tai-khoan/doi-tra" className="link">Đổi / trả</Link> / {d.maDoiTra}</p>
          <h1 className="h1">{LOAI[d.loai]} {d.maDoiTra}</h1>
        </div>
        <Badge tone={tt?.tone}>{tt?.nhan ?? d.trangThai}</Badge>
      </div>
      <section className={s.panel}>
        <dl className={s.kv}>
          <dt>Ngày gửi</dt><dd>{formatNgay(d.ngayDoiTra)}</dd>
          <dt>Hóa đơn</dt><dd><Link to={`/tai-khoan/hoa-don/${d.maHoaDon}`} className="link">{d.maHoaDon}</Link></dd>
          {d.lyDo && <><dt>Lý do</dt><dd>{d.lyDo}</dd></>}
          {d.trangThai === 'HOAN_TAT' && d.loai === 'TRA' && <><dt>Tiền hoàn</dt><dd className="num">{formatTien(d.tienHoan)}{d.ngayHoan ? ` · ${formatNgay(d.ngayHoan)}` : ''}</dd></>}
        </dl>
        {d.trangThai === 'CHO_DUYET' && <p className={f.info}>Cửa hàng sẽ liên hệ để hướng dẫn gửi hàng. Vui lòng giữ nguyên tem nhãn sản phẩm.</p>}
      </section>
      <section className={s.panel} aria-labelledby="sp-dt">
        <h2 id="sp-dt" className={s.panelTitle}>Sản phẩm</h2>
        <ul className={s.items}>
          {d.chiTiets.map((c) => (
            <li key={c.maSp} className={s.item} style={{ gridTemplateColumns: '1fr auto' }}>
              <div>
                <p className={s.itemName}>{c.tenSp ?? c.maSp}</p>
                <p className={s.itemSub}>{c.maSp}{c.maSpDoi ? ` → đổi sang ${c.tenSpDoi ?? ''} (${c.maSpDoi})` : ''}</p>
              </div>
              <span className="num">SL {c.soLuong}</span>
            </li>
          ))}
        </ul>
      </section>
      <div><Button variant="secondary" iconLeft={<Download size={16} aria-hidden />} loading={pdf.isPending} onClick={() => pdf.mutate(d.maDoiTra)}>Tải PDF</Button></div>
    </>
  );
}

interface Chon { chon: boolean; soLuong: number; maSpDoi: string }

export function DoiTraNewPage() {
  useTitle('Yêu cầu đổi / trả');
  const [params, setParams] = useSearchParams();
  const maHd = params.get('hoaDon') ?? '';
  const hdsQ = useHoaDons();
  const hdQ = useHoaDon(maHd);
  const dtsQ = useDoiTras();
  const qc = useQueryClient();
  const navigate = useNavigate();
  const [loai, setLoai] = useState<'DOI' | 'TRA'>('DOI');
  const [lyDo, setLyDo] = useState('');
  const [chon, setChon] = useState<Record<string, Chon>>({});
  const [loi, setLoi] = useState('');

  const hopLe = (hdsQ.data ?? []).filter(duocDoiTra);
  const hd = hdQ.data;

  // so luong da yeu cau o cac phieu khac (tru phieu bi tu choi) -> con lai duoc doi / tra
  const daYeuCau = useMemo(() => {
    const m = new Map<string, number>();
    (dtsQ.data ?? []).filter((d) => d.maHoaDon === maHd && d.trangThai !== 'TU_CHOI')
      .forEach((d) => (d.chiTiets ?? []).forEach((c) => m.set(c.maSp, (m.get(c.maSp) ?? 0) + c.soLuong)));
    return m;
  }, [dtsQ.data, maHd]);

  const dongs = (hd?.chiTiets ?? []).map((c) => ({ ...c, conLai: Math.max(0, c.soLuong - (daYeuCau.get(c.maSp) ?? 0)) }));
  const daChon = dongs.filter((d) => chon[d.maSp]?.chon);

  // doi hang: lay bien the cung mau cua cac dong dang chon
  const btQs = useQueries({
    queries: loai === 'DOI' ? daChon.map((d) => ({ queryKey: qk.bienThe(d.maSp), queryFn: () => sanPhamApi.layBienThe(d.maSp) })) : [],
  });

  const gui = useMutation({
    mutationFn: (yc: YeuCauDoiTra) => doiTraApi.gui(yc),
    onSuccess: (dt) => {
      qc.invalidateQueries({ queryKey: qk.doiTras });
      toast.success('Đã gửi yêu cầu, cửa hàng sẽ xử lý sớm');
      navigate(`/tai-khoan/doi-tra/${dt.maDoiTra}`, { replace: true });
    },
    meta: { tuXuLyLoi: true },
  });

  const capNhat = (maSp: string, p: Partial<Chon>) =>
    setChon((x) => ({ ...x, [maSp]: { ...(x[maSp] ?? { chon: false, soLuong: 1, maSpDoi: '' }), ...p } }));

  const submit = (e: FormEvent) => {
    e.preventDefault();
    if (gui.isPending || !hd) return;
    if (daChon.length === 0) { setLoi('Chọn ít nhất 1 sản phẩm cần đổi / trả.'); return; }
    if (loai === 'DOI' && daChon.some((d) => !chon[d.maSp]?.maSpDoi)) { setLoi('Chọn size / màu muốn đổi sang cho từng sản phẩm.'); return; }
    setLoi('');
    gui.mutate({
      maHoaDon: hd.maHoaDon, loai, lyDo: lyDo.trim() || undefined,
      chiTiets: daChon.map((d) => ({ maSp: d.maSp, soLuong: chon[d.maSp].soLuong, maSpDoi: loai === 'DOI' ? chon[d.maSp].maSpDoi : null })),
    });
  };

  return (
    <>
      <div>
        <p className="small muted"><Link to="/tai-khoan/doi-tra" className="link">Đổi / trả</Link> / Tạo yêu cầu</p>
        <h1 className="h1">Yêu cầu đổi / trả</h1>
      </div>

      <section className={s.panel}>
        {hdsQ.isLoading ? <Skeleton h={44} /> : hopLe.length === 0 && !maHd ? (
          <EmptyState title="Không có hóa đơn nào đủ điều kiện" description={`Chỉ đổi / trả được hóa đơn đã thanh toán trong vòng ${quyTac.soNgayDoiTra} ngày.`} />
        ) : (
          <SelectField label="Hóa đơn" value={maHd} onChange={(e) => { setChon({}); setParams(e.target.value ? { hoaDon: e.target.value } : {}, { replace: true }); }}>
            <option value="">Chọn hóa đơn</option>
            {hopLe.map((h) => <option key={h.maHoaDon} value={h.maHoaDon}>{h.maHoaDon} · {formatNgay(h.ngayLap)} · {formatTien(h.tongTien)}</option>)}
            {maHd && !hopLe.some((h) => h.maHoaDon === maHd) && <option value={maHd}>{maHd}</option>}
          </SelectField>
        )}
      </section>

      {maHd && (hdQ.error ? <ErrorState error={hdQ.error} onRetry={() => hdQ.refetch()} />
        : !hd ? <Skeleton h={200} />
        : !duocDoiTra(hd) ? <p className={f.alert}>Hóa đơn {hd.maHoaDon} không đủ điều kiện đổi / trả (chưa thanh toán hoặc đã quá {quyTac.soNgayDoiTra} ngày).</p>
        : (
          <form className={s.panel} onSubmit={submit} noValidate>
            {gui.error && <p className={f.alert} role="alert">{gui.error.message}</p>}
            <fieldset className={s.radios} style={{ border: 0, padding: 0 }}>
              <legend className="sr-only">Loại yêu cầu</legend>
              {(['DOI', 'TRA'] as const).map((l) => (
                <label key={l} className={s.radio}>
                  <input type="radio" name="loai" checked={loai === l} onChange={() => setLoai(l)} />
                  {l === 'DOI' ? 'Đổi size / màu' : 'Trả hàng, hoàn tiền'}
                </label>
              ))}
            </fieldset>

            <div className={s.list}>
              {dongs.map((d) => {
                const c = chon[d.maSp];
                const idx = daChon.findIndex((x) => x.maSp === d.maSp);
                const bt = idx >= 0 ? btQs[idx] : undefined;
                const luaChon = (bt?.data ?? []).filter((b) => b.maSp !== d.maSp && b.trangThai === 'DANG_BAN' && b.soLuong > 0);
                return (
                  <div key={d.maSp} className={`${s.pick} ${c?.chon ? s.pickOn : ''}`}>
                    <label className={s.check}>
                      <input type="checkbox" checked={!!c?.chon} disabled={d.conLai === 0} onChange={(e) => capNhat(d.maSp, { chon: e.target.checked })} />
                      <span>
                        <strong>{d.tenSp}</strong>
                        <span className={s.itemSub} style={{ display: 'block' }}>
                          {nhanBienThe(d.mauSac, d.kichCo)} · đã mua {d.soLuong} · {d.conLai > 0 ? `còn đổi / trả được ${d.conLai}` : 'đã yêu cầu hết'}
                        </span>
                      </span>
                    </label>
                    {c?.chon && (
                      <div className={s.pickBody}>
                        <QuantityStepper value={c.soLuong} onChange={(n) => capNhat(d.maSp, { soLuong: n })} min={1} max={d.conLai} />
                        {loai === 'DOI' && (
                          bt?.isLoading ? <Skeleton h={44} />
                            : luaChon.length === 0 ? <p className="small muted">Hiện không còn size / màu khác của mẫu này để đổi.</p>
                            : (
                              <SelectField label="Đổi sang" value={c.maSpDoi} onChange={(e) => capNhat(d.maSp, { maSpDoi: e.target.value })} required>
                                <option value="">Chọn size / màu</option>
                                {luaChon.map((b) => <option key={b.maSp} value={b.maSp}>{nhanBienThe(b.mauSac, b.kichCo)} (còn {b.soLuong})</option>)}
                              </SelectField>
                            )
                        )}
                      </div>
                    )}
                  </div>
                );
              })}
            </div>

            <TextAreaField label="Lý do" value={lyDo} onChange={(e) => setLyDo(e.target.value)} maxLength={500} placeholder="Ví dụ: áo hơi rộng, muốn đổi size nhỏ hơn" />
            {loai === 'TRA' && <p className={f.info}>Tiền hoàn được tính theo giá bạn đã trả (sau khuyến mãi) và chuyển lại sau khi cửa hàng nhận hàng.</p>}
            {loi && <p className={f.alert} role="alert">{loi}</p>}
            <div><Button type="submit" loading={gui.isPending}>Gửi yêu cầu</Button></div>
          </form>
        ))}
    </>
  );
}
