import { useState } from 'react';
import { toast } from 'sonner';
import { ShoppingBag } from 'lucide-react';
import {
  Badge, Breadcrumb, Button, ConfirmDialog, Drawer, EmptyState, ErrorState, Price, ProductImage,
  QuantityStepper, SelectField, Skeleton, TextField,
} from '../components';
import s from './ThietKePage.module.css';

const MAU = [
  ['--c-ink', 'Chữ chính, nút chính'], ['--c-ink-2', 'Chữ phụ'], ['--c-muted', 'Ghi chú'],
  ['--c-line', 'Đường kẻ'], ['--c-surface', 'Nền khối'], ['--c-accent', 'Giá KM, SALE, lỗi'],
  ['--c-success', 'Còn hàng'], ['--c-warning', 'Sắp hết'],
];

/** Trang mau design system: xem nhanh token va component o moi kich thuoc man hinh. */
export default function ThietKePage() {
  const [sl, setSl] = useState(1);
  const [drawer, setDrawer] = useState(false);
  const [xacNhan, setXacNhan] = useState(false);
  return (
    <div className={`container ${s.page}`}>
      <Breadcrumb items={[{ label: 'Trang chủ', to: '/' }, { label: 'Design system' }]} />
      <h1 className="h1">Design system</h1>

      <section className={s.sec}>
        <h2 className="h2">Màu</h2>
        <div className={s.swatches}>
          {MAU.map(([v, d]) => (
            <div key={v}><span className={s.sw} style={{ background: `var(${v})` }} /><p className="small"><code>{v}</code></p><p className="small muted">{d}</p></div>
          ))}
        </div>
      </section>

      <section className={s.sec}>
        <h2 className="h2">Chữ</h2>
        <p style={{ fontSize: 'var(--fs-display)', lineHeight: 'var(--lh-display)', fontWeight: 700 }}>Mặc đẹp mỗi ngày</p>
        <p className="h1">Tiêu đề trang H1</p>
        <p className="h2">Tiêu đề khối H2</p>
        <p>Nội dung thân bài 15px, dễ đọc trên mọi màn hình. Tiếng Việt có dấu đầy đủ: ă â đ ê ô ơ ư.</p>
        <p className="small muted">Ghi chú nhỏ 13px</p>
        <p className="label">Nhãn in hoa</p>
      </section>

      <section className={s.sec}>
        <h2 className="h2">Nút</h2>
        <div className={s.row}>
          <Button iconLeft={<ShoppingBag size={16} aria-hidden />}>Thêm vào giỏ</Button>
          <Button variant="secondary">Mua ngay</Button>
          <Button variant="ghost">Bỏ qua</Button>
          <Button variant="link">Xem chi tiết</Button>
          <Button loading>Đang gửi</Button>
          <Button disabled>Hết hàng</Button>
        </div>
      </section>

      <section className={s.sec}>
        <h2 className="h2">Giá, nhãn, số lượng</h2>
        <div className={s.row}>
          <Price goc={249000} />
          <Price goc={589000} km={471000} />
          <Price goc={1490000} km={1192000} size="lg" />
        </div>
        <div className={s.row}>
          <Badge tone="sale">-20%</Badge><Badge tone="success">Còn hàng</Badge>
          <Badge tone="warning">Chỉ còn 3</Badge><Badge>Hết hàng</Badge><Badge tone="dark">Mới</Badge>
        </div>
        <QuantityStepper value={sl} onChange={setSl} max={9} />
      </section>

      <section className={s.sec}>
        <h2 className="h2">Form</h2>
        <div className={s.form}>
          <TextField label="Họ tên" required placeholder="Nguyễn Văn A" />
          <TextField label="Số điện thoại" required error="Số điện thoại không hợp lệ" defaultValue="0901" />
          <TextField label="Mã giảm giá" hint="Ví dụ: WELCOME10" />
          <SelectField label="Tỉnh / thành"><option>TP Hồ Chí Minh</option><option>Hà Nội</option></SelectField>
        </div>
      </section>

      <section className={s.sec}>
        <h2 className="h2">Ảnh sản phẩm 3:4 và khung chờ</h2>
        <div className={s.grid}>
          <ProductImage src="/images/khong-co.jpg" alt="Áo thun AIRism cổ tròn" />
          <div><Skeleton ratio="3 / 4" /><Skeleton h={14} w="80%" /><Skeleton h={14} w="40%" /></div>
        </div>
      </section>

      <section className={s.sec}>
        <h2 className="h2">Trạng thái và phản hồi</h2>
        <div className={s.row}>
          <Button variant="secondary" onClick={() => toast.success('Đã thêm vào giỏ hàng')}>Toast thành công</Button>
          <Button variant="secondary" onClick={() => toast.error('Áo thun AIRism - Trắng - M chỉ còn 2 cái')}>Toast lỗi</Button>
          <Button variant="secondary" onClick={() => setDrawer(true)}>Mở drawer</Button>
          <Button variant="secondary" onClick={() => setXacNhan(true)}>Hộp xác nhận</Button>
        </div>
        <EmptyState title="Giỏ hàng đang trống" description="Khám phá sản phẩm và thêm vào giỏ." action={<Button>Mua sắm ngay</Button>} />
        <ErrorState error={new Error('Không kết nối được máy chủ.')} onRetry={() => toast('Thử lại')} />
      </section>

      <Drawer open={drawer} onClose={() => setDrawer(false)} title="Bộ lọc" footer={<Button block onClick={() => setDrawer(false)}>Xem kết quả</Button>}>
        <p>Nội dung drawer (bộ lọc trên mobile, giỏ hàng nhanh).</p>
      </Drawer>
      <ConfirmDialog
        open={xacNhan}
        title="Hủy đơn hàng?"
        description="Đơn DH00012 sẽ bị hủy và không thể khôi phục."
        confirmLabel="Hủy đơn"
        danger
        onConfirm={() => { setXacNhan(false); toast.success('Đã hủy đơn'); }}
        onClose={() => setXacNhan(false)}
      />
    </div>
  );
}
