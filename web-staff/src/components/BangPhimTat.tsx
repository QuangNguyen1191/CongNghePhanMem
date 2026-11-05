import { Modal } from 'antd';
import { BANG_PHIM_TAT } from '../lib/phimTat';

export function BangPhimTat({ mo, dong }: { mo: boolean; dong: () => void }) {
  return (
    <Modal open={mo} onCancel={dong} footer={null} title="Phím tắt bàn phím" width={640}>
      <div className="bang-phim">
        {BANG_PHIM_TAT.map((n) => (
          <section key={n.nhom}>
            <h3>{n.nhom}</h3>
            <dl>
              {n.ds.map(([p, m]) => (
                <div key={p} className="dong-phim">
                  <dt>
                    {p.split(' + ').map((k, i) => (
                      <span key={i}>
                        {i > 0 && ' + '}
                        <kbd>{k}</kbd>
                      </span>
                    ))}
                  </dt>
                  <dd>{m}</dd>
                </div>
              ))}
            </dl>
          </section>
        ))}
      </div>
    </Modal>
  );
}
