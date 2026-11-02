import { test, expect } from '@playwright/test';
import { admin, anDanh, khach, loi, nhanVien, ok, RUN, taoSanPhamCoTon } from './helpers';

const homNay = () => new Date().toISOString().slice(0, 10);
const cong = (n: number) => new Date(Date.now() + n * 86400000).toISOString().slice(0, 10);

test.describe('KhuyenMai', () => {
  test('dang chay: cong khai, chi chua KM con han', async () => {
    const ds = await ok(await (await anDanh()).get('/api/khuyen-mai/dang-chay'));
    const h = homNay();
    for (const k of ds) {
      expect(k.ngayBd.slice(0, 10) <= h).toBeTruthy();
      expect(k.ngayKt.slice(0, 10) >= h).toBeTruthy();
    }
  });

  test('tinh tien: voucher, muc hoa don, loi', async () => {
    const ad = await admin();
    const sp = await taoSanPhamCoTon(ad, 20, 500000);
    const api = await anDanh();
    const a = await ok(await api.post('/api/khuyen-mai/tinh-tien', { data: { dongHangs: [{ maSp: sp.maSp, soLuong: 1 }] } }));
    expect(a).toMatchObject({ tamTinh: 500000, tienGiam: 0, tongTien: 500000 });
    // 3 x 500k = 1.5tr >= 1tr -> KM02 giam 5%
    const b = await ok(await api.post('/api/khuyen-mai/tinh-tien', {
      data: { dongHangs: [{ maSp: sp.maSp, soLuong: 2 }, { maSp: sp.maSp.toLowerCase(), soLuong: 1 }] } }));
    expect(b.dongs).toHaveLength(1);
    expect(b).toMatchObject({ tamTinh: 1500000, tienGiam: 75000, tongTien: 1425000, maKm: 'KM02' });
    // voucher thay the muc hoa don, khong cong don
    const c = await ok(await api.post('/api/khuyen-mai/tinh-tien', {
      data: { dongHangs: [{ maSp: sp.maSp, soLuong: 3 }], maVoucher: ' welcome10 ' } }));
    expect(c).toMatchObject({ tienGiam: 150000, tongTien: 1350000, maKm: 'KM03' });
    await loi(await api.post('/api/khuyen-mai/tinh-tien', { data: { dongHangs: [{ maSp: sp.maSp, soLuong: 1 }], maVoucher: 'KHONGCO' } }), 404, /voucher/);
    await loi(await api.post('/api/khuyen-mai/tinh-tien', { data: { dongHangs: [] } }), 400, /Chưa có sản phẩm/);
    await loi(await api.post('/api/khuyen-mai/tinh-tien', { data: { dongHangs: [{ maSp: sp.maSp, soLuong: 0 }] } }), 400, /Số lượng/);
    await loi(await api.post('/api/khuyen-mai/tinh-tien', { data: { dongHangs: [{ maSp: sp.maSp, soLuong: 21 }] } }), 400, /chỉ còn 20/);
    await loi(await api.post('/api/khuyen-mai/tinh-tien', { data: { dongHangs: [{ maSp: 'SP999', soLuong: 1 }] } }), 404);
  });

  test('admin CRUD: SAN_PHAM -> gia khuyen mai ap vao tinh tien; ket thuc -> het ap dung', async () => {
    const ad = await admin();
    const sp = await taoSanPhamCoTon(ad, 10, 100000);
    const km = await ok(await ad.post('/api/khuyen-mai', {
      data: { tenKm: `KM SP ${RUN}`, hinhThuc: 'SAN_PHAM', ngayBd: homNay(), ngayKt: cong(7),
        sanPhams: [{ maSp: sp.maSp, phanTramGiamGia: 20 }] },
    }));
    expect(km.maKm).toMatch(/^KM\d+/);
    expect(km.sanPhams).toHaveLength(1);
    expect((await ok(await ad.get(`/api/khuyen-mai/${km.maKm}`))).tenKm).toBe(`KM SP ${RUN}`);
    expect((await ok(await ad.get('/api/khuyen-mai'))).some((k: any) => k.maKm === km.maKm)).toBeTruthy();
    expect((await ok(await ad.get('/api/khuyen-mai/dang-chay'))).some((k: any) => k.maKm === km.maKm)).toBeTruthy();

    const gia = await ok(await ad.get('/api/san-pham/gia-khuyen-mai'));
    expect(gia[sp.maSp]).toBe(80000);
    const t = await ok(await ad.post('/api/khuyen-mai/tinh-tien', { data: { dongHangs: [{ maSp: sp.maSp, soLuong: 2 }] } }));
    expect(t).toMatchObject({ tamTinh: 160000, tongTien: 160000, maKm: km.maKm });

    // sua: doi % giam
    expect((await ad.put(`/api/khuyen-mai/${km.maKm}`, {
      data: { tenKm: `KM SP ${RUN} sửa`, hinhThuc: 'SAN_PHAM', ngayBd: homNay(), ngayKt: cong(7),
        sanPhams: [{ maSp: sp.maSp, phanTramGiamGia: 50 }] } })).status()).toBe(204);
    expect((await ok(await ad.get('/api/san-pham/gia-khuyen-mai')))[sp.maSp]).toBe(50000);

    expect((await ad.post(`/api/khuyen-mai/${km.maKm}/ket-thuc`)).status()).toBe(204);
    expect((await ok(await ad.get('/api/san-pham/gia-khuyen-mai')))[sp.maSp]).toBeUndefined();
    expect((await ok(await ad.get('/api/khuyen-mai/dang-chay'))).some((k: any) => k.maKm === km.maKm)).toBeFalsy();
  });

  test('admin tao VOUCHER moi, ma trung -> 400', async () => {
    const ad = await admin();
    const ma = `T${RUN}`;
    const km = await ok(await ad.post('/api/khuyen-mai', {
      data: { tenKm: `Voucher ${RUN}`, hinhThuc: 'VOUCHER', ngayBd: homNay(), ngayKt: cong(3),
        vouchers: [{ maVoucher: ma.toLowerCase(), phanTramGiamGia: 15 }] },
    }));
    expect(km.vouchers[0].maVoucher).toBe(ma);
    await loi(await ad.post('/api/khuyen-mai', {
      data: { tenKm: 'Trùng', hinhThuc: 'VOUCHER', ngayBd: homNay(), ngayKt: cong(3), vouchers: [{ maVoucher: ma, phanTramGiamGia: 5 }] } }), 400, /đã tồn tại/);
    // sua chinh KM do, giu nguyen ma voucher -> khong bao trung voi chinh no
    expect((await ad.put(`/api/khuyen-mai/${km.maKm}`, {
      data: { tenKm: `Voucher ${RUN}`, hinhThuc: 'VOUCHER', ngayBd: homNay(), ngayKt: cong(5), vouchers: [{ maVoucher: ma, phanTramGiamGia: 20 }] } })).status()).toBe(204);
  });

  test('validate khuyen mai', async () => {
    const ad = await admin();
    const co = { tenKm: 'X', ngayBd: homNay(), ngayKt: cong(1) };
    await loi(await ad.post('/api/khuyen-mai', { data: { ...co, hinhThuc: 'ABC' } }), 400, /Hình thức/);
    await loi(await ad.post('/api/khuyen-mai', { data: { ...co, tenKm: ' ', hinhThuc: 'HOA_DON', mucHoaDons: [{ mucApDungHoaDon: 1, phanTramGiamGia: 1 }] } }), 400, /tên/);
    await loi(await ad.post('/api/khuyen-mai', { data: { ...co, ngayKt: cong(-3), hinhThuc: 'HOA_DON', mucHoaDons: [{ mucApDungHoaDon: 1, phanTramGiamGia: 1 }] } }), 400, /Ngày kết thúc/);
    await loi(await ad.post('/api/khuyen-mai', { data: { ...co, hinhThuc: 'SAN_PHAM', sanPhams: [] } }), 400);
    await loi(await ad.post('/api/khuyen-mai', { data: { ...co, hinhThuc: 'SAN_PHAM', sanPhams: [{ maSp: 'SP999', phanTramGiamGia: 10 }] } }), 400, /không tồn tại/);
    await loi(await ad.post('/api/khuyen-mai', { data: { ...co, hinhThuc: 'HOA_DON', mucHoaDons: [{ mucApDungHoaDon: 1, phanTramGiamGia: 150 }] } }), 400);
    await loi(await ad.post('/api/khuyen-mai', { data: { ...co, hinhThuc: 'VOUCHER', vouchers: [{ maVoucher: ' ', phanTramGiamGia: 10 }] } }), 400);
    await loi(await ad.get('/api/khuyen-mai/KM99'), 404);
    await loi(await ad.put('/api/khuyen-mai/KM99', { data: { ...co, hinhThuc: 'HOA_DON', mucHoaDons: [{ mucApDungHoaDon: 1, phanTramGiamGia: 1 }] } }), 404);
    await loi(await ad.post('/api/khuyen-mai/KM99/ket-thuc'), 404);
  });

  test('phan quyen khuyen mai', async () => {
    expect((await (await nhanVien()).get('/api/khuyen-mai')).status()).toBe(403);
    expect((await (await khach()).post('/api/khuyen-mai', { data: {} })).status()).toBe(403);
    expect((await (await anDanh()).get('/api/khuyen-mai/KM01')).status()).toBe(401);
  });
});
