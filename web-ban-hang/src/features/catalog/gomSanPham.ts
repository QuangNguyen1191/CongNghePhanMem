// CSDL luu moi size-mau la 1 dong SAN_PHAM (SKU). Web gom cac SKU cung ten_sp thanh 1 "mau san pham" de hien thi,
// dung quy tac backend dang dung (SanPhamBLL.LayCacBienThe, DoiTraBLL: cung ten_sp = cung mau).
import type { GiaKhuyenMai, SanPham } from '../../api/types';

export interface MauSac {
  ten: string;
  hinhAnh: string | null;
  /** SKU dai dien cua mau nay (uu tien con hang) */
  maSp: string;
  conHang: boolean;
}

export interface MauSanPham {
  key: string;
  tenSp: string;
  maLoaiSp: string;
  tenLoaiSp: string;
  chatLieu: string | null;
  skus: SanPham[];
  mauSacs: MauSac[];
  kichCos: string[];
  giaThapNhat: number;
  giaCaoNhat: number;
  /** gia thap nhat sau khuyen mai (bang giaThapNhat neu khong co KM) */
  giaBanThapNhat: number;
  phanTramGiamMax: number;
  coGiamGia: boolean;
  tongTon: number;
  conHang: boolean;
  /** SKU mo khi bam vao the */
  maSpDaiDien: string;
  hinhAnh: string | null;
  /** ma SP lon nhat: dung xap xi "moi nhat" vi SAN_PHAM khong co ngay tao (da thong nhat o Phase 1) */
  maSpMoiNhat: string;
}

const THU_TU_SIZE = ['XXS', 'XS', 'S', 'M', 'L', 'XL', 'XXL', '3XL'];

export function soSanhSize(a: string, b: string) {
  const ia = THU_TU_SIZE.indexOf(a.toUpperCase());
  const ib = THU_TU_SIZE.indexOf(b.toUpperCase());
  if (ia >= 0 && ib >= 0) return ia - ib;
  if (ia >= 0) return -1;
  if (ib >= 0) return 1;
  const na = Number(a), nb = Number(b);
  if (!Number.isNaN(na) && !Number.isNaN(nb)) return na - nb;
  return a.localeCompare(b, 'vi');
}

export const giaBan = (sp: SanPham, km: GiaKhuyenMai | undefined) => km?.[sp.maSp] ?? sp.donGia;

export function gomSanPham(skus: SanPham[], km?: GiaKhuyenMai): MauSanPham[] {
  const nhom = new Map<string, SanPham[]>();
  for (const s of skus) {
    if (s.trangThai !== 'DANG_BAN') continue;
    const ds = nhom.get(s.tenSp);
    if (ds) ds.push(s); else nhom.set(s.tenSp, [s]);
  }

  return [...nhom.entries()].map(([tenSp, ds]) => {
    const mauMap = new Map<string, SanPham[]>();
    for (const s of ds) {
      const k = s.mauSac ?? '';
      const arr = mauMap.get(k);
      if (arr) arr.push(s); else mauMap.set(k, [s]);
    }
    const mauSacs: MauSac[] = [...mauMap.entries()].map(([ten, arr]) => {
      const conHang = arr.find((x) => x.soLuong > 0);
      const dau = conHang ?? arr[0];
      return { ten, hinhAnh: arr.find((x) => x.hinhAnh)?.hinhAnh ?? null, maSp: dau.maSp, conHang: !!conHang };
    }).sort((a, b) => Number(b.conHang) - Number(a.conHang));

    const gias = ds.map((s) => s.donGia);
    const giaBans = ds.map((s) => giaBan(s, km));
    const phanTram = ds.map((s) => (km?.[s.maSp] != null ? Math.round((1 - km[s.maSp] / s.donGia) * 100) : 0));
    const tongTon = ds.reduce((t, s) => t + s.soLuong, 0);
    const daiDien = mauSacs[0];
    return {
      key: daiDien.maSp,
      tenSp,
      maLoaiSp: ds[0].maLoaiSp,
      tenLoaiSp: ds[0].tenLoaiSp ?? '',
      chatLieu: ds[0].chatLieu ?? null,
      skus: ds,
      mauSacs,
      kichCos: [...new Set(ds.map((s) => s.kichCo).filter((x): x is string => !!x))].sort(soSanhSize),
      giaThapNhat: Math.min(...gias),
      giaCaoNhat: Math.max(...gias),
      giaBanThapNhat: Math.min(...giaBans),
      phanTramGiamMax: Math.max(...phanTram),
      coGiamGia: phanTram.some((p) => p > 0),
      tongTon,
      conHang: tongTon > 0,
      maSpDaiDien: daiDien.maSp,
      hinhAnh: daiDien.hinhAnh,
      maSpMoiNhat: ds.map((s) => s.maSp).sort().at(-1)!,
    };
  });
}

/** Ma mau hien cham tron. Ten mau lay tu CSDL (mau_sac); mau chua co trong bang thi hien xam trung tinh. */
const BANG_MAU: Record<string, string> = {
  'trắng': '#ffffff', 'đen': '#111111', 'xám': '#9b9b9b', 'xanh navy': '#1f2a44', 'xanh lá': '#4f6b4a',
  'xanh nhạt': '#a9c4dc', 'xanh đậm': '#27364f', 'be': '#d8c6a5', 'hồng': '#e7b3bb', 'đỏ': '#b3202f',
  'nâu': '#6b4a34', 'vàng': '#e2c046', 'kem': '#efe6d2',
  'trắng ngà': '#f1ecdf', 'xám đậm': '#555555', 'xanh dương': '#4a6d9c', 'xanh dương đậm': '#24385a',
  'xanh ô liu': '#6b6b3a', 'xanh lá đậm': '#2f4a35', 'cam đậm': '#b5552b', 'nâu đậm': '#4a3326',
};
export function maMau(ten: string): string {
  const k = ten.trim().toLowerCase();
  if (k === 'hoa nhí') return 'repeating-radial-gradient(circle at 30% 30%, #e7b3bb 0 2px, #f6f1e7 2px 5px)';
  return BANG_MAU[k] ?? '#c9c9c9';
}
