import { useQuery } from '@tanstack/react-query';
import { qk } from '../../api/queryKeys';

/**
 * CSDL chi luu 1 anh/SKU (SAN_PHAM.hinh_anh). Cac anh con lai cua san pham UNIQLO that nam o
 * public/images/uniqlo/<uniqloId>/ va duoc liet ke trong manifest.json (sinh tu database/tools/gen_manifest_anh.py).
 * Day la file tinh di kem anh, khong thay the du lieu API: thieu manifest thi chi hien anh trong CSDL.
 */
type BoAnh = Record<string, { mau: Record<string, string[]>; chung: string[] }>;

const DUONG_DAN = /^\/images\/uniqlo\/([^/]+)\/([^/-]+)-\d+\.jpg$/;

export function useBoAnh() {
  return useQuery({
    queryKey: qk.boAnh,
    queryFn: async (): Promise<BoAnh> => {
      const r = await fetch('/images/uniqlo/manifest.json');
      return r.ok ? r.json() : {};
    },
    staleTime: Infinity,
    gcTime: Infinity,
    retry: false,
  });
}

/** Tat ca anh cho 1 SKU: anh cua mau sac do truoc, roi anh chung cua san pham. Luon co it nhat anh trong CSDL. */
export function layAnh(boAnh: BoAnh | undefined, hinhAnh: string | null | undefined): string[] {
  if (!hinhAnh) return [];
  const m = DUONG_DAN.exec(hinhAnh);
  const sp = m && boAnh?.[m[1]];
  if (!sp) return [hinhAnh];
  const theoMau = sp.mau[m![2]] ?? [];
  return [...new Set([hinhAnh, ...theoMau, ...sp.chung])];
}
