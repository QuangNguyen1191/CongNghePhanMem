import { forwardRef, useState, type InputHTMLAttributes } from 'react';
import { Eye, EyeOff } from 'lucide-react';
import { TextField } from './Field';
import s from '../pages/Form.module.css';

type Props = { label: string; error?: string; hint?: string; required?: boolean } & Omit<InputHTMLAttributes<HTMLInputElement>, 'type'>;

/** O mat khau co nut hien / an */
export const PasswordField = forwardRef<HTMLInputElement, Props>((props, ref) => {
  const [hien, setHien] = useState(false);
  return (
    <div className={s.pwWrap}>
      <TextField ref={ref} type={hien ? 'text' : 'password'} {...props} />
      <button type="button" className={s.pwToggle} onClick={() => setHien((v) => !v)} aria-label={hien ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'} aria-pressed={hien}>
        {hien ? <EyeOff size={18} aria-hidden /> : <Eye size={18} aria-hidden />}
      </button>
    </div>
  );
});
PasswordField.displayName = 'PasswordField';
