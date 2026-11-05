import { useMemo, useState } from 'react';
import { useParams, useSearchParams } from 'react-router-dom';
import { SlidersHorizontal, X } from 'lucide-react';
import { Breadcrumb, Button, ButtonLink, Drawer, EmptyState, ErrorState, type MucBreadcrumb } from '../components';
import { useLoaiSp } from '../features/catalog/useCatalogMeta';
import { useMauSanPham, useTimKiem } from '../features/catalog/useSanPham';
import { FilterPanel, type KhoaMang } from '../features/catalog/FilterPanel';
import { ProductGrid, ProductGridSkeleton } from '../features/catalog/ProductGrid';
import { KHOANG_GIA, NHAN_SAP_XEP, demBoLoc, docTieuChi, giaTriBoLoc, locVaSapXep, type SapXep } from '../features/catalog/boLoc';
import { useTitle } from '../lib/useTitle';
import { usePhimTat } from '../features/phimTat/usePhimTat';
import NotFoundPage from './NotFoundPage';
import s from './ListingPage.module.css';

const MOI_TRANG = 12;

type CheDo = 'tat-ca' | 'danh-muc' | 'tim-kiem';

export default function ListingPage({ cheDo }: { cheDo: CheDo }) {
  const { maLoaiSp } = useParams();
  const [params, setParams] = useSearchParams();
  const tuKhoaNhap = cheDo === 'tim-kiem' ? (params.get('q') ?? '').trim().replace(/\s+/g, ' ') : '';
  // dac ta W2.1: tu khoa it nhat 2 ky tu
  const tuKhoa = tuKhoaNhap.length >= 2 ? tuKhoaNhap : '';
  const [moLoc, setMoLoc] = useState(false);

  const loaiQ = useLoaiSp();
  const tatCa = useMauSanPham();
  const tim = useTimKiem(tuKhoa);

  const loai = loaiQ.data?.find((l) => l.maLoaiSp === maLoaiSp);
  const tieuChi = docTieuChi(params);
  const trang = Math.max(1, Number(params.get('trang')) || 1);

  // Danh sach goc theo che do (truoc khi loc)
  const goc = useMemo(() => {
    if (!tatCa.data) return undefined;
    if (cheDo === 'danh-muc') return tatCa.data.filter((m) => m.maLoaiSp === maLoaiSp);
    if (cheDo === 'tim-kiem') {
      if (!tuKhoa) return [];
      if (!tim.data) return undefined;
      // API tim theo tung SKU (ten, ma, mau); hien du mau san pham chua SKU khop
      const ten = new Set(tim.data.map((m) => m.tenSp));
      return tatCa.data.filter((m) => ten.has(m.tenSp));
    }
    return tatCa.data;
  }, [tatCa.data, tim.data, cheDo, maLoaiSp, tuKhoa]);

  const ketQua = useMemo(() => (goc ? locVaSapXep(goc, tieuChi) : []), [goc, params]); // eslint-disable-line react-hooks/exhaustive-deps
  const giaTri = useMemo(() => giaTriBoLoc(goc ?? []), [goc]);
  const hienThi = ketQua.slice(0, trang * MOI_TRANG);
  const soLoc = demBoLoc(tieuChi);

  const tieuDe = cheDo === 'danh-muc' ? loai?.tenLoaiSp : cheDo === 'tim-kiem' ? `Kết quả cho “${tuKhoa}”` : tieuChi.giamGia ? 'Đang giảm giá' : 'Tất cả sản phẩm';
  useTitle(cheDo === 'tim-kiem' ? `Tìm “${tuKhoa}”` : tieuDe);

  const capNhat = (sua: (p: URLSearchParams) => void) => {
    const p = new URLSearchParams(params);
    sua(p);
    p.delete('trang'); // doi bo loc thi ve trang 1
    setParams(p, { replace: true, preventScrollReset: true });
  };
  const toggle = (khoa: KhoaMang, v: string) => capNhat((p) => {
    const ds = new Set((p.get(khoa) ?? '').split(',').filter(Boolean));
    if (ds.has(v)) ds.delete(v); else ds.add(v);
    if (ds.size) p.set(khoa, [...ds].join(',')); else p.delete(khoa);
  });
  const datCo = (khoa: 'conHang' | 'giamGia', v: boolean) => capNhat((p) => { if (v) p.set(khoa, '1'); else p.delete(khoa); });
  const xoaHet = () => capNhat((p) => { ['loai', 'mau', 'size', 'gia', 'conHang', 'giamGia'].forEach((k) => p.delete(k)); });
  const sapXep = (v: SapXep) => capNhat((p) => { if (v === 'noi-bat') p.delete('sap'); else p.set('sap', v); });
  const xemThem = () => { const p = new URLSearchParams(params); p.set('trang', String(trang + 1)); setParams(p, { replace: true, preventScrollReset: true }); };

  // phim tat 'f': desktop dua con tro vao bo loc ben trai, mobile mo ngan bo loc
  usePhimTat('f', () => {
    const ben = document.querySelector<HTMLElement>('[data-bo-loc-ben]');
    if (ben && ben.offsetParent !== null) ben.querySelector<HTMLElement>('input, button, select')?.focus();
    else setMoLoc(true);
  });

  if (cheDo === 'danh-muc' && loaiQ.data && !loai) return <NotFoundPage />;

  const breadcrumb: MucBreadcrumb[] = [{ label: 'Trang chủ', to: '/' }];
  if (cheDo === 'danh-muc') breadcrumb.push({ label: 'Sản phẩm', to: '/san-pham' }, { label: loai?.tenLoaiSp ?? '…' });
  else if (cheDo === 'tim-kiem') breadcrumb.push({ label: 'Tìm kiếm' });
  else breadcrumb.push({ label: 'Sản phẩm' });

  const loi = tatCa.error ?? tim.error;
  const dangTai = !loi && (goc === undefined || tatCa.isLoading);

  // Chip bo loc dang chon
  const chips: { nhan: string; bo: () => void }[] = [
    ...tieuChi.loai.map((v) => ({ nhan: loaiQ.data?.find((l) => l.maLoaiSp === v)?.tenLoaiSp ?? v, bo: () => toggle('loai', v) })),
    ...tieuChi.mau.map((v) => ({ nhan: v, bo: () => toggle('mau', v) })),
    ...tieuChi.size.map((v) => ({ nhan: `Size ${v}`, bo: () => toggle('size', v) })),
    ...tieuChi.gia.map((v) => ({ nhan: KHOANG_GIA.find((k) => k.ma === v)?.nhan ?? v, bo: () => toggle('gia', v) })),
    ...(tieuChi.conHang ? [{ nhan: 'Còn hàng', bo: () => datCo('conHang', false) }] : []),
    ...(tieuChi.giamGia ? [{ nhan: 'Đang giảm giá', bo: () => datCo('giamGia', false) }] : []),
  ];

  const boLoc = (
    <FilterPanel
      tieuChi={tieuChi}
      giaTri={giaTri}
      loaiSps={cheDo === 'danh-muc' ? undefined : loaiQ.data}
      onToggle={toggle}
      onCo={datCo}
    />
  );

  return (
    <div className={`container ${s.page}`}>
      <Breadcrumb items={breadcrumb} />
      <header className={s.head}>
        <h1 className="h1">{tieuDe ?? ' '}</h1>
        {!dangTai && !loi && <p className="muted num" aria-live="polite">{ketQua.length} sản phẩm</p>}
      </header>

      {cheDo === 'tim-kiem' && !tuKhoa ? (
        <EmptyState
          title={tuKhoaNhap ? 'Vui lòng nhập ít nhất 2 ký tự' : 'Nhập từ khóa để tìm sản phẩm'}
          description="Bạn có thể tìm theo tên, mã sản phẩm hoặc màu sắc."
          action={<ButtonLink to="/san-pham" variant="secondary">Xem tất cả sản phẩm</ButtonLink>}
        />
      ) : (
        <div className={s.layout}>
          <aside className={s.sidebar} aria-label="Bộ lọc sản phẩm" data-bo-loc-ben>{boLoc}</aside>

          <section className={s.main} aria-label="Danh sách sản phẩm">
            <div className={s.toolbar}>
              <Button variant="secondary" className={s.filterBtn} iconLeft={<SlidersHorizontal size={16} aria-hidden />} onClick={() => setMoLoc(true)}>
                Bộ lọc{soLoc > 0 ? ` (${soLoc})` : ''}
              </Button>
              <label className={s.sort}>
                <span className="small muted">Sắp xếp</span>
                <select value={tieuChi.sap} onChange={(e) => sapXep(e.target.value as SapXep)}>
                  {(Object.keys(NHAN_SAP_XEP) as SapXep[]).map((k) => <option key={k} value={k}>{NHAN_SAP_XEP[k]}</option>)}
                </select>
              </label>
            </div>

            {chips.length > 0 && (
              <ul className={s.chips} aria-label="Bộ lọc đang chọn">
                {chips.map((c) => (
                  <li key={c.nhan}>
                    <button type="button" className={s.chip} onClick={c.bo} aria-label={`Bỏ lọc ${c.nhan}`}>
                      {c.nhan}<X size={14} aria-hidden />
                    </button>
                  </li>
                ))}
                <li><Button variant="link" onClick={xoaHet}>Xóa tất cả</Button></li>
              </ul>
            )}

            {loi ? (
              <ErrorState error={loi} onRetry={() => { tatCa.refetch(); if (tuKhoa) tim.refetch(); }} />
            ) : dangTai ? (
              <ProductGridSkeleton cols={3} count={6} />
            ) : ketQua.length === 0 ? (
              goc && goc.length > 0 ? (
                <EmptyState title="Không có sản phẩm phù hợp" description="Thử bỏ bớt bộ lọc để xem thêm sản phẩm." action={<Button variant="secondary" onClick={xoaHet}>Xóa bộ lọc</Button>} />
              ) : cheDo === 'tim-kiem' ? (
                <EmptyState title={`Không tìm thấy “${tuKhoa}”`} description="Kiểm tra lại chính tả hoặc thử từ khóa ngắn hơn, ví dụ “áo thun”, “jean”." action={<ButtonLink to="/san-pham" variant="secondary">Xem tất cả sản phẩm</ButtonLink>} />
              ) : (
                <EmptyState title="Danh mục chưa có sản phẩm" action={<ButtonLink to="/san-pham" variant="secondary">Xem tất cả sản phẩm</ButtonLink>} />
              )
            ) : (
              <>
                <ProductGrid items={hienThi} cols={3} />
                <div className={s.more}>
                  <p className="small muted num">Đang xem {hienThi.length} / {ketQua.length} sản phẩm</p>
                  {hienThi.length < ketQua.length && <Button variant="secondary" onClick={xemThem}>Xem thêm</Button>}
                </div>
              </>
            )}
          </section>
        </div>
      )}

      <Drawer
        open={moLoc}
        onClose={() => setMoLoc(false)}
        title="Bộ lọc"
        side="right"
        footer={
          <div className={s.drawerFoot}>
            <Button variant="secondary" onClick={xoaHet} disabled={soLoc === 0}>Xóa lọc</Button>
            <Button onClick={() => setMoLoc(false)}>Xem {ketQua.length} sản phẩm</Button>
          </div>
        }
      >
        {boLoc}
      </Drawer>
    </div>
  );
}
