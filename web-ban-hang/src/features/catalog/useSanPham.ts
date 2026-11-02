import { useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import { sanPhamApi } from '../../api/catalogApi';
import { qk } from '../../api/queryKeys';
import { gomSanPham } from './gomSanPham';

// Danh sach SKU dang ban: tai 1 lan, cache 5 phut, moi trang (chu, danh muc, lien quan) dung chung.
export const useSkuDangBan = () =>
  useQuery({ queryKey: qk.sanPhamDangBan, queryFn: sanPhamApi.layDangBan, staleTime: 5 * 60_000 });

export const useGiaKhuyenMai = () =>
  useQuery({ queryKey: qk.giaKhuyenMai, queryFn: sanPhamApi.giaKhuyenMai, staleTime: 5 * 60_000 });

/** Mau san pham da gom + gia khuyen mai. Loi gia KM khong chan trang (hien gia goc). */
export function useMauSanPham() {
  const sp = useSkuDangBan();
  const km = useGiaKhuyenMai();
  const data = useMemo(() => (sp.data ? gomSanPham(sp.data, km.data) : undefined), [sp.data, km.data]);
  return { data, isLoading: sp.isLoading || (km.isLoading && !km.isError), error: sp.error, refetch: sp.refetch };
}

/** Tim kiem: moi tu khoa la 1 cau truy van moi o backend (GET /api/san-pham?tuKhoa=) */
export function useTimKiem(tuKhoa: string) {
  const km = useGiaKhuyenMai();
  const q = useQuery({
    queryKey: qk.timKiem(tuKhoa),
    queryFn: () => sanPhamApi.timKiem(tuKhoa),
    enabled: tuKhoa.trim().length > 0,
    staleTime: 60_000,
  });
  const data = useMemo(() => (q.data ? gomSanPham(q.data, km.data) : undefined), [q.data, km.data]);
  return { data, isLoading: q.isLoading && q.fetchStatus !== 'idle', error: q.error, refetch: q.refetch };
}
