import { ButtonLink, EmptyState } from '../components';

import { useTitle } from '../lib/useTitle';

export default function NotFoundPage() {
  useTitle('Không tìm thấy trang');
  return (
    <div className="container">
      <EmptyState
        as="h1"
        title="Không tìm thấy trang"
        description="Đường dẫn có thể đã thay đổi hoặc không còn tồn tại."
        action={<ButtonLink to="/">Về trang chủ</ButtonLink>}
      />
    </div>
  );
}
