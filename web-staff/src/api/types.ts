// Kiểu dữ liệu khớp DTO của backend (JSON camelCase)

export interface PhienDangNhap {
  maTk: string;
  tenDangNhap: string;
  vaiTro: 'ADMIN' | 'NHAN_VIEN' | 'KHACH_HANG';
  maNv?: string | null;
  hoTen: string;
  chucVu?: string | null;
  token?: string | null;
}

export interface LoaiSp {
  maLoaiSp: string;
  tenLoaiSp: string;
}

export interface SanPham {
  maSp: string;
  tenSp: string;
  maLoaiSp: string;
  chatLieu?: string | null;
  moTa?: string | null;
  donGia: number;
  soLuong: number;
  trangThai: 'DANG_BAN' | 'NGUNG_BAN';
  hinhAnh?: string | null;
  kichCo?: string | null;
  mauSac?: string | null;
  tenLoaiSp?: string | null;
}

export interface NhaCungCap {
  maNcc: string;
  tenNcc: string;
  diaChi?: string | null;
  sdt?: string | null;
  email?: string | null;
  trangThai: string;
}

export interface KhachHang {
  maKh: string;
  maTk?: string | null;
  hoTen: string;
  sdt?: string | null;
  email?: string | null;
  ngaySinh?: string | null;
  hangThanhVien: string;
  diemTichLuy: number;
  diaChi?: string | null;
  tinhThanh?: string | null;
  sdtDaXacThuc?: boolean;
}

export interface RuiRoKhach {
  sdt: string;
  maKh?: string | null;
  hoTen?: string | null;
  coTaiKhoan: boolean;
  sdtDaXacThuc: boolean;
  soDonHoanTat: number;
  soDonDangCho: number;
  soDonBiTuChoi: number;
  soLanBomHang: number;
  duocDatCod: boolean;
  mucRuiRo: 'THAP' | 'TRUNG_BINH' | 'CAO';
  ghiChu: string;
}

export interface ChiTietDon {
  maSp: string;
  soLuong: number;
  donGia: number;
  thanhTien: number;
  tenSp?: string | null;
  kichCo?: string | null;
  mauSac?: string | null;
}

export type TrangThaiDon = 'CHO_XAC_NHAN' | 'DA_XAC_NHAN' | 'DANG_GIAO' | 'HOAN_TAT' | 'DA_HUY' | 'TU_CHOI';

export interface DonDatHang {
  maDon: string;
  maKh: string;
  maNv?: string | null;
  ngayDat: string;
  trangThai: TrangThaiDon;
  diaChiGiao: string;
  sdtNhan: string;
  phuongThucTt: string;
  maVoucher?: string | null;
  tongTien: number;
  tenNguoiNhan?: string | null;
  tienGiam: number;
  tenKhachHang?: string | null;
  tenNhanVien?: string | null;
  maHoaDon?: string | null;
  nguon: 'TAI_KHOAN' | 'VANG_LAI';
  emailNhan?: string | null;
  yeuCauHuy?: 'CHO_DUYET' | 'DA_DUYET' | 'TU_CHOI' | null;
  lyDoHuy?: string | null;
  ngayYeuCauHuy?: string | null;
  phanHoiHuy?: string | null;
  ngayHuy?: string | null;
  nguoiHuy?: 'KHACH' | 'NHAN_VIEN' | null;
  trangThaiHoanTien?: 'CHO_HOAN' | 'DA_HOAN' | null;
  soTienHoan?: number | null;
  chiTiets: ChiTietDon[];
}

export interface HoanTien {
  maHoanTien: string;
  maDon: string;
  maHoaDon: string;
  soTien: number;
  phuongThuc: string;
  trangThai: 'CHO_HOAN' | 'DA_HOAN';
  ngayTao: string;
  ngayHoan?: string | null;
  maNv?: string | null;
  ghiChu?: string | null;
  tenKhachHang?: string | null;
  sdtNhan?: string | null;
  tenNhanVien?: string | null;
}

export interface ChiTietHoaDon {
  maHoaDon: string;
  maSp: string;
  soLuong: number;
  donGiaBan: number;
  thanhTien: number;
  tenSp?: string | null;
  kichCo?: string | null;
  mauSac?: string | null;
}

export interface HoaDon {
  maHoaDon: string;
  maDon?: string | null;
  maKh?: string | null;
  maNv: string;
  ngayLap: string;
  trangThai: 'CHO_THANH_TOAN' | 'DA_THANH_TOAN' | 'DA_HUY';
  maKm?: string | null;
  tienGiam: number;
  phuongThucTt: string;
  tongTien: number;
  tenKhachHang?: string | null;
  tenNhanVien?: string | null;
  tenKm?: string | null;
  chiTiets: ChiTietHoaDon[];
}

export interface DongHang {
  maSp: string;
  soLuong: number;
}

export interface BangTinhTien {
  dongs: ChiTietHoaDon[];
  tamTinh: number;
  tienGiam: number;
  tongTien: number;
  maKm?: string | null;
  ghiChuKm?: string | null;
  loaiGiam?: string | null;
  maVoucher?: string | null;
}

export interface ChiTietDoiTra {
  maDoiTra?: string;
  maSp: string;
  soLuong: number;
  maSpDoi?: string | null;
  tenSp?: string | null;
  tenSpDoi?: string | null;
}

export interface DoiTra {
  maDoiTra: string;
  maHoaDon: string;
  maNv?: string | null;
  loai: 'DOI' | 'TRA';
  lyDo?: string | null;
  ngayDoiTra: string;
  trangThai: 'CHO_DUYET' | 'DA_DUYET' | 'TU_CHOI' | 'HOAN_TAT';
  tienHoan: number;
  ngayHoan?: string | null;
  maKh?: string | null;
  tenKhachHang?: string | null;
  tenNhanVien?: string | null;
  chiTiets: ChiTietDoiTra[];
}

export interface ChiTietPhieuNhap {
  maPhieuNhap?: string;
  maSp: string;
  soLuong: number;
  donGiaNhap: number;
  thanhTien?: number;
  tenSp?: string | null;
  kichCo?: string | null;
  mauSac?: string | null;
}

export interface PhieuNhap {
  maPhieuNhap: string;
  maNcc: string;
  maNv: string;
  ngayNhap: string;
  tongTien: number;
  tenNcc?: string | null;
  tenNhanVien?: string | null;
  chiTiets: ChiTietPhieuNhap[];
}

export interface KetQuaDocExcel<T> {
  hopLe: T[];
  lois: { dong: number; thongBao: string }[];
  tongSoDong: number;
}

export interface NhatKySql {
  thoiDiem: string;
  cauSql: string;
  thamSo: string;
  file: string;
  dong: number;
  hamGoi: string;
  thoiGianMs: number;
  soDong: number;
}
