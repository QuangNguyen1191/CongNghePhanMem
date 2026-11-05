import { useState } from 'react';
import { Link, useLocation, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { CheckCircle2, Download } from 'lucide-react';
import { donHangApi } from '../../api/khachApi';
import { vangLaiApi } from '../../api/vangLaiApi';
import { ApiError } from '../../api/apiClient';
import { qk } from '../../api/queryKeys';
import { Badge, Button, EmptyState, ErrorState, LoadingRegion, Skeleton } from '../../components';
import { HuyDonDialog } from '../../features/account/HuyDonDialog';
import { cuaHang } from '../../config/cuaHang';
import { useDonHang } from '../../features/account/useTaiKhoan';
import { BUOC_DON, PHUONG_THUC, TRANG_THAI_DON } from '../../features/account/nhan';
import { useTaiPdf } from '../../features/account/taiPdf';
import { formatNgay, formatTien } from '../../lib/format';
import { useTitle } from '../../lib/useTitle';
import { usePhienVangLai } from '../../lib/phienVangLai';
import { useUuDaiTvm } from '../../features/vangLai/useUuDai';
import { DanhSachDong, TongTien } from './DongSanPham';
import f from '../Form.module.css';
import s from './Account.module.css';

/** Chi tiet don. cheDo 'vang-lai' (v3): khach tra cuu bang SDT da xac thuc OTP (trang /tra-cuu-don/:ma), khong co link hoa don. */
export default function DonHangDetailPage({ cheDo = 'tai-khoan' }: { cheDo?: 'tai-khoan' | 'vang-lai' }) {
  const { ma = '' } = useParams();
  useTitle(`Đơn hàng ${ma}`);
  const moiDat = (useLocation().state as { moiDat?: boolean } | null)?.moiDat;
  const vl = cheDo === 'vang-lai';
  const pv = usePhienVangLai();
  const sdt = pv?.sdt ?? '';
  const qTk = useDonHang(ma, !vl);
  const qVl = useQuery({ queryKey: qk.vangLaiDon(sdt, ma), queryFn: () => vangLaiApi.don(ma), enabled: vl && !!pv, retry: false });
  const q = vl ? qVl : qTk;
  const dsUrl = vl ? '/tra-cuu-don' : '/tai-khoan/don-hang';
  const uuDaiMa = useUuDaiTvm().data?.maKm;
  const qc = useQueryClient();
  const [hoiHuy, setHoiHuy] = useState(false);
  const pdf = useTaiPdf(vl ? vangLaiApi.pdf : donHangApi.pdf, (m) => `DonHang_${m}.pdf`);
  const lamMoi = () => {
    if (vl) { qc.invalidateQueries({ queryKey: ['vang-lai', sdt] }); return; }
    qc.invalidateQueries({ queryKey: qk.donHang(ma) });
    qc.invalidateQueries({ queryKey: qk.donHangs });
  };
  // v3: cho xac nhan -> huy ngay; da xac nhan -> gui yeu cau cho cua hang duyet (cung 1 API)
  const huy = useMutation({
    mutationFn: (lyDo: string) => (vl ? vangLaiApi.yeuCauHuy(ma, lyDo) : donHangApi.yeuCauHuy(ma, lyDo)),
    onSuccess: (don) => {
      toast.success(don.trangThai === 'DA_HUY' ? 'Đã hủy đơn hàng' : 'Đã gửi yêu cầu hủy, cửa hàng sẽ phản hồi sớm');
      setHoiHuy(false); lamMoi();
    },
    onError: () => { setHoiHuy(false); lamMoi(); },
  });

  if (q.error) {
    return q.error instanceof ApiError && (q.error.khongTimThay || q.error.khongCoQuyen)
      ? <EmptyState title="Không tìm thấy đơn hàng" description={vl ? 'Đơn không tồn tại hoặc không thuộc số điện thoại đã xác thực.' : 'Đơn không tồn tại hoặc không thuộc tài khoản của bạn.'} action={<Link to={dsUrl} className="link">Về danh sách đơn</Link>} />
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
          <p className="small muted"><Link to={dsUrl} className="link">{vl ? 'Tra cứu đơn' : 'Đơn hàng'}</Link> / {d.maDon}</p>
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
      {d.trangThai === 'DA_HUY' && (d.lyDoHuy || d.nguoiHuy) && (
        <p className="small muted">
          {d.yeuCauHuy === 'DA_DUYET' ? 'Cửa hàng đã đồng ý yêu cầu hủy của bạn' : d.nguoiHuy === 'KHACH' ? 'Bạn đã hủy đơn' : 'Cửa hàng đã hủy đơn'}
          {d.ngayHuy ? ` lúc ${formatNgay(d.ngayHuy)}` : ''}{d.lyDoHuy ? `. Lý do: ${d.lyDoHuy}` : ''}
        </p>
      )}
      {d.trangThaiHoanTien && d.soTienHoan != null && (
        <p className={d.trangThaiHoanTien === 'DA_HOAN' ? f.success : f.info} role="status">
          {d.trangThaiHoanTien === 'DA_HOAN'
            ? `Cửa hàng đã hoàn ${formatTien(d.soTienHoan)} cho đơn này.`
            : `Cửa hàng sẽ hoàn ${formatTien(d.soTienHoan)} qua ${PHUONG_THUC[d.phuongThucTt] ?? 'phương thức bạn đã thanh toán'}. Cần hỗ trợ: ${cuaHang.hotline}.`}
        </p>
      )}
      {d.yeuCauHuy === 'CHO_DUYET' && d.trangThai !== 'DA_HUY' && (
        <p className={f.info} role="status">
          Bạn đã gửi yêu cầu hủy{d.ngayYeuCauHuy ? ` lúc ${formatNgay(d.ngayYeuCauHuy)}` : ''} (lý do: {d.lyDoHuy}). Cửa hàng đang xem xét và sẽ phản hồi sớm.
        </p>
      )}
      {d.yeuCauHuy === 'TU_CHOI' && d.trangThai !== 'DA_HUY' && (
        <p className={f.alert} role="status">Cửa hàng không thể hủy đơn này: {d.phanHoiHuy}. Cần hỗ trợ, vui lòng gọi {cuaHang.hotline}.</p>
      )}
      {d.trangThai === 'DANG_GIAO' && (
        <p className="small muted">Đơn đang được giao nên không hủy trực tuyến được. Cần hỗ trợ, vui lòng gọi {cuaHang.hotline}.</p>
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
            {d.maHoaDon && <><dt>Hóa đơn</dt><dd>{vl ? d.maHoaDon : <Link to={`/tai-khoan/hoa-don/${d.maHoaDon}`} className="link">{d.maHoaDon}</Link>}</dd></>}
          </dl>
        </section>
      </div>

      <section className={s.panel} aria-labelledby="san-pham">
        <h2 id="san-pham" className={s.panelTitle}>Sản phẩm ({d.chiTiets.reduce((t, c) => t + c.soLuong, 0)})</h2>
        <DanhSachDong dongs={d.chiTiets} />
        <TongTien tamTinh={tamTinh} tienGiam={d.tienGiam} tongTien={d.tongTien} ghiChu={d.maVoucher ? `mã ${d.maVoucher}` : d.maKm && d.maKm === uuDaiMa ? 'ưu đãi thành viên mới' : null} />
      </section>

      <div className={f.actions}>
        <Button variant="secondary" iconLeft={<Download size={16} aria-hidden />} loading={pdf.isPending} onClick={() => pdf.mutate(d.maDon)}>Tải PDF</Button>
        {d.trangThai === 'CHO_XAC_NHAN' && <Button variant="ghost" onClick={() => setHoiHuy(true)}>Hủy đơn hàng</Button>}
        {d.trangThai === 'DA_XAC_NHAN' && !d.yeuCauHuy && <Button variant="ghost" onClick={() => setHoiHuy(true)}>Yêu cầu hủy đơn</Button>}
      </div>

      <HuyDonDialog
        open={hoiHuy}
        maDon={d.maDon}
        canDuyet={d.trangThai === 'DA_XAC_NHAN'}
        loading={huy.isPending}
        onConfirm={(lyDo) => huy.mutate(lyDo)}
        onClose={() => setHoiHuy(false)}
      />
    </>
  );
}
