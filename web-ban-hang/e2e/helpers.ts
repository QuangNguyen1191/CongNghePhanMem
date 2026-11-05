import { expect, request, type Page } from '@playwright/test';

export const API = process.env.API_URL ?? 'http://localhost:5212';
/** Nhan cua o bat buoc co dau * phia sau: khop dung ten o, co hoac khong co dau * */
export const nhan = (ten: string) => new RegExp(`^${ten}( \\*)?$`);

export const MAT_KHAU = '123456'; // mat khau mau cua moi tai khoan trong QL_BanThoiTrang_DuLieuMau.sql

export interface Sku { maSp: string; tenSp: string; mauSac: string; kichCo: string; soLuong: number; donGia: number; trangThai: string }

/** Goi API that de chuan bi du lieu (khong thay the giao dien dang duoc test) */
export async function apiDangNhap(ten: string, mk = MAT_KHAU, noi: 'web-ban-hang' | 'web-staff' = 'web-ban-hang') {
  const ctx = await request.newContext({ baseURL: API });
  const r = await ctx.post(`/api/tai-khoan/dang-nhap/${noi}`, { data: { tenDangNhap: ten, matKhau: mk } });
  expect(r.ok(), `dang nhap ${ten}: ${await r.text()}`).toBeTruthy();
  const p = await r.json();
  return request.newContext({ baseURL: API, extraHTTPHeaders: { Authorization: `Bearer ${p.token}` } });
}

export async function xoaGioServer(ten: string) {
  const ctx = await apiDangNhap(ten);
  await ctx.delete('/api/gio-hang');
  await ctx.dispose();
}

/** SKU dang ban con du hang (de them vao gio khong bi het hang) */
export async function skuConHang(toiThieu = 3, boQua: string[] = []): Promise<Sku> {
  const ctx = await request.newContext({ baseURL: API });
  const ds: Sku[] = await (await ctx.get('/api/san-pham?chiDangBan=true')).json();
  await ctx.dispose();
  const sku = ds.find((s) => s.soLuong >= toiThieu && s.kichCo && s.mauSac && !boQua.includes(s.tenSp));
  if (!sku) throw new Error('Khong co SKU con hang trong du lieu mau');
  return sku;
}

export async function dangNhapUI(page: Page, ten: string, mk = MAT_KHAU) {
  await page.getByLabel(nhan('Tên đăng nhập')).fill(ten);
  await page.getByLabel(nhan('Mật khẩu')).fill(mk);
  await page.getByRole('button', { name: 'Đăng nhập' }).click();
}

/** Mo trang chi tiet SKU va chon dung size cua SKU (mau da duoc chon san theo SKU tren URL) */
export async function moVaChonSize(page: Page, sku: Sku) {
  await page.goto(`/san-pham/${sku.maSp}`);
  await expect(page.getByRole('heading', { level: 1, name: sku.tenSp })).toBeVisible();
  await page.locator('label', { has: page.locator('input[name="size"]') }).filter({ hasText: new RegExp(`^${sku.kichCo}`) }).click();
  await expect(page.getByText(/Còn hàng|Chỉ còn \d+ sản phẩm/)).toBeVisible();
}

/** SDT di dong moi (10 so, 09...) chua dung trong CSDL */
export const sdtMoi = (duoi = '') => `09${String(Date.now()).slice(-8 + duoi.length)}${duoi}`;

/** v3: xac thuc SDT bang OTP tren giao dien (che do demo: ma hien tren trang) */
export async function xacThucOtpUI(page: Page, sdt: string) {
  await page.getByLabel(nhan('Số điện thoại')).fill(sdt);
  await page.getByRole('button', { name: 'Gửi mã OTP' }).click();
  const demo = page.getByText(/mã OTP là \d{6}/);
  await expect(demo).toBeVisible();
  const ma = ((await demo.textContent()) ?? '').match(/(\d{6})/)![1];
  await page.getByLabel(nhan('Mã OTP')).fill(ma);
  await page.getByRole('button', { name: 'Xác nhận' }).click();
  await expect(page.getByLabel(nhan('Mã OTP'))).toHaveCount(0); // xac thuc xong: o nhap ma bien mat
}

/** v3: xac thuc OTP qua API (chuan bi du lieu), tra ve context mang token phien vang lai */
export async function apiVangLai(sdt: string) {
  const ctx = await request.newContext({ baseURL: API });
  const gui = await ctx.post('/api/vang-lai/otp', { data: { sdt, mucDich: 'MUA_HANG' } });
  expect(gui.ok(), await gui.text()).toBeTruthy();
  const { maDemo } = await gui.json();
  const xt = await ctx.post('/api/vang-lai/xac-thuc', { data: { sdt, ma: maDemo } });
  expect(xt.ok(), await xt.text()).toBeTruthy();
  const phien = await xt.json();
  await ctx.dispose();
  const api = await request.newContext({ baseURL: API, extraHTTPHeaders: { Authorization: `Bearer ${phien.token}` } });
  return { api, phien };
}

export async function soTrenIconGio(page: Page) {
  const link = page.getByRole('link', { name: /^Giỏ hàng/ }).first();
  const nhan = (await link.getAttribute('aria-label')) ?? '';
  return Number(nhan.match(/(\d+) sản phẩm/)?.[1] ?? 0);
}
