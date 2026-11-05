import { useState } from 'react';
import { Link } from 'react-router-dom';
import type { TrangThaiDon } from '../../api/types';
import { Badge, ButtonLink, EmptyState, ErrorState, LoadingRegion, Skeleton } from '../../components';
import { useDonHangs } from '../../features/account/useTaiKhoan';
import { TRANG_THAI_DON } from '../../features/account/nhan';
import { formatNgay, formatTien } from '../../lib/format';
import { useTitle } from '../../lib/useTitle';
import s from './Account.module.css';

const LOC: { nhan: string; ds: TrangThaiDon[] | null }[] = [
  { nhan: 'Tất cả', ds: null },
  { nhan: 'Chờ xác nhận', ds: ['CHO_XAC_NHAN'] },
  { nhan: 'Đang xử lý', ds: ['DA_XAC_NHAN', 'DANG_GIAO'] },
  { nhan: 'Hoàn tất', ds: ['HOAN_TAT'] },
  { nhan: 'Đã hủy', ds: ['DA_HUY', 'TU_CHOI'] },
];

export default function DonHangListPage() {
  useTitle('Đơn hàng của tôi');
  const q = useDonHangs();
  const [loc, setLoc] = useState(0);
  const ds = (q.data ?? []).filter((d) => !LOC[loc].ds || LOC[loc].ds!.includes(d.trangThai));

  return (
    <>
      <h1 className="h1">Đơn hàng của tôi</h1>
      <div className={s.tabs} role="group" aria-label="Lọc theo trạng thái">
        {LOC.map((l, i) => (
          <button key={l.nhan} type="button" className={`${s.tab} ${i === loc ? s.tabOn : ''}`} aria-pressed={i === loc} onClick={() => setLoc(i)}>
            {l.nhan}{q.data && ` (${q.data.filter((d) => !l.ds || l.ds.includes(d.trangThai)).length})`}
          </button>
        ))}
      </div>
      {q.error ? <ErrorState error={q.error} onRetry={() => q.refetch()} />
        : q.isLoading ? <LoadingRegion><div className={s.list}>{[0, 1, 2].map((i) => <Skeleton key={i} h={88} />)}</div></LoadingRegion>
        : ds.length === 0 ? (
          <EmptyState
            title={q.data?.length ? 'Không có đơn hàng ở trạng thái này' : 'Bạn chưa có đơn hàng nào'}
            action={!q.data?.length && <ButtonLink to="/san-pham">Mua sắm ngay</ButtonLink>}
          />
        ) : (
          <ul className={s.list}>
            {ds.map((d) => (
              <li key={d.maDon}>
                <Link to={`/tai-khoan/don-hang/${d.maDon}`} className={s.row}>
                  <div className={s.rowTop}>
                    <span className={s.code}>{d.maDon}</span>
                    <Badge tone={TRANG_THAI_DON[d.trangThai]?.tone}>{TRANG_THAI_DON[d.trangThai]?.nhan ?? d.trangThai}</Badge>
                  </div>
                  <div className={s.rowMeta}>
                    <span>Ngày đặt {formatNgay(d.ngayDat)}</span>
                    <span>Tổng <strong className="num">{formatTien(d.tongTien)}</strong></span>
                    {d.yeuCauHuy === 'CHO_DUYET' && d.trangThai !== 'DA_HUY' && <span>Đang chờ duyệt yêu cầu hủy</span>}
                  </div>
                </Link>
              </li>
            ))}
          </ul>
        )}
    </>
  );
}
