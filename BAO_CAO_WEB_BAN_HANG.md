# Báo cáo tiến độ: Web bán hàng (giao diện khách hàng)

Đồ án **QL_BanThoiTrang** (cửa hàng thời trang UNIQLO). Giai đoạn 1 (web bán hàng cho khách) đã xong. Giai đoạn tiếp theo là web staff (nhân viên), sau đó là app desktop admin.

> **Cập nhật 04/11/2026 (sau buổi chấm):** theo yêu cầu của giáo viên, khách **không cần đăng ký vẫn mua được hàng**. Đã thêm xác thực số điện thoại bằng OTP, chống đơn ảo, tra cứu đơn, và ưu đãi cho khách tạo tài khoản. Chi tiết ở **mục 2.6**; hủy đơn và hoàn tiền ở **mục 2.7**.

## 1. Tổng quan những gì đã có

| Phần | Công nghệ | Trạng thái |
|---|---|---|
| Cơ sở dữ liệu | SQL Server, CSDL `QL_BanThoiTrang_v3` (23 bảng) + dữ liệu mẫu | Xong |
| Backend dùng chung | .NET 8, kiến trúc 3 lớp DTO / DAL (Dapper) / BLL + WebApi (JWT, Swagger), khoảng 120 API | Xong, dùng chung cho cả 3 giao diện |
| Web bán hàng (khách) | React 18 + TypeScript + Vite, React Query, React Router | Xong, có mua không cần tài khoản |
| Kiểm thử | 56 test ràng buộc CSDL (SQL), 60 test tích hợp backend (xUnit), 116 test API bằng Playwright, 34 test E2E giao diện trên máy tính và điện thoại. Tất cả đều đạt | Xong |
| Web staff (nhân viên) | | Chưa làm (giai đoạn sau) |
| App desktop admin (WinForms) | | Chưa làm (giai đoạn sau) |

Kiến trúc:

```
Web bán hàng (React) ──HTTP/JSON──> WebApi ──> BLL (quy tắc nghiệp vụ) ──> DAL (câu SQL) ──> SQL Server
Web staff (sắp làm)  ──────────────^          ^
App admin WinForms (sắp làm) ─────────────────┘ (gọi thẳng BLL)
```

## 2. Chức năng web bán hàng

### 2.1 Xem và tìm sản phẩm
- **Trang chủ:** banner, mua theo danh mục, sản phẩm đang giảm giá, sản phẩm mới, dòng thông báo khuyến mãi đang chạy.
- **Danh sách sản phẩm theo danh mục** (Áo thun, Áo sơ mi, Áo khoác, Quần jean, Quần kaki, Váy đầm), có menu lớn (mega menu) trên máy tính và menu trượt trên điện thoại.
- **Bộ lọc:** theo màu, kích cỡ, khoảng giá. **Sắp xếp:** mới nhất, giá tăng / giảm, tên A–Z / Z–A.
- **Tìm kiếm theo tên** (ô tìm trên header). Phải nhập ít nhất 2 ký tự, ô tìm luôn giữ đúng từ khóa đang tìm.
- **Trang chi tiết sản phẩm:**
  - Ảnh đổi theo màu, chọn màu / size, size hết hàng bị khóa, hiện số tồn kho.
  - Giá khuyến mãi.
  - Nút "Thêm vào giỏ" và "Mua ngay".
  - Sản phẩm liên quan và sản phẩm đã xem gần đây.
- **Trang hướng dẫn chọn size, chính sách đổi trả.**

### 2.2 Giỏ hàng
- Khách **chưa đăng nhập** vẫn thêm được vào giỏ (lưu ở trình duyệt). **Đăng nhập xong thì giỏ tự gộp** vào giỏ trên server.
- Khách chưa đăng nhập có 2 nút: **Mua không cần tài khoản** hoặc **Đăng nhập để thanh toán**.
- Tăng / giảm số lượng (không vượt tồn kho), xóa sản phẩm.
- **Đổi màu / size ngay trong giỏ** (nút "Đổi").
- Tạm tính, thông báo nếu sản phẩm hết hàng hoặc ngừng bán.

### 2.3 Thanh toán và đặt hàng
- Điền sẵn người nhận, số điện thoại, địa chỉ từ hồ sơ khách (sửa được).
- Ba phương thức thanh toán: **COD, chuyển khoản, online**.
- **Mã giảm giá (voucher):** server kiểm tra mã còn hợp lệ, web hiện số tiền được giảm. Nhấn Enter trong ô mã chỉ áp mã, không đặt hàng.
- Chống bấm đặt hàng 2 lần. Đặt xong chuyển sang trang xác nhận đơn.
- Đơn mới có trạng thái **Chờ xác nhận**. Kho chỉ bị trừ khi nhân viên xác nhận (phần nhân viên làm ở web staff).

### 2.4 Tài khoản khách hàng
- **Đăng ký, đăng nhập** (JWT). Chỉ tài khoản khách hàng mới đăng nhập được vào web này. Từ v3, đăng ký **bắt buộc xác thực số điện thoại bằng OTP**.
- **Hồ sơ:** sửa thông tin, xem **hạng thành viên và điểm tích lũy** (cứ 10.000đ trên hóa đơn đã thanh toán thì được 1 điểm).
- **Đổi mật khẩu.**
- **Đơn hàng:** danh sách, lọc theo trạng thái, xem chi tiết, theo dõi tiến trình (Chờ xác nhận → Đã xác nhận → Đang giao → Hoàn tất), **hủy đơn** khi còn chờ xác nhận, **tải PDF đơn hàng**.
- **Hóa đơn:** danh sách và chi tiết (dạng main-sub), **tải PDF hóa đơn**.
- **Đổi / trả hàng:**
  - Gửi yêu cầu từ hóa đơn đã thanh toán **trong vòng 30 ngày**.
  - Chọn đổi size / màu hoặc trả hàng hoàn tiền.
  - Không cho yêu cầu vượt số lượng đã mua.
  - Theo dõi trạng thái (Chờ duyệt / Đã duyệt / Từ chối / Hoàn tất) và tải PDF phiếu.

### 2.5 Trải nghiệm và giao diện
- **Responsive:** dùng tốt trên máy tính, máy tính bảng và điện thoại.
- Có trạng thái đang tải (skeleton), trang trống, báo lỗi rõ ràng bằng tiếng Việt, và trang 404.
- Hỗ trợ trình đọc màn hình (nhãn cho nút và ô nhập, link "Bỏ qua tới nội dung").

### 2.6 Mua không cần tài khoản (v3, theo yêu cầu giáo viên 04/11/2026)

**Luồng của khách:**
1. Thêm hàng vào giỏ, bấm **Mua không cần tài khoản**.
2. Nhập số điện thoại, bấm **Gửi mã OTP**, nhập mã 6 số. Hiện là **OTP giả lập** (không tốn tiền SMS): mã hiện ngay trên màn hình và in ở console server. Khi cần chạy thật chỉ phải viết thêm 1 lớp gửi SMS / email (`IGuiOtp`), không sửa chỗ khác.
3. Nhập họ tên, email (không bắt buộc), địa chỉ, chọn cách thanh toán, đặt hàng. Lần sau dùng lại số đó thì tên và địa chỉ được điền sẵn.
4. Trang **Đặt hàng thành công** có ô **Tạo tài khoản trong 10 giây**: số điện thoại đã xác thực nên chỉ cần tên đăng nhập và mật khẩu. Đơn vừa đặt và mọi đơn cũ cùng số tự gắn vào tài khoản.
5. Trang **Tra cứu đơn hàng** (`/tra-cuu-don`, có ở chân trang và menu điện thoại): nhập số điện thoại + OTP để xem đơn, tải PDF, hủy đơn còn chờ xác nhận.

**Xác thực và chống đơn ảo:**

| Quy tắc | Giá trị | Lý do |
|---|---|---|
| Mã OTP | 6 số, hết hạn sau 5 phút, sai 5 lần phải lấy mã mới; CSDL chỉ lưu bản băm SHA-256 | Không đoán được mã, lộ CSDL cũng không lộ mã |
| Gửi mã | 60 giây mới gửi lại, tối đa 5 mã / số / ngày, giới hạn số lần theo địa chỉ IP | Chống spam tin nhắn |
| Phiên sau khi xác thực | 30 phút, chỉ dùng được cho các API khách vãng lai | Token này không mở được API của khách có tài khoản hay nhân viên |
| Số lượng | Tối đa 5 cái mỗi sản phẩm, tối đa 2 đơn đang chờ xác nhận mỗi số | Chặn đặt hàng loạt để giữ hàng |
| COD | Đơn trả khi nhận hàng tối đa 5.000.000đ; số đã "bom hàng" 2 lần (đơn bị hủy sau khi đã lập hóa đơn) không được chọn COD | Giảm thiệt hại giao hàng không ai nhận |
| Tồn kho | Vẫn chỉ trừ khi nhân viên xác nhận đơn | Đơn ảo không giữ được hàng |
| 1 số điện thoại = 1 khách | Khóa UNIQUE trong CSDL | Gom đơn theo số, gắn vào tài khoản khi đăng ký |
| Đăng ký | Bắt buộc OTP của đúng số khai báo | Trước đây ai cũng đăng ký được bằng số của người khác rồi xem đơn của họ; nay đã chặn |

Phần cho nhân viên (API đã có, giao diện làm ở web staff): lọc đơn theo nguồn `VANG_LAI`, xem **mức rủi ro** của số điện thoại (số đơn, số lần bom hàng, có được COD không: THẤP / TRUNG BÌNH / CAO) trước khi xác nhận.

**Ưu đãi thành viên mới (khuyến khích khách tạo tài khoản):**
- Khuyến mãi hình thức mới `THANH_VIEN_MOI` (KM06): **giảm 10% cho 3 đơn đầu đặt bằng tài khoản, tối đa 100.000đ mỗi đơn**. Admin đổi được %, số đơn, mức tối đa.
- Không cộng dồn: hệ thống tự chọn mức có lợi nhất giữa ưu đãi này, khuyến mãi hóa đơn và voucher. Nếu khách nhập voucher kém hơn, web báo "ưu đãi thành viên mới đang giảm nhiều hơn".
- Đơn bị hủy / từ chối thì trả lại lượt. Đơn mua khi chưa có tài khoản không tính vào 3 lượt.
- Web hiện quảng cáo ưu đãi ở trang thanh toán, trang đặt hàng thành công, trang đăng ký, và hiện số lượt còn lại khi khách đã đăng nhập.

**CSDL v3** (thêm 3 bảng tính cả `HOAN_TIEN` ở mục 2.7, không xóa dữ liệu cũ): bảng `MA_OTP`, bảng `CT_KM_THANH_VIEN_MOI`, cột `KHACH_HANG.sdt_da_xac_thuc`, cột `DON_DAT_HANG.nguon / email_nhan / ma_km`. CSDL v2 đang có dữ liệu chỉ cần chạy thêm `QL_BanThoiTrang_BoSung_v3.sql`.

### 2.7 Hủy đơn và hoàn tiền (v3)

Dùng chung cho khách có tài khoản và khách vãng lai. Khách luôn phải chọn **lý do hủy** (có sẵn danh sách lý do, hoặc tự nhập).

| Đơn đang ở | Khách bấm | Kết quả |
|---|---|---|
| Chờ xác nhận | **Hủy đơn hàng** | Hủy ngay. Kho chưa bị trừ, chưa có hóa đơn nên không phải hoàn gì. Lượt ưu đãi thành viên mới được trả lại |
| Đã xác nhận (chưa giao) | **Yêu cầu hủy đơn** | Gửi yêu cầu cho cửa hàng, mỗi đơn 1 lần. Trong lúc chờ, nhân viên không giao được đơn. **Đồng ý:** đơn hủy, hàng về kho, hóa đơn hủy, điểm tính lại; nếu đã chuyển khoản / online thì tự tạo **phiếu hoàn tiền** chờ thu ngân hoàn; không tính là bom hàng. **Từ chối:** bắt buộc ghi lý do (vd hàng đã đóng gói), khách thấy lý do, đơn giao bình thường |
| Đang giao | (không có nút) | Hiện hotline. Khách từ chối nhận thì nhân viên hủy, tính là bom hàng |

CSDL lưu đủ dấu vết: lý do, thời điểm gửi yêu cầu, phản hồi của cửa hàng, thời điểm hủy, ai hủy (`KHACH` / `NHAN_VIEN`), nhân viên xử lý; bảng `HOAN_TIEN` theo dõi tiền đã hoàn hay chưa. Khách xem được trạng thái yêu cầu và hoàn tiền ngay trên trang đơn hàng / tra cứu đơn. Màn hình duyệt yêu cầu hủy và danh sách hoàn tiền cho nhân viên (API đã có) sẽ làm ở web staff.

## 3. Đáp ứng 9 yêu cầu của đồ án

| Yêu cầu | Đã làm ở đâu |
|---|---|
| 1. Hiện dòng code chạy truy vấn | Mọi câu SQL đi qua `DAL/DbHelper.cs`, có ghi file, dòng code và tên hàm. Xem ở API `GET /api/nhat-ky-sql`, header `X-Sql-Vi-Tri`, hoặc console WebApi |
| 2. Tìm kiếm: tái sử dụng / truy vấn mới | `TimKiemBLL`: lọc trên danh sách đã tải (0 câu SQL) hoặc chạy câu SQL mới. Trên web: bộ lọc màu / size / giá lọc tại chỗ, còn tìm theo từ khóa thì gọi server |
| 3. Thống kê 2 cách | `ThongKeBLL` có 2 cách: dùng lại dữ liệu đã tải (LINQ) hoặc GROUP BY bằng SQL, có so sánh thời gian và số câu SQL. Phần giao diện sẽ làm ở web staff / app admin |
| 4. Main-sub | Web khách: danh sách hóa đơn → chi tiết hóa đơn, danh sách đơn → chi tiết đơn. API: `GET /api/hoa-don` + `/{ma}/chi-tiet` |
| 5a–5e. Báo cáo thống kê | API đã có (tổng tiền, thu chi, nhân viên, khách hàng, sản phẩm, xuất Excel / PDF). Phần giao diện để cho web staff / app admin |
| 6. Tìm kiếm nâng cao (hợp / giao) | API `POST /api/tim-kiem/...` hỗ trợ `>, <, =, <>, LIKE, IN`, có chọn AND (giao) hoặc OR (hợp), thêm các truy vấn UNION / INTERSECT. Phần giao diện để cho web staff |
| 7. Excel | `ExcelBLL`: xuất danh sách, tải file mẫu, nhập file (xem trước + báo lỗi từng dòng) |
| 8. PDF | Web khách tải được PDF đơn hàng, hóa đơn, phiếu đổi trả (font tiếng Việt nhúng sẵn) |
| 9. Bàn phím | Web khách có phím tắt, bấm `?` để xem bảng: `/` tìm kiếm, `g`+`h/s/c/t/d` chuyển trang, `←` `→` đổi ảnh, `a` thêm giỏ, `f` mở bộ lọc, `Enter` áp mã giảm giá, `Ctrl+Enter` đặt hàng, `Esc` đóng hộp thoại. Mọi thao tác đều dùng được bằng `Tab` / `Enter` / `Space` |

## 4. Cách chạy để demo

1. Tạo CSDL: chạy `database/QL_BanThoiTrang_v3.sql`, rồi `database/QL_BanThoiTrang_DuLieuMau.sql`.
2. Chạy backend: `dotnet run --project backend/WebApi` (cổng 5212, Swagger: http://localhost:5212/swagger).
3. Chạy web: trong thư mục `web-ban-hang` chạy `npm install` rồi `npm run dev`, mở http://localhost:5173.
4. Tài khoản khách mẫu: `khach01` / `123456`. Khách vãng lai mẫu để tra cứu đơn: số `0987000021` (có đơn hoàn tất và đơn chờ xác nhận), `0987000022` (đã bom hàng 2 lần, không được COD).
   Demo mua không cần tài khoản: không đăng nhập, thêm hàng vào giỏ, bấm "Mua không cần tài khoản", nhập một số di động bất kỳ (10 số, đầu 03/05/07/08/09), mã OTP hiện trên màn hình.
5. Chạy test giao diện: `npm run test:e2e`.

## 5. Những điểm cần hỏi lại giáo viên

1. **Đặc tả và CSDL đang khác nhau.** File đặc tả `DacTa_ChucNang_Uniqlo_v3.docx` mô tả CSDL 14 bảng (UniqloDB), còn nhóm đang dùng CSDL `QL_BanThoiTrang_v3` 23 bảng (có thêm giỏ hàng, phiếu nhập, đổi trả, khuyến mãi...). Hỏi: nhóm **giữ CSDL 23 bảng và cập nhật lại đặc tả** có được không, hay phải làm đúng 14 bảng?
2. **OTP.** ~~OTP có bắt buộc không?~~ Đã làm theo yêu cầu khách vãng lai: OTP xác thực số điện thoại khi mua không cần tài khoản, khi tra cứu đơn và khi đăng ký. Đăng nhập vẫn bằng tên đăng nhập + mật khẩu. Hỏi: **OTP giả lập** (mã hiện trên màn hình) có được chấp nhận không, hay cần gửi email / SMS thật? Các con số chống đơn ảo ở mục 2.6 (5 cái / sản phẩm, 2 đơn chờ, COD tối đa 5 triệu, ưu đãi 10% cho 3 đơn đầu) có cần chỉnh không?
3. **Thanh toán online.** Hiện "Chuyển khoản" và "Online" được coi là đã thanh toán, chưa nối cổng thanh toán thật (VNPay / MoMo...), và chưa có trạng thái "Chờ thanh toán" như đặc tả. Hỏi: có cần tích hợp cổng thanh toán thật (sandbox) không?
4. **Thời điểm trừ tồn kho.** Hiện kho bị trừ khi nhân viên xác nhận đơn, không phải lúc khách đặt. Hỏi: cách này có phù hợp không?
5. **Quy tắc nghiệp vụ tự đặt.** Nhóm tự chọn các con số: đổi trả trong 30 ngày, 10.000đ = 1 điểm, cách xếp hạng thành viên, khách được hủy đơn khi còn "Chờ xác nhận". Hỏi: giáo viên có yêu cầu con số / quy định cụ thể nào không?
6. **Yêu cầu 9 (bàn phím).** Hỏi: bộ phím tắt + điều hướng bằng Tab như mục 3 đã đủ chưa, hay cần thêm (ví dụ phím tắt cho từng nút trên web staff / app admin)?
7. **Phân chia yêu cầu theo giao diện.** Các yêu cầu 3, 5, 6, 7 (thống kê, báo cáo, tìm kiếm nâng cao, Excel) nhóm định làm phần giao diện ở web staff và app admin, web khách chỉ dùng phần phù hợp (tìm kiếm, PDF, main-sub). Hỏi: mỗi giao diện có phải thể hiện đủ cả 9 yêu cầu không?
8. **Hình ảnh và tên thương hiệu.** Web đang dùng tên "UNIQLO" và ảnh sản phẩm UNIQLO cho đồ án. Hỏi: dùng như vậy cho bài nộp có được không, hay cần đổi sang tên / ảnh tự tạo?
9. **Hình thức nộp và chấm.** Hỏi: cần nộp mã nguồn, file CSDL, báo cáo Word, video demo hay chạy demo trực tiếp? Có chấm phần test tự động không?

## 6. Kế hoạch tiếp theo

- Làm **web staff** cho nhân viên: xác nhận / giao đơn, lập hóa đơn tại quầy, duyệt đổi trả, nhập hàng, quản lý sản phẩm / khách hàng, các màn hình thống kê và báo cáo (yêu cầu 3, 5, 6, 7).
- Làm **app desktop admin** (WinForms) gọi thẳng BLL: quản lý nhân viên, tài khoản, cấu hình.
- Sửa đặc tả hoặc CSDL theo ý kiến giáo viên ở mục 5.
