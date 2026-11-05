import { Button, Input, Modal } from 'antd';
import { useRef, useState, type ReactNode } from 'react';

interface CauHoi {
  tieuDe: string;
  moTa?: ReactNode;
  batBuoc?: boolean;
  nutOk?: string;
  nguyHiem?: boolean;
  goiY?: string[];
}

/** Hộp thoại nhập lý do, trả Promise<string | null> (null = bấm hủy). Ctrl+Enter để xác nhận. */
export function useHoiLyDo(): [(c: CauHoi) => Promise<string | null>, ReactNode] {
  const [cau, setCau] = useState<CauHoi | null>(null);
  const [gt, setGt] = useState('');
  const [loi, setLoi] = useState(false);
  const traLoi = useRef<(v: string | null) => void>(() => {});
  const oNhap = useRef<HTMLTextAreaElement | null>(null);

  const hoi = (c: CauHoi) =>
    new Promise<string | null>((res) => {
      traLoi.current = res;
      setGt('');
      setLoi(false);
      setCau(c);
    });

  const ok = () => {
    if (cau?.batBuoc && !gt.trim()) {
      setLoi(true);
      return;
    }
    traLoi.current(gt.trim());
    setCau(null);
  };
  const huy = () => {
    traLoi.current(null);
    setCau(null);
  };

  const hop = (
    <Modal
      open={!!cau}
      title={cau?.tieuDe}
      onOk={ok}
      onCancel={huy}
      okText={cau?.nutOk ?? 'Xác nhận'}
      cancelText="Quay lại"
      okButtonProps={{ danger: cau?.nguyHiem }}
      destroyOnClose
    >
      <div
        onKeyDown={(e) => {
          if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
            e.preventDefault();
            ok();
          }
        }}
      >
      {cau?.moTa && <div style={{ marginBottom: 12 }}>{cau.moTa}</div>}
      {cau?.goiY && (
        <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap', marginBottom: 8 }}>
          {cau.goiY.map((g) => (
            <Button key={g} size="small" onClick={() => { setGt(g); setLoi(false); setTimeout(() => oNhap.current?.focus(), 0); }}>
              {g}
            </Button>
          ))}
        </div>
      )}
      <Input.TextArea
        autoFocus
        rows={3}
        maxLength={255}
        showCount
        value={gt}
        status={loi ? 'error' : undefined}
        placeholder={cau?.batBuoc ? 'Bắt buộc nhập lý do' : 'Lý do (không bắt buộc)'}
        aria-label="Lý do"
        onChange={(e) => {
          setGt(e.target.value);
          setLoi(false);
        }}
        ref={(r) => { oNhap.current = r?.resizableTextArea?.textArea ?? null; }}
      />
      {loi && <div style={{ color: '#c8102e', marginTop: 4 }} role="alert">Vui lòng nhập lý do</div>}
      <div style={{ color: '#737373', fontSize: 12, marginTop: 6 }}>
        <kbd>Ctrl</kbd> + <kbd>Enter</kbd> để xác nhận
      </div>
      </div>
    </Modal>
  );
  return [hoi, hop];
}
