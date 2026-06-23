"use client";

import Image from "next/image";
import Link from "next/link";
import { useState, useEffect } from "react";
import { Header } from "@/app/component/layout/header/Header";
import {
  getOrders,
  OrderDetails,
} from "../../../services/order.service";

export default function OrdersPage() {

  const [orders, setOrders] = useState<OrderDetails[]>([]);

  const [loading, setLoading] =
    useState(true);

  const [error, setError] =
    useState("");

  const [tab, setTab] = useState<"active" | "completed">("active");

  function getStatusColor(status: string) {
    switch (status) {
      case "Created":
        return "text-gray-500";

      case "Paid":
        return "text-blue-500";

      case "Assembling":
        return "text-yellow-500";

      case "Shipping":
        return "text-indigo-500";

      case "Delivered":
        return "text-green-500";

      case "Cancelled":
        return "text-red-500";

      default:
        return "text-text";
    }
  }

  function getStatusText(status: string) {
    switch (status) {
      case "Created":
        return "Создан";

      case "Paid":
        return "Оплачен";

      case "Assembling":
        return "Собирается";

      case "Shipping":
        return "В пути";

      case "Delivered":
        return "Доставлен";

      case "Cancelled":
        return "Отменён";

      default:
        return status;
    }
  }

  useEffect(() => {
    async function loadOrders() {
      try {
        const response =
          await getOrders();

        setOrders(response);
      } catch (e) {
        setError(
          "Не удалось загрузить заказы"
        );
      } finally {
        setLoading(false);
      }
    }

    loadOrders();
  }, []);

  if (loading) {
    return (
      <>
        <Header />
        <div className="container mx-auto py-10">
          Загрузка заказов...
        </div>
      </>
    );
  }

  if (error) {
    return (
      <>
        <Header />
        <div className="container mx-auto py-10 text-red-500">
          {error}
        </div>
      </>
    );
  }

  const filteredOrders = orders.filter((order) =>
    tab === "active"
      ? !["Delivered", "Cancelled"].includes(order.status)
      : ["Delivered", "Cancelled"].includes(order.status)
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
                  <h2
                    className={`text-[22px] font-bold ${getStatusColor(order.status)}`}
                  >
                    {getStatusText(order.status)}
                  </h2>

                  <p className="mt-3 text-lg text-text-secondary">
                    Заказ от{" "}
                    {new Date(order.createdAt)
                      .toLocaleDateString("ru-RU")}
                  </p>

                  {["Assembling", "Shipping"].includes(order.status) && (
                    <div className="mt-2 text-xl font-semibold text-text">
                      Обновлён:{" "}
                      {new Date(order.updatedAt).toLocaleDateString("ru-RU")}
                    </div>
                  )}

                  <div className="mt-auto flex gap-3 pt-8">
                    {order.status === "Delivered" && (
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
                  <div className="flex flex-col items-start">

                    <div className="relative h-36 w-28 overflow-hidden rounded-2xl bg-surface-secondary">
                      <Image
                        src="/images/product-placeholder.png"
                        alt="Товар"
                        fill
                        className="object-contain"
                      />
                    </div>

                    {order.items.length > 1 && (
                      <div className="mt-3 text-sm text-text-secondary">
                        +{order.items.length - 1} товар
                        {order.items.length - 1 > 1 ? "а" : ""}
                      </div>
                    )}

                    <div className="mt-4 text-2xl font-bold text-text">
                      {Number(order.totalAmount).toLocaleString("ru-RU")} ₽
                    </div>

                  </div>
                </div>
              </div>
            </div>
          ))}

          {orders.length === 0 && (
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