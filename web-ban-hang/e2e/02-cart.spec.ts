import { test, expect } from '@playwright/test';
import { dangNhapUI, moVaChonSize, skuConHang, soTrenIconGio, xoaGioServer } from './helpers';

test.describe('Giỏ hàng', () => {
  test('khách chưa đăng nhập: thêm, sửa số lượng, xóa', async ({ page }) => {
    const sku = await skuConHang(3);
    await moVaChonSize(page, sku);
    await page.getByRole('button', { name: 'Thêm vào giỏ hàng' }).click();
    await expect(page.getByText(/Đã thêm vào giỏ/)).toBeVisible();
    await expect.poll(() => soTrenIconGio(page)).toBe(1);

    await page.goto('/gio-hang');
    await expect(page.getByRole('heading', { level: 1 })).toContainText('Giỏ hàng');
    const dong = page.getByRole('list', { name: 'Sản phẩm trong giỏ' }).getByRole('listitem');
    await expect(dong).toHaveCount(1);
    await expect(dong.getByRole('link', { name: sku.tenSp })).toBeVisible();
    await dong.getByRole('button', { name: /Tăng/ }).click();
    await expect.poll(() => soTrenIconGio(page)).toBe(2);
    await expect(page.getByRole('button', { name: 'Đăng nhập để thanh toán' })).toBeVisible();

    // tai lai trang: gio tam van con
    await page.reload();
    await expect(dong).toHaveCount(1);

    await dong.getByRole('button', { name: /^Xóa / }).click();
    await expect(page.getByText('Giỏ hàng đang trống')).toBeVisible();
  });

  test('không cho thêm vượt tồn kho', async ({ page }) => {
    const sku = await skuConHang(1);
    await moVaChonSize(page, sku);
    const o = page.getByRole('spinbutton', { name: 'Số lượng' });
    await o.fill(String(sku.soLuong + 5));
    await o.blur();
    await expect(o).toHaveValue(String(sku.soLuong));
  });

  test('giỏ tạm được gộp vào tài khoản khi đăng nhập ở bước thanh toán', async ({ page }) => {
    await xoaGioServer('khach02');
    const sku = await skuConHang(3);
    await moVaChonSize(page, sku);
    await page.getByRole('button', { name: 'Mua ngay' }).click();
    await expect(page).toHaveURL(/\/dang-nhap\?tiep=%2Fthanh-toan/);
    await expect(page.getByText(/Giỏ hàng tạm của bạn sẽ được giữ lại/)).toBeVisible();
    await dangNhapUI(page, 'khach02');
    await expect(page).toHaveURL(/\/thanh-toan$/);
    await expect(page.getByRole('heading', { level: 1, name: 'Thanh toán' })).toBeVisible();
    await expect(page.getByLabel('Đơn hàng').getByText(sku.tenSp)).toBeVisible();
    await xoaGioServer('khach02');
  });
});
