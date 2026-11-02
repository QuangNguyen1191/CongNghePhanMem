import { MutationCache, QueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { ApiError } from '../api/apiClient';

// Xu ly loi tap trung: thao tac (mutation) loi -> toast thong bao tieng Viet tu backend.
// Truy van (query) loi -> trang tu hien ErrorState co nut "Thu lai".
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 60_000,
      refetchOnWindowFocus: false,
      // loi 4xx la loi nghiep vu / quyen, thu lai vo ich
      retry: (lan, loi) => !(loi instanceof ApiError && loi.status >= 400 && loi.status < 500) && lan < 2,
    },
    mutations: { retry: false },
  },
  mutationCache: new MutationCache({
    onError: (loi, _bien, _ctx, mutation) => {
      if (mutation.meta?.tuXuLyLoi) return;
      toast.error(loi instanceof Error ? loi.message : 'Có lỗi xảy ra, vui lòng thử lại.');
    },
  }),
});

declare module '@tanstack/react-query' {
  interface Register {
    mutationMeta: { tuXuLyLoi?: boolean };
  }
}
