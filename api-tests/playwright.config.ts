import { defineConfig } from '@playwright/test';

// Test API backend QL_BanThoiTrang. Chay API truoc (dotnet run --project WebApi), roi: npx playwright test
export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  workers: 1,                      // cac test co ghi du lieu (ton kho, hoa don...) nen chay tuan tu
  timeout: 60_000,
  reporter: [['list'], ['html', { open: 'never', outputFolder: 'playwright-report' }]],
  use: {
    baseURL: process.env.API_URL ?? 'http://localhost:5212',
    extraHTTPHeaders: { Accept: 'application/json' },
  },
});
