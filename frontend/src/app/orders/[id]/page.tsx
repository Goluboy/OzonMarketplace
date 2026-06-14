"use client";

import Image from "next/image";
import Link from "next/link";
import { Header } from "@/app/component/layout/header/Header";

export default function OrderPage() {
  const order = {
    id: "ORD-12345",
    status: "delivered",
    createdAt: "15.05.2026 14:30",
    deliveredAt: "18.05.2026",
    address: "г. Москва, ул. Ленина, 1",
    totalPrice: 199998,
    items: [
      {
        id: "1",
        name: "iPhone 17",
        quantity: 2,
        price: 99999,
        image: "https://ir.ozone.ru/s3/multimedia-1-j/wc1000/10351974475.jpg",
      }
    ],
  };

  function getStatus() {
    switch (order.status) {
      case "delivered":
        return {
          icon: "✅",
          text: "Доставлен",
          color: "text-green-500",
        };

      case "shipping":
        return {
          icon: "🚚",
          text: "В пути",
          color: "text-blue-500",
        };

      case "processing":
        return {
          icon: "⏳",
          text: "В обработке",
          color: "text-yellow-500",
        };

      default:
        return {
          icon: "📦",
          text: "Создан",
          color: "text-text",
        };
    }
  }

  const status = getStatus();

  return (
    <>
      <Header />

      <div className="container mx-auto py-8">
        <div className="grid gap-6 lg:grid-cols-[1fr_360px]">

          <div className="space-y-6">

            <div className="rounded-3xl bg-surface p-6">
              <h1 className="mb-6 text-[32px] font-bold text-text">
                📄 Заказ #{order.id}
              </h1>

              <div className="space-y-4">
                <div className="flex items-center gap-3">
                  <span className={status.color}>
                    {status.icon} {status.text}
                  </span>
                </div>

                <div>
                  <div className="text-text-secondary">
                    Дата создания
                  </div>

                  <div className="font-medium text-text">
                    {order.createdAt}
                  </div>
                </div>

                <div>
                  <div className="text-text-secondary">
                    Дата доставки
                  </div>

                  <div className="font-medium text-text">
                    {order.deliveredAt}
                  </div>
                </div>
              </div>
            </div>

            {/* Товары */}
            <div className="rounded-3xl bg-surface p-6">
              <h2 className="mb-6 text-2xl font-semibold text-text">
                Товары
              </h2>

              <div className="space-y-4">
                {order.items.map((item) => (
                  <div
                    key={item.id}
                    className="flex items-center gap-5 rounded-2xl bg-surface-secondary p-4"
                  >
                    <div className="relative h-24 w-24 overflow-hidden rounded-2xl bg-background">
                      <Image
                        src={item.image}
                        alt={item.name}
                        fill
                        className="object-contain"
                      />
                    </div>

                    <div className="flex-1">
                      <div className="font-medium text-text">
                        {item.name}
                      </div>

                      <div className="mt-1 text-sm text-text-secondary">
                        Количество: {item.quantity}
                      </div>
                    </div>

                    <div className="text-xl font-bold text-text">
                      {(item.quantity * item.price).toLocaleString("ru-RU")} ₽
                    </div>
                  </div>
                ))}
              </div>
            </div>


            <div className="rounded-3xl bg-surface p-6">
              <h2 className="mb-4 text-2xl font-semibold text-text">
                Доставка
              </h2>

              <div className="text-text-secondary">
                Адрес доставки
              </div>

              <div className="mt-2 text-lg font-medium text-text">
                {order.address}
              </div>
            </div>

            <Link
              href="/orders"
              className="inline-flex rounded-2xl bg-surface px-6 py-4 font-medium text-text transition hover:opacity-80"
            >
              ← Вернуться к списку заказов
            </Link>
          </div>


          <div>
            <div className="sticky top-4 rounded-3xl bg-surface p-6">
              <h2 className="mb-6 text-2xl font-semibold text-text">
                Итого
              </h2>

              <div className="space-y-4">
                <div className="flex justify-between">
                  <span className="text-text-secondary">
                    Товаров
                  </span>

                  <span className="font-medium text-text">
                    {order.items.length}
                  </span>
                </div>

                <div className="border-t border-border pt-5">
                  <div className="flex justify-between">
                    <span className="text-xl font-semibold text-text">
                      Общая сумма
                    </span>

                    <span className="text-3xl font-bold text-text">
                      {order.totalPrice.toLocaleString("ru-RU")} ₽
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}