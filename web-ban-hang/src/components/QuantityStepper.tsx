import { useEffect, useState } from 'react';
import { Minus, Plus } from 'lucide-react';
import s from './QuantityStepper.module.css';

interface Props {
  value: number;
  onChange: (v: number) => void;
  min?: number;
  max?: number;
  disabled?: boolean;
  label?: string;
}

export function QuantityStepper({ value, onChange, min = 1, max = 99, disabled, label = 'Số lượng' }: Props) {
  // o nhap giu ban nhap rieng, chi bao ra ngoai khi roi o / Enter (tranh goi API moi lan go phim)
  const [nhap, setNhap] = useState(String(value));
  useEffect(() => setNhap(String(value)), [value]);
  const dat = (v: number) => {
    const moi = Math.min(max, Math.max(min, Number.isFinite(v) ? v : value));
    setNhap(String(moi));
    if (moi !== value) onChange(moi);
  };
  return (
    <div className={s.wrap} role="group" aria-label={label}>
      <button type="button" className={s.btn} onClick={() => dat(value - 1)} disabled={disabled || value <= min} aria-label="Giảm số lượng">
        <Minus size={16} aria-hidden />
      </button>
      <input
        className={`${s.input} num`}
        type="number"
        inputMode="numeric"
        value={nhap}
        min={min}
        max={max}
        disabled={disabled}
        aria-label={label}
        onChange={(e) => setNhap(e.target.value)}
        onBlur={() => dat(parseInt(nhap, 10))}
        onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); dat(parseInt(nhap, 10)); } }}
      />
      <button type="button" className={s.btn} onClick={() => dat(value + 1)} disabled={disabled || value >= max} aria-label="Tăng số lượng">
        <Plus size={16} aria-hidden />
      </button>
    </div>
  );
}
