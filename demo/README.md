# Bản demo đồ án QL_BanThoiTrang (UNIQLO)

Chạy toàn bộ hệ thống trên một máy bằng **2 lần bấm đúp**: web bán hàng (khách), web staff (nhân viên), App Admin (quản lý) và WebApi, dùng chung một CSDL có sẵn dữ liệu mẫu.

| File | Làm gì | Khi nào |
| --- | --- | --- |
| `1_CAI_DAT_CSDL.bat` | Tự tìm SQL Server (`localhost` hoặc `.\SQLEXPRESS`), tạo CSDL `QL_BanThoiTrang` + nạp dữ liệu mẫu, chạy 56 test kiểm tra CSDL. **Đã có CSDL thì giữ nguyên, không xóa.** | 1 lần trên mỗi máy |
| `2_CHAY_DEMO.bat` | Mở WebApi, 2 web và App Admin, đợi sẵn sàng rồi tự mở trình duyệt. Lần đầu tự cài thư viện (vài phút) | Mỗi lần demo |
| `3_DUNG_DEMO.bat` | Tắt WebApi và 2 web | Demo xong |
| `4_LAM_MOI_DU_LIEU.bat` | Đưa CSDL về dữ liệu mẫu sạch (sao lưu `.bak` trước rồi mới xóa, phải gõ `LAMMOI` để xác nhận) | Sau khi tập demo, trước buổi demo thật |

## Cần cài trước (1 lần)

- **SQL Server** 2017 trở lên (Express hoặc Developer), có `sqlcmd` (đi kèm khi cài). Đăng nhập bằng Windows Authentication.
- **.NET 8 SDK**: chạy WebApi và App Admin.
- **Node.js** 18 trở lên: chạy 2 web.
- Trình duyệt Chrome / Edge.

Không cần Visual Studio. Không cần cài thư viện bằng tay: `2_CHAY_DEMO.bat` tự `npm install` và tự tải gói NuGet ở lần đầu.

## Tài khoản demo (mật khẩu đều là `123456`)

| Nơi | Tài khoản | Vai trò |
| --- | --- | --- |
| App Admin | `giamdoc` (Giám đốc), `quanly` (Quản lý cửa hàng) | ADMIN |
| Web staff http://localhost:5174 | `thungan1` (bán tại quầy), `banhangonline` (đơn online), `thukho` (nhập hàng), `quanly` (tất cả) | NHAN_VIEN / ADMIN |
| Web bán hàng http://localhost:5173 | `khach01` … `khach14` (`khach15` đang bị khóa) | KHACH_HANG |
| Mua không cần tài khoản | Trang `/tra-cuu-don`, SĐT `0987000021`. SĐT `0987000022` là khách "bom hàng" 2 lần | Khách vãng lai (OTP hiện ngay trên màn hình ở chế độ demo) |

Voucher có sẵn: `WELCOME10`. Khuyến mãi đang chạy: KM02 (hóa đơn từ 1 triệu giảm 5%), KM05 (giảm giá 5 sản phẩm UNIQLO).

## Kịch bản demo theo 9 yêu cầu (App Admin)

Thanh trạng thái dưới cùng App Admin luôn ghi: tên thao tác, **số câu SQL**, **dòng code chạy câu cuối** (ví dụ `HoaDonDAL.cs:34 LayChiTiet()`), thời gian. Bấm `F1` ở bất kỳ màn nào để xem bảng phím tắt.

| # | Yêu cầu | Thao tác | Điểm cần chỉ cho giảng viên |
| --- | --- | --- | --- |
| 1 | Hiện dòng code chạy truy vấn | Làm bất kỳ thao tác nào, nhìn thanh trạng thái. `Ctrl+L` mở Nhật ký SQL | `File.cs:dòng`, tên hàm, số câu SQL của thao tác và của cả phiên |
| 2 | Tìm kiếm: tái sử dụng hay truy vấn mới | `Ctrl+4` Sản phẩm, gõ `jean`; bấm `F3` đổi chế độ rồi gõ lại | Tái sử dụng: **0 câu SQL**. Truy vấn mới: **1 câu SQL**. Cùng số dòng |
| 3 | Thống kê 2 cách | `Ctrl+9` Báo cáo thống kê, `Alt+2` (5b), bấm `Ctrl+M` So sánh 2 cách | Bảng thời gian + số câu SQL của 2 cách, dòng "Kết quả 2 cách giống nhau từng ô" |
| 4 | Main-sub, không có nút chi tiết | `Ctrl+3` Hóa đơn, bấm 1 dòng / dùng phím mũi tên | Chi tiết hiện ngay bên phải, chỉ **1 câu SQL** mỗi lần chọn |
| 5a–5e | 5 báo cáo | Trong Báo cáo thống kê: `Alt+1` … `Alt+5`, `F5` xem | 5b chọn kỳ tháng / quý / năm / "Tất cả các năm" (thu, chi, lợi nhuận). 5c, 5d, 5e có dòng / cột Tổng cộng. `Ctrl+E` Excel, `Ctrl+P` PDF |
| 6 | Tìm nâng cao, hợp và giao | `Ctrl+0` Tìm kiếm nâng cao. Ví dụ "HĐ đã thanh toán từ 1 triệu (AND)" rồi `F5`. Nút **A ∪ B (UNION)** với TP Hồ Chí Minh và Hà Nội | Toán tử `>=`, AND = giao, OR = hợp. Câu hỏi "khách ở TP HCM và Hà Nội" → **OR / hợp** (AND ra 0 vì không ai ở 2 tỉnh) |
| 7 | Đọc, ghi Excel | Ở Sản phẩm / Khách hàng: `Ctrl+E` xuất, "File mẫu", `Ctrl+I` nhập | Bước xem trước báo dòng hợp lệ + lỗi từng dòng, bấm Lưu mới ghi vào CSDL |
| 8 | PDF hóa đơn, phiếu, báo cáo | `Ctrl+P` ở Hóa đơn, Đơn hàng, Nhập hàng, Đổi trả, Báo cáo thống kê | File lưu ở `Tài liệu\QLBanThoiTrang`, tự mở để in |
| 9 | Hỗ trợ bàn phím | `Ctrl+K` mở nhanh màn hình (gõ tên không dấu), `Ctrl+1…9`, `F1` bảng phím tắt | Mọi nút có phím tắt ghi ngay trên nút |

Thêm: **rê chuột** vào một dòng ở bất kỳ bảng nào (tài khoản, đơn, hóa đơn, sản phẩm…) để hiện thẻ xem nhanh; rê sang dòng khác thì thẻ đổi theo.

## Kịch bản liên thông 3 giao diện (5 phút)

1. **Web bán hàng** (`khach01`): thêm 1 sản phẩm vào giỏ, áp voucher `WELCOME10`, đặt hàng COD.
2. **App Admin** → Đơn đặt hàng (`Ctrl+2`): đơn mới hiện đầu danh sách, chọn rồi `Alt+X` xác nhận → kho bị trừ, hóa đơn được lập.
3. **Web bán hàng**: khách vào Tài khoản → Đơn hàng thấy trạng thái "Đã xác nhận".
4. **Web staff** (`banhangonline`): bấm Giao hàng → Hoàn tất. Số liệu Tổng quan ở cả 3 nơi khớp nhau.
5. **App Admin** → Báo cáo thống kê 5a: doanh thu hôm nay tăng đúng bằng đơn vừa hoàn tất.

## Sự cố thường gặp

| Hiện tượng | Cách xử lý |
| --- | --- |
| `1_CAI_DAT_CSDL.bat` báo không kết nối được SQL Server | Mở `services.msc`, bật dịch vụ "SQL Server (...)". Tên instance khác `localhost` / `SQLEXPRESS` thì sửa file `demo\may_chu.txt` (vd `.\TEN_INSTANCE`) |
| Cửa sổ "Demo - WebApi" báo lỗi kết nối CSDL | Chưa chạy bước 1, hoặc dịch vụ SQL Server đang tắt |
| Báo cổng 5212 / 5173 / 5174 đang bận | Đã có bản cũ đang chạy: chạy `3_DUNG_DEMO.bat` rồi chạy lại bước 2 |
| App Admin không đăng nhập được | Chỉ tài khoản ADMIN (`giamdoc`, `quanly`) vào được App Admin, đúng như thiết kế |
| Web thiếu ảnh sản phẩm | Ảnh nằm trong `web-ban-hang/public/images/uniqlo`, web staff lấy ảnh qua web bán hàng (cổng 5173) nên cần chạy cả 2 web |

Dữ liệu mẫu, cách tạo CSDL thủ công bằng SSMS: xem `database/README.md`. Chi tiết App Admin: `backend/AppAdmin/README.md`.
