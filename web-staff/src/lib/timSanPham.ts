import { useQuery } from '@tanstack/react-query';
import { useEffect, useMemo, useRef, useState } from 'react';
import { get, ngheSql } from '../api/client';
import type { SanPham } from '../api/types';
import { boDau } from './dinhDang';

// Yêu cầu 2: tìm kiếm sản phẩm theo 2 chế độ để so sánh.
//  - TAI_SU_DUNG: tải danh sách 1 lần (1 câu SQL), mỗi lần gõ chỉ lọc trên danh sách đã có trong bộ nhớ → 0 câu SQL.
//  - TRUY_VAN_MOI: mỗi lần gõ (sau 300 ms) gọi API có từ khóa → backend chạy 1 câu SQL mới (LIKE).
export type CheDoTim = 'TAI_SU_DUNG' | 'TRUY_VAN_MOI';

const KHOA_CHE_DO = 'web-staff.che-do-tim';

export function useCheDoTim(): [CheDoTim, (c: CheDoTim) => void] {
  const [c, setC] = useState<CheDoTim>(() => {
    try {
      return (localStorage.getItem(KHOA_CHE_DO) as CheDoTim) || 'TAI_SU_DUNG';
    } catch {
      return 'TAI_SU_DUNG';
    }
  });
  const doi = (m: CheDoTim) => {
    setC(m);
    try {
      localStorage.setItem(KHOA_CHE_DO, m);
    } catch {
      /* bỏ qua */
    }
  };
  return [c, doi];
}

export function locSanPham(ds: SanPham[], tuKhoa: string) {
  const k = boDau(tuKhoa.trim());
  if (!k) return ds;
  const cacTu = k.split(/\s+/);
  return ds.filter((s) => {
    const chuoi = boDau(`${s.maSp} ${s.tenSp} ${s.mauSac ?? ''} ${s.kichCo ?? ''} ${s.tenLoaiSp ?? ''}`);
    return cacTu.every((t) => chuoi.includes(t));
  });
}

export interface ThongKeTim {
  soCauSql: number;
  thoiGianMs: number;
  nguon: string;
}

export function useTimSanPham(tuKhoa: string, cheDo: CheDoTim, opt: { chiDangBan?: boolean; maLoaiSp?: string } = {}) {
  const chiDangBan = opt.chiDangBan ?? false;
  const [tuKhoaTre, setTuKhoaTre] = useState(tuKhoa);
  useEffect(() => {
    const t = setTimeout(() => setTuKhoaTre(tuKhoa), 300);
    return () => clearTimeout(t);
  }, [tuKhoa]);

  // Ghi lại số câu SQL của lần gọi /api/san-pham gần nhất (đọc từ header X-So-Cau-Sql)
  const sqlCuoi = useRef<{ soCau: number; ms: number } | null>(null);
  useEffect(
    () =>
      ngheSql((t) => {
        if (/^GET \/api\/san-pham(\?|$)/.test(t.duongDan)) sqlCuoi.current = { soCau: t.soCau, ms: t.thoiGianMs };
      }),
    [],
  );

  const toanBo = useQuery({
    queryKey: ['san-pham', 'tat-ca', chiDangBan],
    queryFn: () => get<SanPham[]>('/api/san-pham', { chiDangBan }),
    staleTime: 5 * 60_000,
    enabled: cheDo === 'TAI_SU_DUNG',
  });

  const coTuKhoa = tuKhoaTre.trim().length >= 1 || !!opt.maLoaiSp;
  const moi = useQuery({
    queryKey: ['san-pham', 'tim', tuKhoaTre.trim(), opt.maLoaiSp ?? '', chiDangBan],
    queryFn: () => get<SanPham[]>('/api/san-pham', { tuKhoa: tuKhoaTre.trim(), maLoaiSp: opt.maLoaiSp, chiDangBan }),
    enabled: cheDo === 'TRUY_VAN_MOI',
    staleTime: 0,
    gcTime: 0,
    placeholderData: (p) => p,
  });

  // Thời gian lọc trong bộ nhớ (chế độ tái sử dụng) đo ngay quanh phép lọc
  const { ketQua, msLoc } = useMemo(() => {
    if (cheDo === 'TAI_SU_DUNG') {
      const t0 = performance.now();
      let ds = toanBo.data ?? [];
      if (opt.maLoaiSp) ds = ds.filter((s) => s.maLoaiSp === opt.maLoaiSp);
      const kq = locSanPham(ds, tuKhoa);
      return { ketQua: kq, msLoc: Math.round((performance.now() - t0) * 100) / 100 };
    }
    return { ketQua: moi.data ?? [], msLoc: 0 };
  }, [cheDo, toanBo.data, moi.data, tuKhoa, opt.maLoaiSp]);

  const thongKe: ThongKeTim =
    cheDo === 'TAI_SU_DUNG'
      ? {
          soCauSql: 0,
          thoiGianMs: msLoc,
          nguon: `Lọc trên ${toanBo.data?.length ?? 0} SP đã tải sẵn (tải 1 lần lúc mở màn hình)`,
        }
      : {
          soCauSql: sqlCuoi.current?.soCau ?? 0,
          thoiGianMs: sqlCuoi.current?.ms ?? 0,
          nguon: coTuKhoa ? `Gọi API với từ khóa "${tuKhoaTre.trim()}"` : 'Gọi API lấy toàn bộ',
        };

  return {
    ketQua,
    dangTai: cheDo === 'TAI_SU_DUNG' ? toanBo.isLoading : moi.isFetching,
    loi: (cheDo === 'TAI_SU_DUNG' ? toanBo.error : moi.error) as Error | null,
    thongKe,
    taiLai: () => (cheDo === 'TAI_SU_DUNG' ? toanBo.refetch() : moi.refetch()),
  };
}
