import { test, expect, APIRequestContext } from '@playwright/test';
import { admin, anDanh, khach, loi, nhanVien, ok, RUN, taoSanPhamCoTon, BASE } from './helpers';
import { request } from '@playwright/test';

// Moi test tao khach moi de gio hang / diem tich luy doc lap
async function khachMoi(ten: string) {
  const api = await anDanh();
  const p = await ok(await api.post('/api/tai-khoan/dang-ky', {
    data: { tenDangNhap: `${ten}${RUN}`, matKhau: '123456', khachHang: { hoTen: `Khách ${ten}`, tinhThanh: 'Hà Nội' } },
  }));
  const ctx = await request.newContext({ baseURL: BASE, extraHTTPHeaders: { Authorization: `Bearer ${p.token}` } });
  return { ctx, maKh: p.maKh as string };
}
const ton = async (api: APIRequestContext, maSp: string) => (await ok(await api.get(`/api/san-pham/${maSp}`))).soLuong as number;

test.describe('GioHang', () => {
  test('them / cap nhat / xoa dong / xoa het / gop gio tam', async () => {
    const ad = await admin();
    const a = await taoSanPhamCoTon(ad, 5, 100000);
    const b = await taoSanPhamCoTon(ad, 5, 50000);
    const { ctx: kh } = await khachMoi('gio');

    const rong = await ok(await kh.get('/api/gio-hang'));
    expect(rong.chiTiets).toHaveLength(0);

    let g = await ok(await kh.post('/api/gio-hang', { data: { maSp: a.maSp, soLuong: 2 } }));
    g = await ok(await kh.post('/api/gio-hang', { data: { maSp: a.maSp, soLuong: 1 } }));
    expect(g.chiTiets).toHaveLength(1);
    expect(g.chiTiets[0]).toMatchObject({ maSp: a.maSp, soLuong: 3, donGia: 100000, thanhTien: 300000 });
    expect(g.tamTinh).toBe(300000);

    await loi(await kh.post('/api/gio-hang', { data: { maSp: a.maSp, soLuong: 3 } }), 400, /chỉ còn 5/);
    await loi(await kh.post('/api/gio-hang', { data: { maSp: 'SP999', soLuong: 1 } }), 404);
    await loi(await kh.post('/api/gio-hang', { data: { maSp: a.maSp, soLuong: 0 } }), 400);
    await loi(await kh.post('/api/gio-hang', { data: { maSp: a.maSp, soLuong: -2 } }), 400);

    g = await ok(await kh.put('/api/gio-hang', { data: { maSp: a.maSp, soLuong: 1 } }));
    expect(g.chiTiets[0].soLuong).toBe(1);
    g = await ok(await kh.put('/api/gio-hang', { data: { maSp: b.maSp, soLuong: 2 } }));
    expect(g.chiTiets).toHaveLength(2);
    g = await ok(await kh.put('/api/gio-hang', { data: { maSp: b.maSp, soLuong: 0 } }));
    expect(g.chiTiets).toHaveLength(1);
    g = await ok(await kh.delete(`/api/gio-hang/${a.maSp}`));
    expect(g.chiTiets).toHaveLength(0);

    // gop gio tam: dong SP khong ton tai / qua ton bi bo qua
    g = await ok(await kh.post('/api/gio-hang/gop', {
      data: [{ maSp: a.maSp, soLuong: 2 }, { maSp: b.maSp, soLuong: 99 }, { maSp: 'SP999', soLuong: 1 }, { maSp: b.maSp, soLuong: 1 }] }));
    expect(g.chiTiets.map((c: any) => [c.maSp, c.soLuong]).sort()).toEqual([[a.maSp, 2], [b.maSp, 1]].sort());

    expect((await kh.delete('/api/gio-hang')).status()).toBe(204);
    expect((await ok(await kh.get('/api/gio-hang'))).chiTiets).toHaveLength(0);
  });

  test('san pham ngung ban khong them vao gio duoc', async () => {
    const ad = await admin();
    const a = await taoSanPhamCoTon(ad, 5);
    await ad.post(`/api/san-pham/${a.maSp}/ngung-ban`);
    const { ctx: kh } = await khachMoi('gng');
    await loi(await kh.post('/api/gio-hang', { data: { maSp: a.maSp, soLuong: 1 } }), 400, /ngừng bán/);
  });

  test('chi khach hang dung gio hang', async () => {
    expect((await (await nhanVien()).get('/api/gio-hang')).status()).toBe(403);
    expect((await (await anDanh()).get('/api/gio-hang')).status()).toBe(401);
  });
});

test.describe('DonHang (online)', () => {
  test('luong day du COD: dat -> xac nhan (tru ton, lap HD) -> giao -> hoan tat (HD da thanh toan, cong diem)', async () => {
    const ad = await admin();
    const sp = await taoSanPhamCoTon(ad, 10, 400000);
    const { ctx: kh, maKh } = await khachMoi('cod');
    await ok(await kh.post('/api/gio-hang', { data: { maSp: sp.maSp, soLuong: 3 } }));

    await loi(await kh.post('/api/don-hang', { data: { diaChiGiao: '', sdtNhan: '0900000000' } }), 400, /địa chỉ/);
    await loi(await kh.post('/api/don-hang', { data: { diaChiGiao: 'x', sdtNhan: '' } }), 400, /số điện thoại/);
    await loi(await kh.post('/api/don-hang', { data: { diaChiGiao: 'x', sdtNhan: '0900000000', phuongThucTt: 'TIEN_MAT' } }), 400, /Phương thức/);

    const don = await ok(await kh.post('/api/don-hang', {
      data: { diaChiGiao: '1 Tràng Tiền, Hà Nội', sdtNhan: '0900000000', tenNguoiNhan: 'Người nhận', phuongThucTt: 'COD' } }));
    // 1.2tr >= 1tr -> giam 5%
    expect(don).toMatchObject({ maKh, trangThai: 'CHO_XAC_NHAN', tienGiam: 60000, tongTien: 1140000 });
    expect(don.chiTiets).toHaveLength(1);
    expect((await ok(await kh.get('/api/gio-hang'))).chiTiets).toHaveLength(0);
    expect(await ton(ad, sp.maSp)).toBe(10);           // chua tru ton
    expect((await ok(await kh.get('/api/don-hang/cua-toi'))).map((d: any) => d.maDon)).toContain(don.maDon);
    await ok(await kh.get(`/api/don-hang/${don.maDon}`));
    await loi(await kh.post('/api/don-hang', { data: { diaChiGiao: 'x', sdtNhan: '0900000000' } }), 400, /trống/);

    const nv = await nhanVien();
    const choXn = await ok(await nv.get('/api/don-hang?trangThai=CHO_XAC_NHAN'));
    expect(choXn.some((d: any) => d.maDon === don.maDon)).toBeTruthy();
    expect(choXn.every((d: any) => d.trangThai === 'CHO_XAC_NHAN')).toBeTruthy();

    await loi(await nv.post(`/api/don-hang/${don.maDon}/giao-hang`), 400);
    const hd = await ok(await nv.post(`/api/don-hang/${don.maDon}/xac-nhan`));
    expect(hd).toMatchObject({ maDon: don.maDon, maKh, trangThai: 'CHO_THANH_TOAN', tongTien: 1140000, maKm: 'KM02' });
    expect(await ton(ad, sp.maSp)).toBe(7);
    await loi(await nv.post(`/api/don-hang/${don.maDon}/xac-nhan`), 400);
    await loi(await kh.post(`/api/don-hang/${don.maDon}/khach-huy`), 400, /đã được xác nhận/);

    expect((await nv.post(`/api/don-hang/${don.maDon}/giao-hang`)).status()).toBe(204);
    expect((await nv.post(`/api/don-hang/${don.maDon}/hoan-tat`)).status()).toBe(204);
    const sau = await ok(await kh.get(`/api/don-hang/${don.maDon}`));
    expect(sau).toMatchObject({ trangThai: 'HOAN_TAT', maHoaDon: hd.maHoaDon });
    expect((await ok(await kh.get(`/api/hoa-don/${hd.maHoaDon}`))).trangThai).toBe('DA_THANH_TOAN');
    const toi = await ok(await kh.get('/api/khach-hang/toi'));
    expect(toi.diemTichLuy).toBe(114);                 // 1.140.000 / 10.000

    const pdf = await kh.get(`/api/don-hang/${don.maDon}/pdf`);
    expect(pdf.status()).toBe(200);
    expect(pdf.headers()['content-type']).toContain('application/pdf');
    expect((await pdf.body()).subarray(0, 4).toString()).toBe('%PDF');
  });

  test('chuyen khoan: xac nhan -> HD da thanh toan ngay; nhan vien huy -> tra ton, huy HD, tru diem', async () => {
    const ad = await admin();
    const sp = await taoSanPhamCoTon(ad, 10, 100000);
    const { ctx: kh } = await khachMoi('ck');
    await ok(await kh.post('/api/gio-hang', { data: { maSp: sp.maSp, soLuong: 2 } }));
    const don = await ok(await kh.post('/api/don-hang', {
      data: { diaChiGiao: 'HN', sdtNhan: '0900000001', phuongThucTt: 'CHUYEN_KHOAN', maVoucher: 'welcome10' } }));
    expect(don).toMatchObject({ maVoucher: 'WELCOME10', tienGiam: 20000, tongTien: 180000 });
    const nv = await nhanVien();
    const hd = await ok(await nv.post(`/api/don-hang/${don.maDon}/xac-nhan`));
    expect(hd).toMatchObject({ trangThai: 'DA_THANH_TOAN', maKm: 'KM03' });
    expect((await ok(await kh.get('/api/khach-hang/toi'))).diemTichLuy).toBe(18);

    expect((await nv.post(`/api/don-hang/${don.maDon}/huy`)).status()).toBe(204);
    expect(await ton(ad, sp.maSp)).toBe(10);
    expect((await ok(await nv.get(`/api/hoa-don/${hd.maHoaDon}`))).trangThai).toBe('DA_HUY');
    expect((await ok(await kh.get('/api/khach-hang/toi'))).diemTichLuy).toBe(0);
    await loi(await nv.post(`/api/don-hang/${don.maDon}/huy`), 400, /kết thúc/);
    // hoa don cua don online khong huy truc tiep
    await loi(await nv.post(`/api/hoa-don/${hd.maHoaDon}/huy`), 400);
  });

  test('khach huy don cho xac nhan; nhan vien tu choi', async () => {
    const ad = await admin();
    const sp = await taoSanPhamCoTon(ad, 10);
    const { ctx: kh } = await khachMoi('huy');
    await ok(await kh.post('/api/gio-hang', { data: { maSp: sp.maSp, soLuong: 1 } }));
    const d1 = await ok(await kh.post('/api/don-hang', { data: { diaChiGiao: 'x', sdtNhan: '0900000002' } }));
    expect((await kh.post(`/api/don-hang/${d1.maDon}/khach-huy`)).status()).toBe(204);
    expect((await ok(await kh.get(`/api/don-hang/${d1.maDon}`))).trangThai).toBe('DA_HUY');

    await ok(await kh.post('/api/gio-hang', { data: { maSp: sp.maSp, soLuong: 1 } }));
    const d2 = await ok(await kh.post('/api/don-hang', { data: { diaChiGiao: 'x', sdtNhan: '0900000002' } }));
    const nv = await nhanVien();
    expect((await nv.post(`/api/don-hang/${d2.maDon}/tu-choi`)).status()).toBe(204);
    await loi(await nv.post(`/api/don-hang/${d2.maDon}/tu-choi`), 400);
    expect((await ok(await kh.get(`/api/don-hang/${d2.maDon}`))).trangThai).toBe('TU_CHOI');
    await loi(await nv.post(`/api/don-hang/${d2.maDon}/hoan-tat`), 400);
  });

  test('khach khong xem / huy duoc don cua nguoi khac; 404', async () => {
    const nv = await nhanVien();
    const ds = await ok(await nv.get('/api/don-hang'));
    const cuaNguoiKhac = ds.find((d: any) => d.maKh !== 'KH001');
    const kh = await khach('khach01');
    await loi(await kh.get(`/api/don-hang/${cuaNguoiKhac.maDon}`), 404);
    expect((await kh.get(`/api/don-hang/${cuaNguoiKhac.maDon}/pdf`)).status()).toBe(404);
    expect([400, 404]).toContain((await kh.post(`/api/don-hang/${cuaNguoiKhac.maDon}/khach-huy`)).status());
    await loi(await kh.get('/api/don-hang/DH99999'), 404);
    await loi(await nv.post('/api/don-hang/DH99999/xac-nhan'), 404);
    await loi(await nv.post('/api/don-hang/DH99999/huy'), 404);
  });

  test('loc danh sach don theo ngay', async () => {
    const nv = await nhanVien();
    const ds = await ok(await nv.get('/api/don-hang?tuNgay=2026-01-01&denNgay=2026-06-30'));
    expect(ds.length).toBeGreaterThan(0);
    for (const d of ds) expect(d.ngayDat.slice(0, 10) >= '2026-01-01' && d.ngayDat.slice(0, 10) <= '2026-06-30').toBeTruthy();
  });

  test('phan quyen don hang', async () => {
    expect((await (await khach()).get('/api/don-hang')).status()).toBe(403);
    expect((await (await khach()).post('/api/don-hang/DH00001/xac-nhan')).status()).toBe(403);
    expect((await (await nhanVien()).post('/api/don-hang', { data: {} })).status()).toBe(403);
    expect((await (await anDanh()).get('/api/don-hang/DH00001')).status()).toBe(401);
  });
});

test.describe('HoaDon (tai quay)', () => {
  test('tinh truoc + ban tai quay khach than thiet: tru ton, cong diem, huy -> tra lai', async () => {
    const ad = await admin();
    const sp = await taoSanPhamCoTon(ad, 10, 300000);
    const nv = await nhanVien();
    const kh = await ok(await nv.post('/api/khach-hang', { data: { hoTen: 'Khách quầy HD', sdt: `08${RUN}11`.slice(0, 11) } }));

    const t = await ok(await nv.post('/api/hoa-don/tinh-truoc', { data: { maKh: kh.maKh, dongHangs: [{ maSp: sp.maSp, soLuong: 4 }] } }));
    expect(t).toMatchObject({ tamTinh: 1200000, tienGiam: 60000, tongTien: 1140000 });

    const hd = await ok(await nv.post('/api/hoa-don/ban-tai-quay', {
      data: { maKh: kh.maKh, phuongThucTt: 'TIEN_MAT', dongHangs: [{ maSp: sp.maSp, soLuong: 4 }] } }));
    expect(hd).toMatchObject({ maKh: kh.maKh, trangThai: 'DA_THANH_TOAN', tongTien: 1140000, maDon: null });
    expect(hd.chiTiets).toHaveLength(1);
    expect(await ton(ad, sp.maSp)).toBe(6);
    expect((await ok(await nv.get(`/api/khach-hang/${kh.maKh}`))).diemTichLuy).toBe(114);

    const ct = await ok(await nv.get(`/api/hoa-don/${hd.maHoaDon}/chi-tiet`));
    expect(ct[0]).toMatchObject({ maSp: sp.maSp, soLuong: 4, donGiaBan: 300000 });
    const ds = await ok(await nv.get(`/api/hoa-don?maKh=${kh.maKh}`));
    expect(ds.map((h: any) => h.maHoaDon)).toEqual([hd.maHoaDon]);

    const pdf = await nv.get(`/api/hoa-don/${hd.maHoaDon}/pdf`);
    expect(pdf.status()).toBe(200);
    expect((await pdf.body()).subarray(0, 4).toString()).toBe('%PDF');

    expect((await nv.post(`/api/hoa-don/${hd.maHoaDon}/huy`)).status()).toBe(204);
    expect(await ton(ad, sp.maSp)).toBe(10);
    expect((await ok(await nv.get(`/api/khach-hang/${kh.maKh}`))).diemTichLuy).toBe(0);
    await loi(await nv.post(`/api/hoa-don/${hd.maHoaDon}/huy`), 400, /đã bị hủy/);
  });

  test('ban tai quay khach vang lai + loi', async () => {
    const ad = await admin();
    const sp = await taoSanPhamCoTon(ad, 2, 100000);
    const nv = await nhanVien();
    const hd = await ok(await nv.post('/api/hoa-don/ban-tai-quay', {
      data: { phuongThucTt: 'CHUYEN_KHOAN', dongHangs: [{ maSp: sp.maSp, soLuong: 1 }] } }));
    expect(hd.maKh).toBeNull();
    await loi(await nv.post('/api/hoa-don/ban-tai-quay', { data: { phuongThucTt: 'COD', dongHangs: [{ maSp: sp.maSp, soLuong: 1 }] } }), 400, /tiền mặt/);
    await loi(await nv.post('/api/hoa-don/ban-tai-quay', { data: { maKh: 'KH999', dongHangs: [{ maSp: sp.maSp, soLuong: 1 }] } }), 404);
    await loi(await nv.post('/api/hoa-don/ban-tai-quay', { data: { dongHangs: [{ maSp: sp.maSp, soLuong: 5 }] } }), 400, /chỉ còn 1/);
    await loi(await nv.post('/api/hoa-don/ban-tai-quay', { data: { dongHangs: [] } }), 400);
    expect(await ton(ad, sp.maSp)).toBe(1);
    await loi(await nv.get('/api/hoa-don/HD99999'), 404);
    await loi(await nv.post('/api/hoa-don/HD99999/huy'), 404);
  });

  test('main-sub: danh sach loc theo trang thai / ngay; chi tiet 1 cau SQL', async () => {
    const nv = await nhanVien();
    const ds = await ok(await nv.get('/api/hoa-don?trangThai=DA_THANH_TOAN&tuNgay=2025-01-01&denNgay=2025-12-31'));
    expect(ds.length).toBeGreaterThan(0);
    for (const h of ds) {
      expect(h.trangThai).toBe('DA_THANH_TOAN');
      expect(h.ngayLap.slice(0, 4)).toBe('2025');
    }
    const r = await nv.get(`/api/hoa-don/${ds[0].maHoaDon}/chi-tiet`);
    expect(r.status()).toBe(200);
    expect(r.headers()['x-so-cau-sql']).toBe('1');
    expect(r.headers()['x-sql-vi-tri']).toMatch(/\.cs:\d+/);
  });

  test('khach xem hoa don cua minh, khong xem duoc cua nguoi khac', async () => {
    const kh = await khach('khach01');
    const cuaToi = await ok(await kh.get('/api/hoa-don/cua-toi'));
    expect(cuaToi.length).toBeGreaterThan(0);
    expect(cuaToi.every((h: any) => h.maKh === 'KH001')).toBeTruthy();
    await ok(await kh.get(`/api/hoa-don/${cuaToi[0].maHoaDon}`));
    const nv = await nhanVien();
    const khac = (await ok(await nv.get('/api/hoa-don?maKh=KH002')))[0];
    await loi(await kh.get(`/api/hoa-don/${khac.maHoaDon}`), 404);
    expect((await kh.get(`/api/hoa-don/${khac.maHoaDon}/pdf`)).status()).toBe(404);
    // hoa don khach vang lai (ma_kh null) cung khong duoc xem
    const vangLai = (await ok(await nv.get('/api/hoa-don'))).find((h: any) => h.maKh === null);
    if (vangLai) await loi(await kh.get(`/api/hoa-don/${vangLai.maHoaDon}`), 404);
  });

  test('phan quyen hoa don', async () => {
    const kh = await khach();
    expect((await kh.get('/api/hoa-don')).status()).toBe(403);
    expect((await kh.post('/api/hoa-don/ban-tai-quay', { data: {} })).status()).toBe(403);
    expect((await kh.get('/api/hoa-don/HD00001/chi-tiet')).status()).toBe(403);
    expect((await (await nhanVien()).get('/api/hoa-don/cua-toi')).status()).toBe(403);
  });
});
