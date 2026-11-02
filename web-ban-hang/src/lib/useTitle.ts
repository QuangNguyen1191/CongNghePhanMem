import { useEffect } from 'react';

export function useTitle(tieuDe?: string) {
  useEffect(() => {
    document.title = tieuDe ? `${tieuDe} | UNIQLO` : 'UNIQLO';
  }, [tieuDe]);
}
