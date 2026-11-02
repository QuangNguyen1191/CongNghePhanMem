import { defineConfig, devices } from '@playwright/test';

// Test E2E web ban hang tren trinh duyet that, goi API that.
// Can chay truoc: SQL Server (CSDL vua nap QL_BanThoiTrang_DuLieuMau.sql) + API (dotnet run --project WebApi, cong 5212).
// Lan dau: npx playwright install chromium. Chay: npm run test:e2e
export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  workers: 1, // cac test dung chung tai khoan khach01 va gio hang tren server
  timeout: 45_000,
  expect: { timeout: 10_000 },
  reporter: [['list'], ['html', { open: 'never', outputFolder: 'playwright-report' }]],
  use: {
    baseURL: process.env.WEB_URL ?? 'http://localhost:5173',
    locale: 'vi-VN',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [
    { name: 'desktop', use: { ...devices['Desktop Chrome'], viewport: { width: 1280, height: 900 } } },
    { name: 'mobile', use: { ...devices['Pixel 7'] }, testMatch: /responsive|cart/ },
  ],
  webServer: {
    command: 'npm run dev',
    url: 'http://localhost:5173',
    reuseExistingServer: true,
    timeout: 60_000,
  },
});
