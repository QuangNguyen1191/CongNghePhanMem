# Phase 1 — Phân tích & đề xuất cho web bán hàng (khách hàng)

Tài liệu đi kèm: [API_MAPPING.md](API_MAPPING.md). Phase 1 chưa có dòng code frontend nào.

## 1. Hiện trạng dự án

```
QL_BanThoiTrang/
├─ database/      SQL Server: QL_BanThoiTrang_v2.sql (cấu trúc) + DuLieuMau.sql (dữ liệu mẫu)
├─ backend/       .NET 8, 3 lớp DTO / DAL (Dapper) / BLL + WebApi (JWT, Swagger) + Tests (xUnit)
├─ api-tests/     105 test Playwright gọi HTTP vào WebApi
└─ web-ban-hang/  (mới) tài liệu Phase 1; code frontend sẽ nằm ở đây
```

- **Chưa có frontend nào.** Backend đã chuẩn bị sẵn cho "2 web React" (CORS mở cổng 5173 cho web khách, 5174 cho web staff). Plan chung của đồ án cũng chốt web khách là React. Vì vậy không có framework cũ cần giữ, và React là lựa chọn khớp với hệ thống.
- **CSDL** (20 bảng, đã được duyệt) phục vụ web khách qua: `LOAI_SP`, `SAN_PHAM` (mỗi dòng 1 SKU size + màu), `TAI_KHOAN`, `KHACH_HANG`, `GIO_HANG` + chi tiết, `DON_DAT_HANG` + chi tiết, `HOA_DON` + chi tiết, `DOI_TRA_HANG` + chi tiết, `KHUYEN_MAI` + 3 bảng chi tiết.
- **Dữ liệu mẫu:** 6 loại SP, 32 SKU (khoảng 12 mẫu sản phẩm), tài khoản khách `khach01`…`khach15` / `123456` (`khach15` bị khóa), voucher `WELCOME10`, `FREESHIP5`, KM hóa đơn ≥ 1.000.000đ giảm 5%.

## 2. Chức năng làm được / không làm được

**Làm đầy đủ với API thật:** trang chủ (danh mục, banner KM đang chạy, khối giảm giá), danh sách SP (lọc danh mục, giá, màu, size, còn hàng; tìm kiếm; sắp xếp giá; phân trang client), chi tiết SP chọn màu/size theo đúng biến thể và tồn kho, đăng ký, đăng nhập, đăng xuất, hồ sơ, đổi mật khẩu, giỏ hàng (cả khi chưa đăng nhập và gộp khi đăng nhập), xem trước tiền + voucher, đặt hàng COD / chuyển khoản / online (ghi nhận), danh sách và chi tiết đơn, hủy đơn chờ xác nhận, tải PDF, hóa đơn của tôi, gửi và theo dõi yêu cầu đổi trả.

**Không làm vì thiếu API hoặc dữ liệu (cần nhóm quyết):**

| Thiếu | Lý do | Đề xuất của tôi |
|---|---|---|
| Lọc giới tính | Không có cột giới tính | Bỏ. Điều hướng theo 6 loại SP |
| Sản phẩm mới / sắp xếp mới nhất | Không có ngày tạo SP | Xấp xỉ bằng mã SP giảm dần, ghi rõ trong báo cáo. Hoặc bỏ |
| Bán chạy | Thống kê chỉ cho ADMIN | Bỏ khối này (không tự thêm API) |
| Bộ sưu tập | Không có trong CSDL | Thay bằng khối "Mua theo danh mục" |
| Size chart | Không có dữ liệu | Trang hướng dẫn chọn size **tĩnh** (nội dung chung, không gắn từng SP), hoặc bỏ |
| Quên mật khẩu | Chỉ admin đặt lại | Hiện hướng dẫn liên hệ cửa hàng |
| Nhiều địa chỉ | Chỉ 1 địa chỉ trong hồ sơ | Địa chỉ hồ sơ là mặc định, sửa được ngay ở checkout |
| Phí ship | Không có | Hiện "Miễn phí vận chuyển" hoặc không hiện dòng phí. Không tự tính |
| Wishlist, đánh giá, thông báo | Không có bảng | Bỏ |
| Lịch sử tìm kiếm, đã xem gần đây | Không có API | Làm ở trình duyệt (localStorage), dữ liệu SP vẫn lấy từ API |
| Ảnh sản phẩm | Có đường dẫn nhưng chưa có file | Nhóm cung cấp ảnh tự chụp / miễn phí bản quyền vào `public/images/` |

## 3. Kiến trúc frontend đề xuất

**Công nghệ:** Vite + React 18 + TypeScript.

| Việc | Thư viện | Lý do |
|---|---|---|
| Định tuyến, bảo vệ trang | react-router-dom 6 | Chuẩn, có loader/redirect |
| Gọi API, cache, chống gọi trùng | @tanstack/react-query 5 | Gộp request giống nhau, cache danh sách SP, trạng thái loading/error sẵn |
| HTTP | fetch bọc trong `apiClient` (không thêm axios) | Ít phụ thuộc |
| Form + validation | react-hook-form + zod | Quy tắc khớp backend (TK ≥ 4, MK ≥ 6, SĐT 9–11 số) |
| Style | CSS Modules + CSS variables (design tokens) | Giao diện riêng, không mang dáng "template Tailwind/shadcn" |
| Icon | lucide-react (SVG) | Nét mảnh, hợp tối giản |
| Toast | sonner | Nhỏ, có hàng đợi, hỗ trợ screen reader |

**Cấu trúc thư mục:**

```
web-ban-hang/
├─ public/images/              ảnh SP (sp001.jpg...) do nhóm cung cấp
├─ src/
│  ├─ app/                     App.tsx, router.tsx, providers (QueryClient, Auth, Cart, Toast)
│  ├─ api/                     TẦNG SERVICE: apiClient.ts (base URL, token, lỗi tập trung)
│  │  ├─ types.ts              kiểu TS khớp DTO backend (SanPhamDTO, GioHangDTO...)
│  │  ├─ sanPhamApi.ts  loaiSpApi.ts  taiKhoanApi.ts  khachHangApi.ts
│  │  ├─ gioHangApi.ts  khuyenMaiApi.ts  donHangApi.ts  hoaDonApi.ts  doiTraApi.ts
│  ├─ features/                LOGIC NGHIỆP VỤ theo chức năng (hooks + component riêng)
│  │  ├─ catalog/              gomSanPham (SKU → mẫu), boLoc, sapXep, useSanPham...
│  │  ├─ product/              chọn biến thể, useBienThe
│  │  ├─ auth/                 AuthContext, useAuth, RequireAuth
│  │  ├─ cart/                 CartContext: 1 giao diện cho giỏ server + giỏ tạm
│  │  ├─ checkout/             useTinhTien, useDatHang (khóa chống gửi trùng)
│  │  ├─ orders/  account/  returns/
│  ├─ components/              UI dùng chung: Button, Input, Select, Price, Badge, QuantityStepper,
│  │                           ProductCard, ProductGrid, Skeleton, EmptyState, ErrorState,
│  │                           ConfirmDialog, Breadcrumb, Drawer, Pagination
│  ├─ layouts/                 MainLayout (Header, MegaMenu, Footer), AccountLayout, CheckoutLayout
│  ├─ pages/                   Home, Listing, ProductDetail, Search, Cart, Checkout, OrderSuccess,
│  │                           Login, Register, Account/*, Orders, OrderDetail, Returns, NotFound
│  ├─ styles/                  tokens.css, base.css
│  └─ lib/                     formatTien, formatNgay, nhanTrangThai, storage
```

**Các cơ chế chính:**

- **Lỗi API tập trung:** `apiClient` đọc `{ thongBao }` và ném `ApiError(status, thongBao, loi)`. `401` thì xóa phiên, chuyển tới `/dang-nhap?quayLai=...`. `403` thì báo "không có quyền". `404` hiện trang không tìm thấy. `5xx` / mất mạng thì toast "Lỗi hệ thống, vui lòng thử lại". Lỗi form (`loi.truong`) gắn vào đúng ô nhập.
- **Token:** backend chỉ hỗ trợ Bearer header (không có cookie httpOnly, không refresh token). Lưu token trong `localStorage` để F5 không mất đăng nhập. Không lưu mật khẩu, không ghi token hay dữ liệu cá nhân ra console, không dùng `dangerouslySetInnerHTML`. Tự đăng xuất khi token hết hạn (đọc `exp`).
- **Giỏ hàng 2 chế độ:** chưa đăng nhập thì giỏ ở localStorage `[{maSp, soLuong}]`, giá và tồn lấy lại từ API. Sau khi đăng nhập, gọi `POST /api/gio-hang/gop` rồi xóa giỏ tạm. Component chỉ thấy 1 API chung `useCart()`.
- **Giá & tồn không tin frontend:** frontend chỉ hiển thị. Tổng tiền ở checkout lấy từ `/api/khuyen-mai/tinh-tien`; đơn được server tính lại khi đặt.
- **Chống đặt trùng:** nút "Đặt hàng" khóa ngay khi bấm (`isPending` của mutation + ref chặn lần 2). Server cũng tự chặn vì giỏ bị xóa sau khi đặt.
- **Trang yêu cầu đăng nhập:** `RequireAuth` bọc `/tai-khoan/*`, `/thanh-toan`, `/don-hang/*`.
- **Hiệu năng:** danh sách SP tải 1 lần và cache 5 phút; ảnh `loading="lazy"`, khung giữ tỉ lệ 3:4 (không giật layout); tách code theo trang (`React.lazy`); tìm kiếm debounce 300 ms.

**Bảng route:**

| Route | Trang | Đăng nhập |
|---|---|---|
| `/` | Trang chủ | |
| `/danh-muc/:maLoaiSp`, `/san-pham` | Danh sách + bộ lọc | |
| `/tim-kiem?q=` | Kết quả tìm kiếm | |
| `/san-pham/:maSp` | Chi tiết (mở theo 1 SKU, chọn được biến thể khác) | |
| `/gio-hang` | Giỏ hàng | |
| `/thanh-toan` → `/dat-hang-thanh-cong/:maDon` | Checkout 1 trang | ✔ |
| `/dang-nhap`, `/dang-ky` | Xác thực | |
| `/tai-khoan`, `/tai-khoan/doi-mat-khau` | Hồ sơ | ✔ |
| `/tai-khoan/don-hang`, `/tai-khoan/don-hang/:ma` | Đơn hàng | ✔ |
| `/tai-khoan/hoa-don`, `/tai-khoan/doi-tra` | Hóa đơn, đổi trả | ✔ |

## 4. Design system

**Nguồn:** skill `/ui-ux-pro-max` (đã chạy, chế độ `--design-system` với "fashion apparel e-commerce minimal premium", variance 3, motion 2, density 4) kết hợp nghiên cứu uniqlo.com/vn/vi.

Skill gợi ý phong cách **Minimalism & Swiss Style** và tôi giữ hướng này. Có 2 chỗ tôi chỉnh lại so với gợi ý của skill, vì brief yêu cầu khác:
- Skill đề xuất màu nhấn vàng đồng `#A16207`. Brief muốn đỏ có kiểm soát nên tôi đổi sang đỏ.
- Skill đề xuất cặp Cormorant + Montserrat (serif sang trọng). Brief cần chữ hiện đại, dễ đọc, và cả web là tiếng Việt. Tôi chọn **Be Vietnam Pro**, một font sans có bộ dấu tiếng Việt chuẩn. Đây là lựa chọn của tôi, không phải kết quả tìm kiếm của skill (skill không trả về khớp tốt cho font tiếng Việt).

**Học từ UNIQLO (chỉ lấy tinh thần, không sao chép):** header có ô tìm kiếm luôn hiện; danh mục xếp theo loại sản phẩm; khối khuyến mãi/hàng mới tách riêng; thẻ sản phẩm gồm tên, 1 dòng mô tả, giá; nhãn khuyến mãi ghi rõ ngày áp dụng; khối chiến dịch xen giữa các hàng sản phẩm. Lưu ý: công cụ chỉ đọc được phần chữ của trang chủ và trang /men, không thấy được màu, ảnh, trang chi tiết hay checkout. Các quyết định thị giác dưới đây là của tôi.

**Nhận diện riêng:** không dùng logo, chữ "UNIQLO" kiểu khối đỏ hay ảnh của họ. Tên hiển thị tạm là chữ "UNIQLO" dạng chữ thường theo đề tài; nếu muốn khác biệt hơn thì đặt tên cửa hàng riêng (lấy từ `CuaHang:Ten` của backend).

**Màu (token):**

| Token | Giá trị | Dùng cho |
|---|---|---|
| `--c-ink` | `#111111` | Chữ chính, nút chính (nền đen chữ trắng) |
| `--c-ink-2` | `#4A4A4A` | Chữ phụ |
| `--c-muted` | `#767676` | Ghi chú (đạt 4.5:1 trên nền trắng) |
| `--c-line` | `#E3E3E3` | Đường kẻ, viền ô |
| `--c-surface` | `#F5F5F3` | Nền khối, nền ảnh chờ |
| `--c-bg` | `#FFFFFF` | Nền trang |
| `--c-accent` | `#C8102E` | **Chỉ cho:** giá khuyến mãi, nhãn SALE, số lượng trên icon giỏ, lỗi |
| `--c-success` | `#1F7A4D` | Còn hàng, đặt thành công |
| `--c-warning` | `#B26B00` | Sắp hết hàng (< 5) |

Không gradient, không đổ bóng màu. Bóng chỉ dùng cho drawer/menu (`0 8px 24px rgba(0,0,0,.08)`).

**Chữ:** Be Vietnam Pro 400/500/600/700, `font-variant-numeric: tabular-nums` cho giá.

| Cấp | Desktop | Mobile | Dùng |
|---|---|---|---|
| Display | 40/48, 700 | 28/36 | Tiêu đề banner |
| H1 | 28/36, 700 | 22/30 | Tên trang, tên SP |
| H2 | 20/28, 600 | 18/26 | Tiêu đề khối |
| Body | 15/24, 400 | 15/24 | Nội dung |
| Small | 13/20 | 13/20 | Mô tả thẻ, ghi chú |
| Label | 12/16, 600, chữ in hoa, giãn chữ 0.04em | | Nhãn, menu danh mục |

**Khoảng cách:** thang 4px (4, 8, 12, 16, 24, 32, 48, 64, 96). Container tối đa 1280px, lề 16px (mobile) / 24px (tablet) / 40px (desktop).

**Hình khối:** bo góc 0 cho ảnh và thẻ, 2px cho nút và ô nhập. Ảnh SP tỉ lệ 3:4, nền `--c-surface`.

**Lưới SP:** 2 cột (< 640px), 3 cột (640–1023px), 4 cột (≥ 1024px), khoảng cách 16px / 24px.

**Chuyển động:** chỉ hover/focus 150–200ms (đổi màu, gạch chân), drawer trượt 200ms. Tắt hết khi `prefers-reduced-motion`. Không có hiệu ứng cuộn trang.

**Component chính:**
- *Header:* dòng thông báo mỏng (KM đang chạy) → logo trái, menu 6 danh mục giữa, ô tìm kiếm + tài khoản + giỏ phải. Mega menu: cột danh mục + khối "Đang giảm giá". Mobile: icon menu mở drawer toàn màn, tìm kiếm thành hàng riêng dưới header.
- *Product card:* ảnh 3:4 → các chấm màu (tối đa 5, "+n") → tên (2 dòng) → dải size "S–XL" → giá; giá KM màu đỏ, giá gốc gạch ngang xám → nhãn "Hết hàng" phủ ảnh khi mọi biến thể = 0.
- *Chi tiết SP:* trái là ảnh lớn + dải ảnh các màu; phải là breadcrumb, tên, mã SP, giá, chọn màu (ô chữ + chấm), chọn size (nút vuông, size hết hàng gạch chéo, không chọn được), dòng tồn kho ("Còn 4 sản phẩm" màu cảnh báo), số lượng (giới hạn theo tồn), nút "Thêm vào giỏ" (đen) + "Mua ngay" (viền), các mục gập: mô tả, chất liệu, đổi trả 30 ngày.
- *Trạng thái:* skeleton đúng hình dạng nội dung; empty state có 1 câu + 1 nút; error state có nút "Thử lại"; toast góc dưới (mobile) / góc trên phải (desktop).
- *Form:* nhãn luôn hiện trên ô, lỗi ngay dưới ô, chạm tối thiểu 44px, focus ring 2px đen.

**Tránh:** thẻ bo góc lớn, gradient, glassmorphism, emoji làm icon, banner tự chạy, popup chào mừng.

## 5. Kế hoạch triển khai

| Phase | Nội dung | Kiểm tra trước khi qua phase sau |
|---|---|---|
| 2. UI/UX | Khởi tạo Vite + TS, tokens.css, component dùng chung, Header / MegaMenu / Footer / layouts, `apiClient` + types | Trang mẫu component ở 375 / 768 / 1280px; build không lỗi |
| 3. Trang chủ & danh mục | Home, Listing, ProductCard, gom SKU, lọc / sắp xếp / tìm / phân trang, đồng bộ bộ lọc lên URL | Lọc và tìm khớp dữ liệu mẫu; gọi API không trùng |
| 4. Chi tiết SP & tài khoản | ProductDetail + biến thể; đăng ký / đăng nhập / đăng xuất; hồ sơ; đổi mật khẩu; RequireAuth; xử lý 401 | Đăng nhập khach01; khach15 bị chặn; tài khoản nhân viên bị chặn |
| 5. Giỏ & checkout | Giỏ tạm + giỏ server + gộp; đổi biến thể; checkout 1 trang (địa chỉ, phương thức, voucher, tổng từ API); đặt hàng chống trùng; trang kết quả | Đặt COD + WELCOME10 thành công; hết hàng, voucher sai, giỏ trống đều báo đúng |
| 6. Đơn hàng | Danh sách, chi tiết, thanh trạng thái, hủy (có xác nhận), PDF, hóa đơn, gửi và xem đổi trả | Hủy được đơn chờ, không hủy được đơn đã xác nhận (test cùng web staff/Swagger) |
| 7. Kiểm thử | Playwright E2E cho luồng mua hàng (dùng lại bộ api-tests), kiểm tra responsive, Lighthouse, sửa lỗi | Kịch bản demo bảo vệ chạy trơn |

Mỗi phase: code trực tiếp trong `C:\Users\vugia\OneDrive\Desktop\QL_BanThoiTrang\web-ban-hang`, chạy với WebApi + CSDL mẫu, rồi báo bạn duyệt.

## 6. Cần bạn quyết trước Phase 2

1. Đồng ý các mục ❌ ở bảng mục 2 là **bỏ hoặc thay thế** như đề xuất (nhất là "Sản phẩm mới" xấp xỉ theo mã SP, và size chart tĩnh).
2. Ảnh sản phẩm: nhóm tự chuẩn bị 32 ảnh (hoặc ít nhất 1 ảnh mỗi mẫu + màu), hay để tôi dùng khung trung tính cho tới khi có ảnh.
3. Tên thương hiệu hiển thị: giữ "UNIQLO" theo đề tài, hay đặt tên cửa hàng riêng.
4. Để demo giá khuyến mãi: tạo 1 KM sản phẩm mới qua API có sẵn (không đụng CSDL/backend). Có tạo không?
