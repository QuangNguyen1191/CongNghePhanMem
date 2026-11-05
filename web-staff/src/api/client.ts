// Gọi WebApi. Tầng giao diện (GUI) chỉ gọi API, không có SQL hay quy tắc nghiệp vụ.
// Mỗi response có header X-So-Cau-Sql / X-Sql-Vi-Tri (yêu cầu 1): lưu lại để thanh trạng thái hiển thị.

export const API_URL = (import.meta.env.VITE_API_URL as string | undefined)?.replace(/\/$/, '') ?? 'http://localhost:5212';

const KHOA_PHIEN = 'web-staff.phien';

export class LoiApi extends Error {
  constructor(public status: number, message: string) {
    super(message);
  }
}

export interface ThongTinSql {
  soCau: number;
  viTri: string | null;
  duongDan: string;
  thoiGianMs: number;
  luc: number;
}

type NgheSql = (t: ThongTinSql) => void;
const cacNguoiNghe = new Set<NgheSql>();
export function ngheSql(f: NgheSql) {
  cacNguoiNghe.add(f);
  return () => {
    cacNguoiNghe.delete(f);
  };
}

let xuLyHetPhien: (() => void) | null = null;
export function khiHetPhien(f: () => void) {
  xuLyHetPhien = f;
}

export function layToken(): string | null {
  try {
    const p = localStorage.getItem(KHOA_PHIEN);
    return p ? (JSON.parse(p).token as string) : null;
  } catch {
    return null;
  }
}

interface TuyChon {
  method?: string;
  body?: unknown;
  query?: Record<string, string | number | boolean | null | undefined>;
  form?: FormData;
}

function taoUrl(duongDan: string, query?: TuyChon['query']) {
  const u = new URL(API_URL + duongDan);
  if (query)
    for (const [k, v] of Object.entries(query)) if (v !== undefined && v !== null && v !== '') u.searchParams.set(k, String(v));
  return u.toString();
}

async function goi(duongDan: string, tc: TuyChon = {}): Promise<Response> {
  const headers: Record<string, string> = {};
  const token = layToken();
  if (token) headers.Authorization = `Bearer ${token}`;
  let body: BodyInit | undefined;
  if (tc.form) body = tc.form;
  else if (tc.body !== undefined) {
    headers['Content-Type'] = 'application/json';
    body = JSON.stringify(tc.body);
  }
  const batDau = performance.now();
  let res: Response;
  try {
    res = await fetch(taoUrl(duongDan, tc.query), { method: tc.method ?? 'GET', headers, body });
  } catch {
    throw new LoiApi(0, 'Không kết nối được máy chủ. Kiểm tra WebApi đã chạy chưa (dotnet run --project WebApi).');
  }
  const soCau = res.headers.get('X-So-Cau-Sql');
  if (soCau !== null) {
    const t: ThongTinSql = {
      soCau: Number(soCau),
      viTri: res.headers.get('X-Sql-Vi-Tri'),
      duongDan: `${tc.method ?? 'GET'} ${duongDan}`,
      thoiGianMs: Math.round(performance.now() - batDau),
      luc: Date.now(),
    };
    cacNguoiNghe.forEach((f) => f(t));
  }
  if (res.status === 401 && token) {
    xuLyHetPhien?.();
    throw new LoiApi(401, 'Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại');
  }
  if (res.status === 403) throw new LoiApi(403, 'Bạn không có quyền thực hiện thao tác này');
  if (!res.ok) {
    let thongBao = `Lỗi ${res.status}`;
    try {
      const j = await res.json();
      if (j?.thongBao) thongBao = j.thongBao;
    } catch {
      /* body không phải JSON */
    }
    throw new LoiApi(res.status, thongBao);
  }
  return res;
}

export async function api<T>(duongDan: string, tc: TuyChon = {}): Promise<T> {
  const res = await goi(duongDan, tc);
  if (res.status === 204) return undefined as T;
  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

export const get = <T>(d: string, query?: TuyChon['query']) => api<T>(d, { query });
export const post = <T>(d: string, body?: unknown, query?: TuyChon['query']) => api<T>(d, { method: 'POST', body: body ?? {}, query });
export const put = <T>(d: string, body?: unknown) => api<T>(d, { method: 'PUT', body });

/** Tải file (PDF / Excel) có kèm token rồi mở hoặc lưu. */
export async function taiFile(duongDan: string, cach: 'mo' | 'luu' = 'mo', query?: TuyChon['query']) {
  const res = await goi(duongDan, { query });
  const blob = await res.blob();
  const cd = res.headers.get('Content-Disposition') ?? '';
  const m = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(cd);
  const ten = m ? decodeURIComponent(m[1]) : duongDan.split('/').pop() || 'tai-xuong';
  const url = URL.createObjectURL(blob);
  if (cach === 'mo' && blob.type === 'application/pdf') {
    const w = window.open(url, '_blank');
    if (!w) luuUrl(url, ten);
  } else luuUrl(url, ten);
  setTimeout(() => URL.revokeObjectURL(url), 60_000);
}

function luuUrl(url: string, ten: string) {
  const a = document.createElement('a');
  a.href = url;
  a.download = ten;
  document.body.appendChild(a);
  a.click();
  a.remove();
}

export const KHOA_LUU_PHIEN = KHOA_PHIEN;
