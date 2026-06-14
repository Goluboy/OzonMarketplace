import { Button } from "@/app/component/ui/Button";

interface CartSummaryProps {
  itemCount: number;
  totalQty: number;
  discount: number;
  totalPrice: number;
}

function formatPrice(value: number) {
  return value.toLocaleString("ru-RU") + " ₽";
}

export function CartSummary({
  itemCount,
  totalQty,
  discount,
  totalPrice,
}: CartSummaryProps) {
  return (
    <div className="sticky top-6 h-fit rounded-xl bg-gray-50 bg-surface p-5">
      <h2 className="mb-4 text-[15px] font-medium text-text">Ваш заказ</h2>

      <div className="space-y-1">
        <div className="flex items-center justify-between py-1.5">
          <span className="text-sm text-text-secondary">Товаров</span>
          <span className="text-sm font-medium text-text">
            {itemCount} {itemCount === 1 ? "позиция" : itemCount < 5 ? "позиции" : "позиций"}
          </span>
        </div>

        <div className="flex items-center justify-between py-1.5">
          <span className="text-sm text-text-secondary">Количество</span>
          <span className="text-sm font-medium text-text">{totalQty} шт.</span>
        </div>

        {discount > 0 && (
          <div className="flex items-center justify-between py-1.5">
            <span className="text-sm text-text-secondary">Скидка</span>
            <span className="text-sm font-medium text-accent">
              −{formatPrice(discount)}
            </span>
          </div>
        )}

        <div className="my-1 h-px bg-border" />

        <div className="flex items-center justify-between py-1.5">
          <span className="text-[14px] font-medium text-text">Итого</span>
          <span className="text-[20px] font-medium text-text">
            {formatPrice(totalPrice)}
          </span>
        </div>
      </div>

      <Button className="mt-4 h-10 w-full bg-primary text-white hover:bg-accent-dark">
        Оформить заказ →
      </Button>

      <p className="mt-3 text-center text-[11px] leading-relaxed text-text-secondary">
        Переход к оформлению и выбору доставки
      </p>
    </div>
  );
}