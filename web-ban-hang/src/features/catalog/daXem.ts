// San pham da xem gan day: chi luu ten mau (ten_sp) o trinh duyet, du lieu SP van lay tu API.
const KHOA = 'wbh.da-xem';
const TOI_DA = 12;

export function docDaXem(): string[] {
  try {
    const ds = JSON.parse(localStorage.getItem(KHOA) ?? '[]');
    return Array.isArray(ds) ? ds.filter((x): x is string => typeof x === 'string') : [];
  } catch {
    return [];
  }
}

export function ghiDaXem(tenSp: string) {
  try {
    localStorage.setItem(KHOA, JSON.stringify([tenSp, ...docDaXem().filter((t) => t !== tenSp)].slice(0, TOI_DA)));
  } catch { /* bo qua */ }
}
