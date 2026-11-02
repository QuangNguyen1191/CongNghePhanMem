import { lazy } from 'react';
import { createBrowserRouter } from 'react-router-dom';
import { MainLayout } from '../layouts/MainLayout';
import { RequireAuth } from '../features/auth/RequireAuth';

// Tach code theo trang: moi trang 1 file JS rieng, chi tai khi vao.
const NotFoundPage = lazy(() => import('../pages/NotFoundPage'));
const HuongDanSizePage = lazy(() => import('../pages/HuongDanSizePage'));
const ChinhSachDoiTraPage = lazy(() => import('../pages/ChinhSachDoiTraPage'));
const ThietKePage = lazy(() => import('../pages/ThietKePage'));
const HomePage = lazy(() => import('../pages/HomePage'));
const ListingPage = lazy(() => import('../pages/ListingPage'));
const ProductDetailPage = lazy(() => import('../pages/ProductDetailPage'));
const DangNhapPage = lazy(() => import('../pages/DangNhapPage'));
const DangKyPage = lazy(() => import('../pages/DangKyPage'));
const CartPage = lazy(() => import('../pages/CartPage'));
const CheckoutPage = lazy(() => import('../pages/CheckoutPage'));
const AccountLayout = lazy(() => import('../pages/account/AccountLayout'));
const HoSoPage = lazy(() => import('../pages/account/HoSoPage'));
const DoiMatKhauPage = lazy(() => import('../pages/account/DoiMatKhauPage'));
const DonHangListPage = lazy(() => import('../pages/account/DonHangListPage'));
const DonHangDetailPage = lazy(() => import('../pages/account/DonHangDetailPage'));
const HoaDonListPage = lazy(() => import('../pages/account/HoaDonPages').then((m) => ({ default: m.HoaDonListPage })));
const HoaDonDetailPage = lazy(() => import('../pages/account/HoaDonPages').then((m) => ({ default: m.HoaDonDetailPage })));
const DoiTraListPage = lazy(() => import('../pages/account/DoiTraPages').then((m) => ({ default: m.DoiTraListPage })));
const DoiTraNewPage = lazy(() => import('../pages/account/DoiTraPages').then((m) => ({ default: m.DoiTraNewPage })));
const DoiTraDetailPage = lazy(() => import('../pages/account/DoiTraPages').then((m) => ({ default: m.DoiTraDetailPage })));

export const router = createBrowserRouter([
  {
    element: <MainLayout />,
    children: [
      { path: '/', element: <HomePage /> },
      { path: '/san-pham', element: <ListingPage key="tat-ca" cheDo="tat-ca" /> },
      { path: '/danh-muc/:maLoaiSp', element: <ListingPage key="danh-muc" cheDo="danh-muc" /> },
      { path: '/tim-kiem', element: <ListingPage key="tim-kiem" cheDo="tim-kiem" /> },
      { path: '/san-pham/:maSp', element: <ProductDetailPage /> },
      { path: '/dang-nhap', element: <DangNhapPage /> },
      { path: '/dang-ky', element: <DangKyPage /> },
      { path: '/gio-hang', element: <CartPage /> },
      { path: '/thanh-toan', element: <RequireAuth><CheckoutPage /></RequireAuth> },
      {
        path: '/tai-khoan',
        element: <RequireAuth><AccountLayout /></RequireAuth>,
        children: [
          { index: true, element: <HoSoPage /> },
          { path: 'mat-khau', element: <DoiMatKhauPage /> },
          { path: 'don-hang', element: <DonHangListPage /> },
          { path: 'don-hang/:ma', element: <DonHangDetailPage /> },
          { path: 'hoa-don', element: <HoaDonListPage /> },
          { path: 'hoa-don/:ma', element: <HoaDonDetailPage /> },
          { path: 'doi-tra', element: <DoiTraListPage /> },
          { path: 'doi-tra/moi', element: <DoiTraNewPage /> },
          { path: 'doi-tra/:ma', element: <DoiTraDetailPage /> },
        ],
      },
      { path: '/huong-dan-chon-size', element: <HuongDanSizePage /> },
      { path: '/chinh-sach-doi-tra', element: <ChinhSachDoiTraPage /> },
      { path: '/thiet-ke', element: <ThietKePage /> },                          // trang mau design system
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]);
