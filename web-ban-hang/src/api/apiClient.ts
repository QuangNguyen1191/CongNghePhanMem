// Tang giao tiep backend: 1 cho duy nhat gan base URL, token, doc loi { thongBao, loi }.
import { session } from '../lib/session';

const BASE_URL = (import.meta.env.VITE_API_URL as string | undefined)?.replace(/\/$/, '') ?? 'http://localhost:5212';

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
    /** Loi tung truong khi backend tra 400 do du lieu sai kieu: { truong: [thong bao] } */
    public readonly loiTruong?: Record<string, string[]>,
  ) {
    super(message);
    this.name = 'ApiError';
  }
  get chuaDangNhap() { return this.status === 401; }
  get khongCoQuyen() { return this.status === 403; }
  get khongTimThay() { return this.status === 404; }
}

type Query = Record<string, string | number | boolean | null | undefined>;

interface Tuy {
  query?: Query;
  /** Ghi de token: dung cho API khach vang lai (token phien OTP, khong phai token dang nhap) */
  token?: string | null;
  body?: unknown;
  signal?: AbortSignal;
  /** goi khi token rieng bi 401 (phien OTP het han) */
  khiHetHan?: () => void;
}

function taoUrl(duongDan: string, query?: Query) {
  const url = new URL(BASE_URL + duongDan);
  if (query) {
    for (const [k, v] of Object.entries(query)) {
      if (v !== undefined && v !== null && v !== '') url.searchParams.set(k, String(v));
    }
  }
  return url.toString();
}

const THONG_BAO_MAC_DINH: Record<number, string> = {
  401: 'Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại.',
  403: 'Bạn không có quyền thực hiện thao tác này.',
  404: 'Không tìm thấy dữ liệu.',
};

async function goi<T>(method: string, duongDan: string, tuy: Tuy = {}): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' };
  const rieng = tuy.token !== undefined;
  const token = rieng ? tuy.token : session.token();
  if (token) headers.Authorization = `Bearer ${token}`;
  if (tuy.body !== undefined) headers['Content-Type'] = 'application/json';

  let res: Response;
  try {
    res = await fetch(taoUrl(duongDan, tuy.query), {
      method,
      headers,
      body: tuy.body !== undefined ? JSON.stringify(tuy.body) : undefined,
      signal: tuy.signal,
    });
  } catch (e) {
    if ((e as Error).name === 'AbortError') throw e;
    throw new ApiError(0, 'Không kết nối được máy chủ. Vui lòng kiểm tra mạng và thử lại.');
  }

  if (res.status === 401 && token) {
    if (rieng) tuy.khiHetHan?.(); else session.xoa(); // token het han / tai khoan bi khoa
  }

  if (!res.ok) {
    let thongBao = res.status === 401 && rieng
      ? 'Phiên xác thực số điện thoại đã hết hạn, vui lòng nhận mã OTP mới.'
      : THONG_BAO_MAC_DINH[res.status] ?? 'Lỗi hệ thống, vui lòng thử lại.';
    let loiTruong: Record<string, string[]> | undefined;
    try {
      const data = await res.json();
      if (data?.thongBao && res.status !== 401) thongBao = data.thongBao;
      if (data?.loi) loiTruong = data.loi;
    } catch { /* body rong */ }
    throw new ApiError(res.status, thongBao, loiTruong);
  }

  if (res.status === 204) return undefined as T;
  const loai = res.headers.get('Content-Type') ?? '';
  if (loai.includes('application/json')) return (await res.json()) as T;
  return (await res.blob()) as T;
}

export const api = {
  get: <T>(d: string, t?: Tuy) => goi<T>('GET', d, t),
  post: <T>(d: string, body?: unknown, t?: Tuy) => goi<T>('POST', d, { ...t, body }),
  put: <T>(d: string, body?: unknown, t?: Tuy) => goi<T>('PUT', d, { ...t, body }),
  del: <T>(d: string, t?: Tuy) => goi<T>('DELETE', d, t),
  /** Tai file (PDF) co kem token, tra ve Blob */
  taiFile: (d: string, t?: Tuy) => goi<Blob>('GET', d, t),
};
