import { Link } from 'react-router-dom';
import type { LoaiSp } from '../api/types';
import { useKhuyenMaiDangChay } from '../features/catalog/useCatalogMeta';
import { formatNgay } from '../lib/format';
import s from './MegaMenu.module.css';

interface Props { id: string; open: boolean; loaiSps: LoaiSp[]; onClose: () => void }

export function MegaMenu({ id, open, loaiSps, onClose }: Props) {
  const { data: kms } = useKhuyenMaiDangChay();
  return (
    <div id={id} className={s.panel} hidden={!open} onKeyDown={(e) => { if (e.key === 'Escape') onClose(); }}>
      <div className={`container ${s.grid}`}>
        <div>
          <p className={`label ${s.heading}`}>Danh mục</p>
          <ul className={s.list}>
            {loaiSps.map((l) => (
              <li key={l.maLoaiSp}><Link to={`/danh-muc/${l.maLoaiSp}`}>{l.tenLoaiSp}</Link></li>
            ))}
          </ul>
        </div>
        <div>
          <p className={`label ${s.heading}`}>Mua sắm</p>
          <ul className={s.list}>
            <li><Link to="/san-pham">Tất cả sản phẩm</Link></li>
            <li><Link to="/san-pham?giamGia=1">Đang giảm giá</Link></li>
            <li><Link to="/huong-dan-chon-size">Hướng dẫn chọn size</Link></li>
          </ul>
        </div>
        <div className={s.promo}>
          <p className={`label ${s.heading}`}>Ưu đãi đang diễn ra</p>
          {kms && kms.length > 0 ? (
            <ul className={s.kmList}>
              {kms.map((k) => (
                <li key={k.maKm}>
                  <p className={s.kmName}>{k.tenKm}</p>
                  <p className="small muted">Đến hết {formatNgay(k.ngayKt)}</p>
                </li>
              ))}
            </ul>
          ) : (
            <p className="small muted">Hiện chưa có chương trình nào.</p>
          )}
        </div>
      </div>
    </div>
  );
}
