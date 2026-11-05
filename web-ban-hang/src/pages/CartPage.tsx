import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { ShoppingBag, X } from 'lucide-react';
import { Breadcrumb, Button, ButtonLink, ConfirmDialog, EmptyState, ErrorState, LoadingRegion, Price, ProductImage, QuantityStepper, Skeleton } from '../components';
import { useGioHang, useThaoTacGio, type DongGio } from '../features/cart/useGioHang';
import { DoiBienThe } from '../features/cart/DoiBienThe';
import { useSession } from '../features/auth/useSession';
import { nhanBienThe } from '../features/account/nhan';
import { quyTac } from '../config/cuaHang';
import { formatTien } from '../lib/format';
import { useTitle } from '../lib/useTitle';
import s from './CartPage.module.css';

function canhBao(d: DongGio) {
  if (!d.conBan) return 'Sản phẩm đã ngừng bán, vui lòng xóa khỏi giỏ.';
  if (d.tonKho <= 0) return 'Đã hết hàng, vui lòng xóa khỏi giỏ.';
  if (d.soLuong > d.tonKho) return `Chỉ còn ${d.tonKho} sản phẩm, vui lòng giảm số lượng.`;
  if (d.tonKho < quyTac.nguongTonThap) return null;
  return null;
}

function DongHangGio({ d }: { d: DongGio }) {
  const { capNhat, xoa } = useThaoTacGio();
  const [moDoi, setMoDoi] = useState(false);
  const dangDoi = capNhat.isPending || xoa.isPending;
  const loi = canhBao(d);
  return (
    <li className={s.line}>
      <Link to={`/san-pham/${d.maSp}`} aria-hidden tabIndex={-1}><ProductImage src={d.hinhAnh} alt="" sizes="120px" /></Link>
      <div className={s.body}>
        <div className={s.top}>
          <div>
            <Link to={`/san-pham/${d.maSp}`} className={s.name}>{d.tenSp}</Link>
            <p className={s.sub}>
              {nhanBienThe(d.mauSac, d.kichCo) || d.maSp}
              {d.conBan && <> · <button type="button" className={s.doi} onClick={() => setMoDoi(true)} disabled={dangDoi}>Đổi</button></>}
            </p>
          </div>
          <button type="button" className={s.remove} onClick={() => xoa.mutate(d.maSp)} disabled={dangDoi} aria-label={`Xóa ${d.tenSp} khỏi giỏ`}><X size={18} aria-hidden /></button>
        </div>
        {d.conBan && <Price goc={d.donGiaGoc} km={d.donGia < d.donGiaGoc ? d.donGia : null} />}
        {loi ? <p className={s.warn} role="alert">{loi}</p>
          : d.tonKho < quyTac.nguongTonThap && <p className="small" style={{ color: 'var(--c-warning)' }}>Chỉ còn {d.tonKho} sản phẩm</p>}
        <div className={s.bottom}>
          {d.conBan && d.tonKho > 0 ? (
            <QuantityStepper
              value={d.soLuong}
              onChange={(n) => capNhat.mutate({ maSp: d.maSp, soLuong: n })}
              min={1}
              max={Math.max(d.tonKho, d.soLuong > d.tonKho ? d.soLuong : 1)}
              disabled={dangDoi}
              label={`Số lượng ${d.tenSp}`}
            />
          ) : <span />}
          <span className={`${s.lineTotal} num`}>{d.conBan ? formatTien(d.thanhTien) : '—'}</span>
        </div>
      </div>
      {d.conBan && <DoiBienThe dong={d} open={moDoi} onClose={() => setMoDoi(false)} />}
    </li>
  );
}

export default function CartPage() {
  useTitle('Giỏ hàng');
  const gio = useGioHang();
  const phien = useSession();
  const navigate = useNavigate();
  const { xoaHet } = useThaoTacGio();
  const [hoiXoa, setHoiXoa] = useState(false);
  const tietKiem = gio.dongs.filter((d) => d.conBan).reduce((t, d) => t + (d.donGiaGoc - d.donGia) * d.soLuong, 0);

  const thanhToan = () => navigate('/thanh-toan');

  return (
    <div className={`container ${s.page}`}>
      <Breadcrumb items={[{ label: 'Trang chủ', to: '/' }, { label: 'Giỏ hàng' }]} />
      <div className={s.head}>
        <h1 className="h1">Giỏ hàng{gio.tongSoLuong > 0 && <span className="muted"> ({gio.tongSoLuong})</span>}</h1>
        {gio.dongs.length > 0 && <Button variant="link" onClick={() => setHoiXoa(true)}>Xóa tất cả</Button>}
      </div>

      {gio.error ? <ErrorState error={gio.error} onRetry={gio.refetch} />
        : gio.isLoading ? <LoadingRegion label="Đang tải giỏ hàng"><Skeleton h={140} /><Skeleton h={140} /></LoadingRegion>
        : gio.dongs.length === 0 ? (
          <EmptyState icon={<ShoppingBag size={32} strokeWidth={1.25} />} title="Giỏ hàng đang trống" description="Khám phá các sản phẩm mới và thêm vào giỏ nhé." action={<ButtonLink to="/san-pham">Tiếp tục mua sắm</ButtonLink>} />
        ) : (
          <div className={s.layout}>
            <ul className={s.lines} aria-label="Sản phẩm trong giỏ">
              {gio.dongs.map((d) => <DongHangGio key={d.maSp} d={d} />)}
            </ul>
            <aside className={s.summary} aria-label="Tóm tắt đơn hàng">
              <h2 className="h2">Tóm tắt</h2>
              <div className={s.rows}>
                <div><span>Tạm tính ({gio.tongSoLuong} sản phẩm)</span><span className="num">{formatTien(gio.tamTinh)}</span></div>
                {tietKiem > 0 && <div className={s.saving}><span>Đã tiết kiệm</span><span className="num">−{formatTien(tietKiem)}</span></div>}
                <div><span>Phí vận chuyển</span><span>Miễn phí</span></div>
              </div>
              <div className={s.total}><span>Tổng</span><span className="num">{formatTien(gio.tamTinh)}</span></div>
              <p className={s.note}>Khuyến mãi theo hóa đơn và mã giảm giá được tính ở bước thanh toán.</p>
              {gio.coLoi && <p className={s.warn} role="alert">Vui lòng xử lý các sản phẩm được đánh dấu trước khi thanh toán.</p>}
              {phien ? <Button size="lg" block onClick={thanhToan} disabled={gio.coLoi}>Thanh toán</Button> : (
                <>
                  {/* v3: 2 lua chon cho khach chua dang nhap */}
                  <Button size="lg" block onClick={thanhToan} disabled={gio.coLoi}>Mua không cần tài khoản</Button>
                  <Button size="lg" block variant="secondary" disabled={gio.coLoi} onClick={() => navigate(`/dang-nhap?tiep=${encodeURIComponent('/thanh-toan')}`)}>Đăng nhập để thanh toán</Button>
                  <p className={s.note}>Mua không cần tài khoản: chỉ cần xác thực số điện thoại bằng mã OTP. Đăng nhập để tích điểm; giỏ hàng được giữ lại và tự gộp vào tài khoản. Chưa có tài khoản? <Link to={`/dang-ky?tiep=${encodeURIComponent('/thanh-toan')}`} className="link">Đăng ký</Link> để nhận ưu đãi thành viên mới.</p>
                </>
              )}
              <Link to="/san-pham" className="link small" style={{ justifySelf: 'center' }}>Tiếp tục mua sắm</Link>
            </aside>
          </div>
        )}

      <ConfirmDialog
        open={hoiXoa}
        title="Xóa tất cả sản phẩm?"
        description="Toàn bộ sản phẩm trong giỏ sẽ bị xóa."
        confirmLabel="Xóa tất cả"
        danger
        loading={xoaHet.isPending}
        onConfirm={() => xoaHet.mutate(undefined, { onSettled: () => setHoiXoa(false) })}
        onClose={() => setHoiXoa(false)}
      />
    </div>
  );
}
