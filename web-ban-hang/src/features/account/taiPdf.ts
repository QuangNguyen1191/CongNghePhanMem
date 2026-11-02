import { useMutation } from '@tanstack/react-query';

/** Tai PDF co kem token (khong mo link truc tiep duoc vi can header Authorization) */
export function useTaiPdf(lay: (ma: string) => Promise<Blob>, tenFile: (ma: string) => string) {
  return useMutation({
    mutationFn: async (ma: string) => {
      const blob = await lay(ma);
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = tenFile(ma);
      document.body.appendChild(a);
      a.click();
      a.remove();
      setTimeout(() => URL.revokeObjectURL(url), 10_000);
    },
  });
}
