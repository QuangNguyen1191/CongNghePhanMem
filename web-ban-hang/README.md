# Web bán hàng (khách hàng) - UNIQLO

React 18 + TypeScript + Vite. Gọi WebApi trong `../backend` (xem `API_MAPPING.md`, `PHASE1_PHAN_TICH.md`).

## Chạy

1. Chạy SQL Server + WebApi (`dotnet run --project ../backend/WebApi`, cổng 5212).
2. Cài Node.js 20+ rồi:

```
npm install
npm run dev        # http://localhost:5173 (cổng đã mở CORS ở backend)
```

Đổi địa chỉ backend trong `.env` (`VITE_API_URL`).

`npm run build` để kiểm tra kiểu + đóng gói. Trang `/thiet-ke` là trang mẫu design system (chỉ có khi chạy `npm run dev`).

## Chức năng

| Trang | Đường dẫn |
|---|---|
| Trang chủ, danh sách, danh mục, tìm kiếm (lọc màu/size/giá, sắp xếp) | `/`, `/san-pham`, `/danh-muc/:ma`, `/tim-kiem?q=` |
| Chi tiết: ảnh theo màu, chọn màu/size, tồn kho, giá KM, thêm giỏ, mua ngay, SP liên quan, đã xem gần đây | `/san-pham/:maSp` |
| Giỏ hàng (chưa đăng nhập: lưu ở trình duyệt, đăng nhập xong tự gộp vào giỏ trên server) | `/gio-hang` |
| Thanh toán: địa chỉ từ hồ sơ, COD/chuyển khoản/online, mã giảm giá, chống bấm 2 lần | `/thanh-toan` |
| Đăng nhập, đăng ký | `/dang-nhap`, `/dang-ky` |
| Tài khoản: hồ sơ + hạng/điểm, đơn hàng (hủy, PDF), hóa đơn (PDF), đổi/trả, đổi mật khẩu | `/tai-khoan/...` |

Giỏ hàng đổi được màu / kích cỡ ngay trong giỏ (nút "Đổi" cạnh tên sản phẩm). Danh sách sắp xếp được theo giá, tên A–Z / Z–A.

## Phím tắt (yêu cầu 9 của đồ án)

Bấm `?` ở bất kỳ trang nào (hoặc link "Phím tắt bàn phím" ở footer) để xem bảng phím tắt. Code: `src/features/phimTat/`.

| Phím | Tác dụng |
|---|---|
| `/` | Đưa con trỏ vào ô tìm kiếm |
| `?` | Mở bảng phím tắt |
| `g` rồi `h` / `s` / `c` / `t` / `d` | Trang chủ / Sản phẩm / Giỏ hàng / Tài khoản / Đơn hàng |
| `←` `→` | Đổi ảnh ở trang chi tiết |
| `a` | Thêm vào giỏ ở trang chi tiết |
| `f` | Mở bộ lọc ở trang danh sách |
| `Enter` trong ô mã giảm giá | Áp dụng mã (không đặt hàng) |
| `Ctrl` + `Enter` ở trang thanh toán | Đặt hàng |
| `Esc` | Đóng menu, bộ lọc, hộp thoại |

Ngoài ra mọi nút, ô nhập, lựa chọn màu/size đều dùng được bằng `Tab`, `Shift+Tab`, mũi tên, `Enter`, `Space`; có link "Bỏ qua tới nội dung" khi bấm `Tab` lần đầu.
Phím tắt không chạy khi đang gõ trong ô nhập.

## Test E2E (Playwright)

Test chạy trên trình duyệt thật và gọi API thật, nên cần:

1. CSDL vừa nạp `QL_BanThoiTrang_DuLieuMau.sql` (dùng tài khoản mẫu `khach01`, `khach02`, `khach15`, mật khẩu `123456`).
2. WebApi đang chạy ở cổng 5212.

```
npx playwright install chromium   # chỉ lần đầu
npm run test:e2e                  # tự bật `npm run dev` nếu chưa chạy
npm run test:e2e:report           # xem báo cáo HTML
```

`e2e/07-vang-lai.spec.ts` (v3) thử mua không cần tài khoản: xác thực SĐT bằng OTP (mã demo hiện trên trang), đặt hàng,
tạo tài khoản nhanh ở trang đặt hàng thành công, ưu đãi thành viên mới, tra cứu và hủy đơn ở `/tra-cuu-don`.

Mỗi lần chạy sẽ tạo thêm 1 tài khoản `e2e...`, 1 đơn hàng đã hủy của khach01 và (nếu còn hóa đơn trong hạn) 1 phiếu đổi trả chờ duyệt.
Muốn dữ liệu sạch thì nạp lại CSDL.

## Cấu trúc

| Thư mục | Nội dung |
|---|---|
| `src/api` | Tầng service: `apiClient` (token, lỗi tập trung), kiểu khớp DTO backend, hàm gọi từng API |
| `src/features` | Logic nghiệp vụ theo chức năng (hooks): catalog, auth, cart, checkout, orders... |
| `src/components` | UI dùng chung: Button, Field, Price, Badge, QuantityStepper, Skeleton, Empty/Error state, Drawer, ConfirmDialog, ProductImage |
| `src/layouts` | Header, MegaMenu, MobileMenu, Footer, MainLayout |
| `src/pages` | Các trang |
| `src/styles` | `tokens.css` (màu, chữ, khoảng cách), `base.css` |
| `public/images/uniqlo` | Ảnh sản phẩm UNIQLO (cột `SAN_PHAM.hinh_anh` trỏ vào ảnh đầu của mỗi màu, `manifest.json` liệt kê đủ bộ ảnh) |
| `e2e` | Test E2E Playwright |
