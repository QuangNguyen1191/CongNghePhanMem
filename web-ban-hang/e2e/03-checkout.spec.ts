import { test, expect } from '@playwright/test';
import { dangNhapUI, nhan, moVaChonSize, skuConHang, soTrenIconGio, xoaGioServer } from './helpers';

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
    await ma.fill('WELCOME10');
    await page.getByRole('button', { name: 'Áp dụng' }).click();
    await expect(page.getByText('Mã WELCOME10', { exact: true })).toBeVisible();

    // so dien thoai sai -> bao loi tai cho, khong gui
    const sdt = page.getByLabel(nhan('Số điện thoại'));
    const sdtCu = await sdt.inputValue();
    await sdt.fill('12ab');
    await page.getByRole('button', { name: 'Đặt hàng' }).click();
    await expect(page.getByText('Số điện thoại gồm 9 đến 11 chữ số')).toBeVisible();
    await sdt.fill(sdtCu || '0912345678');

    await page.getByLabel(/Thanh toán khi nhận hàng/).check();
    await page.getByRole('button', { name: 'Đặt hàng' }).click();

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
    await page.getByRole('dialog').getByRole('button', { name: 'Hủy đơn' }).click();
    await expect(page.getByText('Đơn hàng đã được hủy.')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Hủy đơn hàng' })).toHaveCount(0);
  });

  test('giỏ trống -> trang thanh toán báo giỏ trống', async ({ page }) => {
    await page.goto('/dang-nhap?tiep=%2Fthanh-toan');
    await dangNhapUI(page, 'khach01');
    await expect(page.getByText('Giỏ hàng đang trống')).toBeVisible();
  });
});
