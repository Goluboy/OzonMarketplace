"use client";

import { useState, useEffect } from "react";
import Image from "next/image";
import Link from "next/link";
import { Header } from "@/app/component/layout/header/Header";
import { AdminGuard } from "../../component/AdminGuard";
import { productService, ProductCardDto } from "../../../../services/product.service";
import { pricingService, ProductDiscountDto, SetDiscountRequest } from "../../../../services/pricing.service";

interface ProductWithDiscount extends ProductCardDto {
  discount?: ProductDiscountDto | null;
  isLoadingDiscount: boolean;
  isUpdating: boolean;
  salePriceInput: string;
}

const EditDiscountModal = ({ 
  product, 
  onClose, 
  onSave,
  isSaving 
}: { 
  product: ProductWithDiscount; 
  onClose: () => void; 
  onSave: (productId: string, salePrice: string) => Promise<void>;
  isSaving: boolean;
}) => {
  const [salePrice, setSalePrice] = useState(product.salePriceInput);
  const [localError, setLocalError] = useState<string | null>(null);
  
  const regularPrice = parseFloat(product.price.amount);
  
  const handleSubmit = async () => {
    const price = parseFloat(salePrice);
    
    if (isNaN(price) || price <= 0) {
      setLocalError("Введите корректную цену");
      return;
    }
    
    if (price >= regularPrice) {
      setLocalError(`Скидочная цена должна быть меньше обычной (${regularPrice.toLocaleString("ru-RU")} ₽)`);
      return;
    }
    
    setLocalError(null);
    await onSave(product.id, salePrice);
    onClose();
  };
  
  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-3xl p-8 max-w-md w-full">
        <h2 className="text-2xl font-bold mb-4">
          {product.discount ? "✏️ Редактировать скидку" : "➕ Создать скидку"}
        </h2>
        <p className="text-gray-600 mb-4">{product.name}</p>
        <p className="text-sm text-gray-500 mb-2">
          Обычная цена: {regularPrice.toLocaleString("ru-RU")} ₽
        </p>
        
        {localError && (
          <div className="mb-4 rounded-xl bg-red-100 p-3 text-red-700 text-sm">
            ❌ {localError}
          </div>
        )}
        
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Скидочная цена (₽)
          </label>
          <input
            type="number"
            step="0.01"
            value={salePrice}
            onChange={(e) => setSalePrice(e.target.value)}
            placeholder="Введите скидочную цену"
            className="w-full rounded-xl border border-gray-200 px-4 py-3 outline-none focus:border-blue-500"
            autoFocus
          />
        </div>
        
        <div className="flex gap-3 mt-6">
          <button
            onClick={handleSubmit}
            disabled={isSaving}
            className="flex-1 rounded-xl bg-blue-600 py-3 font-medium text-white transition hover:bg-blue-700 disabled:opacity-50"
          >
            {isSaving ? "Сохранение..." : "Сохранить"}
          </button>
          <button
            onClick={onClose}
            className="flex-1 rounded-xl bg-gray-100 py-3 font-medium text-gray-700 transition hover:bg-gray-200"
          >
            Отмена
          </button>
        </div>
      </div>
    </div>
  );
};

function AdminDiscountsPageContent() {
  const [products, setProducts] = useState<ProductWithDiscount[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [selectedProduct, setSelectedProduct] = useState<ProductWithDiscount | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [nextCursor, setNextCursor] = useState<string | null>(null);
  const [hasMore, setHasMore] = useState(true);

  useEffect(() => {
    const loadFirstPage = async () => {
      try {
        setLoading(true);
        
        const result = await productService.getProducts({
          pageSize: 20,
          sortBy: 'createdAt',
          sortOrder: 'desc',
        });
        
        const newProducts: ProductWithDiscount[] = result.items.map(product => ({
          ...product,
          discount: null,
          isLoadingDiscount: true,
          isUpdating: false,
          salePriceInput: "",
        }));
        
        setProducts(newProducts);
        setNextCursor(result.nextCursor);
        setHasMore(!!result.nextCursor);
        
        for (const product of newProducts) {
          try {
            const discount = await pricingService.getProductDiscount(product.id);
            
            setProducts(prev => prev.map(p => 
              p.id === product.id 
                ? { 
                    ...p, 
                    discount, 
                    isLoadingDiscount: false,
                    salePriceInput: discount?.salePrice?.amount ?? ""
                  }
                : p
            ));
          } catch (err) {
            const errorMessage = err instanceof Error ? err.message : String(err);
            if (errorMessage.includes("404") || errorMessage.includes("Не найдена")) {
              setProducts(prev => prev.map(p => 
                p.id === product.id 
                  ? { ...p, discount: null, isLoadingDiscount: false, salePriceInput: "" }
                  : p
              ));
            } else {
              console.error(`Ошибка загрузки скидки для товара ${product.id}:`, err);
              setProducts(prev => prev.map(p => 
                p.id === product.id 
                  ? { ...p, isLoadingDiscount: false }
                  : p
              ));
            }
          }
        }
        
      } catch (err) {
        console.error("Ошибка загрузки:", err);
        const errorMessage = err instanceof Error ? err.message : "Не удалось загрузить товары";
        setError(errorMessage);
      } finally {
        setLoading(false);
      }
    };
    
    loadFirstPage();
  }, []);

  const handleLoadMore = async () => {
    if (!nextCursor || loadingMore) return;
    
    try {
      setLoadingMore(true);
      
      const result = await productService.getProducts({
        pageSize: 20,
        cursor: nextCursor,
        sortBy: 'createdAt',
        sortOrder: 'desc',
      });
      
      const newProducts: ProductWithDiscount[] = result.items.map(product => ({
        ...product,
        discount: null,
        isLoadingDiscount: true,
        isUpdating: false,
        salePriceInput: "",
      }));
      
      setProducts(prev => [...prev, ...newProducts]);
      setNextCursor(result.nextCursor);
      setHasMore(!!result.nextCursor);
      
      for (const product of newProducts) {
        try {
          const discount = await pricingService.getProductDiscount(product.id);
          
          setProducts(prev => prev.map(p => 
            p.id === product.id 
              ? { 
                  ...p, 
                  discount, 
                  isLoadingDiscount: false,
                  salePriceInput: discount?.salePrice?.amount ?? ""
                }
              : p
          ));
        } catch (err) {
          const errorMessage = err instanceof Error ? err.message : String(err);
          if (errorMessage.includes("404") || errorMessage.includes("Не найдена")) {
            setProducts(prev => prev.map(p => 
              p.id === product.id 
                ? { ...p, discount: null, isLoadingDiscount: false, salePriceInput: "" }
                : p
            ));
          } else {
            console.error(`Ошибка загрузки скидки для товара ${product.id}:`, err);
            setProducts(prev => prev.map(p => 
              p.id === product.id 
                ? { ...p, isLoadingDiscount: false }
                : p
            ));
          }
        }
      }
      
    } catch (err) {
      console.error("Ошибка загрузки:", err);
      const errorMessage = err instanceof Error ? err.message : "Не удалось загрузить товары";
      setError(errorMessage);
    } finally {
      setLoadingMore(false);
    }
  };

  const handleSaveDiscount = async (productId: string, salePriceInput: string) => {
    setIsSaving(true);
    
    try {
      const product = products.find(p => p.id === productId);
      if (!product) throw new Error("Товар не найден");
      
      const salePriceAmount = parseFloat(salePriceInput);
      const discountData: SetDiscountRequest = {
        salePrice: {
          amount: salePriceAmount.toFixed(2),
          currency: product.price.currency,
        }
      };
      
      let updatedDiscount: ProductDiscountDto;
      
      if (product.discount) {
        updatedDiscount = await pricingService.updateDiscount(product.id, discountData);
        setSuccessMessage(`Скидка для "${product.name}" обновлена`);
      } else {
        updatedDiscount = await pricingService.createDiscount(product.id, discountData);
        setSuccessMessage(`Скидка для "${product.name}" создана`);
      }
      
      setProducts(prev => prev.map(p => 
        p.id === productId 
          ? { 
              ...p, 
              discount: updatedDiscount,
              isUpdating: false,
              salePriceInput: salePriceInput
            }
          : p
      ));
      
      setTimeout(() => setSuccessMessage(null), 3000);
      
    } catch (err) {
      console.error("Ошибка сохранения скидки:", err);
      const errorMessage = err instanceof Error ? err.message : "Не удалось сохранить скидку";
      setError(errorMessage);
      setTimeout(() => setError(null), 3000);
    } finally {
      setIsSaving(false);
    }
  };

  const handleToggleDiscount = async (product: ProductWithDiscount, activate: boolean) => {
    setProducts(prev => prev.map(p => 
      p.id === product.id ? { ...p, isUpdating: true } : p
    ));
    
    try {
      let updatedDiscount: ProductDiscountDto;
      
      if (activate) {
        updatedDiscount = await pricingService.activateDiscount(product.id);
        setSuccessMessage(`Скидка для "${product.name}" активирована`);
      } else {
        updatedDiscount = await pricingService.deactivateDiscount(product.id);
        setSuccessMessage(`Скидка для "${product.name}" деактивирована`);
      }
      
      setProducts(prev => prev.map(p => 
        p.id === product.id 
          ? { ...p, discount: updatedDiscount, isUpdating: false }
          : p
      ));
      
      setTimeout(() => setSuccessMessage(null), 3000);
      
    } catch (err) {
      console.error("Ошибка изменения статуса скидки:", err);
      const errorMessage = err instanceof Error ? err.message : "Не удалось изменить статус скидки";
      setError(errorMessage);
      setProducts(prev => prev.map(p => 
        p.id === product.id ? { ...p, isUpdating: false } : p
      ));
      
      setTimeout(() => setError(null), 3000);
    }
  };

  if (loading) {
    return (
      <>
        <Header />
        <div className="container mx-auto py-8 px-4">
          <div className="flex items-center justify-center min-h-[400px]">
            <div className="text-center">
              <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
              <p className="text-gray-500">Загрузка товаров и скидок...</p>
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
        <div className="mb-6">
          <h1 className="text-[32px] font-bold text-gray-900">
            💸 Управление скидками
          </h1>
          <p className="mt-2 text-gray-500">
            Установка и управление скидочными ценами на товары
          </p>
        </div>

        {error && (
          <div className="mb-6 rounded-2xl bg-red-100 p-4 text-red-700">
            ❌ {error}
          </div>
        )}
        
        {successMessage && (
          <div className="mb-6 rounded-2xl bg-green-100 p-4 text-green-700">
            ✅ {successMessage}
          </div>
        )}

        <div className="space-y-5">
          {products.map((product) => (
            <div key={product.id} className="rounded-3xl bg-white border p-6 hover:shadow-lg transition-shadow">
              <div className="grid gap-6 lg:grid-cols-[120px_1fr_280px]">
                <div className="relative h-28 overflow-hidden rounded-2xl bg-gray-100">
                  {product.imageUrl ? (
                    <Image
                      src={product.imageUrl}
                      alt={product.name}
                      fill
                      className="object-contain p-3"
                      sizes="120px"
                    />
                  ) : (
                    <div className="flex items-center justify-center h-full">
                      <span className="text-4xl">📷</span>
                    </div>
                  )}
                </div>

                <div>
                  <h2 className="text-2xl font-semibold text-gray-900">
                    {product.name}
                  </h2>

                  <div className="mt-4 space-y-2">
                    <div className="text-gray-500">Обычная цена</div>
                    <div className="text-xl font-bold text-gray-900">
                      {parseFloat(product.price.amount).toLocaleString("ru-RU")} ₽
                    </div>
                  </div>

                  {product.isLoadingDiscount ? (
                    <div className="mt-5 text-gray-400">Загрузка скидки...</div>
                  ) : product.discount ? (
                    <div className="mt-5">
                      <div className="mb-2 text-gray-500">Скидочная цена</div>
                      <div className="text-2xl font-bold text-green-600">
                        {parseFloat(product.discount.salePrice.amount).toLocaleString("ru-RU")} ₽
                      </div>
                      <div className="text-sm text-gray-500 mt-1">
                        Экономия: {(parseFloat(product.price.amount) - parseFloat(product.discount.salePrice.amount)).toLocaleString("ru-RU")} ₽
                      </div>
                    </div>
                  ) : (
                    <div className="mt-5">
                      <div className="text-gray-500">Скидка</div>
                      <div className="text-gray-400 mt-1">Не установлена</div>
                    </div>
                  )}
                </div>

                <div className="flex flex-col justify-between gap-5">
                  <div>
                    <div className="text-gray-500">Статус скидки</div>
                    {product.isLoadingDiscount ? (
                      <div className="mt-2 text-gray-400">Загрузка...</div>
                    ) : product.discount ? (
                      <div className={`mt-2 text-lg font-semibold ${product.discount.isActive ? "text-green-600" : "text-red-600"}`}>
                        {product.discount.isActive ? "🟢 Активна" : "🔴 Отключена"}
                      </div>
                    ) : (
                      <div className="mt-2 text-gray-400">⚪ Не создана</div>
                    )}
                  </div>

                  <div className="flex flex-col gap-3">
                    {product.discount ? (
                      <>
                        {product.discount.isActive ? (
                          <button
                            onClick={() => handleToggleDiscount(product, false)}
                            disabled={product.isUpdating}
                            className="rounded-2xl bg-red-500 px-5 py-3 font-medium text-white transition hover:bg-red-600 disabled:opacity-50"
                          >
                            {product.isUpdating ? "..." : "🔴 Деактивировать"}
                          </button>
                        ) : (
                          <button
                            onClick={() => handleToggleDiscount(product, true)}
                            disabled={product.isUpdating}
                            className="rounded-2xl bg-green-500 px-5 py-3 font-medium text-white transition hover:bg-green-600 disabled:opacity-50"
                          >
                            {product.isUpdating ? "..." : "🟢 Активировать"}
                          </button>
                        )}
                        <button
                          onClick={() => setSelectedProduct(product)}
                          disabled={product.isUpdating}
                          className="rounded-2xl bg-primary px-5 py-3 font-medium text-white transition hover:bg-primary disabled:opacity-50"
                        >
                          ✏️ Редактировать цену
                        </button>
                      </>
                    ) : (
                      <button
                        onClick={() => setSelectedProduct(product)}
                        className="rounded-2xl bg-green-600 px-5 py-3 font-medium text-white transition hover:bg-green-700"
                      >
                        ➕ Создать скидку
                      </button>
                    )}
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>

        {products.length === 0 && (
          <div className="rounded-3xl bg-gray-50 p-20 text-center">
            <div className="text-2xl font-semibold text-gray-900">Товаров пока нет</div>
            <div className="mt-3 text-gray-500">Сначала создайте товары</div>
            <button
              onClick={() => window.location.href = '/admin/products/new'}
              className="mt-6 rounded-2xl bg-primary px-6 py-3 font-medium text-white transition hover:bg-primary"
            >
              Создать товар
            </button>
          </div>
        )}

        {/* Кнопка "Загрузить еще" */}
        {hasMore && nextCursor && (
          <div className="mt-8 text-center">
            <button
              onClick={handleLoadMore}
              disabled={loadingMore}
              className="rounded-2xl bg-gray-100 px-8 py-3 font-medium text-gray-700 transition hover:bg-gray-200 disabled:opacity-50"
            >
              {loadingMore ? "Загрузка..." : "Загрузить еще товары"}
            </button>
          </div>
        )}
      </div>

      {selectedProduct && (
        <EditDiscountModal
          product={selectedProduct}
          onClose={() => setSelectedProduct(null)}
          onSave={handleSaveDiscount}
          isSaving={isSaving}
        />
      )}
    </>
  );
}

export default function AdminDiscountsPage() {
  return (
    <AdminGuard>
      <AdminDiscountsPageContent />
    </AdminGuard>
  );
}