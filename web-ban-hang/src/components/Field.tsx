import { forwardRef, useId, type InputHTMLAttributes, type ReactNode, type SelectHTMLAttributes, type TextareaHTMLAttributes } from 'react';
import s from './Field.module.css';

interface Khung {
  label: string;
  error?: string;
  hint?: ReactNode;
  required?: boolean;
}

function useMa(id?: string) {
  const sinh = useId();
  const ma = id ?? sinh;
  return { ma, maLoi: `${ma}-loi`, maGoiY: `${ma}-goiy` };
}

function Vo({ label, error, hint, required, ma, maLoi, maGoiY, children }: Khung & { ma: string; maLoi: string; maGoiY: string; children: ReactNode }) {
  return (
    <div className={`${s.field} ${error ? s.coLoi : ''}`}>
      <label htmlFor={ma} className={s.label}>
        {label}
        {required && <span className={s.batBuoc} aria-hidden> *</span>}
      </label>
      {children}
      {error ? (
        <p id={maLoi} className={s.loi} role="alert">{error}</p>
      ) : hint ? (
        <p id={maGoiY} className={s.goiY}>{hint}</p>
      ) : null}
    </div>
  );
}

export const TextField = forwardRef<HTMLInputElement, Khung & InputHTMLAttributes<HTMLInputElement>>(
  ({ label, error, hint, required, id, ...rest }, ref) => {
    const m = useMa(id);
    return (
      <Vo {...{ label, error, hint, required }} {...m}>
        <input
          ref={ref}
          id={m.ma}
          className={s.control}
          aria-invalid={!!error || undefined}
          aria-required={required || undefined}
          aria-describedby={error ? m.maLoi : hint ? m.maGoiY : undefined}
          {...rest}
        />
      </Vo>
    );
  },
);
TextField.displayName = 'TextField';

export const SelectField = forwardRef<HTMLSelectElement, Khung & SelectHTMLAttributes<HTMLSelectElement>>(
  ({ label, error, hint, required, id, children, ...rest }, ref) => {
    const m = useMa(id);
    return (
      <Vo {...{ label, error, hint, required }} {...m}>
        <select ref={ref} id={m.ma} className={`${s.control} ${s.select}`} aria-invalid={!!error || undefined}
          aria-describedby={error ? m.maLoi : hint ? m.maGoiY : undefined} {...rest}>
          {children}
        </select>
      </Vo>
    );
  },
);
SelectField.displayName = 'SelectField';

export const TextAreaField = forwardRef<HTMLTextAreaElement, Khung & TextareaHTMLAttributes<HTMLTextAreaElement>>(
  ({ label, error, hint, required, id, ...rest }, ref) => {
    const m = useMa(id);
    return (
      <Vo {...{ label, error, hint, required }} {...m}>
        <textarea ref={ref} id={m.ma} className={`${s.control} ${s.textarea}`} aria-invalid={!!error || undefined}
          aria-describedby={error ? m.maLoi : hint ? m.maGoiY : undefined} {...rest} />
      </Vo>
    );
  },
);
TextAreaField.displayName = 'TextAreaField';
