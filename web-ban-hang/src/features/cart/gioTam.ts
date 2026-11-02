// Gio hang khi CHUA dang nhap: chi luu [{ maSp, soLuong }] o trinh duyet. Gia, ten, ton kho luon lay lai tu API.
// Dang nhap xong -> POST /api/gio-hang/gop roi xoa gio tam (xem features/auth/useAuth.ts).
import { useSyncExternalStore } from 'react';
import type { DongHang } from '../../api/types';

const KHOA = 'wbh.gio-tam';
const nghe = new Set<() => void>();
let banChup: DongHang[] | null = null;

function docKho(): DongHang[] {
  try {
    const raw = localStorage.getItem(KHOA);
    const ds = raw ? (JSON.parse(raw) as unknown) : [];
    if (!Array.isArray(ds)) return [];
    return ds
      .filter((d): d is DongHang => typeof d?.maSp === 'string' && Number.isInteger(d?.soLuong) && d.soLuong > 0)
      .slice(0, 50);
  } catch {
    return [];
  }
}

function ghi(ds: DongHang[]) {
  banChup = ds;
  try { localStorage.setItem(KHOA, JSON.stringify(ds)); } catch { /* storage bi chan: giu trong bo nho */ }
  nghe.forEach((f) => f());
}

export const gioTam = {
  doc(): DongHang[] {
    if (banChup === null) banChup = docKho();
    return banChup;
  },
  them(maSp: string, soLuong: number) {
    const ds = gioTam.doc();
    const cu = ds.find((d) => d.maSp === maSp);
    ghi(cu ? ds.map((d) => (d.maSp === maSp ? { ...d, soLuong: d.soLuong + soLuong } : d)) : [...ds, { maSp, soLuong }]);
  },
  datSoLuong(maSp: string, soLuong: number) {
    const ds = gioTam.doc();
    ghi(soLuong <= 0 ? ds.filter((d) => d.maSp !== maSp) : ds.map((d) => (d.maSp === maSp ? { ...d, soLuong } : d)));
  },
  xoa(maSp: string) { ghi(gioTam.doc().filter((d) => d.maSp !== maSp)); },
  xoaHet() { ghi([]); },
  nghe(f: () => void) {
    nghe.add(f);
    const onStorage = (e: StorageEvent) => { if (e.key === KHOA) { banChup = null; f(); } };
    window.addEventListener('storage', onStorage);
    return () => { nghe.delete(f); window.removeEventListener('storage', onStorage); };
  },
};

export const useGioTam = () => useSyncExternalStore(gioTam.nghe, gioTam.doc, gioTam.doc);
