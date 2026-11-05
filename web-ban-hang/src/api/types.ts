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
  /** v3: loai giam da ap dung: HOA_DON | VOUCHER | THANH_VIEN_MOI (null: khong giam) */
  loaiGiam?: 'HOA_DON' | 'VOUCHER' | 'THANH_VIEN_MOI' | null;
  /** ma voucher THUC SU duoc ap (null neu uu dai thanh vien moi tot hon voucher da nhap) */
  maVoucher?: string | null;
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
  /** v3: TAI_KHOAN | VANG_LAI */
  nguon?: 'TAI_KHOAN' | 'VANG_LAI';
  emailNhan?: string | null;
  maKm?: string | null;
  /** v3: khach xin huy don da xac nhan: CHO_DUYET -> DA_DUYET (da huy) | TU_CHOI (kem phanHoiHuy) */
  yeuCauHuy?: 'CHO_DUYET' | 'DA_DUYET' | 'TU_CHOI' | null;
  lyDoHuy?: string | null;
  ngayYeuCauHuy?: string | null;
  phanHoiHuy?: string | null;
  ngayHuy?: string | null;
  nguoiHuy?: 'KHACH' | 'NHAN_VIEN' | null;
  /** don da tra truoc bi huy: CHO_HOAN -> DA_HOAN */
  trangThaiHoanTien?: 'CHO_HOAN' | 'DA_HOAN' | null;
  soTienHoan?: number | null;
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
  /** v3: so don mua khong can tai khoan vua duoc gan vao tai khoan moi (khi dang ky) */
  soDonDaGan?: number | null;
}

// ---- v3: khach vang lai (mua khong can tai khoan) ----
export type MucDichOtp = 'MUA_HANG' | 'TRA_CUU' | 'DANG_KY';

export interface KetQuaGuiOtp {
  sdt: string;
  hetHanSauGiay: number;
  guiLaiSauGiay: number;
  /** chi co o che do demo (OTP gia lap) */
  maDemo?: string | null;
}

export interface PhienVangLai {
  sdt: string;
  token: string;
  hetHan: string;
  coTaiKhoan: boolean;
  hoTen?: string | null;
  email?: string | null;
  diaChi?: string | null;
  soDonCu: number;
}

export interface DatHangVangLaiYeuCau {
  hoTen: string;
  email?: string;
  diaChiGiao: string;
  phuongThucTt: PhuongThucTt;
  maVoucher?: string;
  dongHangs: DongHang[];
}

export interface UuDaiThanhVienMoi {
  maKm: string;
  tenKm: string;
  soDonDau: number;
  phanTramGiamGia: number;
  giamToiDa?: number | null;
  ngayKt: string;
  /** chi co khi da dang nhap */
  daDung?: number | null;
  conLai?: number | null;
}
