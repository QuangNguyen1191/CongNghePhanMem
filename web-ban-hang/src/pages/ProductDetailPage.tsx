import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { toast } from 'sonner';
import { ChevronLeft, ChevronRight, RefreshCcw, Ruler, ShoppingBag, Truck } from 'lucide-react';
import { sanPhamApi } from '../api/catalogApi';
import { ApiError } from '../api/apiClient';
import { qk } from '../api/queryKeys';
import type { SanPham } from '../api/types';
import { Breadcrumb, Button, EmptyState, ErrorState, Price, ProductImage, QuantityStepper, Skeleton, LoadingRegion } from '../components';
import { layAnh, useBoAnh } from '../features/catalog/boAnh';
import { maMau, soSanhSize } from '../features/catalog/gomSanPham';
import { useGiaKhuyenMai, useMauSanPham } from '../features/catalog/useSanPham';
import { ProductGrid } from '../features/catalog/ProductGrid';
import { docDaXem, ghiDaXem } from '../features/catalog/daXem';
import { useSoLuongGio, useThaoTacGio } from '../features/cart/useGioHang';
import { quyTac } from '../config/cuaHang';
import { usePhimTat } from '../features/phimTat/usePhimTat';
import { useTitle } from '../lib/useTitle';
import s from './ProductDetailPage.module.css';

function Gallery({ anhs, ten }: { anhs: string[]; ten: string }) {
  const [i, setI] = useState(0);
  useEffect(() => setI(0), [anhs]);
  const n = anhs.length;
  const di = (b: number) => setI((x) => (x + b + n) % n);
  // phim tat: mui ten trai / phai doi anh
  usePhimTat('ArrowLeft', () => di(-1), n > 1);
  usePhimTat('ArrowRight', () => di(1), n > 1);
  if (n === 0) return <ProductImage src={null} alt={ten} />;
  return (
    <div className={s.gallery}>
      <div className={s.thumbs} role="tablist" aria-label="Ảnh sản phẩm">
        {anhs.map((a, k) => (
          <button
            key={a}
            type="button"
            role="tab"
            aria-selected={k === i}
            aria-label={`Ảnh ${k + 1} / ${n}`}
            className={`${s.thumb} ${k === i ? s.thumbOn : ''}`}
            onClick={() => setI(k)}
          >
            <img src={a} alt="" loading="lazy" decoding="async" onError={(e) => { e.currentTarget.style.visibility = 'hidden'; }} />
          </button>
        ))}
      </div>
      <div className={s.main} role="tabpanel" aria-live="polite">
        <ProductImage src={anhs[i]} alt={`${ten} - ảnh ${i + 1}`} priority={i === 0} sizes="(min-width:1024px) 50vw, 100vw" />
        {n > 1 && (
          <>
            <button type="button" className={`${s.nav} ${s.prev}`} onClick={() => di(-1)} aria-label="Ảnh trước"><ChevronLeft size={20} aria-hidden /></button>
            <button type="button" className={`${s.nav} ${s.next}`} onClick={() => di(1)} aria-label="Ảnh sau"><ChevronRight size={20} aria-hidden /></button>
            <span className={s.counter} aria-hidden>{i + 1} / {n}</span>
          </>
        )}
      </div>
    </div>
  );
}

function DetailSkeleton() {
  return (
    <LoadingRegion label="Đang tải sản phẩm">
      <div className={`container ${s.layout}`}>
        <Skeleton ratio="3 / 4" />
        <div className={s.info}>
          <Skeleton h={14} w="30%" />
          <Skeleton h={32} w="80%" />
          <Skeleton h={24} w="35%" />
          <Skeleton h={120} />
          <Skeleton h={48} />
        </div>
      </div>
    </LoadingRegion>
  );
}

export default function ProductDetailPage() {
  const { maSp = '' } = useParams();
  const navigate = useNavigate();
  const spQ = useQuery({ queryKey: qk.sanPham(maSp), queryFn: () => sanPhamApi.layTheoMa(maSp), placeholderData: keepPreviousData });
  const btQ = useQuery({ queryKey: qk.bienThe(maSp), queryFn: () => sanPhamApi.layBienThe(maSp), placeholderData: keepPreviousData });
  const kmQ = useGiaKhuyenMai();
  const { data: boAnh } = useBoAnh();
  const tatCa = useMauSanPham();
  const { them } = useThaoTacGio();
  const soLuongGio = useSoLuongGio();

  const sp = spQ.data;
  useTitle(sp?.tenSp);

  // bien the con ban; SKU dang xem van giu de hien thong bao neu da ngung ban
  const bienThes = useMemo(() => (btQ.data ?? []).filter((b) => b.trangThai === 'DANG_BAN'), [btQ.data]);
  const mauSacs = useMemo(() => {
    const ds: { ten: string; anh: string | null; conHang: boolean }[] = [];
    for (const b of bienThes) {
      const ten = b.mauSac ?? '';
      const cu = ds.find((m) => m.ten === ten);
      if (cu) { cu.conHang ||= b.soLuong > 0; if (!cu.anh) cu.anh = b.hinhAnh ?? null; }
      else ds.push({ ten, anh: b.hinhAnh ?? null, conHang: b.soLuong > 0 });
    }
    return ds;
  }, [bienThes]);
  const kichCos = useMemo(() => [...new Set(bienThes.map((b) => b.kichCo ?? '').filter(Boolean))].sort(soSanhSize), [bienThes]);

  const [mau, setMau] = useState<string | null>(null);
  const [size, setSize] = useState<string | null>(null);
  const [soLuong, setSoLuong] = useState(1);
  const [loiChon, setLoiChon] = useState('');

  // doi sang mau san pham khac (vd bam SP lien quan) -> chon lai tu dau
  const tenHienTai = sp?.tenSp;
  useEffect(() => { setMau(null); setSize(null); setSoLuong(1); setLoiChon(''); }, [tenHienTai]);
  useEffect(() => {
    if (!sp || mau !== null) return;
    setMau(sp.mauSac ?? '');
    if (kichCos.length === 1) setSize(kichCos[0]);
  }, [sp, mau, kichCos]);
  useEffect(() => { if (sp) ghiDaXem(sp.tenSp); }, [sp]);

  const timSku = (m: string | null, k: string | null) =>
    bienThes.find((b) => (b.mauSac ?? '') === (m ?? '') && (b.kichCo ?? '') === (k ?? ''));
  const skuChon: SanPham | undefined = size ? timSku(mau, size) : undefined;
  const skuGia = skuChon ?? bienThes.find((b) => (b.mauSac ?? '') === mau) ?? sp;
  const giaKm = skuGia ? kmQ.data?.[skuGia.maSp] : undefined;
  const ton = skuChon?.soLuong ?? 0;

  // gioi han so luong theo ton cua SKU dang chon
  useEffect(() => { if (skuChon && soLuong > Math.max(1, skuChon.soLuong)) setSoLuong(Math.max(1, skuChon.soLuong)); }, [skuChon, soLuong]);

  const chonBienThe = (m: string, k: string | null) => {
    setMau(m);
    setSize(k);
    setLoiChon('');
    const sku = k ? timSku(m, k) : bienThes.find((b) => (b.mauSac ?? '') === m);
    // URL theo SKU dang chon de chia se / tai lai trang van dung
    if (sku && sku.maSp !== maSp) navigate(`/san-pham/${sku.maSp}`, { replace: true, preventScrollReset: true });
  };

  const anhMau = mauSacs.find((m) => m.ten === mau)?.anh ?? sp?.hinhAnh ?? null;
  const anhs = useMemo(() => layAnh(boAnh, anhMau), [boAnh, anhMau]);

  const lienQuan = useMemo(() => (tatCa.data ?? [])
    .filter((m) => m.maLoaiSp === sp?.maLoaiSp && m.tenSp !== sp?.tenSp && m.conHang).slice(0, 4), [tatCa.data, sp]);
  const daXem = useMemo(() => {
    const ten = docDaXem().filter((t) => t !== sp?.tenSp);
    return ten.map((t) => tatCa.data?.find((m) => m.tenSp === t)).filter((m) => !!m).slice(0, 4);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tatCa.data, sp?.tenSp]);

  const kiemTraChon = () => {
    if (!skuChon) { setLoiChon('Vui lòng chọn kích cỡ.'); document.getElementById('chon-size')?.focus(); return false; }
    if (skuChon.soLuong <= 0) { setLoiChon('Kích cỡ này đã hết hàng, vui lòng chọn kích cỡ khác.'); return false; }
    return true;
  };

  const themVaoGio = async (muaNgay: boolean) => {
    if (!kiemTraChon() || !skuChon) return;
    try {
      await them.mutateAsync({ sku: skuChon, soLuong });
    } catch {
      return; // loi da hien toast (MutationCache)
    }
    if (muaNgay) {
      navigate('/thanh-toan'); // v3: chua dang nhap van thanh toan duoc (trang thanh toan co nut Dang nhap)
      return;
    }
    toast.success(`Đã thêm vào giỏ: ${skuChon.tenSp} (${[skuChon.mauSac, skuChon.kichCo].filter(Boolean).join(', ')})`, {
      action: { label: 'Xem giỏ', onClick: () => navigate('/gio-hang') },
    });
  };

  // phim tat 'a': them vao gio (bao loi neu chua chon size)
  usePhimTat('a', () => { if (!them.isPending && bienThes.length > 0) themVaoGio(false); }, !!sp);

  if (spQ.isLoading || (btQ.isLoading && !btQ.data)) return <DetailSkeleton />;
  if (spQ.error) {
    if (spQ.error instanceof ApiError && spQ.error.khongTimThay) {
      return (
        <div className="container">
          <EmptyState as="h1" title="Không tìm thấy sản phẩm" description="Sản phẩm không tồn tại hoặc đường dẫn bị sai." action={<Link to="/san-pham" className="link">Xem tất cả sản phẩm</Link>} />
        </div>
      );
    }
    return <div className="container"><ErrorState error={spQ.error} onRetry={() => spQ.refetch()} /></div>;
  }
  if (!sp) return null;

  const ngungBan = bienThes.length === 0;
  const coChon = !!skuChon;

  return (
    <div className={s.page}>
      <div className="container">
        <Breadcrumb items={[
          { label: 'Trang chủ', to: '/' },
          { label: sp.tenLoaiSp ?? 'Sản phẩm', to: `/danh-muc/${sp.maLoaiSp}` },
          { label: sp.tenSp },
        ]} />
      </div>

      <div className={`container ${s.layout}`}>
        <Gallery anhs={anhs} ten={sp.tenSp} />

        <div className={s.info}>
          <p className="label muted">{sp.tenLoaiSp}</p>
          <h1 className={s.title}>{sp.tenSp}</h1>
          {skuGia && <Price goc={skuGia.donGia} km={giaKm} size="lg" />}
          {giaKm != null && giaKm < (skuGia?.donGia ?? 0) && (
            <p className={s.saleNote}>Giá ưu đãi đến khi hết chương trình khuyến mãi</p>
          )}

          {ngungBan ? (
            <div className={s.notice} role="status">Sản phẩm này đã ngừng bán.</div>
          ) : (
            <>
              {mauSacs.some((m) => m.ten) && (
                <fieldset className={s.group}>
                  <legend className={s.legend}>Màu sắc: <strong>{mau || '—'}</strong></legend>
                  <div className={s.swatches}>
                    {mauSacs.map((m) => (
                      <label key={m.ten} className={`${s.swatch} ${m.ten === mau ? s.on : ''} ${!m.conHang ? s.het : ''}`} title={m.ten}>
                        <input
                          type="radio"
                          name="mau"
                          className="sr-only"
                          checked={m.ten === mau}
                          onChange={() => chonBienThe(m.ten, size && (timSku(m.ten, size)?.soLuong ?? 0) > 0 ? size : null)}
                        />
                        <span className={s.dot} style={{ background: maMau(m.ten) }} aria-hidden />
                        <span className="sr-only">{m.ten}{!m.conHang ? ' (hết hàng)' : ''}</span>
                      </label>
                    ))}
                  </div>
                </fieldset>
              )}

              {kichCos.length > 0 && (
                <fieldset className={s.group} id="chon-size" tabIndex={-1}>
                  <legend className={`${s.legend} ${s.legendRow}`}>
                    <span>Kích cỡ: <strong>{size ?? 'Chưa chọn'}</strong></span>
                    <Link to="/huong-dan-chon-size" className={s.sizeGuide}><Ruler size={14} aria-hidden />Hướng dẫn chọn size</Link>
                  </legend>
                  <div className={s.sizes}>
                    {kichCos.map((k) => {
                      const b = timSku(mau, k);
                      const het = !b || b.soLuong <= 0;
                      return (
                        <label key={k} className={`${s.size} ${k === size ? s.on : ''} ${het ? s.het : ''}`}>
                          <input
                            type="radio"
                            name="size"
                            className="sr-only"
                            checked={k === size}
                            disabled={het} // size het hang: gach cheo, khong chon duoc
                            onChange={() => chonBienThe(mau ?? '', k)}
                          />
                          <span>{k}</span>
                          {het && <span className="sr-only"> (hết hàng)</span>}
                        </label>
                      );
                    })}
                  </div>
                </fieldset>
              )}

              <p className={s.stock} role="status" aria-live="polite">
                {!coChon ? <span className="muted">Chọn kích cỡ để xem tình trạng hàng</span>
                  : ton <= 0 ? <span className={s.out}>Hết hàng</span>
                  : ton < quyTac.nguongTonThap ? <span className={s.low}>Chỉ còn {ton} sản phẩm</span>
                  : <span className={s.ok}>Còn hàng</span>}
              </p>

              <div className={s.qtyRow}>
                <QuantityStepper value={soLuong} onChange={setSoLuong} min={1} max={Math.max(1, Math.min(ton || 1, 99))} disabled={!coChon || ton <= 0} />
              </div>

              {loiChon && <p className={s.error} role="alert">{loiChon}</p>}

              <div className={s.cta}>
                <Button size="lg" block iconLeft={<ShoppingBag size={18} aria-hidden />} loading={them.isPending} onClick={() => themVaoGio(false)} disabled={coChon && ton <= 0}>
                  Thêm vào giỏ hàng
                </Button>
                <Button size="lg" block variant="secondary" onClick={() => themVaoGio(true)} disabled={them.isPending || (coChon && ton <= 0)}>
                  Mua ngay
                </Button>
              </div>
              {soLuongGio > 0 && (
                <p className="small muted">Mua ngay sẽ thanh toán cùng {soLuongGio} sản phẩm đang có trong giỏ hàng.</p>
              )}
              <p className="small muted">Mẹo: phím <kbd>a</kbd> để thêm vào giỏ, <kbd>←</kbd> <kbd>→</kbd> để xem ảnh, <kbd>?</kbd> để xem mọi phím tắt.</p>
            </>
          )}

          <ul className={s.service}>
            <li><Truck size={18} aria-hidden /><span>Thanh toán khi nhận hàng (COD), chuyển khoản hoặc online</span></li>
            <li><RefreshCcw size={18} aria-hidden /><span>Đổi size, đổi màu hoặc trả hàng trong {quyTac.soNgayDoiTra} ngày. <Link to="/chinh-sach-doi-tra" className="link">Xem chính sách</Link></span></li>
          </ul>

          <div className={s.details}>
            {sp.moTa && (
              <details open>
                <summary>Mô tả sản phẩm</summary>
                <p>{sp.moTa}</p>
              </details>
            )}
            {sp.chatLieu && (
              <details>
                <summary>Chất liệu</summary>
                <p>{sp.chatLieu}</p>
              </details>
            )}
            <details>
              <summary>Thông tin biến thể</summary>
              <p>Mã sản phẩm: <span className="num">{skuChon?.maSp ?? sp.maSp}</span>{skuChon ? ` · ${[skuChon.mauSac, skuChon.kichCo].filter(Boolean).join(' / ')}` : ''}</p>
            </details>
          </div>
        </div>
      </div>

      {lienQuan.length > 0 && (
        <section className={`container ${s.more}`} aria-labelledby="lien-quan">
          <h2 id="lien-quan" className="h2">Sản phẩm cùng danh mục</h2>
          <ProductGrid items={lienQuan} />
        </section>
      )}
      {daXem.length > 0 && (
        <section className={`container ${s.more}`} aria-labelledby="da-xem">
          <h2 id="da-xem" className="h2">Đã xem gần đây</h2>
          <ProductGrid items={daXem} />
        </section>
      )}
    </div>
  );
}
