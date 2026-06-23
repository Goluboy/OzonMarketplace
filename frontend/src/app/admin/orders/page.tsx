"use client";

import Link from "next/link";
import { useState, useEffect } from "react";
import { Header } from "@/app/component/layout/header/Header";
import {
  AdminOrder,
  getAdminOrders,
  updateOrderStatus,
  forceCancelOrder,
} from "../../../../services/order.service";

export default function AdminOrdersPage() {
  function getStatusText(status: string) {
    switch (status) {
      case "Created":
        return "Создан";

      case "Paid":
        return "Оплачен";

      case "Assembling":
        return "Комплектуется";

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

  const [orders, setOrders] = useState<
    AdminOrder[]
  >([]);

  async function handleStatusChange(
    orderId: string,
    status: string
  ) {
    try {
      await updateOrderStatus(
        orderId,
        status
      );

      setOrders((prev) =>
        prev.map((o) =>
          o.id === orderId
            ? { ...o, status }
            : o
        )
      );
    } catch (error) {
      console.error(error);
    }
  }

  async function handleForceCancel(
    orderId: string
  ) {
    if (
      !confirm(
        "Принудительно отменить заказ?"
      )
    ) {
      return;
    }

    try {
      const updated =
        await forceCancelOrder(orderId);

      setOrders((prev) =>
        prev.map((o) =>
          o.id === orderId
            ? updated
            : o
        )
      );
    } catch (error) {
      console.error(error);

      alert(
        "Не удалось отменить заказ"
      );
    }
  }

  useEffect(() => {
    async function loadOrders() {
      try {
        const response =
          await getAdminOrders();

        setOrders(response);
      } catch (error) {
        console.error(error);
      }
    }

    loadOrders();
  }, []);

  return (
    <>
      <Header />
      <div className="mb-4">
        <Link
          href="/admin"
          className="
            inline-flex
            items-center
            rounded-2xl
            bg-surface-secondary
            px-5
            py-3
            text-text
            transition
            hover:opacity-80
          "
        >
          ← В админ-панель
        </Link>
      </div>
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
                      Покупатель: {order.customerName}
                    </div>

                    <div className="text-text-secondary">
                      Дата:{" "}
                      {new Date(order.createdAt).toLocaleDateString("ru-RU")}
                    </div>

                    <div className="text-lg font-medium text-text">
                      Сумма:{" "}
                      {Number(
                        order.totalAmount
                      ).toLocaleString("ru-RU")} ₽
                    </div>
                  </div>
                </div>

                {/* Управление */}
                <div className="flex flex-col gap-4">
                  <select
                    value={order.status}
                    onChange={(e) =>
                      handleStatusChange(
                        order.id,
                        e.target.value
                      )
                    }
                  >
                    <option value="Created">Создан</option>
                    <option value="Paid">Оплачен</option>
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

                  <div className="flex gap-3">
                    <button
                    onClick={() =>
                      handleForceCancel(order.id)
                    }
                    disabled={
                      order.status === "Delivered" ||
                      order.status === "Cancelled"
                    }
                    className={`
                      rounded-2xl
                      px-5
                      py-3
                      font-medium
                      text-white
                      ${
                        order.status === "Delivered" || order.status === "Cancelled"
                          ? "bg-gray-400 cursor-not-allowed"
                          : "bg-red-500 hover:opacity-90"
                      }
                    `}
                  >
                    Отменить
                  </button>

                  <Link
                    href={`/admin/orders/${order.id}`}
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