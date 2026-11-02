import { useState } from 'react';
import s from './ProductImage.module.css';

interface Props {
  src?: string | null;
  alt: string;
  /** anh dau trang (LCP) thi tai ngay, con lai lazy */
  priority?: boolean;
  sizes?: string;
  /** anh thu 2, hien khi re chuot vao the cha co class .hover-swap (chi tren thiet bi co chuot) */
  hoverSrc?: string | null;
}

/** Anh SP ti le 3:4, giu cho truoc (khong giat layout). Thieu / loi anh thi hien khung trung tinh kem ten. */
export function ProductImage({ src, alt, priority, hoverSrc, sizes = '(min-width:1024px) 25vw, (min-width:640px) 33vw, 50vw' }: Props) {
  const [loi, setLoi] = useState(false);
  return (
    <div className={s.frame}>
      {src && !loi ? (
        <img
          src={src}
          alt={alt}
          sizes={sizes}
          loading={priority ? 'eager' : 'lazy'}
          decoding="async"
          // React 18 chua nhan fetchPriority (camelCase) -> dung ten thuoc tinh HTML
          {...(priority ? { fetchpriority: 'high' } : {})}
          onError={() => setLoi(true)}
          className={s.img}
        />
      ) : (
        <div className={s.fallback} role="img" aria-label={alt}>
          <span className={s.mark} aria-hidden>UNIQLO</span>
          <span className={s.name} aria-hidden>{alt}</span>
        </div>
      )}
      {hoverSrc && src && !loi && (
        <img src={hoverSrc} alt="" aria-hidden loading="lazy" decoding="async" className={`${s.img} ${s.hover}`} />
      )}
    </div>
  );
}
