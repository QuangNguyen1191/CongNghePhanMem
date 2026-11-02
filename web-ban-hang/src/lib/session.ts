// Luu phien dang nhap. Backend chi ho tro Bearer token (khong co cookie httpOnly / refresh token),
// nen token nam o localStorage de F5 khong mat dang nhap. Khong luu mat khau, khong log token.
import type { PhienDangNhap } from '../api/types';

const KHOA = 'wbh.phien';
type NgheThayDoi = (p: PhienDangNhap | null) => void;
const nguoiNghe = new Set<NgheThayDoi>();

function hetHan(token: string): boolean {
  try {
    const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
    return typeof payload.exp === 'number' && payload.exp * 1000 <= Date.now();
  } catch {
    return true;
  }
}

export const session = {
  doc(): PhienDangNhap | null {
    try {
      const raw = localStorage.getItem(KHOA);
      if (!raw) return null;
      const p = JSON.parse(raw) as PhienDangNhap;
      if (!p.token || hetHan(p.token)) {
        localStorage.removeItem(KHOA);
        return null;
      }
      return p;
    } catch {
      return null;
    }
  },
  ghi(p: PhienDangNhap) {
    try { localStorage.setItem(KHOA, JSON.stringify(p)); } catch { /* trinh duyet chan storage */ }
    nguoiNghe.forEach((f) => f(p));
  },
  xoa() {
    try { localStorage.removeItem(KHOA); } catch { /* bo qua */ }
    nguoiNghe.forEach((f) => f(null));
  },
  token(): string | null {
    return this.doc()?.token ?? null;
  },
  nghe(f: NgheThayDoi) {
    nguoiNghe.add(f);
    return () => { nguoiNghe.delete(f); };
  },
};
