import { test, expect } from '@playwright/test';
import { skuConHang, soTrenIconGio } from './helpers';

// Yeu cau 9: thao tac bang ban phim
test.describe('Phím tắt bàn phím', () => {
  test('? mở bảng phím tắt, / vào ô tìm, g c tới giỏ, g h về trang chủ', async ({ page }) => {
    await page.goto('/san-pham');
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();

    await page.keyboard.press('Shift+?');
    const bang = page.getByRole('dialog', { name: 'Phím tắt' });
    await expect(bang).toBeVisible();
    await expect(bang.getByText('Đưa con trỏ vào ô tìm kiếm')).toBeVisible();
    await page.keyboard.press('Escape');
    await expect(bang).toBeHidden();

    await page.keyboard.press('/');
    const tim = page.getByRole('searchbox', { name: 'Tìm kiếm sản phẩm' }).first();
    await expect(tim).toBeFocused();
    // dang go trong o tim: phim tat khong chay
    await page.keyboard.type('gc');
    await expect(page).toHaveURL(/\/san-pham$/);
    await tim.fill('');
    await page.keyboard.press('Escape');
    await tim.blur();

    await page.keyboard.press('g');
    await page.keyboard.press('c');
    await expect(page).toHaveURL(/\/gio-hang$/);
    await page.keyboard.press('g');
    await page.keyboard.press('h');
    await expect(page).toHaveURL(/\/$/);
  });

  test('tìm kiếm 1 ký tự bị chặn, Enter tìm khi đủ 2 ký tự', async ({ page }) => {
    await page.goto('/');
    const tim = page.getByRole('searchbox', { name: 'Tìm kiếm sản phẩm' }).first();
    await tim.fill('a');
    await tim.press('Enter');
    await expect(page.getByText('Nhập ít nhất 2 ký tự')).toBeVisible();
    await expect(page).toHaveURL(/\/$/);
    await tim.fill('áo');
    await tim.press('Enter');
    await expect(page).toHaveURL(/\/tim-kiem\?q=/);
    await expect(tim).toHaveValue('áo');
  });

  test('chi tiết: chọn size bằng bàn phím, phím a thêm vào giỏ', async ({ page }) => {
    const sku = await skuConHang(3);
    await page.goto(`/san-pham/${sku.maSp}`);
    await expect(page.getByRole('heading', { level: 1, name: sku.tenSp })).toBeVisible();
    await page.keyboard.press('a'); // chua chon size -> bao loi
    await expect(page.getByText('Vui lòng chọn kích cỡ.')).toBeVisible();
    await page.locator('label', { has: page.locator('input[name="size"]') }).filter({ hasText: new RegExp(`^${sku.kichCo}`) }).click();
    await page.locator('body').click({ position: { x: 1, y: 1 } });
    await page.keyboard.press('a');
    await expect.poll(() => soTrenIconGio(page)).toBeGreaterThan(0);
  });
});
