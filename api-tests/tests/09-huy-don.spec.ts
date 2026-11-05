import { test, expect, request, type APIRequestContext } from '@playwright/test';
import { admin, anDanh, BASE, dangKy, khach, loi, nhanVien, ok, RUN, sdtMoi, taoSanPhamCoTon, xacThucSdt } from './helpers';

// v3: huy don / yeu cau huy don da xac nhan, nhan vien duyet - tu choi, hoan tien don da tra truoc
async function khachMoi(ten: string) {
  const p = await ok(await dangKy(await anDanh(), { tenDangNhap: `${ten}${RUN}`, matKhau: '123456', khachHang: { hoTen: `Khách ${ten}` } }));
  return request.newContext({ baseURL: BASE, extraHTTPHeaders: { Authorization: `Bearer ${p.token}` } });
}
async function datDon(kh: APIRequestContext, maSp: string, phuongThucTt = 'COD') {
  await ok(await kh.post('/api/gio-hang', { data: { maSp, soLuong: 1 } }));
  return ok(await kh.post('/api/don-hang', { data: { diaChiGiao: '5 Đinh Tiên Hoàng, Hà Nội', sdtNhan: '0911111111', phuongThucTt } }));
}
const ton = async (api: APIRequestContext, maSp: string) => (await ok(await api.get(`/api/san-pham/${maSp}`))).soLuong as number;

test.describe('Huy don', () => {
  test('cho xac nhan: yeu cau huy = huy ngay, bat buoc ly do', async () => {
    const sp = await taoSanPhamCoTon(await admin(), 10);
    const kh = await khachMoi('hy1');
    const don = await datDon(kh, sp.maSp);
    await loi(await kh.post(`/api/don-hang/${don.maDon}/yeu-cau-huy`, { data: { lyDo: ' ' } }), 400, /lý do/);
    const sau = await ok(await kh.post(`/api/don-hang/${don.maDon}/yeu-cau-huy`, { data: { lyDo: 'Đặt nhầm' } }));
    expect(sau).toMatchObject({ trangThai: 'DA_HUY', nguoiHuy: 'KHACH', lyDoHuy: 'Đặt nhầm', yeuCauHuy: null });
    // khach khac khong dung duoc don nay
    await loi(await (await khach('khach02')).post(`/api/don-hang/${don.maDon}/yeu-cau-huy`, { data: { lyDo: 'x' } }), 400);
  });

  test('da xac nhan (chuyen khoan): xin huy -> chan giao -> duyet: tra kho, huy HD, tao phieu hoan tien -> da hoan', async () => {
    const ad = await admin();
    const nv = await nhanVien();
    const sp = await taoSanPhamCoTon(ad, 10, 300000);
    const kh = await khachMoi('hy2');
    const don = await datDon(kh, sp.maSp, 'CHUYEN_KHOAN');
    const hd = await ok(await nv.post(`/api/don-hang/${don.maDon}/xac-nhan`));
    expect(await ton(ad, sp.maSp)).toBe(9);

    const yc = await ok(await kh.post(`/api/don-hang/${don.maDon}/yeu-cau-huy`, { data: { lyDo: 'Đổi ý' } }));
    expect(yc).toMatchObject({ trangThai: 'DA_XAC_NHAN', yeuCauHuy: 'CHO_DUYET', lyDoHuy: 'Đổi ý' });
    await loi(await kh.post(`/api/don-hang/${don.maDon}/yeu-cau-huy`, { data: { lyDo: 'x' } }), 400, /đang xem xét/);
    await loi(await nv.post(`/api/don-hang/${don.maDon}/giao-hang`), 400, /xin hủy/);
    expect((await ok(await nv.get('/api/don-hang?yeuCauHuy=CHO_DUYET'))).some((d: any) => d.maDon === don.maDon)).toBeTruthy();
    // khach khong tu duyet duoc
    expect((await kh.post(`/api/don-hang/${don.maDon}/duyet-huy`)).status()).toBe(403);

    expect((await nv.post(`/api/don-hang/${don.maDon}/duyet-huy`)).status()).toBe(204);
    const sau = await ok(await kh.get(`/api/don-hang/${don.maDon}`));
    expect(sau).toMatchObject({ trangThai: 'DA_HUY', yeuCauHuy: 'DA_DUYET', nguoiHuy: 'KHACH', trangThaiHoanTien: 'CHO_HOAN', soTienHoan: hd.tongTien });
    expect(await ton(ad, sp.maSp)).toBe(10);
    expect((await ok(await nv.get(`/api/hoa-don/${hd.maHoaDon}`))).trangThai).toBe('DA_HUY');

    const ds = await ok(await nv.get('/api/hoan-tien?trangThai=CHO_HOAN'));
    const phieu = ds.find((h: any) => h.maDon === don.maDon);
    expect(phieu).toMatchObject({ soTien: hd.tongTien, phuongThuc: 'CHUYEN_KHOAN', trangThai: 'CHO_HOAN' });
    expect((await kh.get('/api/hoan-tien')).status()).toBe(403);
    const daHoan = await ok(await nv.post(`/api/hoan-tien/${phieu.maHoanTien}/da-hoan`, { data: { lyDo: 'Đã chuyển khoản lại' } }));
    expect(daHoan).toMatchObject({ trangThai: 'DA_HOAN', ghiChu: 'Đã chuyển khoản lại' });
    await loi(await nv.post(`/api/hoan-tien/${phieu.maHoanTien}/da-hoan`), 400, /đã hoàn/);
    expect((await ok(await kh.get(`/api/don-hang/${don.maDon}`))).trangThaiHoanTien).toBe('DA_HOAN');
  });

  test('tu choi yeu cau (bat buoc ly do) -> khong xin lai duoc; dang giao khong huy online; nhan vien huy co ly do', async () => {
    const nv = await nhanVien();
    const sp = await taoSanPhamCoTon(await admin(), 10);
    const kh = await khachMoi('hy3');
    const don = await datDon(kh, sp.maSp);
    await ok(await nv.post(`/api/don-hang/${don.maDon}/xac-nhan`));
    await loi(await nv.post(`/api/don-hang/${don.maDon}/tu-choi-huy`, { data: { lyDo: 'x' } }), 400, /không có yêu cầu/);
    await ok(await kh.post(`/api/don-hang/${don.maDon}/yeu-cau-huy`, { data: { lyDo: 'Muốn đổi màu' } }));
    await loi(await nv.post(`/api/don-hang/${don.maDon}/tu-choi-huy`, { data: {} }), 400, /lý do từ chối/);
    expect((await nv.post(`/api/don-hang/${don.maDon}/tu-choi-huy`, { data: { lyDo: 'Hàng đã đóng gói' } })).status()).toBe(204);
    expect(await ok(await kh.get(`/api/don-hang/${don.maDon}`))).toMatchObject({ yeuCauHuy: 'TU_CHOI', phanHoiHuy: 'Hàng đã đóng gói' });
    await loi(await kh.post(`/api/don-hang/${don.maDon}/yeu-cau-huy`, { data: { lyDo: 'lần 2' } }), 400, /từ chối/);

    expect((await nv.post(`/api/don-hang/${don.maDon}/giao-hang`)).status()).toBe(204);
    await loi(await kh.post(`/api/don-hang/${don.maDon}/yeu-cau-huy`, { data: { lyDo: 'x' } }), 400, /đang được giao/);
    expect((await nv.post(`/api/don-hang/${don.maDon}/huy`, { data: { lyDo: 'Khách không nhận hàng' } })).status()).toBe(204);
    expect(await ok(await kh.get(`/api/don-hang/${don.maDon}`))).toMatchObject({ trangThai: 'DA_HUY', nguoiHuy: 'NHAN_VIEN', lyDoHuy: 'Khách không nhận hàng', trangThaiHoanTien: null });
  });

  test('khach vang lai xin huy don da xac nhan; duyet xong khong bi tinh bom hang', async () => {
    const nv = await nhanVien();
    const sp = await taoSanPhamCoTon(await admin(), 10);
    const sdt = sdtMoi(95);
    const vl = await request.newContext({ baseURL: BASE, extraHTTPHeaders: { Authorization: `Bearer ${await xacThucSdt(await anDanh(), sdt, 'MUA_HANG')}` } });
    const don = await ok(await vl.post('/api/vang-lai/don-hang', {
      data: { hoTen: 'Khách Hủy', diaChiGiao: '9 Hàng Bài, Hoàn Kiếm, Hà Nội', phuongThucTt: 'COD', dongHangs: [{ maSp: sp.maSp, soLuong: 1 }] } }));
    await ok(await nv.post(`/api/don-hang/${don.maDon}/xac-nhan`));
    await loi(await vl.post(`/api/vang-lai/don-hang/${don.maDon}/huy`), 400, /yêu cầu hủy/);
    const yc = await ok(await vl.post(`/api/vang-lai/don-hang/${don.maDon}/yeu-cau-huy`, { data: { lyDo: 'Không cần nữa' } }));
    expect(yc.yeuCauHuy).toBe('CHO_DUYET');
    expect((await nv.post(`/api/don-hang/${don.maDon}/duyet-huy`)).status()).toBe(204);
    expect((await ok(await vl.get(`/api/vang-lai/don-hang/${don.maDon}`))).trangThai).toBe('DA_HUY');
    expect((await ok(await nv.get(`/api/khach-hang/rui-ro/${sdt}`))).soLanBomHang).toBe(0);
  });
});
