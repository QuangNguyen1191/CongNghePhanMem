import { Breadcrumb } from '../components';
import s from './StaticPage.module.css';

// Bang so do tham khao chung (nhom tu soan), khong gan voi tung san pham vi CSDL chua co du lieu size chart.
const AO = [
  ['S', '84 – 88', '70 – 74', '155 – 165', '45 – 55'],
  ['M', '88 – 94', '74 – 80', '160 – 170', '55 – 65'],
  ['L', '94 – 100', '80 – 86', '165 – 175', '65 – 72'],
  ['XL', '100 – 106', '86 – 92', '170 – 180', '72 – 80'],
];
const QUAN = [
  ['28', '70 – 73', '88 – 91'],
  ['29', '73 – 76', '91 – 94'],
  ['30', '76 – 79', '94 – 97'],
  ['31', '79 – 82', '97 – 100'],
  ['32', '82 – 85', '100 – 103'],
];

export default function HuongDanSizePage() {
  return (
    <div className={`container ${s.page}`}>
      <Breadcrumb items={[{ label: 'Trang chủ', to: '/' }, { label: 'Hướng dẫn chọn size' }]} />
      <h1 className={`h1 ${s.title}`}>Hướng dẫn chọn size</h1>
      <p className="muted">Số đo tính theo cm, dùng để tham khảo chung cho các dòng sản phẩm.</p>

      <section className={s.section}>
        <h2 className="h2">Áo (áo thun, sơ mi, áo khoác, váy đầm)</h2>
        <div className={s.tableWrap}>
          <table className={s.table}>
            <thead><tr><th scope="col">Size</th><th scope="col">Vòng ngực</th><th scope="col">Vòng eo</th><th scope="col">Chiều cao</th><th scope="col">Cân nặng (kg)</th></tr></thead>
            <tbody>{AO.map((r) => <tr key={r[0]}>{r.map((c, i) => i === 0 ? <th scope="row" key={i}>{c}</th> : <td key={i} className="num">{c}</td>)}</tr>)}</tbody>
          </table>
        </div>
      </section>

      <section className={s.section}>
        <h2 className="h2">Quần (jean, kaki)</h2>
        <div className={s.tableWrap}>
          <table className={s.table}>
            <thead><tr><th scope="col">Size</th><th scope="col">Vòng eo</th><th scope="col">Vòng mông</th></tr></thead>
            <tbody>{QUAN.map((r) => <tr key={r[0]}>{r.map((c, i) => i === 0 ? <th scope="row" key={i}>{c}</th> : <td key={i} className="num">{c}</td>)}</tr>)}</tbody>
          </table>
        </div>
      </section>

      <section className={s.section}>
        <h2 className="h2">Cách đo</h2>
        <ul>
          <li>Vòng ngực: đo ngang qua điểm cao nhất của ngực, thước dây song song mặt đất.</li>
          <li>Vòng eo: đo tại vị trí nhỏ nhất của eo, thả lỏng người.</li>
          <li>Vòng mông: đo ngang qua điểm rộng nhất của mông.</li>
          <li>Nằm giữa hai size: chọn size lớn hơn nếu thích mặc rộng.</li>
        </ul>
      </section>
    </div>
  );
}
