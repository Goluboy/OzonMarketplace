"use client";

import { useState, useEffect, Suspense } from "react";
import Image from "next/image";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { Header } from "@/app/component/layout/header/Header";
import { AdminGuard } from "../../component/AdminGuard";
import { productService, ProductCardDto } from "../../../../services/product.service";

function AdminProductsPageContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const successMessage = searchParams.get("success");
  
  const [products, setProducts] = useState<ProductCardDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [nextCursor, setNextCursor] = useState<string | null>(null);
  const [loadingMore, setLoadingMore] = useState(false);

  useEffect(() => {
    const loadProducts = async (cursor?: string) => {
      try {
        if (cursor) {
          setLoadingMore(true);
        } else {
          setLoading(true);
        }
        
        const result = await productService.getProducts({
          pageSize: 12,
          cursor: cursor,
          sortBy: 'createdAt',
          sortOrder: 'desc',
        });
        
        if (cursor) {
          setProducts(prev => [...prev, ...result.items]);
        } else {
          setProducts(result.items);
        }
        
        setNextCursor(result.nextCursor);
      } catch (err) {
        console.error("Ошибка загрузки товаров:", err);
        setError(err instanceof Error ? err.message : "Не удалось загрузить товары");
      } finally {
        setLoading(false);
        setLoadingMore(false);
      }
    };
    
    loadProducts();
  }, []);

  const handleLoadMore = async () => {
    if (!nextCursor || loadingMore) return;
    
    try {
      setLoadingMore(true);
      const result = await productService.getProducts({
        pageSize: 12,
        cursor: nextCursor,
        sortBy: 'createdAt',
        sortOrder: 'desc',
      });
      
      setProducts(prev => [...prev, ...result.items]);
      setNextCursor(result.nextCursor);
    } catch (err) {
      console.error("Ошибка загрузки товаров:", err);
      setError(err instanceof Error ? err.message : "Не удалось загрузить товары");
    } finally {
      setLoadingMore(false);
    }
  };

  const handleDelete = async (productId: string, productName: string) => {
    if (!confirm(`Вы уверены, что хотите удалить товар "${productName}"?`)) {
      return;
    }
    
    setDeletingId(productId);
    try {
      await productService.deleteProduct(productId);
      setProducts(prev => prev.filter(p => p.id !== productId));
    } catch (err) {
      console.error("Ошибка удаления:", err);
      setError(err instanceof Error ? err.message : "Не удалось удалить товар");
    } finally {
      setDeletingId(null);
    }
  };

  useEffect(() => {
    if (successMessage === "created") {
      const timer = setTimeout(() => {
        router.replace("/admin/products");
      }, 3000);
      return () => clearTimeout(timer);
    }
  }, [successMessage, router]);

  if (loading) {
    return (
      <>
        <Header />
        <div className="container mx-auto py-8">
          <div className="flex items-center justify-center height-[400px]">
            <div className="text-center">
              <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
              <p className="text-gray-500">Загрузка товаров...</p>
            </div>
          </div>
        </div>
      </>
    );
  }

  return (
    <>
      <Header />
      <div className="mb-4">
        <Link
          href="/admin"
          className="
            inline-flex
            items-center
            rounded-2xl
            bg-surface-secondary
            px-5
            py-3
            text-text
            transition
            hover:opacity-80
          "
        >
          ← В админ-панель
        </Link>
      </div>
      <div className="container mx-auto py-8 px-4">
        {/* Верхняя панель */}
        <div className="mb-6 flex items-center justify-between flex-wrap gap-4">
          <div>
            <h1 className="text-[32px] font-bold text-gray-900">
              📦 Товары
            </h1>

            <p className="mt-2 text-gray-500">
              Управление товарами магазина
            </p>
          </div>

          <Link
            href="/admin/products/new"
            className="rounded-2xl bg-primary px-6 py-4 font-semibold text-white transition hover:bg-primary"
          >
            + Создать товар
          </Link>
        </div>

        {/* Сообщение об успехе */}
        {successMessage === "created" && (
          <div className="mb-6 rounded-2xl bg-green-100 p-4 text-green-700">
            ✅ Товар успешно создан!
          </div>
        )}

        {/* Ошибка */}
        {error && (
          <div className="mb-6 rounded-2xl bg-red-100 p-4 text-red-700">
            ❌ {error}
          </div>
        )}

        {/* Карточки товаров */}
        {products.length === 0 && !error ? (
          <div className="rounded-3xl bg-gray-50 p-20 text-center">
            <p className="text-gray-500 mb-4">Нет товаров</p>
            <Link
              href="/admin/products/new"
              className="inline-block rounded-2xl bg-primary px-6 py-3 font-semibold text-white transition hover:bg-primary"
            >
              Создать первый товар
            </Link>
          </div>
        ) : (
          <>
            <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
              {products.map((product) => (
                <div
                  key={product.id}
                  className="rounded-3xl bg-white border p-6 hover:shadow-lg transition-shadow"
                >
                  {/* Фото */}
                  <div className="relative h-60 overflow-hidden rounded-2xl bg-gray-100">
                    {product.imageUrl ? (
                      <Image
                        src={product.imageUrl}
                        alt={product.name}
                        fill
                        className="object-contain p-5"
                      />
                    ) : (
                      <div className="flex items-center justify-center h-full">
                        <span className="text-4xl">📷</span>
                      </div>
                    )}
                  </div>

                  {/* Информация */}
                  <div className="mt-5">
                    <h2 className="text-xl font-semibold text-gray-900 line-clamp-1">
                      {product.name}
                    </h2>

                    <div className="mt-4 text-2xl font-bold text-gray-900">
                      {Number(product.price.amount).toLocaleString("ru-RU")} ₽
                    </div>
                  </div>

                  {/* Кнопки */}
                  <div className="mt-6 flex gap-3">
                    <Link
                      href={`/admin/products/${product.id}`}
                      className="flex-1 rounded-2xl bg-primary px-5 py-3 text-center font-medium text-white transition hover:bg-primary"
                    >
                      Изменить
                    </Link>

                    <button
                      onClick={() => handleDelete(product.id, product.name)}
                      disabled={deletingId === product.id}
                      className="rounded-2xl bg-red-500 px-5 py-3 font-medium text-white transition hover:bg-red-600 disabled:opacity-50"
                    >
                      {deletingId === product.id ? "..." : "Удалить"}
                    </button>
                  </div>
                </div>
              ))}
            </div>

            {/* Кнопка "Загрузить еще" */}
            {nextCursor && (
              <div className="mt-8 text-center">
                <button
                  onClick={handleLoadMore}
                  disabled={loadingMore}
                  className="rounded-2xl bg-gray-100 px-8 py-3 font-medium text-gray-700 transition hover:bg-gray-200 disabled:opacity-50"
                >
                  {loadingMore ? "Загрузка..." : "Загрузить еще"}
                </button>
              </div>
            )}
          </>
        )}
      </div>
    </>
  );
}

function AdminProductsPageLoading() {
  return (
    <>
      <Header />
      <div className="container mx-auto py-8">
        <div className="flex items-center justify-center h-[400px]">
          <div className="text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
            <p className="text-gray-500">Загрузка товаров...</p>
          </div>
        </div>
      </div>
    </>
  );
}

export default function AdminProductsPage() {
  return (
    <AdminGuard>
      <Suspense fallback={<AdminProductsPageLoading />}>
        <AdminProductsPageContent />
      </Suspense>
    </AdminGuard>
  );
}