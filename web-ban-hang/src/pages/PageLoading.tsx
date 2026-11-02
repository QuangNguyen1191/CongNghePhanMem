import { LoadingRegion, Skeleton } from '../components';

export function PageLoading() {
  return (
    <LoadingRegion label="Đang tải trang">
      <div className="container" style={{ paddingBlock: 'var(--s-6)', display: 'grid', gap: 'var(--s-4)' }}>
        <Skeleton w="40%" h={28} />
        <Skeleton w="100%" h={240} />
      </div>
    </LoadingRegion>
  );
}
