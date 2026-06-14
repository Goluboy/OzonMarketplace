// app/admin/page.tsx
"use client";

import Link from "next/link";
import { Header } from "@/app/component/layout/header/Header";
import { AdminGuard } from "../component/AdminGuard";
import { useAuth } from "../../../providers/AuthProvider";

function AdminPageContent() {
  const { user } = useAuth();

  return (
    <>
      <Header />

      <div className="container mx-auto py-8">
        <div className="flex justify-between items-center mb-8">
          <h1 className="text-[32px] font-bold text-text">
            👑 Админ-панель
          </h1>
          
          {/* Показываем роль текущего пользователя */}
          <div className="px-3 py-1 rounded-full text-sm font-medium bg-gray-100">
            {user?.role === 'admin' ? 'Администратор' : 'Продавец'}
          </div>
        </div>

        <div className="grid gap-6 md:grid-cols-3">
          <Link
            href="/admin/products"
            className="
              rounded-3xl
              bg-surface
              p-8
              transition
              hover:-translate-y-1
              hover:shadow-lg
            "
          >
            <div className="text-5xl">📦</div>

            <h2 className="mt-5 text-2xl font-semibold text-text">
              Товары
            </h2>

            <p className="mt-2 text-text-secondary">
              Создание, редактирование и удаление товаров
            </p>
          </Link>

          <Link
            href="/admin/discounts"
            className="
              rounded-3xl
              bg-surface
              p-8
              transition
              hover:-translate-y-1
              hover:shadow-lg
            "
          >
            <div className="text-5xl">💸</div>

            <h2 className="mt-5 text-2xl font-semibold text-text">
              Скидки
            </h2>

            <p className="mt-2 text-text-secondary">
              Управление ценами и акциями
            </p>
          </Link>

          <Link
            href="/admin/orders"
            className="
              rounded-3xl
              bg-surface
              p-8
              transition
              hover:-translate-y-1
              hover:shadow-lg
            "
          >
            <div className="text-5xl">📋</div>

            <h2 className="mt-5 text-2xl font-semibold text-text">
              Заказы
            </h2>

            <p className="mt-2 text-text-secondary">
              Управление заказами покупателей
            </p>
          </Link>
        </div>
      </div>
    </>
  );
}

export default function AdminPage() {
  return (
    <AdminGuard>
      <AdminPageContent />
    </AdminGuard>
  );
}