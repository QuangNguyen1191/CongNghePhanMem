import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { KHOA_LUU_PHIEN, khiHetPhien, post } from '../api/client';
import type { PhienDangNhap } from '../api/types';
import { boDau } from './dinhDang';

// Nhóm quyền màn hình theo chức vụ (cột NHAN_VIEN.chuc_vu). Backend vẫn kiểm tra vai trò NHAN_VIEN / ADMIN cho mọi API;
// phần này chỉ ẩn các màn hình không thuộc việc của từng người để giao diện gọn hơn.
export type NhomQuyen = 'ban-hang' | 'don-online' | 'kho';
const TAT_CA: NhomQuyen[] = ['ban-hang', 'don-online', 'kho'];

export function quyenTheoChucVu(p: PhienDangNhap | null): NhomQuyen[] {
  if (!p) return [];
  if (p.vaiTro === 'ADMIN') return TAT_CA;
  const cv = boDau(p.chucVu ?? '');
  const q: NhomQuyen[] = [];
  if (cv.includes('thu ngan') || cv.includes('ban hang tai quay')) q.push('ban-hang');
  if (cv.includes('online')) q.push('don-online');
  if (cv.includes('kho')) q.push('kho');
  if (cv.includes('quan ly') || cv.includes('giam doc') || q.length === 0) return TAT_CA;
  return q;
}

interface GiaTriPhien {
  phien: PhienDangNhap | null;
  quyen: NhomQuyen[];
  coQuyen: (q?: NhomQuyen[]) => boolean;
  dangNhap: (tenDangNhap: string, matKhau: string) => Promise<PhienDangNhap>;
  dangXuat: () => void;
}

const Ctx = createContext<GiaTriPhien | null>(null);

function docPhien(): PhienDangNhap | null {
  try {
    const s = localStorage.getItem(KHOA_LUU_PHIEN);
    return s ? (JSON.parse(s) as PhienDangNhap) : null;
  } catch {
    return null;
  }
}

export function PhienProvider({ children }: { children: ReactNode }) {
  const [phien, setPhien] = useState<PhienDangNhap | null>(docPhien);

  const dangXuat = useCallback(() => {
    try {
      localStorage.removeItem(KHOA_LUU_PHIEN);
    } catch {
      /* bỏ qua */
    }
    setPhien(null);
  }, []);

  useEffect(() => khiHetPhien(dangXuat), [dangXuat]);

  const dangNhap = useCallback(async (tenDangNhap: string, matKhau: string) => {
    const p = await post<PhienDangNhap>('/api/tai-khoan/dang-nhap/web-staff', { tenDangNhap, matKhau });
    try {
      localStorage.setItem(KHOA_LUU_PHIEN, JSON.stringify(p));
    } catch {
      /* trình duyệt chặn lưu: vẫn dùng được tới khi tải lại trang */
    }
    setPhien(p);
    return p;
  }, []);

  const gt = useMemo<GiaTriPhien>(() => {
    const quyen = quyenTheoChucVu(phien);
    return { phien, quyen, coQuyen: (q) => !q || q.some((x) => quyen.includes(x)), dangNhap, dangXuat };
  }, [phien, dangNhap, dangXuat]);

  return <Ctx.Provider value={gt}>{children}</Ctx.Provider>;
}

export function usePhien() {
  const c = useContext(Ctx);
  if (!c) throw new Error('usePhien phải nằm trong PhienProvider');
  return c;
}
