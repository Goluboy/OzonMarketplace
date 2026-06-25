// app/component/product/ProductGallery.tsx
"use client";

import { useState } from "react";
import Image from "next/image";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { ProductPageDto } from "../../../../services/product.service";

interface ProductGalleryProps {
  product: ProductPageDto;
}

export function ProductGallery({ product }: ProductGalleryProps) {
  const [selectedImage, setSelectedImage] = useState(0);
  
  const images = product.images.length > 0 
    ? product.images.map(img => img.url)
    : ["/placeholder-image.jpg"];
  
  const nextImage = () => {
    setSelectedImage((prev) => (prev + 1) % images.length);
  };
  
  const prevImage = () => {
    setSelectedImage((prev) => (prev - 1 + images.length) % images.length);
  };

  return (
    <div className="sticky top-8">
      {/* Главное изображение */}
      <div className="relative aspect-square bg-white rounded-2xl border border-gray-200 overflow-hidden mb-4">
        <img
          src={images[selectedImage]}
          alt={product.name}
          className="object-contain p-8"
          sizes="(max-width: 768px) 100vw, 50vw"
        />
        
        {/* Кнопки навигации */}
        {images.length > 1 && (
          <>
            <button
              onClick={prevImage}
              className="absolute left-4 top-1/2 -translate-y-1/2 w-10 h-10 bg-white/80 backdrop-blur-sm rounded-full flex items-center justify-center shadow-lg hover:bg-white transition-colors"
            >
              <ChevronLeft size={20} />
            </button>
            <button
              onClick={nextImage}
              className="absolute right-4 top-1/2 -translate-y-1/2 w-10 h-10 bg-white/80 backdrop-blur-sm rounded-full flex items-center justify-center shadow-lg hover:bg-white transition-colors"
            >
              <ChevronRight size={20} />
            </button>
          </>
        )}
      </div>
      
      {/* Миниатюры */}
      {images.length > 1 && (
        <div className="flex gap-2 overflow-x-auto pb-2">
          {images.map((img, idx) => (
            <button
              key={idx}
              onClick={() => setSelectedImage(idx)}
              className={`
                relative w-20 h-20 flex-shrink-0 rounded-lg overflow-hidden border-2 transition-all
                ${selectedImage === idx ? 'border-blue-500 shadow-md' : 'border-gray-200 hover:border-gray-300'}
              `}
            >
              <img
                src={img}
                alt={`${product.name} - ${idx + 1}`}
                className="object-cover"
                sizes="80px"
              />
            </button>
          ))}
        </div>
      )}
    </div>
  );
}