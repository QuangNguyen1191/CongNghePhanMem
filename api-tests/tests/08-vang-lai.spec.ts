import { test, expect, request } from '@playwright/test';
import { admin, anDanh, BASE, dangKy, loi, nhanVien, ok, sdtMoi, taoSanPhamCoTon, xacThucSdt } from './helpers';

// v3: khach vang lai mua hang khong can tai khoan (xac thuc SDT bang OTP) + uu dai thanh vien moi
const vangLai = async (token: string) =>
  request.newContext({ baseURL: BASE, extraHTTPHeaders: { Authorization: `Bearer ${token}`, Accept: 'application/json' } });

test.describe('Khach vang lai', () => {
  test('OTP: sdt sai 400, gui lai ngay 400, nhap sai bao so lan con lai', async () => {
    const api = await anDanh();
    await loi(await api.post('/api/vang-lai/otp', { data: { sdt: '12345' } }), 400, /không hợp lệ/);
    const sdt = sdtMoi(80);
    const gui = await ok(await api.post('/api/vang-lai/otp', { data: { sdt, mucDich: 'TRA_CUU' } }));
    expect(gui).toMatchObject({ sdt, hetHanSauGiay: 300, guiLaiSauGiay: 60 });
    await loi(await api.post('/api/vang-lai/otp', { data: { sdt } }), 400, /đợi/);
    const sai = gui.maDemo === '000000' ? '111111' : '000000';
    await loi(await api.post('/api/vang-lai/xac-thuc', { data: { sdt, ma: sai } }), 400, /còn 4 lần/);
    const phien = await ok(await api.post('/api/vang-lai/xac-thuc', { data: { sdt, ma: gui.maDemo } }));
    expect(phien).toMatchObject({ sdt, coTaiKhoan: false, soDonCu: 0 });
    expect(phien.token).toBeTruthy();
  });

  test('dat hang -> tra cuu -> huy; token vang lai khong dung duoc API khac', async () => {
    const sp = await taoSanPhamCoTon(await admin(), 20, 300000);
    const sdt = sdtMoi(81);
    const vl = await vangLai(await xacThucSdt(await anDanh(), sdt, 'MUA_HANG'));
    const don = await ok(await vl.post('/api/vang-lai/don-hang', {
      data: { hoTen: 'Khách Vãng Lai', email: 'vl@test.vn', diaChiGiao: '12 Hai Bà Trưng, Quận 1, TP HCM', phuongThucTt: 'COD',
        dongHangs: [{ maSp: sp.maSp, soLuong: 2 }] },
    }));
    expect(don).toMatchObject({ trangThai: 'CHO_XAC_NHAN', nguon: 'VANG_LAI', sdtNhan: sdt, emailNhan: 'vl@test.vn', tongTien: 600000 });
    const ds = await ok(await vl.get('/api/vang-lai/don-hang'));
    expect(ds.map((d: { maDon: string }) => d.maDon)).toEqual([don.maDon]);
    expect((await vl.get(`/api/vang-lai/don-hang/${don.maDon}/pdf`)).headers()['content-type']).toContain('application/pdf');

    // token vang lai chi dung cho /api/vang-lai
    expect((await vl.get(`/api/don-hang/${don.maDon}`)).status()).toBe(401);
    expect((await vl.get('/api/khach-hang/toi')).status()).toBe(401);
    // SDT khac khong xem duoc don nay
    const khac = await vangLai(await xacThucSdt(await anDanh(), sdtMoi(82), 'TRA_CUU'));
    await loi(await khac.get(`/api/vang-lai/don-hang/${don.maDon}`), 404);
    await loi(await khac.post(`/api/vang-lai/don-hang/${don.maDon}/huy`), 404);

    expect((await vl.post(`/api/vang-lai/don-hang/${don.maDon}/huy`)).status()).toBe(204);
    expect((await ok(await vl.get(`/api/vang-lai/don-hang/${don.maDon}`))).trangThai).toBe('DA_HUY');
  });

  test('chong don ao: toi da 5 cai / SP, 2 don dang cho, COD toi da 5 trieu', async () => {
    const sp = await taoSanPhamCoTon(await admin(), 50, 1200000);
    const vl = await vangLai(await xacThucSdt(await anDanh(), sdtMoi(83), 'MUA_HANG'));
    const don = (soLuong: number, pt = 'COD') => vl.post('/api/vang-lai/don-hang', {
      data: { hoTen: 'Chống Đơn Ảo', diaChiGiao: '1 Lê Duẩn, Quận 1, TP HCM', phuongThucTt: pt, dongHangs: [{ maSp: sp.maSp, soLuong }] },
    });
    await loi(await don(6), 400, /tối đa 5 cái/);
    await loi(await don(5), 400, /COD/);                // 6 trieu
    await ok(await don(5, 'CHUYEN_KHOAN'));
    await ok(await don(1));
    await loi(await don(1), 400, /đang có 2 đơn chờ/);
  });

  test('nhan vien: loc don vang lai, xem rui ro SDT bom hang', async () => {
    const nv = await nhanVien();
    const ds = await ok(await nv.get('/api/don-hang?nguon=VANG_LAI'));
    expect(ds.length).toBeGreaterThan(0);
    for (const d of ds) expect(d.nguon).toBe('VANG_LAI');
    const rr = await ok(await nv.get('/api/khach-hang/rui-ro/0987000022'));   // du lieu mau: 2 lan khong nhan hang
    expect(rr).toMatchObject({ mucRuiRo: 'CAO', soLanBomHang: 2, duocDatCod: false, coTaiKhoan: false });
    // SDT nguoi nhan khac SDT cua khach: gui kem maKh thi van ra lich su cua khach, khong bao "khach moi"
    const sdtLa = sdtMoi(84);
    expect((await ok(await nv.get(`/api/khach-hang/rui-ro/${sdtLa}`))).maKh ?? null).toBeNull();
    expect(await ok(await nv.get(`/api/khach-hang/rui-ro/${sdtLa}?maKh=KH022`))).toMatchObject({ maKh: 'KH022', soLanBomHang: 2, mucRuiRo: 'CAO' });
    // khach thuong khong xem duoc
    expect((await (await anDanh()).get('/api/khach-hang/rui-ro/0987000022')).status()).toBe(401);
  });
});

test.describe('Dang ky + uu dai thanh vien moi', () => {
  test('dang ky khong co OTP -> 400; tao tai khoan nhanh tu phien vang lai -> gan don cu', async () => {
    const api = await anDanh();
    await loi(await api.post('/api/tai-khoan/dang-ky', {
      data: { tenDangNhap: `kotp${sdtMoi(84)}`, matKhau: '123456', khachHang: { hoTen: 'Không OTP', sdt: sdtMoi(84) } },
    }), 400, /OTP/);

    const sp = await taoSanPhamCoTon(await admin(), 10, 250000);
    const sdt = sdtMoi(85);
    const token = await xacThucSdt(api, sdt, 'MUA_HANG');
    const don = await ok(await (await vangLai(token)).post('/api/vang-lai/don-hang', {
      data: { hoTen: 'Sẽ Đăng Ký', diaChiGiao: '9 Pasteur, Quận 3, TP HCM', phuongThucTt: 'COD', dongHangs: [{ maSp: sp.maSp, soLuong: 1 }] },
    }));
    // chi can ten dang nhap + mat khau, ho so lay tu don vang lai
    const p = await ok(await api.post('/api/tai-khoan/dang-ky', {
      data: { tenDangNhap: `nhanh${sdt}`, matKhau: '123456', khachHang: { sdt }, phienXacThuc: token },
    }));
    expect(p).toMatchObject({ hoTen: 'Sẽ Đăng Ký', maKh: don.maKh, soDonDaGan: 1 });
    const kh = await request.newContext({ baseURL: BASE, extraHTTPHeaders: { Authorization: `Bearer ${p.token}` } });
    expect((await ok(await kh.get('/api/don-hang/cua-toi'))).map((d: { maDon: string }) => d.maDon)).toContain(don.maDon);

    // uu dai: 3 luot, don vang lai cu khong tinh
    const ud = await ok(await kh.get('/api/khuyen-mai/thanh-vien-moi'));
    expect(ud).toMatchObject({ soDonDau: 3, phanTramGiamGia: 10, giamToiDa: 100000, daDung: 0, conLai: 3 });
    // khach chua dang nhap: chi thay chuong trinh
    expect(await ok(await api.get('/api/khuyen-mai/thanh-vien-moi'))).toMatchObject({ soDonDau: 3, conLai: null });
  });

  test('3 don dau giam 10% (toi da 100.000d), don thu 4 het uu dai', async () => {
    const ad = await admin();
    const sp = await taoSanPhamCoTon(ad, 30, 400000);
    const api = await anDanh();
    const p = await ok(await dangKy(api, { tenDangNhap: `uudai${sdtMoi(86)}`, matKhau: '123456', khachHang: { hoTen: 'Ưu Đãi' } }));
    const kh = await request.newContext({ baseURL: BASE, extraHTTPHeaders: { Authorization: `Bearer ${p.token}` } });

    const tinh = await ok(await kh.post('/api/khuyen-mai/tinh-tien', { data: { dongHangs: [{ maSp: sp.maSp, soLuong: 1 }] } }));
    expect(tinh).toMatchObject({ loaiGiam: 'THANH_VIEN_MOI', tienGiam: 40000, tongTien: 360000 });
    // don lon 1.6tr: KM hoa don 5% = 80.000d < uu dai 10% nhung chi giam toi da 100.000d
    const lon = await ok(await kh.post('/api/khuyen-mai/tinh-tien', { data: { dongHangs: [{ maSp: sp.maSp, soLuong: 4 }] } }));
    expect(lon).toMatchObject({ loaiGiam: 'THANH_VIEN_MOI', tienGiam: 100000 });

    for (let i = 1; i <= 4; i++) {
      await ok(await kh.post('/api/gio-hang', { data: { maSp: sp.maSp, soLuong: 1 } }));
      const d = await ok(await kh.post('/api/don-hang', { data: { diaChiGiao: '1 Lê Lợi, Quận 1, TP HCM', sdtNhan: '0911111111' } }));
      if (i <= 3) expect(d).toMatchObject({ nguon: 'TAI_KHOAN', tienGiam: 40000, maKm: 'KM06' });
      else expect(d.tienGiam).toBe(0);
    }
    expect(await ok(await kh.get('/api/khuyen-mai/thanh-vien-moi'))).toMatchObject({ daDung: 4, conLai: 0 });

    // xac nhan don thanh vien moi -> hoa don ghi KM06 (thong ke hieu qua khuyen mai)
    const nv = await nhanVien();
    const don1 = (await ok(await kh.get('/api/don-hang/cua-toi'))).find((d: { tienGiam: number }) => d.tienGiam === 40000);
    const hd = await ok(await nv.post(`/api/don-hang/${don1.maDon}/xac-nhan`));
    expect(hd).toMatchObject({ maKm: 'KM06', tienGiam: 40000 });
  });

  test('admin tao khuyen mai THANH_VIEN_MOI: kiem tra du lieu', async () => {
    const ad = await admin();
    await loi(await ad.post('/api/khuyen-mai', {
      data: { tenKm: 'TV mới sai', hinhThuc: 'THANH_VIEN_MOI', ngayBd: '2027-01-01', ngayKt: '2027-02-01' },
    }), 400, /thành viên mới/);
    await loi(await ad.post('/api/khuyen-mai', {
      data: { tenKm: 'TV mới sai', hinhThuc: 'THANH_VIEN_MOI', ngayBd: '2027-01-01', ngayKt: '2027-02-01',
        thanhVienMoi: { soDonDau: 0, phanTramGiamGia: 10 } },
    }), 400, /1 đến 10/);
    const km = await ok(await ad.post('/api/khuyen-mai', {
      data: { tenKm: 'TV mới 2027', hinhThuc: 'THANH_VIEN_MOI', ngayBd: '2028-01-01', ngayKt: '2028-02-01',
        thanhVienMoi: { soDonDau: 2, phanTramGiamGia: 15, giamToiDa: 50000 } },
    }));
    expect(km).toMatchObject({ hinhThuc: 'THANH_VIEN_MOI', phanTramGiamGia: 15, thanhVienMoi: { soDonDau: 2, giamToiDa: 50000 } });
  });
});
