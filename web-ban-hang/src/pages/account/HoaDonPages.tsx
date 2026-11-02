import { Link, useParams } from 'react-router-dom';
import { Download, RefreshCcw } from 'lucide-react';
import { hoaDonApi } from '../../api/khachApi';
import { ApiError } from '../../api/apiClient';
import type { HoaDon } from '../../api/types';
import { Badge, Button, ButtonLink, EmptyState, ErrorState, LoadingRegion, Skeleton } from '../../components';
import { useHoaDon, useHoaDons } from '../../features/account/useTaiKhoan';
import { PHUONG_THUC, TRANG_THAI_HOA_DON } from '../../features/account/nhan';
import { useTaiPdf } from '../../features/account/taiPdf';
import { quyTac } from '../../config/cuaHang';
import { formatNgay, formatTien } from '../../lib/format';
import { useTitle } from '../../lib/useTitle';
import { DanhSachDong, TongTien } from './DongSanPham';
import f from '../Form.module.css';
import s from './Account.module.css';

/** Cung dieu kien backend (KhoBLL.GuiYeuCau): da thanh toan va trong 30 ngay tu ngay lap */
export function duocDoiTra(h: Pick<HoaDon, 'trangThai' | 'ngayLap'>) {
  if (h.trangThai !== 'DA_THANH_TOAN') return false;
  const homNay = new Date(); homNay.setHours(0, 0, 0, 0);
  const lap = new Date(h.ngayLap); lap.setHours(0, 0, 0, 0);
  return (homNay.getTime() - lap.getTime()) / 86_400_000 <= quyTac.soNgayDoiTra;
}

export function HoaDonListPage() {
  useTitle('Hóa đơn');
  const q = useHoaDons();
  return (
    <>
      <div>
        <h1 className="h1">Hóa đơn</h1>
        <p className="small muted">Gồm hóa đơn của đơn online và hóa đơn mua tại cửa hàng.</p>
      </div>
      {q.error ? <ErrorState error={q.error} onRetry={() => q.refetch()} />
        : q.isLoading ? <LoadingRegion><div className={s.list}>{[0, 1, 2].map((i) => <Skeleton key={i} h={88} />)}</div></LoadingRegion>
        : !q.data?.length ? <EmptyState title="Chưa có hóa đơn nào" description="Hóa đơn được tạo khi cửa hàng xác nhận đơn hàng hoặc khi bạn mua tại cửa hàng." />
        : (
          <ul className={s.list}>
            {q.data.map((h) => {
              const tt = TRANG_THAI_HOA_DON[h.trangThai];
              return (
                <li key={h.maHoaDon}>
                  <Link to={`/tai-khoan/hoa-don/${h.maHoaDon}`} className={s.row}>
                    <div className={s.rowTop}>
                      <span className={s.code}>{h.maHoaDon}</span>
                      <Badge tone={tt?.tone}>{tt?.nhan ?? h.trangThai}</Badge>
                    </div>
                    <div className={s.rowMeta}>
                      <span>{formatNgay(h.ngayLap)}</span>
                      <span>{h.maDon ? `Đơn online ${h.maDon}` : 'Mua tại cửa hàng'}</span>
                      <span>Tổng <strong className="num">{formatTien(h.tongTien)}</strong></span>
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

export function HoaDonDetailPage() {
  const { ma = '' } = useParams();
  useTitle(`Hóa đơn ${ma}`);
  const q = useHoaDon(ma);
  const pdf = useTaiPdf(hoaDonApi.pdf, (m) => `HoaDon_${m}.pdf`);
  if (q.error) {
    return q.error instanceof ApiError && (q.error.khongTimThay || q.error.khongCoQuyen)
      ? <EmptyState title="Không tìm thấy hóa đơn" action={<Link to="/tai-khoan/hoa-don" className="link">Về danh sách hóa đơn</Link>} />
      : <ErrorState error={q.error} onRetry={() => q.refetch()} />;
  }
  const h = q.data;
  if (!h) return <LoadingRegion><Skeleton h={40} w="50%" /><Skeleton h={300} /></LoadingRegion>;
  const tt = TRANG_THAI_HOA_DON[h.trangThai];
  const tamTinh = h.chiTiets.reduce((t, c) => t + c.thanhTien, 0);
  return (
    <>
      <div className={s.titleRow}>
        <div>
          <p className="small muted"><Link to="/tai-khoan/hoa-don" className="link">Hóa đơn</Link> / {h.maHoaDon}</p>
          <h1 className="h1">Hóa đơn {h.maHoaDon}</h1>
        </div>
        <Badge tone={tt?.tone}>{tt?.nhan ?? h.trangThai}</Badge>
      </div>
      <section className={s.panel}>
        <dl className={s.kv}>
          <dt>Ngày lập</dt><dd>{formatNgay(h.ngayLap)}</dd>
          <dt>Nguồn</dt><dd>{h.maDon ? <Link to={`/tai-khoan/don-hang/${h.maDon}`} className="link">Đơn online {h.maDon}</Link> : 'Mua tại cửa hàng'}</dd>
          <dt>Thanh toán</dt><dd>{PHUONG_THUC[h.phuongThucTt] ?? h.phuongThucTt}</dd>
          {h.tenKm && <><dt>Khuyến mãi</dt><dd>{h.tenKm}</dd></>}
        </dl>
      </section>
      <section className={s.panel} aria-labelledby="sp-hd">
        <h2 id="sp-hd" className={s.panelTitle}>Sản phẩm</h2>
        <DanhSachDong dongs={h.chiTiets.map((c) => ({ ...c, donGia: c.donGiaBan }))} />
        <TongTien tamTinh={tamTinh} tienGiam={h.tienGiam} tongTien={h.tongTien} ghiChu={h.tenKm} />
      </section>
      <div className={f.actions}>
        <Button variant="secondary" iconLeft={<Download size={16} aria-hidden />} loading={pdf.isPending} onClick={() => pdf.mutate(h.maHoaDon)}>Tải PDF</Button>
        {duocDoiTra(h)
          ? <ButtonLink to={`/tai-khoan/doi-tra/moi?hoaDon=${h.maHoaDon}`} iconLeft={<RefreshCcw size={16} aria-hidden />}>Yêu cầu đổi / trả</ButtonLink>
          : h.trangThai === 'DA_THANH_TOAN' && <p className="small muted">Đã quá {quyTac.soNgayDoiTra} ngày kể từ ngày mua nên không thể đổi / trả.</p>}
      </div>
    </>
  );
}
