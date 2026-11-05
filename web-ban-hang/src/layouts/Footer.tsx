import { Link } from 'react-router-dom';
import { useLoaiSp } from '../features/catalog/useCatalogMeta';
import { cuaHang, quyTac } from '../config/cuaHang';
import { SU_KIEN_MO_BANG } from '../features/phimTat/PhimTatToanCuc';
import s from './Footer.module.css';

export function Footer() {
  const { data: loaiSps } = useLoaiSp();
  const lienHe = [
    cuaHang.diaChi && { k: 'Địa chỉ', v: cuaHang.diaChi },
    cuaHang.hotline && { k: 'Hotline', v: cuaHang.hotline },
    cuaHang.email && { k: 'Email', v: cuaHang.email },
    cuaHang.gioMoCua && { k: 'Giờ mở cửa', v: cuaHang.gioMoCua },
  ].filter(Boolean) as { k: string; v: string }[];

  return (
    <footer className={s.footer}>
      <div className={`container ${s.grid}`}>
        <section>
          <h2 className={`label ${s.heading}`}>Mua sắm</h2>
          <ul className={s.list}>
            <li><Link to="/san-pham">Tất cả sản phẩm</Link></li>
            {(loaiSps ?? []).map((l) => <li key={l.maLoaiSp}><Link to={`/danh-muc/${l.maLoaiSp}`}>{l.tenLoaiSp}</Link></li>)}
          </ul>
        </section>
        <section>
          <h2 className={`label ${s.heading}`}>Hỗ trợ khách hàng</h2>
          <ul className={s.list}>
            <li><Link to="/huong-dan-chon-size">Hướng dẫn chọn size</Link></li>
            <li><Link to="/chinh-sach-doi-tra">Chính sách đổi trả</Link></li>
            <li><Link to="/tra-cuu-don">Tra cứu đơn hàng</Link></li>
            <li>
              <button type="button" className={s.linkBtn} onClick={() => window.dispatchEvent(new Event(SU_KIEN_MO_BANG))}>
                Phím tắt bàn phím <kbd className={s.kbd}>?</kbd>
              </button>
            </li>
          </ul>
        </section>
        <section>
          <h2 className={`label ${s.heading}`}>Chính sách</h2>
          <ul className={s.list}>
            <li>Đổi trả trong {quyTac.soNgayDoiTra} ngày</li>
            <li>Thanh toán: COD, chuyển khoản, online</li>
            <li>Tích {(quyTac.tienMoiDiem / 1000).toLocaleString('vi-VN')}.000₫ = 1 điểm thành viên</li>
          </ul>
        </section>
        <section>
          <h2 className={`label ${s.heading}`}>{cuaHang.ten}</h2>
          {lienHe.length > 0 ? (
            <dl className={s.contact}>
              {lienHe.map((x) => <div key={x.k}><dt>{x.k}</dt><dd>{x.v}</dd></div>)}
            </dl>
          ) : (
            <p className={s.note}>Trang phục giản dị, chất lượng cho mỗi ngày.</p>
          )}
        </section>
      </div>
      <div className={`container ${s.bottom}`}>
        <p>© {new Date().getFullYear()} {cuaHang.ten}. Đồ án môn Công nghệ phần mềm, không dùng cho mục đích thương mại.</p>
      </div>
    </footer>
  );
}
