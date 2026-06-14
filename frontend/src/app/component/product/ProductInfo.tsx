"use client";

import { Star, Heart, Share2, Shield, Truck, RotateCcw } from "lucide-react";
import { useState } from "react";
import { ProductPageDto } from "../../../../services/product.service";

interface ProductInfoProps {
  product: ProductPageDto;
}

export function ProductInfo({ product }: ProductInfoProps) {
  const [liked, setLiked] = useState(false);
  
  const regularPrice = parseFloat(product.price.amount);
  const discountPrice = product.discountPrice ? parseFloat(product.discountPrice.amount) : null;
  const hasDiscount = discountPrice !== null && discountPrice < regularPrice;
  const discountPercent = hasDiscount 
    ? Math.round(((regularPrice - discountPrice) / regularPrice) * 100)
    : 0;
  const savings = hasDiscount ? regularPrice - discountPrice : 0;

  return (
    <div className="space-y-6">
      {/* Бренд/Категория */}
      {product.categoryName && (
        <div className="text-sm text-gray-500">
          {product.categoryName}
        </div>
      )}
      
      {/* Название */}
      <h1 className="text-3xl font-bold text-gray-900 leading-tight">
        {product.name}
      </h1>
      
      {/* Рейтинг (если есть) */}
      <div className="flex items-center gap-4">
        <div className="flex items-center gap-1">
          <Star size={18} className="fill-yellow-400 stroke-yellow-400" />
          <span className="font-semibold">4.8</span>
          <span className="text-gray-400">★</span>
        </div>
        <button className="text-sm text-blue-600 hover:text-blue-700">
          127 отзывов
        </button>
        <button className="text-sm text-gray-500 hover:text-gray-600">
          Задать вопрос
        </button>
      </div>
      
      {/* Артикул */}
      <div className="text-sm text-gray-500">
        Артикул: {product.sku}
      </div>
      
      {/* Цены */}
      <div className="space-y-2">
        {hasDiscount ? (
          <>
            <div className="flex items-baseline gap-3">
              <span className="text-4xl font-bold text-gray-900">
                {discountPrice.toLocaleString("ru-RU")} ₽
              </span>
              <span className="text-xl text-gray-400 line-through">
                {regularPrice.toLocaleString("ru-RU")} ₽
              </span>
              <span className="bg-red-100 text-red-700 text-sm font-semibold px-2 py-1 rounded-lg">
                -{discountPercent}%
              </span>
            </div>
            <div className="text-sm text-green-600 font-medium">
              Экономия {savings.toLocaleString("ru-RU")} ₽
            </div>
          </>
        ) : (
          <span className="text-4xl font-bold text-gray-900">
            {regularPrice.toLocaleString("ru-RU")} ₽
          </span>
        )}
      </div>
      
      {/* Кнопки действий */}
      <div className="flex gap-3">
        <button
          onClick={() => setLiked(!liked)}
          className={`
            flex items-center gap-2 px-5 py-2.5 rounded-xl border transition-all
            ${liked 
              ? 'border-red-200 bg-red-50 text-red-600' 
              : 'border-gray-300 hover:border-red-300 text-gray-600 hover:text-red-600'
            }
          `}
        >
          <Heart size={18} className={liked ? 'fill-red-600' : ''} />
          <span>{liked ? 'В избранном' : 'В избранное'}</span>
        </button>
        
        <button className="flex items-center gap-2 px-5 py-2.5 rounded-xl border border-gray-300 hover:border-gray-400 text-gray-600 transition-all">
          <Share2 size={18} />
          <span>Поделиться</span>
        </button>
      </div>
      
      {/* Преимущества */}
      <div className="border-t border-gray-100 pt-6 space-y-3">
        <div className="flex items-center gap-3 text-sm text-gray-600">
          <Shield size={18} className="text-green-600" />
          <span>Оригинальная продукция с гарантией</span>
        </div>
        <div className="flex items-center gap-3 text-sm text-gray-600">
          <Truck size={18} className="text-blue-600" />
          <span>Бесплатная доставка от 3000 ₽</span>
        </div>
        <div className="flex items-center gap-3 text-sm text-gray-600">
          <RotateCcw size={18} className="text-purple-600" />
          <span>Возврат товара в течение 14 дней</span>
        </div>
      </div>
    </div>
  );
}