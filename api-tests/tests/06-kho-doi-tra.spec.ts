import { test, expect } from '@playwright/test';
import { admin, anDanh, khach, loi, nhanVien, ok, RUN, taoSanPhamCoTon, BASE } from './helpers';
import { request } from '@playwright/test';

const ton = async (api: any, maSp: string) => (await ok(await api.get(`/api/san-pham/${maSp}`))).soLuong as number;

test.describe('PhieuNhap', () => {
  test('lap phieu: gop dong trung, cong ton, xem, chi tiet, pdf', async () => {
    const ad = await admin();
    const sp = await ok(await ad.post('/api/san-pham', { data: { tenSp: `Nhập ${RUN}`, maLoaiSp: 'LSP02', donGia: 300000 } }));
    const nv = await nhanVien();
    const p = await ok(await nv.post('/api/phieu-nhap', {
      data: { maNcc: 'NCC02', chiTiets: [{ maSp: sp.maSp, soLuong: 5, donGiaNhap: 150000 }, { maSp: sp.maSp.toLowerCase(), soLuong: 3, donGiaNhap: 150000 }] } }));
    expect(p).toMatchObject({ maNcc: 'NCC02', tongTien: 1200000 });
    expect(p.chiTiets).toHaveLength(1);
    expect(p.chiTiets[0]).toMatchObject({ soLuong: 8, thanhTien: 1200000 });
    expect(await ton(ad, sp.maSp)).toBe(8);

    await ok(await nv.get(`/api/phieu-nhap/${p.maPhieuNhap}`));
    expect(await ok(await nv.get(`/api/phieu-nhap/${p.maPhieuNhap}/chi-tiet`))).toHaveLength(1);
    const ds = await ok(await nv.get('/api/phieu-nhap?maNcc=NCC02'));
    expect(ds.some((x: any) => x.maPhieuNhap === p.maPhieuNhap)).toBeTruthy();
    expect(ds.every((x: any) => x.maNcc === 'NCC02')).toBeTruthy();
    const pdf = await nv.get(`/api/phieu-nhap/${p.maPhieuNhap}/pdf`);
    expect(pdf.status()).toBe(200);
    expect((await pdf.body()).subarray(0, 4).toString()).toBe('%PDF');
  });

  test('validate phieu nhap', async () => {
    const nv = await nhanVien();
    await loi(await nv.post('/api/phieu-nhap', { data: { maNcc: 'NCC01', chiTiets: [] } }), 400, /chưa có sản phẩm/);
    await loi(await nv.post('/api/phieu-nhap', { data: { maNcc: 'NCC01' } }), 400);
    await loi(await nv.post('/api/phieu-nhap', { data: { maNcc: 'NCC99', chiTiets: [{ maSp: 'SP001', soLuong: 1, donGiaNhap: 1 }] } }), 404);
    await loi(await nv.post('/api/phieu-nhap', { data: { maNcc: 'NCC04', chiTiets: [{ maSp: 'SP001', soLuong: 1, donGiaNhap: 1 }] } }), 400, /ngừng hợp tác/);
    await loi(await nv.post('/api/phieu-nhap', { data: { maNcc: 'NCC01', chiTiets: [{ maSp: 'SP999', soLuong: 1, donGiaNhap: 1 }] } }), 404);
    await loi(await nv.post('/api/phieu-nhap', { data: { maNcc: 'NCC01', chiTiets: [{ maSp: 'SP001', soLuong: 0, donGiaNhap: 1 }] } }), 400);
    await loi(await nv.post('/api/phieu-nhap', { data: { maNcc: 'NCC01', chiTiets: [{ maSp: 'SP001', soLuong: 1, donGiaNhap: 0 }] } }), 400);
    await loi(await nv.post('/api/phieu-nhap', {
      data: { maNcc: 'NCC01', chiTiets: [{ maSp: 'SP001', soLuong: 1, donGiaNhap: 1 }, { maSp: 'SP001', soLuong: 1, donGiaNhap: 2 }] } }), 400, /2 đơn giá/);
    await loi(await nv.get('/api/phieu-nhap/PN99999'), 404);
    expect((await nv.get('/api/phieu-nhap/PN99999/pdf')).status()).toBe(404);
  });

  test('phan quyen phieu nhap', async () => {
    expect((await (await khach()).get('/api/phieu-nhap')).status()).toBe(403);
    expect((await (await anDanh()).post('/api/phieu-nhap', { data: {} })).status()).toBe(401);
  });
});

test.describe('DoiTra', () => {
  async function hoaDonCuaKhachMoi(soLuong = 3) {
    const ad = await admin();
    const ten = `Đổi trả ${RUN}-${Math.random().toString(36).slice(2, 6)}`;
    const m = await taoSanPhamCoTon(ad, 20, 200000, ten, 'M');
    const l = await taoSanPhamCoTon(ad, 20, 200000, ten, 'L');
    const api = await anDanh();
    const ten2 = `dt${RUN}${Math.random().toString(36).slice(2, 5)}`;
    const p = await ok(await api.post('/api/tai-khoan/dang-ky', { data: { tenDangNhap: ten2, matKhau: '123456', khachHang: { hoTen: 'Khách đổi trả' } } }));
    const kh = await request.newContext({ baseURL: BASE, extraHTTPHeaders: { Authorization: `Bearer ${p.token}` } });
    const nv = await nhanVien();
    const hd = await ok(await nv.post('/api/hoa-don/ban-tai-quay', {
      data: { maKh: p.maKh, phuongThucTt: 'TIEN_MAT', dongHangs: [{ maSp: m.maSp, soLuong: soLuong }] } }));
    return { ad, nv, kh, hd, m, l, maKh: p.maKh };
  }

  test('khach gui yeu cau TRA -> nhan vien duyet -> hoan tat: nhap kho, hoan tien theo gia thuc tra', async () => {
    const { ad, nv, kh, hd, m, maKh } = await hoaDonCuaKhachMoi(6);   // 1.2tr -> giam 5%
    expect(hd.tienGiam).toBe(60000);
    const dt = await ok(await kh.post('/api/doi-tra', {
      data: { maHoaDon: hd.maHoaDon, loai: 'TRA', lyDo: 'Không vừa', chiTiets: [{ maSp: m.maSp, soLuong: 2 }] } }));
    expect(dt).toMatchObject({ trangThai: 'CHO_DUYET', loai: 'TRA', maKh });
    expect((await ok(await kh.get('/api/doi-tra/cua-toi'))).map((x: any) => x.maDoiTra)).toContain(dt.maDoiTra);
    await ok(await kh.get(`/api/doi-tra/${dt.maDoiTra}`));
    const pdf = await kh.get(`/api/doi-tra/${dt.maDoiTra}/pdf`);
    expect(pdf.status()).toBe(200);

    // khong tra qua so luong con lai
    await loi(await kh.post('/api/doi-tra', {
      data: { maHoaDon: hd.maHoaDon, loai: 'TRA', chiTiets: [{ maSp: m.maSp, soLuong: 5 }] } }), 400, /chỉ còn 4/);

    const choDuyet = await ok(await nv.get('/api/doi-tra?trangThai=CHO_DUYET'));
    expect(choDuyet.some((x: any) => x.maDoiTra === dt.maDoiTra)).toBeTruthy();
    expect((await nv.post(`/api/doi-tra/${dt.maDoiTra}/duyet`)).status()).toBe(204);
    await loi(await nv.post(`/api/doi-tra/${dt.maDoiTra}/duyet`), 400);
    const tonTruoc = await ton(ad, m.maSp);
    const xong = await ok(await nv.post(`/api/doi-tra/${dt.maDoiTra}/hoan-tat`));
    expect(xong).toMatchObject({ trangThai: 'HOAN_TAT', tienHoan: 380000 });   // 2 x 200k x 95%
    expect(xong.ngayHoan).toBeTruthy();
    expect(await ton(ad, m.maSp)).toBe(tonTruoc + 2);
    await loi(await nv.post(`/api/doi-tra/${dt.maDoiTra}/hoan-tat`), 400, /đã được xử lý/);
    // hoa don da co phieu doi tra -> khong huy duoc
    await loi(await nv.post(`/api/hoa-don/${hd.maHoaDon}/huy`), 400, /đổi trả/);
  });

  test('nhan vien lap phieu DOI tai quay: doi size, tru ton size moi', async () => {
    const { ad, nv, hd, m, l } = await hoaDonCuaKhachMoi(2);
    await loi(await nv.post('/api/doi-tra', {
      data: { maHoaDon: hd.maHoaDon, loai: 'DOI', chiTiets: [{ maSp: m.maSp, soLuong: 1, maSpDoi: 'SP001' }] } }), 400, /cùng mẫu/);
    await loi(await nv.post('/api/doi-tra', {
      data: { maHoaDon: hd.maHoaDon, loai: 'DOI', chiTiets: [{ maSp: m.maSp, soLuong: 1 }] } }), 404, /đổi sang/);
    const dt = await ok(await nv.post('/api/doi-tra', {
      data: { maHoaDon: hd.maHoaDon, loai: 'DOI', chiTiets: [{ maSp: m.maSp, soLuong: 1, maSpDoi: l.maSp }] } }));
    const tm = await ton(ad, m.maSp), tl = await ton(ad, l.maSp);
    const xong = await ok(await nv.post(`/api/doi-tra/${dt.maDoiTra}/hoan-tat`));
    expect(xong).toMatchObject({ trangThai: 'HOAN_TAT', tienHoan: 0, ngayHoan: null });
    expect(await ton(ad, m.maSp)).toBe(tm + 1);
    expect(await ton(ad, l.maSp)).toBe(tl - 1);
  });

  test('tu choi phieu; validate', async () => {
    const { nv, kh, hd, m } = await hoaDonCuaKhachMoi(1);
    const dt = await ok(await kh.post('/api/doi-tra', { data: { maHoaDon: hd.maHoaDon, loai: 'TRA', chiTiets: [{ maSp: m.maSp, soLuong: 1 }] } }));
    expect((await nv.post(`/api/doi-tra/${dt.maDoiTra}/tu-choi`)).status()).toBe(204);
    await loi(await nv.post(`/api/doi-tra/${dt.maDoiTra}/hoan-tat`), 400);
    await loi(await nv.post(`/api/doi-tra/${dt.maDoiTra}/tu-choi`), 400);

    await loi(await kh.post('/api/doi-tra', { data: { maHoaDon: hd.maHoaDon, loai: 'BAN', chiTiets: [{ maSp: m.maSp, soLuong: 1 }] } }), 400, /DOI hoặc TRA/);
    await loi(await kh.post('/api/doi-tra', { data: { maHoaDon: hd.maHoaDon, loai: 'TRA', chiTiets: [] } }), 400);
    await loi(await kh.post('/api/doi-tra', { data: { maHoaDon: hd.maHoaDon, loai: 'TRA', chiTiets: [{ maSp: 'SP001', soLuong: 1 }] } }), 400, /không có trong hóa đơn/);
    await loi(await kh.post('/api/doi-tra', { data: { maHoaDon: 'HD99999', loai: 'TRA', chiTiets: [{ maSp: m.maSp, soLuong: 1 }] } }), 404);
    // hoa don nguoi khac
    const kh1 = await khach('khach01');
    await loi(await kh1.post('/api/doi-tra', { data: { maHoaDon: hd.maHoaDon, loai: 'TRA', chiTiets: [{ maSp: m.maSp, soLuong: 1 }] } }), 400, /không phải của bạn/);
    await loi(await kh1.get(`/api/doi-tra/${dt.maDoiTra}`), 404);
    await loi(await nv.get('/api/doi-tra/DT99999'), 404);
    await loi(await nv.post('/api/doi-tra/DT99999/hoan-tat'), 404);
  });

  test('qua 30 ngay khong doi tra duoc', async () => {
    const nv = await nhanVien();
    const cu = (await ok(await nv.get('/api/hoa-don?trangThai=DA_THANH_TOAN&denNgay=2025-06-30')))[0];
    const ct = await ok(await nv.get(`/api/hoa-don/${cu.maHoaDon}/chi-tiet`));
    await loi(await nv.post('/api/doi-tra', { data: { maHoaDon: cu.maHoaDon, loai: 'TRA', chiTiets: [{ maSp: ct[0].maSp, soLuong: 1 }] } }), 400, /30 ngày/);
  });

  test('phan quyen doi tra', async () => {
    const kh = await khach();
    expect((await kh.get('/api/doi-tra')).status()).toBe(403);
    expect((await kh.post('/api/doi-tra/DT00001/duyet')).status()).toBe(403);
    expect((await (await nhanVien()).get('/api/doi-tra/cua-toi')).status()).toBe(403);
    expect((await (await anDanh()).post('/api/doi-tra', { data: {} })).status()).toBe(401);
  });
});
