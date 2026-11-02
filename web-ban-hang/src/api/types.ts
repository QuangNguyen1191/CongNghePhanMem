// Kieu du lieu khop DTO backend (backend/DTO/*.cs), JSON camelCase.

export interface LoaiSp {
  maLoaiSp: string;
  tenLoaiSp: string;
}

/** 1 dong SAN_PHAM = 1 SKU (1 size + 1 mau) */
export interface SanPham {
  maSp: string;
  tenSp: string;
  maLoaiSp: string;
  tenLoaiSp?: string | null;
  chatLieu?: string | null;
  moTa?: string | null;
  donGia: number;
  soLuong: number; // ton kho hien tai
  trangThai: 'DANG_BAN' | 'NGUNG_BAN';
  hinhAnh?: string | null;
  kichCo?: string | null;
  mauSac?: string | null;
}

/** ma SP -> gia sau khuyen mai hom nay */
export type GiaKhuyenMai = Record<string, number>;

export interface KhuyenMai {
  maKm: string;
  tenKm: string;
  hinhThuc: 'SAN_PHAM' | 'HOA_DON' | 'VOUCHER';
  phanTramGiamGia?: number | null;
  ngayBd: string;
  ngayKt: string;
}

export interface DongHang {
  maSp: string;
  soLuong: number;
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

export interface BangTinhTien {
  dongs: ChiTietHoaDon[];
  tamTinh: number;
  tienGiam: number;
  tongTien: number;
  maKm?: string | null;
  ghiChuKm?: string | null;
}

export interface ChiTietGioHang {
  maGioHang: string;
  maSp: string;
  soLuong: number;
  tenSp?: string | null;
  kichCo?: string | null;
  mauSac?: string | null;
  hinhAnh?: string | null;
  donGia: number;
  tonKho: number;
  thanhTien: number;
}

export interface GioHang {
  maGioHang: string;
  maKh: string;
  ngayCapNhat: string;
  chiTiets: ChiTietGioHang[];
  tamTinh: number;
}

export type TrangThaiDon = 'CHO_XAC_NHAN' | 'DA_XAC_NHAN' | 'DANG_GIAO' | 'HOAN_TAT' | 'DA_HUY' | 'TU_CHOI';
export type PhuongThucTt = 'COD' | 'CHUYEN_KHOAN' | 'ONLINE';

export interface ChiTietDonDatHang {
  maDon: string;
  maSp: string;
  soLuong: number;
  donGia: number;
  thanhTien: number;
  tenSp?: string | null;
  kichCo?: string | null;
  mauSac?: string | null;
}

export interface DonDatHang {
  maDon: string;
  maKh: string;
  maNv?: string | null;
  ngayDat: string;
  trangThai: TrangThaiDon;
  diaChiGiao: string;
  sdtNhan: string;
  phuongThucTt: PhuongThucTt;
  maVoucher?: string | null;
  tongTien: number;
  tenNguoiNhan?: string | null;
  tienGiam: number;
  tenKhachHang?: string | null;
  tenNhanVien?: string | null;
  maHoaDon?: string | null;
  chiTiets: ChiTietDonDatHang[];
}

export interface DatHangYeuCau {
  diaChiGiao: string;
  sdtNhan: string;
  tenNguoiNhan?: string;
  phuongThucTt: PhuongThucTt;
  maVoucher?: string;
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
  chiTiets: ChiTietDoiTra[];
}

export interface KhachHang {
  maKh: string;
  maTk?: string | null;
  hoTen: string;
  sdt?: string | null;
  email?: string | null;
  ngaySinh?: string | null;
  hangThanhVien: 'DONG' | 'BAC' | 'VANG' | 'KIM_CUONG';
  diemTichLuy: number;
  diaChi?: string | null;
  tinhThanh?: string | null;
}

export interface PhienDangNhap {
  maTk: string;
  tenDangNhap: string;
  vaiTro: string;
  maKh?: string | null;
  hoTen: string;
  token: string;
}
