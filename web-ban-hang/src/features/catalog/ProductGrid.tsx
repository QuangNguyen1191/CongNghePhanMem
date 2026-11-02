import { LoadingRegion, Skeleton } from '../../components';
import type { MauSanPham } from './gomSanPham';
import { ProductCard } from './ProductCard';
import s from './ProductGrid.module.css';

export function ProductGrid({ items, cols = 4 }: { items: MauSanPham[]; cols?: 3 | 4 }) {
  return (
    <ul className={`${s.grid} ${cols === 3 ? s.c3 : ''}`}>
      {items.map((m, i) => <li key={m.key}><ProductCard sp={m} priority={i < 4} /></li>)}
    </ul>
  );
}

export function ProductGridSkeleton({ count = 8, cols = 4 }: { count?: number; cols?: 3 | 4 }) {
  return (
    <LoadingRegion label="Đang tải sản phẩm">
      <ul className={`${s.grid} ${cols === 3 ? s.c3 : ''}`}>
        {Array.from({ length: count }, (_, i) => (
          <li key={i} className={s.sk}>
            <Skeleton ratio="3 / 4" />
            <Skeleton h={12} w="40%" />
            <Skeleton h={16} w="85%" />
            <Skeleton h={16} w="35%" />
          </li>
        ))}
      </ul>
    </LoadingRegion>
  );
}
