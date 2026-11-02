import { test, expect } from '@playwright/test';
import { skuConHang } from './helpers';

test.describe('Danh mục & sản phẩm', () => {
  test('trang chủ có danh mục, sản phẩm và mở được chi tiết', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByRole('heading', { name: 'Mua theo danh mục' })).toBeVisible();
    const the = page.locator('article').first();
    await expect(the).toBeVisible();
    const ten = (await the.locator('h3').innerText()).trim();
    await the.click();
    await expect(page.getByRole('heading', { level: 1 })).toHaveText(ten);
  });

  test('lọc theo danh mục, tìm kiếm có kết quả và không có kết quả', async ({ page }) => {
    await page.goto('/danh-muc/LSP04');
    await expect(page.getByRole('heading', { level: 1, name: 'Quần jean' })).toBeVisible();
    await expect(page.locator('article').first()).toBeVisible();

    await page.goto('/');
    const o = page.getByRole('searchbox').first();
    await o.fill('jeans');
    await o.press('Enter');
    await expect(page).toHaveURL(/\/tim-kiem\?q=jeans/);
    await expect(page.locator('article').first()).toBeVisible();

    await page.goto('/tim-kiem?q=khongcosanphamnaoca');
    await expect(page.getByText(/Không tìm thấy/i).first()).toBeVisible();
  });

  test('chi tiết: đổi màu, chọn size, xem tồn kho, đổi ảnh', async ({ page }) => {
    const sku = await skuConHang();
    await page.goto(`/san-pham/${sku.maSp}`);
    await expect(page.getByRole('heading', { level: 1, name: sku.tenSp })).toBeVisible();
    await expect(page.getByText(`Màu sắc: ${sku.mauSac}`)).toBeVisible();
    await expect(page.getByText('Chọn kích cỡ để xem tình trạng hàng')).toBeVisible();

    // bam them vao gio khi chua chon size -> bao loi
    await page.getByRole('button', { name: 'Thêm vào giỏ hàng' }).click();
    await expect(page.getByText('Vui lòng chọn kích cỡ.')).toBeVisible();

    await page.locator('label', { has: page.locator('input[name="size"]') }).filter({ hasText: new RegExp(`^${sku.kichCo}`) }).click();
    await expect(page.getByText(/Còn hàng|Chỉ còn \d+ sản phẩm/)).toBeVisible();
    await expect(page.getByText(`Kích cỡ: ${sku.kichCo}`)).toBeVisible();

    const thumbs = page.getByRole('tab');
    if ((await thumbs.count()) > 1) {
      await thumbs.nth(1).click();
      await expect(thumbs.nth(1)).toHaveAttribute('aria-selected', 'true');
    }
    // doi mau khac (neu co)
    const mau = page.locator('input[name="mau"]');
    if ((await mau.count()) > 1) {
      await page.locator('label:has(input[name="mau"])').nth(1).click();
      await expect(page.getByText(`Màu sắc: ${sku.mauSac}`)).toHaveCount(0);
    }
    await expect(page.getByRole('heading', { name: 'Sản phẩm cùng danh mục' })).toBeVisible();
  });

  test('mã sản phẩm không tồn tại -> thông báo không tìm thấy', async ({ page }) => {
    await page.goto('/san-pham/SP99999');
    await expect(page.getByRole('heading', { level: 1, name: 'Không tìm thấy sản phẩm' })).toBeVisible();
  });

  test('đường dẫn sai -> trang 404', async ({ page }) => {
    await page.goto('/khong-co-trang-nay');
    await expect(page.getByRole('heading', { level: 1, name: 'Không tìm thấy trang' })).toBeVisible();
  });
});
