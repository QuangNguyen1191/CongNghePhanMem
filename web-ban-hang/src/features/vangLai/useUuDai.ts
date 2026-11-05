import { useQuery } from '@tanstack/react-query';
import { uuDaiApi } from '../../api/vangLaiApi';
import { qk } from '../../api/queryKeys';
import { formatTien } from '../../lib/format';
import type { UuDaiThanhVienMoi } from '../../api/types';
import { useSession } from '../auth/useSession';

/** Chuong trinh uu dai thanh vien moi dang chay (null: khong co). Da dang nhap: kem so luot con lai. */
export function useUuDaiTvm() {
  const phien = useSession();
  return useQuery({ queryKey: qk.uuDaiTvm(!!phien), queryFn: uuDaiApi.thanhVienMoi, staleTime: 60_000 });
}

/** "giam 10% cho 3 don dau (toi da 100.000d moi don)" */
export const moTaUuDai = (u: UuDaiThanhVienMoi) =>
  `giảm ${u.phanTramGiamGia.toLocaleString('vi-VN')}% cho ${u.soDonDau} đơn đầu tiên${u.giamToiDa ? ` (tối đa ${formatTien(u.giamToiDa)} mỗi đơn)` : ''}`;
