# Hướng dẫn test App Admin theo 9 yêu cầu đồ án

Mỗi mục ghi: **mở màn hình nào → làm gì → thấy gì là ĐẠT**. Đánh dấu ☐ → ☑ khi test xong.
Các con số (số dòng, số khách...) tính trên **dữ liệu mẫu mới nạp**. Nếu bạn đã thêm hay sửa dữ liệu thì số có thể lệch một chút, chỉ cần đúng logic là đạt.

---

## 0. Chuẩn bị

### 0.1 CSDL dùng để test

Trên máy bạn, SQL Server tên là `localhost\SQLEXPRESS`. Hiện có 3 CSDL:

| CSDL | Ghi chú |
| --- | --- |
| `QL_BanThoiTrang` | CSDL chính, dữ liệu mẫu **cũ** (tạo 02/11), có thể thiếu bảng/cột mới |
| `QL_BanThoiTrang_KiemThu` | CSDL test cũ (05/11) |
| `QL_BanThoiTrang_HDTest` | **Mới tạo 25/11 từ repo mới nhất, dữ liệu mẫu sạch. Nên dùng cái này để test** |

Muốn tạo lại CSDL test sạch (ví dụ sau khi test đã thêm nhiều hóa đơn), xem mục 0.4.

### 0.2 Chạy App Admin

```powershell
cd C:\Users\vugia\OneDrive\Desktop\CNPM\backend
```

```powershell
dotnet run --project AppAdmin
```

(Hoặc mở `backend\QLBanThoiTrang.sln` bằng Visual Studio 2022, đặt `AppAdmin` làm startup project, F5.)

### 0.3 Trỏ app vào đúng CSDL

File cấu hình mặc định dùng `Server=localhost`, nhưng máy bạn là `localhost\SQLEXPRESS`, nên lần đầu sẽ báo lỗi kết nối:

1. Ở màn hình đăng nhập bấm **Cấu hình kết nối CSDL (F8)**.
2. Nhập chuỗi:
   `Server=localhost\SQLEXPRESS;Database=QL_BanThoiTrang_HDTest;Trusted_Connection=True;TrustServerCertificate=True`
3. Bấm **Kiểm tra kết nối** → thấy báo thành công → **Lưu**.
4. Đăng nhập **`quanly` / `123456`** (hoặc `giamdoc` / `123456`).

### 0.4 (Tùy chọn) Tạo lại CSDL test sạch

Mở **SSMS**, chạy `DROP DATABASE QL_BanThoiTrang_HDTest;`, rồi nhờ mình tạo lại, hoặc tự làm: chép 2 file `database\QL_BanThoiTrang_v3.sql`, `database\QL_BanThoiTrang_DuLieuMau.sql`, thay chữ `QL_BanThoiTrang` thành `QL_BanThoiTrang_HDTest` trong 2 bản chép, rồi chạy lần lượt bằng SSMS.

### 0.5 Test đăng nhập (phụ)

| ☐ | Làm | Đạt khi |
| --- | --- | --- |
| ☐ | Đăng nhập `thungan1` / `123456` | Bị từ chối: "Chỉ tài khoản quản lý (ADMIN)..." |
| ☐ | Đăng nhập `quanly` / `123456` | Vào được cửa sổ chính, menu trái có 16 mục |

---

## Yêu cầu 1. Chỉ ra dòng code chạy câu truy vấn

**Nhìn ở đâu:** thanh trạng thái **dưới cùng** cửa sổ chính.

| ☐ | Làm | Đạt khi |
| --- | --- | --- |
| ☐ | `Ctrl+3` mở **Hóa đơn**, bấm vào 1 hóa đơn bất kỳ | Thanh dưới hiện dạng `Chi tiết hóa đơn (sub): 1 câu SQL · câu cuối ở HoaDonDAL.cs:<dòng> <TênHàm>() · <n> ms`. Góc phải cộng dồn tổng số câu SQL của phiên |
| ☐ | Bấm vào thanh trạng thái (hoặc `Ctrl+L`) | Mở **Nhật ký SQL**: mỗi dòng là 1 câu SQL kèm `File.cs:dòng`, tên hàm, thời gian |
| ☐ | Trong Nhật ký SQL chọn 1 dòng | Hiện câu SQL đầy đủ + tham số. Có nút **Sao chép câu SQL**, chọn 100/200/500 câu, ô **Tự làm mới mỗi 2 giây** |
| ☐ | Mở file trong code để đối chiếu: `backend\DAL\HoaDonDAL.cs`, nhảy tới đúng số dòng vừa thấy | Dòng đó đúng là chỗ gọi câu SQL |

Giải thích khi giảng viên hỏi: mọi câu SQL đi qua `backend\DAL\DbHelper.cs` (dòng 22-35), dùng `[CallerFilePath]` + `[CallerLineNumber]` để tự lấy file và dòng đã gọi. App gom lại ở `backend\AppAdmin\HaTang\ThaoTac.cs`.

---

## Yêu cầu 2. Tìm kiếm: tái sử dụng danh sách hay chạy truy vấn mới

| ☐ | Làm | Đạt khi |
| --- | --- | --- |
| ☐ | `Ctrl+4` mở **Sản phẩm & tồn kho**. Cạnh ô tìm có nút **"Chế độ: tái sử dụng (F3)"** | Danh sách 195 SKU đã tải sẵn |
| ☐ | Bấm `Ctrl+F` (hoặc `/`), gõ `jean` | Sau khi ngừng gõ ~0,35 giây tự lọc. Cạnh ô tìm hiện `36 dòng · 0 câu SQL · 0 ms (lọc trên danh sách đã tải)` |
| ☐ | Bấm `F3` (nút đổi thành **"Chế độ: truy vấn mới (F3)"**) | Tự tìm lại, hiện `36 dòng · 1 câu SQL · <n> ms (câu SELECT mới)`. **Cùng số dòng** với chế độ tái sử dụng |
| ☐ | Gõ tiếp vài chữ khác ở mỗi chế độ | Tái sử dụng luôn **0 câu SQL**, truy vấn mới **mỗi lần gõ 1 câu SQL** |
| ☐ | Làm tương tự ở **Hóa đơn** (`Ctrl+3`, gõ `HD000`) và **Khách hàng** (`Ctrl+6`) | Cùng cách hoạt động |

Kết luận để trình bày: tái sử dụng nhanh hơn, không tốn CSDL; truy vấn mới luôn có dữ liệu mới nhất.

---

## Yêu cầu 3. Thống kê 2 cách (tái sử dụng / truy vấn mới)

| ☐ | Làm | Đạt khi |
| --- | --- | --- |
| ☐ | `Ctrl+9` mở **Báo cáo thống kê**. Hàng trên có "Cách tính: ◉ Tái sử dụng (LINQ) ○ Truy vấn mới (GROUP BY)" | |
| ☐ | Chọn báo cáo 5b (`Alt+2`), bấm `F5` | Dòng thông tin dưới tiêu đề ghi `Tái sử dụng (LINQ) · <n> ms · <k> câu SQL` |
| ☐ | Bấm `F3` (đổi cách tính) rồi `F5` | Ghi `Truy vấn mới (GROUP BY) · ...`, số liệu bảng giống hệt |
| ☐ | Bấm **So sánh 2 cách** (`Ctrl+M`) | Hiện khung so sánh: thời gian, số câu SQL, số dòng của 2 cách, và dòng **"Kết quả 2 cách giống nhau từng ô"** |
| ☐ | Bấm So sánh lần nữa | Lần đầu cách tái sử dụng có tải dữ liệu (vài câu SQL), **lần sau 0 câu SQL**; truy vấn mới lần nào cũng chạy SQL |
| ☐ | Bấm **Làm mới dữ liệu đã tải** (`Ctrl+Shift+R`) rồi So sánh lại | Cách tái sử dụng tải lại từ CSDL (lại có câu SQL) |

---

## Yêu cầu 4. Main-sub form (không có nút "Chi tiết")

| ☐ | Làm | Đạt khi |
| --- | --- | --- |
| ☐ | `Ctrl+3` mở **Hóa đơn** | Bảng hóa đơn bên trái, khung chi tiết bên phải. **Không có nút "Chi tiết"** |
| ☐ | Bấm vào bất kỳ ô nào của 1 hóa đơn, ví dụ `HD00002` | Chi tiết (thông tin + các dòng sản phẩm) hiện **ngay bên phải**. Thanh dưới báo **1 câu SQL** |
| ☐ | Dùng phím `↑ ↓` chuyển dòng | Chi tiết tự đổi theo, không cần bấm gì thêm |
| ☐ | Rê chuột vào 1 dòng (không bấm) | Hiện thẻ xem nhanh nội dung dòng đó |
| ☐ | Thử ở **Đơn đặt hàng** (`Ctrl+2`), **Nhập hàng** (`Ctrl+5`), **Khách hàng** (`Ctrl+6`), **Khuyến mãi** (`Ctrl+8`) | Cùng kiểu main-sub |

---

## Yêu cầu 5. Năm báo cáo thống kê

Mở **Báo cáo thống kê** (`Ctrl+9`). Chọn báo cáo bằng `Alt+1` … `Alt+5`, xem bằng `F5`.
Dữ liệu mẫu có hóa đơn các năm **2024, 2025, 2026**.

| ☐ | Báo cáo | Làm | Đạt khi |
| --- | --- | --- | --- |
| ☐ | **5a.** Tổng tiền từ ngày A tới ngày B | `Alt+1`, chọn Từ `01/01/2025`, Đến `31/12/2025`, `F5` | Ra số hóa đơn đã thanh toán và tổng tiền trong khoảng đó. Đổi ngày thì số đổi theo |
| ☐ | **5b.** Thu - chi - lợi nhuận theo tháng / quý / năm | `Alt+2`, chọn "Theo tháng" + năm `2025`, `F5`. Rồi thử "Theo quý", "Theo năm", và năm = "Tất cả các năm" | Mỗi kỳ có 3 cột **Thu, Chi, Lợi nhuận** + dòng **Tổng cộng**. Lợi nhuận âm hiện chữ đỏ |
| ☐ | **5c.** Theo nhân viên × năm | `Alt+3`, `F5` | Mỗi dòng 1 nhân viên (NV1, NV2...), mỗi năm 1 cột + cột **Tổng cộng**, dòng cuối **Tổng cộng** |
| ☐ | **5d.** Theo khách hàng × quý / năm | `Alt+4`, chọn `2025 (cột = quý)`, `F5`; rồi chọn `Tất cả (cột = năm)` | 1 năm: cột Quý 1-4. Tất cả: mỗi năm 1 cột. Có dòng và cột **Tổng cộng** |
| ☐ | **5e.** Theo sản phẩm × quý (giám đốc) | Đăng nhập **`giamdoc`**, `Alt+5`, chọn năm `2025`, `F5` | Mỗi dòng 1 sản phẩm (SP1, SP2...), cột Quý 1-4 + **Tổng cộng** |
| ☐ | **5e bị chặn với người không phải giám đốc** | Đăng xuất, đăng nhập **`quanly`** (Quản lý cửa hàng), `Ctrl+9`, `Alt+5` | Bảng và biểu đồ trống, tiêu đề ghi "5e. Theo sản phẩm × quý: chỉ dành cho giám đốc", dòng dưới ghi tài khoản Trần Thu Hà (Quản lý cửa hàng) không có quyền. Thanh trạng thái **không** có câu SQL nào chạy. Các báo cáo 5a-5d vẫn xem bình thường |
| ☐ | Xuất báo cáo | Ở báo cáo bất kỳ: `Ctrl+E` (Excel), `Ctrl+P` (PDF) | File mở ra được, có đủ dòng Tổng cộng |

> Quyền xem 5e dựa theo **chức vụ** của nhân viên gắn với tài khoản ADMIN: chức vụ có chữ "Giám đốc" mới xem được (`giamdoc` = Nguyễn Minh Khang, Giám đốc). Server cũng chặn: API `GET /api/thong-ke/san-pham` trả 403 cho `quanly`, 200 cho `giamdoc`. Code: `backend\BLL\PhanQuyenBLL.cs` (nhóm `bao-cao-giam-doc`), `backend\AppAdmin\CacManHinh\ThongKeMH.cs`, `backend\WebApi\Controllers\BaoCaoController.cs`.

### Giao diện báo cáo (gọn, top 10, biểu đồ bên phải)

| ☐ | Làm | Đạt khi |
| --- | --- | --- |
| ☐ | Nhìn bố cục màn hình | Chọn loại báo cáo, tham số, cách tính nằm **chung 1 hàng ngang** ở trên. Bảng bên **trái**, **biểu đồ cột bên phải**; kéo được vạch chia ở giữa |
| ☐ | 5e năm 2025, đăng nhập `giamdoc` (nhiều hơn 10 sản phẩm) | Tiêu đề ghi `· top 10`, bảng hiện 10 sản phẩm doanh thu cao nhất + Tổng cộng. Dòng thông tin ghi `hiện 10/<tổng> dòng · Tổng cộng tính trên tất cả` |
| ☐ | Bấm **Xem tất cả** (`Ctrl+T`) | Hiện đủ mọi dòng, nút đổi thành **Thu gọn top 10**. Biểu đồ cũng vẽ đủ |
| ☐ | 5b theo tháng, "Tất cả các năm" | Mặc định hiện **10 kỳ gần nhất** |
| ☐ | 5c (chỉ 6 nhân viên) | Nút **Xem tất cả vẫn bấm được**. Bấm thì nút đổi thành "Thu gọn top 10" và ngược lại; bảng vẫn hiện đủ 6 nhân viên ở cả 2 chế độ (dòng thông tin ghi `hiện 6/6 dòng`) |
| ☐ | Rê chuột lên cột của biểu đồ | Hiện giá trị của cột đó |

---

## Yêu cầu 6. Tìm kiếm nâng cao (>, <, >=, <=, hợp / giao)

Mở **Tìm kiếm nâng cao** (`Ctrl+0`).

### 6a. Tìm hóa đơn có tổng tiền lớn hơn X

| ☐ | Làm | Đạt khi |
| --- | --- | --- |
| ☐ | "Tìm trong" chọn **Hóa đơn**. Thêm điều kiện: trường **Tổng tiền**, toán tử `>`, giá trị `2000000`. `F5` | Ra **97** hóa đơn, tất cả có tổng tiền > 2.000.000 |
| ☐ | Đổi toán tử thành `>=`, `<`, `<=` và thử lại | Số dòng đổi đúng theo toán tử |
| ☐ | Ô "Ví dụ" chọn **"HĐ đã thanh toán từ 1 triệu (AND)"**, `F5` | Ra **173** hóa đơn (trạng thái = DA_THANH_TOAN **AND** tổng tiền >= 1.000.000) |
| ☐ | Bấm **Đổi cách chạy** rồi `F5` lại | Cùng số dòng; 1 cách 0 câu SQL (lọc danh sách đã tải), 1 cách chạy câu SQL mới |

### 6b. Khách hàng ở TP HCM và Hà Nội: HỢP hay GIAO?

**Đáp án: HỢP (OR / UNION).** Mỗi khách chỉ có 1 tỉnh, nên "ở TP HCM **và** ở Hà Nội" theo nghĩa giao (AND) ra 0 người. Câu hỏi thực tế là lấy danh sách khách của cả 2 nơi, tức là phép hợp.

| ☐ | Làm | Đạt khi |
| --- | --- | --- |
| ☐ | "Tìm trong" chọn **Khách hàng**. Điều kiện 1: Tỉnh / thành `=` `TP Hồ Chí Minh`. Điều kiện 2: Tỉnh / thành `=` `Hà Nội`. Ghép chọn **HOẶC (OR)**, `F5` | Ra **16** khách (9 ở TP HCM + 7 ở Hà Nội) |
| ☐ | Giữ nguyên, đổi Ghép sang **VÀ (AND)**, `F5` | Ra **0** khách → chứng minh dùng giao là sai |
| ☐ | Ở hàng dưới: Tỉnh A = `TP Hồ Chí Minh`, Tỉnh B = `Hà Nội`, bấm nút **"A ∪ B: khách ở tỉnh A hoặc tỉnh B (UNION)"** | Ra **16** khách, chạy câu SQL `UNION` viết sẵn (`backend\DAL\NguoiDungDAL.cs` dòng 124) |
| ☐ | Bấm **"Khách ở A ∩ có đơn giao tới B (INTERSECT)"** và **"Vừa mua tại quầy ∩ vừa đặt online (INTERSECT)"** | Ra danh sách theo phép giao (ví dụ đúng nghĩa của giao) |
| ☐ | Ví dụ có sẵn: **"Khách ở TP HCM HOẶC hạng Vàng (OR = hợp)"** → **13** khách; **"Khách ở Hà Nội VÀ trên 100 điểm (AND = giao)"** → **6** khách | Đúng số |
| ☐ | **Xuất Excel kết quả** | File Excel có đúng các dòng vừa tìm |

---

## Yêu cầu 7. Đọc / ghi file Excel

File lưu ở `Tài liệu\QLBanThoiTrang`.

| ☐ | Làm | Đạt khi |
| --- | --- | --- |
| ☐ | **Sản phẩm** (`Ctrl+4`) → `Ctrl+E` Xuất Excel | File mở được, đủ 195 SKU, có tiêu đề |
| ☐ | Bấm **File mẫu** | Ra file Excel mẫu có dòng ví dụ |
| ☐ | Điền 2 dòng vào file mẫu: 1 dòng đúng, 1 dòng sai (ví dụ để trống tên hoặc giá là chữ). Lưu file. `Ctrl+I` Nhập Excel, chọn file | Hiện **xem trước**: 1 dòng hợp lệ, 1 dòng lỗi kèm lý do. Bấm Lưu → chỉ dòng hợp lệ được thêm |
| ☐ | **Nhập hàng** (`Ctrl+5`) → Lập phiếu nhập → tải file mẫu chi tiết → điền `SP001`, số lượng `20`, giá `120000` và 1 dòng mã sai → **Đọc từ Excel** | Xem trước báo 1 lỗi. Lưu → tồn kho của SP001 tăng 20 |
| ☐ | Thử thêm: **Khách hàng**, **Nhà cung cấp** (xuất / nhập / file mẫu); **Hóa đơn**, **Nhân viên**, **Báo cáo thống kê** (xuất) | Đều ra file đúng |

---

## Yêu cầu 8. Xuất PDF hóa đơn, phiếu, báo cáo

File lưu ở `Tài liệu\QLBanThoiTrang` và **tự mở** để in.

| ☐ | Màn hình | Làm | Đạt khi |
| --- | --- | --- | --- |
| ☐ | Hóa đơn (`Ctrl+3`) | Chọn `HD00002`, `Ctrl+P` | PDF "HÓA ĐƠN BÁN HÀNG" có thông tin cửa hàng, khách, các dòng SP, tổng tiền |
| ☐ | Đơn đặt hàng (`Ctrl+2`) | Chọn 1 đơn, `Ctrl+P` | PDF "ĐƠN ĐẶT HÀNG" |
| ☐ | Nhập hàng (`Ctrl+5`) | Chọn 1 phiếu, **In phiếu (PDF)** | PDF "PHIẾU NHẬP KHO" |
| ☐ | Đổi trả | Chọn 1 phiếu, `Ctrl+P` | PDF "PHIẾU ĐỔI HÀNG" / "PHIẾU TRẢ HÀNG" |
| ☐ | Báo cáo thống kê | Xem 1 báo cáo, `Ctrl+P` | PDF báo cáo có bảng + dòng Tổng cộng, in được |

---

## Yêu cầu 9. Hỗ trợ bàn phím

Làm toàn bộ phần này **không dùng chuột**.

| ☐ | Phím | Đạt khi |
| --- | --- | --- |
| ☐ | `F1` (hoặc `?`) | Mở bảng phím tắt, tự liệt kê phím của màn hình đang mở |
| ☐ | `Ctrl+K`, gõ `khuyen mai` (không dấu), `Enter` | Mở màn Khuyến mãi |
| ☐ | `Ctrl+1` … `Ctrl+9`, `Ctrl+0`, `Ctrl+L` | Mở đúng các màn: Tổng quan, Đơn hàng, Hóa đơn, Sản phẩm, Nhập hàng, Khách hàng, Nhân viên, Khuyến mãi, Thống kê, Tìm kiếm nâng cao, Nhật ký SQL |
| ☐ | `Ctrl+F` / `/`, gõ, `Esc` | Vào ô tìm, `Esc` xóa ô tìm |
| ☐ | `↑ ↓ PgUp PgDn Home End` trong bảng | Chọn dòng, chi tiết bên phải đổi theo |
| ☐ | `Ctrl+N`, `F2` / `Enter`, `Delete` | Thêm mới, sửa, xóa (luôn hỏi lại) |
| ☐ | Trong hộp thoại: `Ctrl+S` / `Ctrl+Enter`, `Esc` | Lưu, đóng |
| ☐ | **Lập hóa đơn tại quầy** (ở Hóa đơn, `Ctrl+N`): `F4` gõ SĐT khách, `F2` gõ mã SP + `Enter`, `F6` gõ voucher `WELCOME10`, `F7` chọn thanh toán, `F9` lưu | Lập được hóa đơn hoàn toàn bằng bàn phím, tiền giảm và tiền thối hiện ngay |
| ☐ | Đơn đặt hàng: chọn đơn `DH00069`, `Alt+X` | Xác nhận đơn (tồn kho giảm, có hóa đơn mới). Các phím khác: `Alt+T` từ chối, `Alt+G` giao, `Alt+H` hoàn tất, `Alt+U` hủy |
| ☐ | Nhìn các nút trên màn hình | Mỗi nút đều ghi phím tắt ngay trên nút |

---

## Khuyến mãi: theo sản phẩm, hóa đơn, voucher

Mở **Khuyến mãi** (`Ctrl+8`). Dữ liệu mẫu:

| Mã | Tên | Hình thức |
| --- | --- | --- |
| KM01 | Giảm giá mùa hè 2025 | Giảm theo sản phẩm (36 SKU), đã kết thúc |
| KM02 | Hóa đơn từ 1 triệu giảm 5% | Giảm theo hóa đơn, đang chạy |
| KM03 | Voucher khách hàng mới | Mã voucher: `WELCOME10` (10%), `FREESHIP5` (5%), đang chạy |
| KM04 | Black Friday 2025 | Giảm theo sản phẩm (24 SKU), đã kết thúc |
| KM05 | Ưu đãi giới hạn thu đông 2026 | Giảm theo sản phẩm (49 SKU), đang chạy |
| KM06 | Ưu đãi thành viên mới | Thành viên mới (loại thứ 4, thêm) |

| ☐ | Làm | Đạt khi |
| --- | --- | --- |
| ☐ | Lọc "Hình thức" lần lượt: Giảm theo sản phẩm / Giảm theo hóa đơn / Mã voucher | Mỗi lần chỉ còn chương trình đúng loại. Chọn 1 dòng thì chi tiết hiện bên phải |
| ☐ | **Theo sản phẩm**: `Ctrl+N`, hình thức "Giảm theo sản phẩm", chọn vài SKU, giảm 15%, từ hôm nay đến cuối tháng, `Ctrl+S` | Lưu được. Vào **Sản phẩm** thấy cột giá KM hôm nay của các SKU đó đã giảm |
| ☐ | **Theo hóa đơn**: kiểm tra KM02 (mức 1.000.000đ, 5%). Lập hóa đơn tại quầy có tổng ≥ 1 triệu | Hóa đơn tự giảm 5% |
| ☐ | **Voucher**: `Ctrl+N`, hình thức "Mã voucher", mã `THU10`, 10%, `Ctrl+S`. Lập hóa đơn tại quầy, `F6` gõ `THU10` | Tiền giảm hiện ngay |
| ☐ | Hóa đơn vừa thỏa KM02 vừa có voucher | Hệ thống chọn mức giảm **có lợi nhất** cho khách |
| ☐ | Chọn 1 KM đang chạy, **Kết thúc ngay** (`Delete`) | Hỏi lại, xác nhận thì tình trạng thành "Đã kết thúc" |

---

## Tóm tắt kết quả

| Yêu cầu | Kết quả test |
| --- | --- |
| 1. Dòng code chạy SQL | ☐ |
| 2. Tìm kiếm tái sử dụng / truy vấn mới | ☐ |
| 3. Thống kê 2 cách | ☐ |
| 4. Main-sub | ☐ |
| 5. 5 báo cáo + giao diện gọn, top 10, biểu đồ | ☐ |
| 6. Tìm nâng cao, hợp / giao | ☐ |
| 7. Excel | ☐ |
| 8. PDF | ☐ |
| 9. Bàn phím | ☐ |
| Khuyến mãi 3 loại | ☐ |
