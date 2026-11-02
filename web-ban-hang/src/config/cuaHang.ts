// Thong tin cua hang hien o footer / trang lien he. Nhom dien that vao day (de trong thi khong hien).
export const cuaHang = {
  ten: 'UNIQLO',
  diaChi: '',
  hotline: '',
  email: '',
  gioMoCua: '',
};

// Quy tac nghiep vu lay tu backend (BLL/ChungBLL.cs > QuyTac), chi de hien thi cho khach.
export const quyTac = {
  soNgayDoiTra: 30,
  tienMoiDiem: 10_000,
  nguongTonThap: 5,
  // XetHang: tong da mua (hoa don da thanh toan) >= nguong -> hang
  nguongHang: [['BAC', 3_000_000], ['VANG', 8_000_000], ['KIM_CUONG', 15_000_000]] as [string, number][],
};
