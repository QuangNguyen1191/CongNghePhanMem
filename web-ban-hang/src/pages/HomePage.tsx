import { useMemo } from 'react';
import { Link } from 'react-router-dom';
import { ArrowRight, RefreshCcw, Truck, Award } from 'lucide-react';
import { ButtonLink, ErrorState, ProductImage, Skeleton } from '../components';
import { useKhuyenMaiDangChay, useLoaiSp } from '../features/catalog/useCatalogMeta';
import { useMauSanPham } from '../features/catalog/useSanPham';
import { ProductGrid, ProductGridSkeleton } from '../features/catalog/ProductGrid';
import { formatNgay } from '../lib/format';
import { quyTac } from '../config/cuaHang';
import { useTitle } from '../lib/useTitle';
import s from './HomePage.module.css';

function SectionHead({ title, to, linkLabel = 'Xem tất cả' }: { title: string; to?: string; linkLabel?: string }) {
  return (
    <div className={s.sectionHead}>
      <h2 className="h2">{title}</h2>
      {to && <Link to={to} className={s.more}>{linkLabel}<ArrowRight size={16} aria-hidden /></Link>}
    </div>
  );
}

export default function HomePage() {
  useTitle();
  const sp = useMauSanPham();
  const loaiQ = useLoaiSp();
  const kmQ = useKhuyenMaiDangChay();

  const { giamGia, moi, theoLoai, noiBat } = useMemo(() => {
    const ds = sp.data ?? [];
    const conHang = ds.filter((m) => m.conHang);
    return {
      giamGia: conHang.filter((m) => m.coGiamGia).sort((a, b) => b.phanTramGiamMax - a.phanTramGiamMax).slice(0, 4),
      moi: [...conHang].sort((a, b) => b.maSpMoiNhat.localeCompare(a.maSpMoiNhat)).slice(0, 8),
      theoLoai: new Map((loaiQ.data ?? []).map((l) => [l.maLoaiSp, ds.filter((m) => m.maLoaiSp === l.maLoaiSp)])),
      // anh lon cua hero: mau co anh, uu tien ao khoac (LSP03) roi mau moi nhat
      noiBat: [...conHang].filter((m) => m.hinhAnh).sort((a, b) => Number(b.maLoaiSp === 'LSP03') - Number(a.maLoaiSp === 'LSP03'))[0],
    };
  }, [sp.data, loaiQ.data]);

  const km = kmQ.data?.[0];

  return (
    <div className={s.home}>
      {/* 1. Chien dich */}
      <section className={s.hero} aria-labelledby="hero-title">
        <div className={`container ${s.heroInner}`}>
          <div className={s.heroText}>
            <p className="label">{km ? 'Ưu đãi đang diễn ra' : 'Bộ sưu tập thường ngày'}</p>
            <h1 id="hero-title" className={s.heroTitle}>{km ? km.tenKm : 'Mặc đơn giản, đẹp mỗi ngày'}</h1>
            <p className={s.heroDesc}>
              {km
                ? `Áp dụng đến hết ${formatNgay(km.ngayKt)}. Ưu đãi được tính tự động ở bước thanh toán.`
                : 'Những thiết kế tối giản, chất liệu dễ chịu, dễ phối cho mọi dịp.'}
            </p>
            <div className={s.heroCta}>
              <ButtonLink to="/san-pham" size="lg">Mua sắm ngay</ButtonLink>
              {giamGia.length > 0 && <ButtonLink to="/san-pham?giamGia=1" size="lg" variant="secondary">Đang giảm giá</ButtonLink>}
            </div>
          </div>
          <div className={s.heroMedia}>
            {sp.isLoading ? <Skeleton ratio="3 / 4" /> : noiBat ? (
              <Link to={`/san-pham/${noiBat.maSpDaiDien}`} aria-label={noiBat.tenSp}>
                <ProductImage src={noiBat.hinhAnh} alt={noiBat.tenSp} priority sizes="(min-width:1024px) 40vw, 100vw" />
              </Link>
            ) : <div className={s.heroBlank} aria-hidden />}
          </div>
        </div>
      </section>

      {/* 2. Danh muc */}
      <section className="container" aria-label="Danh mục">
        <SectionHead title="Mua theo danh mục" to="/san-pham" />
        {loaiQ.error ? <ErrorState error={loaiQ.error} onRetry={() => loaiQ.refetch()} /> : (
          <ul className={s.cats}>
            {(loaiQ.data ?? Array.from({ length: 6 }, (_, i) => ({ maLoaiSp: `sk${i}`, tenLoaiSp: '' }))).map((l) => {
              const ds = theoLoai.get(l.maLoaiSp) ?? [];
              const anh = ds.find((m) => m.hinhAnh)?.hinhAnh;
              return (
                <li key={l.maLoaiSp}>
                  {l.tenLoaiSp ? (
                    <Link to={`/danh-muc/${l.maLoaiSp}`} className={s.cat}>
                      <ProductImage src={anh} alt={l.tenLoaiSp} sizes="(min-width:1024px) 16vw, 33vw" />
                      <span className={s.catName}>{l.tenLoaiSp}</span>
                      {sp.data && <span className="small muted">{ds.length} mẫu</span>}
                    </Link>
                  ) : <Skeleton ratio="3 / 4" />}
                </li>
              );
            })}
          </ul>
        )}
      </section>

      {/* 3. Dang giam gia: chi hien khi co KM san pham con han */}
      {giamGia.length > 0 && (
        <section className="container" aria-label="Đang giảm giá">
          <SectionHead title="Đang giảm giá" to="/san-pham?giamGia=1" />
          <ProductGrid items={giamGia} />
        </section>
      )}

      {/* 4. San pham moi */}
      <section className="container" aria-label="Sản phẩm mới">
        <SectionHead title="Sản phẩm mới" to="/san-pham?sap=moi" />
        {sp.error ? <ErrorState error={sp.error} onRetry={() => sp.refetch()} />
          : sp.isLoading ? <ProductGridSkeleton count={4} />
          : <ProductGrid items={moi} />}
      </section>

      {/* 5. Gioi thieu thuong hieu */}
      <section className={s.brand} aria-labelledby="brand-title">
        <div className={`container ${s.brandInner}`}>
          <p className="label">Về chúng tôi</p>
          <h2 id="brand-title" className={s.brandTitle}>Quần áo tốt là quần áo bạn mặc lại nhiều lần.</h2>
          <p className={s.brandDesc}>
            Chúng tôi chọn chất liệu bền, đường may gọn và màu sắc dễ phối, để mỗi món đồ đi cùng bạn qua nhiều mùa
            thay vì chỉ một lần mặc.
          </p>
        </div>
      </section>

      {/* 6. Dich vu: lay dung quy tac nghiep vu cua backend */}
      <section className="container" aria-label="Dịch vụ khách hàng">
        <ul className={s.services}>
          <li>
            <RefreshCcw size={24} strokeWidth={1.5} aria-hidden />
            <div><p className={s.svTitle}>Đổi trả trong {quyTac.soNgayDoiTra} ngày</p><p className="small muted">Đổi size, đổi màu cùng mẫu hoặc trả hàng hoàn tiền.</p></div>
          </li>
          <li>
            <Truck size={24} strokeWidth={1.5} aria-hidden />
            <div><p className={s.svTitle}>Thanh toán khi nhận hàng</p><p className="small muted">Hỗ trợ COD, chuyển khoản và thanh toán online.</p></div>
          </li>
          <li>
            <Award size={24} strokeWidth={1.5} aria-hidden />
            <div><p className={s.svTitle}>Tích điểm thành viên</p><p className="small muted">Mỗi {quyTac.tienMoiDiem.toLocaleString('vi-VN')}₫ được 1 điểm, lên hạng Bạc, Vàng, Kim cương.</p></div>
          </li>
        </ul>
      </section>
    </div>
  );
}
