import { useEffect, useRef } from 'react';
import { dangNhapLieu, coHopThoaiMo, phimTron } from './phimTat';

/**
 * Gan 1 phim tat don (vd 'a', 'f', 'ArrowLeft') khi component dang hien.
 * Bo qua khi dang go trong o nhap, khi co hop thoai mo, hoac khi bam kem Ctrl/Alt/Meta.
 */
export function usePhimTat(phim: string, xuLy: (e: KeyboardEvent) => void, batDau = true) {
  const ref = useRef(xuLy);
  ref.current = xuLy;
  useEffect(() => {
    if (!batDau) return;
    const nghe = (e: KeyboardEvent) => {
      if (e.defaultPrevented || e.repeat || !phimTron(e) || dangNhapLieu(e) || coHopThoaiMo()) return;
      if (e.key !== phim) return;
      e.preventDefault();
      ref.current(e);
    };
    window.addEventListener('keydown', nghe);
    return () => window.removeEventListener('keydown', nghe);
  }, [phim, batDau]);
}
