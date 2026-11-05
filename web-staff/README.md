# Web staff – UNIQLO (QL_BanThoiTrang)

Giao diện cho nhân viên cửa hàng: bán tại quầy, xử lý đơn online, đổi trả, hoàn tiền, nhập hàng, tra cứu sản phẩm và khách hàng.
Phần danh mục, nhân viên, khuyến mãi, báo cáo thống kê vẫn nằm ở app admin (WinForms).

React 18 + Vite + TypeScript + Ant Design 5 + TanStack Query. Giao diện theo hướng UNIQLO, chọn qua skill ui-ux-pro-max (style "Data-Dense Dashboard"),
bảng màu thay bằng màu thương hiệu: menu đen, đỏ UNIQLO `#E50012` cho logo, mục đang chọn, số việc chờ và nút thanh toán; góc vuông, chữ đậm,
tiêu đề có vạch đỏ. Font Be Vietnam Pro (hỗ trợ tiếng Việt), số dùng tabular-nums. Màu và kiểu chung nằm ở `src/main.tsx` (theme) và `src/styles.css`.

## Cách chạy

1. Chạy CSDL và backend như hướng dẫn ở `BAO_CAO_WEB_BAN_HANG.md` (backend ở cổng 5212).
2. Trong thư mục `web-staff`:

   ```bash
   npm install
   copy .env.example .env      # Windows (macOS/Linux: cp)
   npm run dev
   ```

3. Mở http://localhost:5174.
4. Ảnh sản phẩm lấy từ `web-ban-hang/public/images`, nên chạy thêm `web-ban-hang` (cổng 5173) để thấy ảnh. Không chạy thì web staff vẫn dùng bình thường, chỉ thiếu ảnh.

`.env`:

```
VITE_API_URL=http://localhost:5212
VITE_ANH_URL=http://localhost:5173
```

## Tài khoản mẫu (mật khẩu `123456`)

| Tài khoản | Chức vụ | Màn hình thấy được |
|---|---|---|
| `thungan1` | Thu ngân | Bán tại quầy, hóa đơn, đổi trả, hoàn tiền, khách hàng, sản phẩm |
| `banhangonline` | Nhân viên bán hàng online | Đơn online, hóa đơn, đổi trả, hoàn tiền, khách hàng, sản phẩm |
| `thukho` | Thủ kho | Nhập hàng, sản phẩm & tồn kho |
| `quanly` | Quản lý cửa hàng | Tất cả |

Quyền theo chức vụ nằm ở `src/lib/phien.tsx` (`quyenTheoChucVu`): chức vụ có chữ "thu ngân" → bán hàng, "online" → đơn online, "kho" → nhập hàng;
quản lý, giám đốc, ADMIN hoặc chức vụ không khớp thì thấy hết. Tài khoản khách hàng không đăng nhập được.

## Các màn hình

| Màn hình | Làm được gì |
|---|---|
| Tổng quan | Số đơn chờ xác nhận, khách xin hủy, đổi trả chờ duyệt, chờ hoàn tiền, sản phẩm tồn thấp |
| Bán tại quầy | Gõ mã/tên SP rồi Enter, tìm khách theo SĐT hoặc thêm khách mới, áp voucher, tiền khách đưa / tiền thối, lập hóa đơn và in PDF. Giỏ giữ lại khi tải lại trang |
| Đơn online | Lọc theo trạng thái / nguồn / ngày. Xác nhận, từ chối, giao, hoàn tất, hủy, "Khách không nhận" (bom hàng). Duyệt hoặc từ chối yêu cầu hủy của khách. Hiện độ tin cậy của SĐT (số lần bom hàng, đơn hoàn tất, bị từ chối) |
| Hóa đơn | Danh sách + chi tiết (main-sub), lọc "Do tôi lập", xuất Excel, in PDF, lập đổi trả từ hóa đơn |
| Đổi trả | Duyệt / từ chối / nhận hàng & hoàn tất yêu cầu từ web, lập phiếu tại quầy (đổi size/màu hoặc trả hàng), in PDF |
| Hoàn tiền | Đơn đã thanh toán online bị hủy: đánh dấu đã hoàn kèm ghi chú đối soát |
| Nhập hàng | Lập phiếu nhập (gõ tay hoặc đọc file Excel, xem trước lỗi từng dòng), tải file mẫu, xuất Excel, in phiếu |
| Sản phẩm & tồn kho | Tìm 2 chế độ, lọc tồn thấp, xem các biến thể size/màu và tồn |
| Khách hàng | Tìm, thêm, sửa; truy vấn hợp / giao (khách mua tại quầy và online) |
| Nhật ký SQL | 200 câu SQL gần nhất kèm file:dòng trong DAL và thời gian chạy |

## 9 yêu cầu của đồ án trên web staff

| Yêu cầu | Ở đâu |
|---|---|
| 1. Dòng code chạy truy vấn | Thanh trên cùng luôn hiện số câu SQL của thao tác vừa làm và vị trí `File.cs:dòng` (đọc header `X-So-Cau-Sql`, `X-Sql-Vi-Tri`). Màn hình Nhật ký SQL xem đầy đủ |
| 2. Tìm kiếm tái sử dụng / truy vấn mới | Bán tại quầy và Sản phẩm có công tắc 2 chế độ (F3), hiện số câu SQL và thời gian để so sánh. Code: `src/lib/timSanPham.ts` |
| 3, 5. Thống kê, báo cáo | Ở app admin |
| 4. Main-sub | Đơn online, hóa đơn, đổi trả, nhập hàng, sản phẩm, khách hàng: bấm dòng (hoặc ↑ ↓) là chi tiết hiện bên phải. Code: `src/components/MainSub.tsx` |
| 6. Hợp / giao | Khách hàng: "TP HCM ∪ Hà Nội (hợp)" và "Mua tại quầy ∩ đặt online (giao)" |
| 7. Excel | Xuất hóa đơn, phiếu nhập, sản phẩm, khách hàng; nhập phiếu nhập từ Excel + file mẫu |
| 8. PDF | Hóa đơn, đơn hàng, phiếu đổi trả, phiếu nhập |
| 9. Bàn phím | Bấm `?` để xem bảng phím tắt |

## Phím tắt

| Phím | Tác dụng |
|---|---|
| `Alt+1…9` | Chuyển màn hình theo thứ tự menu |
| `?` | Bảng phím tắt |
| `/` | Ô tìm kiếm của màn hình |
| `Alt+L` | Làm mới dữ liệu |
| `Alt+N` | Lập phiếu đổi trả / phiếu nhập, thêm khách hàng |
| `↑ ↓ Home End` | Chọn dòng trong danh sách main-sub |
| `Ctrl+Enter` | Xác nhận hộp thoại nhập lý do |
| Bán tại quầy | `F2` tìm SP, `F3` đổi chế độ tìm, `F4` SĐT khách, `F6` voucher, `F7` phương thức thanh toán, `F9` thanh toán, `+ / −` số lượng, `Alt+Delete` xóa giỏ |
| Đơn online | `Alt+X` xác nhận / duyệt hủy, `Alt+G` giao, `Alt+H` hoàn tất, `Alt+P` in PDF |

## Cấu trúc

```
src/
  api/        client.ts (gọi API, token, đọc header SQL, tải PDF/Excel), types.ts
  lib/        phien (đăng nhập, quyền), phimTat, timSanPham, dinhDang, nhan (nhãn trạng thái)
  components/ MainSub, ThanhSql, HoiLyDo (hộp nhập lý do), BangPhimTat, DoiMatKhau, Chung
  pages/      một file cho mỗi màn hình
```

`npm run build` để build bản chạy thật (thư mục `dist`).
