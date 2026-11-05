import { test, expect } from '@playwright/test';
import { dangNhapUI, nhan, sdtMoi, xacThucOtpUI } from './helpers';

test.describe('Tài khoản', () => {
  test('trang cần đăng nhập chuyển về đăng nhập rồi quay lại', async ({ page }) => {
    await page.goto('/tai-khoan/don-hang');
    await expect(page).toHaveURL(/\/dang-nhap\?tiep=%2Ftai-khoan%2Fdon-hang/);
    await dangNhapUI(page, 'khach01');
    await expect(page).toHaveURL(/\/tai-khoan\/don-hang$/);
    await expect(page.getByRole('heading', { level: 1, name: 'Đơn hàng của tôi' })).toBeVisible();
  });

  test('sai mật khẩu và tài khoản bị khóa hiện thông báo', async ({ page }) => {
    await page.goto('/dang-nhap');
    await dangNhapUI(page, 'khach01', 'saimatkhau');
    await expect(page.getByRole('alert').filter({ hasText: 'Sai tên đăng nhập hoặc mật khẩu' })).toBeVisible();
    await dangNhapUI(page, 'khach15');
    await expect(page.getByRole('alert').filter({ hasText: /bị khóa/ })).toBeVisible();
  });

  test('đăng ký, sửa hồ sơ, đổi mật khẩu, đăng xuất, đăng nhập lại', async ({ page }) => {
    const ten = `e2e${Date.now().toString(36)}`;
    await page.goto('/dang-ky');
    await page.getByRole('button', { name: 'Đăng ký' }).click();
    await expect(page.getByText('Tên đăng nhập phải có ít nhất 4 ký tự')).toBeVisible();
    await expect(page.getByText('Vui lòng nhập họ tên')).toBeVisible();

    await page.getByLabel(nhan('Tên đăng nhập')).fill(ten);
    await page.getByLabel(nhan('Mật khẩu')).fill('123456');
    await page.getByLabel(nhan('Nhập lại mật khẩu')).fill('123456');
    await page.getByLabel(nhan('Họ và tên')).fill('Khách Kiểm Thử');
    // v3: SDT bat buoc, phai xac thuc OTP
    await page.getByRole('button', { name: 'Đăng ký' }).click();
    await expect(page.getByText('Vui lòng xác thực số điện thoại bằng mã OTP')).toBeVisible();
    await page.getByLabel(nhan('Số điện thoại')).fill('0123');
    await page.getByRole('button', { name: 'Gửi mã OTP' }).click();
    await expect(page.getByText(/Số điện thoại di động gồm 10 chữ số/)).toBeVisible();
    const sdtDk = sdtMoi('1');
    await xacThucOtpUI(page, sdtDk);
    await expect(page.getByText(new RegExp(`${sdtDk} đã xác thực`))).toBeVisible();
    await page.getByLabel(nhan('Địa chỉ')).fill('12 Nguyễn Huệ, Quận 1');
    await page.getByLabel(nhan('Tỉnh / thành phố')).fill('TP Hồ Chí Minh');
    await page.getByRole('button', { name: 'Đăng ký' }).click();
    await expect(page).toHaveURL('/');

    await page.goto('/tai-khoan');
    await expect(page.getByText('Đồng', { exact: true })).toBeVisible();
    await expect(page.getByLabel(nhan('Họ và tên'))).toHaveValue('Khách Kiểm Thử');
    await page.getByLabel(nhan('Số điện thoại')).fill('12');
    await page.getByRole('button', { name: 'Lưu thay đổi' }).click();
    await expect(page.getByText('Số điện thoại gồm 9 đến 11 chữ số')).toBeVisible();
    const sdt = sdtMoi('2');
    await page.getByLabel(nhan('Số điện thoại')).fill(sdt);
    await page.getByRole('button', { name: 'Lưu thay đổi' }).click();
    await expect(page.getByText('Đã cập nhật thông tin')).toBeVisible();
    await page.reload();
    await expect(page.getByLabel(nhan('Số điện thoại'))).toHaveValue(sdt);
    await expect(page.getByLabel(nhan('Địa chỉ'))).toHaveValue('12 Nguyễn Huệ, Quận 1');

    await page.getByRole('link', { name: 'Đổi mật khẩu' }).click();
    await page.getByLabel(nhan('Mật khẩu hiện tại')).fill('sai000');
    await page.getByLabel(nhan('Mật khẩu mới')).fill('654321');
    await page.getByLabel(nhan('Nhập lại mật khẩu mới')).fill('654321');
    await page.getByRole('button', { name: 'Đổi mật khẩu' }).click();
    await expect(page.getByRole('alert').filter({ hasText: 'Mật khẩu cũ không đúng' })).toBeVisible();
    await page.getByLabel(nhan('Mật khẩu hiện tại')).fill('123456');
    await page.getByRole('button', { name: 'Đổi mật khẩu' }).click();
    await expect(page.getByText('Đã đổi mật khẩu')).toBeVisible();

    await page.getByRole('button', { name: 'Đăng xuất' }).click();
    await expect(page).toHaveURL('/');
    await page.goto('/dang-nhap');
    await dangNhapUI(page, ten, '654321');
    await expect(page).toHaveURL('/');
    await expect(page.getByRole('link', { name: /Tài khoản của Khách Kiểm Thử/ })).toBeVisible();
  });

  test('hóa đơn: danh sách, chi tiết, tải PDF', async ({ page }) => {
    await page.goto('/dang-nhap?tiep=%2Ftai-khoan%2Fhoa-don');
    await dangNhapUI(page, 'khach01');
    await expect(page.getByRole('heading', { level: 1, name: 'Hóa đơn' })).toBeVisible();
    const dong = page.getByRole('link', { name: /HD\d+/ }).first();
    await expect(dong).toBeVisible();
    await dong.click();
    await expect(page.getByRole('heading', { level: 1, name: /Hóa đơn HD\d+/ })).toBeVisible();
    const tai = page.waitForEvent('download');
    await page.getByRole('button', { name: 'Tải PDF' }).click();
    expect((await tai).suggestedFilename()).toMatch(/^HoaDon_HD\d+\.pdf$/);
  });

  test('gửi yêu cầu đổi hàng từ hóa đơn còn hạn', async ({ page }) => {
    await page.goto('/dang-nhap?tiep=%2Ftai-khoan%2Fdoi-tra%2Fmoi');
    await dangNhapUI(page, 'khach01');
    await expect(page.getByRole('heading', { level: 1, name: 'Yêu cầu đổi / trả' })).toBeVisible();
    const chonHd = page.getByLabel('Hóa đơn');
    test.skip(!(await chonHd.isVisible().catch(() => false)) || (await chonHd.locator('option').count()) < 2,
      'Không có hóa đơn nào của khach01 còn trong hạn 30 ngày');
    // Moi lan chay test tao 1 phieu CHO_DUYET -> tim hoa don / dong con doi duoc va con size / mau khac
    const doiSang = page.getByLabel('Đổi sang');
    const soHd = await chonHd.locator('option').count();
    let duoc = false;
    for (let i = 1; i < soHd && !duoc; i++) {
      await chonHd.selectOption({ index: i });
      const dongs = page.locator('input[type="checkbox"]:not([disabled])');
      await dongs.first().waitFor({ timeout: 3000 }).catch(() => {});
      for (let j = 0; j < (await dongs.count()) && !duoc; j++) {
        await dongs.nth(j).check();
        if (await doiSang.isVisible().catch(() => false)
          && (await doiSang.locator('option').count()) > 1) duoc = true;
        else await dongs.nth(j).uncheck();
      }
    }
    test.skip(!duoc, 'Không còn sản phẩm nào đổi được (đã yêu cầu hết hoặc không còn size / màu khác)');
    await doiSang.selectOption({ index: 1 });
    await page.getByLabel('Lý do').fill('Muốn đổi size');
    await page.getByRole('button', { name: 'Gửi yêu cầu' }).click();
    await expect(page).toHaveURL(/\/tai-khoan\/doi-tra\/DT\d+/);
    await expect(page.getByText('Chờ duyệt')).toBeVisible();
  });
});
