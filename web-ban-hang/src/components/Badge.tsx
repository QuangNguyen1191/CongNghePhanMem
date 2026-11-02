import type { ReactNode } from 'react';
import s from './Badge.module.css';

type Tone = 'neutral' | 'sale' | 'success' | 'warning' | 'dark';

export function Badge({ tone = 'neutral', children }: { tone?: Tone; children: ReactNode }) {
  return <span className={`${s.badge} ${s[tone]}`}>{children}</span>;
}
