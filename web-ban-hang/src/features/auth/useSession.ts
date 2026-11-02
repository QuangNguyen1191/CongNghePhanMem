import { useEffect, useState } from 'react';
import { session } from '../../lib/session';
import type { PhienDangNhap } from '../../api/types';

/** Phien dang nhap hien tai, tu cap nhat khi dang nhap / dang xuat / token het han (ca o tab khac). */
export function useSession(): PhienDangNhap | null {
  const [p, setP] = useState(() => session.doc());
  useEffect(() => {
    const huy = session.nghe(setP);
    const onStorage = (e: StorageEvent) => { if (e.key === 'wbh.phien') setP(session.doc()); };
    window.addEventListener('storage', onStorage);
    return () => { huy(); window.removeEventListener('storage', onStorage); };
  }, []);
  return p;
}
