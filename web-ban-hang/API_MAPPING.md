# API_MAPPING — Web bán hàng (khách hàng)

Đối chiếu từng chức năng frontend với API **đang có thật** trong `backend/WebApi/Controllers` (bản đã sửa, 105 test Playwright pass).
Không có endpoint nào trong bảng là tự đặt ra. Chỗ nào backend chưa có thì ghi rõ ở cột Status.

**Quy ước chung của backend (đã kiểm tra trong code):**
- Base URL: `http://localhost:5212` (launchSettings). CORS cho phép `http://localhost:5173` và `5174` → web khách chạy cổng **5173**.
- JSON dùng **camelCase** (`maSp`, `tenSp`, `donGia`...). Ngày dạng ISO.
- Lỗi nghiệp vụ: `400 { "thongBao": "..." }`; không tìm thấy: `404 { "thongBao" }`; sai kiểu dữ liệu: `400 { thongBao, loi: { truong: [..] } }`; lỗi hệ thống: `500`.
- Xác thực: `Authorization: Bearer <token>` (JWT, sống 12 giờ, **không có refresh token**). Tài khoản bị khóa thì token cũ bị từ chối ngay → `401`.
- Vai trò khách: `KHACH_HANG`. Gọi API của nhân viên/admin → `403`.
- Mọi response có header `X-So-Cau-Sql`, `X-Sql-Vi-Tri` (yêu cầu 1 của đồ án, web khách không cần hiển thị).

Status: ✅ có đủ API · ⚠️ có API nhưng phải xử lý thêm ở frontend / có hạn chế · ❌ chưa có API hoặc CSDL không có dữ liệu

---

## 1. Danh mục & sản phẩm (không cần đăng nhập)

| Frontend Feature | API Endpoint | HTTP Method | Request | Response | Status |
|---|---|---|---|---|---|
| Menu danh mục (header, trang chủ, bộ lọc) | `/api/loai-sp` | GET | — | `LoaiSpDTO[] { maLoaiSp, tenLoaiSp }` (6 loại: Áo thun, Áo sơ mi, Áo khoác, Quần jean, Quần kaki, Váy đầm) | ✅ |
| Danh sách sản phẩm (tải 1 lần, lọc tại trình duyệt) | `/api/san-pham?chiDangBan=true` | GET | query `chiDangBan=true` | `SanPhamDTO[] { maSp, tenSp, maLoaiSp, tenLoaiSp, chatLieu, moTa, donGia, soLuong, trangThai, hinhAnh, kichCo, mauSac }` — **mỗi dòng là 1 SKU (1 size + 1 màu)** | ⚠️ frontend gom các SKU cùng `tenSp` thành 1 sản phẩm hiển thị |
| Lọc theo danh mục | `/api/san-pham?maLoaiSp=LSP01&chiDangBan=true` | GET | query `maLoaiSp` | như trên | ✅ (hoặc lọc ngay trên danh sách đã tải) |
| Tìm kiếm theo từ khóa | `/api/san-pham?tuKhoa=...&chiDangBan=true` | GET | query `tuKhoa` (+ `maLoaiSp`) | như trên; LIKE trên tên, mã SP, màu | ✅ |
| Giá khuyến mãi hôm nay | `/api/san-pham/gia-khuyen-mai` | GET | — | `{ [maSp]: giaSauGiam }` | ✅ (dữ liệu mẫu hiện **không có** KM sản phẩm nào còn hạn, xem mục 9) |
| Lọc theo giá / màu / size / còn hàng | (không có endpoint lọc) | — | — | lọc trên danh sách đã tải | ⚠️ làm ở frontend, hợp lệ vì danh sách đầy đủ chỉ ~32 SKU |
| Sắp xếp giá tăng/giảm | (không có) | — | — | sắp xếp trên danh sách đã tải | ⚠️ frontend |
| Sắp xếp "mới nhất" | (không có) | — | — | — | ❌ `SAN_PHAM` không có cột ngày tạo. Chỉ có thể dùng mã SP giảm dần làm xấp xỉ (cần nhóm đồng ý) |
| Phân trang / tải thêm | (không có phân trang ở API) | — | — | — | ⚠️ phân trang phía client (12 sản phẩm/lần, nút "Xem thêm") |
| Lọc theo giới tính | — | — | — | — | ❌ CSDL không có cột giới tính |
| Chi tiết sản phẩm | `/api/san-pham/{maSp}` | GET | path `maSp` | `SanPhamDTO` (kể cả SP ngừng bán) | ✅ |
| Chọn màu/size đúng biến thể + tồn kho | `/api/san-pham/{maSp}/bien-the` | GET | path `maSp` | `SanPhamDTO[]` cùng `tenSp` (mỗi phần tử có `kichCo`, `mauSac`, `soLuong`, `trangThai`) | ✅ frontend bỏ các biến thể `NGUNG_BAN`, khóa size có `soLuong = 0` |
| Hiển thị hết hàng | (trường `soLuong`) | — | — | `soLuong = 0` | ✅ |
| Sản phẩm liên quan | `/api/san-pham?maLoaiSp=...&chiDangBan=true` | GET | cùng loại | như trên | ✅ |
| Thư viện ảnh nhiều ảnh | — | — | — | mỗi SKU chỉ có 1 `hinhAnh` | ⚠️ gallery = ảnh của các màu; không có nhiều ảnh cho 1 màu |
| Bảng hướng dẫn chọn size | — | — | — | — | ❌ CSDL không có dữ liệu size chart |
| Chương trình khuyến mãi đang chạy (banner) | `/api/khuyen-mai/dang-chay` | GET | — | `KhuyenMaiDTO[] { maKm, tenKm, hinhThuc, phanTramGiamGia, ngayBd, ngayKt }` (không kèm danh sách SP/voucher) | ✅ |

## 2. Tài khoản

| Frontend Feature | API Endpoint | HTTP Method | Request | Response | Status |
|---|---|---|---|---|---|
| Đăng nhập | `/api/tai-khoan/dang-nhap/web-ban-hang` | POST | `{ tenDangNhap, matKhau }` | `PhienDangNhapDTO { maTk, tenDangNhap, vaiTro, maKh, hoTen, token }`; tài khoản nhân viên → 400 | ✅ |
| Đăng ký | `/api/tai-khoan/dang-ky` | POST | `{ tenDangNhap (≥4), matKhau (≥6), khachHang: { hoTen*, sdt?, email?, ngaySinh?, diaChi?, tinhThanh? } }` | `PhienDangNhapDTO` kèm token (đăng nhập luôn). SĐT trùng khách mua tại quầy chưa có TK thì gắn vào hồ sơ cũ | ✅ |
| Đăng xuất | — | — | — | xóa token ở client (JWT không có API thu hồi) | ⚠️ frontend |
| Xem thông tin cá nhân, hạng thành viên, điểm | `/api/khach-hang/toi` | GET | Bearer | `KhachHangDTO { maKh, hoTen, sdt, email, ngaySinh, hangThanhVien, diemTichLuy, diaChi, tinhThanh }` | ✅ |
| Cập nhật thông tin | `/api/khach-hang/toi` | PUT | `KhachHangDTO` **đầy đủ** | 204 | ⚠️ backend ghi đè cả `ngaySinh, diaChi, tinhThanh, sdt, email` → frontend phải gửi lại đủ các trường (lấy từ GET rồi sửa) |
| Đổi mật khẩu | `/api/tai-khoan/doi-mat-khau` | POST | `{ matKhauCu, matKhauMoi (≥6) }` | `{ thongBao }` | ✅ |
| Quên / đặt lại mật khẩu | — | — | — | chỉ có `POST /api/tai-khoan/{ma}/dat-lai-mat-khau` cho ADMIN | ❌ khách không tự đặt lại được; hiển thị hướng dẫn "liên hệ cửa hàng" |
| Sổ địa chỉ nhiều địa chỉ | — | — | — | chỉ 1 `diaChi` + `tinhThanh` trong hồ sơ | ⚠️ dùng địa chỉ hồ sơ làm mặc định, nhập địa chỉ khác ngay ở checkout |

## 3. Giỏ hàng

| Frontend Feature | API Endpoint | HTTP Method | Request | Response | Status |
|---|---|---|---|---|---|
| Xem giỏ (đã đăng nhập) | `/api/gio-hang` | GET | Bearer | `GioHangDTO { maGioHang, chiTiets: [{ maSp, soLuong, tenSp, kichCo, mauSac, hinhAnh, donGia (đã áp KM SP), tonKho, thanhTien }], tamTinh }` | ✅ |
| Thêm vào giỏ | `/api/gio-hang` | POST | `{ maSp, soLuong }` (cộng dồn) | `GioHangDTO`; vượt tồn → 400 "… chỉ còn N cái" | ✅ |
| Cập nhật số lượng | `/api/gio-hang` | PUT | `{ maSp, soLuong }` (≤0 là xóa dòng) | `GioHangDTO` | ✅ |
| Xóa 1 dòng | `/api/gio-hang/{maSp}` | DELETE | path | `GioHangDTO` | ✅ |
| Xóa hết | `/api/gio-hang` | DELETE | — | 204 | ✅ |
| Đổi size/màu trong giỏ | `DELETE /api/gio-hang/{maSpCu}` + `POST /api/gio-hang` | DELETE+POST | — | `GioHangDTO` | ⚠️ không có API đổi trực tiếp; ghép 2 lời gọi, kiểm tra tồn trước |
| Giỏ khi chưa đăng nhập | — | — | lưu `[{maSp, soLuong}]` trong localStorage | — | ⚠️ frontend; giá/tồn lấy lại từ `/api/san-pham/{ma}` |
| Gộp giỏ tạm sau đăng nhập | `/api/gio-hang/gop` | POST | `[{ maSp, soLuong }]` | `GioHangDTO` (bỏ qua dòng hết hàng/ngừng bán) | ✅ |
| Kiểm tra tồn kho | (trường `tonKho` trong giỏ + lỗi 400 khi thêm) | — | — | — | ✅ |

## 4. Thanh toán & đặt hàng

| Frontend Feature | API Endpoint | HTTP Method | Request | Response | Status |
|---|---|---|---|---|---|
| Xem trước tổng tiền + áp voucher / KM hóa đơn | `/api/khuyen-mai/tinh-tien` | POST | `{ dongHangs: [{maSp, soLuong}], maVoucher? }` (không cần đăng nhập) | `BangTinhTienDTO { dongs, tamTinh, tienGiam, tongTien, maKm, ghiChuKm }`; voucher sai → 400 | ✅ |
| Đặt hàng | `/api/don-hang` | POST | `{ diaChiGiao*, sdtNhan*, tenNguoiNhan?, phuongThucTt: COD \| CHUYEN_KHOAN \| ONLINE, maVoucher? }` — **lấy hàng từ giỏ trên server**, server tự tính giá | `DonDatHangDTO` trạng thái `CHO_XAC_NHAN`; giỏ bị xóa | ✅ |
| Phí vận chuyển | — | — | — | — | ❌ CSDL không có phí ship; tổng = `tongTien` |
| Thanh toán online (cổng thanh toán) | — | — | — | `ONLINE`/`CHUYEN_KHOAN` chỉ là phương thức ghi nhận; nhân viên xác nhận thì coi là đã trả | ❌ không tích hợp cổng ngoài (đúng yêu cầu) |
| Chặn đặt trùng | — | — | — | server xóa giỏ sau khi đặt nên lần gửi thứ 2 nhận 400 "Giỏ hàng đang trống" | ⚠️ frontend khóa nút + cờ đang gửi |
| Trang kết quả đặt hàng | `/api/don-hang/{ma}` | GET | Bearer | `DonDatHangDTO` kèm `chiTiets` | ✅ |

## 5. Đơn hàng, hóa đơn, đổi trả

| Frontend Feature | API Endpoint | HTTP Method | Request | Response | Status |
|---|---|---|---|---|---|
| Danh sách đơn của tôi | `/api/don-hang/cua-toi` | GET | Bearer | `DonDatHangDTO[]` (không kèm `chiTiets`, mới nhất trước) | ✅ |
| Chi tiết đơn | `/api/don-hang/{ma}` | GET | Bearer | `DonDatHangDTO` + `chiTiets [{maSp, soLuong, donGia, thanhTien, tenSp, kichCo, mauSac}]`; đơn người khác → 404 | ✅ |
| Theo dõi trạng thái | (trường `trangThai`) | — | — | `CHO_XAC_NHAN → DA_XAC_NHAN → DANG_GIAO → HOAN_TAT`, hoặc `DA_HUY`, `TU_CHOI` | ✅ (không có lịch sử thời điểm từng bước) |
| Hủy đơn | `/api/don-hang/{ma}/khach-huy` | POST | Bearer | 204; chỉ khi `CHO_XAC_NHAN`, ngược lại 400 | ✅ |
| Tải PDF đơn | `/api/don-hang/{ma}/pdf` | GET | Bearer | file PDF | ✅ |
| Hóa đơn của tôi (lịch sử mua, cả mua tại quầy) | `/api/hoa-don/cua-toi`, `/api/hoa-don/{ma}`, `/api/hoa-don/{ma}/pdf` | GET | Bearer | `HoaDonDTO` | ✅ |
| Gửi yêu cầu đổi / trả | `/api/doi-tra` | POST | `{ maHoaDon, loai: DOI \| TRA, lyDo?, chiTiets: [{maSp, soLuong, maSpDoi?}] }` (≤30 ngày, hóa đơn đã thanh toán, đổi sang size/màu cùng mẫu) | `DoiTraDTO` | ✅ |
| Danh sách / chi tiết đổi trả | `/api/doi-tra/cua-toi`, `/api/doi-tra/{ma}`, `/api/doi-tra/{ma}/pdf` | GET | Bearer | `DoiTraDTO` | ✅ |

## 6. Trang chủ

| Frontend Feature | API Endpoint | HTTP Method | Request | Response | Status |
|---|---|---|---|---|---|
| Danh mục nổi bật | `/api/loai-sp` | GET | — | 6 loại | ✅ |
| Sản phẩm mới | — | — | — | — | ❌ không có ngày tạo (chỉ xấp xỉ bằng mã SP) |
| Sản phẩm bán chạy | `/api/thong-ke/san-pham` chỉ cho ADMIN | — | — | — | ❌ không có API công khai |
| Sản phẩm đang khuyến mãi | `/api/san-pham/gia-khuyen-mai` | GET | — | map giá | ✅ (ẩn khối khi rỗng) |
| Banner chiến dịch | `/api/khuyen-mai/dang-chay` | GET | — | KM đang chạy (dữ liệu mẫu: KM02 hóa đơn ≥1tr giảm 5%, KM03 voucher) | ✅ nội dung hình ảnh banner là tĩnh |
| Bộ sưu tập | — | — | — | — | ❌ CSDL không có khái niệm bộ sưu tập |

## 7. Chức năng bổ sung

| Frontend Feature | API Endpoint | Status |
|---|---|---|
| Wishlist | — | ❌ không có bảng/API |
| Đánh giá, nhận xét | — | ❌ không có bảng/API |
| Mã giảm giá | `/api/khuyen-mai/tinh-tien` + `maVoucher` khi đặt | ✅ |
| Thông báo cho khách | — | ❌ |
| Lịch sử tìm kiếm | — | ⚠️ chỉ làm được ở trình duyệt (localStorage), không lưu server |
| Sản phẩm đã xem gần đây | — | ⚠️ chỉ ở trình duyệt; dữ liệu SP vẫn lấy từ API |

## 8. Không dùng ở web khách

Thống kê, tìm kiếm nâng cao, Excel, nhật ký SQL, nhân viên, nhà cung cấp, phiếu nhập, bán tại quầy, xử lý đơn: thuộc web staff / app admin.

## 9. Lưu ý dữ liệu & nghiệp vụ cần nhóm biết

1. **Ảnh sản phẩm chưa có.** `hinh_anh` lưu `/images/sp001.jpg`… nhưng không có file ảnh nào, và backend không phục vụ file tĩnh. Đề xuất: đặt ảnh vào `web-ban-hang/public/images/` (Vite phục vụ đúng đường dẫn này). Ảnh phải do nhóm chụp hoặc lấy nguồn miễn phí bản quyền, **không lấy ảnh của UNIQLO**. Khi thiếu ảnh, web hiện khung trung tính kèm tên SP.
2. **Khuyến mãi sản phẩm đã hết hạn.** KM01 (hè 2025) và KM04 (Black Friday 2025) đều đã qua so với hôm nay (02/11/2026), nên `gia-khuyen-mai` trả rỗng và khối "Đang giảm giá" sẽ trống. Muốn demo giá gạch ngang thì nhân viên/admin tạo KM sản phẩm mới qua API có sẵn (`POST /api/khuyen-mai`), không cần sửa CSDL.
3. **Tồn kho không bị giữ chỗ khi đặt.** Đơn `CHO_XAC_NHAN` chưa trừ tồn; nhân viên xác nhận mới trừ, nếu thiếu thì báo lỗi. Web chỉ hiển thị tồn tại thời điểm xem.
4. **Voucher và KM hóa đơn không cộng dồn.** Có voucher hợp lệ thì dùng voucher, không thì lấy mức KM hóa đơn cao nhất. Web hiển thị `ghiChuKm` để khách hiểu.
5. **Gom sản phẩm theo `tenSp`.** Backend cũng dùng chính quy tắc này (biến thể, đổi hàng), nên frontend gom theo tên là nhất quán.
