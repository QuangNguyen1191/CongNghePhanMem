// Khoa cache React Query dung chung, tranh goi API trung.
export const qk = {
  loaiSp: ['loai-sp'] as const,
  sanPhamDangBan: ['san-pham', 'dang-ban'] as const,
  sanPham: (ma: string) => ['san-pham', ma] as const,
  bienThe: (ma: string) => ['san-pham', ma, 'bien-the'] as const,
  timKiem: (q: string, loai?: string) => ['san-pham', 'tim', q, loai ?? ''] as const,
  giaKhuyenMai: ['san-pham', 'gia-km'] as const,
  kmDangChay: ['khuyen-mai', 'dang-chay'] as const,
  boAnh: ['bo-anh-uniqlo'] as const,
  // uu dai thanh vien moi: kem so luot con lai cua khach dang nhap -> de duoi 'toi' khi da dang nhap
  uuDaiTvm: (dangNhap: boolean) => (dangNhap ? ['toi', 'uu-dai-tvm'] : ['uu-dai-tvm']) as readonly string[],
  // don cua khach vang lai: theo SDT da xac thuc
  vangLaiDons: (sdt: string) => ['vang-lai', sdt, 'don-hang'] as const,
  vangLaiDon: (sdt: string, ma: string) => ['vang-lai', sdt, 'don-hang', ma] as const,
  // du lieu rieng cua khach: bat dau bang 'toi' de xoa het 1 lan khi dang xuat
  hoSo: ['toi', 'ho-so'] as const,
  gioHang: ['toi', 'gio-hang'] as const,
  donHangs: ['toi', 'don-hang'] as const,
  donHang: (ma: string) => ['toi', 'don-hang', ma] as const,
  hoaDons: ['toi', 'hoa-don'] as const,
  hoaDon: (ma: string) => ['toi', 'hoa-don', ma] as const,
  doiTras: ['toi', 'doi-tra'] as const,
  doiTra: (ma: string) => ['toi', 'doi-tra', ma] as const,
};
