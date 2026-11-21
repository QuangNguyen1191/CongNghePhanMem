# Backend QL_BanThoiTrang (.NET 8)

Dùng chung cho 3 giao diện: web bán hàng (khách), web staff (nhân viên), app desktop admin (WinForms).

## Các lớp

```
WebApi (lớp giao diện cho 2 web)   AppAdmin (WinForms, gọi thẳng BLL)
            \                         /
             BLL  - quy tắc nghiệp vụ, thống kê 2 cách, tìm kiếm nâng cao, Excel, PDF
              |
             DAL  - toàn bộ câu SQL (Dapper), ghi nhật ký file + dòng code chạy truy vấn
              |
           SQL Server (database/QL_BanThoiTrang_v3.sql)
DTO: đối tượng truyền dữ liệu giữa các lớp
```

Lớp giao diện không gọi DAL. Cấu hình kết nối và nhật ký SQL đi qua `BLL/HeThongBLL.cs`.

| Project | Nội dung |
| --- | --- |
| DTO | Lớp dữ liệu: sản phẩm, khách, nhân viên, đơn, hóa đơn, phiếu nhập, đổi trả, bảng thống kê... |
| DAL | `DbHelper` (mọi câu SQL chạy qua đây), `NhatKySql`, các lớp `...DAL` theo bảng |
| BLL | `TaiKhoanBLL`, `XacThucBLL` (OTP, v3), `DanhMucBLL`, `NhanSuBLL`, `KhuyenMaiBLL`, `BanHangBLL`, `KhoBLL`, `ThongKeBLL`, `TimKiemBLL`, `ExcelBLL`, `PdfBLL` |
| WebApi | 18 controller, JWT, Swagger |
| Tests | 60 test tích hợp chạy trên CSDL có dữ liệu mẫu (khách vãng lai: `VangLaiTest.cs`, hủy đơn: `HuyDonTest.cs`) |
| AppAdmin | App desktop admin WinForms .NET 8 (16 màn hình, đủ 13 chức năng + 9 yêu cầu), xem `AppAdmin/README.md` |

## Chạy

1. Tạo CSDL: chạy `database/QL_BanThoiTrang_v3.sql` rồi `database/QL_BanThoiTrang_DuLieuMau.sql` (xem `database/README.md`).
2. Sửa chuỗi kết nối trong `WebApi/appsettings.json` nếu cần. Mặc định dùng Windows Authentication:
   `Server=localhost;Database=QL_BanThoiTrang;Trusted_Connection=True;TrustServerCertificate=True`
   (SQL Express thì đổi `Server=localhost\\SQLEXPRESS`).
3. Mở `QLBanThoiTrang.sln` bằng Visual Studio 2022, đặt WebApi làm startup project, F5 (app desktop: đặt `AppAdmin` làm startup project).
   Hoặc dòng lệnh: `dotnet run --project WebApi`, rồi mở http://localhost:5212/swagger
4. Trong Swagger: gọi `POST /api/tai-khoan/dang-nhap/web-staff` với `quanly` / `123456`, copy `token`,
   bấm Authorize và dán vào để thử các API khác.

Chạy test: sửa chuỗi kết nối qua biến môi trường `QLBTT_KET_NOI` (mặc định `sa` / `Strong!Pass123` trên localhost), rồi
`dotnet test Tests`. Test có tạo thêm hóa đơn, đơn, phiếu nhập; muốn sạch thì tạo lại CSDL.

Test API bằng Playwright (121 test, gọi HTTP vào WebApi đang chạy): xem `../api-tests/README.md`.

## Đăng nhập theo nơi

| Đường dẫn | Ai vào được |
| --- | --- |
| `POST /api/tai-khoan/dang-nhap/web-ban-hang` | KHACH_HANG |
| `POST /api/tai-khoan/dang-nhap/web-staff` | NHAN_VIEN, ADMIN |
| `POST /api/tai-khoan/dang-nhap/app-admin` | ADMIN (app desktop gọi `TaiKhoanBLL.DangNhap(..., NoiDangNhap.AppAdmin)`) |

Lỗi nghiệp vụ trả về 400 (không tìm thấy: 404, thiếu quyền theo chức vụ: 403) với body `{ "thongBao": "..." }` tiếng Việt.

## Phân quyền theo chức vụ (nhân viên)

Bên trong vai trò NHAN_VIEN, server chặn từng API theo **nhóm quyền** suy ra từ chức vụ (`NHAN_VIEN.chuc_vu`), không chỉ ẩn menu ở web.
Quy tắc ở `BLL/PhanQuyenBLL.cs` (`NhomQuyen.TheoChucVu`), chốt chặn `[CanQuyen(...)]` ở `WebApi/HaTang/CanQuyen.cs`.

| Chức vụ chứa | Nhóm quyền | API được dùng thêm (ngoài sản phẩm, tổng quan, nhật ký SQL, đổi mật khẩu) |
| --- | --- | --- |
| "thu ngân", "bán hàng tại quầy" | `ban-hang` | bán tại quầy (`hoa-don/tinh-truoc`, `ban-tai-quay`, `hoa-don/{ma}/huy`) + nhóm chứng từ bên dưới |
| "online" | `don-online` | xử lý đơn (`api/don-hang`: danh sách, xác nhận, giao, hoàn tất, hủy, duyệt / từ chối xin hủy) + nhóm chứng từ |
| "kho" | `kho` | `api/phieu-nhap`, `GET api/nha-cung-cap`, Excel phiếu nhập / nhà cung cấp |
| `ban-hang` hoặc `don-online` | | hóa đơn, đổi trả, hoàn tiền, khách hàng (SĐT, địa chỉ), tìm nâng cao hóa đơn / khách, Excel hóa đơn / khách |
| "quản lý", "giám đốc", vai trò ADMIN | tất cả | |
| không khớp từ khóa nào | không có | chỉ xem sản phẩm, tổng quan (mặc định an toàn: admin đặt đúng chức vụ để cấp quyền) |

Nhóm quyền **không nằm trong token**: mỗi request tính lại từ chức vụ hiện tại (nhớ 30 giây cùng lúc kiểm tra tài khoản bị khóa,
`TaiKhoanBLL.KiemTraPhien`), admin sửa chức vụ thì có hiệu lực ngay ở request kế tiếp. Đăng nhập web staff trả kèm `nhomQuyen` để web ẩn menu
đúng theo server. Khách hàng không bị ảnh hưởng (vẫn chỉ xem chứng từ của mình). Test: `api-tests/tests/10-chuc-vu.spec.ts`.

## v3: Khách mua không cần tài khoản (`api/vang-lai`)

1. `POST /api/vang-lai/otp` `{ sdt, mucDich: MUA_HANG | TRA_CUU | DANG_KY }`: gửi mã 6 số. Hiện là **OTP giả lập**:
   mã in ra console WebApi, và khi `Otp:HienMaDemo = true` (bật sẵn trong `appsettings.Development.json`) API trả luôn `maDemo`
   để web hiện lên màn hình. Muốn gửi SMS / email thật: viết lớp cài `IGuiOtp` rồi gán `XacThucBLL.BoGui`.
2. `POST /api/vang-lai/xac-thuc` `{ sdt, ma }`: đúng mã thì nhận token vai trò `VANG_LAI` (30 phút, chỉ dùng được cho `/api/vang-lai/*`).
3. Với token đó: `POST don-hang` (gửi kèm `dongHangs` vì giỏ nằm ở trình duyệt), `GET don-hang`, `GET don-hang/{ma}`,
   `POST don-hang/{ma}/huy`, `GET don-hang/{ma}/pdf`, `GET phien`.

Chống đơn ảo (hằng số trong `ChungBLL.QuyTac`): mã hết hạn 5 phút, sai 5 lần phải gửi mã mới, 60 giây mới gửi lại, tối đa 5 mã / SĐT / ngày,
giới hạn theo IP (`Otp:ToiDaMoiIpMoiGio`); khách vãng lai tối đa 5 cái / sản phẩm, 2 đơn đang chờ / SĐT, COD tối đa 5.000.000đ;
SĐT đã "bom hàng" 2 lần (đơn bị hủy sau khi đã lập hóa đơn) không được chọn COD. Nhân viên xem mức rủi ro: `GET /api/khach-hang/rui-ro/{sdt}`,
lọc đơn: `GET /api/don-hang?nguon=VANG_LAI`.

Đăng ký (`POST /api/tai-khoan/dang-ky`) bắt buộc SĐT và `phienXacThuc` (token OTP của đúng SĐT đó). Đơn cũ của SĐT tự gắn vào tài khoản mới
(`soDonDaGan`). Ưu đãi thành viên mới (`GET /api/khuyen-mai/thanh-vien-moi`): giảm 10% cho 3 đơn đầu đặt bằng tài khoản, tối đa 100.000đ / đơn,
không cộng dồn, chỉ thay voucher / KM hóa đơn khi giảm nhiều hơn.

## v3: Hủy đơn và hoàn tiền

| Trạng thái đơn | Khách (`POST /api/don-hang/{ma}/yeu-cau-huy` hoặc `/api/vang-lai/don-hang/{ma}/yeu-cau-huy`, body `{ lyDo }`) |
| --- | --- |
| `CHO_XAC_NHAN` | Hủy ngay (chưa trừ kho, chưa có hóa đơn) |
| `DA_XAC_NHAN` | Gửi yêu cầu (`yeu_cau_huy = CHO_DUYET`), mỗi đơn 1 lần. Nhân viên không giao được đơn đang có yêu cầu |
| `DANG_GIAO` trở đi | Không hủy trực tuyến được (liên hệ hotline / từ chối nhận hàng) |

Nhân viên: `POST /api/don-hang/{ma}/duyet-huy` (hủy: trả kho, hủy hóa đơn, tính lại điểm, **không tính bom hàng**),
`POST /api/don-hang/{ma}/tu-choi-huy` `{ lyDo }` (bắt buộc lý do cho khách), lọc `GET /api/don-hang?yeuCauHuy=CHO_DUYET`,
`POST /api/don-hang/{ma}/huy` `{ lyDo? }` (nhân viên tự hủy, vd khách không nhận hàng: tính bom hàng).
Hóa đơn đã thanh toán (chuyển khoản / online) bị hủy thì tự sinh phiếu `HOAN_TIEN` chờ hoàn:
`GET /api/hoan-tien?trangThai=CHO_HOAN`, `POST /api/hoan-tien/{ma}/da-hoan` `{ lyDo? }` (ghi chú).

## 9 yêu cầu đồ án nằm ở đâu

| Yêu cầu | Chỗ trong code |
| --- | --- |
| 1. Hiện dòng code chạy truy vấn | `DAL/DbHelper.cs` ghi `[CallerFilePath]`, `[CallerLineNumber]`, tên hàm vào `DAL/NhatKySql.cs`. Web: header `X-So-Cau-Sql`, `X-Sql-Vi-Tri` trên mọi response và `GET /api/nhat-ky-sql`. Console WebApi in từng câu khi `NhatKySql:InRaConsole = true` |
| 2. Tìm kiếm: tái sử dụng / truy vấn mới | `TimKiemBLL.Loc...` lọc danh sách đang có (0 câu SQL) / `TimKiemBLL.Tim...` và `GET /api/san-pham?tuKhoa=` chạy câu mới |
| 3. Thống kê 2 cách | `ThongKeBLL`: tham số `cach = TAI_SU_DUNG` (dùng lại danh sách hóa đơn đã tải, gom bằng LINQ) hoặc `TRUY_VAN_MOI` (GROUP BY trong `ThongKeDAL`). Kết quả kèm thời gian và số câu SQL để so sánh |
| 4. Main-sub | `GET /api/hoa-don` (danh sách) + `GET /api/hoa-don/{ma}/chi-tiet` (chỉ 1 câu SQL khi bấm 1 dòng) |
| 5a-5e | `GET /api/thong-ke/tong-tien`, `thu-chi`, `nhan-vien`, `khach-hang`, `san-pham` (thêm `dinhDang=excel` hoặc `pdf`) |
| 6. Tìm nâng cao, hợp và giao | `POST /api/tim-kiem/hoa-don|khach-hang|san-pham` với `>, <, >=, <=, =, <>, LIKE, IN`, `khopTatCa` true = giao (AND), false = hợp (OR). Thêm `GET /api/khach-hang/hai-tinh-hop` (UNION) và `o-tinh-a-giao-tinh-b`, `vua-tai-quay-vua-online` (INTERSECT) |
| 7. Excel | `ExcelBLL`, `api/excel`: xuất danh sách, tải file mẫu, đọc file (xem trước + lỗi từng dòng) rồi mới ghi |
| 8. PDF | `PdfBLL`: hóa đơn, đơn đặt hàng, phiếu nhập, phiếu đổi trả, báo cáo thống kê. Font tiếng Việt nhúng sẵn trong BLL (`BLL/Fonts`, DejaVu Sans) |
| 9. Bàn phím | Web bán hàng: phím tắt toàn trang (`/`, `?`, `g`+chữ, `a`, `f`, `Ctrl+Enter`...) trong `web-ban-hang/src/features/phimTat`, bảng phím tắt mở bằng `?` (xem `web-ban-hang/README.md`). Web staff: bấm `?`. App admin: mọi nút có phím tắt, F1 / `?` mở bảng phím tắt, Ctrl+K mở nhanh màn hình (xem `AppAdmin/README.md`) |

## Thư viện

Dapper 2.1.35, Microsoft.Data.SqlClient 5.2.2, BCrypt.Net-Next 4.0.3, ClosedXML 0.104.2,
QuestPDF 2024.10.4 (giấy phép Community, miễn phí cho đồ án), JwtBearer 8.0.11, Swashbuckle 6.6.2, xUnit.
