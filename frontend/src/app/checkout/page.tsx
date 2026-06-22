"use client";

import { Header } from "@/app/component/layout/header/Header";
import { useRouter } from "next/navigation";
import { useState, useEffect } from "react";
import { useCart } from "@/contexts/CartContext";
import { authService } from "../../../services/auth.service";
import { createOrder } from "../../../services/order.service";

export default function CheckoutPage() {
  const router = useRouter();

  const { items, clearCart } = useCart();

  const [paymentMethod, setPaymentMethod] = useState("card");

  const [customerName, setCustomerName] = useState("");
  const [customerEmail, setCustomerEmail] = useState("");
  const [deliveryAddress, setDeliveryAddress] = useState("");

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const discount = items.reduce((sum, item) => {
    if (!item.discountPrice) return sum;

    return (
      sum +
      (item.price - item.discountPrice) *
        item.quantity
    );
  }, 0);

  const totalPrice = items.reduce((sum, item) => {
    const currentPrice =
      item.discountPrice ?? item.price;

    return sum + currentPrice * item.quantity;
  }, 0);

  const productsPrice = items.reduce(
    (sum, item) => sum + item.price * item.quantity,
    0
  );

  async function handleSubmit(
    e: React.FormEvent
  ) {
    const user = authService.getUser();

    if (!user) {
      setError("Необходимо авторизоваться");
      return;
    }

    e.preventDefault();

    setError("");

    if (!customerName.trim()) {
      setError("Введите имя");
      return;
    }

    if (!customerEmail.trim()) {
      setError("Введите email");
      return;
    }

    if (!deliveryAddress.trim()) {
      setError("Введите адрес доставки");
      return;
    }

    const payload = {
      customerName,
      customerEmail,
      deliveryAddress,

      items: items.map((item) => ({
        productId: item.id,
        quantity: item.quantity,
      })),
    };

    setLoading(true);

    try {
      const result = await createOrder(payload);

      clearCart();

      router.push(
        `/orders/${result.orderId}`
      );
    } catch (error) {
      console.error(error);

      setError(
        "Не удалось оформить заказ"
      );
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    const user = authService.getUser();

    if (user) {
      setCustomerName(user.name);
      setCustomerEmail(user.email);
    }
  }, []);

  return (
    <>
      <Header />

      <div className="container mx-auto py-8">
        <h1 className="mb-6 text-[32px] font-bold text-text">
          Оформление заказа
        </h1>

        <form
          onSubmit={handleSubmit}
          className="grid gap-6 lg:grid-cols-[1fr_340px]"
        >

          <div className="space-y-6">

            <div className="rounded-3xl bg-surface p-6">
              <h2 className="mb-5 text-xl font-semibold text-text">
                Способ оплаты
              </h2>

              <div className="grid gap-3 md:grid-cols-3">
                <label
                  className={`cursor-pointer rounded-2xl bg-gray-50 p-4 transition ${
                    paymentMethod === "card"
                      ? "border-accent bg-accent/5"
                      : "border-border"
                  }`}
                >
                  <input
                    type="radio"
                    className="hidden"
                    value="card"
                    checked={paymentMethod === "card"}
                    onChange={(e) => setPaymentMethod(e.target.value)}
                  />

                  <div className="font-medium text-text">
                    Банковская карта
                  </div>

                  <div className="mt-1 text-sm text-text-secondary">
                    Visa · MasterCard · Мир
                  </div>
                </label>

                <label
                  className={`cursor-pointer rounded-2xl bg-gray-50 p-4 transition ${
                    paymentMethod === "sbp"
                      ? "border-accent bg-accent/5"
                      : "border-border"
                  }`}
                >
                  <input
                    type="radio"
                    className="hidden"
                    value="sbp"
                    checked={paymentMethod === "sbp"}
                    onChange={(e) => setPaymentMethod(e.target.value)}
                  />

                  <div className="font-medium text-text">
                    СБП
                  </div>

                  <div className="mt-1 text-sm text-text-secondary">
                    Быстрые платежи
                  </div>
                </label>

                <label
                  className={`cursor-pointer rounded-2xl bg-gray-50 p-4 transition ${
                    paymentMethod === "cash"
                      ? "border-accent bg-accent/5"
                      : "border-border"
                  }`}
                >
                  <input
                    type="radio"
                    className="hidden"
                    value="cash"
                    checked={paymentMethod === "cash"}
                    onChange={(e) => setPaymentMethod(e.target.value)}
                  />

                  <div className="font-medium text-text">
                    При получении
                  </div>

                  <div className="mt-1 text-sm text-text-secondary">
                    Наличными или картой
                  </div>
                </label>
              </div>
            </div>

            <div className="rounded-3xl bg-surface p-6">
              <h2 className="mb-5 text-xl font-semibold text-text">
                Контактные данные
              </h2>

              <div className="space-y-4">
                <input
                  value={customerName}
                  onChange={(e) => setCustomerName(e.target.value)}
                  className="w-full rounded-2xl bg-gray-50 border-border bg-background px-5 py-4 outline-none"
                  placeholder="Имя"
                />

                <input
                  type="email"
                  value={customerEmail}
                  onChange={(e) => setCustomerEmail(e.target.value)}
                  className="w-full rounded-2xl bg-gray-50 border-border bg-background px-5 py-4 outline-none"
                  placeholder="Email"
                />

                <input
                  value={deliveryAddress}
                  onChange={(e) => setDeliveryAddress(e.target.value)}
                  className="w-full rounded-2xl bg-gray-50 border-border bg-background px-5 py-4 outline-none"
                  placeholder="Адрес доставки"
                />
              </div>
            </div>


            <div className="rounded-3xl bg-surface p-6">
              <h2 className="mb-5 text-xl font-semibold text-text">
                Ваш заказ
              </h2>

              <div className="space-y-3">
                {items.map((item) => (
                  <div
                    key={item.id}
                    className="flex items-center justify-between rounded-2xl bg-surface-secondary px-4 py-4"
                  >
                    <div>
                      <div className="font-medium text-text">
                        {item.name}
                      </div>

                      <div className="text-sm text-text-secondary">
                        Количество: {item.quantity}
                      </div>
                    </div>

                    <div className="font-semibold text-text">
                      {(
                        (item.discountPrice ?? item.price) *
                        item.quantity
                      ).toLocaleString("ru-RU")} ₽
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>


  <div>
  <div className="sticky top-4 rounded-3xl bg-surface p-6 bg-gray-50">

    <h2 className="mb-5 text-xl font-semibold text-text ">
      Ваш заказ
    </h2>

              <div className="space-y-4">
  <div className="flex justify-between">
    <span className="text-text-secondary">
      Товары
    </span>

    <span className="font-medium text-text">
      {productsPrice.toLocaleString("ru-RU")} ₽
    </span>
  </div>

  <div className="flex justify-between">
    <span className="text-text-secondary">
      Скидка
    </span>

    <span className="font-medium text-green-500">
      -{discount.toLocaleString("ru-RU")} ₽
    </span>
  </div>

  <div className="flex justify-between">
    <span className="text-text-secondary">
      Доставка
    </span>

    <span className="font-medium text-green-500">
      Бесплатно
    </span>
  </div>

  <div className="border-t border-border pt-5">
    <div className="flex justify-between">
      <span className="text-xl font-semibold text-text">
        Итого
      </span>

      <span className="text-3xl font-bold text-text">
        {totalPrice.toLocaleString("ru-RU")} ₽
      </span>
    </div>
  </div>

  <button
    type="submit"
    disabled={loading}
    className="
      mt-6
      w-full
      rounded-2xl
      bg-accent
      py-4
      text-lg
      text-white
      font-semibold
      transition
      hover:opacity-90
      disabled:cursor-not-allowed
      disabled:opacity-50
      bg-primary
    "
  >
    {loading ? "Оформление..." : "Заказать"}
  </button>
</div>

              {error && (
                <div className="mt-5 rounded-2xl bg-red-50 p-4 text-sm text-red-500">
                  {error}
                </div>
              )}
            </div>
          </div>
        </form>
      </div>
    </>
  );
}