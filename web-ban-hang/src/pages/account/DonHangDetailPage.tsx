import { useState } from 'react';
import { Link, useLocation, useParams } from 'react-router-dom';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { CheckCircle2, Download } from 'lucide-react';
import { donHangApi } from '../../api/khachApi';
import { ApiError } from '../../api/apiClient';
import { qk } from '../../api/queryKeys';
import { Badge, Button, ConfirmDialog, EmptyState, ErrorState, LoadingRegion, Skeleton } from '../../components';
import { useDonHang } from '../../features/account/useTaiKhoan';
import { BUOC_DON, PHUONG_THUC, TRANG_THAI_DON } from '../../features/account/nhan';
import { useTaiPdf } from '../../features/account/taiPdf';
import { formatNgay } from '../../lib/format';
import { useTitle } from '../../lib/useTitle';
import { DanhSachDong, TongTien } from './DongSanPham';
import f from '../Form.module.css';
import s from './Account.module.css';

export default function DonHangDetailPage() {
  const { ma = '' } = useParams();
  useTitle(`Đơn hàng ${ma}`);
  const moiDat = (useLocation().state as { moiDat?: boolean } | null)?.moiDat;
  const q = useDonHang(ma);
  const qc = useQueryClient();
  const [hoiHuy, setHoiHuy] = useState(false);
  const pdf = useTaiPdf(donHangApi.pdf, (m) => `DonHang_${m}.pdf`);
  const huy = useMutation({
    mutationFn: () => donHangApi.huy(ma),
    onSuccess: () => {
      toast.success('Đã hủy đơn hàng');
      setHoiHuy(false);
      qc.invalidateQueries({ queryKey: qk.donHang(ma) });
      qc.invalidateQueries({ queryKey: qk.donHangs });
    },
    onError: () => { setHoiHuy(false); qc.invalidateQueries({ queryKey: qk.donHang(ma) }); },
  });

  if (q.error) {
    return q.error instanceof ApiError && (q.error.khongTimThay || q.error.khongCoQuyen)
      ? <EmptyState title="Không tìm thấy đơn hàng" description="Đơn không tồn tại hoặc không thuộc tài khoản của bạn." action={<Link to="/tai-khoan/don-hang" className="link">Về danh sách đơn</Link>} />
      : <ErrorState error={q.error} onRetry={() => q.refetch()} />;
  }
  const d = q.data;
  if (!d) return <LoadingRegion><Skeleton h={40} w="50%" /><Skeleton h={300} /></LoadingRegion>;

  const tt = TRANG_THAI_DON[d.trangThai];
  const buoc = BUOC_DON.indexOf(d.trangThai);
  const tamTinh = d.chiTiets.reduce((t, c) => t + c.thanhTien, 0);

  return (
    <>
      {moiDat && (
        <p className={f.success} role="status"><CheckCircle2 size={16} aria-hidden style={{ verticalAlign: '-3px', marginRight: 6 }} />
          Đặt hàng thành công! Cửa hàng sẽ xác nhận đơn sớm. Bạn có thể hủy đơn khi đơn còn ở trạng thái chờ xác nhận.
        </p>
      )}
      <div className={s.titleRow}>
        <div>
          <p className="small muted"><Link to="/tai-khoan/don-hang" className="link">Đơn hàng</Link> / {d.maDon}</p>
          <h1 className="h1">Đơn hàng {d.maDon}</h1>
        </div>
        <Badge tone={tt?.tone}>{tt?.nhan ?? d.trangThai}</Badge>
      </div>

      {buoc >= 0 ? (
        <ol className={s.steps} aria-label="Tiến trình đơn hàng">
          {BUOC_DON.map((b, i) => (
            <li key={b} className={`${s.step} ${i <= buoc ? s.stepDone : ''}`} aria-current={i === buoc ? 'step' : undefined}>{TRANG_THAI_DON[b].nhan}</li>
          ))}
        </ol>
      ) : (
        <p className={f.info}>{d.trangThai === 'DA_HUY' ? 'Đơn hàng đã được hủy.' : 'Cửa hàng đã từ chối đơn hàng này. Vui lòng liên hệ cửa hàng nếu cần hỗ trợ.'}</p>
      )}

      <div className={s.grid2}>
        <section className={s.panel} aria-labelledby="giao-hang">
          <h2 id="giao-hang" className={s.panelTitle}>Giao hàng</h2>
          <dl className={s.kv}>
            <dt>Người nhận</dt><dd>{d.tenNguoiNhan || d.tenKhachHang || '—'}</dd>
            <dt>Điện thoại</dt><dd className="num">{d.sdtNhan}</dd>
            <dt>Địa chỉ</dt><dd>{d.diaChiGiao}</dd>
          </dl>
        </section>
        <section className={s.panel} aria-labelledby="thanh-toan">
          <h2 id="thanh-toan" className={s.panelTitle}>Thanh toán</h2>
          <dl className={s.kv}>
            <dt>Ngày đặt</dt><dd>{formatNgay(d.ngayDat)}</dd>
            <dt>Phương thức</dt><dd>{PHUONG_THUC[d.phuongThucTt] ?? d.phuongThucTt}</dd>
            {d.maVoucher && <><dt>Mã giảm giá</dt><dd>{d.maVoucher}</dd></>}
            {d.maHoaDon && <><dt>Hóa đơn</dt><dd><Link to={`/tai-khoan/hoa-don/${d.maHoaDon}`} className="link">{d.maHoaDon}</Link></dd></>}
          </dl>
        </section>
      </div>

      <section className={s.panel} aria-labelledby="san-pham">
        <h2 id="san-pham" className={s.panelTitle}>Sản phẩm ({d.chiTiets.reduce((t, c) => t + c.soLuong, 0)})</h2>
        <DanhSachDong dongs={d.chiTiets} />
        <TongTien tamTinh={tamTinh} tienGiam={d.tienGiam} tongTien={d.tongTien} ghiChu={d.maVoucher ? `mã ${d.maVoucher}` : null} />
      </section>

      <div className={f.actions}>
        <Button variant="secondary" iconLeft={<Download size={16} aria-hidden />} loading={pdf.isPending} onClick={() => pdf.mutate(d.maDon)}>Tải PDF</Button>
        {d.trangThai === 'CHO_XAC_NHAN' && <Button variant="ghost" onClick={() => setHoiHuy(true)}>Hủy đơn hàng</Button>}
      </div>

      <ConfirmDialog
        open={hoiHuy}
        title="Hủy đơn hàng?"
        description={`Đơn ${d.maDon} sẽ bị hủy và không thể khôi phục. Bạn có thể đặt lại đơn mới bất cứ lúc nào.`}
        confirmLabel="Hủy đơn"
        cancelLabel="Giữ đơn"
        danger
        loading={huy.isPending}
        onConfirm={() => huy.mutate()}
        onClose={() => setHoiHuy(false)}
      />
    </>
  );
}
