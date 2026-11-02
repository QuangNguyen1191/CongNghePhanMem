# CSDL QL_BanThoiTrang

Cần SQL Server 2017 trở lên (đã chạy thử trên SQL Server 2022).

## Thứ tự chạy

| Bước | File | Ghi chú |
| --- | --- | --- |
| 1 | `QL_BanThoiTrang_v2.sql` | Tạo CSDL mới, bản đầy đủ (gốc + bổ sung v2) |
| 1' | `QL_BanThoiTrang_goc.sql` rồi `QL_BanThoiTrang_BoSung_v2.sql` | Cách khác, ra cùng cấu trúc với bước 1 |
| 2 | `QL_BanThoiTrang_DuLieuMau.sql` | Nạp dữ liệu mẫu, chỉ chạy trên CSDL còn trống |
| 3 | `KiemThu_QL_BanThoiTrang.sql` | 43 test PASS/FAIL + kết quả các câu thống kê yêu cầu 5, 6. Không làm đổi dữ liệu |

Chạy bằng SSMS: mở file, bấm Execute. Chạy bằng sqlcmd: thêm cờ `-I`
(bật QUOTED_IDENTIFIER, cần cho filtered index), ví dụ:

```
sqlcmd -S localhost -E -I -i QL_BanThoiTrang_v2.sql
```

Muốn làm lại từ đầu: `DROP DATABASE QL_BanThoiTrang;` rồi chạy lại bước 1, 2.

## Tài khoản mẫu (mật khẩu đều là `123456`)

| Tên đăng nhập | Vai trò | Dùng ở |
| --- | --- | --- |
| giamdoc, quanly | ADMIN | App desktop admin |
| thungan1, thungan2, thukho, banhangonline | NHAN_VIEN | Web staff |
| khach01 ... khach15 | KHACH_HANG | Web bán hàng (khach15 đang bị khóa) |

## Sản phẩm mẫu

18 sản phẩm thật của UNIQLO Việt Nam (195 SKU = màu × size), lấy ngày 02/11/2026, khai báo trong
`tools/uniqlo_catalog.json` (tên, giá, chất liệu, mô tả, màu, size, ảnh). Ảnh nằm ở
`web-ban-hang/public/images/uniqlo/` (nguồn và bản quyền: `NGUON.md` trong thư mục đó, chỉ dùng cho đồ án).
Cột `SAN_PHAM.hinh_anh` lưu ảnh đầu tiên của màu; các ảnh còn lại web đọc từ `manifest.json` cùng thư mục.

Khuyến mãi đang chạy để demo: `KM05` (20/10 - 31/12/2026) giảm đúng mức UNIQLO đang giảm cho
5 sản phẩm (Áo Thun Dáng Boxy Ngắn, Áo Khoác Vải Cotton Canvas, Quần Jeans Baggy Ống Cong Barrel,
Quần Smart Pants nữ, Chân Váy Satin).

## Sinh lại dữ liệu mẫu

`tools/gen_dulieumau.py` sinh ra file dữ liệu mẫu (cố định seed, chạy lại ra y hệt).
`tools/gen_manifest_anh.py` sinh lại danh sách ảnh cho web (bỏ ảnh trùng):

```
python3 tools/gen_dulieumau.py > QL_BanThoiTrang_DuLieuMau.sql
python3 tools/gen_manifest_anh.py
```
