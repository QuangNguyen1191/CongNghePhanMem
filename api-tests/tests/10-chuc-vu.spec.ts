import { test, expect, request } from '@playwright/test';
import { admin, banOnline, BASE, dangNhap, khach, nhanVien, ok, RUN, thuKho, anDanh } from './helpers';

// Phan quyen theo CHUC VU ben trong vai tro NHAN_VIEN (BLL/PhanQuyenBLL.cs, WebApi/HaTang/CanQuyen.cs):
//   thu ngan -> ban tai quay | ban hang online -> don online | thu kho -> kho | ADMIN -> tat ca
// Cac lenh ghi deu gui du lieu SAI co y: duoc qua chot quyen thi nhan 400 (loi du lieu), bi chan thi 403 -> khong ghi gi vao CSDL.

test.describe('Phan quyen theo chuc vu', () => {
  test('dang nhap tra ve nhom quyen theo chuc vu', async () => {
    const api = await anDanh();
    const quyen = async (ten: string, noi: 'web-staff' | 'web-ban-hang') => (await ok(await dangNhap(api, ten, noi))).nhomQuyen;
    expect(await quyen('thungan1', 'web-staff')).toEqual(['ban-hang']);
    expect(await quyen('banhangonline', 'web-staff')).toEqual(['don-online']);
    expect(await quyen('thukho', 'web-staff')).toEqual(['kho']);
    expect(await quyen('quanly', 'web-staff')).toEqual(['ban-hang', 'don-online', 'kho']);
    expect(await quyen('khach01', 'web-ban-hang')).toEqual([]);
  });

  // [mo ta, API, body (lenh ghi), thu ngan, ban online, thu kho, admin]: true = duoc qua chot quyen
  const BAN_SAI = { phuongThucTt: 'SAI', dongHangs: [] };
  const PN_RONG = { maNcc: 'NCC01', chiTiets: [] };
  const TIM_RONG = { dieuKiens: [], khopTatCa: true };
  const MA_TRAN: [string, string, string, object | null, boolean, boolean, boolean, boolean][] = [
    ['ban tai quay', 'POST', '/api/hoa-don/ban-tai-quay', BAN_SAI, true, false, false, true],
    ['tinh tien truoc tai quay', 'POST', '/api/hoa-don/tinh-truoc', { dongHangs: [] }, true, false, false, true],
    ['danh sach don online', 'GET', '/api/don-hang?trangThai=CHO_XAC_NHAN', null, false, true, false, true],
    ['xac nhan don online', 'POST', '/api/don-hang/DH99999/xac-nhan', null, false, true, false, true],
    ['lap phieu nhap', 'POST', '/api/phieu-nhap', PN_RONG, false, false, true, true],
    ['danh sach phieu nhap', 'GET', '/api/phieu-nhap', null, false, false, true, true],
    ['danh sach nha cung cap', 'GET', '/api/nha-cung-cap', null, false, false, true, true],
    ['danh sach hoa don', 'GET', '/api/hoa-don', null, true, true, false, true],
    ['danh sach doi tra', 'GET', '/api/doi-tra', null, true, true, false, true],
    ['danh sach hoan tien', 'GET', '/api/hoan-tien', null, true, true, false, true],
    ['danh sach khach hang (SDT, dia chi)', 'GET', '/api/khach-hang', null, true, true, false, true],
    ['tim nang cao hoa don', 'POST', '/api/tim-kiem/hoa-don', TIM_RONG, true, true, false, true],
    ['xuat excel khach hang', 'GET', '/api/excel/khach-hang', null, true, true, false, true],
    ['xuat excel phieu nhap', 'GET', '/api/excel/phieu-nhap', null, false, false, true, true],
    ['san pham (moi nhan vien)', 'GET', '/api/san-pham/ton-thap', null, true, true, true, true],
    ['nhat ky SQL (moi nhan vien)', 'GET', '/api/nhat-ky-sql?soLuong=5', null, true, true, true, true],
  ];

  test('ma tran: moi chuc vu chi goi duoc API cua viec minh', async () => {
    const nguoi = [await nhanVien(), await banOnline(), await thuKho(), await admin()];
    const ten = ['thu ngan', 'ban online', 'thu kho', 'admin'];
    const sai: string[] = [];
    for (const [moTa, m, url, body, ...duoc] of MA_TRAN) {
      for (let i = 0; i < nguoi.length; i++) {
        const r = await nguoi[i].fetch(url, { method: m, data: body ?? undefined });
        const qua = r.status() !== 403;
        if (qua !== duoc[i]) sai.push(`${ten[i]} / ${moTa}: ${r.status()} (mong ${duoc[i] ? 'duoc qua' : '403'})`);
      }
    }
    expect(sai, sai.join('\n')).toEqual([]);
  });

  test('403 theo chuc vu co thong bao tieng Viet', async () => {
    const r = await (await thuKho()).post('/api/hoa-don/ban-tai-quay', { data: BAN_SAI });
    expect(r.status()).toBe(403);
    expect((await r.json()).thongBao).toMatch(/Chức vụ của bạn không có quyền.*bán tại quầy/);
  });

  test('khach hang khong bi anh huong: van xem chung tu cua minh', async () => {
    const kh = await khach();
    await ok(await kh.get('/api/hoa-don/cua-toi'));
    await ok(await kh.get('/api/don-hang/cua-toi'));
    await ok(await kh.get('/api/khach-hang/toi'));
  });

  test('admin doi chuc vu -> quyen doi ngay voi token dang dung', async () => {
    const ad = await admin();
    const ten = `cv${RUN}`;
    const nv = await ok(await ad.post('/api/nhan-vien', {
      data: { nhanVien: { hoTen: 'NV Chức Vụ', email: `${ten}@shop.vn`, chucVu: 'Thủ kho', luong: 7000000 },
        tenDangNhap: ten, matKhau: '123456', vaiTro: 'NHAN_VIEN' },
    }));
    const phien = await ok(await dangNhap(await anDanh(), ten, 'web-staff'));
    expect(phien.nhomQuyen).toEqual(['kho']);
    const api = await request.newContext({ baseURL: BASE, extraHTTPHeaders: { Authorization: `Bearer ${phien.token}` } });

    expect((await api.post('/api/phieu-nhap', { data: PN_RONG })).status()).toBe(400);           // kho: qua chot
    expect((await api.post('/api/hoa-don/ban-tai-quay', { data: BAN_SAI })).status()).toBe(403);

    expect((await ad.put(`/api/nhan-vien/${nv.maNv}`, {
      data: { hoTen: 'NV Chức Vụ', email: `${ten}@shop.vn`, chucVu: 'Thu ngân', luong: 7000000 } })).status()).toBe(204);
    // cung token, khong dang nhap lai
    expect((await api.post('/api/hoa-don/ban-tai-quay', { data: BAN_SAI })).status()).toBe(400);
    expect((await api.post('/api/phieu-nhap', { data: PN_RONG })).status()).toBe(403);

    // chuc vu khong khop tu khoa nao -> khong co nhom nao (mac dinh an toan), van xem san pham duoc
    expect((await ad.put(`/api/nhan-vien/${nv.maNv}`, {
      data: { hoTen: 'NV Chức Vụ', email: `${ten}@shop.vn`, chucVu: 'Tạp vụ', luong: 7000000 } })).status()).toBe(204);
    expect((await api.post('/api/hoa-don/ban-tai-quay', { data: BAN_SAI })).status()).toBe(403);
    expect((await api.get('/api/hoa-don')).status()).toBe(403);
    await ok(await api.get('/api/san-pham/ton-thap'));

    expect((await ad.post(`/api/nhan-vien/${nv.maNv}/nghi-viec`)).status()).toBe(204);
    await api.dispose();
  });
});
