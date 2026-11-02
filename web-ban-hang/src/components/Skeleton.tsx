import s from './Skeleton.module.css';

export function Skeleton({ w = '100%', h = 16, ratio, className }: { w?: string | number; h?: string | number; ratio?: string; className?: string }) {
  return <span className={`${s.sk} ${className ?? ''}`} style={{ width: w, height: ratio ? undefined : h, aspectRatio: ratio }} aria-hidden />;
}

/** Khung cho - dung khi dang tai, giu dung kich thuoc noi dung that (khong giat layout) */
export function LoadingRegion({ label = 'Đang tải', children }: { label?: string; children: React.ReactNode }) {
  return (
    <div role="status" aria-live="polite" aria-busy>
      <span className="sr-only">{label}…</span>
      {children}
    </div>
  );
}
