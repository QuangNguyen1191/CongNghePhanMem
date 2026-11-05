// v3: Tra cuu don khong can tai khoan: xac thuc SDT bang OTP -> xem tat ca don cua SDT do.
import { Link, useLocation } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { vangLaiApi } from '../api/vangLaiApi';
import { qk } from '../api/queryKeys';
import { Badge, Breadcrumb, ButtonLink, EmptyState, ErrorState, LoadingRegion, Skeleton } from '../components';
import { TRANG_THAI_DON } from '../features/account/nhan';
import { useSession } from '../features/auth/useSession';
import { XacThucSdt } from '../features/vangLai/XacThucSdt';
import { formatNgay, formatTien } from '../lib/format';
import { phienVangLai, usePhienVangLai } from '../lib/phienVangLai';
import { useTitle } from '../lib/useTitle';
import DonHangDetailPage from './account/DonHangDetailPage';
import f from './Form.module.css';
import a from './account/Account.module.css';
import s from './CartPage.module.css';

/** Khung chung: chua xac thuc -> form OTP */
function CanXacThuc({ children }: { children: React.ReactNode }) {
  const pv = usePhienVangLai();
  const phien = useSession();
  const { pathname } = useLocation();
  if (pv) return <>{children}</>;
  return (
    <div className={f.card} style={{ marginInline: 'auto', marginTop: 'var(--s-5)' }}>
      <div className={f.head}>
        <h1 className="h1">Tra cứu đơn hàng</h1>
        <p className="muted small">Nhập số điện thoại đã dùng khi đặt hàng, xác nhận bằng mã OTP để xem và theo dõi đơn.</p>
      </div>
      <XacThucSdt mucDich="TRA_CUU" goiY="Mã OTP chỉ để xác nhận bạn là chủ số điện thoại" />
      <p className={f.foot}>
        {phien
          ? <>Đơn đặt bằng tài khoản xem ở <Link to="/tai-khoan/don-hang" className="link">Đơn hàng của tôi</Link>.</>
          : <>Đã có tài khoản? <Link to={`/dang-nhap?tiep=${encodeURIComponent(pathname === '/tra-cuu-don' ? '/tai-khoan/don-hang' : pathname)}`} className="link">Đăng nhập</Link> để xem tất cả đơn.</>}
      </p>
    </div>
  );
}

function DanhSach() {
  useTitle('Tra cứu đơn hàng');
  const pv = usePhienVangLai()!;
  const phien = useSession();
  const q = useQuery({ queryKey: qk.vangLaiDons(pv.sdt), queryFn: vangLaiApi.donHangs });
  return (
    <div style={{ display: 'grid', gap: 'var(--s-5)', marginTop: 'var(--s-4)' }}>
      <div className={a.titleRow}>
        <div>
          <h1 className="h1">Đơn hàng của số {pv.sdt}</h1>
          <p className="small muted">Phiên tra cứu hết hạn lúc {new Date(pv.hetHan).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })}.{' '}
            <button type="button" className="link" onClick={phienVangLai.xoa}>Tra cứu số khác</button></p>
        </div>
      </div>
      {!phien && !pv.coTaiKhoan && (q.data?.length ?? 0) > 0 && (
        <p className={f.info}><Link to="/dang-ky" className="link">Tạo tài khoản</Link> bằng số này để gom các đơn vào tài khoản, tích điểm và nhận ưu đãi thành viên mới.</p>
      )}
      {pv.coTaiKhoan && !phien && <p className={f.info}>Số này đã có tài khoản. <Link to="/dang-nhap?tiep=%2Ftai-khoan%2Fdon-hang" className="link">Đăng nhập</Link> để xem hóa đơn, đổi trả và điểm tích lũy.</p>}
      {q.error ? <ErrorState error={q.error} onRetry={() => q.refetch()} />
        : q.isLoading ? <LoadingRegion><div className={a.list}>{[0, 1].map((i) => <Skeleton key={i} h={88} />)}</div></LoadingRegion>
        : !q.data?.length ? <EmptyState title="Số điện thoại này chưa có đơn hàng nào" action={<ButtonLink to="/san-pham">Mua sắm ngay</ButtonLink>} />
        : (
          <ul className={a.list}>
            {q.data.map((d) => (
              <li key={d.maDon}>
                <Link to={`/tra-cuu-don/${d.maDon}`} className={a.row}>
                  <div className={a.rowTop}>
                    <span className={a.code}>{d.maDon}</span>
                    <Badge tone={TRANG_THAI_DON[d.trangThai]?.tone}>{TRANG_THAI_DON[d.trangThai]?.nhan ?? d.trangThai}</Badge>
                  </div>
                  <div className={a.rowMeta}>
                    <span>Ngày đặt {formatNgay(d.ngayDat)}</span>
                    <span>{d.chiTiets.reduce((t, c) => t + c.soLuong, 0)} sản phẩm</span>
                    <span>Tổng <strong className="num">{formatTien(d.tongTien)}</strong></span>
                    {d.nguon === 'TAI_KHOAN' && <span>Đặt bằng tài khoản</span>}
                    {d.yeuCauHuy === 'CHO_DUYET' && d.trangThai !== 'DA_HUY' && <span>Đang chờ duyệt yêu cầu hủy</span>}
                  </div>
                </Link>
              </li>
            ))}
          </ul>
        )}
    </div>
  );
}

export default function TraCuuDonPage() {
  useTitle('Tra cứu đơn hàng');
  return (
    <div className={`container ${s.page}`}>
      <Breadcrumb items={[{ label: 'Tra cứu đơn hàng' }]} />
      <CanXacThuc><DanhSach /></CanXacThuc>
    </div>
  );
}

export function TraCuuDonChiTietPage() {
  return (
    <div className={`container ${s.page}`}>
      <Breadcrumb items={[{ label: 'Tra cứu đơn hàng', to: '/tra-cuu-don' }, { label: 'Chi tiết' }]} />
      <CanXacThuc><div style={{ display: 'grid', gap: 'var(--s-5)', marginTop: 'var(--s-4)' }}><DonHangDetailPage cheDo="vang-lai" /></div></CanXacThuc>
    </div>
  );
}
