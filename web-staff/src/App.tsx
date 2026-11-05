import {
  AppstoreOutlined,
  DownOutlined,
  DashboardOutlined,
  DatabaseOutlined,
  FileTextOutlined,
  InboxOutlined,
  KeyOutlined,
  LogoutOutlined,
  QuestionCircleOutlined,
  RetweetOutlined,
  ShoppingCartOutlined,
  ShoppingOutlined,
  TeamOutlined,
  WalletOutlined,
} from '@ant-design/icons';
import { useQuery } from '@tanstack/react-query';
import { Avatar, Badge, Button, Dropdown, Layout, Menu, Result, Tooltip } from 'antd';
import { lazy, Suspense, useMemo, useState, type ReactNode } from 'react';
import { Navigate, Route, Routes, useLocation, useNavigate } from 'react-router-dom';
import { get } from './api/client';
import type { DoiTra, DonDatHang, HoanTien } from './api/types';
import { ThanhSql } from './components/ThanhSql';
import { BangPhimTat } from './components/BangPhimTat';
import { DoiMatKhau } from './components/DoiMatKhau';
import { usePhien, type NhomQuyen } from './lib/phien';
import { usePhimTat } from './lib/phimTat';
import DangNhapPage from './pages/DangNhapPage';

const TongQuanPage = lazy(() => import('./pages/TongQuanPage'));
const BanHangPage = lazy(() => import('./pages/BanHangPage'));
const DonHangPage = lazy(() => import('./pages/DonHangPage'));
const HoanTienPage = lazy(() => import('./pages/HoanTienPage'));
const HoaDonPage = lazy(() => import('./pages/HoaDonPage'));
const DoiTraPage = lazy(() => import('./pages/DoiTraPage'));
const NhapHangPage = lazy(() => import('./pages/NhapHangPage'));
const SanPhamPage = lazy(() => import('./pages/SanPhamPage'));
const KhachHangPage = lazy(() => import('./pages/KhachHangPage'));
const NhatKySqlPage = lazy(() => import('./pages/NhatKySqlPage'));

interface MucMenu {
  duongDan: string;
  ten: string;
  icon: ReactNode;
  quyen?: NhomQuyen[];
  trang: ReactNode;
  dem?: 'don' | 'doiTra' | 'hoanTien';
}

const MENU: MucMenu[] = [
  { duongDan: '/', ten: 'Tổng quan', icon: <DashboardOutlined />, trang: <TongQuanPage /> },
  { duongDan: '/ban-hang', ten: 'Bán tại quầy', icon: <ShoppingCartOutlined />, quyen: ['ban-hang'], trang: <BanHangPage /> },
  { duongDan: '/don-hang', ten: 'Đơn online', icon: <ShoppingOutlined />, quyen: ['don-online'], trang: <DonHangPage />, dem: 'don' },
  { duongDan: '/hoa-don', ten: 'Hóa đơn', icon: <FileTextOutlined />, quyen: ['ban-hang', 'don-online'], trang: <HoaDonPage /> },
  { duongDan: '/doi-tra', ten: 'Đổi trả', icon: <RetweetOutlined />, quyen: ['ban-hang', 'don-online'], trang: <DoiTraPage />, dem: 'doiTra' },
  { duongDan: '/hoan-tien', ten: 'Hoàn tiền', icon: <WalletOutlined />, quyen: ['ban-hang', 'don-online'], trang: <HoanTienPage />, dem: 'hoanTien' },
  { duongDan: '/nhap-hang', ten: 'Nhập hàng', icon: <InboxOutlined />, quyen: ['kho'], trang: <NhapHangPage /> },
  { duongDan: '/san-pham', ten: 'Sản phẩm & tồn kho', icon: <AppstoreOutlined />, trang: <SanPhamPage /> },
  { duongDan: '/khach-hang', ten: 'Khách hàng', icon: <TeamOutlined />, quyen: ['ban-hang', 'don-online'], trang: <KhachHangPage /> },
  { duongDan: '/nhat-ky-sql', ten: 'Nhật ký SQL', icon: <DatabaseOutlined />, trang: <NhatKySqlPage /> },
];

// Số việc đang chờ hiện trên menu, tự làm mới mỗi 30 giây
function useSoViecCho(bat: boolean) {
  const opt = { enabled: bat, refetchInterval: 30_000 };
  const don = useQuery({ queryKey: ['don-hang', 'cho'], queryFn: () => get<DonDatHang[]>('/api/don-hang', { trangThai: 'CHO_XAC_NHAN' }), ...opt });
  const huy = useQuery({ queryKey: ['don-hang', 'xin-huy'], queryFn: () => get<DonDatHang[]>('/api/don-hang', { yeuCauHuy: 'CHO_DUYET' }), ...opt });
  const dt = useQuery({ queryKey: ['doi-tra', 'cho'], queryFn: () => get<DoiTra[]>('/api/doi-tra', { trangThai: 'CHO_DUYET' }), ...opt });
  const ht = useQuery({ queryKey: ['hoan-tien', 'cho'], queryFn: () => get<HoanTien[]>('/api/hoan-tien', { trangThai: 'CHO_HOAN' }), ...opt });
  return { don: (don.data?.length ?? 0) + (huy.data?.length ?? 0), doiTra: dt.data?.length ?? 0, hoanTien: ht.data?.length ?? 0 };
}

function KhongCoQuyen() {
  return <Result status="403" title="Màn hình này không thuộc chức vụ của bạn" subTitle="Liên hệ quản lý nếu bạn cần quyền này." />;
}

export default function App() {
  const { phien } = usePhien();
  if (!phien) {
    return (
      <Routes>
        <Route path="*" element={<DangNhapPage />} />
      </Routes>
    );
  }
  return <KhungChinh />;
}

function KhungChinh() {
  const { phien, coQuyen, dangXuat } = usePhien();
  const nav = useNavigate();
  const loc = useLocation();
  const [thuGon, setThuGon] = useState(() => window.innerWidth < 1200);
  const [moPhim, setMoPhim] = useState(false);
  const [moDoiMk, setMoDoiMk] = useState(false);
  const menu = useMemo(() => MENU.filter((m) => coQuyen(m.quyen)), [coQuyen]);
  const dem = useSoViecCho(true);

  usePhimTat([
    { phim: '?', chay: () => setMoPhim(true) },
    ...menu.slice(0, 9).map((m, i) => ({ phim: `Alt+${i + 1}`, chay: () => nav(m.duongDan) })),
  ]);

  const khoaChon = menu.find((m) => (m.duongDan === '/' ? loc.pathname === '/' : loc.pathname.startsWith(m.duongDan)))?.duongDan ?? '/';

  return (
    <Layout className="khung-app">
      <a href="#noi-dung" className="bo-qua">
        Bỏ qua tới nội dung
      </a>
      <Layout.Sider
        width={232}
        collapsedWidth={64}
        collapsible
        collapsed={thuGon}
        onCollapse={setThuGon}
        theme="dark"
        className="thanh-ben"
        trigger={null}
      >
        <div className="logo" onClick={() => nav('/')} role="link" aria-label="Về tổng quan">
          <span className="logo-o">
            UNI
            <br />
            QLO
          </span>
          {!thuGon && (
            <span className="logo-chu">
              STAFF
              <small>Cửa hàng</small>
            </span>
          )}
        </div>
        <Menu
          theme="dark"
          mode="inline"
          selectedKeys={[khoaChon]}
          onClick={(e) => nav(e.key)}
          items={menu.map((m, i) => {
            const n = m.dem ? dem[m.dem] : 0;
            return {
              key: m.duongDan,
              icon: m.icon,
              title: `${m.ten} (Alt+${i + 1})`,
              label: (
                <span className="muc-menu">
                  <span>{m.ten}</span>
                  {n > 0 && <Badge count={n} size="small" color="#E60012" />}
                </span>
              ),
            };
          })}
        />
        <Button type="text" className="nut-thu-gon" onClick={() => setThuGon(!thuGon)} aria-label={thuGon ? 'Mở rộng menu' : 'Thu gọn menu'}>
          {thuGon ? '»' : '« Thu gọn'}
        </Button>
      </Layout.Sider>
      <Layout>
        <Layout.Header className="thanh-tren">
          <ThanhSql />
          <div className="thanh-tren-phai">
            <Tooltip title="Phím tắt (?)">
              <Button type="text" icon={<QuestionCircleOutlined />} onClick={() => setMoPhim(true)} aria-label="Bảng phím tắt" />
            </Tooltip>
            <Dropdown
              trigger={['click']}
              menu={{
                items: [
                  { key: 'mk', icon: <KeyOutlined />, label: 'Đổi mật khẩu', onClick: () => setMoDoiMk(true) },
                  { type: 'divider' },
                  { key: 'out', icon: <LogoutOutlined />, label: 'Đăng xuất', danger: true, onClick: dangXuat },
                ],
              }}
            >
              <button className="nguoi-dung" aria-label="Tài khoản">
                <Avatar size={30} style={{ background: '#111', fontSize: 13 }}>
                  {phien!.hoTen.split(' ').pop()?.[0]}
                </Avatar>
                <span className="nguoi-dung-chu">
                  <b>{phien!.hoTen}</b>
                  <small>
                    {phien!.chucVu ?? (phien!.vaiTro === 'ADMIN' ? 'Quản trị' : 'Nhân viên')} · {phien!.maNv}
                  </small>
                </span>
                <DownOutlined aria-hidden style={{ color: '#8c8c8c', fontSize: 11 }} />
              </button>
            </Dropdown>
          </div>
        </Layout.Header>
        <Layout.Content id="noi-dung" className="noi-dung" tabIndex={-1}>
          <Suspense fallback={<div className="dang-tai-trang">Đang tải…</div>}>
            <Routes>
              {MENU.map((m) => (
                <Route key={m.duongDan} path={m.duongDan} element={coQuyen(m.quyen) ? m.trang : <KhongCoQuyen />} />
              ))}
              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
          </Suspense>
        </Layout.Content>
      </Layout>
      <BangPhimTat mo={moPhim} dong={() => setMoPhim(false)} />
      <DoiMatKhau mo={moDoiMk} dong={() => setMoDoiMk(false)} />
    </Layout>
  );
}

export { MENU };
