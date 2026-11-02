import { test, expect } from '@playwright/test';
import { skuConHang } from './helpers';

// Moi trang chinh: khong tran ngang, khong loi JS, co tieu de h1 (tru trang chu dung hero)
test.describe('Responsive & lỗi trang', () => {
  for (const w of [375, 768, 1024, 1440]) {
    test(`không tràn ngang và không lỗi JS ở ${w}px`, async ({ page }) => {
      const sku = await skuConHang(1);
      const loi: string[] = [];
      page.on('pageerror', (e) => loi.push(e.message));
      page.on('console', (m) => { if (m.type() === 'error' && !/Failed to load resource/.test(m.text())) loi.push(m.text()); });
      await page.setViewportSize({ width: w, height: 900 });
      for (const url of ['/', '/san-pham', '/danh-muc/LSP01', `/san-pham/${sku.maSp}`, '/gio-hang', '/dang-nhap', '/dang-ky', '/huong-dan-chon-size', '/chinh-sach-doi-tra']) {
        await page.goto(url);
        await expect(page.getByRole('heading', { level: 1 }).first()).toBeVisible();
        const tran = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
        expect(tran, `${url} tràn ngang ${tran}px`).toBeLessThanOrEqual(0);
      }
      expect(loi, loi.join('\n')).toEqual([]);
    });
  }

  test('menu di động mở được và đi tới danh mục', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 800 });
    await page.goto('/');
    await page.getByRole('button', { name: 'Mở menu' }).click();
    await page.getByRole('dialog').getByRole('link', { name: 'Áo khoác' }).click();
    await expect(page).toHaveURL(/\/danh-muc\/LSP03/);
    await expect(page.getByRole('heading', { level: 1, name: 'Áo khoác' })).toBeVisible();
  });
});
