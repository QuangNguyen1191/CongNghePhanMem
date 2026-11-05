// v3: Xac thuc so dien thoai bang OTP (gia lap) cho khach khong can tai khoan.
// Buoc 1 nhap SDT -> gui ma; buoc 2 nhap ma 6 so -> nhan phien VANG_LAI (luu o lib/phienVangLai).
// Dung chung cho: thanh toan khong can tai khoan, tra cuu don, dang ky tai khoan.
import { useEffect, useRef, useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { toast } from 'sonner';
import { CheckCircle2 } from 'lucide-react';
import { vangLaiApi } from '../../api/vangLaiApi';
import type { KetQuaGuiOtp, MucDichOtp, PhienVangLai } from '../../api/types';
import { Button, TextField } from '../../components';
import { chuanHoaSdt, kiemTra } from '../../lib/kiemTra';
import { phienVangLai, usePhienVangLai } from '../../lib/phienVangLai';
import s from './XacThucSdt.module.css';

interface Props {
  mucDich: MucDichOtp;
  /** SDT dien san (vd tu ho so) */
  sdtBanDau?: string;
  label?: string;
  goiY?: string;
  /** loi tu form cha (vd "Vui long xac thuc SDT") */
  loi?: string;
  onXong?: (p: PhienVangLai) => void;
}

/** Hien trang thai da xac thuc neu da co phien con han; bam "Doi so" de xac thuc so khac. */
export function XacThucSdt({ mucDich, sdtBanDau = '', label = 'Số điện thoại', goiY, loi, onXong }: Props) {
  const phien = usePhienVangLai();
  const [sdt, setSdt] = useState(sdtBanDau);
  const [loiSdt, setLoiSdt] = useState('');
  const [daGui, setDaGui] = useState<(KetQuaGuiOtp & { luc: number }) | null>(null);
  const [maOtp, setMaOtp] = useState('');
  const [loiMa, setLoiMa] = useState('');
  const [conGiay, setConGiay] = useState(0);
  const oMa = useRef<HTMLInputElement>(null);

  // dem nguoc "gui lai ma"
  useEffect(() => {
    if (!daGui) return;
    const tinh = () => setConGiay(Math.max(0, Math.ceil(daGui.guiLaiSauGiay - (Date.now() - daGui.luc) / 1000)));
    tinh();
    const t = setInterval(tinh, 1000);
    return () => clearInterval(t);
  }, [daGui]);

  const gui = useMutation({
    mutationFn: (so: string) => vangLaiApi.guiOtp(so, mucDich),
    onSuccess: (kq) => {
      setDaGui({ ...kq, luc: Date.now() });
      setMaOtp(''); setLoiMa('');
      toast.success(kq.maDemo ? `Mã OTP (demo): ${kq.maDemo}` : `Đã gửi mã OTP tới ${kq.sdt}`);
      setTimeout(() => oMa.current?.focus(), 0);
    },
    onError: (e) => setLoiSdt(e.message),
    meta: { tuXuLyLoi: true },
  });

  const xacThuc = useMutation({
    mutationFn: () => vangLaiApi.xacThuc(daGui!.sdt, maOtp.trim()),
    onSuccess: (p) => {
      phienVangLai.ghi(p);
      setDaGui(null); setMaOtp('');
      toast.success('Đã xác thực số điện thoại');
      onXong?.(p);
    },
    onError: (e) => { setLoiMa(e.message); oMa.current?.select(); },
    meta: { tuXuLyLoi: true },
  });

  const guiMa = () => {
    const l = kiemTra.sdtDiDong(sdt);
    setLoiSdt(l);
    if (l || gui.isPending) return;
    gui.mutate(chuanHoaSdt(sdt));
  };

  const nhapMa = () => {
    if (!/^\d{6}$/.test(maOtp.trim())) { setLoiMa('Mã OTP gồm 6 chữ số'); return; }
    if (!xacThuc.isPending) xacThuc.mutate();
  };

  if (phien && !daGui) {
    return (
      <div className={s.daXacThuc} role="status">
        <CheckCircle2 size={16} aria-hidden />
        <span>{label} <strong>{phien.sdt}</strong> đã xác thực</span>
        <button type="button" className={s.linkNut} onClick={() => { setSdt(phien.sdt); phienVangLai.xoa(); }}>Đổi số</button>
      </div>
    );
  }

  // Enter trong o nhap: thuc hien buoc hien tai, khong submit form cha
  const enter = (lam: () => void) => (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.ctrlKey && !e.metaKey) { e.preventDefault(); e.stopPropagation(); lam(); }
  };

  return (
    <div className={s.khung}>
      <div className={s.hang}>
        <TextField
          label={label} name="sdtOtp" type="tel" inputMode="numeric" autoComplete="tel" required
          value={sdt} readOnly={!!daGui}
          onChange={(e) => { setSdt(e.target.value); setLoiSdt(''); }}
          onKeyDown={enter(guiMa)}
          error={loiSdt || (!daGui ? loi : undefined)}
          hint={goiY ?? 'Cửa hàng gửi mã OTP để xác nhận số điện thoại là của bạn'}
        />
        {!daGui && <Button variant="secondary" className={s.nut} loading={gui.isPending} onClick={guiMa}>Gửi mã OTP</Button>}
      </div>

      {daGui && (
        <>
          {daGui.maDemo && (
            <p className={s.demo} role="status">
              Chế độ demo (chưa nối SMS): mã OTP là <strong>{daGui.maDemo}</strong>. Khi chạy thật, mã được gửi qua tin nhắn.
            </p>
          )}
          <div className={s.hang}>
            <TextField
              ref={oMa} label="Mã OTP" name="maOtp" inputMode="numeric" autoComplete="one-time-code" maxLength={6} required
              value={maOtp}
              onChange={(e) => { setMaOtp(e.target.value.replace(/\D/g, '')); setLoiMa(''); }}
              onKeyDown={enter(nhapMa)}
              error={loiMa}
              hint={`Mã 6 số gửi tới ${daGui.sdt}, hiệu lực ${Math.round(daGui.hetHanSauGiay / 60)} phút`}
            />
            <Button className={s.nut} loading={xacThuc.isPending} onClick={nhapMa}>Xác nhận</Button>
          </div>
          <div className={s.phu}>
            <button type="button" className={s.linkNut} disabled={conGiay > 0 || gui.isPending} onClick={() => gui.mutate(daGui.sdt)}>
              {conGiay > 0 ? `Gửi lại mã sau ${conGiay}s` : 'Gửi lại mã'}
            </button>
            <button type="button" className={s.linkNut} onClick={() => { setDaGui(null); setMaOtp(''); setLoiMa(''); }}>Đổi số điện thoại</button>
          </div>
        </>
      )}
    </div>
  );
}
