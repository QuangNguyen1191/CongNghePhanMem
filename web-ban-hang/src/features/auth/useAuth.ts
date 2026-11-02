import { useMutation, useQueryClient, type QueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { taiKhoanApi, gioHangApi, type DangKyYeuCau } from '../../api/khachApi';
import { qk } from '../../api/queryKeys';
import type { PhienDangNhap } from '../../api/types';
import { session } from '../../lib/session';
import { gioTam } from '../cart/gioTam';

/** Sau khi co phien: luu phien, gop gio tam vao gio tren server. Loi gop khong chan dang nhap. */
async function batDauPhien(p: PhienDangNhap, qc: QueryClient) {
  session.ghi(p);
  qc.removeQueries({ queryKey: ['toi'] });
  const tam = gioTam.doc();
  if (tam.length === 0) return;
  try {
    const gio = await gioHangApi.gop(tam);
    qc.setQueryData(qk.gioHang, gio);
    const boQua = tam.filter((d) => !gio.chiTiets.some((c) => c.maSp === d.maSp)).length;
    if (boQua > 0) toast.warning(`${boQua} sản phẩm trong giỏ tạm đã hết hàng hoặc ngừng bán nên không được thêm.`);
    gioTam.xoaHet();
  } catch {
    toast.error('Chưa gộp được giỏ hàng tạm. Sản phẩm vẫn được giữ, bạn có thể thử lại sau.');
  }
}

export function useDangNhap() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ tenDangNhap, matKhau }: { tenDangNhap: string; matKhau: string }) =>
      taiKhoanApi.dangNhap(tenDangNhap.trim(), matKhau),
    onSuccess: (p) => batDauPhien(p, qc),
    meta: { tuXuLyLoi: true }, // form tu hien loi ngay duoi o nhap
  });
}

export function useDangKy() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (yc: DangKyYeuCau) => taiKhoanApi.dangKy(yc),
    onSuccess: (p) => batDauPhien(p, qc),
    meta: { tuXuLyLoi: true },
  });
}

/** JWT khong co API thu hoi: dang xuat = xoa token + du lieu rieng cua khach trong cache. */
export function dangXuat(qc: QueryClient) {
  session.xoa();
  qc.removeQueries({ queryKey: ['toi'] });
}
