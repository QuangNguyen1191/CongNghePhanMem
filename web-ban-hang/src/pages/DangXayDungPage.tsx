import { Construction } from 'lucide-react';
import { ButtonLink, EmptyState } from '../components';

/** Tam thoi cho cac route se lam o Phase 3-6. Xoa khi trang that hoan thanh. */
export default function DangXayDungPage({ ten }: { ten: string }) {
  return (
    <div className="container">
      <EmptyState
        icon={<Construction size={32} strokeWidth={1.25} />}
        title={ten}
        description="Trang này đang được xây dựng ở giai đoạn tiếp theo."
        action={<ButtonLink to="/" variant="secondary">Về trang chủ</ButtonLink>}
      />
    </div>
  );
}
