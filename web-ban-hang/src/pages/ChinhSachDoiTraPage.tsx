import { Breadcrumb } from '../components';
import { quyTac } from '../config/cuaHang';
import s from './StaticPage.module.css';

// Noi dung lay dung theo quy tac nghiep vu cua backend (DoiTraBLL, QuyTac.SoNgayDoiTra).
export default function ChinhSachDoiTraPage() {
  return (
    <div className={`container ${s.page}`}>
      <Breadcrumb items={[{ label: 'Trang chủ', to: '/' }, { label: 'Chính sách đổi trả' }]} />
      <h1 className={`h1 ${s.title}`}>Chính sách đổi trả</h1>

      <section className={s.section}>
        <h2 className="h2">Điều kiện</h2>
        <ul>
          <li>Trong vòng {quyTac.soNgayDoiTra} ngày kể từ ngày lập hóa đơn.</li>
          <li>Hóa đơn đã được thanh toán.</li>
          <li>Số lượng đổi / trả không vượt quá số lượng đã mua (trừ phần đã đổi trả trước đó).</li>
          <li>Đổi hàng: chỉ đổi sang size hoặc màu khác của cùng mẫu sản phẩm, sản phẩm mới phải đang được bán.</li>
        </ul>
      </section>

      <section className={s.section}>
        <h2 className="h2">Quy trình</h2>
        <ol>
          <li>Vào Tài khoản → Hóa đơn, chọn hóa đơn và bấm "Yêu cầu đổi / trả".</li>
          <li>Chọn sản phẩm, số lượng, lý do và gửi yêu cầu.</li>
          <li>Cửa hàng duyệt yêu cầu. Bạn theo dõi trạng thái tại mục Đổi trả.</li>
          <li>Khi hoàn tất, tiền hoàn (với yêu cầu trả hàng) được ghi trên phiếu đổi trả.</li>
        </ol>
      </section>
    </div>
  );
}
