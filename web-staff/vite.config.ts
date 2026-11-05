import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Cổng 5174 đã được mở CORS sẵn cho web staff (backend/WebApi/appsettings.json > Cors:CacNguon)
export default defineConfig({
  plugins: [react()],
  server: { port: 5174, strictPort: true },
  build: {
    chunkSizeWarningLimit: 1500,
    rollupOptions: { output: { manualChunks: { antd: ['antd', '@ant-design/icons'], react: ['react', 'react-dom', 'react-router-dom'] } } },
  },
});
