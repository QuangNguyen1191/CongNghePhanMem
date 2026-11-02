import { Link } from 'react-router-dom';
import { ChevronRight } from 'lucide-react';
import s from './Breadcrumb.module.css';

export interface MucBreadcrumb { label: string; to?: string }

export function Breadcrumb({ items }: { items: MucBreadcrumb[] }) {
  return (
    <nav aria-label="Đường dẫn" className={s.nav}>
      <ol className={s.list}>
        {items.map((m, i) => {
          const cuoi = i === items.length - 1;
          return (
            <li key={`${m.label}-${i}`} className={s.item}>
              {m.to && !cuoi ? <Link to={m.to} className={s.link}>{m.label}</Link> : <span aria-current={cuoi ? 'page' : undefined}>{m.label}</span>}
              {!cuoi && <ChevronRight size={12} aria-hidden className={s.sep} />}
            </li>
          );
        })}
      </ol>
    </nav>
  );
}
