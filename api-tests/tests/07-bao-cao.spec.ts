import { test, expect } from '@playwright/test';
import ExcelJS from 'exceljs';
import { admin, anDanh, khach, loi, nhanVien, ok, RUN, taoSanPhamCoTon, sdtMoi } from './helpers';

const XLSX = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';
const laXlsx = (b: Buffer) => b.subarray(0, 2).toString() === 'PK';
// Tao file theo dung cot cua file mau (exceljs khong mo duoc file ClosedXML sinh ra nen ghi lai tieu de)
const COT_MAU: Record<string, string[]> = {
  'san-pham': ['Tên sản phẩm*', 'Mã loại*', 'Chất liệu', 'Mô tả', 'Đơn giá*', 'Kích cỡ', 'Màu sắc', 'Hình ảnh'],
  'khach-hang': ['Họ tên*', 'SĐT', 'Email', 'Ngày sinh', 'Địa chỉ', 'Tỉnh thành'],
  'chi-tiet-phieu-nhap': ['Mã SP*', 'Số lượng*', 'Đơn giá nhập*'],
};
async function fileMau(loai: string, dongs: any[][]) {
  const wb = new ExcelJS.Workbook();
  const ws = wb.addWorksheet(loai);
  ws.addRow(COT_MAU[loai]);
  dongs.forEach(d => ws.addRow(d));
  return Buffer.from(await wb.xlsx.writeBuffer());
}
const laPdf = (b: Buffer) => b.subarray(0, 4).toString() === '%PDF';

test.describe('ThongKe (yeu cau 3 + 5, chi ADMIN)', () => {
  test('cac nam co du lieu', async () => {
    const ds = await ok(await (await admin()).get('/api/thong-ke/cac-nam'));
    expect(ds.length).toBeGreaterThan(0);
    expect(ds).toContain(2025);
  });

  const baoCao = [
    'tong-tien?tuNgay=2025-01-01&denNgay=2025-12-31',
    'thu-chi?kieu=THANG&nam=2025', 'thu-chi?kieu=QUY', 'thu-chi?kieu=NAM',
    'nhan-vien', 'khach-hang?nam=2025', 'khach-hang', 'san-pham?nam=2025',
  ];
  for (const url of baoCao) {
    test(`2 cach cho ket qua giong nhau: ${url}`, async () => {
      const ad = await admin();
      await ad.post('/api/thong-ke/lam-moi');
      const moi = await ok(await ad.get(`/api/thong-ke/${url}&cach=TRUY_VAN_MOI`.replace(/^([^?]*)&/, '$1?')));
      const tsd = await ok(await ad.get(`/api/thong-ke/${url}&cach=TAI_SU_DUNG`.replace(/^([^?]*)&/, '$1?')));
      expect(moi.cachTinh).toBe('TRUY_VAN_MOI');
      expect(tsd.cachTinh).toBe('TAI_SU_DUNG');
      expect(moi.cots.length).toBeGreaterThan(0);
      expect(moi.dongs.length).toBeGreaterThan(0);
      expect(tsd.cots).toEqual(moi.cots);
      expect(tsd.dongs).toEqual(moi.dongs);
      expect(moi.soCauTruyVan).toBeGreaterThan(0);
      // tai su dung lan 2: dung bo nho dem, khong chay them cau SQL
      const tsd2 = await ok(await ad.get(`/api/thong-ke/${url}&cach=TAI_SU_DUNG`.replace(/^([^?]*)&/, '$1?')));
      expect(tsd2.soCauTruyVan).toBe(0);
    });
  }

  test('tong tien khop voi danh sach hoa don da thanh toan', async () => {
    const ad = await admin();
    const t = await ok(await ad.get('/api/thong-ke/tong-tien?tuNgay=2025-03-01&denNgay=2025-03-31'));
    const ds = await ok(await ad.get('/api/hoa-don?tuNgay=2025-03-01&denNgay=2025-03-31&trangThai=DA_THANH_TOAN'));
    expect(t.dongs[0].giaTris[0]).toBe(ds.length);
    expect(t.dongs[0].giaTris[1]).toBe(ds.reduce((s: number, h: any) => s + h.tongTien, 0));
  });

  test('thong ke tai su dung cap nhat sau khi co hoa don moi (khong tra so cu)', async () => {
    const ad = await admin();
    const homNay = new Date().toISOString().slice(0, 10);
    const q = `/api/thong-ke/tong-tien?tuNgay=${homNay}&denNgay=${homNay}`;
    await ok(await ad.get(`${q}&cach=TAI_SU_DUNG`));         // nap bo nho dem
    const sp = await taoSanPhamCoTon(ad, 5, 123000);
    await ok(await (await nhanVien()).post('/api/hoa-don/ban-tai-quay', { data: { dongHangs: [{ maSp: sp.maSp, soLuong: 1 }] } }));
    const moi = await ok(await ad.get(`${q}&cach=TRUY_VAN_MOI`));
    const tsd = await ok(await ad.get(`${q}&cach=TAI_SU_DUNG`));
    expect(tsd.dongs).toEqual(moi.dongs);
  });

  test('xuat excel / pdf', async () => {
    const ad = await admin();
    const x = await ad.get('/api/thong-ke/thu-chi?kieu=QUY&nam=2025&dinhDang=excel');
    expect(x.status()).toBe(200);
    expect(x.headers()['content-type']).toContain(XLSX);
    expect(laXlsx(await x.body())).toBeTruthy();
    const p = await ad.get('/api/thong-ke/san-pham?nam=2025&dinhDang=PDF');
    expect(p.status()).toBe(200);
    expect(laPdf(await p.body())).toBeTruthy();
    expect(p.headers()['content-disposition']).toContain('.pdf');
  });

  test('tham so sai -> 400', async () => {
    const ad = await admin();
    await loi(await ad.get('/api/thong-ke/tong-tien?tuNgay=2025-12-31&denNgay=2025-01-01'), 400, /Ngày bắt đầu/);
    await loi(await ad.get('/api/thong-ke/thu-chi?kieu=TUAN'), 400, /THANG/);
    await loi(await ad.get('/api/thong-ke/nhan-vien?cach=ABC'), 400);
    expect((await ad.get('/api/thong-ke/tong-tien')).status()).toBe(400);   // thieu ngay bat buoc
    expect((await ad.get('/api/thong-ke/san-pham')).status()).toBe(400);    // thieu nam
  });

  test('chi ADMIN xem thong ke', async () => {
    expect((await (await nhanVien()).get('/api/thong-ke/cac-nam')).status()).toBe(403);
    expect((await (await khach()).get('/api/thong-ke/nhan-vien')).status()).toBe(403);
    expect((await (await anDanh()).post('/api/thong-ke/lam-moi')).status()).toBe(401);
  });
});

test.describe('TimKiem nang cao (yeu cau 6)', () => {
  test('danh sach truong / toan tu', async () => {
    const t = await ok(await (await nhanVien()).get('/api/tim-kiem/truong'));
    expect(t.hoaDon).toContain('tong_tien');
    expect(t.khachHang).toContain('tinh_thanh');
    expect(t.sanPham).toContain('don_gia');
    expect(t.toanTu).toEqual(expect.arrayContaining(['=', '<>', '>', 'LIKE', 'IN']));
  });

  test('hoa don: AND (giao) va OR (hop)', async () => {
    const nv = await nhanVien();
    const giao = await ok(await nv.post('/api/tim-kiem/hoa-don', { data: { khopTatCa: true, dieuKiens: [
      { truong: 'tong_tien', toanTu: '>=', giaTri: '1000000' }, { truong: 'trang_thai', toanTu: '=', giaTri: 'DA_THANH_TOAN' }] } }));
    expect(giao.length).toBeGreaterThan(0);
    expect(giao.every((h: any) => h.tongTien >= 1000000 && h.trangThai === 'DA_THANH_TOAN')).toBeTruthy();
    const hop = await ok(await nv.post('/api/tim-kiem/hoa-don', { data: { khopTatCa: false, dieuKiens: [
      { truong: 'tong_tien', toanTu: '>=', giaTri: '1000000' }, { truong: 'trang_thai', toanTu: '=', giaTri: 'DA_HUY' }] } }));
    expect(hop.every((h: any) => h.tongTien >= 1000000 || h.trangThai === 'DA_HUY')).toBeTruthy();
    expect(hop.length).toBeGreaterThanOrEqual(giao.length);
    const ngay = await ok(await nv.post('/api/tim-kiem/hoa-don', { data: { dieuKiens: [
      { truong: 'ngay_lap', toanTu: '>=', giaTri: '2026-01-01' }, { truong: 'tinh_thanh', toanTu: 'LIKE', giaTri: 'Hà' }] } }));
    expect(ngay.every((h: any) => h.ngayLap >= '2026-01-01' && /Hà/.test(h.tinhThanhKhach))).toBeTruthy();
  });

  test('khach hang IN / <>, san pham LIKE + so', async () => {
    const nv = await nhanVien();
    const kh = await ok(await nv.post('/api/tim-kiem/khach-hang', { data: { dieuKiens: [
      { truong: 'hang_thanh_vien', toanTu: 'IN', giaTri: 'VANG, KIM_CUONG' }] } }));
    expect(kh.length).toBeGreaterThan(0);
    expect(kh.every((k: any) => ['VANG', 'KIM_CUONG'].includes(k.hangThanhVien))).toBeTruthy();
    const kh2 = await ok(await nv.post('/api/tim-kiem/khach-hang', { data: { dieuKiens: [
      { truong: 'tinh_thanh', toanTu: '<>', giaTri: 'Hà Nội' }] } }));
    expect(kh2.every((k: any) => k.tinhThanh !== 'Hà Nội')).toBeTruthy();
    const sp = await ok(await nv.post('/api/tim-kiem/san-pham', { data: { dieuKiens: [
      { truong: 'ten_sp', toanTu: 'LIKE', giaTri: 'Áo' }, { truong: 'don_gia', toanTu: '<', giaTri: '300000' }] } }));
    expect(sp.length).toBeGreaterThan(0);
    expect(sp.every((s: any) => s.tenSp.toLowerCase().includes('áo') && s.donGia < 300000)).toBeTruthy();
  });

  test('khong co dieu kien -> 400; truong / toan tu / gia tri sai -> 400; chong SQL injection', async () => {
    const nv = await nhanVien();
    await loi(await nv.post('/api/tim-kiem/san-pham', { data: { dieuKiens: [] } }), 400, /ít nhất 1 điều kiện/);
    await loi(await nv.post('/api/tim-kiem/san-pham', { data: { dieuKiens: [{ truong: 'mat_khau', toanTu: '=', giaTri: 'x' }] } }), 400);
    await loi(await nv.post('/api/tim-kiem/san-pham', { data: { dieuKiens: [{ truong: 'don_gia', toanTu: '; DROP', giaTri: '1' }] } }), 400);
    await loi(await nv.post('/api/tim-kiem/san-pham', { data: { dieuKiens: [{ truong: 'don_gia', toanTu: '>', giaTri: 'abc' }] } }), 400);
    await loi(await nv.post('/api/tim-kiem/hoa-don', { data: { dieuKiens: [{ truong: 'ngay_lap', toanTu: '>', giaTri: 'hom qua' }] } }), 400);
    const inj = await ok(await nv.post('/api/tim-kiem/khach-hang', { data: { dieuKiens: [
      { truong: 'ho_ten', toanTu: '=', giaTri: "x' OR 1=1 --" }] } }));
    expect(inj).toHaveLength(0);
  });

  test('phan quyen tim kiem', async () => {
    expect((await (await khach()).post('/api/tim-kiem/san-pham', { data: {} })).status()).toBe(403);
    expect((await (await anDanh()).get('/api/tim-kiem/truong')).status()).toBe(401);
  });
});

test.describe('Excel (yeu cau 7)', () => {
  for (const loai of ['san-pham', 'khach-hang', 'nha-cung-cap', 'hoa-don', 'phieu-nhap', 'hoa-don?tuNgay=2025-01-01&denNgay=2025-12-31&trangThai=DA_THANH_TOAN']) {
    test(`xuat ${loai}`, async () => {
      const r = await (await nhanVien()).get(`/api/excel/${loai}`);
      expect(r.status()).toBe(200);
      expect(r.headers()['content-type']).toContain(XLSX);
      expect(r.headers()['content-disposition']).toContain('.xlsx');
      expect(laXlsx(await r.body())).toBeTruthy();
    });
  }

  test('xuat nhan vien chi ADMIN', async () => {
    expect((await (await nhanVien()).get('/api/excel/nhan-vien')).status()).toBe(403);
    const r = await (await admin()).get('/api/excel/nhan-vien');
    expect(r.status()).toBe(200);
    expect(laXlsx(await r.body())).toBeTruthy();
  });

  for (const loai of ['san-pham', 'khach-hang', 'nha-cung-cap', 'chi-tiet-phieu-nhap']) {
    test(`file mau ${loai} + doc lai file mau rong`, async () => {
      const nv = await nhanVien();
      const r = await nv.get(`/api/excel/mau/${loai}`);
      expect(r.status()).toBe(200);
      const buf = await r.body();
      expect(laXlsx(buf)).toBeTruthy();
      const kq = await ok(await nv.post(`/api/excel/doc/${loai}`, { multipart: { file: { name: 'mau.xlsx', mimeType: XLSX, buffer: buf } } }));
      expect(kq).toHaveProperty('hopLe');
      expect(kq).toHaveProperty('lois');
    });
  }

  test('file xuat danh sach khong dung de nhap -> 400 bao thieu cot, khuyen dung file mau', async () => {
    const nv = await nhanVien();
    const buf = await (await nv.get('/api/excel/san-pham')).body();
    await loi(await nv.post('/api/excel/doc/san-pham', { multipart: { file: { name: 'sp.xlsx', mimeType: XLSX, buffer: buf } } }), 400, /thiếu cột/);
  });

  test('dien file mau san pham -> doc: dong hop le + loi tung dong (ke ca ma loai khong ton tai)', async () => {
    const nv = await nhanVien();
    const buf = await fileMau('san-pham', [
      [`Excel áo ${RUN}`, 'lsp01', 'Cotton', '', 199000, 'm', 'Trắng', ''],
      [`Thiếu giá ${RUN}`, 'LSP01', '', '', '', 'L', '', ''],
      ['', 'LSP01', '', '', 100000, '', '', ''],
      [`Sai loại ${RUN}`, 'LSP99', '', '', 100000, '', '', ''],
      [`Giá âm ${RUN}`, 'LSP01', '', '', -5, '', '', '']]);
    const kq = await ok(await nv.post('/api/excel/doc/san-pham', { multipart: { file: { name: 'sp.xlsx', mimeType: XLSX, buffer: buf } } }));
    expect(kq.tongSoDong).toBe(5);
    expect(kq.hopLe).toHaveLength(1);
    expect(kq.hopLe[0]).toMatchObject({ tenSp: `Excel áo ${RUN}`, maLoaiSp: 'LSP01', donGia: 199000, kichCo: 'M' });
    expect(kq.lois.map((l: any) => l.dong).sort()).toEqual([3, 4, 5, 6]);
    // buoc 2: admin ghi cac dong hop le
    const sp = await ok(await (await admin()).post('/api/excel/nhap/san-pham', { data: kq.hopLe }));
    expect(sp[0].maSp).toMatch(/^SP/);
  });

  test('dien file mau khach hang (ngay sinh dd/MM/yyyy) va chi tiet phieu nhap', async () => {
    const nv = await nhanVien();
    const bufKh = await fileMau('khach-hang', [
      [`Excel KH ${RUN}`, sdtMoi(9), `ex${RUN}@mail.vn`, '15/08/1999', 'Q1', 'TP Hồ Chí Minh'],
      ['SĐT chữ', '09abc', '', '', '', '']]);
    const kq = await ok(await nv.post('/api/excel/doc/khach-hang', { multipart: { file: { name: 'kh.xlsx', mimeType: XLSX, buffer: bufKh } } }));
    expect(kq.hopLe).toHaveLength(1);
    expect(kq.hopLe[0].ngaySinh.slice(0, 10)).toBe('1999-08-15');
    expect(kq.lois).toHaveLength(1);

    const bufPn = await fileMau('chi-tiet-phieu-nhap', [['sp002', 4, 100000], ['SP999', 1, 1], ['SP003', 0, 1]]);
    const kq2 = await ok(await nv.post('/api/excel/doc/chi-tiet-phieu-nhap', { multipart: { file: { name: 'pn.xlsx', mimeType: XLSX, buffer: bufPn } } }));
    expect(kq2.hopLe).toHaveLength(1);
    expect(kq2.hopLe[0]).toMatchObject({ maSp: 'SP002', soLuong: 4, thanhTien: 400000 });
    expect(kq2.lois).toHaveLength(2);
  });

  test('file excel co 2 cot trung tieu de / sheet rong -> 400, khong 500', async () => {
    const nv = await nhanVien();
    const wb = new ExcelJS.Workbook();
    const ws = wb.addWorksheet('x');
    ws.addRow(['Họ tên*', 'Họ tên*', 'SĐT']);
    ws.addRow(['A', 'B', '0900000000']);
    const r = await nv.post('/api/excel/doc/khach-hang', { multipart: { file: { name: 'x.xlsx', mimeType: XLSX, buffer: Buffer.from(await wb.xlsx.writeBuffer()) } } });
    expect(r.status()).toBe(400);
    const rong = new ExcelJS.Workbook();
    rong.addWorksheet('rong');
    const r2 = await nv.post('/api/excel/doc/khach-hang', { multipart: { file: { name: 'r.xlsx', mimeType: XLSX, buffer: Buffer.from(await rong.xlsx.writeBuffer()) } } });
    expect(r2.status()).toBe(400);
  });

  test('doc file khong phai excel / loai sai / thieu file -> 400 hoac 404, khong 500', async () => {
    const nv = await nhanVien();
    const rac = await nv.post('/api/excel/doc/san-pham', { multipart: { file: { name: 'x.xlsx', mimeType: XLSX, buffer: Buffer.from('khong phai excel') } } });
    expect(rac.status()).toBe(400);
    const buf = await (await nv.get('/api/excel/mau/san-pham')).body();
    await loi(await nv.post('/api/excel/doc/abc', { multipart: { file: { name: 'x.xlsx', mimeType: XLSX, buffer: buf } } }), 404);
    expect((await nv.post('/api/excel/doc/san-pham')).status()).toBe(400);
    await loi(await nv.get('/api/excel/mau/abc'), 404);
  });

  test('nhap khach hang: dong hop le duoc them, dong loi tra ve', async () => {
    const nv = await nhanVien();
    const r = await ok(await nv.post('/api/excel/nhap/khach-hang', { data: [
      { hoTen: `Excel KH ${RUN}`, sdt: sdtMoi(7), tinhThanh: 'Huế' },
      { hoTen: '', sdt: sdtMoi(8) },
      { hoTen: 'Trùng SĐT', sdt: '0912000000' }] }));
    expect(r.soDongDaThem).toBe(1);
    expect(r.lois.length).toBe(2);
    const sau = await ok(await nv.get(`/api/khach-hang/theo-sdt/${sdtMoi(7)}`));
    expect(sau.hoTen).toBe(`Excel KH ${RUN}`);
  });

  test('nhap nha cung cap / san pham chi ADMIN', async () => {
    const nv = await nhanVien();
    expect((await nv.post('/api/excel/nhap/nha-cung-cap', { data: [] })).status()).toBe(403);
    expect((await nv.post('/api/excel/nhap/san-pham', { data: [] })).status()).toBe(403);
    const ad = await admin();
    const n = await ok(await ad.post('/api/excel/nhap/nha-cung-cap', { data: [{ tenNcc: `Excel NCC ${RUN}` }, { tenNcc: '' }] }));
    expect(n.soDongDaThem).toBe(1);
    expect(n.lois.length).toBe(1);
    const sp = await ok(await ad.post('/api/excel/nhap/san-pham', { data: [{ tenSp: `Excel SP ${RUN}`, maLoaiSp: 'LSP01', donGia: 99000, kichCo: 'M' }] }));
    expect(sp).toHaveLength(1);
    expect(sp[0].maSp).toMatch(/^SP/);
  });

  test('phan quyen excel', async () => {
    expect((await (await khach()).get('/api/excel/san-pham')).status()).toBe(403);
    expect((await (await anDanh()).get('/api/excel/mau/san-pham')).status()).toBe(401);
  });
});

test.describe('NhatKySql + header (yeu cau 1)', () => {
  test('moi response co header so cau SQL va vi tri dong code', async () => {
    const r = await (await anDanh()).get('/api/san-pham/SP001');
    expect(r.headers()['x-so-cau-sql']).toBe('1');
    expect(r.headers()['x-sql-vi-tri']).toMatch(/DAL.*\.cs:\d+ \w+/);
  });

  test('xem nhat ky gan nhat (nhan vien), xoa (admin)', async () => {
    const nv = await nhanVien();
    await nv.get('/api/san-pham/SP002');
    const ds = await ok(await nv.get('/api/nhat-ky-sql?soLuong=5'));
    expect(ds.length).toBeGreaterThan(0);
    expect(ds.length).toBeLessThanOrEqual(5);
    expect(ds[0]).toMatchObject({ cauSql: expect.any(String), file: expect.any(String), dong: expect.any(Number) });
    expect((await nv.delete('/api/nhat-ky-sql')).status()).toBe(403);
    const ad = await admin();
    expect((await ad.delete('/api/nhat-ky-sql')).status()).toBe(204);
    expect((await (await khach()).get('/api/nhat-ky-sql')).status()).toBe(403);
  });
});

test.describe('He thong', () => {
  test('swagger + trang goc chuyen huong', async () => {
    const api = await anDanh();
    const s = await ok(await api.get('/swagger/v1/swagger.json'));
    expect(Object.keys(s.paths).length).toBeGreaterThan(80);
    const r = await api.get('/', { maxRedirects: 0 });
    expect(r.status()).toBe(302);
  });

  test('token gia mao / het han -> 401', async () => {
    const { request } = await import('@playwright/test');
    const bad = await request.newContext({ baseURL: process.env.API_URL ?? 'http://localhost:5212',
      extraHTTPHeaders: { Authorization: 'Bearer eyJhbGciOiJIUzI1NiJ9.eyJ2YWlfdHJvIjoiQURNSU4ifQ.xxx' } });
    expect((await bad.get('/api/nhan-vien')).status()).toBe(401);
  });

  test('CORS cho web staff', async () => {
    const r = await (await anDanh()).fetch('/api/san-pham', { method: 'OPTIONS', headers: {
      Origin: 'http://localhost:5174', 'Access-Control-Request-Method': 'GET' } });
    expect(r.status()).toBe(204);
    expect(r.headers()['access-control-allow-origin']).toBe('http://localhost:5174');
  });
});
