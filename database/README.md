# CSDL QL_BanThoiTrang

Cần SQL Server 2017 trở lên (đã chạy thử trên SQL Server 2022).

## Thứ tự chạy

| Bước | File | Ghi chú |
| --- | --- | --- |
| 1 | `QL_BanThoiTrang_v3.sql` | Tạo CSDL mới, bản đầy đủ 23 bảng (gốc + bổ sung v2 + bổ sung v3) |
| 1' | `QL_BanThoiTrang_goc.sql` → `QL_BanThoiTrang_BoSung_v2.sql` → `QL_BanThoiTrang_BoSung_v3.sql` | Cách khác, ra cùng cấu trúc với bước 1 |
| 2 | `QL_BanThoiTrang_DuLieuMau.sql` | Nạp dữ liệu mẫu, chỉ chạy trên CSDL còn trống |
| 3 | `KiemThu_QL_BanThoiTrang.sql` | 56 test PASS/FAIL + kết quả các câu thống kê yêu cầu 5, 6. Không làm đổi dữ liệu |

Chạy bằng SSMS: mở file, bấm Execute. Chạy bằng sqlcmd: thêm cờ `-I`
(bật QUOTED_IDENTIFIER, cần cho filtered index), ví dụ:

```
sqlcmd -S localhost -E -I -i QL_BanThoiTrang_v3.sql
```

Muốn làm lại từ đầu: `DROP DATABASE QL_BanThoiTrang;` rồi chạy lại bước 1, 2.

**Đã có CSDL v2 đang chạy (có dữ liệu)?** Chỉ cần chạy thêm `QL_BanThoiTrang_BoSung_v3.sql`: file chỉ thêm bảng / cột,
không xóa dữ liệu, chạy lại nhiều lần vẫn an toàn, và tự thêm chương trình "Ưu đãi thành viên mới".

## Bổ sung v3 (04/11/2026): khách mua không cần tài khoản

| Thay đổi | Mục đích |
| --- | --- |
| Bảng mới `MA_OTP` | Mã OTP xác thực SĐT: lưu bản băm SHA-256 (không lưu mã gốc), hạn 5 phút, đếm số lần nhập sai, IP gửi |
| `KHACH_HANG.sdt_da_xac_thuc` + chỉ mục UNIQUE `UX_KH_sdt` | 1 SĐT = 1 khách: gom đơn của khách vãng lai, gắn vào tài khoản khi đăng ký |
| `DON_DAT_HANG.nguon` (`TAI_KHOAN` / `VANG_LAI`), `email_nhan`, `ma_km` | Phân biệt đơn khách vãng lai, ghi KM đã áp lúc đặt |
| `KHUYEN_MAI.hinh_thuc` thêm `THANH_VIEN_MOI` + bảng mới `CT_KM_THANH_VIEN_MOI` | Ưu đãi thành viên mới: giảm X% cho N đơn đầu đặt bằng tài khoản, tối đa Y đồng mỗi đơn |
| `DON_DAT_HANG.yeu_cau_huy` (`CHO_DUYET` / `DA_DUYET` / `TU_CHOI`), `ly_do_huy`, `ngay_yeu_cau_huy`, `phan_hoi_huy`, `ngay_huy`, `nguoi_huy` (`KHACH` / `NHAN_VIEN`) | Khách xin hủy đơn đã xác nhận, nhân viên duyệt / từ chối; lưu lý do, ai hủy, lúc nào. Hủy theo yêu cầu được duyệt không tính bom hàng |
| Bảng mới `HOAN_TIEN` (1 đơn tối đa 1 phiếu, `CHO_HOAN` → `DA_HOAN`) | Đơn đã chuyển khoản / online bị hủy thì tự sinh phiếu chờ hoàn tiền |

## Tài khoản mẫu (mật khẩu đều là `123456`)

| Tên đăng nhập | Vai trò | Dùng ở |
| --- | --- | --- |
| giamdoc, quanly | ADMIN | App desktop admin |
| thungan1, thungan2, thukho, banhangonline | NHAN_VIEN | Web staff |
| khach01 ... khach15 | KHACH_HANG | Web bán hàng (khach15 đang bị khóa) |

Khách vãng lai mẫu (chưa có tài khoản, tra cứu đơn bằng OTP ở trang `/tra-cuu-don`):

| SĐT | Khách | Đơn |
| --- | --- | --- |
| 0987000021 | KH021 Đặng Thu Trang | DH00071 hoàn tất, DH00072 chờ xác nhận |
| 0987000022 | KH022 Ngô Bảo Long | DH00073, DH00074 bị hủy sau khi đã lập hóa đơn ("bom hàng" 2 lần, không được chọn COD nữa), DH00075 chờ xác nhận |

## Sản phẩm mẫu

18 sản phẩm thật của UNIQLO Việt Nam (195 SKU = màu × size), lấy ngày 02/11/2026, khai báo trong
`tools/uniqlo_catalog.json` (tên, giá, chất liệu, mô tả, màu, size, ảnh). Ảnh nằm ở
`web-ban-hang/public/images/uniqlo/` (nguồn và bản quyền: `NGUON.md` trong thư mục đó, chỉ dùng cho đồ án).
Cột `SAN_PHAM.hinh_anh` lưu ảnh đầu tiên của màu; các ảnh còn lại web đọc từ `manifest.json` cùng thư mục.

Khuyến mãi đang chạy để demo: `KM05` (20/10 - 31/12/2026) giảm đúng mức UNIQLO đang giảm cho
5 sản phẩm (Áo Thun Dáng Boxy Ngắn, Áo Khoác Vải Cotton Canvas, Quần Jeans Baggy Ống Cong Barrel,
Quần Smart Pants nữ, Chân Váy Satin). `KM06` "Ưu đãi thành viên mới": giảm 10% cho 3 đơn đầu đặt bằng tài khoản,
tối đa 100.000đ mỗi đơn.

## Sinh lại dữ liệu mẫu

`tools/gen_dulieumau.py` sinh ra file dữ liệu mẫu (cố định seed, chạy lại ra y hệt).
`tools/gen_manifest_anh.py` sinh lại danh sách ảnh cho web (bỏ ảnh trùng):

```
python3 tools/gen_dulieumau.py > QL_BanThoiTrang_DuLieuMau.sql
python3 tools/gen_manifest_anh.py
```
