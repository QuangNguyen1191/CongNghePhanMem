import { test, expect } from '@playwright/test';
import { admin, anDanh, khach, loi, nhanVien, ok, RUN, taoSanPhamCoTon } from './helpers';

test.describe('LoaiSp', () => {
  test('ai cung xem duoc danh sach loai', async () => {
    const ds = await ok(await (await anDanh()).get('/api/loai-sp'));
    expect(ds.map((l: any) => l.maLoaiSp)).toContain('LSP01');
  });

  test('admin them / sua / xoa loai, trung ten 400', async () => {
    const ad = await admin();
    const l = await ok(await ad.post('/api/loai-sp', { data: { tenLoaiSp: `Loại ${RUN}` } }));
    expect(l.maLoaiSp).toMatch(/^LSP\d+/);
    await loi(await ad.post('/api/loai-sp', { data: { tenLoaiSp: `Loại ${RUN}` } }), 400, /đã tồn tại/);
    await loi(await ad.post('/api/loai-sp', { data: { tenLoaiSp: '  ' } }), 400);
    expect((await ad.put(`/api/loai-sp/${l.maLoaiSp}`, { data: { tenLoaiSp: `Loại sửa ${RUN}` } })).status()).toBe(204);
    const ds = await ok(await ad.get('/api/loai-sp'));
    expect(ds.find((x: any) => x.maLoaiSp === l.maLoaiSp).tenLoaiSp).toBe(`Loại sửa ${RUN}`);
    await loi(await ad.put('/api/loai-sp/LSP99', { data: { tenLoaiSp: `Không có ${RUN}` } }), 404);
    expect((await ad.delete(`/api/loai-sp/${l.maLoaiSp}`)).status()).toBe(204);
    await loi(await ad.delete(`/api/loai-sp/${l.maLoaiSp}`), 404);
    await loi(await ad.delete('/api/loai-sp/LSP01'), 400, /còn sản phẩm/);
  });

  test('body null / sai kieu -> 400 { thongBao } (khong 500)', async () => {
    const ad = await admin();
    await loi(await ad.post('/api/loai-sp', { data: { tenLoaiSp: null } }), 400, /không hợp lệ/);
    await loi(await ad.post('/api/san-pham', { data: { tenSp: 'x', maLoaiSp: 'LSP01', donGia: 'abc' } }), 400, /không hợp lệ/);
    await loi(await ad.post('/api/san-pham', { data: '{khong phai json', headers: { 'content-type': 'application/json' } }), 400);
    await loi(await ad.post('/api/nha-cung-cap', { data: { tenNcc: null } }), 400);
  });

  test('nhan vien khong them duoc loai', async () => {
    expect((await (await nhanVien()).post('/api/loai-sp', { data: { tenLoaiSp: 'x' } })).status()).toBe(403);
  });
});

test.describe('SanPham', () => {
  test('danh sach, loc dang ban, tim tu khoa, loc loai', async () => {
    const api = await anDanh();
    const tatCa = await ok(await api.get('/api/san-pham'));
    expect(tatCa.length).toBeGreaterThanOrEqual(30);
    const dangBan = await ok(await api.get('/api/san-pham?chiDangBan=true'));
    expect(dangBan.every((s: any) => s.trangThai === 'DANG_BAN')).toBeTruthy();
    const tim = await ok(await api.get('/api/san-pham?tuKhoa=AIRism'));
    expect(tim.length).toBeGreaterThan(0);
    expect(tim.every((s: any) => /AIRism/i.test(s.tenSp + s.maSp + (s.chatLieu ?? '') + (s.moTa ?? '')))).toBeTruthy();
    const theoLoai = await ok(await api.get('/api/san-pham?maLoaiSp=LSP04'));
    expect(theoLoai.length).toBeGreaterThan(0);
    expect(theoLoai.every((s: any) => s.maLoaiSp === 'LSP04')).toBeTruthy();
    // tim tieng Viet co dau
    const timVn = await ok(await api.get(`/api/san-pham?tuKhoa=${encodeURIComponent('Áo thun')}`));
    expect(timVn.length).toBeGreaterThan(0);
  });

  test('xem chi tiet, bien the, 404', async () => {
    const api = await anDanh();
    const sp = await ok(await api.get('/api/san-pham/SP001'));
    expect(sp).toMatchObject({ maSp: 'SP001', tenLoaiSp: expect.any(String) });
    const bt = await ok(await api.get('/api/san-pham/SP001/bien-the'));
    expect(bt.length).toBeGreaterThan(1);
    expect(bt.every((s: any) => s.tenSp === sp.tenSp)).toBeTruthy();
    await loi(await api.get('/api/san-pham/SP999'), 404);
    await loi(await api.get('/api/san-pham/SP999/bien-the'), 404);
  });

  test('gia khuyen mai hom nay la dictionary maSp -> gia', async () => {
    const g = await ok(await (await anDanh()).get('/api/san-pham/gia-khuyen-mai'));
    expect(typeof g).toBe('object');
    for (const v of Object.values(g)) expect(typeof v).toBe('number');
  });

  test('ton thap chi cho nhan vien', async () => {
    expect((await (await anDanh()).get('/api/san-pham/ton-thap')).status()).toBe(401);
    expect((await (await khach()).get('/api/san-pham/ton-thap')).status()).toBe(403);
    const ds = await ok(await (await nhanVien()).get('/api/san-pham/ton-thap'));
    expect(ds.every((s: any) => s.soLuong < 5)).toBeTruthy();
  });

  test('admin them, them nhieu, sua, ngung ban / ban lai', async () => {
    const ad = await admin();
    const sp = await ok(await ad.post('/api/san-pham', {
      data: { tenSp: `Áo test ${RUN}`, maLoaiSp: 'LSP01', donGia: 150000, soLuong: 999, kichCo: 'm', mauSac: ' Xanh ' },
    }));
    expect(sp).toMatchObject({ soLuong: 0, kichCo: 'M', mauSac: 'Xanh', trangThai: 'DANG_BAN' });

    const nhieu = await ok(await ad.post('/api/san-pham/them-nhieu', {
      data: ['S', 'L', 'XL'].map(k => ({ tenSp: `Áo test ${RUN}`, maLoaiSp: 'LSP01', donGia: 150000, kichCo: k })),
    }));
    expect(nhieu).toHaveLength(3);
    expect(new Set(nhieu.map((s: any) => s.maSp)).size).toBe(3);
    const bt = await ok(await ad.get(`/api/san-pham/${sp.maSp}/bien-the`));
    expect(bt).toHaveLength(4);

    expect((await ad.put(`/api/san-pham/${sp.maSp}`, {
      data: { tenSp: `Áo test ${RUN}`, maLoaiSp: 'LSP01', donGia: 175000, kichCo: 'M', mauSac: 'Xanh', moTa: 'mô tả' },
    })).status()).toBe(204);
    const sau = await ok(await ad.get(`/api/san-pham/${sp.maSp}`));
    expect(sau).toMatchObject({ donGia: 175000, moTa: 'mô tả' });

    expect((await ad.post(`/api/san-pham/${sp.maSp}/ngung-ban`)).status()).toBe(204);
    expect((await ok(await ad.get(`/api/san-pham/${sp.maSp}`))).trangThai).toBe('NGUNG_BAN');
    const dangBan = await ok(await ad.get('/api/san-pham?chiDangBan=true'));
    expect(dangBan.some((s: any) => s.maSp === sp.maSp)).toBeFalsy();
    expect((await ad.post(`/api/san-pham/${sp.maSp}/ngung-ban?ngung=false`)).status()).toBe(204);
    expect((await ok(await ad.get(`/api/san-pham/${sp.maSp}`))).trangThai).toBe('DANG_BAN');
  });

  test('sua san pham khong lam thay doi ton kho', async () => {
    const ad = await admin();
    const sp = await taoSanPhamCoTon(ad, 12);
    expect((await ok(await ad.get(`/api/san-pham/${sp.maSp}`))).soLuong).toBe(12);
    expect((await ad.put(`/api/san-pham/${sp.maSp}`, {
      data: { tenSp: sp.tenSp, maLoaiSp: 'LSP01', donGia: 210000, soLuong: 0, kichCo: 'M' },
    })).status()).toBe(204);
    expect((await ok(await ad.get(`/api/san-pham/${sp.maSp}`))).soLuong).toBe(12);
  });

  test('validate san pham -> 400', async () => {
    const ad = await admin();
    await loi(await ad.post('/api/san-pham', { data: { tenSp: '', maLoaiSp: 'LSP01', donGia: 1 } }), 400, /tên/);
    await loi(await ad.post('/api/san-pham', { data: { tenSp: 'x', maLoaiSp: 'LSP01', donGia: 0 } }), 400, /Đơn giá/);
    await loi(await ad.post('/api/san-pham', { data: { tenSp: 'x', maLoaiSp: 'LSP99', donGia: 1 } }), 400, /Loại/);
    await loi(await ad.post('/api/san-pham', { data: { tenSp: 'x', maLoaiSp: 'LSP01', donGia: 1, trangThai: 'ABC' } }), 400);
    await loi(await ad.put('/api/san-pham/SP999', { data: { tenSp: 'x', maLoaiSp: 'LSP01', donGia: 1 } }), 404);
    await loi(await ad.post('/api/san-pham/SP999/ngung-ban'), 404);
    // them nhieu: 1 dong sai thi khong them dong nao
    const truoc = (await ok(await ad.get('/api/san-pham'))).length;
    await loi(await ad.post('/api/san-pham/them-nhieu', {
      data: [{ tenSp: `Lô ${RUN}`, maLoaiSp: 'LSP01', donGia: 1 }, { tenSp: '', maLoaiSp: 'LSP01', donGia: 1 }] }), 400);
    expect((await ok(await ad.get('/api/san-pham'))).length).toBe(truoc);
  });

  test('nhan vien / khach khong them sua duoc san pham', async () => {
    expect((await (await nhanVien()).post('/api/san-pham', { data: {} })).status()).toBe(403);
    expect((await (await khach()).put('/api/san-pham/SP001', { data: {} })).status()).toBe(403);
    expect((await (await anDanh()).post('/api/san-pham/SP001/ngung-ban')).status()).toBe(401);
  });
});

test.describe('NhaCungCap', () => {
  test('nhan vien xem, admin them / sua / xoa', async () => {
    const ds = await ok(await (await nhanVien()).get('/api/nha-cung-cap'));
    expect(ds.length).toBeGreaterThanOrEqual(4);
    const ad = await admin();
    const n = await ok(await ad.post('/api/nha-cung-cap', { data: { tenNcc: `NCC ${RUN}`, sdt: '0281234567', email: ' ' } }));
    expect(n).toMatchObject({ maNcc: expect.stringMatching(/^NCC/), trangThai: 'HOP_TAC', email: null });
    expect((await ad.put(`/api/nha-cung-cap/${n.maNcc}`, { data: { tenNcc: `NCC sửa ${RUN}`, trangThai: 'NGUNG_HOP_TAC' } })).status()).toBe(204);
    const sau = (await ok(await ad.get('/api/nha-cung-cap'))).find((x: any) => x.maNcc === n.maNcc);
    expect(sau).toMatchObject({ tenNcc: `NCC sửa ${RUN}`, trangThai: 'NGUNG_HOP_TAC' });
    expect((await ad.delete(`/api/nha-cung-cap/${n.maNcc}`)).status()).toBe(204);
    await loi(await ad.delete(`/api/nha-cung-cap/${n.maNcc}`), 404);
    await loi(await ad.delete('/api/nha-cung-cap/NCC01'), 400, /phiếu nhập/);
    await loi(await ad.post('/api/nha-cung-cap', { data: { tenNcc: '' } }), 400);
    await loi(await ad.post('/api/nha-cung-cap', { data: { tenNcc: 'x', trangThai: 'BAY' } }), 400);
    await loi(await ad.put('/api/nha-cung-cap/NCC99', { data: { tenNcc: 'x' } }), 404);
  });

  test('phan quyen nha cung cap', async () => {
    expect((await (await khach()).get('/api/nha-cung-cap')).status()).toBe(403);
    expect((await (await nhanVien()).post('/api/nha-cung-cap', { data: { tenNcc: 'x' } })).status()).toBe(403);
    expect((await (await nhanVien()).delete('/api/nha-cung-cap/NCC01')).status()).toBe(403);
  });
});
