import { useEffect, useRef, type ReactNode } from 'react';
import { X } from 'lucide-react';
import { Button } from './Button';
import s from './Overlay.module.css';

/** Dung <dialog> goc cua trinh duyet: tu bay focus, Esc de dong, khoa cuon nen. */
function useDialog(open: boolean, onClose: () => void) {
  const ref = useRef<HTMLDialogElement>(null);
  useEffect(() => {
    const d = ref.current;
    if (!d) return;
    if (open && !d.open) {
      d.showModal();
      document.body.style.overflow = 'hidden';
    } else if (!open && d.open) {
      d.close();
    }
    return () => { document.body.style.overflow = ''; };
  }, [open]);
  useEffect(() => {
    const d = ref.current;
    if (!d) return;
    const dong = () => { document.body.style.overflow = ''; onClose(); };
    d.addEventListener('close', dong);
    return () => d.removeEventListener('close', dong);
  }, [onClose]);
  // bam ra ngoai (vung nen) thi dong
  const onClick = (e: React.MouseEvent<HTMLDialogElement>) => { if (e.target === e.currentTarget) ref.current?.close(); };
  return { ref, onClick };
}

interface DrawerProps {
  open: boolean;
  onClose: () => void;
  title: string;
  side?: 'left' | 'right';
  children: ReactNode;
  footer?: ReactNode;
}

export function Drawer({ open, onClose, title, side = 'right', children, footer }: DrawerProps) {
  const { ref, onClick } = useDialog(open, onClose);
  return (
    <dialog ref={ref} className={`${s.drawer} ${s[side]}`} onClick={onClick} aria-label={title}>
      <div className={s.panel}>
        <header className={s.head}>
          <h2 className="h2">{title}</h2>
          <button type="button" className={s.close} onClick={() => ref.current?.close()} aria-label="Đóng">
            <X size={20} aria-hidden />
          </button>
        </header>
        <div className={s.body}>{children}</div>
        {footer && <footer className={s.foot}>{footer}</footer>}
      </div>
    </dialog>
  );
}

interface ConfirmProps {
  open: boolean;
  title: string;
  description?: ReactNode;
  confirmLabel?: string;
  cancelLabel?: string;
  danger?: boolean;
  loading?: boolean;
  onConfirm: () => void;
  onClose: () => void;
}

/** Xac nhan truoc hanh dong quan trong (xoa khoi gio, huy don) */
export function ConfirmDialog({ open, title, description, confirmLabel = 'Đồng ý', cancelLabel = 'Quay lại', danger, loading, onConfirm, onClose }: ConfirmProps) {
  const { ref, onClick } = useDialog(open, onClose);
  return (
    <dialog ref={ref} className={s.confirm} onClick={onClick} aria-labelledby="xn-tieu-de">
      <div className={s.confirmBox}>
        <h2 id="xn-tieu-de" className="h2">{title}</h2>
        {description && <div className={s.confirmDesc}>{description}</div>}
        <div className={s.confirmActions}>
          <Button variant="secondary" onClick={() => ref.current?.close()} disabled={loading}>{cancelLabel}</Button>
          <Button variant="primary" className={danger ? s.danger : undefined} onClick={onConfirm} loading={loading} autoFocus>
            {confirmLabel}
          </Button>
        </div>
      </div>
    </dialog>
  );
}
