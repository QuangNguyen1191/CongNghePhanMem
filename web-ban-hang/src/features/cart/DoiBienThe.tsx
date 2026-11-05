import { useEffect, useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { sanPhamApi } from '../../api/catalogApi';
import { qk } from '../../api/queryKeys';
import { Button, Drawer, ErrorState, Skeleton } from '../../components';
import { soSanhSize } from '../catalog/gomSanPham';
import { useThaoTacGio, type DongGio } from './useGioHang';
import s from './DoiBienThe.module.css';

/** Ngan ben phai de doi mau / size cua 1 dong trong gio (giu nguyen so luong). */
export function DoiBienThe({ dong, open, onClose }: { dong: DongGio; open: boolean; onClose: () => void }) {
  const btQ = useQuery({ queryKey: qk.bienThe(dong.maSp), queryFn: () => sanPhamApi.layBienThe(dong.maSp), enabled: open });
  const { doiBienThe } = useThaoTacGio();
  const [mau, setMau] = useState(dong.mauSac ?? '');
  const [size, setSize] = useState(dong.kichCo ?? '');
  useEffect(() => { if (open) { setMau(dong.mauSac ?? ''); setSize(dong.kichCo ?? ''); } }, [open, dong.mauSac, dong.kichCo]);

  const bienThes = useMemo(() => (btQ.data ?? []).filter((b) => b.trangThai === 'DANG_BAN'), [btQ.data]);
  const mauSacs = [...new Set(bienThes.map((b) => b.mauSac ?? ''))];
  const kichCos = [...new Set(bienThes.map((b) => b.kichCo ?? '').filter(Boolean))].sort(soSanhSize);
  const tim = (m: string, k: string) => bienThes.find((b) => (b.mauSac ?? '') === m && (b.kichCo ?? '') === k);
  const chon = tim(mau, size);
  const giongCu = chon?.maSp === dong.maSp;
  const duHang = !!chon && chon.soLuong >= dong.soLuong;

  const xacNhan = () => {
    if (!chon || giongCu) { onClose(); return; }
    doiBienThe.mutate({ maSpCu: dong.maSp, skuMoi: chon, soLuong: dong.soLuong }, { onSuccess: onClose });
  };

  return (
    <Drawer
      open={open}
      onClose={onClose}
      title="Đổi màu / kích cỡ"
      footer={
        <Button block onClick={xacNhan} loading={doiBienThe.isPending} disabled={!chon || (!giongCu && !duHang)}>
          {giongCu ? 'Giữ nguyên' : 'Cập nhật giỏ hàng'}
        </Button>
      }
    >
      <p className={s.ten}>{dong.tenSp}</p>
      {btQ.error ? <ErrorState error={btQ.error} onRetry={() => btQ.refetch()} />
        : btQ.isLoading ? <Skeleton h={160} />
        : (
          <div className={s.form}>
            {mauSacs.some(Boolean) && (
              <fieldset className={s.group}>
                <legend className="label">Màu sắc</legend>
                <div className={s.opts}>
                  {mauSacs.map((m) => (
                    <label key={m} className={`${s.opt} ${m === mau ? s.on : ''}`}>
                      <input type="radio" name="doi-mau" className="sr-only" checked={m === mau}
                        onChange={() => { setMau(m); if (!((tim(m, size)?.soLuong ?? 0) > 0)) setSize(''); }} />
                      {m}
                    </label>
                  ))}
                </div>
              </fieldset>
            )}
            {kichCos.length > 0 && (
              <fieldset className={s.group}>
                <legend className="label">Kích cỡ</legend>
                <div className={s.opts}>
                  {kichCos.map((k) => {
                    const b = tim(mau, k);
                    const het = !b || b.soLuong <= 0;
                    return (
                      <label key={k} className={`${s.opt} ${k === size ? s.on : ''} ${het ? s.het : ''}`}>
                        <input type="radio" name="doi-size" className="sr-only" checked={k === size} disabled={het} onChange={() => setSize(k)} />
                        {k}{het && <span className="sr-only"> (hết hàng)</span>}
                      </label>
                    );
                  })}
                </div>
              </fieldset>
            )}
            <p className="small" role="status" aria-live="polite">
              {!chon ? 'Chọn kích cỡ còn hàng.'
                : giongCu ? 'Đây là phân loại đang có trong giỏ.'
                : !duHang ? <span className={s.loi}>Chỉ còn {chon.soLuong} sản phẩm, ít hơn số lượng trong giỏ ({dong.soLuong}).</span>
                : `Còn ${chon.soLuong} sản phẩm. Số lượng giữ nguyên: ${dong.soLuong}.`}
            </p>
          </div>
        )}
    </Drawer>
  );
}
