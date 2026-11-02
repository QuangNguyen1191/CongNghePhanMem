// Bo loc + sap xep tren danh sach mau san pham da tai (khong goi API them).
import { soSanhSize, type MauSanPham } from './gomSanPham';

export type SapXep = 'noi-bat' | 'moi' | 'gia-tang' | 'gia-giam';

export const KHOANG_GIA = [
  { ma: 'duoi-300', nhan: 'Dưới 300.000₫', tu: 0, den: 300_000 },
  { ma: '300-500', nhan: '300.000₫ – 500.000₫', tu: 300_000, den: 500_000 },
  { ma: '500-1000', nhan: '500.000₫ – 1.000.000₫', tu: 500_000, den: 1_000_000 },
  { ma: 'tren-1000', nhan: 'Trên 1.000.000₫', tu: 1_000_000, den: Infinity },
] as const;

export const NHAN_SAP_XEP: Record<SapXep, string> = {
  'noi-bat': 'Nổi bật',
  moi: 'Mới nhất',
  'gia-tang': 'Giá thấp đến cao',
  'gia-giam': 'Giá cao đến thấp',
};

export interface TieuChi {
  loai: string[];
  mau: string[];
  size: string[];
  gia: string[];
  conHang: boolean;
  giamGia: boolean;
  sap: SapXep;
}

const tach = (v: string | null) => (v ? v.split(',').filter(Boolean) : []);

export function docTieuChi(p: URLSearchParams): TieuChi {
  const sap = p.get('sap') as SapXep | null;
  return {
    loai: tach(p.get('loai')),
    mau: tach(p.get('mau')),
    size: tach(p.get('size')),
    gia: tach(p.get('gia')),
    conHang: p.get('conHang') === '1',
    giamGia: p.get('giamGia') === '1',
    sap: sap && sap in NHAN_SAP_XEP ? sap : 'noi-bat',
  };
}

export function demBoLoc(t: TieuChi) {
  return t.loai.length + t.mau.length + t.size.length + t.gia.length + Number(t.conHang) + Number(t.giamGia);
}

export function locVaSapXep(ds: MauSanPham[], t: TieuChi): MauSanPham[] {
  const khoang = KHOANG_GIA.filter((k) => t.gia.includes(k.ma));
  const kq = ds.filter((m) => {
    if (t.loai.length && !t.loai.includes(m.maLoaiSp)) return false;
    if (t.mau.length && !m.mauSacs.some((c) => t.mau.includes(c.ten))) return false;
    if (t.size.length && !m.skus.some((s) => s.kichCo && t.size.includes(s.kichCo) && (!t.conHang || s.soLuong > 0))) return false;
    if (t.conHang && !m.conHang) return false;
    if (t.giamGia && !m.coGiamGia) return false;
    if (khoang.length && !khoang.some((k) => m.giaBanThapNhat >= k.tu && m.giaBanThapNhat < k.den)) return false;
    return true;
  });
  const sx = [...kq];
  switch (t.sap) {
    case 'gia-tang': sx.sort((a, b) => a.giaBanThapNhat - b.giaBanThapNhat); break;
    case 'gia-giam': sx.sort((a, b) => b.giaBanThapNhat - a.giaBanThapNhat); break;
    case 'moi': sx.sort((a, b) => b.maSpMoiNhat.localeCompare(a.maSpMoiNhat)); break;
    default: sx.sort((a, b) => Number(b.conHang) - Number(a.conHang)); // het hang xuong cuoi
  }
  return sx;
}

/** Cac gia tri co the loc, lay tu chinh du lieu dang hien (khong liet ke mau / size khong ton tai) */
export function giaTriBoLoc(ds: MauSanPham[]) {
  const mau = new Map<string, number>();
  const size = new Set<string>();
  for (const m of ds) {
    for (const c of m.mauSacs) mau.set(c.ten, (mau.get(c.ten) ?? 0) + 1);
    for (const k of m.kichCos) size.add(k);
  }
  return {
    mau: [...mau.keys()].filter(Boolean).sort((a, b) => a.localeCompare(b, 'vi')),
    size: [...size].sort(soSanhSize),
    coGiamGia: ds.some((m) => m.coGiamGia),
  };
}
