import { useEffect, useRef, useState, type FormEvent } from 'react';
import { Link, NavLink, useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { ChevronDown, Menu, Search, ShoppingBag, User } from 'lucide-react';
import { useLoaiSp, useKhuyenMaiDangChay } from '../features/catalog/useCatalogMeta';
import { useSession } from '../features/auth/useSession';
import { useSoLuongGio } from '../features/cart/useGioHang';
import { MegaMenu } from './MegaMenu';
import { MobileMenu } from './MobileMenu';
import s from './Header.module.css';

function AnnouncementBar() {
  const { data } = useKhuyenMaiDangChay();
  const km = data?.[0];
  if (!km) return null;
  return (
    <div className={s.announce}>
      <p className="container">{km.tenKm}{data.length > 1 ? ` · và ${data.length - 1} ưu đãi khác` : ''}</p>
    </div>
  );
}

function SearchForm({ id, autoFocus }: { id: string; autoFocus?: boolean }) {
  const [params] = useSearchParams();
  const { pathname } = useLocation();
  const [q, setQ] = useState(pathname === '/tim-kiem' ? params.get('q') ?? '' : '');
  const navigate = useNavigate();
  const submit = (e: FormEvent) => {
    e.preventDefault();
    const t = q.trim();
    if (t) navigate(`/tim-kiem?q=${encodeURIComponent(t)}`);
  };
  return (
    <form role="search" className={s.search} onSubmit={submit}>
      <label htmlFor={id} className="sr-only">Tìm kiếm sản phẩm</label>
      <Search size={18} aria-hidden className={s.searchIcon} />
      <input
        id={id}
        type="search"
        value={q}
        onChange={(e) => setQ(e.target.value)}
        placeholder="Tìm sản phẩm, màu sắc…"
        autoComplete="off"
        enterKeyHint="search"
        autoFocus={autoFocus}
      />
    </form>
  );
}

export function Header() {
  const { data: loaiSps } = useLoaiSp();
  const phien = useSession();
  const soLuongGio = useSoLuongGio();
  const [megaMo, setMegaMo] = useState(false);
  const [mobileMo, setMobileMo] = useState(false);
  const { pathname } = useLocation();
  const vungMega = useRef<HTMLDivElement>(null);

  // doi trang thi dong menu
  useEffect(() => { setMegaMo(false); setMobileMo(false); }, [pathname]);

  return (
    <header className={s.header}>
      <a href="#noi-dung" className="skip-link">Bỏ qua tới nội dung</a>
      <AnnouncementBar />
      <div className={s.bar}>
        <div className={`container ${s.row}`}>
          <button type="button" className={`${s.icon} ${s.onlyMobile}`} onClick={() => setMobileMo(true)} aria-label="Mở menu">
            <Menu size={22} aria-hidden />
          </button>

          <Link to="/" className={s.logo} aria-label="UNIQLO - Trang chủ">UNIQLO</Link>

          <nav aria-label="Danh mục" className={s.nav}>
            <div
              ref={vungMega}
              className={s.megaWrap}
              onMouseEnter={() => setMegaMo(true)}
              onMouseLeave={() => setMegaMo(false)}
              onBlur={(e) => { if (!vungMega.current?.contains(e.relatedTarget as Node)) setMegaMo(false); }}
            >
              <button
                type="button"
                className={s.navLink}
                aria-expanded={megaMo}
                aria-controls="mega-menu"
                onClick={() => setMegaMo((v) => !v)}
                onKeyDown={(e) => { if (e.key === 'Escape') setMegaMo(false); }}
              >
                Sản phẩm <ChevronDown size={14} aria-hidden />
              </button>
              <MegaMenu id="mega-menu" open={megaMo} loaiSps={loaiSps ?? []} onClose={() => setMegaMo(false)} />
            </div>
            {(loaiSps ?? []).map((l) => (
              <NavLink key={l.maLoaiSp} to={`/danh-muc/${l.maLoaiSp}`} className={({ isActive }) => `${s.navLink} ${s.catLink} ${isActive ? s.active : ''}`}>
                {l.tenLoaiSp}
              </NavLink>
            ))}
          </nav>

          <div className={s.actions}>
            <div className={s.onlyDesktop}><SearchForm id="tim-desktop" /></div>
            <Link to={phien ? '/tai-khoan' : '/dang-nhap'} className={s.icon} aria-label={phien ? `Tài khoản của ${phien.hoTen}` : 'Đăng nhập'}>
              <User size={22} aria-hidden />
            </Link>
            <Link to="/gio-hang" className={s.icon} aria-label={soLuongGio > 0 ? `Giỏ hàng, ${soLuongGio} sản phẩm` : 'Giỏ hàng'}>
              <ShoppingBag size={22} aria-hidden />
              {soLuongGio > 0 && <span className={s.count} aria-hidden>{soLuongGio > 99 ? '99+' : soLuongGio}</span>}
            </Link>
          </div>
        </div>
        <div className={`container ${s.mobileSearch}`}><SearchForm id="tim-mobile" /></div>
      </div>
      <MobileMenu open={mobileMo} onClose={() => setMobileMo(false)} loaiSps={loaiSps ?? []} daDangNhap={!!phien} />
    </header>
  );
}
