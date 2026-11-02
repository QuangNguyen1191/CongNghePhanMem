import { Suspense } from 'react';
import { Outlet, ScrollRestoration } from 'react-router-dom';
import { Header } from './Header';
import { Footer } from './Footer';
import { PageLoading } from '../pages/PageLoading';
import s from './MainLayout.module.css';

export function MainLayout() {
  return (
    <div className={s.shell}>
      <Header />
      <main id="noi-dung" className={s.main} tabIndex={-1}>
        <Suspense fallback={<PageLoading />}>
          <Outlet />
        </Suspense>
      </main>
      <Footer />
      <ScrollRestoration />
    </div>
  );
}
