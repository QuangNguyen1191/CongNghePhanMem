import type { ReactNode } from 'react';
import { AlertCircle, PackageOpen } from 'lucide-react';
import { Button } from './Button';
import s from './StateBlock.module.css';

interface Props {
  title: string;
  description?: ReactNode;
  action?: ReactNode;
  icon?: ReactNode;
  /** trang chi co khoi trang thai (404...) thi tieu de la h1 */
  as?: 'p' | 'h1' | 'h2';
}

export function EmptyState({ title, description, action, icon, as: Tag = 'p' }: Props) {
  return (
    <div className={s.block}>
      <span className={s.icon} aria-hidden>{icon ?? <PackageOpen size={32} strokeWidth={1.25} />}</span>
      <Tag className={s.title}>{title}</Tag>
      {description && <p className={s.desc}>{description}</p>}
      {action && <div className={s.action}>{action}</div>}
    </div>
  );
}

export function ErrorState({ title = 'Không tải được dữ liệu', error, onRetry }: { title?: string; error?: unknown; onRetry?: () => void }) {
  const thongBao = error instanceof Error ? error.message : undefined;
  return (
    <div className={s.block} role="alert">
      <span className={`${s.icon} ${s.err}`} aria-hidden><AlertCircle size={32} strokeWidth={1.25} /></span>
      <p className={s.title}>{title}</p>
      {thongBao && <p className={s.desc}>{thongBao}</p>}
      {onRetry && <div className={s.action}><Button variant="secondary" onClick={onRetry}>Thử lại</Button></div>}
    </div>
  );
}
