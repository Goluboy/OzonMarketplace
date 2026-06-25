// app/registration/page.tsx
'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '../../../providers/AuthProvider';
import { Header } from '@/app/component/layout/header/Header';

export default function RegisterPage() {
  const { isAuthenticated, register, loading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    // Если пользователь уже авторизован, отправляем на главную
    if (isAuthenticated) {
      router.push('/');
    }
  }, [isAuthenticated, router]);

  const handleRegister = () => {
    register(); // Открывает страницу регистрации Keycloak
  };

  if (loading) {
    return (
      <>
        <Header />
        <div className="min-h-screen flex items-center justify-center">
          <div className="text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto mb-4"></div>
            <p>Загрузка...</p>
          </div>
        </div>
      </>
    );
  }

  return (
    <>
      <Header />

      <div className="min-h-screen flex justify-center px-4">
        <div className="w-full max-w-sm rounded-3xl">
          <h1 className="text-4xl font-normal mb-6">
            Регистрация
          </h1>

          <div className="space-y-4 mb-5">
            <p className="text-gray-600 text-center mb-6">
              Нажмите на кнопку ниже, чтобы создать аккаунт
            </p>
          </div>

          <button
            onClick={handleRegister}
            className="w-full py-3.5 rounded-full bg-primary text-white text-sm font-medium mb-6 transition-all active:scale-[0.98]"
          >
            Зарегистрироваться через Keycloak
          </button>

          <div className="flex items-center gap-3 mb-5">
            <div className="flex-1 h-px border-t bg-gray-200" />
            <span className="text-xs">Или</span>
            <div className="flex-1 h-px border-t bg-gray-200" />
          </div>

          <p className="text-center text-sm">
            Уже есть аккаунт?{" "}
            <button 
              onClick={() => router.push('/login')}
              className="underline underline-offset-2"
            >
              Войти
            </button>
          </p>
        </div>
      </div>
    </>
  );
}