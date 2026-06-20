"use client";
import Image from "next/image";
import { Heart } from "lucide-react";
import { useState } from "react";
import { cn } from "@/lib/utils/cn";
import Link from "next/link";

export interface Item {
  id: number;
  title: string;
  price: number;
  oldPrice?: number;
  rating?: number;
  reviewCount?: number;
  imageUrl: string | null;
  badge?: string;
}

interface ItemCardProps {
  item: Item;
  className?: string;
}

export function ItemCard({ item, className }: ItemCardProps) {
  const [liked, setLiked] = useState(false);
  const discount =
    item.oldPrice && item.oldPrice > item.price
      ? Math.round(((item.oldPrice - item.price) / item.oldPrice) * 100)
      : null;

  return (
    <div
      className={cn(
        "group relative flex flex-col bg-white rounded-xl overflow-hidden border border-gray-100 hover:shadow-lg transition-shadow duration-200 cursor-pointer",
        className
      )}
    >
      <Link href={`/product/${item.id}`} className="flex-1">
        <div className="relative aspect-square overflow-hidden bg-gray-50">
          {item.imageUrl ? (
            <Image
              src={item.imageUrl}
              alt={item.title}
              fill
              className="object-cover group-hover:scale-105 transition-transform duration-300"
              sizes="(max-width: 640px) 50vw, (max-width: 1024px) 33vw, 25vw"
            />
          ) : (
            <div className="flex items-center justify-center h-full bg-gray-100">
              <span className="text-4xl text-gray-400">📷</span>
            </div>
          )}

          {discount && (
            <span className="absolute top-2 left-2 bg-white text-black text-xs font-bold px-2 py-0.5 rounded-md z-10">
              -{discount}%
            </span>
          )}
          {item.badge && !discount && (
            <span className="absolute top-2 left-2 bg-gray-800 text-white text-xs font-medium px-2 py-0.5 rounded-md z-10">
              {item.badge}
            </span>
          )}
        </div>

        <div className="flex flex-col gap-1 p-3 flex-1">
          <div className="flex items-baseline gap-2 flex-wrap">
            <span className="text-lg font-bold text-gray-900">
              {item.price.toLocaleString("ru-RU")} ₽
            </span>
            {item.oldPrice && (
              <span className="text-sm text-gray-400 line-through">
                {item.oldPrice.toLocaleString("ru-RU")} ₽
              </span>
            )}
          </div>

          <p className="text-sm text-gray-700 line-clamp-2 leading-snug min-h-[40px]">
            {item.title}
          </p>

          {item.rating && (
            <div className="flex items-center gap-1 mt-auto pt-1">
              <span className="text-[#F4730E] text-xs">★</span>
              <span className="text-xs font-medium text-gray-700">
                {item.rating.toFixed(1)}
              </span>
              {item.reviewCount && (
                <span className="text-xs text-gray-400">({item.reviewCount})</span>
              )}
            </div>
          )}
        </div>
      </Link>

      <button
        onClick={(e) => {
          e.stopPropagation();
          setLiked((v) => !v);
        }}
        className="absolute top-2 right-2 w-8 h-8 flex items-center justify-center rounded-full bg-white/80 backdrop-blur-sm hover:bg-white transition-colors z-10"
        aria-label="В избранное"
      >
        <Heart
          size={16}
          className={cn(
            "transition-colors",
            liked ? "fill-red-500 stroke-red-500" : "stroke-gray-400"
          )}
        />
      </button>

      <div className="px-3 pb-3 transition-opacity duration-150">
        <button
          onClick={(e) => {
            e.stopPropagation();
            console.log("Добавлено в корзину:", item.id);
          }}
          className="w-full bg-[#540303] hover:bg-[#6b0404] active:bg-[#540303] text-white text-sm font-semibold py-2 rounded-lg transition-colors"
        >
          В корзину
        </button>
      </div>
    </div>
  );
}