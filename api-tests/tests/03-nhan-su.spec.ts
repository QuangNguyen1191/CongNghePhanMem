import { test, expect } from '@playwright/test';
import { admin, anDanh, dangNhap, khach, loi, nhanVien, ok, RUN, sdtMoi } from './helpers';

test.describe('NhanVien (chi ADMIN)', () => {
  test('danh sach + chi tiet + 404', async () => {
    const ad = await admin();
    const ds = await ok(await ad.get('/api/nhan-vien'));
    expect(ds.length).toBeGreaterThanOrEqual(6);
    const nv = await ok(await ad.get(`/api/nhan-vien/${ds[0].maNv}`));
    expect(nv.maNv).toBe(ds[0].maNv);
    await loi(await ad.get('/api/nhan-vien/NV999'), 404);
  });

  test('them nhan vien kem tai khoan -> dang nhap web staff, sua, nghi viec -> khong dang nhap duoc', async () => {
    const ad = await admin();
    const ten = `nv${RUN}`;
    const nv = await ok(await ad.post('/api/nhan-vien', {
      data: { nhanVien: { hoTen: 'NV Test', email: `${ten}@shop.vn`, chucVu: 'Thu ngân', luong: 7000000 },
        tenDangNhap: ten, matKhau: '123456', vaiTro: 'NHAN_VIEN' },
    }));
    expect(nv).toMatchObject({ maNv: expect.stringMatching(/^NV/), trangThai: 'DANG_LAM', tenDangNhap: ten });
    const api = await anDanh();
    await ok(await dangNhap(api, ten, 'web-staff'));

    expect((await ad.put(`/api/nhan-vien/${nv.maNv}`, {
      data: { hoTen: 'NV Test Sửa', email: `${ten}@shop.vn`, chucVu: 'Bán hàng', luong: 8000000 } })).status()).toBe(204);
    const sau = await ok(await ad.get(`/api/nhan-vien/${nv.maNv}`));
    expect(sau).toMatchObject({ hoTen: 'NV Test Sửa', chucVu: 'Bán hàng', luong: 8000000 });

    expect((await ad.post(`/api/nhan-vien/${nv.maNv}/nghi-viec`)).status()).toBe(204);
    expect((await ok(await ad.get(`/api/nhan-vien/${nv.maNv}`))).trangThai).toBe('NGHI_VIEC');
    await loi(await dangNhap(api, ten, 'web-staff'), 400);
  });

  test('validate them / sua nhan vien', async () => {
    const ad = await admin();
    const base = { nhanVien: { hoTen: 'A', email: `v${RUN}@shop.vn` }, tenDangNhap: `v${RUN}`, matKhau: '123456', vaiTro: 'NHAN_VIEN' };
    await loi(await ad.post('/api/nhan-vien', { data: { ...base, vaiTro: 'KHACH_HANG' } }), 400, /Vai trò/);
    await loi(await ad.post('/api/nhan-vien', { data: { ...base, tenDangNhap: 'quanly' } }), 400, /đã tồn tại/);
    await loi(await ad.post('/api/nhan-vien', { data: { ...base, matKhau: '1' } }), 400, /6 ký tự/);
    await loi(await ad.post('/api/nhan-vien', { data: { ...base, nhanVien: { hoTen: 'A', email: 'khong-hop-le' } } }), 400, /Email/);
    await loi(await ad.post('/api/nhan-vien', { data: { ...base, nhanVien: { hoTen: 'A', email: 'a@b.c', luong: -1 } } }), 400, /Lương/);
    await loi(await ad.put('/api/nhan-vien/NV999', { data: { hoTen: 'x', email: `nv999${RUN}@x.vn` } }), 404);
    await loi(await ad.post('/api/nhan-vien/NV999/nghi-viec'), 404);
  });

  test('nhan vien khong vao duoc API nhan vien', async () => {
    expect((await (await nhanVien()).get('/api/nhan-vien')).status()).toBe(403);
    expect((await (await khach()).get('/api/nhan-vien')).status()).toBe(403);
  });
});

test.describe('KhachHang', () => {
  test('khach xem va sua ho so cua minh', async () => {
    const kh = await khach('khach02');
    const toi = await ok(await kh.get('/api/khach-hang/toi'));
    expect(toi.maKh).toBe('KH002');
    expect((await kh.put('/api/khach-hang/toi', {
      data: { ...toi, maKh: 'KH003', diaChi: `Địa chỉ ${RUN}`, hangThanhVien: 'KIM_CUONG', diemTichLuy: 99999 } })).status()).toBe(204);
    const sau = await ok(await kh.get('/api/khach-hang/toi'));
    expect(sau.diaChi).toBe(`Địa chỉ ${RUN}`);
    // khong tu nang hang / diem duoc
    expect(sau.hangThanhVien).toBe(toi.hangThanhVien);
    expect(sau.diemTichLuy).toBe(toi.diemTichLuy);
    // KH003 khong bi sua
    const kh3 = await ok(await (await nhanVien()).get('/api/khach-hang/KH003'));
    expect(kh3.diaChi).not.toBe(`Địa chỉ ${RUN}`);
  });

  test('nhan vien: danh sach, chi tiet, tim theo SDT, them tai quay, sua', async () => {
    const nv = await nhanVien();
    const ds = await ok(await nv.get('/api/khach-hang'));
    expect(ds.length).toBeGreaterThanOrEqual(15);
    await ok(await nv.get('/api/khach-hang/KH001'));
    await loi(await nv.get('/api/khach-hang/KH999'), 404);
    const theoSdt = await ok(await nv.get('/api/khach-hang/theo-sdt/0912000000'));
    expect(theoSdt.maKh).toBe('KH001');
    await loi(await nv.get('/api/khach-hang/theo-sdt/0000000000'), 404);

    const sdt = sdtMoi(3);
    const moi = await ok(await nv.post('/api/khach-hang', { data: { hoTen: '  Khách mới  ', sdt, tinhThanh: 'Hà Nội' } }));
    expect(moi).toMatchObject({ hoTen: 'Khách mới', sdt, maTk: null, hangThanhVien: 'DONG', diemTichLuy: 0 });
    await loi(await nv.post('/api/khach-hang', { data: { hoTen: 'Trùng', sdt } }), 400, /Số điện thoại/);
    await loi(await nv.post('/api/khach-hang', { data: { hoTen: 'SĐT sai', sdt: '12ab' } }), 400, /không hợp lệ/);
    await loi(await nv.post('/api/khach-hang', { data: { hoTen: '' } }), 400, /họ tên/);

    expect((await nv.put(`/api/khach-hang/${moi.maKh}`, { data: { hoTen: 'Khách đã sửa', sdt, tinhThanh: 'Đà Nẵng' } })).status()).toBe(204);
    expect((await ok(await nv.get(`/api/khach-hang/${moi.maKh}`))).tinhThanh).toBe('Đà Nẵng');
    await loi(await nv.put(`/api/khach-hang/${moi.maKh}`, { data: { hoTen: 'x', sdt: '0912000000' } }), 400, /khách hàng khác/);
    await loi(await nv.put('/api/khach-hang/KH999', { data: { hoTen: 'x' } }), 404);
  });

  test('admin sua hang / diem, nhan vien khong duoc', async () => {
    const nv = await nhanVien();
    const moi = await ok(await nv.post('/api/khach-hang', { data: { hoTen: 'Hạng điểm', sdt: sdtMoi(4) } }));
    expect((await nv.put(`/api/khach-hang/${moi.maKh}/hang-diem?hang=VANG&diem=10`)).status()).toBe(403);
    const ad = await admin();
    expect((await ad.put(`/api/khach-hang/${moi.maKh}/hang-diem?hang=VANG&diem=10`)).status()).toBe(204);
    expect(await ok(await ad.get(`/api/khach-hang/${moi.maKh}`))).toMatchObject({ hangThanhVien: 'VANG', diemTichLuy: 10 });
    await loi(await ad.put(`/api/khach-hang/${moi.maKh}/hang-diem?hang=XYZ&diem=1`), 400);
    await loi(await ad.put(`/api/khach-hang/${moi.maKh}/hang-diem?hang=BAC&diem=-1`), 400);
    await loi(await ad.put('/api/khach-hang/KH999/hang-diem?hang=BAC&diem=1'), 404);
    // tra lai dung diem theo hoa don (giu CSDL qua bai kiem tra A6 trong KiemThu_QL_BanThoiTrang.sql)
    expect((await ad.put(`/api/khach-hang/${moi.maKh}/hang-diem?hang=DONG&diem=0`)).status()).toBe(204);
  });

  test('yeu cau 6: hop / giao theo tinh', async () => {
    const nv = await nhanVien();
    const hop = await ok(await nv.get('/api/khach-hang/hai-tinh-hop'));
    expect(hop.length).toBeGreaterThan(0);
    expect(hop.every((k: any) => ['TP Hồ Chí Minh', 'Hà Nội'].includes(k.tinhThanh))).toBeTruthy();
    const hop2 = await ok(await nv.get(`/api/khach-hang/hai-tinh-hop?tinhA=${encodeURIComponent('Đà Nẵng')}&tinhB=${encodeURIComponent('Hà Nội')}`));
    expect(hop2.every((k: any) => ['Đà Nẵng', 'Hà Nội'].includes(k.tinhThanh))).toBeTruthy();
    const giao = await ok(await nv.get('/api/khach-hang/o-tinh-a-giao-tinh-b'));
    expect(Array.isArray(giao)).toBeTruthy();
    expect(giao.every((k: any) => k.tinhThanh === 'TP Hồ Chí Minh')).toBeTruthy();
    const ca2 = await ok(await nv.get('/api/khach-hang/vua-tai-quay-vua-online'));
    expect(ca2.length).toBeGreaterThan(0);
  });

  test('phan quyen khach hang', async () => {
    const kh = await khach();
    expect((await kh.get('/api/khach-hang')).status()).toBe(403);
    expect((await kh.get('/api/khach-hang/KH002')).status()).toBe(403);
    expect((await (await nhanVien()).get('/api/khach-hang/toi')).status()).toBe(403);
    expect((await (await anDanh()).get('/api/khach-hang/toi')).status()).toBe(401);
  });
});
