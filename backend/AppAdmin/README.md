# App desktop admin (WinForms .NET 8)

Ứng dụng quản trị cửa hàng chạy trên Windows, **gọi thẳng BLL** (không qua WebApi, không gọi DAL), dùng chung CSDL và quy tắc nghiệp vụ với web bán hàng và web staff.
Chỉ tài khoản vai trò `ADMIN` đăng nhập được (`TaiKhoanBLL.DangNhap(..., NoiDangNhap.AppAdmin)`).

Không thêm thư viện NuGet nào ngoài những gì BLL đã dùng. Biểu đồ vẽ bằng GDI+ (`HaTang/BieuDoCot.cs`), giao diện dựng bằng code (không có file `.Designer.cs`).

## Chạy

1. Tạo CSDL như `database/README.md` (`QL_BanThoiTrang_v3.sql` rồi `QL_BanThoiTrang_DuLieuMau.sql`).
2. Mở `backend/QLBanThoiTrang.sln` bằng Visual Studio 2022 (cần workload **.NET desktop development**), chuột phải `AppAdmin` → *Set as Startup Project*, F5.
   Dòng lệnh: `dotnet run --project backend/AppAdmin` (chỉ chạy trên Windows).
3. Chuỗi kết nối nằm trong `AppAdmin/appsettings.json` (mặc định `localhost\SQLEXPRESS`, Windows Authentication).
   Sửa ngay trong app: màn hình đăng nhập → **Cấu hình kết nối CSDL (F8)** → Kiểm tra kết nối → Lưu (ghi lại `appsettings.json` cạnh file .exe).
4. Đăng nhập `quanly` hoặc `giamdoc`, mật khẩu `123456`.

File Excel / PDF lưu ở `Documents\QLBanThoiTrang` (đổi bằng `App:ThuMucXuat` trong `appsettings.json`). PDF lưu xong tự mở để xem / in.

## Cấu trúc

```
AppAdmin/
  Program.cs            đọc cấu hình, nạp vào BLL (HeThongBLL.CauHinh, ThongTinCuaHang), đăng nhập → cửa sổ chính
  FormDangNhap.cs       đăng nhập + cấu hình kết nối CSDL
  FormChinh.cs          menu trái, vùng màn hình, thanh trạng thái SQL, phím tắt toàn app, bảng phím tắt, mở nhanh, đổi mật khẩu
  HaTang/               dùng chung
    ThaoTac.cs          mọi lệnh gọi BLL chạy qua đây: đếm câu SQL, vị trí File.cs:dòng, đổi lỗi thành thông báo tiếng Việt
    Luoi.cs             bảng dữ liệu (DataGridView VirtualMode): cột khai báo bằng hàm, sắp xếp, giữ dòng chọn, sự kiện main-sub
    ManHinh.cs          lớp cha của màn hình: tiêu đề, thanh nút gắn phím tắt; KhungThongTin, KhungChiTiet, BoCuc (chia main-sub)
    OTimKiem.cs         ô tìm 2 chế độ (tái sử dụng / truy vấn mới), báo số dòng + số câu SQL + ms
    FormNhap.cs         lớp cha hộp thoại thêm / sửa (Ctrl+S lưu, Esc hủy, lỗi nghiệp vụ thì giữ hộp thoại)
    BieuDoCot.cs        biểu đồ cột nhóm, hỗ trợ số âm, rê chuột xem giá trị
    GiaoDien.cs Nhan.cs màu, font, nhãn tiếng Việt cho mã trạng thái
    HopThoai.cs         hỏi / thông báo / nhập lý do, lưu Excel, mở PDF
    CauHinhApp.cs       đọc / ghi appsettings.json, phiên đăng nhập
  CacManHinh/           16 màn hình (mỗi file 1 nhóm)
  CacHopThoai/          hộp thoại: sản phẩm, NCC, nhân viên, khách hàng, khuyến mãi, lập phiếu nhập / hóa đơn / đổi trả, xem trước Excel
```

## Màn hình

| Menu | Làm được gì |
| --- | --- |
| Tổng quan | Ô số liệu: đơn chờ xác nhận, khách xin hủy, đổi trả chờ duyệt, chờ hoàn tiền, SP tồn thấp, doanh thu hôm nay / tháng, KM đang chạy (bấm để mở danh sách đã lọc sẵn); biểu đồ thu - chi - lợi nhuận theo tháng |
| Đơn đặt hàng | Lọc trạng thái / nguồn (tài khoản, vãng lai) / ngày; xác nhận, từ chối, giao, hoàn tất, hủy (kèm lý do, cảnh báo bom hàng), duyệt / từ chối xin hủy; độ tin cậy SĐT; in PDF |
| Hóa đơn | Main-sub; tìm 2 chế độ; lập hóa đơn tại quầy (tìm khách theo SĐT, voucher, tiền thối); hủy HĐ tại quầy; lập đổi trả từ HĐ; PDF, Excel |
| Đổi trả | Duyệt, từ chối, nhận hàng & hoàn tất; lập phiếu tại quầy (chọn dòng HĐ, đổi sang biến thể cùng mẫu), hoàn tất ngay; PDF |
| Hoàn tiền | Phiếu chờ hoàn (tổng tiền), đánh dấu đã hoàn kèm ghi chú đối soát, mở đơn liên quan |
| Sản phẩm & tồn kho | Tìm 2 chế độ, lọc loại / trạng thái / tồn thấp; thêm 1 SKU hoặc 1 mẫu nhiều size × màu; sửa; ngừng / mở bán; giá KM hôm nay; biến thể cùng mẫu; Excel xuất / nhập / mẫu |
| Loại sản phẩm | Thêm, sửa tên, xóa (chặn khi còn SKU), đếm số mẫu / SKU |
| Nhà cung cấp | Thêm, sửa, xóa hoặc chuyển ngừng hợp tác nếu đã có phiếu; phiếu nhập của NCC; Excel |
| Nhập hàng | Lập phiếu (chọn SP hoặc đọc Excel có xem trước lỗi), lọc ngày / NCC, chi tiết phiếu, PDF, Excel |
| Khách hàng & giỏ hàng | Tìm 2 chế độ (SĐT / mã / tên), lọc hạng, có / không tài khoản; thêm khách tại quầy, sửa, chỉnh hạng điểm; tab Thông tin + độ tin cậy SĐT, Đơn hàng, Hóa đơn, Giỏ hàng (xem / xóa giỏ); Excel |
| Nhân viên | Thêm kèm tài khoản, sửa, cho nghỉ việc (khóa tài khoản), đặt lại mật khẩu, hóa đơn và doanh số của nhân viên, Excel |
| Tài khoản | Lọc vai trò / trạng thái, khóa / mở khóa, đặt lại mật khẩu (hiện mật khẩu tạm có nút sao chép) |
| Khuyến mãi | 4 hình thức; lọc hình thức / tình trạng; thêm, sửa (bảng chi tiết sửa trực tiếp), kết thúc ngay |
| Báo cáo thống kê | 5a-5e, tham số theo từng báo cáo, 2 cách tính, so sánh 2 cách, bảng + biểu đồ, Excel / PDF |
| Tìm kiếm nâng cao | Hóa đơn / khách hàng / sản phẩm, tối đa 10 điều kiện, AND / OR, 2 cách chạy, ví dụ có sẵn, 3 truy vấn UNION / INTERSECT, xuất Excel kết quả |
| Nhật ký SQL | 100-500 câu gần nhất kèm vị trí trong DAL, lọc, gom theo vị trí (số lần, tổng ms), xem câu đầy đủ + tham số, bật / tắt ghi, tự làm mới |

## 9 yêu cầu đồ án

| Yêu cầu | Ở đâu trong app |
| --- | --- |
| 1. Dòng code chạy truy vấn | Thanh trạng thái dưới cùng sau **mỗi** thao tác: `Tên thao tác: N câu SQL · câu cuối ở HoaDonDAL.cs:42 LayDanhSach() · 12 ms`, cộng dồn số câu của phiên. Bấm vào để mở Nhật ký SQL. Code: `HaTang/ThaoTac.cs` |
| 2. Tìm kiếm tái sử dụng / truy vấn mới | F3 ở Sản phẩm, Khách hàng, Hóa đơn. Cùng 1 `TimKiemNangCaoDTO` chạy `TimKiemBLL.Loc...` hoặc `Tim...`, cạnh ô tìm hiện `12 dòng · 0 câu SQL · 0 ms` để so sánh |
| 3. Thống kê 2 cách | Báo cáo thống kê: radio cách tính (F3), **So sánh 2 cách** (Ctrl+M) chạy cả hai, báo "Kết quả 2 cách giống nhau từng ô" |
| 4. Main-sub | Hóa đơn → chi tiết chỉ 1 câu SQL (`HoaDonBLL.LayChiTiet`). Tương tự đơn hàng, phiếu nhập, NCC, nhân viên, khách hàng, khuyến mãi, sản phẩm |
| 5a-5e | Báo cáo thống kê (Alt+1 … Alt+5 chọn báo cáo) |
| 6. Hợp / giao | Tìm kiếm nâng cao: AND = giao, OR = hợp; nút UNION / INTERSECT viết sẵn |
| 7. Excel | Ctrl+E xuất, Ctrl+I nhập (xem trước + lỗi từng dòng), nút File mẫu |
| 8. PDF | Ctrl+P ở hóa đơn, đơn hàng, phiếu nhập, đổi trả, báo cáo |
| 9. Bàn phím | Bảng dưới đây; F1 hoặc `?` mở bảng phím tắt của màn hình đang mở |

## Phím tắt

| Phím | Tác dụng |
| --- | --- |
| `F1` / `?` | Bảng phím tắt (tự liệt kê nút của màn hình đang mở) |
| `Ctrl+K` | Mở nhanh màn hình: gõ tên (không dấu cũng được), Enter |
| `Ctrl+1 … Ctrl+9`, `Ctrl+0`, `Ctrl+L` | Tổng quan, Đơn hàng, Hóa đơn, Sản phẩm, Nhập hàng, Khách hàng, Nhân viên, Khuyến mãi, Thống kê, Tìm kiếm nâng cao, Nhật ký SQL |
| `F5` | Làm mới / xem báo cáo |
| `Ctrl+F` hoặc `/` | Vào ô tìm kiếm; `Esc` xóa ô tìm |
| `F3` | Đổi chế độ tìm (tái sử dụng ↔ truy vấn mới) / đổi cách tính thống kê |
| `Ctrl+N` | Thêm mới (`Ctrl+Shift+N`: thêm mẫu SP nhiều biến thể) |
| `F2` / `Enter` | Sửa dòng đang chọn |
| `Delete` | Xóa / ngừng bán / khóa / hủy HĐ / kết thúc KM (luôn hỏi lại) |
| `Ctrl+E`, `Ctrl+I`, `Ctrl+P` | Xuất Excel, nhập Excel, in PDF |
| `↑ ↓ PgUp PgDn Home End` | Chọn dòng, chi tiết tự cập nhật; `Tab` ra khỏi bảng |
| Đơn hàng | `Alt+X` xác nhận, `Alt+T` từ chối, `Alt+G` giao, `Alt+H` hoàn tất, `Alt+U` hủy, `Alt+D` duyệt xin hủy, `Alt+R` từ chối xin hủy |
| Đổi trả / Hoàn tiền | `Alt+X` duyệt / đã hoàn, `Alt+T` từ chối, `Alt+H` hoàn tất |
| Hộp thoại | `Ctrl+S` hoặc `Ctrl+Enter` lưu, `Esc` đóng; lập HĐ: `F2` mã SP, `F4` SĐT, `F6` voucher, `F7` thanh toán, `Insert` chọn SP, `F9` thanh toán |

## Chặn thêm ở giao diện (backend chưa chặn, xem `PHAN_TICH_API_VA_FLOW.md`)

- Không tự khóa tài khoản đang đăng nhập, không tự cho mình nghỉ việc (vấn đề 9).
- Sửa khuyến mãi voucher: mã đã lưu không xóa / đổi tên được (BLL bỏ qua thao tác xóa), không đổi được hình thức khi đã có voucher (vấn đề 2).
- Phiếu nhập: hỏi xác nhận kèm tổng tiền vì không sửa / hủy được sau khi lưu (vấn đề 6).
- Xóa NCC đã có phiếu nhập: đề nghị chuyển sang ngừng hợp tác thay vì báo lỗi.

## Kiểm thử thủ công gợi ý (dữ liệu mẫu)

1. Đăng nhập `thungan1` → bị từ chối ("Chỉ tài khoản quản lý (ADMIN)..."); `quanly` → vào được.
2. Hóa đơn: bấm 1 dòng → thanh trạng thái báo `1 câu SQL · HoaDonDAL.cs:…`. Gõ `HD000` ở chế độ tái sử dụng (0 câu) rồi F3 (1 câu).
3. Báo cáo thống kê → 5b → So sánh 2 cách: lần đầu tái sử dụng chạy 3 câu, lần sau 0 câu; truy vấn mới luôn 2 câu; kết quả giống nhau.
4. Tìm kiếm nâng cao → ví dụ "Khách ở TP HCM HOẶC hạng Vàng (OR = hợp)", đổi sang AND để thấy kết quả giao.
5. Đơn đặt hàng → lọc "Chờ xác nhận" → đơn `DH00075` (SĐT bom hàng 2 lần) hiện "Rủi ro cao"; xác nhận đơn khác → tồn kho giảm, có hóa đơn mới.
6. Nhập hàng → Tải file mẫu → điền 2 dòng (1 dòng mã SP sai) → Đọc từ Excel → xem trước báo 1 lỗi → lưu → tồn kho tăng.
7. Khuyến mãi → thêm voucher `THU10` 10% → Hóa đơn → Lập hóa đơn tại quầy → nhập `THU10` → tiền giảm hiện ngay.
