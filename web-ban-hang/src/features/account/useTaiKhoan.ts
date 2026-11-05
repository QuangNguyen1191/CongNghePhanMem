import { useQuery } from '@tanstack/react-query';
import { donHangApi, doiTraApi, hoaDonApi, khachHangApi } from '../../api/khachApi';
import { qk } from '../../api/queryKeys';

export const useHoSo = (enabled = true) => useQuery({ queryKey: qk.hoSo, queryFn: khachHangApi.toi, enabled });
export const useDonHangs = () => useQuery({ queryKey: qk.donHangs, queryFn: donHangApi.cuaToi });
export const useDonHang = (ma: string, enabled = true) => useQuery({ queryKey: qk.donHang(ma), queryFn: () => donHangApi.layTheoMa(ma), enabled });
export const useHoaDons = () => useQuery({ queryKey: qk.hoaDons, queryFn: hoaDonApi.cuaToi });
export const useHoaDon = (ma: string) => useQuery({ queryKey: qk.hoaDon(ma), queryFn: () => hoaDonApi.layTheoMa(ma), enabled: !!ma });
export const useDoiTras = () => useQuery({ queryKey: qk.doiTras, queryFn: doiTraApi.cuaToi });
export const useDoiTra = (ma: string) => useQuery({ queryKey: qk.doiTra(ma), queryFn: () => doiTraApi.layTheoMa(ma) });
