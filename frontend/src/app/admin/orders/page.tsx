"use client";

import Link from "next/link";
import { Header } from "@/app/component/layout/header/Header";

export default function AdminOrdersPage() {
  // Временно. Потом заменить на GET /api/orders
  const orders = [
    {
      id: "ORD-12345",
      customer: "Иван Иванов",
      totalPrice: 279997,
      status: "В пути",
      date: "15.05.2026",
    },
    {
      id: "ORD-12344",
      customer: "Петр Петров",
      totalPrice: 99999,
      status: "Доставлен",
      date: "14.05.2026",
    },
    {
      id: "ORD-12343",
      customer: "Анна Смирнова",
      totalPrice: 37387,
      status: "Создан",
      date: "13.05.2026",
    },
  ];

  return (
    <>
      <Header />

      <div className="container mx-auto py-8">
        {/* Верхняя панель */}
        <div className="mb-6">
          <h1 className="text-[32px] font-bold text-text">
            📋 Заказы
          </h1>

          <p className="mt-2 text-text-secondary">
            Управление заказами покупателей
          </p>
        </div>

        <div className="space-y-5">
          {orders.map((order) => (
            <div
              key={order.id}
              className="rounded-3xl bg-surface p-6"
            >
              <div className="flex flex-col gap-6 lg:flex-row lg:items-center lg:justify-between">
                {/* Информация */}
                <div>
                  <h2 className="text-2xl font-semibold text-text">
                    {order.id}
                  </h2>

                  <div className="mt-3 space-y-1">
                    <div className="text-text-secondary">
                      Покупатель: {order.customer}
                    </div>

                    <div className="text-text-secondary">
                      Дата: {order.date}
                    </div>

                    <div className="text-lg font-medium text-text">
                      Сумма:{" "}
                      {order.totalPrice.toLocaleString("ru-RU")} ₽
                    </div>
                  </div>
                </div>

                {/* Управление */}
                <div className="flex flex-col gap-4">
                  <select
                    defaultValue={order.status}
                    className="
                      rounded-2xl
                      border
                      border-border
                      bg-background
                      px-4
                      py-3
                      text-text
                      outline-none
                    "
                  >
                    <option>Создан</option>
                    <option>В обработке</option>
                    <option>В пути</option>
                    <option>Доставлен</option>
                    <option>Отменён</option>
                  </select>

                  <div className="flex gap-3">
                    <button className="rounded-2xl bg-accent px-5 py-3 font-medium text-white transition hover:opacity-90">
                      Сохранить
                    </button>

                    <Link
                      href={`/orders/${order.id}`}
                      className="
                        rounded-2xl
                        bg-surface-secondary
                        px-5
                        py-3
                        font-medium
                        text-text
                        transition
                        hover:opacity-80
                      "
                    >
                      Подробнее
                    </Link>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>

        {orders.length === 0 && (
          <div className="rounded-3xl bg-surface p-20 text-center">
            <div className="text-2xl font-semibold text-text">
              Заказов пока нет
            </div>

            <div className="mt-3 text-text-secondary">
              Здесь будут отображаться заказы покупателей
            </div>
          </div>
        )}
      </div>
    </>
  );
}