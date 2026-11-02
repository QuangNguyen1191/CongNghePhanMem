import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { RouterProvider } from 'react-router-dom';
import { QueryClientProvider } from '@tanstack/react-query';
import { Toaster } from 'sonner';
import { router } from './app/router';
import { queryClient } from './app/queryClient';
import './styles/tokens.css';
import './styles/base.css';
import { session } from './lib/session';

// phien bi xoa vi bat ky ly do nao (401, het han, dang xuat) -> bo cache du lieu rieng cua khach
session.nghe((p) => { if (!p) queryClient.removeQueries({ queryKey: ['toi'] }); });

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
      <Toaster
        position="top-right"
        closeButton
        toastOptions={{ style: { borderRadius: 2, fontFamily: 'var(--font-sans)' } }}
      />
    </QueryClientProvider>
  </StrictMode>,
);
