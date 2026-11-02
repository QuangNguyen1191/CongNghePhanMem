import { useState, type FormEvent } from 'react';
import { useMutation } from '@tanstack/react-query';
import { toast } from 'sonner';
import { taiKhoanApi } from '../../api/khachApi';
import { Button, PasswordField } from '../../components';
import { kiemTra } from '../../lib/kiemTra';
import { useTitle } from '../../lib/useTitle';
import f from '../Form.module.css';
import s from './Account.module.css';

export default function DoiMatKhauPage() {
  useTitle('Đổi mật khẩu');
  const [cu, setCu] = useState('');
  const [moi, setMoi] = useState('');
  const [lai, setLai] = useState('');
  const [loi, setLoi] = useState<{ cu?: string; moi?: string; lai?: string }>({});
  const doi = useMutation({
    mutationFn: () => taiKhoanApi.doiMatKhau(cu, moi),
    onSuccess: (r) => { toast.success(r?.thongBao ?? 'Đã đổi mật khẩu'); setCu(''); setMoi(''); setLai(''); },
    meta: { tuXuLyLoi: true },
  });
  const submit = (e: FormEvent) => {
    e.preventDefault();
    const l = {
      cu: cu ? '' : 'Vui lòng nhập mật khẩu hiện tại',
      moi: kiemTra.matKhau(moi) || (moi === cu ? 'Mật khẩu mới phải khác mật khẩu hiện tại' : ''),
      lai: lai !== moi ? 'Mật khẩu nhập lại không khớp' : '',
    };
    setLoi(l);
    if (!l.cu && !l.moi && !l.lai && !doi.isPending) doi.mutate();
  };
  return (
    <>
      <h1 className="h1">Đổi mật khẩu</h1>
      <section className={s.panel} style={{ maxWidth: 480 }}>
        {doi.error && <p className={f.alert} role="alert">{doi.error.message}</p>}
        <form className={f.form} onSubmit={submit} noValidate>
          <PasswordField label="Mật khẩu hiện tại" autoComplete="current-password" value={cu} onChange={(e) => setCu(e.target.value)} error={loi.cu} required />
          <PasswordField label="Mật khẩu mới" autoComplete="new-password" value={moi} onChange={(e) => setMoi(e.target.value)} error={loi.moi} hint="Ít nhất 6 ký tự" required />
          <PasswordField label="Nhập lại mật khẩu mới" autoComplete="new-password" value={lai} onChange={(e) => setLai(e.target.value)} error={loi.lai} required />
          <div><Button type="submit" loading={doi.isPending}>Đổi mật khẩu</Button></div>
        </form>
      </section>
    </>
  );
}
