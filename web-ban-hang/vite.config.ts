import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Cổng 5173 là cổng backend đã mở CORS cho web bán hàng (appsettings.json > Cors:CacNguon)
export default defineConfig({
  plugins: [react()],
  server: { port: 5173, strictPort: true },
});
