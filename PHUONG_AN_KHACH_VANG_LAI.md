# Phương án: khách vãng lai mua hàng không cần đăng ký

> **Trạng thái: đã làm xong (04/11/2026)** theo phương án này, OTP giả lập. Kết quả và test: xem mục 2.6 trong `BAO_CAO_WEB_BAN_HANG.md`.

Yêu cầu của giáo viên: khách **không có tài khoản vẫn đặt hàng được** trên web bán hàng.
Đi kèm: đơn của khách vãng lai phải **xác thực được** (để web staff duyệt an toàn, chống đơn ảo), và
**khuyến khích tạo tài khoản** bằng ưu đãi giảm giá cho những đơn đầu.

## 0. Hiện trạng (đã đọc code)

| Chỗ | Hiện tại | Vấn đề với khách vãng lai |
|---|---|---|
| Web `/thanh-toan` | Bọc `RequireAuth`, bắt đăng nhập | Khách vãng lai bị đẩy sang trang đăng nhập |
| API `POST /api/don-hang` | Chỉ vai trò `KHACH_HANG`, lấy hàng từ giỏ trên server | Khách vãng lai không có giỏ trên server (giỏ nằm ở trình duyệt) |
| `DON_DAT_HANG.ma_kh` | `NOT NULL`, FK tới `KHACH_HANG` | Không sao: `KHACH_HANG.ma_tk` đã cho phép NULL (khách chưa có tài khoản), dùng lại được |
| Đăng ký (`TaiKhoanBLL.DangKy`) | Nếu SĐT trùng khách cũ chưa có tài khoản thì **gắn tài khoản vào khách đó** | Tiện cho việc gắn đơn cũ, nhưng **không kiểm tra người đăng ký có sở hữu SĐT không** → ai biết SĐT của người khác là xem được đơn của họ. Phải vá khi mở cho khách vãng lai |
| Voucher `WELCOME10` (KM03 "khách hàng mới") | Mã chung, ai cũng dùng được, không giới hạn số lần | Không phải ưu đãi khách mới thật sự |

## 1. Ý tưởng chính

**Khách vãng lai = một dòng `KHACH_HANG` có `ma_tk = NULL`, nhận diện bằng số điện thoại đã xác thực OTP.**

- Mọi việc của khách vãng lai (đặt hàng, tra cứu đơn, hủy đơn, đăng ký gắn đơn cũ) đều phải qua **một bước xác thực SĐT bằng mã OTP 6 số**.
- Xác thực xong, server cấp một **phiên khách vãng lai** (JWT ngắn hạn 30 phút, vai trò `VANG_LAI`, chứa SĐT và mã KH). Đặt hàng và tra cứu đơn dùng chung phiên này.
- Vì SĐT đã được chứng minh là của khách, việc **gom các đơn cùng SĐT vào một khách** và **gắn vào tài khoản khi đăng ký** là an toàn.
- Web staff không cần quy trình riêng: đơn vãng lai vẫn là `DON_DAT_HANG` trạng thái `CHO_XAC_NHAN`, dùng lại các API xác nhận / từ chối / giao / hoàn tất đã có, chỉ thêm thông tin để nhân viên đánh giá rủi ro.

### Luồng mua hàng của khách vãng lai

```
Giỏ (trình duyệt) → Thanh toán → chọn "Mua không cần tài khoản"
  → nhập Họ tên, SĐT, Email (tùy chọn), Địa chỉ
  → bấm "Gửi mã xác thực" → nhập OTP 6 số
  → bấm "Đặt hàng" → đơn CHO_XAC_NHAN (nguồn VÃNG LAI, SĐT đã xác thực)
  → trang "Đặt hàng thành công": mã đơn, link tra cứu, nút "Tạo tài khoản để giảm 10% cho 3 đơn tiếp theo"
```

Xác thực OTP **trước khi** tạo đơn, nên CSDL không bao giờ có đơn rác chưa xác thực, không cần thêm trạng thái "chờ xác thực" và không cần dọn đơn hết hạn.

### Tra cứu đơn (trang mới `/tra-cuu-don`)

Nhập SĐT → OTP → thấy **tất cả đơn của SĐT đó**: xem chi tiết, theo dõi tiến trình, hủy khi còn "Chờ xác nhận", tải PDF.
Không cho tra cứu chỉ bằng "mã đơn + SĐT", vì mã đơn tăng dần (DH00001, DH00002...) và SĐT thường bị người khác biết, nên đoán được.

### OTP gửi bằng gì

Đồ án không có tổng đài SMS, nên đề xuất **giả lập**: server sinh mã, lưu bản băm, ghi ra console WebApi; khi bật cấu hình demo (`Otp:HienMaDemo = true`) thì API trả luôn mã để web hiện thành thông báo "Mã OTP (demo): 123456".
Việc gửi tách thành interface `IGuiOtp`, sau này thay bằng gửi email thật (SMTP Gmail) hoặc SMS mà không sửa nghiệp vụ.

## 2. Chống đơn ảo / bom hàng

| Lớp | Quy tắc (số liệu nhóm tự đặt, sửa được trong `QuyTac`) |
|---|---|
| Xác thực SĐT | Bắt buộc OTP. Mã hết hạn sau **5 phút**, sai **5 lần** là hủy mã, mỗi mã dùng **1 lần**, chỉ lưu bản băm |
| Giới hạn gửi OTP | Mỗi SĐT tối đa **1 lần / 60 giây** và **5 lần / ngày**; mỗi IP tối đa **10 lần / giờ** |
| Giới hạn đơn đang chờ | Mỗi SĐT vãng lai tối đa **2 đơn** đang "Chờ xác nhận" cùng lúc |
| Giới hạn giá trị | Đơn vãng lai trả **COD** tối đa **5.000.000đ**, mỗi SKU tối đa **5 cái** (đơn lớn hơn: chuyển khoản hoặc tạo tài khoản) |
| Lịch sử bom hàng | Đếm số đơn của SĐT bị **từ chối** hoặc **nhân viên hủy khi đang giao**. Từ **2 lần** trở lên thì SĐT đó **không được chọn COD** nữa |
| Không trừ kho khi đặt | Giữ nguyên: kho chỉ trừ khi nhân viên xác nhận, nên đơn ảo không giữ hàng |

**Cho web staff sau này** (backend làm sẵn API): danh sách đơn có cột *Nguồn* (Tài khoản / Vãng lai), dấu "SĐT đã xác thực", và **chỉ số rủi ro** của SĐT (số đơn hoàn tất, số đơn bị từ chối / bom). Nhân viên gọi điện xác nhận với đơn rủi ro rồi bấm Xác nhận hoặc Từ chối như đơn thường.

## 3. Ưu đãi khi tạo tài khoản

- **Ưu đãi thành viên mới:** giảm **10% cho 3 đơn đầu** đặt bằng tài khoản, tối đa **100.000đ mỗi đơn** (con số đề xuất, admin sửa được).
- Lưu thành một chương trình khuyến mãi hình thức mới `THANH_VIEN_MOI`, nên admin bật / tắt / sửa % ở màn hình khuyến mãi, và hóa đơn ghi `ma_km` để **thống kê hiệu quả khuyến mãi** (yêu cầu 5) như các KM khác.
- Đếm "đơn đầu" = đơn có nguồn `TAI_KHOAN` của khách, không tính đơn đã hủy / bị từ chối (hủy thì được trả lại lượt).
- Không cộng dồn: giống quy tắc hiện tại, hệ thống chọn **mức giảm có lợi nhất** giữa voucher, KM hóa đơn và ưu đãi thành viên mới, và ghi rõ đang áp dụng mức nào.
- Mỗi SĐT chỉ hưởng một lần (vì SĐT đã xác thực OTP, tạo nhiều tài khoản bằng cùng SĐT là không được).
- ~~Bỏ voucher chung `WELCOME10`~~ Khi làm: **giữ `WELCOME10`** (test và demo cũ đang dùng); ưu đãi thành viên mới không cộng dồn, hệ thống tự chọn mức giảm có lợi hơn.

### Tạo tài khoản nhanh và gắn đơn cũ

- Ngay trên trang đặt hàng thành công (đang có phiên vãng lai đã xác thực SĐT): khách chỉ cần nhập **tên đăng nhập + mật khẩu** là có tài khoản, không phải nhập lại họ tên / SĐT / địa chỉ, không cần OTP lần nữa.
- Đăng ký thường (`/dang-ky`) thêm bước **xác thực OTP cho SĐT**. Nhờ đó vá luôn lỗ hổng ở mục 0.
- Khi đăng ký, nếu SĐT đã có khách vãng lai thì **gắn tài khoản vào đúng khách đó**: toàn bộ đơn, hóa đơn, điểm tích lũy cũ tự hiện trong tài khoản. Web báo: "Đã gắn 2 đơn hàng trước đây vào tài khoản của bạn".
- Đơn vãng lai cũ **không tính** vào 3 đơn ưu đãi, nên khách đã mua vãng lai rồi mới đăng ký vẫn được trọn ưu đãi (đúng mục đích kéo khách tạo tài khoản).

## 4. Thay đổi cụ thể

### CSDL: file mới `QL_BanThoiTrang_BoSung_v3.sql` (chạy sau v2, chỉ thêm, chạy lại an toàn) + bản đầy đủ `QL_BanThoiTrang_v3.sql`

| Thay đổi | Mục đích |
|---|---|
| Bảng mới `MA_OTP` (ma_otp, sdt, muc_dich `DAT_HANG/TRA_CUU/DANG_KY`, ma_bam, ngay_tao, han_dung, so_lan_sai, da_dung, dia_chi_ip) | Lưu OTP, đếm lần gửi / lần sai để giới hạn |
| `DON_DAT_HANG` thêm `nguon` (`TAI_KHOAN` / `VANG_LAI`, mặc định `TAI_KHOAN`), `email_nhan`, `ma_km` (KM đã áp lúc đặt; khi làm dùng cột này thay cho `sdt_da_xac_thuc` trên đơn) | Web staff phân biệt đơn vãng lai, gửi thông tin đơn qua email |
| `KHACH_HANG` thêm `sdt_da_xac_thuc` (bit) + **unique index lọc trên `sdt`** | 1 SĐT = 1 khách, để gom đơn và gắn tài khoản chính xác (đã kiểm tra dữ liệu mẫu không trùng SĐT) |
| `KHUYEN_MAI.hinh_thuc` thêm giá trị `THANH_VIEN_MOI` + bảng `CT_KM_THANH_VIEN_MOI` (ma_km, so_don_dau, phan_tram_giam_gia, giam_toi_da) | Ưu đãi thành viên mới, quản lý như KM khác |
| Dữ liệu mẫu: thêm KM `THANH_VIEN_MOI`, 2 khách / 5 đơn vãng lai mẫu (SĐT 0987000021, 0987000022), giữ `WELCOME10` | Demo ngay |

CSDL từ 20 lên 22 bảng.

### Backend

| Lớp | Việc |
|---|---|
| DTO | `GuiOtpDTO`, `XacThucOtpDTO`, `PhienVangLaiDTO`, `DatHangVangLaiDTO` (thông tin nhận hàng + danh sách dòng hàng + voucher), `RuiRoKhachDTO` |
| DAL | `MaOtpDAL`, thêm hàm trong `DonDatHangDAL` (lọc theo nguồn, theo SĐT, đếm đơn chờ / đơn bom), `KhuyenMaiDAL` (ưu đãi thành viên mới) |
| BLL | `XacThucBLL` (gửi / kiểm OTP, giới hạn), `DonDatHangBLL.DatHangVangLai` (tìm hoặc tạo khách theo SĐT, kiểm các luật chống đơn ảo), `KhuyenMaiBLL.TinhTien` thêm tham số mã KH để tính ưu đãi thành viên mới, `TaiKhoanBLL.DangKy` bắt buộc SĐT đã xác thực + `DangKyTuPhienVangLai` |
| WebApi | `POST /api/vang-lai/otp`, `POST /api/vang-lai/xac-thuc`, `POST /api/vang-lai/don-hang`, `GET /api/vang-lai/don-hang`, `GET /api/vang-lai/don-hang/{ma}`, `POST .../{ma}/huy`, `GET .../{ma}/pdf`, `POST /api/vang-lai/tao-tai-khoan`; staff: `GET /api/don-hang?nguon=VANG_LAI`, `GET /api/khach-hang/rui-ro?sdt=` |
| Kiểm thử | Test xUnit cho OTP (hết hạn, sai 5 lần, giới hạn gửi), đặt hàng vãng lai, gắn đơn khi đăng ký, ưu đãi 3 đơn đầu; test API Playwright cho các đường dẫn mới |

### Web bán hàng

| Chỗ | Việc |
|---|---|
| `/thanh-toan` | Bỏ bắt đăng nhập. Chưa đăng nhập thì hiện 2 lựa chọn: **Đăng nhập** (có ưu đãi thành viên) hoặc **Mua không cần tài khoản**. Form vãng lai có ô OTP, đặt hàng gửi kèm giỏ từ trình duyệt |
| Trang mới `/dat-hang-thanh-cong/:ma` | Thông tin đơn, hướng dẫn tra cứu, khung **tạo tài khoản nhanh** + nhắc ưu đãi |
| Trang mới `/tra-cuu-don` | SĐT → OTP → danh sách đơn, chi tiết, hủy, tải PDF. Thêm link trên header và footer |
| `/dang-ky` | Thêm bước OTP cho SĐT, khung giới thiệu ưu đãi, báo số đơn cũ đã được gắn |
| Thanh toán khi đã đăng nhập | Hiện dòng "Ưu đãi thành viên mới: −10% (còn 2 lượt)" |
| Kiểm thử E2E | Mua vãng lai từ đầu đến cuối, tra cứu + hủy, tạo tài khoản nhanh rồi thấy đơn cũ, ưu đãi đơn đầu |

### Báo cáo

Cập nhật `BAO_CAO_WEB_BAN_HANG.md` (mục chức năng, chống đơn ảo, ưu đãi, câu hỏi OTP ở mục 5 được trả lời bằng phương án giả lập) và `database/README.md`.

## 5. Cần chốt

1. **OTP giả lập** (hiện mã trên màn hình khi demo) hay **gửi email thật** qua Gmail SMTP? Đề xuất: giả lập, có sẵn chỗ để thay bằng email.
2. **Ưu đãi**: 10% cho 3 đơn đầu, tối đa 100.000đ / đơn, có được không?
3. **Đăng ký thường cũng bắt OTP SĐT** (để gắn đơn cũ an toàn)? Đề xuất: có.
4. Các con số chống đơn ảo ở mục 2 có cần đổi không?
