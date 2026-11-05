import { test, expect } from '@playwright/test';
import { apiDangNhap, dangNhapUI, MAT_KHAU, nhan, moVaChonSize, skuConHang, soTrenIconGio, xoaGioServer } from './helpers';

test.describe('Thanh toán & đơn hàng', () => {
  test.beforeEach(async () => { await xoaGioServer('khach01'); });
  test.afterEach(async () => { await xoaGioServer('khach01'); });

  test('đặt hàng COD có mã giảm giá, xem đơn, hủy đơn', async ({ page }) => {
    await page.goto('/dang-nhap');
    await dangNhapUI(page, 'khach01');
    await expect(page).toHaveURL('/');

    const sku = await skuConHang(3);
    await moVaChonSize(page, sku);
    await page.getByRole('button', { name: 'Thêm vào giỏ hàng' }).click();
    await expect.poll(() => soTrenIconGio(page)).toBe(1);

    await page.goto('/gio-hang');
    await page.getByRole('button', { name: 'Thanh toán' }).click();
    await expect(page).toHaveURL(/\/thanh-toan$/);

    // dia chi lay san tu ho so
    await expect(page.getByLabel(nhan('Người nhận'))).not.toHaveValue('');
    await expect(page.getByLabel(nhan('Địa chỉ giao hàng'))).not.toHaveValue('');

    // ma sai -> loi, ma dung -> giam
    const ma = page.getByLabel(nhan('Mã giảm giá'));
    await ma.fill('SAIMA123');
    await page.getByRole('button', { name: 'Áp dụng' }).click();
    await expect(page.getByLabel('Đơn hàng').getByRole('alert')).toBeVisible();
    // Enter trong o ma = ap ma, KHONG duoc dat hang
    await ma.fill('WELCOME10');
    await ma.press('Enter');
    await expect(page.getByText('Mã WELCOME10', { exact: true })).toBeVisible();
    await expect(page).toHaveURL(/\/thanh-toan$/);
    await expect(ma).toHaveAttribute('readonly', '');

    // so dien thoai sai -> bao loi tai cho, khong gui
    const sdt = page.getByLabel(nhan('Số điện thoại'));
    const sdtCu = await sdt.inputValue();
    await sdt.fill('12ab');
    await page.getByRole('button', { name: 'Đặt hàng' }).click();
    await expect(page.getByText('Số điện thoại gồm 9 đến 11 chữ số')).toBeVisible();
    await sdt.fill(sdtCu || '0912345678');

    await page.getByLabel(/Thanh toán khi nhận hàng/).check();
    // phim tat Ctrl + Enter = dat hang
    await page.getByLabel(nhan('Người nhận')).press('Control+Enter');

    await expect(page).toHaveURL(/\/tai-khoan\/don-hang\/DH\d+/);
    await expect(page.getByText('Đặt hàng thành công!')).toBeVisible();
    await expect(page.getByText('Chờ xác nhận').first()).toBeVisible();
    await expect(page.getByText('WELCOME10', { exact: true })).toBeVisible();
    await expect.poll(() => soTrenIconGio(page)).toBe(0);
    const maDon = page.url().split('/').pop()!;

    // don vua dat co trong danh sach
    await page.goto('/tai-khoan/don-hang');
    await expect(page.getByRole('link', { name: new RegExp(maDon) })).toBeVisible();

    // huy don
    await page.getByRole('link', { name: new RegExp(maDon) }).click();
    await page.getByRole('button', { name: 'Hủy đơn hàng' }).click();
    // v3: bat buoc chon ly do
    await page.getByRole('dialog').getByRole('button', { name: 'Hủy đơn' }).click();
    await expect(page.getByRole('dialog').getByText('Vui lòng chọn lý do hủy')).toBeVisible();
    await page.getByRole('dialog').getByLabel('Đặt nhầm sản phẩm / size / màu').check();
    await page.getByRole('dialog').getByRole('button', { name: 'Hủy đơn' }).click();
    await expect(page.getByText('Đơn hàng đã được hủy.')).toBeVisible();
    await expect(page.getByText(/Bạn đã hủy đơn.*Lý do: Đặt nhầm sản phẩm/)).toBeVisible();
    await expect(page.getByRole('button', { name: 'Hủy đơn hàng' })).toHaveCount(0);
  });

  test('đơn đã xác nhận: gửi yêu cầu hủy, cửa hàng duyệt -> đã hủy, hoàn tiền', async ({ page }) => {
    const sku = await skuConHang(3);
    const kh = await apiDangNhap('khach01');
    await kh.post('/api/gio-hang', { data: { maSp: sku.maSp, soLuong: 1 } });
    const don = await (await kh.post('/api/don-hang', { data: { diaChiGiao: '1 Tràng Tiền, Hà Nội', sdtNhan: '0912345678', phuongThucTt: 'CHUYEN_KHOAN' } })).json();
    const nv = await apiDangNhap('banhangonline', MAT_KHAU, 'web-staff');
    expect((await nv.post(`/api/don-hang/${don.maDon}/xac-nhan`)).ok()).toBeTruthy();

    await page.goto('/dang-nhap');
    await dangNhapUI(page, 'khach01');
    await expect(page).toHaveURL('/');
    await page.goto(`/tai-khoan/don-hang/${don.maDon}`);
    await page.getByRole('button', { name: 'Yêu cầu hủy đơn' }).click();
    const hop = page.getByRole('dialog');
    await hop.getByLabel('Lý do khác').check();
    await hop.getByLabel('Lý do của bạn').fill('Mua tại cửa hàng rồi');
    await hop.getByRole('button', { name: 'Gửi yêu cầu' }).click();
    await expect(page.getByText(/Bạn đã gửi yêu cầu hủy.*Mua tại cửa hàng rồi/)).toBeVisible();
    await expect(page.getByRole('button', { name: 'Yêu cầu hủy đơn' })).toHaveCount(0);

    expect((await nv.post(`/api/don-hang/${don.maDon}/duyet-huy`)).status()).toBe(204);
    await page.reload();
    await expect(page.getByText('Đơn hàng đã được hủy.')).toBeVisible();
    await expect(page.getByText(/Cửa hàng đã đồng ý yêu cầu hủy của bạn/)).toBeVisible();
    await expect(page.getByText(/Cửa hàng sẽ hoàn .*₫/)).toBeVisible();
    await kh.dispose(); await nv.dispose();
  });

  test('giỏ trống -> trang thanh toán báo giỏ trống', async ({ page }) => {
    await page.goto('/dang-nhap?tiep=%2Fthanh-toan');
    await dangNhapUI(page, 'khach01');
    await expect(page.getByText('Giỏ hàng đang trống')).toBeVisible();
  });
});
