import { memo } from 'react';
import { Link } from 'react-router-dom';
import { Badge, Price, ProductImage } from '../../components';
import { layAnh, useBoAnh } from './boAnh';
import { maMau, type MauSanPham } from './gomSanPham';
import s from './ProductCard.module.css';

const SO_CHAM_TOI_DA = 5;

function khoangSize(ks: string[]) {
  if (ks.length === 0) return null;
  return ks.length === 1 ? ks[0] : `${ks[0]} – ${ks[ks.length - 1]}`;
}

export const ProductCard = memo(function ProductCard({ sp, priority }: { sp: MauSanPham; priority?: boolean }) {
  const thua = sp.mauSacs.length - SO_CHAM_TOI_DA;
  const nhieuGia = sp.giaThapNhat !== sp.giaCaoNhat;
  const { data: boAnh } = useBoAnh();
  const anhPhu = layAnh(boAnh, sp.hinhAnh)[1];
  return (
    <article className={s.card}>
      <Link to={`/san-pham/${sp.maSpDaiDien}`} className={`${s.link} hover-swap`}>
        <div className={s.media}>
          <ProductImage src={sp.hinhAnh} alt={sp.tenSp} priority={priority} hoverSrc={anhPhu} />
          <div className={s.badges}>
            {sp.coGiamGia && <Badge tone="sale">-{sp.phanTramGiamMax}%</Badge>}
          </div>
          {!sp.conHang && <span className={s.soldOut}>Hết hàng</span>}
        </div>
        <div className={s.body}>
          {sp.mauSacs.length > 0 && sp.mauSacs[0].ten && (
            <div className={s.swatches}>
              <span className="sr-only">{sp.mauSacs.length} màu: {sp.mauSacs.map((m) => m.ten).join(', ')}</span>
              {sp.mauSacs.slice(0, SO_CHAM_TOI_DA).map((m) => (
                <span key={m.ten} className={s.dot} style={{ background: maMau(m.ten) }} title={m.ten} aria-hidden />
              ))}
              {thua > 0 && <span className={s.more} aria-hidden>+{thua}</span>}
            </div>
          )}
          <p className={s.meta}>
            <span>{sp.tenLoaiSp}</span>
            {khoangSize(sp.kichCos) && <span>{khoangSize(sp.kichCos)}</span>}
          </p>
          <h3 className={s.name}>{sp.tenSp}</h3>
          <Price goc={sp.giaThapNhat} km={sp.coGiamGia ? sp.giaBanThapNhat : null} tu={nhieuGia} />
        </div>
      </Link>
    </article>
  );
});
