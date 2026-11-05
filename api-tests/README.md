# Kiểm thử API backend bằng Playwright

116 test gọi thẳng HTTP vào WebApi (không cần trình duyệt), phủ toàn bộ 18 controller (kể cả khách mua không cần tài khoản, v3): đăng nhập theo nơi, phân quyền
ADMIN / NHAN_VIEN / KHACH_HANG / chưa đăng nhập, CRUD danh mục, giỏ hàng, luồng đơn online (đặt, xác nhận, giao, hoàn tất, hủy),
bán tại quầy, phiếu nhập, đổi trả, khuyến mãi, thống kê 2 cách, tìm kiếm nâng cao, Excel, PDF, nhật ký SQL.

| File | Nội dung |
| --- | --- |
| `tests/01-tai-khoan.spec.ts` | Đăng nhập 3 nơi, đăng ký, đổi mật khẩu, khóa / mở / đặt lại mật khẩu, token của tài khoản bị khóa |
| `tests/02-danh-muc.spec.ts` | Loại SP, sản phẩm (tìm, biến thể, tồn thấp, thêm nhiều, ngừng bán), nhà cung cấp, body sai kiểu |
| `tests/03-nhan-su.spec.ts` | Nhân viên (thêm kèm tài khoản, nghỉ việc), khách hàng (hồ sơ của tôi, tìm SĐT, hạng điểm, hợp / giao) |
| `tests/04-khuyen-mai.spec.ts` | Tính tiền (voucher, mức hóa đơn, KM sản phẩm), CRUD khuyến mãi, kết thúc KM |
| `tests/05-ban-hang.spec.ts` | Giỏ hàng, đơn online COD / chuyển khoản, bán tại quầy, main-sub hóa đơn, PDF, xem chứng từ người khác |
| `tests/06-kho-doi-tra.spec.ts` | Phiếu nhập, đổi / trả hàng (hoàn tiền theo giá thực trả, quá 30 ngày) |
| `tests/08-vang-lai.spec.ts` | v3: OTP (sai SĐT, gửi lại quá sớm, nhập sai), đặt / tra cứu / hủy đơn không cần tài khoản, token vãng lai không gọi được API khác, chống đơn ảo, nhân viên lọc đơn + xem rủi ro SĐT, đăng ký phải có OTP, gắn đơn cũ, ưu đãi 3 đơn đầu |
| `tests/09-huy-don.spec.ts` | v3: hủy ngay khi chờ xác nhận, xin hủy đơn đã xác nhận → chặn giao → duyệt (trả kho, hủy HĐ, phiếu hoàn tiền → đã hoàn) / từ chối (bắt buộc lý do), đang giao không hủy được, khách vãng lai xin hủy không bị tính bom hàng |
| `tests/07-bao-cao.spec.ts` | Thống kê 5a-5e (2 cách ra cùng kết quả, Excel, PDF), tìm kiếm nâng cao, Excel xuất / đọc / nhập, nhật ký SQL, Swagger, CORS |

Mỗi test tự tạo sản phẩm / khách / hóa đơn riêng (tên có hậu tố theo thời gian) nên chạy lại nhiều lần trên cùng CSDL vẫn được.
Sau khi chạy, `database/KiemThu_QL_BanThoiTrang.sql` vẫn ra 56/56 PASS.

Test đăng ký và khách vãng lai cần API chạy ở môi trường Development (`dotnet run` mặc định đã vậy) để API trả mã OTP demo (`Otp:HienMaDemo`).

## Chạy

1. Tạo CSDL và nạp dữ liệu mẫu (xem `database/README.md`). Test dựa vào dữ liệu mẫu: tài khoản `quanly`, `thungan1`, `khach01`,
   `khach15` (bị khóa), `LSP01`, `NCC01`..`NCC04`, KM02 (hóa đơn từ 1 triệu giảm 5%), voucher `WELCOME10`.
2. Chạy API: `dotnet run --project backend/WebApi` (mặc định http://localhost:5212).
3. Trong thư mục này:

```
npm install
npx playwright test                 # chạy hết
npx playwright test tests/05-ban-hang.spec.ts   # 1 file
npx playwright show-report          # xem báo cáo HTML
```

API ở địa chỉ khác thì đặt biến `API_URL`, ví dụ `API_URL=http://localhost:5000 npx playwright test`.

## Lỗi backend tìm ra và đã sửa

| # | Lỗi | Sửa ở |
| --- | --- | --- |
| 1 | Khóa tài khoản / cho nhân viên nghỉ việc nhưng token JWT cũ vẫn dùng được thêm tối đa 12 giờ | `Program.cs` (OnTokenValidated), `TaiKhoanBLL.ConHoatDong`, `NhanVienBLL.NghiViec`. Câu kiểm tra được nhớ 30 giây và không tính vào header `X-So-Cau-Sql` |
| 2 | Sửa / xóa theo mã không tồn tại trả 400 thay vì 404 (loại SP, sản phẩm, NCC, nhân viên, khách hàng, hạng điểm, kết thúc KM), trái với quy ước trong README | `QuyTac.CanTimThay` trong `ChungBLL.cs`, dùng ở `DanhMucBLL`, `NhanSuBLL`, `KhuyenMaiBLL` |
| 3 | "Kết thúc" khuyến mãi bắt đầu hôm nay thì vẫn còn áp dụng hôm nay; kết thúc KM đã hết hạn từ trước lại kéo dài ngày kết thúc tới hôm qua | `KhuyenMaiDAL.KetThuc`, `KhuyenMaiBLL.KetThuc` |
| 4 | `POST /api/gio-hang` nhận số lượng 0 hoặc âm, số âm làm giảm số lượng trong giỏ | `GioHangBLL.Them` |
| 5 | Thống kê cách TAI_SU_DUNG trả số cũ tới 5 phút sau khi có hóa đơn / phiếu nhập / đổi trả mới, lệch với TRUY_VAN_MOI | Gọi `ThongKeBLL.XoaBoNhoDem()` sau bán tại quầy, xác nhận / hoàn tất / hủy đơn, hủy hóa đơn, lập phiếu nhập, hoàn tất đổi trả |
| 6 | `GET /api/thong-ke/tong-tien` thiếu `tuNgay` / `denNgay` gây lỗi 500 (SqlDateTime overflow); `san-pham` thiếu `nam` không báo lỗi | `BaoCaoController.cs` |
| 7 | Body thiếu trường / sai kiểu trả ProblemDetails mặc định, không có `thongBao` như các lỗi khác | `BoLocLoi.LoiDuLieuGuiLen`, đăng ký trong `Program.cs` |
| 8 | Đọc Excel sản phẩm: mã loại không tồn tại vẫn được coi là hợp lệ ở bước xem trước, tới lúc ghi mới hỏng cả lô | `ExcelBLL.DocSanPham` |
| 9 | File Excel có 2 cột tiêu đề trùng nhau gây lỗi 500 | `ExcelBLL.Doc` |
| 10 | Script kiểm thử CSDL báo FAIL sai khi nhân viên hủy đơn đã xác nhận (đơn và hóa đơn cùng DA_HUY là hợp lệ) | `database/KiemThu_QL_BanThoiTrang.sql` (A4) |
