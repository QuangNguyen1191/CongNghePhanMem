import { useEffect, useState, type FormEvent } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { khachHangApi } from '../../api/khachApi';
import { qk } from '../../api/queryKeys';
import type { KhachHang } from '../../api/types';
import { Button, ErrorState, LoadingRegion, Skeleton, TextField } from '../../components';
import { useHoSo } from '../../features/account/useTaiKhoan';
import { HANG_THANH_VIEN } from '../../features/account/nhan';
import { quyTac } from '../../config/cuaHang';
import { session } from '../../lib/session';
import { coLoi, kiemTra, type Loi } from '../../lib/kiemTra';
import { formatTien } from '../../lib/format';
import { useTitle } from '../../lib/useTitle';
import f from '../Form.module.css';
import s from './Account.module.css';

type Form = { hoTen: string; sdt: string; email: string; ngaySinh: string; diaChi: string; tinhThanh: string };
const tuHoSo = (k: KhachHang): Form => ({
  hoTen: k.hoTen ?? '', sdt: k.sdt ?? '', email: k.email ?? '', ngaySinh: k.ngaySinh?.slice(0, 10) ?? '',
  diaChi: k.diaChi ?? '', tinhThanh: k.tinhThanh ?? '',
});

function TheThanhVien({ k }: { k: KhachHang }) {
  // diem = floor(tong da mua / 10.000) -> uoc tinh tong da mua de biet con bao nhieu len hang
  const tongUoc = k.diemTichLuy * quyTac.tienMoiDiem;
  const tiep = quyTac.nguongHang.find(([, nguong]) => tongUoc < nguong);
  const truoc = [...quyTac.nguongHang].reverse().find(([, nguong]) => tongUoc >= nguong)?.[1] ?? 0;
  const pct = tiep ? Math.min(100, Math.round(((tongUoc - truoc) / (tiep[1] - truoc)) * 100)) : 100;
  return (
    <section className={s.member} aria-label="Hạng thành viên">
      <div className={s.memberRow}>
        <div>
          <p className="label muted">Hạng thành viên</p>
          <p className={s.tier}>{HANG_THANH_VIEN[k.hangThanhVien] ?? k.hangThanhVien}</p>
        </div>
        <div style={{ textAlign: 'right' }}>
          <p className="label muted">Điểm tích lũy</p>
          <p className={`${s.tier} num`}>{k.diemTichLuy.toLocaleString('vi-VN')}</p>
        </div>
      </div>
      <div className={s.bar} role="progressbar" aria-valuenow={pct} aria-valuemin={0} aria-valuemax={100} aria-label="Tiến độ lên hạng"><span style={{ width: `${pct}%` }} /></div>
      <p className="small muted">
        {tiep
          ? `Mua thêm khoảng ${formatTien(tiep[1] - tongUoc)} để lên hạng ${HANG_THANH_VIEN[tiep[0]]}.`
          : 'Bạn đang ở hạng cao nhất.'}
        {' '}Mỗi {formatTien(quyTac.tienMoiDiem)} trên hóa đơn đã thanh toán được 1 điểm.
      </p>
    </section>
  );
}

export default function HoSoPage() {
  useTitle('Tài khoản');
  const q = useHoSo();
  const qc = useQueryClient();
  const [v, setV] = useState<Form | null>(null);
  const [loi, setLoi] = useState<Loi<Form>>({});
  useEffect(() => { if (q.data && !v) setV(tuHoSo(q.data)); }, [q.data, v]);

  const luu = useMutation({
    mutationFn: (k: KhachHang) => khachHangApi.capNhat(k),
    onSuccess: (_d, k) => {
      qc.setQueryData(qk.hoSo, k);
      const p = session.doc();
      if (p && p.hoTen !== k.hoTen) session.ghi({ ...p, hoTen: k.hoTen });
      toast.success('Đã cập nhật thông tin');
    },
  });

  if (q.error) return <ErrorState error={q.error} onRetry={() => q.refetch()} />;
  if (!q.data || !v) return <LoadingRegion><Skeleton h={140} /><Skeleton h={320} /></LoadingRegion>;

  const doi = (k: keyof Form) => (e: { target: { value: string } }) => {
    setV((x) => x && { ...x, [k]: e.target.value });
    if (loi[k]) setLoi((l) => ({ ...l, [k]: '' }));
  };
  const submit = (e: FormEvent) => {
    e.preventDefault();
    const l: Loi<Form> = { hoTen: kiemTra.hoTen(v.hoTen), sdt: kiemTra.sdt(v.sdt), email: kiemTra.email(v.email), ngaySinh: kiemTra.ngaySinh(v.ngaySinh) };
    setLoi(l);
    if (coLoi(l) || luu.isPending) return;
    // PUT ghi de toan bo ho so: gui lai du truong, giu nguyen hang / diem
    luu.mutate({
      ...q.data!, hoTen: v.hoTen.trim(), sdt: v.sdt.trim() || null, email: v.email.trim() || null,
      ngaySinh: v.ngaySinh || null, diaChi: v.diaChi.trim() || null, tinhThanh: v.tinhThanh.trim() || null,
    });
  };
  const doiChua = JSON.stringify(v) !== JSON.stringify(tuHoSo(q.data));

  return (
    <>
      <h1 className="h1">Hồ sơ & điểm</h1>
      <TheThanhVien k={q.data} />
      <section className={s.panel} aria-labelledby="ho-so">
        <h2 id="ho-so" className={s.panelTitle}>Thông tin cá nhân</h2>
        <form className={f.form} onSubmit={submit} noValidate>
          <TextField label="Họ và tên" name="hoTen" value={v.hoTen} onChange={doi('hoTen')} error={loi.hoTen} required autoComplete="name" />
          <div className={f.grid2}>
            <TextField label="Số điện thoại" name="sdt" type="tel" inputMode="numeric" value={v.sdt} onChange={doi('sdt')} error={loi.sdt} autoComplete="tel" />
            <TextField label="Email" name="email" type="email" value={v.email} onChange={doi('email')} error={loi.email} autoComplete="email" />
          </div>
          <div className={f.grid2}>
            <TextField label="Ngày sinh" name="ngaySinh" type="date" value={v.ngaySinh} onChange={doi('ngaySinh')} error={loi.ngaySinh} max={new Date().toISOString().slice(0, 10)} />
            <TextField label="Tỉnh / thành phố" name="tinhThanh" value={v.tinhThanh} onChange={doi('tinhThanh')} autoComplete="address-level1" />
          </div>
          <TextField label="Địa chỉ" name="diaChi" value={v.diaChi} onChange={doi('diaChi')} hint="Dùng làm địa chỉ giao hàng mặc định khi thanh toán" autoComplete="street-address" />
          <div className={f.actions}>
            <Button type="submit" loading={luu.isPending} disabled={!doiChua}>Lưu thay đổi</Button>
            {doiChua && <Button variant="ghost" onClick={() => { setV(tuHoSo(q.data!)); setLoi({}); }}>Hủy</Button>}
          </div>
        </form>
      </section>
    </>
  );
}
