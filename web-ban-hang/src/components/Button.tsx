import { forwardRef, type ButtonHTMLAttributes, type ReactNode } from 'react';
import { Link, type LinkProps } from 'react-router-dom';
import s from './Button.module.css';

type Kieu = 'primary' | 'secondary' | 'ghost' | 'link';
type Co = 'md' | 'lg';

interface ChungProps {
  variant?: Kieu;
  size?: Co;
  block?: boolean;
  loading?: boolean;
  iconLeft?: ReactNode;
}

const lop = (v: Kieu, c: Co, block?: boolean) => [s.btn, s[v], s[c], block ? s.block : ''].join(' ');

export const Button = forwardRef<HTMLButtonElement, ChungProps & ButtonHTMLAttributes<HTMLButtonElement>>(
  ({ variant = 'primary', size = 'md', block, loading, iconLeft, children, disabled, className, type = 'button', ...rest }, ref) => (
    <button
      ref={ref}
      type={type}
      className={`${lop(variant, size, block)} ${className ?? ''}`}
      disabled={disabled || loading}
      aria-busy={loading || undefined}
      {...rest}
    >
      {loading ? <span className={s.spinner} aria-hidden /> : iconLeft}
      <span>{children}</span>
    </button>
  ),
);
Button.displayName = 'Button';

export function ButtonLink({ variant = 'primary', size = 'md', block, iconLeft, children, className, ...rest }: ChungProps & LinkProps) {
  return (
    <Link className={`${lop(variant, size, block)} ${className ?? ''}`} {...rest}>
      {iconLeft}
      <span>{children}</span>
    </Link>
  );
}
