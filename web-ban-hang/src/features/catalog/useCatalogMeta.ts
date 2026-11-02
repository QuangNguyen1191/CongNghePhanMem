import { useQuery } from '@tanstack/react-query';
import { khuyenMaiApi, loaiSpApi } from '../../api/catalogApi';
import { qk } from '../../api/queryKeys';

// Danh muc va KM it thay doi: cache lau, header + trang chu + bo loc dung chung 1 lan goi.
export const useLoaiSp = () =>
  useQuery({ queryKey: qk.loaiSp, queryFn: loaiSpApi.layTatCa, staleTime: 30 * 60_000 });

export const useKhuyenMaiDangChay = () =>
  useQuery({ queryKey: qk.kmDangChay, queryFn: khuyenMaiApi.dangChay, staleTime: 10 * 60_000 });
