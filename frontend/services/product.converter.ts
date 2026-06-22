import { PublicProductCard } from "./public.service";
import { type Item } from "@/app/component/home/itemCard/ItemCard";

export function convertToItemCard(product: PublicProductCard): Item {
  const regularPrice = parseFloat(product.price.amount);
  const discountPrice = product.discountPrice ? parseFloat(product.discountPrice.amount) : null;
  const hasDiscount = discountPrice !== null && discountPrice < regularPrice;
  
  return {
    id: product.id,
    title: product.name,
    price: hasDiscount ? discountPrice : regularPrice,
    oldPrice: hasDiscount ? regularPrice : undefined,
    rating: undefined,
    reviewCount: undefined,
    imageUrl: product.imageUrl,
    badge: undefined,
  };
}

export function convertToItemCards(products: PublicProductCard[]): Item[] {
  return products.map(convertToItemCard);
}