"use client";

import { useState } from "react";
import { ShoppingCart, Check, Minus, Plus, CreditCard } from "lucide-react";
import { useCart } from "@/contexts/CartContext";
import { ProductPageDto } from "../../../../services/product.service";

interface ProductBuyCardProps {
  product: ProductPageDto;
}

export function ProductBuyCard({ product }: ProductBuyCardProps) {
  const [quantity, setQuantity] = useState(1);
  const [added, setAdded] = useState(false);
  const { addToCart } = useCart();
  
  const regularPrice = parseFloat(product.price.amount);
  const discountPrice = product.discountPrice ? parseFloat(product.discountPrice.amount) : null;
  const currentPrice = discountPrice && discountPrice < regularPrice ? discountPrice : regularPrice;
  const totalPrice = currentPrice * quantity;
  
  const incrementQuantity = () => {
    if (quantity < 99) {
      setQuantity(prev => prev + 1);
    }
  };
  
  const decrementQuantity = () => {
    if (quantity > 1) {
      setQuantity(prev => prev - 1);
    }
  };
  
  const handleAddToCart = () => {
    addToCart({
      id: product.id,
      name: product.name,
      image: product.images?.[0]?.url || "/placeholder.png",
      price: parseFloat(product.price.amount),
      discountPrice: product.discountPrice
        ? parseFloat(product.discountPrice.amount)
        : undefined,
      quantity,
    });

    setAdded(true);

    setTimeout(() => {
      setAdded(false);
    }, 2000);
  };
  
  const handleBuyNow = () => {
    console.log("Покупка в 1 клик:", { productId: product.id, quantity });
  };

  return (
    <div className="sticky top-8">
      <div className="bg-gray-50 rounded-2xl p-6 space-y-6 border border-gray-200">
        {/* Цена */}
        <div className="text-center">
          <div className="text-3xl font-bold text-gray-900">
            {totalPrice.toLocaleString("ru-RU")} ₽
          </div>
          {quantity > 1 && (
            <div className="text-sm text-gray-500 mt-1">
              {currentPrice.toLocaleString("ru-RU")} ₽ × {quantity}
            </div>
          )}
        </div>
        
        {/* Выбор количества */}
        <div className="space-y-2">
          <div className="text-sm text-gray-600">Количество</div>
          <div className="flex items-center justify-between bg-white rounded-xl border border-gray-200 p-1">
            <button
              onClick={decrementQuantity}
              disabled={quantity <= 1}
              className="w-10 h-10 flex items-center justify-center rounded-lg hover:bg-gray-100 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              <Minus size={18} />
            </button>
            <span className="w-16 text-center font-medium text-lg">
              {quantity}
            </span>
            <button
              onClick={incrementQuantity}
              disabled={quantity >= 99}
              className="w-10 h-10 flex items-center justify-center rounded-lg hover:bg-gray-100 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              <Plus size={18} />
            </button>
          </div>
          <div className="text-xs text-gray-400">
            В наличии: много
          </div>
        </div>
        
        {/* Кнопки */}
        <div className="space-y-3">
          <button
            onClick={handleAddToCart}
            className={`
              w-full py-3 rounded-xl font-semibold transition-all duration-200 flex items-center justify-center gap-2
              ${added 
                ? 'bg-green-600 text-white' 
                : 'bg-[#540303] hover:bg-[#6b0404] text-white'
              }
            `}
          >
            {added ? (
              <>
                <Check size={20} />
                Добавлено!
              </>
            ) : (
              <>
                <ShoppingCart size={20} />
                В корзину
              </>
            )}
          </button>
          
          <button
            onClick={handleBuyNow}
            className="w-full py-3 rounded-xl font-semibold border-2 border-[#540303] text-[#540303] hover:bg-[#540303] hover:text-white transition-all duration-200 flex items-center justify-center gap-2"
          >
            <CreditCard size={20} />
            Купить в 1 клик
          </button>
        </div>
        
        {/* Доставка */}
        <div className="border-t border-gray-200 pt-4 space-y-2 text-sm">
          <div className="flex justify-between">
            <span className="text-gray-600">Доставка:</span>
            <span className="font-medium text-gray-900">Бесплатно</span>
          </div>
          <div className="flex justify-between">
            <span className="text-gray-600">Завтра:</span>
            <span className="text-gray-900">при заказе до 18:00</span>
          </div>
        </div>
      </div>
    </div>
  );
}