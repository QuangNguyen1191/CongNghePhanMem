# Hướng dẫn chạy hệ thống QL Bán Thời Trang (UNIQLO)

Hệ thống gồm 4 phần, dùng chung 1 CSDL SQL Server `QL_BanThoiTrang`:

| Phần | Thư mục | Công nghệ | Địa chỉ |
| --- | --- | --- | --- |
| WebApi (backend) | `backend\WebApi` | ASP.NET Core .NET 8 | http://localhost:5212 (Swagger: http://localhost:5212/swagger) |
| Web bán hàng (khách) | `web-ban-hang` | React + Vite | http://localhost:5173 |
| Web staff (nhân viên) | `web-staff` | React + Vite + Ant Design | http://localhost:5174 |
| App desktop (quản lý) | `backend\AppAdmin` | WinForms .NET 8 | Cửa sổ Windows, gọi thẳng BLL, **không cần WebApi** |

Hai web cần WebApi đang chạy. App desktop chỉ cần CSDL.

---

## 0. Cài đặt cần có (1 lần)

- **SQL Server** 2017 trở lên (Express hoặc Developer), có `sqlcmd`. Đăng nhập bằng Windows Authentication.
- **.NET 8 SDK** (chạy WebApi và App Admin).
- **Node.js 18** trở lên (chạy 2 web).
- Tùy chọn: Visual Studio 2022 với workload **.NET desktop development** nếu muốn mở bằng VS.

Kiểm tra nhanh:

```powershell
dotnet --version
node --version
sqlcmd -?
```

---

## Cách nhanh nhất: dùng thư mục `demo` (bấm đúp)

| Bước | File | Làm gì |
| --- | --- | --- |
| 1 | `demo\1_CAI_DAT_CSDL.bat` | Tự tìm SQL Server (`localhost` hoặc `.\SQLEXPRESS`), tạo CSDL + dữ liệu mẫu. Đã có CSDL thì giữ nguyên. Chỉ cần chạy 1 lần |
| 2 | `demo\2_CHAY_DEMO.bat` | Mở cùng lúc WebApi, web bán hàng, web staff và App Admin, rồi tự mở trình duyệt. Lần đầu tự `npm install`, mất vài phút |
| 3 | `demo\3_DUNG_DEMO.bat` | Tắt WebApi và 2 web |
| 4 | `demo\4_LAM_MOI_DU_LIEU.bat` | Đưa CSDL về dữ liệu mẫu sạch (sao lưu `.bak` trước, phải gõ `LAMMOI` để xác nhận) |

Chỉ muốn chạy WebApi + 2 web (không mở App Admin): bấm đúp `CHAY_WEB.bat` ở thư mục gốc.

Nếu SQL Server của bạn có tên instance khác `localhost` / `SQLEXPRESS`, sửa file `demo\may_chu.txt` (ví dụ `.\TEN_INSTANCE`).

Các phần dưới đây là cách chạy **thủ công từng phần**.

---

## 1. Tạo CSDL

Mở PowerShell tại thư mục `CNPM\database`:

```powershell
sqlcmd -S localhost -E -I -i QL_BanThoiTrang_v3.sql
```

```powershell
sqlcmd -S localhost -E -I -i QL_BanThoiTrang_DuLieuMau.sql
```

- Dùng SQL Express thì thay `localhost` bằng `.\SQLEXPRESS`.
- Cờ `-I` là bắt buộc (bật QUOTED_IDENTIFIER cho filtered index).
- Hoặc mở 2 file trên bằng SSMS và bấm Execute theo đúng thứ tự.
- Tùy chọn: `KiemThu_QL_BanThoiTrang.sql` chạy 56 test kiểm tra CSDL, không làm đổi dữ liệu.
- Muốn làm lại từ đầu: `DROP DATABASE QL_BanThoiTrang;` rồi chạy lại 2 lệnh trên.

**Chuỗi kết nối** (mặc định `Server=localhost;Database=QL_BanThoiTrang;Trusted_Connection=True;TrustServerCertificate=True`) nằm ở:

- `backend\WebApi\appsettings.json` (cho WebApi, tức là cho 2 web)
- `backend\AppAdmin\appsettings.json` (cho App Admin, sửa được ngay trong app bằng F8 ở màn hình đăng nhập)

Dùng SQL Express thì đổi `Server=localhost` thành `Server=localhost\\SQLEXPRESS` trong file JSON.

---

## 2. Bật WebApi (bắt buộc cho 2 web)

```powershell
cd backend\WebApi
```

```powershell
dotnet run --launch-profile http
```

Đợi dòng `Now listening on: http://localhost:5212`. Giữ cửa sổ này mở.
Thử API: mở http://localhost:5212/swagger.

---

## 3. Bật web bán hàng (cổng 5173)

Mở cửa sổ PowerShell mới tại thư mục `CNPM`:

```powershell
cd web-ban-hang
```

```powershell
npm install
```

```powershell
npm run dev
```

- `npm install` chỉ cần chạy lần đầu.
- Mặc định web gọi API ở `http://localhost:5212`. Muốn đổi thì chép `.env.example` thành `.env` rồi sửa `VITE_API_URL`.
- Mở http://localhost:5173.

---

## 4. Bật web staff (cổng 5174)

Mở cửa sổ PowerShell mới tại thư mục `CNPM`:

```powershell
cd web-staff
```

```powershell
npm install
```

```powershell
copy .env.example .env
```

```powershell
npm run dev
```

- File `.env` gồm `VITE_API_URL=http://localhost:5212` và `VITE_ANH_URL=http://localhost:5173`.
- Ảnh sản phẩm lấy từ web bán hàng, nên chạy cả web bán hàng thì mới thấy ảnh (không chạy vẫn dùng được, chỉ thiếu ảnh).
- Mở http://localhost:5174.

---

## 5. Chạy app desktop App Admin

**Cách 1, dòng lệnh** (tại thư mục `CNPM`):

```powershell
dotnet run --project backend\AppAdmin
```

**Cách 2, Visual Studio 2022:** mở `backend\QLBanThoiTrang.sln`, chuột phải project `AppAdmin` → *Set as Startup Project* → F5.

- Chỉ tài khoản vai trò **ADMIN** đăng nhập được: `quanly` hoặc `giamdoc`, mật khẩu `123456`.
- Không kết nối được CSDL: ở màn hình đăng nhập bấm **Cấu hình kết nối CSDL (F8)** → Kiểm tra kết nối → Lưu.
- File Excel / PDF xuất ra lưu ở `Documents\QLBanThoiTrang` (đổi bằng `App:ThuMucXuat` trong `appsettings.json`).

---

## 6. Tài khoản mẫu (mật khẩu đều là `123456`)

| Nơi dùng | Tài khoản | Ghi chú |
| --- | --- | --- |
| App Admin | `giamdoc`, `quanly` | ADMIN |
| Web staff | `thungan1` | Thu ngân: bán tại quầy, hóa đơn, đổi trả, hoàn tiền, khách hàng |
| Web staff | `banhangonline` | Xử lý đơn online |
| Web staff | `thukho` | Nhập hàng, sản phẩm và tồn kho |
| Web staff | `quanly` | Thấy tất cả |
| Web bán hàng | `khach01` … `khach14` | `khach15` đang bị khóa |
| Web bán hàng, khách vãng lai | SĐT `0987000021` ở trang `/tra-cuu-don` | SĐT `0987000022` là khách "bom hàng" 2 lần |

Voucher có sẵn: `WELCOME10`. Khuyến mãi đang chạy: KM02 (hóa đơn từ 1 triệu giảm 5%), KM05 (giảm giá 5 sản phẩm), KM06 (ưu đãi thành viên mới).

---

## 7. Sự cố thường gặp

| Hiện tượng | Cách xử lý |
| --- | --- |
| WebApi hoặc App Admin báo lỗi kết nối CSDL | Bật dịch vụ "SQL Server (...)" trong `services.msc`; kiểm tra tên server trong chuỗi kết nối (localhost hay `localhost\SQLEXPRESS`) |
| Báo cổng 5212 / 5173 / 5174 đang bận | Đang có bản cũ chạy: tắt cửa sổ cũ hoặc chạy `demo\3_DUNG_DEMO.bat` |
| Web mở được nhưng đăng nhập báo lỗi mạng | WebApi chưa chạy hoặc chưa sẵn sàng |
| App Admin không đăng nhập được bằng `thungan1` | Đúng thiết kế: chỉ ADMIN vào được App Admin |
| Web staff thiếu ảnh sản phẩm | Chạy thêm web bán hàng (cổng 5173) |

---

## 8. Danh sách chức năng của App Admin

Đọc từ code `backend\AppAdmin` (menu trong `FormChinh.cs`, màn hình trong `CacManHinh\`). App có **16 màn hình** chia 6 nhóm, cộng màn hình đăng nhập.

### Chức năng chung toàn app

- **Đăng nhập** (chỉ ADMIN), nhớ tên đăng nhập cuối, **cấu hình kết nối CSDL** ngay trong app (F8: kiểm tra, lưu).
- **Đổi mật khẩu**, đăng xuất.
- **Thanh trạng thái SQL**: sau mỗi thao tác hiện tên thao tác, số câu SQL, dòng code chạy câu cuối (ví dụ `HoaDonDAL.cs:42 LayDanhSach()`), thời gian ms, tổng số câu SQL của phiên. Bấm vào để mở Nhật ký SQL.
- **Mở nhanh màn hình** (`Ctrl+K`, gõ tên không dấu cũng được).
- **Bảng phím tắt** (`F1` hoặc `?`), tự liệt kê nút của màn hình đang mở.
- **Thẻ xem nhanh**: rê chuột vào một dòng ở bất kỳ bảng nào để xem tóm tắt.
- **Bảng dữ liệu**: sắp xếp theo cột, giữ dòng đang chọn, `Ctrl+C` sao chép dòng; kiểu **main-sub** (chọn dòng là chi tiết hiện bên cạnh, không cần nút "Chi tiết").
- **Tìm kiếm 2 chế độ** (`F3` để đổi): tái sử dụng dữ liệu đã tải (0 câu SQL) hoặc truy vấn mới, có báo số dòng, số câu SQL, ms.

### Nhóm TỔNG QUAN

**1. Tổng quan** (`Ctrl+1`)
- Ô số liệu: đơn chờ xác nhận, khách xin hủy, đổi trả chờ duyệt, chờ hoàn tiền, sản phẩm tồn thấp, doanh thu hôm nay và tháng này, khuyến mãi đang chạy. Bấm vào ô để mở danh sách đã lọc sẵn.
- Biểu đồ cột thu, chi, lợi nhuận theo tháng.

### Nhóm BÁN HÀNG

**2. Đơn đặt hàng** (`Ctrl+2`)
- Lọc theo trạng thái, nguồn (có tài khoản hay khách vãng lai), khoảng ngày.
- Xác nhận (`Alt+X`, trừ kho và lập hóa đơn), từ chối (`Alt+T`), giao hàng (`Alt+G`), hoàn tất (`Alt+H`), hủy đơn kèm lý do (`Alt+U`, cảnh báo bom hàng).
- Duyệt (`Alt+D`) hoặc từ chối (`Alt+R`) yêu cầu xin hủy của khách.
- Hiện độ tin cậy của SĐT (số lần bom hàng, đơn hoàn tất), đánh dấu "Rủi ro cao".
- In đơn ra PDF.

**3. Hóa đơn** (`Ctrl+3`)
- Danh sách + chi tiết main-sub (chọn 1 dòng chỉ chạy 1 câu SQL), tìm 2 chế độ.
- **Lập hóa đơn tại quầy**: tìm khách theo SĐT, chọn sản phẩm theo mã, áp voucher, tính tiền khách đưa và tiền thối.
- Hủy hóa đơn tại quầy, lập phiếu đổi trả từ hóa đơn.
- In PDF, xuất Excel.

**4. Đổi trả**
- Duyệt, từ chối, nhận hàng và hoàn tất yêu cầu đổi trả từ web.
- Lập phiếu tại quầy: chọn dòng hóa đơn, đổi sang biến thể khác cùng mẫu (size, màu) hoặc trả hàng.
- In PDF.

**5. Hoàn tiền**
- Danh sách phiếu chờ hoàn (đơn đã thanh toán online bị hủy) kèm tổng tiền.
- Đánh dấu đã hoàn kèm ghi chú đối soát, mở đơn liên quan.

### Nhóm KHO HÀNG

**6. Sản phẩm & tồn kho** (`Ctrl+4`)
- Tìm 2 chế độ, lọc theo loại, trạng thái, tồn thấp.
- Thêm 1 SKU, hoặc thêm 1 mẫu với nhiều size × màu cùng lúc (`Ctrl+Shift+N`).
- Sửa, ngừng bán hoặc mở bán lại; xem giá khuyến mãi hôm nay và các biến thể cùng mẫu.
- Excel: xuất, nhập (có xem trước và báo lỗi từng dòng), tải file mẫu.

**7. Loại sản phẩm**
- Thêm, sửa tên, xóa (chặn xóa khi còn SKU), đếm số mẫu và số SKU của từng loại.

**8. Nhà cung cấp**
- Thêm, sửa; xóa hoặc chuyển sang "ngừng hợp tác" nếu nhà cung cấp đã có phiếu nhập.
- Xem phiếu nhập của từng nhà cung cấp; Excel xuất, nhập, file mẫu.

**9. Nhập hàng** (`Ctrl+5`)
- Lập phiếu nhập: chọn sản phẩm hoặc đọc từ file Excel (xem trước lỗi từng dòng); hỏi xác nhận kèm tổng tiền vì phiếu không sửa được sau khi lưu.
- Lọc theo ngày, nhà cung cấp; xem chi tiết phiếu.
- In phiếu PDF, xuất Excel.

### Nhóm CON NGƯỜI

**10. Khách hàng & giỏ hàng** (`Ctrl+6`)
- Tìm 2 chế độ (theo SĐT, mã, tên), lọc theo hạng, có hoặc không có tài khoản.
- Thêm khách tại quầy, sửa thông tin, chỉnh hạng và điểm.
- Các tab: Thông tin (kèm độ tin cậy SĐT), Đơn hàng, Hóa đơn, Giỏ hàng (xem và xóa giỏ).
- Excel xuất, nhập, file mẫu.

**11. Nhân viên** (`Ctrl+7`)
- Thêm nhân viên kèm tạo tài khoản, sửa, cho nghỉ việc (tự khóa tài khoản), đặt lại mật khẩu.
- Xem hóa đơn và doanh số của từng nhân viên; xuất Excel.
- Không cho tự cho mình nghỉ việc.

**12. Tài khoản**
- Lọc theo vai trò, trạng thái; khóa hoặc mở khóa tài khoản (không tự khóa tài khoản đang đăng nhập).
- Đặt lại mật khẩu, hiện mật khẩu tạm có nút sao chép.

### Nhóm KINH DOANH

**13. Khuyến mãi** (`Ctrl+8`)
- 4 hình thức: giảm theo sản phẩm, giảm theo hóa đơn, mã voucher, ưu đãi thành viên mới.
- Lọc theo hình thức và tình trạng (sắp diễn ra, đang chạy, đã kết thúc).
- Thêm chương trình, sửa (bảng chi tiết sửa trực tiếp), kết thúc ngay.

**14. Báo cáo thống kê** (`Ctrl+9`), chọn báo cáo bằng `Alt+1` … `Alt+5`
- **5a.** Tổng tiền từ ngày A đến ngày B (số hóa đơn đã thanh toán và tổng tiền).
- **5b.** Thu, chi, lợi nhuận theo tháng, quý, năm hoặc tất cả các năm (thu = hóa đơn đã thanh toán; chi = phiếu nhập + tiền hoàn trả).
- **5c.** Doanh thu theo nhân viên × năm.
- **5d.** Doanh thu theo khách hàng × quý (1 năm) hoặc × năm (tất cả năm).
- **5e.** Doanh thu theo sản phẩm × quý của 1 năm. **Chỉ giám đốc** (`giamdoc`) xem được; tài khoản ADMIN khác (`quanly`) bị chặn.
- Nút **Xem tất cả** luôn bấm được (mặc định hiện top 10).
- Bảng có dòng/cột tổng cộng + biểu đồ cột; xem top 10 hoặc xem tất cả.
- **2 cách tính** (`F3` đổi cách), **so sánh 2 cách** (`Ctrl+M`): bảng thời gian, số câu SQL và xác nhận kết quả giống nhau từng ô.
- Xuất Excel (`Ctrl+E`), xuất PDF (`Ctrl+P`).

**15. Tìm kiếm nâng cao** (`Ctrl+0`)
- Tìm trên hóa đơn, khách hàng hoặc sản phẩm, tối đa 10 điều kiện, nối bằng AND (giao) hoặc OR (hợp), toán tử so sánh như `>=`.
- 2 cách chạy (lọc trên dữ liệu đã tải hoặc truy vấn mới), có sẵn các ví dụ mẫu.
- 3 truy vấn hợp/giao viết sẵn: khách ở tỉnh A ∪ tỉnh B (UNION); khách ở A ∩ có đơn giao tới B (INTERSECT); khách vừa mua tại quầy ∩ vừa đặt online (INTERSECT).
- Xuất Excel kết quả.

### Nhóm HỆ THỐNG

**16. Nhật ký SQL** (`Ctrl+L`)
- Xem 100 đến 500 câu SQL gần nhất kèm vị trí file:dòng trong DAL và thời gian chạy.
- Lọc, gom theo vị trí (số lần, tổng ms), xem câu đầy đủ kèm tham số, sao chép câu SQL.
- Bật/tắt ghi nhật ký, xóa nhật ký, tự làm mới.

### Phím tắt chính

| Phím | Tác dụng |
| --- | --- |
| `Ctrl+1` … `Ctrl+9`, `Ctrl+0`, `Ctrl+L` | Mở các màn hình như ghi ở trên |
| `Ctrl+K` | Mở nhanh màn hình |
| `F1` / `?` | Bảng phím tắt |
| `F5` | Làm mới / xem báo cáo |
| `Ctrl+F` hoặc `/` | Vào ô tìm kiếm; `Esc` xóa ô tìm |
| `F3` | Đổi chế độ tìm / đổi cách tính thống kê |
| `Ctrl+N` | Thêm mới |
| `F2` / `Enter` | Sửa dòng đang chọn |
| `Delete` | Xóa / ngừng bán / khóa / hủy (luôn hỏi lại) |
| `Ctrl+E`, `Ctrl+I`, `Ctrl+P` | Xuất Excel, nhập Excel, in PDF |
| `Ctrl+S` / `Ctrl+Enter`, `Esc` | Lưu, đóng hộp thoại |
| Lập hóa đơn tại quầy | `F2` mã SP, `F4` SĐT, `F6` voucher, `F7`/`F9` thanh toán, `Insert` chọn SP |

### Đáp ứng 9 yêu cầu đồ án

| # | Yêu cầu | Ở đâu trong App Admin |
| --- | --- | --- |
| 1 | Hiện dòng code chạy truy vấn | Thanh trạng thái dưới cùng + màn hình Nhật ký SQL |
| 2 | Tìm kiếm tái sử dụng / truy vấn mới | `F3` ở Sản phẩm, Khách hàng, Hóa đơn |
| 3 | Thống kê 2 cách | Báo cáo thống kê, nút So sánh 2 cách |
| 4 | Main-sub | Hóa đơn, đơn hàng, phiếu nhập, NCC, nhân viên, khách hàng, khuyến mãi, sản phẩm |
| 5a–5e | 5 báo cáo | Báo cáo thống kê |
| 6 | Tìm nâng cao, hợp và giao | Tìm kiếm nâng cao (AND/OR, UNION/INTERSECT) |
| 7 | Đọc, ghi Excel | Sản phẩm, NCC, Khách hàng, Nhập hàng... (`Ctrl+E`, `Ctrl+I`, File mẫu) |
| 8 | Xuất PDF | Hóa đơn, đơn hàng, phiếu nhập, đổi trả, báo cáo (`Ctrl+P`) |
| 9 | Hỗ trợ bàn phím | Phím tắt ghi trên mọi nút, `Ctrl+K`, `F1` |

Kịch bản demo chi tiết theo từng yêu cầu: xem `demo\README.md`. Tài liệu gốc của app: `backend\AppAdmin\README.md`.
