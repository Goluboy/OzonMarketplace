"use client";

import { useState, useEffect, Suspense } from "react";
import { useSearchParams } from "next/navigation";
import { Header } from "@/app/component/layout/header/Header";
import { ItemGrid } from "@/app/component/home/itemCard/ItemGrid";
import { publicService } from "../../services/public.service";
import { convertToItemCards } from "../../services/product.converter";
import { type Item } from "@/app/component/home/itemCard/ItemCard";
import { SearchProvider, useSearch } from "../../contexts/SearchContext";

function HomeContent() {
  const searchParams = useSearchParams();
  const { searchQuery, setSearchQuery } = useSearch();
  const [items, setItems] = useState<Item[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [nextCursor, setNextCursor] = useState<string | null>(null);
  
  useEffect(() => {
    const searchFromUrl = searchParams.get('search');
    if (searchFromUrl) {
      setSearchQuery(searchFromUrl);
    }
  }, [searchParams, setSearchQuery]);

  useEffect(() => {
    const loadFirstPage = async () => {
      try {
        setLoading(true);
        
        const result = await publicService.getProducts({
          search: searchQuery || undefined,
          sortBy: 'createdAt',
          sortOrder: 'desc',
          pageSize: 20,
        });
        
        const newItems = convertToItemCards(result.items);
        setItems(newItems);
        setNextCursor(result.nextCursor);
        
      } catch (err) {
        console.error("Ошибка загрузки:", err);
        setError(err instanceof Error ? err.message : "Не удалось загрузить товары");
      } finally {
        setLoading(false);
      }
    };
    
    loadFirstPage();
  }, [searchQuery]);

  const handleLoadMore = async () => {
    if (!nextCursor || loadingMore) return;
    
    try {
      setLoadingMore(true);
      
      const result = await publicService.getProducts({
        search: searchQuery || undefined,
        sortBy: 'createdAt',
        sortOrder: 'desc',
        cursor: nextCursor,
        pageSize: 20,
      });
      
      const newItems = convertToItemCards(result.items);
      setItems(prev => [...prev, ...newItems]);
      setNextCursor(result.nextCursor);
      
    } catch (err) {
      console.error("Ошибка загрузки:", err);
      setError(err instanceof Error ? err.message : "Не удалось загрузить товары");
    } finally {
      setLoadingMore(false);
    }
  };

  const hasMore = !!nextCursor;

  return (
    <>
      <Header />
      
      <main className="w-full px-4 py-6">
        {error && (
          <div className="mb-6 rounded-xl bg-red-100 p-4 text-red-700">
            ❌ {error}
          </div>
        )}
        
        <ItemGrid items={items} loading={loading} />
        
        {!loading && hasMore && items.length > 0 && (
          <div className="mt-8 text-center">
            <button
              onClick={handleLoadMore}
              disabled={loadingMore}
              className="rounded-xl bg-gray-100 px-6 py-2 font-medium text-gray-700 transition hover:bg-gray-200 disabled:opacity-50"
            >
              {loadingMore ? "Загрузка..." : "Загрузить еще"}
            </button>
          </div>
        )}
        
        {!loading && !hasMore && items.length > 0 && (
          <div className="mt-8 text-center text-gray-400 text-sm">
            🎉 Вы просмотрели все {items.length} товаров
          </div>
        )}
      </main>
    </>
  );
}

// 🆕 Fallback компонент для Suspense
function HomeLoading() {
  return (
    <>
      <Header />
      <main className="w-full px-4 py-6">
        <div className="flex items-center justify-center h-[400px]">
          <div className="text-center">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
            <p className="text-gray-500">Загрузка товаров...</p>
          </div>
        </div>
      </main>
    </>
  );
}

export default function Home() {
  return (
    <SearchProvider>
      {/* 🆕 Suspense boundary оборачивает компонент с useSearchParams */}
      <Suspense fallback={<HomeLoading />}>
        <HomeContent />
      </Suspense>
    </SearchProvider>
  );
}