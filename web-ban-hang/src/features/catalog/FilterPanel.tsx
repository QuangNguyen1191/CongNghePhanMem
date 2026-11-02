import type { LoaiSp } from '../../api/types';
import { KHOANG_GIA, type TieuChi } from './boLoc';
import { maMau } from './gomSanPham';
import s from './FilterPanel.module.css';

export type KhoaMang = 'loai' | 'mau' | 'size' | 'gia';

interface Props {
  tieuChi: TieuChi;
  giaTri: { mau: string[]; size: string[]; coGiamGia: boolean };
  /** chi hien nhom "Danh muc" o trang tat ca / tim kiem */
  loaiSps?: LoaiSp[];
  onToggle: (khoa: KhoaMang, v: string) => void;
  onCo: (khoa: 'conHang' | 'giamGia', v: boolean) => void;
}

function Nhom({ tieuDe, children }: { tieuDe: string; children: React.ReactNode }) {
  return (
    <fieldset className={s.group}>
      <legend className={`label ${s.legend}`}>{tieuDe}</legend>
      {children}
    </fieldset>
  );
}

export function FilterPanel({ tieuChi, giaTri, loaiSps, onToggle, onCo }: Props) {
  return (
    <div className={s.panel}>
      {loaiSps && loaiSps.length > 0 && (
        <Nhom tieuDe="Danh mục">
          <div className={s.checks}>
            {loaiSps.map((l) => (
              <label key={l.maLoaiSp} className={s.check}>
                <input type="checkbox" checked={tieuChi.loai.includes(l.maLoaiSp)} onChange={() => onToggle('loai', l.maLoaiSp)} />
                <span>{l.tenLoaiSp}</span>
              </label>
            ))}
          </div>
        </Nhom>
      )}

      {giaTri.mau.length > 0 && (
        <Nhom tieuDe="Màu sắc">
          <div className={s.colors}>
            {giaTri.mau.map((m) => {
              const chon = tieuChi.mau.includes(m);
              return (
                <button key={m} type="button" className={`${s.color} ${chon ? s.on : ''}`} aria-pressed={chon} onClick={() => onToggle('mau', m)}>
                  <span className={s.dot} style={{ background: maMau(m) }} aria-hidden />
                  <span>{m}</span>
                </button>
              );
            })}
          </div>
        </Nhom>
      )}

      {giaTri.size.length > 0 && (
        <Nhom tieuDe="Kích cỡ">
          <div className={s.sizes}>
            {giaTri.size.map((k) => {
              const chon = tieuChi.size.includes(k);
              return (
                <button key={k} type="button" className={`${s.size} ${chon ? s.on : ''}`} aria-pressed={chon} onClick={() => onToggle('size', k)}>
                  {k}
                </button>
              );
            })}
          </div>
        </Nhom>
      )}

      <Nhom tieuDe="Khoảng giá">
        <div className={s.checks}>
          {KHOANG_GIA.map((k) => (
            <label key={k.ma} className={s.check}>
              <input type="checkbox" checked={tieuChi.gia.includes(k.ma)} onChange={() => onToggle('gia', k.ma)} />
              <span className="num">{k.nhan}</span>
            </label>
          ))}
        </div>
      </Nhom>

      <Nhom tieuDe="Tình trạng">
        <div className={s.checks}>
          <label className={s.check}>
            <input type="checkbox" checked={tieuChi.conHang} onChange={(e) => onCo('conHang', e.target.checked)} />
            <span>Chỉ hiện còn hàng</span>
          </label>
          <label className={s.check}>
            <input type="checkbox" checked={tieuChi.giamGia} onChange={(e) => onCo('giamGia', e.target.checked)} disabled={!giaTri.coGiamGia && !tieuChi.giamGia} />
            <span>Đang giảm giá{!giaTri.coGiamGia && ' (hiện chưa có)'}</span>
          </label>
        </div>
      </Nhom>
    </div>
  );
}
