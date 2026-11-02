import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useSession } from './useSession';

/** Trang can dang nhap: chua co phien (hoac token het han / bi khoa -> 401 xoa phien) thi ve trang dang nhap, kem duong quay lai. */
export function RequireAuth({ children }: { children: ReactNode }) {
  const phien = useSession();
  const { pathname, search } = useLocation();
  if (!phien) return <Navigate to={`/dang-nhap?tiep=${encodeURIComponent(pathname + search)}`} replace />;
  return <>{children}</>;
}

/** Chi chap nhan duong dan noi bo (chong open redirect qua ?tiep=) */
export function duongDanTiep(raw: string | null, macDinh = '/') {
  if (!raw || !raw.startsWith('/') || raw.startsWith('//') || raw.startsWith('/\\')) return macDinh;
  if (raw.startsWith('/dang-nhap') || raw.startsWith('/dang-ky')) return macDinh;
  return raw;
}
