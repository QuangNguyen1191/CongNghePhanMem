import { Link } from 'react-router-dom';
import { ProductImage } from '../../components';
import { useSkuDangBan } from '../../features/catalog/useSanPham';
import { nhanBienThe } from '../../features/account/nhan';
import { formatTien } from '../../lib/format';
import s from './Account.module.css';

export interface Dong {
  maSp: string;
  tenSp?: string | null;
  kichCo?: string | null;
  mauSac?: string | null;
  soLuong: number;
  donGia: number;
  thanhTien: number;
}

/** Danh sach dong hang trong don / hoa don. Chi tiet don khong co anh -> lay anh tu danh sach SKU dang ban (neu con). */
export function DanhSachDong({ dongs }: { dongs: Dong[] }) {
  const { data } = useSkuDangBan();
  const anh = new Map((data ?? []).map((x) => [x.maSp, x.hinhAnh ?? null]));
  return (
    <ul className={s.items}>
      {dongs.map((d) => (
        <li key={d.maSp} className={s.item}>
          <ProductImage src={anh.get(d.maSp)} alt={d.tenSp ?? d.maSp} sizes="64px" />
          <div>
            <Link to={`/san-pham/${d.maSp}`} className={s.itemName}>{d.tenSp ?? d.maSp}</Link>
            <p className={s.itemSub}>{nhanBienThe(d.mauSac, d.kichCo) || d.maSp} · SL {d.soLuong} × <span className="num">{formatTien(d.donGia)}</span></p>
          </div>
          <span className="num">{formatTien(d.thanhTien)}</span>
        </li>
      ))}
    </ul>
  );
}

export function TongTien({ tamTinh, tienGiam, tongTien, ghiChu }: { tamTinh: number; tienGiam: number; tongTien: number; ghiChu?: string | null }) {
  return (
    <div className={s.sum}>
      <div><span>Tạm tính</span><span className="num">{formatTien(tamTinh)}</span></div>
      {tienGiam > 0 && <div className={s.discount}><span>Giảm giá{ghiChu ? ` (${ghiChu})` : ''}</span><span className="num">−{formatTien(tienGiam)}</span></div>}
      <div><span>Phí vận chuyển</span><span>Miễn phí</span></div>
      <div className={s.sumTotal}><span>Tổng cộng</span><span className="num">{formatTien(tongTien)}</span></div>
    </div>
  );
}
