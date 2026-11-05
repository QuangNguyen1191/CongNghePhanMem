// Phien khach vang lai (sau khi nhap dung OTP): token 30 phut, chi dung cho /api/vang-lai.
// Luu sessionStorage (dong tab la mat) vi khach vang lai hay dung may chung; tach rieng phien dang nhap (session.ts).
import { useSyncExternalStore } from 'react';
import type { PhienVangLai } from '../api/types';

const KHOA = 'wbh.vang-lai';
const nghe = new Set<() => void>();
let banChup: PhienVangLai | null | undefined;

function con(p: PhienVangLai | null): PhienVangLai | null {
  return p && p.token && new Date(p.hetHan).getTime() > Date.now() + 5_000 ? p : null;
}

function docKho(): PhienVangLai | null {
  try {
    const raw = sessionStorage.getItem(KHOA);
    return raw ? con(JSON.parse(raw) as PhienVangLai) : null;
  } catch {
    return null;
  }
}

function ghi(p: PhienVangLai | null) {
  banChup = p;
  try {
    if (p) sessionStorage.setItem(KHOA, JSON.stringify(p)); else sessionStorage.removeItem(KHOA);
  } catch { /* storage bi chan: giu trong bo nho */ }
  nghe.forEach((f) => f());
}

export const phienVangLai = {
  doc(): PhienVangLai | null {
    if (banChup === undefined) banChup = docKho();
    const p = con(banChup);
    if (!p && banChup) banChup = null; // het han
    return p;
  },
  ghi: (p: PhienVangLai) => ghi(p),
  xoa: () => ghi(null),
  token: () => phienVangLai.doc()?.token ?? null,
  nghe(f: () => void) { nghe.add(f); return () => { nghe.delete(f); }; },
};

export const usePhienVangLai = () => useSyncExternalStore(phienVangLai.nghe, phienVangLai.doc, phienVangLai.doc);
