import { formatTien } from '../lib/format';
import s from './Price.module.css';

interface Props {
  /** don gia goc */
  goc: number;
  /** gia sau khuyen mai (neu co) */
  km?: number | null;
  size?: 'sm' | 'lg';
  /** "Từ" khi mau co nhieu muc gia */
  tu?: boolean;
}

export function Price({ goc, km, size = 'sm', tu }: Props) {
  const coKm = km != null && km < goc;
  return (
    <p className={`${s.price} ${s[size]} num`}>
      {tu && <span className={s.tu}>Từ </span>}
      {coKm ? (
        <>
          <span className={s.km}>
            <span className="sr-only">Giá khuyến mãi </span>{formatTien(km)}
          </span>
          <del className={s.goc}>
            <span className="sr-only">Giá gốc </span>{formatTien(goc)}
          </del>
        </>
      ) : (
        <span>{formatTien(goc)}</span>
      )}
    </p>
  );
}
