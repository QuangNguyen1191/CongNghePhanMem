# Phân tích và đánh giá API, luồng nghiệp vụ (QL_BanThoiTrang)

Tài liệu này đọc toàn bộ `backend/` (DTO, DAL, BLL, WebApi), `database/`, tài liệu và test hiện có (ngày 07/11/2026), rồi đánh giá:
API có đủ và nhất quán không, các luồng nghiệp vụ chạy đúng chưa, còn lỗi / rủi ro gì, và app desktop admin (`backend/AppAdmin`) dùng chúng ra sao.

## 0. Tóm tắt

- Backend làm tốt phần lõi: 3 lớp tách bạch, mọi câu SQL qua `DbHelper` (có vị trí file:dòng), mọi thao tác nhiều bước nằm trong 1 giao dịch,
  trừ kho bằng `UPDATE ... WHERE so_luong + @thayDoi >= 0` nên không âm kho kể cả khi 2 người bấm cùng lúc, đổi trạng thái kiểu
  "chỉ đổi khi trạng thái hiện tại đúng là X" nên 2 nhân viên không xử lý trùng 1 đơn.
- 18 controller, 127 endpoint (60 GET, 53 POST, 9 PUT, 5 DELETE). Đủ cho 13 chức năng và 9 yêu cầu đồ án; app admin không cần thêm API nào vì gọi thẳng BLL.
- Tìm thấy **20 vấn đề**, không có lỗi làm hỏng dữ liệu kho hay tiền ngay lập tức. Đáng sửa nhất:
  1. Trả hàng không tính lại điểm / hạng thành viên.
  2. Sửa khuyến mãi voucher: voucher cũ không xóa được, đổi hình thức khác thì voucher cũ **vẫn còn dùng được**.
  3. SĐT khách thêm tại quầy không được chuẩn hóa như luồng OTP, có thể sinh 2 khách cho cùng 1 số.
  4. OTP: đếm số lần nhập sai không nguyên tử, gửi song song nhiều lần đoán được nhiều hơn 5 lần.
  5. Đăng nhập không giới hạn số lần sai.
- App admin đã chặn ở giao diện các trường hợp 2 (khóa mã voucher cũ, khóa đổi hình thức), tự khóa / tự cho nghỉ việc chính mình,
  và hiển thị rõ các quy ước dễ hiểu nhầm (phiếu nhập không sửa được, bộ nhớ đệm thống kê). Các chỗ còn lại nên sửa ở BLL / DAL (mục 6).

## 1. Kiến trúc và quy ước

```
Web bán hàng (React) ─┐
Web staff (React) ────┼─ HTTP/JSON ─> WebApi (18 controller, JWT) ─┐
App admin (WinForms) ─┴──────────────── gọi thẳng ─────────────────┴─> BLL ─> DAL (Dapper, DbHelper) ─> SQL Server (23 bảng)
```

| Quy ước | Cách làm | Nhận xét |
| --- | --- | --- |
| Lỗi nghiệp vụ | BLL ném `LoiNghiepVu` (400) / `KhongTimThay` (404), thông báo tiếng Việt. `BoLocLoi` đổi cả `SqlException` 547, 2627, ≥ 50000 | Tốt, app admin dùng lại đúng quy ước này trong `HaTang/ThaoTac.cs` |
| Đăng nhập theo nơi | `TaiKhoanBLL.DangNhap(..., NoiDangNhap)`: khách → web bán hàng, nhân viên → web staff, ADMIN → app admin (+ web staff) | Đúng yêu cầu giáo viên về TAI_KHOAN – NHAN_VIEN – KHACH_HANG |
| Khóa tài khoản | `OnTokenValidated` gọi `ConHoatDong` (nhớ 30 giây) | Khóa từ app admin (process khác) có hiệu lực trên web sau tối đa 30 giây |
| Token khách vãng lai | Vai trò `VANG_LAI`, 30 phút, chỉ dùng được dưới `/api/vang-lai` | Chặn đúng chỗ (middleware), không phụ thuộc từng controller |
| Sinh mã | `SinhMa` lấy MAX với `UPDLOCK, HOLDLOCK` trong giao dịch | Không trùng mã, đổi lại là tuần tự hóa việc tạo chứng từ (ổn với quy mô cửa hàng) |
| Yêu cầu 1 | `[CallerFilePath]`, `[CallerLineNumber]` trong `DbHelper`, header `X-So-Cau-Sql`, `X-Sql-Vi-Tri` | Cách làm gọn, không phải sửa từng câu SQL |

## 2. Bản đồ API

| Nhóm | Controller (đường dẫn) | Ai dùng | Chức năng đồ án |
| --- | --- | --- | --- |
| Tài khoản | `TaiKhoan` (`/api/tai-khoan`): đăng nhập 3 nơi, đăng ký, đổi / đặt lại mật khẩu, khóa / mở | Cả 3 giao diện | Quản lý tài khoản |
| Danh mục | `LoaiSp`, `SanPham`, `NhaCungCap` | Web khách đọc, admin ghi | Loại SP, sản phẩm, NCC |
| Nhân sự | `NhanVien` (ADMIN), `KhachHang` (hồ sơ của tôi, tìm SĐT, rủi ro, hạng điểm, hợp / giao) | Admin, web staff | Nhân viên, khách hàng |
| Bán hàng | `GioHang`, `DonHang`, `HoaDon`, `HoanTien`, `KhuyenMai` | Khách, nhân viên | Giỏ hàng, đơn, hóa đơn, hoàn tiền, khuyến mãi |
| Kho | `PhieuNhap`, `DoiTra` | Nhân viên | Nhập hàng, đổi trả |
| Khách vãng lai | `VangLai` (`/api/vang-lai`): OTP, xác thực, đặt / tra cứu / hủy đơn, PDF | Web khách | Mua không cần tài khoản |
| Báo cáo | `ThongKe` (5a-5e, json / excel / pdf), `TimKiem` (nâng cao), `Excel`, `NhatKySql` | Admin, nhân viên | Báo cáo thống kê, yêu cầu 1-8 |

Nhận xét chung:
- Kiểu đặt tên RPC cho thao tác (`POST /don-hang/{ma}/xac-nhan`, `/giao-hang`, `/hoan-tat`) rõ nghĩa hơn REST thuần với luồng trạng thái, nên giữ.
- Phân quyền đặt ở controller (`[Authorize(Roles = ...)]`) và kiểm tra quyền sở hữu chứng từ của khách ở `ChiCuaMinh`: đủ, không thấy endpoint nào để lộ dữ liệu khách khác.
- Danh sách (hóa đơn, đơn, khách hàng, sản phẩm) không phân trang. Với dữ liệu mẫu (vài trăm dòng) ổn; nên ghi rõ giới hạn này trong báo cáo.

## 3. Các luồng nghiệp vụ

### 3.1 Đơn đặt hàng online

```
                 khách hủy (chưa trừ kho)
          ┌───────────────────────────────────> DA_HUY
CHO_XAC_NHAN ──xác nhận──> DA_XAC_NHAN ──giao──> DANG_GIAO ──hoàn tất──> HOAN_TAT
   │  (trừ kho + lập HĐ;        │  ▲                  │      (HĐ COD → đã thanh toán, cộng điểm)
   │   CK/online: HĐ đã TT)     │  │ từ chối xin hủy  │
   └─từ chối─> TU_CHOI          │  │ (bắt buộc lý do) └─ nhân viên hủy (khách không nhận): trả kho, hủy HĐ,
                                │  │                     phiếu hoàn tiền nếu đã trả trước, TÍNH bom hàng
                    khách xin hủy (yeu_cau_huy = CHO_DUYET, chặn giao) ─ duyệt ─> DA_HUY (không tính bom hàng)
```

Đúng và chặt: kho chỉ trừ lúc xác nhận nên đơn ảo không giữ hàng; mọi bước đổi trạng thái kèm điều kiện trạng thái cũ; hủy đơn đã có hóa đơn
thì trả kho + hủy HĐ + tính lại điểm + tạo `HOAN_TIEN` trong cùng giao dịch.
Chỗ cần lưu ý: kiểm tra "khách đang xin hủy" ở `GiaoHang` / `NhanVienHuy` chạy **trước** khi mở giao dịch (vấn đề 11).
Hóa đơn COD lập lúc xác nhận nên thống kê ghi doanh thu vào ngày xác nhận, không phải ngày thu tiền (vấn đề 20).

### 3.2 Bán tại quầy

`HoaDonBLL.TinhTruoc` (xem trước) → `BanTaiQuay`: tính lại giá trong giao dịch (không tin số giao diện gửi), lập HĐ đã thanh toán, trừ kho, tính lại điểm.
Hủy HĐ tại quầy chỉ cho HĐ không thuộc đơn online và chưa có phiếu đổi trả. Luồng đúng.

### 3.3 Nhập hàng

`PhieuNhapBLL.Lap`: gộp dòng trùng, chặn 1 SP 2 giá trong 1 phiếu, NCC phải đang hợp tác, cộng kho trong giao dịch, xóa bộ nhớ đệm thống kê.
Thiếu: không sửa / hủy được phiếu (vấn đề 6). App admin hỏi xác nhận kèm tổng tiền trước khi lưu.

### 3.4 Đổi trả

`GuiYeuCau` kiểm: HĐ đã thanh toán, trong 30 ngày, không vượt số lượng đã mua trừ các phiếu chưa bị từ chối, đổi chỉ sang biến thể cùng tên.
`HoanTat`: nhập hàng về kho; đổi thì xuất hàng mới (chặn nếu thiếu); trả thì hoàn theo giá thực trả (`tổng HĐ / (tổng + giảm)`), làm tròn từng dòng.
Thiếu: không tính lại điểm / hạng sau khi trả hàng (vấn đề 1).

### 3.5 Khuyến mãi và tính tiền

`KhuyenMaiBLL.TinhTien` dùng chung cho giỏ, đơn, tại quầy: giá dòng = giá − % KM sản phẩm cao nhất; giảm cả đơn = voucher (nếu nhập) hoặc mức HĐ cao nhất đạt được;
khách có tài khoản còn lượt thì so thêm ưu đãi thành viên mới, lấy mức có lợi hơn, **không cộng dồn**. Logic rõ, có ghi chú cho giao diện.
Vấn đề: sửa KM voucher (vấn đề 2), nhiều chương trình thành viên mới chạy cùng lúc (vấn đề 8), HĐ chỉ lưu 1 `ma_km` (vấn đề 12).

### 3.6 Khách vãng lai (OTP)

Gửi mã (60 giây / lần, 5 mã / ngày / SĐT, giới hạn theo IP) → xác thực (5 phút, sai 5 lần phải gửi lại, so sánh thời gian hằng) → token 30 phút.
Chống đơn ảo: 5 cái / SKU, 2 đơn chờ / SĐT, COD ≤ 5 triệu, 2 lần bom hàng thì cấm COD. Đăng ký bắt buộc OTP đúng SĐT nên đã vá lỗ hổng "đăng ký bằng SĐT người khác".
Vấn đề: vấn đề 3 (đếm sai không nguyên tử, không kiểm mục đích mã) và vấn đề 7 (SĐT tại quầy không chuẩn hóa nên có thể không khớp với khách đã xác thực OTP).

### 3.7 Thống kê 2 cách (yêu cầu 3, 5)

`ThongKeBLL`: `TAI_SU_DUNG` tải 1 lần 3 danh sách (HĐ đã thanh toán, chi tiết, khoản chi) rồi gom bằng LINQ, nhớ 5 phút; `TRUY_VAN_MOI` gửi GROUP BY.
Kết quả kèm thời gian và số câu SQL. Bộ nhớ đệm là biến static trong từng process (vấn đề 13). Quy ước "chi = phiếu nhập + tiền hoàn trả hàng" nên ghi rõ trong báo cáo (vấn đề 20).

### 3.8 Tìm kiếm nâng cao (yêu cầu 2, 6)

`TimKiemBLL`: tên trường lấy từ danh sách cho phép, giá trị luôn là tham số, tối đa 10 điều kiện, ghép AND / OR; cùng bộ điều kiện chạy được bằng SQL mới
hoặc lọc trên danh sách có sẵn (cùng ngữ nghĩa NULL). Không có lỗ hổng SQL injection. Hạn chế: không ghép lồng `(A OR B) AND C` (một mức AND hoặc OR cho cả bộ).

## 4. Điểm làm tốt (nên nêu trong báo cáo)

1. Ràng buộc nằm ở BLL + CSDL (CHECK, UNIQUE, filtered index), giao diện chỉ hiển thị: 3 giao diện dùng chung 1 bộ quy tắc.
2. Giao dịch + cập nhật có điều kiện chống ghi đè và âm kho khi nhiều người thao tác cùng lúc.
3. Giá luôn tính lại ở server lúc chốt đơn / hóa đơn.
4. Lịch sử hủy đơn đủ dấu vết (ai, lúc nào, lý do, phản hồi của cửa hàng), phân biệt hủy theo yêu cầu với bom hàng.
5. Có 3 tầng kiểm thử (56 test SQL, 60 xUnit, 116 Playwright) và README ghi rõ các lỗi đã sửa.

## 5. Vấn đề phát hiện

Mức: **Cao** = sai dữ liệu / có thể bị lợi dụng; **TB** = sai nghiệp vụ hoặc thiếu chức năng quan trọng; **Thấp** = hiệu năng, nhất quán, quy ước.

| # | Mức | Vấn đề | Ở đâu | Đề xuất |
| --- | --- | --- | --- | --- |
| 1 | TB | Trả hàng hoàn tiền nhưng không tính lại điểm / hạng. `TongDaMua` cộng nguyên tổng HĐ, không trừ tiền đã hoàn, nên khách trả hàng vẫn giữ điểm | `DoiTraBLL.HoanTat`, `KhachHangDAL.TongDaMua` | `TongDaMua` trừ `SUM(tien_hoan)` của phiếu TRA hoàn tất; gọi `TichDiem.TinhLai` trong giao dịch `HoanTat` |
| 2 | Cao | Sửa KM voucher: `KhuyenMaiDAL.Sua` không xóa voucher cũ (đúng, vì có thể đã dùng), nhưng giao diện không biết nên tưởng đã xóa. Nặng hơn: đổi hình thức VOUCHER → loại khác thì các mã cũ vẫn nằm trong `CT_KM_VOUCHER` và `LayVoucherDangChay` (không lọc `hinh_thuc`) **vẫn áp dụng được** | `KhuyenMaiDAL.Sua`, `LayVoucherDangChay` | BLL chặn đổi hình thức khi đã có voucher; `LayVoucherDangChay` thêm `AND k.hinh_thuc = 'VOUCHER'`; thêm cột `trang_thai` cho voucher để ngừng từng mã. App admin đã khóa mã cũ và khóa ô hình thức |
| 3 | TB | OTP: `XacThuc` đọc `so_lan_sai` rồi mới tăng, không nguyên tử: gửi 50 request song song thì cả 50 lần đoán đều được xét. Không kiểm `muc_dich` lúc xác thực. Băm SHA-256(SĐT:mã) không có khóa bí mật, lộ CSDL thì dò lại 10^6 mã rất nhanh (README đang ghi "lộ CSDL cũng không lộ mã") | `XacThucBLL.XacThuc`, `MaOtpDAL` | `UPDATE MA_OTP SET so_lan_sai += 1 OUTPUT inserted.so_lan_sai WHERE ma_otp = @id AND so_lan_sai < 5` trước khi so mã; băm HMAC với khóa trong cấu hình; sửa câu trong README |
| 4 | TB | Đăng nhập không giới hạn số lần sai (OTP thì có), dò mật khẩu 6 ký tự được | `TaiKhoanBLL.DangNhap` | Đếm lần sai theo tài khoản + IP, khóa tạm 15 phút sau 5 lần |
| 5 | Thấp | Nhân viên đã nghỉ việc không cho đi làm lại được: `NhanVienDAL.Sua` không cập nhật `trang_thai`, mở khóa tài khoản xong vẫn bị chặn "Nhân viên đã nghỉ việc" | `NhanVienBLL`, `NhanVienDAL.Sua` | Thêm `NhanVienBLL.DiLamLai(maNv)` (đổi DANG_LAM + mở khóa TK trong 1 giao dịch) |
| 6 | TB | Phiếu nhập không sửa / hủy được; nhập sai số lượng thì tồn kho sai vĩnh viễn (trừ khi sửa tay CSDL) | `PhieuNhapBLL` | Thêm "hủy phiếu nhập" (trừ lại kho nếu còn đủ) hoặc phiếu điều chỉnh tồn kho có lý do |
| 7 | TB | `KhachHangBLL.ThemTaiQuay / Sua` không gọi `QuyTac.ChuanHoaSdt` như luồng OTP: `84901234567` và `0901234567` thành 2 khách; chấp nhận 9-11 số trong khi OTP chỉ nhận 10 số di động, nên khách tại quầy có thể không bao giờ gắn được với tài khoản / đơn vãng lai | `KhachHangBLL.KiemTra` | Chuẩn hóa SĐT trong `KiemTra`, dùng chung `LaSdtDiDong` |
| 8 | Thấp | Cho phép nhiều chương trình THANH_VIEN_MOI chạy trùng thời gian; hệ thống lấy chương trình % cao nhất, admin dễ nhầm | `KhuyenMaiBLL.KiemTra` | Chặn chồng thời gian với KM thành viên mới khác |
| 9 | Thấp | Admin tự khóa tài khoản của mình hoặc tự cho mình nghỉ việc được; có thể khóa hết mọi ADMIN | `TaiKhoanBLL.KhoaMo`, `NhanVienBLL.NghiViec` | Truyền người thao tác vào BLL và chặn; luôn còn ít nhất 1 ADMIN hoạt động. App admin đã chặn ở giao diện |
| 10 | Thấp | N+1 câu SQL: `DoiTraBLL.LayDanhSach` chạy 1 + N câu (mỗi phiếu 1 câu chi tiết); `HoaDonBLL.Huy` tải toàn bộ phiếu đổi trả chỉ để kiểm 1 HĐ | `DoiTraBLL`, `HoaDonBLL.Huy` | 1 câu lấy chi tiết theo danh sách mã (`WHERE ma_doi_tra IN @ds`); `EXISTS` theo `ma_hoa_don` |
| 11 | Thấp | `GiaoHang` và `NhanVienHuy` đọc `yeu_cau_huy` ngoài giao dịch: khách gửi xin hủy đúng lúc nhân viên bấm giao thì đơn vẫn chuyển sang đang giao | `DonDatHangBLL` | Đưa điều kiện vào câu UPDATE: `WHERE trang_thai = 'DA_XAC_NHAN' AND ISNULL(yeu_cau_huy,'') <> 'CHO_DUYET'` |
| 12 | Thấp | HĐ chỉ có 1 cột `ma_km`: đơn vừa có KM sản phẩm vừa có voucher chỉ ghi voucher, thống kê hiệu quả KM sản phẩm bị thiếu | Thiết kế CSDL | Ghi rõ quy ước trong báo cáo, hoặc bảng `HOA_DON_KM` nhiều dòng |
| 13 | Thấp | Bộ nhớ đệm `TAI_SU_DUNG` là static trong từng process: HĐ lập trên web (process WebApi) không xóa bộ đệm của app admin, nên app admin lệch với `TRUY_VAN_MOI` tối đa 5 phút | `ThongKeBLL._dem` | Chấp nhận được với yêu cầu 3; app admin hiện giờ tải và có nút "Làm mới dữ liệu đã tải" |
| 14 | Thấp | Nhập KH / NCC từ Excel: lỗi báo "Dòng i" theo thứ tự trong danh sách hợp lệ, không phải số dòng trong file | `ExcelBLL.TungDong` | Giữ số dòng gốc trong DTO kết quả đọc |
| 15 | Thấp | Mật khẩu tạm dùng `Random.Shared` (không dùng cho bảo mật), chỉ 6 chữ số | `TaiKhoanBLL.DatLaiMatKhau` | `RandomNumberGenerator`, 8-10 ký tự chữ + số, bắt đổi ở lần đăng nhập đầu |
| 16 | Thấp | Từ chối đơn (`TuChoi`) không có lý do, trong khi hủy / từ chối xin hủy đều có; khách không biết vì sao đơn bị từ chối | `DonDatHangBLL.TuChoi` | Nhận `LyDoDTO`, lưu vào `phan_hoi_huy` |
| 17 | Thấp | Swagger bật ở mọi môi trường; `Jwt:Key` nằm trong `appsettings.json` đưa lên repo | `Program.cs`, `appsettings.json` | Chỉ bật Swagger khi Development; đọc khóa từ biến môi trường khi triển khai |
| 18 | Thấp | API danh sách không phân trang | `HoaDon`, `DonHang`, `KhachHang`, `SanPham` | Thêm `trang`, `soDong` khi dữ liệu lớn |
| 19 | Thấp | Kiểu tham số không đồng nhất: `PUT /khach-hang/{ma}/hang-diem?hang=&diem=` nhận query thay vì body; mở bán lại là `POST /san-pham/{ma}/ngung-ban?ngung=false` | `KhachHangController`, `SanPhamController` | Body JSON; tách `/mo-ban` |
| 20 | Thấp | Quy ước thống kê cần ghi rõ: tiền hoàn trả hàng tính là "chi" (không trừ doanh thu), HĐ trả 1 phần vẫn tính đủ ở 5a / 5c / 5d / 5e; doanh thu COD tính theo ngày xác nhận đơn | `ThongKeDAL`, `DonDatHangBLL.XacNhan` | Ghi trong báo cáo hoặc hỏi giáo viên chọn cách tính |

## 6. Ưu tiên sửa

1. Vấn đề 2 (voucher vẫn dùng được sau khi đổi hình thức): 1 dòng SQL trong `LayVoucherDangChay` + 1 kiểm tra trong `KhuyenMaiBLL.Sua`.
2. Vấn đề 1 và 7: sai dữ liệu khách hàng, sửa trong BLL, có thể thêm test vào `Tests/NghiepVuTest.cs`.
3. Vấn đề 3 và 4: bảo mật đăng nhập / OTP, giáo viên hay hỏi khi demo.
4. Vấn đề 6 và 5: thêm chức năng hủy phiếu nhập, cho nhân viên đi làm lại (app admin có sẵn chỗ đặt nút).
5. Các mục Thấp: ghi vào phần "hạn chế" của báo cáo nếu không kịp sửa.

## 7. App admin dùng backend thế nào

App desktop `backend/AppAdmin` (WinForms .NET 8) gọi thẳng BLL như README backend quy định, không đi qua WebApi và không gọi DAL.
Chi tiết màn hình, phím tắt, cách chạy: xem `backend/AppAdmin/README.md`.

| Chức năng (13) | Màn hình | BLL dùng |
| --- | --- | --- |
| Quản lý loại SP | Loại sản phẩm | `LoaiSpBLL` |
| Quản lý sản phẩm | Sản phẩm & tồn kho | `SanPhamBLL`, `TimKiemBLL`, `ExcelBLL` |
| Quản lý giỏ hàng | Khách hàng → tab Giỏ hàng (xem, xóa giỏ của khách) | `GioHangBLL.Xem / XoaHet` |
| Quản lý tài khoản | Tài khoản | `TaiKhoanBLL` |
| Quản lý đơn đặt hàng | Đơn đặt hàng | `DonDatHangBLL`, `KhachHangBLL.RuiRo`, `PdfBLL` |
| Quản lý khách hàng | Khách hàng & giỏ hàng | `KhachHangBLL`, `TimKiemBLL`, `ExcelBLL` |
| Quản lý nhân viên | Nhân viên | `NhanVienBLL`, `TaiKhoanBLL.DatLaiMatKhau` |
| Quản lý nhà cung cấp | Nhà cung cấp | `NhaCungCapBLL`, `ExcelBLL` |
| Quản lý nhập hàng | Nhập hàng | `PhieuNhapBLL`, `ExcelBLL.DocChiTietPhieuNhap`, `PdfBLL` |
| Quản lý hóa đơn | Hóa đơn (+ lập hóa đơn tại quầy) | `HoaDonBLL`, `TimKiemBLL`, `PdfBLL` |
| Đổi trả và hoàn tiền | Đổi trả, Hoàn tiền | `DoiTraBLL`, `HoanTienBLL` |
| Chương trình khuyến mãi | Khuyến mãi | `KhuyenMaiBLL` |
| Báo cáo và thống kê | Tổng quan, Báo cáo thống kê, Tìm kiếm nâng cao | `ThongKeBLL`, `TimKiemBLL`, `ExcelBLL`, `PdfBLL` |

| Yêu cầu (9) | Trên app admin |
| --- | --- |
| 1. Dòng code chạy truy vấn | Thanh trạng thái sau mỗi thao tác: số câu SQL + `File.cs:dòng Hàm()` của câu cuối + thời gian; màn hình Nhật ký SQL (lọc, gom theo vị trí, xem câu đầy đủ kèm tham số) |
| 2. Tìm kiếm tái sử dụng / truy vấn mới | Ô tìm ở Sản phẩm, Khách hàng, Hóa đơn: F3 đổi chế độ, cùng 1 bộ điều kiện chạy `TimKiemBLL.Loc...` (0 câu SQL) hoặc `Tim...` (SELECT mới), hiện số câu + ms |
| 3. Thống kê 2 cách | Báo cáo thống kê: chọn cách tính, nút "So sánh 2 cách" chạy cả hai, đối chiếu từng ô, bảng thời gian + số câu SQL |
| 4. Main-sub | Mọi danh sách: chọn dòng (chuột / ↑ ↓) là chi tiết bên phải tự nạp, chỉ chạy câu SQL của dòng dừng lại (hẹn 150 ms) |
| 5a-5e | 5 báo cáo, bảng + biểu đồ, xuất Excel / PDF |
| 6. Tìm nâng cao hợp / giao | Tìm kiếm nâng cao: tối đa 10 điều kiện, 8 toán tử, AND / OR, ví dụ có sẵn; 3 truy vấn UNION / INTERSECT viết sẵn |
| 7. Excel | Xuất mọi danh sách; nhập SP / KH / NCC / chi tiết phiếu nhập: xem trước dòng hợp lệ + lỗi từng dòng rồi mới ghi; tải file mẫu |
| 8. PDF | Hóa đơn, đơn hàng, phiếu nhập, phiếu đổi trả, báo cáo thống kê |
| 9. Bàn phím | Mọi nút có phím tắt ghi ngay trên nút; F1 / `?` mở bảng phím tắt; Ctrl+K mở nhanh màn hình; Ctrl+0-9 chuyển màn hình |

## 8. Câu hỏi nên hỏi lại giáo viên

1. Tiền hoàn khi trả hàng nên tính là "chi" hay trừ thẳng vào doanh thu của kỳ (ảnh hưởng 5a, 5b)?
2. Doanh thu đơn COD tính theo ngày xác nhận đơn (hiện tại) hay ngày giao xong / thu tiền?
3. Phiếu nhập nhập sai: cần chức năng hủy phiếu hay phiếu điều chỉnh tồn kho?
4. Admin có cần bán tại quầy trên app desktop không (app đã có sẵn, web staff cũng có)?
