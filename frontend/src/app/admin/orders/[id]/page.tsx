"use client";

import Image from "next/image";
import Link from "next/link";
import { useEffect, useState } from "react";
import { useParams } from "next/navigation";

import { Header } from "@/app/component/layout/header/Header";

import {
  AdminOrder,
  getAdminOrderById,
  updateOrderStatus,
  forceCancelOrder,
} from "../../../../../services/order.service";

export default function AdminOrderPage() {
  const params = useParams();

  const [order, setOrder] =
    useState<AdminOrder | null>(null);

  const [loading, setLoading] =
    useState(true);

  const [error, setError] =
    useState("");

  function getStatus() {
    switch (order?.status) {
      case "Created":
        return {
          icon: "📦",
          text: "Создан",
          color: "text-gray-500",
        };

      case "Paid":
        return {
          icon: "💳",
          text: "Оплачен",
          color: "text-blue-500",
        };

      case "Assembling":
        return {
          icon: "📦",
          text: "Собирается",
          color: "text-yellow-500",
        };

      case "Shipping":
        return {
          icon: "🚚",
          text: "В пути",
          color: "text-indigo-500",
        };

      case "Delivered":
        return {
          icon: "✅",
          text: "Доставлен",
          color: "text-green-500",
        };

      case "Cancelled":
        return {
          icon: "❌",
          text: "Отменён",
          color: "text-red-500",
        };

      default:
        return {
          icon: "📦",
          text: order?.status,
          color: "text-text",
        };
    }
  }

  const status = getStatus();

  useEffect(() => {
    async function loadOrder() {
      try {
        const data =
          await getAdminOrderById(
            params.id as string
          );

        setOrder(data);
      } catch {
        setError(
          "Не удалось загрузить заказ"
        );
      } finally {
        setLoading(false);
      }
    }

    loadOrder();
  }, [params.id]);

  async function handleStatusChange(
    status: string
  ) {
    if (!order) {
      return;
    }

    try {
      const updated =
        await updateOrderStatus(
          order.id,
          status
        );

      setOrder(updated);
    } catch (error) {
      console.error(error);

      alert(
        "Не удалось обновить статус"
      );
    }
  }

  async function handleForceCancel() {
    if (!order) {
      return;
    }

    if (
      !confirm(
        "Принудительно отменить заказ?"
      )
    ) {
      return;
    }

    try {
      const updated =
        await forceCancelOrder(
          order.id
        );

      setOrder(updated);

      alert("Заказ отменён");
    } catch (error) {
      console.error(error);

      alert(
        "Не удалось отменить заказ"
      );
    }
  }

  if (loading) {
    return (
      <>
        <Header />
        <div className="container mx-auto py-10">
          Загрузка заказа...
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

  if (!order) {
    return null;
  }

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
                    Покупатель
                  </div>

                  <div className="font-medium text-text">
                    {order.customerName}
                  </div>
                </div>

                <div>
                  <div className="text-text-secondary">
                    Email
                  </div>

                  <div className="font-medium text-text">
                    {order.customerEmail}
                  </div>
                </div>

                <div>
                  <div className="text-text-secondary">
                    Дата создания
                  </div>

                  <div className="font-medium text-text">
                    {new Date(
                      order.createdAt
                    ).toLocaleString(
                      "ru-RU"
                    )}
                  </div>
                </div>

                <div>
                  <div className="text-text-secondary">
                    Последнее изменение
                  </div>

                  <div className="font-medium text-text">
                    {new Date(
                      order.updatedAt
                    ).toLocaleString(
                      "ru-RU"
                    )}
                  </div>
                </div>
              </div>
            </div>

            <div className="rounded-3xl bg-surface p-6">
              <h2 className="mb-6 text-2xl font-semibold text-text">
                Товары
              </h2>

              <div className="space-y-4">
                {order.items.map(
                  (item) => (
                    <div
                      key={
                        item.productId
                      }
                      className="flex items-center gap-5 rounded-2xl bg-surface-secondary p-4"
                    >
                      <div className="relative h-24 w-24 overflow-hidden rounded-2xl bg-background">
                        <Image
                          src="/images/product-placeholder.png"
                          alt="Товар"
                          fill
                          className="object-contain"
                        />
                      </div>

                      <div className="flex-1">
                        <div className="font-medium text-text">
                          {
                            item.productName
                          }
                        </div>

                        <div className="mt-1 text-sm text-text-secondary">
                          Количество:{" "}
                          {
                            item.quantity
                          }
                        </div>
                      </div>

                      <div className="text-xl font-bold text-text">
                        {(
                          Number(item.priceAtPurchase.amount) *
                          item.quantity
                        ).toLocaleString("ru-RU")} ₽
                      </div>
                    </div>
                  )
                )}
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
                {
                  order.deliveryAddress
                }
              </div>
            </div>

            <Link
              href="/admin/orders"
              className="inline-flex rounded-2xl bg-surface px-6 py-4 font-medium text-text transition hover:opacity-80"
            >
              ← К списку заказов
            </Link>
          </div>

          <div>

            <div className="sticky top-4 space-y-6">

              <div className="rounded-3xl bg-surface p-6">
                <h2 className="mb-6 text-2xl font-semibold text-text">
                  Управление
                </h2>

                <select
                  value={order.status}
                  onChange={(e) =>
                    handleStatusChange(
                      e.target.value
                    )
                  }
                  className="mb-4 w-full rounded-2xl border border-border bg-background px-4 py-3"
                >
                  <option value="Created">
                    Создан
                  </option>

                  <option value="Paid">
                    Оплачен
                  </option>

                  <option value="Assembling">
                    Собирается
                  </option>

                  <option value="Shipping">
                    В пути
                  </option>

                  <option value="Delivered">
                    Доставлен
                  </option>

                  <option value="Cancelled">
                    Отменён
                  </option>
                </select>

                <button
                  onClick={
                    handleForceCancel
                  }
                  className="w-full rounded-2xl bg-red-500 px-5 py-3 font-medium text-white"
                >
                  Принудительно отменить
                </button>
              </div>

              <div className="rounded-3xl bg-surface p-6">
                <h2 className="mb-6 text-2xl font-semibold text-text">
                  Итого
                </h2>

                <div className="space-y-4">

                  <div className="flex justify-between">
                    <span className="text-text-secondary">
                      Товаров
                    </span>

                    <span className="font-medium text-text">
                      {order.items.reduce(
                        (
                          sum,
                          item
                        ) =>
                          sum +
                          item.quantity,
                        0
                      )}
                    </span>
                  </div>

                  <div className="border-t border-border pt-5">
                    <div className="flex justify-between">
                      <span className="text-xl font-semibold text-text">
                        Общая сумма
                      </span>

                      <span className="text-3xl font-bold text-text">
                        {Number(
                          order
                            .totalAmount
                            .amount
                        ).toLocaleString(
                          "ru-RU"
                        )} ₽
                      </span>
                    </div>
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