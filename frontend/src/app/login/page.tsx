'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '../../../providers/AuthProvider';
import { Header } from '@/app/component/layout/header/Header';

export default function LoginPage() {
  const { isAuthenticated, user, login, loading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (isAuthenticated && user) {
      if (user.role === 'admin' || user.role === 'seller') {
        router.push('/admin');
      } else {
        router.push('/');
      }
    }
  }, [isAuthenticated, user, router]);

  if (loading) {
    return (
      <>
        <Header />
        <div className="min-h-screen flex items-center justify-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto mb-4"></div>
        </div>
      </>
    );
  }

  return (
    <>
      <Header />
      <div className="min-h-screen flex justify-center px-4">
        <div className="w-full max-w-sm rounded-3xl">
          <h1 className="text-4xl font-normal mb-6">Войти</h1>

          <div className="mb-6 p-3 bg-blue-50 text-blue-800 rounded-xl text-sm">
            <p className="font-medium mb-2">🔐 Тестовые пользователи:</p>
            <ul className="space-y-1 text-xs">
              <li>Customer: test@example.com / password → Главная</li>
              <li>Admin: admin@example.com / admin123 → Админка</li>
              <li>Seller: seller@example.com / seller123 → Админка</li>
            </ul>
          </div>

          <button
            onClick={login}
            className="w-full py-3.5 rounded-full text-white bg-primary text-sm font-medium mb-6 transition-all active:scale-[0.98]"
          >
            Войти через Keycloak
          </button>

          <div className="flex items-center gap-3 mb-5">
            <div className="flex-1 h-px bg-gray-200"/>
            <span className="text-xs">Или</span>
            <div className="flex-1 h-px bg-gray-200"/>
          </div>

          <p className="text-center text-sm">
            <button
              onClick={() => router.push('/registration')}
              className="underline underline-offset-2"
            >
              Зарегистрироваться
            </button>
          </p>
        </div>
      </div>
    </>
  );
}