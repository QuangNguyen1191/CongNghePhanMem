import { test, expect } from '@playwright/test';
import { admin, anDanh, ctxCho, dangNhap, khach, loi, ok, RUN, sdtMoi, BASE, dangKy } from './helpers';
import { request } from '@playwright/test';

test.describe('TaiKhoan - dang nhap / dang ky / doi mat khau', () => {
  test('dang nhap dung noi tra token + thong tin phien', async () => {
    const api = await anDanh();
    const nv = await ok(await dangNhap(api, 'thungan1', 'web-staff'));
    expect(nv).toMatchObject({ tenDangNhap: 'thungan1', vaiTro: 'NHAN_VIEN', maNv: expect.any(String), maKh: null });
    expect(nv.token.split('.')).toHaveLength(3);

    const ad = await ok(await dangNhap(api, 'quanly', 'app-admin'));
    expect(ad.vaiTro).toBe('ADMIN');

    const kh = await ok(await dangNhap(api, 'khach01', 'web-ban-hang'));
    expect(kh).toMatchObject({ vaiTro: 'KHACH_HANG', maKh: 'KH001', maNv: null });
  });

  test('dang nhap sai noi / sai mat khau / bi khoa -> 400', async () => {
    const api = await anDanh();
    await loi(await dangNhap(api, 'khach01', 'web-staff'), 400, /nhân viên/);
    await loi(await dangNhap(api, 'thungan1', 'web-ban-hang'), 400);
    await loi(await dangNhap(api, 'thungan1', 'app-admin'), 400, /ADMIN/);
    await loi(await dangNhap(api, 'quanly', 'web-staff', 'sai-mat-khau'), 400, /Sai tên đăng nhập/);
    await loi(await dangNhap(api, 'khong-ton-tai', 'web-staff'), 400, /Sai tên đăng nhập/);
    await loi(await dangNhap(api, 'khach15', 'web-ban-hang'), 400, /khóa/);
  });

  test('body thieu truong / rong -> 400 chu khong phai 500', async () => {
    const api = await anDanh();
    const r1 = await api.post('/api/tai-khoan/dang-nhap/web-staff', { data: {} });
    expect(r1.status()).toBe(400);
    const r2 = await api.post('/api/tai-khoan/dang-nhap/web-staff', { data: { tenDangNhap: null, matKhau: null } });
    expect(r2.status()).toBe(400);
  });

  test('dang ky khach moi -> dang nhap web ban hang duoc, trung ten -> 400', async () => {
    const api = await anDanh();
    const ten = `kh${RUN}`;
    const p = await ok(await dangKy(api, { tenDangNhap: ten, matKhau: 'abc123', khachHang: { hoTen: 'Khách Test', sdt: sdtMoi(1), email: `${ten}@test.vn` } }));
    expect(p).toMatchObject({ tenDangNhap: ten, vaiTro: 'KHACH_HANG', maKh: expect.stringMatching(/^KH/) });
    expect(p.token).toBeTruthy();
    await ok(await dangNhap(api, ten, 'web-ban-hang', 'abc123'));

    await loi(await dangKy(api, { tenDangNhap: ten, matKhau: 'abc123', khachHang: { hoTen: 'X' } }), 400, /đã tồn tại/);
    await loi(await dangKy(api, { tenDangNhap: 'ab', matKhau: 'abc123', khachHang: { hoTen: 'X' } }), 400, /4 ký tự/);
    await loi(await dangKy(api, { tenDangNhap: `x${ten}`, matKhau: '123', khachHang: { hoTen: 'X' } }), 400, /6 ký tự/);
    await loi(await dangKy(api, { tenDangNhap: `y${ten}`, matKhau: '123456', khachHang: { hoTen: '' } }), 400, /họ tên/);
    // SDT da co tai khoan
    await loi(await dangKy(api, { tenDangNhap: `z${ten}`, matKhau: '123456', khachHang: { hoTen: 'Z', sdt: sdtMoi(1) } }), 400, /Số điện thoại/);
  });

  test('dang ky voi SDT khach tai quay -> gan tai khoan vao khach cu', async () => {
    const ad = await admin();
    const sdt = sdtMoi(2);
    const khCu = await ok(await ad.post('/api/khach-hang', { data: { hoTen: 'Khách quầy', sdt } }));
    expect(khCu.maTk).toBeNull();
    const api = await anDanh();
    const p = await ok(await dangKy(api, { tenDangNhap: `quay${RUN}`, matKhau: '123456', khachHang: { hoTen: 'Khách quầy online', sdt } }));
    expect(p.maKh).toBe(khCu.maKh);
  });

  test('doi mat khau: sai mat khau cu 400, dung -> dang nhap bang mat khau moi', async () => {
    const api = await anDanh();
    const ten = `dmk${RUN}`;
    const p = await ok(await dangKy(api, { tenDangNhap: ten, matKhau: '111111', khachHang: { hoTen: 'Đổi MK' } }));
    const kh = await request.newContext({ baseURL: BASE, extraHTTPHeaders: { Authorization: `Bearer ${p.token}` } });
    await loi(await kh.post('/api/tai-khoan/doi-mat-khau', { data: { matKhauCu: 'sai', matKhauMoi: '222222' } }), 400, /cũ không đúng/);
    await loi(await kh.post('/api/tai-khoan/doi-mat-khau', { data: { matKhauCu: '111111', matKhauMoi: '22' } }), 400, /6 ký tự/);
    const r = await ok(await kh.post('/api/tai-khoan/doi-mat-khau', { data: { matKhauCu: '111111', matKhauMoi: '222222' } }));
    expect(r.thongBao).toBeTruthy();
    await ok(await dangNhap(api, ten, 'web-ban-hang', '222222'));
    await loi(await dangNhap(api, ten, 'web-ban-hang', '111111'), 400);
  });

  test('doi mat khau khong co token -> 401', async () => {
    const api = await anDanh();
    expect((await api.post('/api/tai-khoan/doi-mat-khau', { data: { matKhauCu: 'a', matKhauMoi: 'b' } })).status()).toBe(401);
  });

  test('admin: danh sach tai khoan khong lo hash mat khau', async () => {
    const ad = await admin();
    const ds = await ok(await ad.get('/api/tai-khoan'));
    expect(ds.length).toBeGreaterThan(10);
    for (const t of ds) expect(t.matKhau ?? '').toBe('');
  });

  test('admin: khoa / mo / dat lai mat khau', async () => {
    const api = await anDanh();
    const ten = `khoa${RUN}`;
    const p = await ok(await dangKy(api, { tenDangNhap: ten, matKhau: '123456', khachHang: { hoTen: 'Bị khóa' } }));
    const ad = await admin();
    expect((await ad.post(`/api/tai-khoan/${p.maTk}/khoa`)).status()).toBe(204);
    await loi(await dangNhap(api, ten, 'web-ban-hang'), 400, /khóa/);
    expect((await ad.post(`/api/tai-khoan/${p.maTk}/mo`)).status()).toBe(204);
    await ok(await dangNhap(api, ten, 'web-ban-hang'));

    const { matKhauTam } = await ok(await ad.post(`/api/tai-khoan/${p.maTk}/dat-lai-mat-khau`));
    expect(matKhauTam).toMatch(/^\d{6}$/);
    await ok(await dangNhap(api, ten, 'web-ban-hang', matKhauTam));

    await loi(await ad.post('/api/tai-khoan/TK999/khoa'), 404);
    await loi(await ad.post('/api/tai-khoan/TK999/dat-lai-mat-khau'), 404);
  });

  test('token cua khach da bi khoa khong dung duoc nua', async () => {
    const api = await anDanh();
    const ten = `tkk${RUN}`;
    const p = await ok(await dangKy(api, { tenDangNhap: ten, matKhau: '123456', khachHang: { hoTen: 'Khóa token' } }));
    const kh = await request.newContext({ baseURL: BASE, extraHTTPHeaders: { Authorization: `Bearer ${p.token}` } });
    await ok(await kh.get('/api/khach-hang/toi'));
    const ad = await admin();
    expect((await ad.post(`/api/tai-khoan/${p.maTk}/khoa`)).status()).toBe(204);
    // Tai khoan bi khoa thi khong duoc dung token cu de dat hang / xem ho so
    expect([401, 403]).toContain((await kh.get('/api/khach-hang/toi')).status());
  });

  test('nhan vien / khach khong goi duoc API quan ly tai khoan', async () => {
    expect((await (await ctxCho('thungan1', 'web-staff')).get('/api/tai-khoan')).status()).toBe(403);
    expect((await (await khach()).post('/api/tai-khoan/TK101/khoa')).status()).toBe(403);
    expect((await (await anDanh()).get('/api/tai-khoan')).status()).toBe(401);
  });
});
