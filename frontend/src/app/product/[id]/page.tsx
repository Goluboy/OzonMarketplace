import { notFound } from "next/navigation";
import { Metadata } from "next";
import { Header } from "@/app/component/layout/header/Header";
import { ProductGallery } from "@/app/component/product/ProductGallery";
import { ProductInfo } from "@/app/component/product/ProductInfo";
import { ProductBuyCard } from "@/app/component/product/ProductBuyCard";
import { productService } from "../../../../services/product.service";
import Link from 'next/link';


type Props = {
  params: Promise<{
    id: string;
  }>;
};

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { id } = await params;
  
  try {
    const product = await productService.getProductForPage(id);
    
    return {
      title: `${product.name} - купить по цене ${parseFloat(product.price.amount).toLocaleString("ru-RU")} ₽`,
      description: product.description.slice(0, 160),
      openGraph: {
        title: product.name,
        description: product.description.slice(0, 160),
        images: product.images[0]?.url,
      },
    };
  } catch {
    return {
      title: "Товар не найден",
    };
  }
}

export default async function ProductPage({ params }: Props) {
  const { id } = await params;
  
  let product;
  let error = null;
  
  try {
    product = await productService.getProductForPage(id);
  } catch (err) {
    console.error("Ошибка загрузки товара:", err);
    error = err instanceof Error ? err.message : "Товар не найден";
  }
  
  if (error || !product) {
    notFound();
  }

  return (
    <>
      <Header />

      <main className="max-w-7xl mx-auto px-4 py-8">
        <div className="mb-6 text-sm text-gray-500">
          <Link href="/">
            <span className="hover:text-gray-700">Главная</span>
          </Link>
          <span className="mx-2">/</span>
          {product.categoryName && (
            <>
              <span className="hover:text-gray-700">{product.categoryName}</span>
              <span className="mx-2">/</span>
            </>
          )}
          <span className="text-gray-900">{product.name}</span>
        </div>

        <div className="grid grid-cols-12 gap-8">
          {/* Галерея - 5 колонок */}
          <div className="col-span-5">
            <ProductGallery product={product} />
          </div>

          {/* Информация - 4 колонки */}
          <div className="col-span-4">
            <ProductInfo product={product} />
          </div>

          {/* Карточка покупки - 3 колонки */}
          <div className="col-span-3">
            <ProductBuyCard product={product} />
          </div>
        </div>

        {/* Описание товара */}
        <div className="mt-12">
          <div className="bg-white rounded-2xl border border-gray-200 p-8">
            <h2 className="text-2xl font-bold text-gray-900 mb-4">
              Описание товара
            </h2>
            <div className="prose prose-lg max-w-none">
              <p className="text-gray-700 whitespace-pre-wrap">
                {product.description}
              </p>
            </div>
          </div>
        </div>
        
        {/* Характеристики (если будут в будущем) */}
        {/* <ProductSpecs product={product} /> */}
      </main>
    </>
  );
}