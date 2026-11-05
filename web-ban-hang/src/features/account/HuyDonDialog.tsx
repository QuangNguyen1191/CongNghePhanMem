// v3: Hop thoai huy / xin huy don: chon ly do (bat buoc), "Ly do khac" thi nhap them.
import { useEffect, useState } from 'react';
import { ConfirmDialog, TextAreaField } from '../../components';

const LY_DO = ['Đổi ý, không muốn mua nữa', 'Đặt nhầm sản phẩm / size / màu', 'Muốn đổi địa chỉ hoặc số điện thoại nhận hàng', 'Tìm được giá tốt hơn', 'Thời gian giao hàng quá lâu'];
const KHAC = 'Lý do khác';

interface Props {
  open: boolean;
  maDon: string;
  /** true: don da xac nhan -> chi gui yeu cau, cua hang duyet */
  canDuyet: boolean;
  loading?: boolean;
  onConfirm: (lyDo: string) => void;
  onClose: () => void;
}

export function HuyDonDialog({ open, maDon, canDuyet, loading, onConfirm, onClose }: Props) {
  const [chon, setChon] = useState('');
  const [khac, setKhac] = useState('');
  const [loi, setLoi] = useState('');
  useEffect(() => { if (open) { setChon(''); setKhac(''); setLoi(''); } }, [open]);

  const xacNhan = () => {
    const lyDo = chon === KHAC ? khac.trim() : chon;
    if (!lyDo) { setLoi(chon === KHAC ? 'Vui lòng nhập lý do' : 'Vui lòng chọn lý do hủy'); return; }
    onConfirm(lyDo.slice(0, 255));
  };

  return (
    <ConfirmDialog
      open={open}
      title={canDuyet ? 'Gửi yêu cầu hủy đơn?' : 'Hủy đơn hàng?'}
      description={
        <div style={{ display: 'grid', gap: 'var(--s-3)' }}>
          <p>{canDuyet
            ? `Đơn ${maDon} đã được cửa hàng xác nhận. Cửa hàng sẽ xem xét và phản hồi; nếu hàng chưa giao, đơn sẽ được hủy và hoàn tiền (nếu bạn đã thanh toán).`
            : `Đơn ${maDon} sẽ bị hủy ngay và không thể khôi phục.`}</p>
          <fieldset style={{ display: 'grid', gap: 'var(--s-2)', border: 0, padding: 0, margin: 0 }}>
            <legend className="small" style={{ fontWeight: 500, marginBottom: 'var(--s-2)' }}>Lý do hủy</legend>
            {[...LY_DO, KHAC].map((l) => (
              <label key={l} style={{ display: 'flex', gap: 'var(--s-2)', alignItems: 'center', minHeight: 32 }}>
                <input type="radio" name="lyDoHuy" value={l} checked={chon === l} onChange={() => { setChon(l); setLoi(''); }} />
                <span>{l}</span>
              </label>
            ))}
          </fieldset>
          {chon === KHAC && (
            <TextAreaField label="Lý do của bạn" name="lyDoKhac" rows={2} maxLength={255} value={khac}
              onChange={(e) => { setKhac(e.target.value); setLoi(''); }} />
          )}
          {loi && <p role="alert" className="small" style={{ color: 'var(--c-accent)' }}>{loi}</p>}
        </div>
      }
      confirmLabel={canDuyet ? 'Gửi yêu cầu' : 'Hủy đơn'}
      cancelLabel="Giữ đơn"
      danger
      loading={loading}
      onConfirm={xacNhan}
      onClose={onClose}
    />
  );
}
