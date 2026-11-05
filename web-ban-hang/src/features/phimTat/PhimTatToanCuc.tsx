import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { InfoDialog } from '../../components';
import { DANH_SACH_PHIM_TAT, coHopThoaiMo, dangNhapLieu, phimTron, type PhimTat } from './phimTat';
import s from './PhimTat.module.css';

/** Nut "Phim tat" o footer phat su kien nay de mo bang */
export const SU_KIEN_MO_BANG = 'wbh:mo-phim-tat';

const DI_TOI: Record<string, string> = {
  h: '/', s: '/san-pham', c: '/gio-hang', t: '/tai-khoan', d: '/tai-khoan/don-hang',
};

/** O tim kiem dang hien tren man hinh (desktop hoac mobile) */
function oTimKiem(): HTMLInputElement | null {
  const ds = document.querySelectorAll<HTMLInputElement>('form[role="search"] input[type="search"]');
  return [...ds].find((o) => o.offsetParent !== null) ?? null;
}

/** Phim tat dung o moi trang: '/', '?', 'g' + chu. Dat 1 lan trong MainLayout. */
export function PhimTatToanCuc() {
  const navigate = useNavigate();
  const [moBang, setMoBang] = useState(false);
  const choG = useRef<number | null>(null);

  useEffect(() => {
    const huyG = () => { if (choG.current) { window.clearTimeout(choG.current); choG.current = null; } };
    const nghe = (e: KeyboardEvent) => {
      if (e.defaultPrevented || !phimTron(e) || dangNhapLieu(e) || coHopThoaiMo()) return;
      if (choG.current) {
        const den = DI_TOI[e.key.toLowerCase()];
        huyG();
        if (den) { e.preventDefault(); navigate(den); }
        return;
      }
      if (e.key === '/') {
        const o = oTimKiem();
        if (o) { e.preventDefault(); o.focus(); o.select(); }
      } else if (e.key === '?') {
        e.preventDefault();
        setMoBang(true);
      } else if (e.key === 'g' && !e.shiftKey) {
        choG.current = window.setTimeout(huyG, 1500); // 'g' roi 1 chu trong 1,5 giay
      }
    };
    const moTuNut = () => setMoBang(true);
    window.addEventListener('keydown', nghe);
    window.addEventListener(SU_KIEN_MO_BANG, moTuNut);
    return () => { window.removeEventListener('keydown', nghe); window.removeEventListener(SU_KIEN_MO_BANG, moTuNut); huyG(); };
  }, [navigate]);

  return <BangPhimTat open={moBang} onClose={() => setMoBang(false)} />;
}

function Phim({ p }: { p: PhimTat }) {
  const lienTiep = p.phim.length > 1 && p.phim[0] !== 'Ctrl';
  return (
    <span className={s.phim}>
      {p.phim.map((k, i) => (
        <span key={k}>
          {i > 0 && <span className={s.noi}>{lienTiep ? (p.phim[0] === '←' ? '/' : 'rồi') : '+'}</span>}
          <kbd className={s.kbd}>{k}</kbd>
        </span>
      ))}
    </span>
  );
}

export function BangPhimTat({ open, onClose }: { open: boolean; onClose: () => void }) {
  const nhoms = [...new Set(DANH_SACH_PHIM_TAT.map((p) => p.nhom))];
  return (
    <InfoDialog open={open} onClose={onClose} title="Phím tắt">
      <div className={s.bang}>
        {nhoms.map((n) => (
          <section key={n}>
            <h3 className={`label muted ${s.nhom}`}>{n}</h3>
            <dl className={s.ds}>
              {DANH_SACH_PHIM_TAT.filter((p) => p.nhom === n).map((p) => (
                <div key={p.moTa} className={s.dong}>
                  <dt><Phim p={p} /></dt>
                  <dd>{p.moTa}</dd>
                </div>
              ))}
            </dl>
          </section>
        ))}
      </div>
    </InfoDialog>
  );
}
