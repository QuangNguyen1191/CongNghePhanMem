import { APIRequestContext, APIResponse, expect, request } from '@playwright/test';

export const BASE = process.env.API_URL ?? 'http://localhost:5212';
export const MAT_KHAU = '123456';

// Hau to duy nhat moi lan chay, de chay lai nhieu lan tren cung CSDL khong bi trung ten / email / SDT
export const RUN = Date.now().toString().slice(-7);
// SDT di dong 10 so (09 + 6 so cuoi cua RUN + 2 so thu tu)
export const sdtMoi = (i: number) => `09${RUN.slice(-6)}${String(i).padStart(2, '0')}`;

type Noi = 'web-ban-hang' | 'web-staff' | 'app-admin';
const tokenCache = new Map<string, string>();

export async function dangNhap(api: APIRequestContext, ten: string, noi: Noi, matKhau = MAT_KHAU) {
  return api.post(`/api/tai-khoan/dang-nhap/${noi}`, { data: { tenDangNhap: ten, matKhau } });
}

export async function layToken(ten: string, noi: Noi, matKhau = MAT_KHAU): Promise<string> {
  const key = `${ten}|${noi}`;
  if (tokenCache.has(key)) return tokenCache.get(key)!;
  const ctx = await request.newContext({ baseURL: BASE });
  const res = await dangNhap(ctx, ten, noi, matKhau);
  expect(res.status(), `dang nhap ${ten} @ ${noi}: ${await res.text()}`).toBe(200);
  const token = (await res.json()).token as string;
  await ctx.dispose();
  tokenCache.set(key, token);
  return token;
}

// Tao request context da gan token
export async function ctxCho(ten: string, noi: Noi): Promise<APIRequestContext> {
  const token = await layToken(ten, noi);
  return request.newContext({ baseURL: BASE, extraHTTPHeaders: { Authorization: `Bearer ${token}`, Accept: 'application/json' } });
}

export const admin = () => ctxCho('quanly', 'web-staff');
export const nhanVien = () => ctxCho('thungan1', 'web-staff');
export const khach = (ten = 'khach01') => ctxCho(ten, 'web-ban-hang');
export const anDanh = () => request.newContext({ baseURL: BASE });

// Kiem tra status, in body khi sai de de doc loi
export async function ok(res: APIResponse, status = 200) {
  const body = await res.text();
  expect(res.status(), `${res.url()} -> ${res.status()}: ${body.slice(0, 500)}`).toBe(status);
  return body ? JSON.parse(body) : undefined;
}

// Loi nghiep vu: 400/404 kem { thongBao }
export async function loi(res: APIResponse, status: number, chua?: string | RegExp) {
  const body = await res.text();
  expect(res.status(), `${res.url()} -> ${res.status()}: ${body.slice(0, 500)}`).toBe(status);
  const j = JSON.parse(body);
  expect(typeof j.thongBao).toBe('string');
  if (chua) expect(j.thongBao).toMatch(chua);
  return j.thongBao as string;
}

// Tao 1 san pham moi co san ton kho (qua phieu nhap) de cac test ban hang khong phu thuoc du lieu mau
export async function taoSanPhamCoTon(ad: APIRequestContext, soLuong = 50, donGia = 200000, tenSp?: string, kichCo = 'M') {
  const sp = await ok(await ad.post('/api/san-pham', {
    data: { tenSp: tenSp ?? `SP test ${RUN}-${Math.random().toString(36).slice(2, 7)}`, maLoaiSp: 'LSP01', donGia, kichCo, mauSac: 'Đỏ' },
  }));
  await ok(await ad.post('/api/phieu-nhap', { data: { maNcc: 'NCC01', chiTiets: [{ maSp: sp.maSp, soLuong, donGiaNhap: donGia / 2 }] } }));
  return sp as { maSp: string; tenSp: string; donGia: number };
}

// v3: xac thuc SDT bang OTP (backend chay o moi truong Development tra luon ma demo) -> token phien vang lai.
// Nho token theo SDT (dung 30 phut) vi moi SDT chi gui duoc 1 ma / 60 giay.
const phienCache = new Map<string, string>();
export async function xacThucSdt(api: APIRequestContext, sdt: string, mucDich = 'DANG_KY'): Promise<string> {
  if (phienCache.has(sdt)) return phienCache.get(sdt)!;
  const gui = await ok(await api.post('/api/vang-lai/otp', { data: { sdt, mucDich } }));
  expect(gui.maDemo, 'can bat Otp:HienMaDemo (chay WebApi o moi truong Development)').toMatch(/^\d{6}$/);
  const phien = await ok(await api.post('/api/vang-lai/xac-thuc', { data: { sdt, ma: gui.maDemo } }));
  phienCache.set(sdt, phien.token);
  return phien.token as string;
}

// Dang ky khach (v3: bat buoc SDT da xac thuc OTP). Khong truyen SDT thi tu tao SDT moi.
let soTuDong = 50;
export async function dangKy(api: APIRequestContext,
  data: { tenDangNhap: string; matKhau: string; khachHang: Record<string, unknown> }) {
  const sdt = (data.khachHang.sdt as string | undefined) ?? sdtMoi(soTuDong++);
  const phienXacThuc = await xacThucSdt(api, sdt);
  return api.post('/api/tai-khoan/dang-ky', { data: { ...data, khachHang: { ...data.khachHang, sdt }, phienXacThuc } });
}
