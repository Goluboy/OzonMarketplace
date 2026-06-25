"use client";

import Link from "next/link";
import { useState } from "react";
import { useCart } from "@/contexts/CartContext";
import { CartItem } from "@/app/component/cart/CartItem";
import { CartSummary } from "@/app/component/cart/CartSummary";
import { createOrder } from "../../../services/order.service";
import { cartItems as initialItems, CartItemType } from "@/app/component/cart/Cartitem.data";
import {Header} from "@/app/component/layout/header/Header"

export default function CartPage() {
  const {
    items,
    increaseQuantity,
    decreaseQuantity,
    removeItem,
  } = useCart();

  const { clearCart } = useCart();

  const totalQty = items.reduce((s, i) => s + i.quantity, 0);

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

  const handleCheckout = async () => {
    const customerName = prompt("Введите ФИО");

    if (!customerName) return;

    const customerEmail = prompt("Введите Email");

    if (!customerEmail) return;

    const deliveryAddress = prompt("Введите адрес доставки");

    if (!deliveryAddress) return;

    try {
      const payload = {
        customerName,
        customerEmail,
        deliveryAddress,

        items: items.map((item) => ({
          productId: item.id,
          quantity: item.quantity,
        })),
      };

      const result = await createOrder(payload);

      alert(
        `Заказ создан.\nНомер: ${result.orderId}`
      );

      clearCart();
    } catch (error) {
      console.error(error);

      alert("Ошибка оформления заказа");
    }
  };

  return (
    <>
    <Header></Header>
    <div className="container mx-auto py-8">
      <div className="mb-6 flex items-baseline justify-between">
        <h1 className="text-[22px] font-medium text-text">Корзина</h1>
        <Link
          href="/"
          className="text-sm text-accent transition-colors hover:text-accent-dark"
        >
          ← Продолжить покупки
        </Link>
      </div>

      {items.length === 0 ? (
        <div className="flex flex-col items-center justify-center gap-3 rounded-xl bg-gray-50 bg-surface py-20 text-center">
          <p className="text-[15px] font-medium text-text">Корзина пуста</p>
          <Link href="/" className="text-sm text-accent hover:text-accent-dark">
            Перейти в каталог →
          </Link>
        </div>
      ) : (
        <div className="grid gap-5 lg:grid-cols-[1fr_280px]">
  <div>
      <div className="overflow-hidden rounded-3xl">
        {items.map((item, index) => (
          <CartItem
            key={item.id}
            item={{
              ...item,
              image: item.image,
              pricePerUnit:
                item.discountPrice ?? item.price,
            }}
            onIncrease={increaseQuantity}
            onDecrease={decreaseQuantity}
            onRemove={removeItem}
          />
        ))}
      </div>
    </div>

    <CartSummary
      itemCount={items.length}
      totalQty={totalQty}
      discount={discount}
      totalPrice={totalPrice}
      onCheckout={handleCheckout}
    />
  </div>
      )}
    </div>
    </>
  );
}