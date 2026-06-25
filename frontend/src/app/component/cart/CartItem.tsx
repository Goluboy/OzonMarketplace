"use client";

import Image from "next/image";
import { CartItemType } from "./Cartitem.data";

interface CartItemProps {
  item: CartItemType;
  onIncrease: (id: string) => void;
  onDecrease: (id: string) => void;
  onRemove: (id: string) => void;
  onToggleFavorite?: (id: string) => void;
  onBuy?: (id: string) => void;
}

function fmt(n: number) {
  return n.toLocaleString("ru-RU") + " ₽";
}

// ── SVG-иконки из Ozon (оригинальные пути) ───────────────────────────────────

const IconHeart = () => (
  <svg width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
    <path d="M11.5 1.5c2.552 0 4.5 1.957 4.5 4.521 0 2.458-1.661 4.416-3.241 5.744-1.617 1.358-3.388 2.258-4.063 2.578-.443.21-.95.21-1.392 0-.675-.32-2.446-1.22-4.063-2.578C1.661 10.437 0 8.479 0 6.02 0 3.457 1.948 1.5 4.5 1.5c1.432 0 2.665.799 3.5 1.926.835-1.127 2.068-1.926 3.5-1.926" />
  </svg>
);

const IconTrash = () => (
  <svg width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
    <path d="M13 6.333c.417 0 .833.235.833.834 0 6.666-.416 8.333-5.833 8.333s-5.834-1.667-5.834-8.333c0-.6.418-.834.834-.834zm-6.667 2.5a.834.834 0 0 0-.833.834v1.666a.834.834 0 1 0 1.666 0V9.667a.834.834 0 0 0-.833-.834m3.333 0a.834.834 0 0 0-.833.834v1.666a.834.834 0 1 0 1.667 0V9.667a.834.834 0 0 0-.834-.834M8.466.5a2.5 2.5 0 0 1 2.371 1.709l.276.826c2.266.09 3.553.406 3.553 1.527 0 .938-.416.938-1.25.938H2.583c-.833 0-1.25 0-1.25-.937 0-1.122 1.288-1.438 3.555-1.528l.274-.826A2.5 2.5 0 0 1 7.535.5zm-.931 1.667a.83.83 0 0 0-.79.57l-.09.265Q7.297 3 8 3q.706 0 1.345.002l-.089-.266a.83.83 0 0 0-.79-.569z" />
  </svg>
);

const IconMinus = () => (
  <svg width="16" height="16" fill="currentColor">
    <path d="M13.416 6.75a1.25 1.25 0 0 1 0 2.5H2.583a1.25 1.25 0 1 1 0-2.5z" />
  </svg>
);

const IconPlus = () => (
  <svg width="16" height="16" fill="currentColor">
    <path d="M8 1.333c.69 0 1.25.56 1.25 1.25V6.75h4.166a1.25 1.25 0 0 1 0 2.5H9.25v4.166a1.25 1.25 0 0 1-2.5 0V9.25H2.583a1.25 1.25 0 0 1 0-2.5H6.75V2.583c0-.69.56-1.25 1.25-1.25" />
  </svg>
);

const IconCoin = ({ color = "currentColor" }: { color?: string }) => (
  <svg width="16" height="16" fill={color} viewBox="0 0 24 24">
    <path d="M21 12c0 7 0 7-9 7s-9 0-9-7c0-.5 0-1 1-1h16c1 0 1 .5 1 1m-1-3H4c-1 0-1-.5-1-1 0-3 2-3 9-3s9 0 9 3c0 .5 0 1-1 1" />
  </svg>
);

const IconSale = () => (
  <svg width="14" height="14" fill="currentColor" viewBox="0 0 16 16">
    <path d="M5.757 2.5c4.238-1.807 8.686-.81 9.922 2.225 1.235 3.034-1.204 6.967-5.443 8.773-4.238 1.808-8.676.811-9.914-2.227s1.196-6.964 5.435-8.77m7.35 3.32c-.564-1.384-3.378-2.001-6.305-.753S2.316 8.796 2.88 10.18s3.385 2.002 6.312.754c2.935-1.251 4.48-3.73 3.915-5.114" />
  </svg>
);

export function CartItem({
  item,
  onIncrease,
  onDecrease,
  onRemove,
  onToggleFavorite,
  onBuy,
}: CartItemProps) {
  const unitPrice = item.discount
    ? Math.round(item.pricePerUnit * (1 - item.discount / 100))
    : item.pricePerUnit;

  const totalPrice = unitPrice * item.quantity;
  const hasDiscount = totalPrice < item.pricePerUnit * item.quantity;

  return (
    <div className="flex gap-3 py-3 px-2 border-border bg-gray-50">

      <div className="relative shrink-0" style={{ width: 88, height: 117 }}>
        <div
          className="relative overflow-hidden w-full h-full"
          style={{ borderRadius: 16, background: "var(--color-bg-secondary, #f3f6ff)" }}
        >
          <img
            src={item.image}
            alt={item.name}
            className="object-contain"
          />
        </div>
      </div>

      <div className="flex flex-1 flex-col gap-1.5 min-w-0 ">

        <p className="text-[14px] font-medium leading-snug text-text">
          {item.name}
        </p>

        {item.variant && (
          <p className="text-[12px] font-semibold text-text-secondary">
            {item.variant}
          </p>
        )}

        <div className="flex flex-wrap gap-1">
          {item.discount && (
            <span
              className="flex items-center gap-1 rounded-[6px] px-1.5 py-0.5 text-[12px] font-semibold leading-4"
              style={{ background: "rgb(241,17,126)", color: "#fff" }}
            >
              <IconSale />
              Распродажа
            </span>
          )}
          {item.installment && (
            <span
              className="flex items-center gap-1 rounded-[6px] px-1.5 py-0.5 text-[12px] font-semibold leading-4"
              style={{ background: "#005bff", color: "#fff" }}
            >
              <IconCoin color="rgb(203,247,120)" />
              0% до 140 дней
            </span>
          )}
          {item.stock != null && item.stock <= 5 && (
            <span
              className="flex items-center gap-1 rounded-[6px] px-1.5 py-0.5 text-[12px] font-semibold leading-4"
              style={{ background: "rgba(255,80,0,0.1)", color: "rgb(255,140,104)" }}
            >
              Осталось {item.stock} шт
            </span>
          )}
          {item.postpay && (
            <span
              className="flex items-center gap-1 rounded-[6px] px-1.5 py-0.5 text-[12px] font-semibold leading-4 border border-border"
              style={{ background: "transparent" }}
            >
              Постоплата
            </span>
          )}
        </div>

        <div className="flex items-center gap-1.5">
          <button
            className="flex items-center justify-center rounded-[8px] text-text-secondary hover:text-text transition-colors"
            style={{ width: 32, height: 32, background: "var(--color-bg-secondary)" }}
            onClick={() => onToggleFavorite?.(item.id)}
            aria-label="В избранное"
          >
            <IconHeart />
          </button>
          <button
            className="flex items-center justify-center rounded-[8px] text-text-secondary hover:text-text transition-colors"
            style={{ width: 32, height: 32, background: "var(--color-bg-secondary)" }}
            onClick={() => onRemove(item.id)}
            aria-label="Удалить"
          >
            <IconTrash />
          </button>
          <button
            className="flex items-center justify-center rounded-[8px] px-3 text-[14px] font-semibold h-8 text-text transition-colors hover:opacity-80"
            style={{ background: "var(--color-bg-secondary)" }}
            onClick={() => onBuy?.(item.id)}
          >
            Купить
          </button>
        </div>
      </div>

      <div className="grid grid-cols-[90px_120px] items-center gap-40 mt-0.5">

          <div className="flex flex-col items-end gap-1 w-[90px]">
            <div
              className="flex items-center rounded-[8px] overflow-hidden"
              style={{ height: 32, background: "var(--color-bg-secondary)" }}
            >
              <button
                className="flex items-center justify-center text-text hover:opacity-70 transition-opacity"
                style={{ width: 32, height: 32 }}
                onClick={() => onDecrease(item.id)}
                aria-label="Уменьшить"
              >
                <IconMinus />
              </button>
              <input
                type="text"
                inputMode="decimal"
                readOnly
                value={item.quantity}
                className="text-center text-[14px] font-semibold bg-transparent border-none outline-none text-text"
                style={{ width: 28 }}
              />
              <button
                className="flex items-center justify-center text-text hover:opacity-70 transition-opacity"
                style={{ width: 32, height: 32 }}
                onClick={() => onIncrease(item.id)}
                aria-label="Увеличить"
              >
                <IconPlus />
              </button>
            </div>

            {item.limitedQty && (
              <span className="text-[12px] font-bold text-red-500">
                Количество ограничено
              </span>
            )}
          </div>
          
          <div className="flex flex-col gap-0.5 w-[120px]">
            <div
              className="flex items-center gap-1 text-[16px] font-bold leading-5"
              style={{ color: hasDiscount ? "#005bff" : "var(--color-text-primary)" }}
            >
              {fmt(totalPrice)}
              <IconCoin color={hasDiscount ? "#005bff" : "currentColor"} />
            </div>
            <span className="text-[12px] font-semibold text-text-secondary">
              {fmt(item.pricePerUnit * item.quantity)}
            </span>
          </div>

        </div>

    </div>
  );
}