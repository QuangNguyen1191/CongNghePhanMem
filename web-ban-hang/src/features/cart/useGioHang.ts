import { useMemo } from 'react';
import { useMutation, useQueries, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { gioHangApi } from '../../api/khachApi';
import { sanPhamApi } from '../../api/catalogApi';
import { qk } from '../../api/queryKeys';
import type { GioHang, SanPham } from '../../api/types';
import { useSession } from '../auth/useSession';
import { useGiaKhuyenMai, useSkuDangBan } from '../catalog/useSanPham';
import { gioTam, useGioTam } from './gioTam';

/** 1 dong gio da du thong tin hien thi, dung chung cho gio server va gio tam */
export interface DongGio {
  maSp: string;
  soLuong: number;
  tenSp: string;
  kichCo: string | null;
  mauSac: string | null;
  hinhAnh: string | null;
  /** gia sau KM san pham */
  donGia: number;
  /** gia goc (de gach ngang khi co KM) */
  donGiaGoc: number;
  tonKho: number;
  thanhTien: number;
  /** false: ngung ban / khong con trong danh muc */
  conBan: boolean;
}

export interface GioHienTai {
  dongs: DongGio[];
  tamTinh: number;
  tongSoLuong: number;
  /** co dong vuot ton / het hang / ngung ban -> chua cho thanh toan */
  coLoi: boolean;
  isLoading: boolean;
  error: unknown;
  refetch: () => void;
  laGioTam: boolean;
}

const tomTat = (dongs: DongGio[]) => ({
  tamTinh: dongs.filter((d) => d.conBan).reduce((t, d) => t + d.thanhTien, 0),
  tongSoLuong: dongs.reduce((t, d) => t + d.soLuong, 0),
  coLoi: dongs.some((d) => !d.conBan || d.tonKho <= 0 || d.soLuong > d.tonKho),
});

export function useGioHang(): GioHienTai {
  const phien = useSession();
  const skuQ = useSkuDangBan();
  const kmQ = useGiaKhuyenMai();
  const serverQ = useQuery({ queryKey: qk.gioHang, queryFn: gioHangApi.xem, enabled: !!phien });
  const tam = useGioTam();

  const theoMa = useMemo(() => new Map((skuQ.data ?? []).map((s) => [s.maSp, s])), [skuQ.data]);
  // dong trong gio tam khong co trong danh sach dang ban -> hoi rieng tung SKU de biet ten (thuong la vua ngung ban)
  const thieu = !phien && skuQ.data ? tam.filter((d) => !theoMa.has(d.maSp)).map((d) => d.maSp) : [];
  const thieuQ = useQueries({
    queries: thieu.map((ma) => ({ queryKey: qk.sanPham(ma), queryFn: () => sanPhamApi.layTheoMa(ma), retry: false })),
  });

  return useMemo<GioHienTai>(() => {
    if (phien) {
      const gio = serverQ.data;
      const dongs: DongGio[] = (gio?.chiTiets ?? []).map((c) => {
        const sku = theoMa.get(c.maSp);
        return {
          maSp: c.maSp, soLuong: c.soLuong, tenSp: c.tenSp ?? c.maSp, kichCo: c.kichCo ?? null, mauSac: c.mauSac ?? null,
          hinhAnh: c.hinhAnh ?? null, donGia: c.donGia, donGiaGoc: sku?.donGia ?? c.donGia, tonKho: c.tonKho,
          thanhTien: c.thanhTien, conBan: !skuQ.data || !!sku,
        };
      });
      return { dongs, ...tomTat(dongs), isLoading: serverQ.isLoading, error: serverQ.error, refetch: serverQ.refetch, laGioTam: false };
    }
    const phu = new Map<string, SanPham>();
    thieuQ.forEach((q) => { if (q.data) phu.set(q.data.maSp, q.data); });
    const dongs: DongGio[] = tam.map((d) => {
      const sku = theoMa.get(d.maSp) ?? phu.get(d.maSp);
      const conBan = !!sku && sku.trangThai === 'DANG_BAN';
      const donGia = sku ? kmQ.data?.[sku.maSp] ?? sku.donGia : 0;
      return {
        maSp: d.maSp, soLuong: d.soLuong, tenSp: sku?.tenSp ?? `Sản phẩm ${d.maSp}`, kichCo: sku?.kichCo ?? null,
        mauSac: sku?.mauSac ?? null, hinhAnh: sku?.hinhAnh ?? null, donGia, donGiaGoc: sku?.donGia ?? 0,
        tonKho: sku?.soLuong ?? 0, thanhTien: donGia * d.soLuong, conBan,
      };
    });
    return {
      dongs, ...tomTat(dongs), isLoading: skuQ.isLoading || thieuQ.some((q) => q.isLoading),
      error: skuQ.error, refetch: skuQ.refetch, laGioTam: true,
    };
  }, [phien, serverQ.data, serverQ.isLoading, serverQ.error, serverQ.refetch, tam, theoMa, thieuQ, kmQ.data, skuQ.data, skuQ.isLoading, skuQ.error, skuQ.refetch]);
}

/** So luong hien tren icon gio. Gio server chi tai khi da dang nhap. */
export function useSoLuongGio() {
  const phien = useSession();
  const tam = useGioTam();
  const q = useQuery({ queryKey: qk.gioHang, queryFn: gioHangApi.xem, enabled: !!phien });
  if (!phien) return tam.reduce((t, d) => t + d.soLuong, 0);
  return q.data?.chiTiets.reduce((t, d) => t + d.soLuong, 0) ?? 0;
}

/** Them / sua / xoa dong gio. Da dang nhap: goi API (server kiem tra ton). Chua dang nhap: kiem tra ton tren trinh duyet. */
export function useThaoTacGio() {
  const phien = useSession();
  const qc = useQueryClient();
  const luu = (g: GioHang) => qc.setQueryData(qk.gioHang, g);

  const them = useMutation({
    mutationFn: async ({ sku, soLuong }: { sku: SanPham; soLuong: number }) => {
      if (phien) { luu(await gioHangApi.them({ maSp: sku.maSp, soLuong })); return; }
      const daCo = gioTam.doc().find((d) => d.maSp === sku.maSp)?.soLuong ?? 0;
      if (daCo + soLuong > sku.soLuong) {
        throw new Error(sku.soLuong - daCo > 0
          ? `Chỉ có thể thêm tối đa ${sku.soLuong - daCo} sản phẩm nữa (còn ${sku.soLuong} trong kho).`
          : `Giỏ hàng đã có đủ ${sku.soLuong} sản phẩm còn trong kho.`);
      }
      gioTam.them(sku.maSp, soLuong);
    },
  });

  const capNhat = useMutation({
    mutationFn: async ({ maSp, soLuong }: { maSp: string; soLuong: number }) => {
      if (phien) { luu(await gioHangApi.capNhat({ maSp, soLuong })); return; }
      gioTam.datSoLuong(maSp, soLuong);
    },
  });

  const xoa = useMutation({
    mutationFn: async (maSp: string) => {
      if (phien) { luu(await gioHangApi.xoaDong(maSp)); return; }
      gioTam.xoa(maSp);
    },
    onSuccess: () => toast.success('Đã xóa sản phẩm khỏi giỏ'),
  });

  const xoaHet = useMutation({
    mutationFn: async () => {
      if (phien) { await gioHangApi.xoaHet(); await qc.invalidateQueries({ queryKey: qk.gioHang }); return; }
      gioTam.xoaHet();
    },
  });

  return { them, capNhat, xoa, xoaHet };
}
