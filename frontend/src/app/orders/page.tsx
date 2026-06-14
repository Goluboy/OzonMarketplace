"use client";

import Image from "next/image";
import Link from "next/link";
import { useState } from "react";
import { Header } from "@/app/component/layout/header/Header";

type OrderStatus = "shipping" | "delivered";

interface OrderItem {
  id: string;
  name: string;
  image: string;
  price: number;
}

interface Order {
  id: string;
  status: OrderStatus;
  deliveryText: string;
  dateText: string;
  items: OrderItem[];
}

export default function OrdersPage() {
  const [tab, setTab] = useState<"active" | "completed">("active");

  // Временно. Потом заменить на GET /api/orders
  const orders: Order[] = [
    {
      id: "12345",
      status: "shipping",
      deliveryText: "Доставка в пункт выдачи",
      dateText: "14–15 июня",
      items: [
        {
          id: "1",
          name: "Игровой набор",
          image: "https://ir.ozone.ru/s3/multimedia-1-p/wc1000/7965249721.jpg",
          price: 2806,
        },
      ],
    },
    {
      id: "12344",
      status: "delivered",
      deliveryText: "Доставка в пункт выдачи",
      dateText: "Получен 11 июня",
      items: [
        {
          id: "2",
          name: "iPhone 17",
          image: "https://ir.ozone.ru/s3/multimedia-1-j/wc1000/10351974475.jpg",
          price: 1739,
        },
      ],
    },
  ];

  const filteredOrders = orders.filter((order) =>
    tab === "active"
      ? order.status !== "delivered"
      : order.status === "delivered"
  );

  return (
    <>
      <Header />

      <div className="container mx-auto py-8">
        <div className="mb-6 rounded-3xl bg-surface p-6">
          <div className="mb-6 flex items-center gap-3 text-[20px] font-bold text-text">
            <span>Заказы</span>

            <span className="text-text-secondary">•</span>

            <span className="text-text-secondary">
              Покупки
            </span>
          </div>

          <div className="flex gap-3">
            <button
              onClick={() => setTab("active")}
              className={`rounded-2xl px-6 py-3 font-semibold transition ${
                tab === "active"
                  ? "bg-black text-white"
                  : "bg-surface-secondary text-text"
              }`}
            >
              Актуальные
            </button>

            <button
              onClick={() => setTab("completed")}
              className={`rounded-2xl px-6 py-3 font-semibold transition ${
                tab === "completed"
                  ? "bg-black text-white"
                  : "bg-surface-secondary text-text"
              }`}
            >
              Завершённые
            </button>
          </div>
        </div>

        <div className="space-y-4 bg-gray-50 rounded-4xl">
          {filteredOrders.map((order) => (
            <div
              key={order.id}
              className="rounded-3xl bg-surface p-6"
            >
              <div className="grid gap-8 lg:grid-cols-[1fr_320px]">
                {/* Левая часть */}
                <div className="flex flex-col">
                  <h2 className="text-[22px] font-bold text-text">
                    {order.status === "shipping"
                      ? "В пути ›"
                      : order.dateText}
                  </h2>

                  <p className="mt-3 text-lg text-text-secondary">
                    {order.deliveryText}
                  </p>

                  {order.status === "shipping" && (
                    <div className="mt-2 text-xl font-semibold text-text">
                      {order.dateText}
                    </div>
                  )}

                  <div className="mt-auto flex gap-3 pt-8">
                    {order.status === "delivered" && (
                      <button className="rounded-2xl bg-accent px-6 py-4 font-medium borde text-primary">
                        Оценить товар
                      </button>
                    )}

                    <Link
                      href={`/orders/${order.id}`}
                      className="rounded-2xl bg-surface-secondary px-6 py-4 font-medium border text-text transition hover:opacity-80"
                    >
                      Подробнее
                    </Link>
                  </div>
                </div>

                {/* Правая часть */}
                <div className="border-l border-border pl-8">
                  <div className="flex gap-5">
                    {order.items.map((item) => (
                      <div key={item.id}>
                        <div className="relative h-36 w-28 overflow-hidden rounded-2xl bg-surface-secondary">
                          <Image
                            src={item.image}
                            alt={item.name}
                            fill
                            className="object-contain"
                          />
                        </div>

                        <div className="mt-3 text-xl font-bold text-text">
                          {item.price.toLocaleString("ru-RU")} ₽
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            </div>
          ))}

          {filteredOrders.length === 0 && (
            <div className="rounded-3xl bg-surface p-16 text-center">
              <div className="text-xl font-semibold text-text">
                Заказов нет
              </div>

              <div className="mt-2 text-text-secondary">
                Здесь будут отображаться ваши покупки
              </div>
            </div>
          )}
        </div>
      </div>
    </>
  );
}