"use client";

import Link from "next/link";
import { useState } from "react";

import { CartItem } from "@/app/component/cart/CartItem";
import { CartSummary } from "@/app/component/cart/CartSummary";
import { cartItems as initialItems, CartItemType } from "@/app/component/cart/Cartitem.data";
import {Header} from "@/app/component/layout/header/Header"
export default function CartPage() {
  const [items, setItems] = useState<CartItemType[]>(initialItems);

  const handleIncrease = (id: string) =>
    setItems((prev) =>
      prev.map((i) => (i.id === id ? { ...i, quantity: i.quantity + 1 } : i))
    );

  const handleDecrease = (id: string) =>
    setItems((prev) =>
      prev
        .map((i) => (i.id === id ? { ...i, quantity: i.quantity - 1 } : i))
        .filter((i) => i.quantity > 0)
    );

  const handleRemove = (id: string) =>
    setItems((prev) => prev.filter((i) => i.id !== id));

  const totalQty = items.reduce((s, i) => s + i.quantity, 0);

  const discount = items.reduce((s, i) => {
    if (!i.discount) return s;
    return s + Math.round(i.pricePerUnit * (i.discount / 100)) * i.quantity;
  }, 0);

  const totalPrice = items.reduce((s, i) => {
    const unit = i.discount
      ? Math.round(i.pricePerUnit * (1 - i.discount / 100))
      : i.pricePerUnit;
    return s + unit * i.quantity;
  }, 0);

  return (
    <>
    <Header></Header>
    <div className="container mx-auto py-8">
      <div className="mb-6 flex items-baseline justify-between">
        <h1 className="text-[22px] font-medium text-text">Корзина</h1>
        <Link
          href="/catalog"
          className="text-sm text-accent transition-colors hover:text-accent-dark"
        >
          ← Продолжить покупки
        </Link>
      </div>

      {items.length === 0 ? (
        <div className="flex flex-col items-center justify-center gap-3 rounded-xl bg-gray-50 bg-surface py-20 text-center">
          <p className="text-[15px] font-medium text-text">Корзина пуста</p>
          <Link href="/catalog" className="text-sm text-accent hover:text-accent-dark">
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
            item={item}
            isLast={index === items.length - 1}
            onIncrease={handleIncrease}
            onDecrease={handleDecrease}
            onRemove={handleRemove}
          />
        ))}
      </div>
    </div>

    <CartSummary
      itemCount={items.length}
      totalQty={totalQty}
      discount={discount}
      totalPrice={totalPrice}
    />
  </div>
      )}
    </div>
    </>
  );
}