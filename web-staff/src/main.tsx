import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { App as AntApp, ConfigProvider } from 'antd';
import viVN from 'antd/locale/vi_VN';
import dayjs from 'dayjs';
import 'dayjs/locale/vi';
import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import App from './App';
import { LoiApi } from './api/client';
import { PhienProvider } from './lib/phien';
import './styles.css';

dayjs.locale('vi');

const qc = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 10_000,
      refetchOnWindowFocus: false,
      retry: (n, e) => !(e instanceof LoiApi && e.status > 0 && e.status < 500) && n < 1,
    },
  },
});

// Design tokens theo hướng UNIQLO (chọn qua ui-ux-pro-max, style "Data-Dense Dashboard", màu thay bằng màu thương hiệu):
// đen đậm cho khung và nút chính, đỏ UNIQLO cho logo, mục đang chọn, số việc chờ và nút chốt (thanh toán).
// Góc vuông (bo 2px), chữ đậm, nền xám nhạt để các khối trắng nổi lên.
const DO = '#E50012';
const DEN = '#1B1B1B';
const theme = {
  token: {
    colorPrimary: DEN,
    colorInfo: '#1d4ed8',
    colorSuccess: '#16803c',
    colorWarning: '#b45309',
    colorError: '#c8102e',
    colorLink: DEN,
    colorLinkHover: DO,
    colorLinkActive: DO,
    colorText: DEN,
    colorTextSecondary: '#555555',
    colorTextTertiary: '#6b6b6b',
    colorBorder: '#d0d0d0',
    colorBorderSecondary: '#e8e8e8',
    colorBgLayout: '#f2f2f2',
    borderRadius: 2,
    borderRadiusLG: 2,
    borderRadiusSM: 2,
    fontFamily: "'Be Vietnam Pro', 'Segoe UI', system-ui, -apple-system, Roboto, Arial, sans-serif",
    fontSize: 14,
    controlHeight: 36,
  },
  components: {
    Layout: { headerBg: '#ffffff', siderBg: DEN, headerHeight: 60, headerPadding: '0 24px' },
    Menu: {
      darkItemBg: DEN,
      darkSubMenuItemBg: DEN,
      darkItemColor: '#c4c4c4',
      darkItemHoverColor: '#ffffff',
      darkItemHoverBg: '#2a2a2a',
      darkItemSelectedBg: '#2f2f2f',
      darkItemSelectedColor: '#ffffff',
      itemHeight: 44,
      itemMarginInline: 0,
      itemBorderRadius: 0,
      iconSize: 16,
    },
    Table: { headerBg: '#f7f7f7', headerColor: '#555555', rowSelectedBg: '#fff1f1', rowHoverBg: '#fafafa', cellPaddingBlockSM: 8, headerSplitColor: 'transparent' },
    Button: { primaryShadow: 'none', defaultShadow: 'none', dangerShadow: 'none', fontWeight: 600 },
    Tag: { defaultBg: '#f2f2f2' },
    Modal: { titleFontSize: 18 },
    Segmented: { itemSelectedBg: DEN, itemSelectedColor: '#ffffff', trackBg: '#ececec', itemColor: '#444444', itemHoverColor: DEN },
  },
};

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <ConfigProvider locale={viVN} theme={theme}>
      <AntApp>
        <QueryClientProvider client={qc}>
          <BrowserRouter>
            <PhienProvider>
              <App />
            </PhienProvider>
          </BrowserRouter>
        </QueryClientProvider>
      </AntApp>
    </ConfigProvider>
  </React.StrictMode>,
);
