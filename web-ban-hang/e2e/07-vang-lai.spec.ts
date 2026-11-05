import { test, expect } from '@playwright/test';
import { apiVangLai, moVaChonSize, nhan, sdtMoi, skuConHang, soTrenIconGio, xacThucOtpUI } from './helpers';

// v3: khach vang lai mua hang khong can tai khoan (xac thuc SDT bang OTP gia lap) + uu dai thanh vien moi
test.describe('Khách mua không cần tài khoản', () => {
  test('đặt hàng không đăng nhập -> tạo tài khoản nhanh -> đơn được gắn, có ưu đãi thành viên mới', async ({ page }) => {
    const sku = await skuConHang(3);
    const sdt = sdtMoi('3');
    await moVaChonSize(page, sku);
    await page.getByRole('button', { name: 'Thêm vào giỏ hàng' }).click();
    await expect.poll(() => soTrenIconGio(page)).toBe(1);

    await page.goto('/gio-hang');
    await expect(page.getByRole('button', { name: 'Đăng nhập để thanh toán' })).toBeVisible();
    await page.getByRole('button', { name: 'Mua không cần tài khoản' }).click();
    await expect(page).toHaveURL(/\/thanh-toan$/);
    await expect(page.getByText(/giảm 10% cho 3 đơn đầu tiên/)).toBeVisible();

    // chua xac thuc SDT -> khong dat duoc
    await page.getByLabel(nhan('Họ và tên')).fill('Khách Vãng Lai E2E');
    await page.getByLabel(nhan('Địa chỉ giao hàng')).fill('15 Lê Lợi, Quận 1, TP Hồ Chí Minh');
    await page.getByRole('button', { name: 'Đặt hàng' }).click();
    await expect(page.getByText('Vui lòng xác thực số điện thoại bằng mã OTP')).toBeVisible();

    // ma sai -> bao so lan con lai
    await page.getByLabel(nhan('Số điện thoại')).fill(sdt);
    await page.getByRole('button', { name: 'Gửi mã OTP' }).click();
    await expect(page.getByText(/mã OTP là \d{6}/)).toBeVisible();
    const ma = ((await page.getByText(/mã OTP là \d{6}/).textContent()) ?? '').match(/(\d{6})/)![1];
    await page.getByLabel(nhan('Mã OTP')).fill(ma === '000000' ? '111111' : '000000');
    await page.getByRole('button', { name: 'Xác nhận' }).click();
    await expect(page.getByText(/còn \d+ lần/)).toBeVisible();
    await page.getByLabel(nhan('Mã OTP')).fill(ma);
    await page.getByRole('button', { name: 'Xác nhận' }).click();
    await expect(page.getByText(new RegExp(`${sdt} đã xác thực`))).toBeVisible();

    await page.getByLabel(/Thanh toán khi nhận hàng/).check();
    await page.getByRole('button', { name: 'Đặt hàng' }).click();
    await expect(page).toHaveURL(/\/dat-hang-thanh-cong\/DH\d+/);
    const maDon = page.url().split('/').pop()!;
    await expect(page.getByText('Đặt hàng thành công!')).toBeVisible();
    await expect(page.getByRole('heading', { name: `Đơn hàng ${maDon}` })).toBeVisible();
    await expect.poll(() => soTrenIconGio(page)).toBe(0);   // gio tam da xoa

    // tao tai khoan nhanh: SDT da xac thuc, chi can ten dang nhap + mat khau
    const ten = `vl${Date.now().toString(36)}`;
    await page.getByLabel(nhan('Tên đăng nhập')).fill(ten);
    await page.getByLabel(nhan('Mật khẩu')).fill('123456');
    await page.getByLabel(nhan('Nhập lại mật khẩu')).fill('123456');
    await page.getByRole('button', { name: 'Tạo tài khoản' }).click();
    await expect(page).toHaveURL(new RegExp(`/tai-khoan/don-hang/${maDon}$`));
    await expect(page.getByText(/1 đơn mua trước đó đã được gắn vào tài khoản/)).toBeVisible();
    await expect(page.getByRole('heading', { level: 1, name: `Đơn hàng ${maDon}` })).toBeVisible();

    // dat don dau tien bang tai khoan -> uu dai thanh vien moi tu ap dung
    await moVaChonSize(page, sku);
    await page.getByRole('button', { name: 'Mua ngay' }).click();
    await expect(page).toHaveURL(/\/thanh-toan$/);
    await expect(page.getByLabel('Đơn hàng').getByText('Ưu đãi thành viên mới', { exact: true })).toBeVisible();
    await expect(page.getByText(/Bạn còn 3\/3 lượt ưu đãi thành viên mới/)).toBeVisible();
  });

  test('tra cứu đơn bằng SĐT: xem danh sách, chi tiết, hủy đơn chờ xác nhận', async ({ page }) => {
    const sku = await skuConHang(3);
    const sdt = sdtMoi('4');
    const { api: vl, phien } = await apiVangLai(sdt);
    const r = await vl.post('/api/vang-lai/don-hang', {
      data: { hoTen: 'Khách Tra Cứu', diaChiGiao: '20 Hai Bà Trưng, Hoàn Kiếm, Hà Nội', phuongThucTt: 'COD', dongHangs: [{ maSp: sku.maSp, soLuong: 1 }] },
    });
    expect(r.ok(), await r.text()).toBeTruthy();
    const { maDon } = await r.json();
    await vl.dispose();

    // vua nhan OTP luc dat (phai doi 60s moi gui lai duoc) -> dung lai phien do trong tab, nhu khach vua dat xong
    await page.addInitScript((p) => { if (!sessionStorage.getItem('wbh.vang-lai')) sessionStorage.setItem('wbh.vang-lai', p); }, JSON.stringify(phien));
    await page.goto('/');
    await page.getByRole('contentinfo').getByRole('link', { name: 'Tra cứu đơn hàng' }).click();
    await expect(page).toHaveURL(/\/tra-cuu-don$/);
    await expect(page.getByRole('heading', { name: `Đơn hàng của số ${sdt}` })).toBeVisible();
    await page.getByRole('link', { name: new RegExp(maDon) }).click();
    await expect(page.getByRole('heading', { level: 1, name: `Đơn hàng ${maDon}` })).toBeVisible();
    await expect(page.getByText('Khách Tra Cứu')).toBeVisible();
    await page.getByRole('button', { name: 'Hủy đơn hàng' }).click();
    await page.getByRole('dialog').getByLabel('Đổi ý, không muốn mua nữa').check();
    await page.getByRole('dialog').getByRole('button', { name: 'Hủy đơn' }).click();
    await expect(page.getByText('Đơn hàng đã được hủy.')).toBeVisible();

    // so khac khong xem duoc don nay
    await page.getByRole('link', { name: 'Tra cứu đơn', exact: true }).click();
    await page.getByRole('button', { name: 'Tra cứu số khác' }).click();
    await xacThucOtpUI(page, sdtMoi('5'));
    await expect(page.getByText('Số điện thoại này chưa có đơn hàng nào')).toBeVisible();
    await page.goto(`/tra-cuu-don/${maDon}`);
    await expect(page.getByText('Không tìm thấy đơn hàng')).toBeVisible();
  });
});
