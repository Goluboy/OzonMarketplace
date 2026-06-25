"use client";

import { useState } from "react";

interface FiltersProps {
  onSearch: (search: string) => void;
  onSortChange: (sort: string) => void;
  onPriceChange: (min: number | null, max: number | null) => void;
  loading?: boolean;
}

export function Filters({ onSearch, onSortChange, onPriceChange, loading }: FiltersProps) {
  const [searchTerm, setSearchTerm] = useState("");
  const [sortBy, setSortBy] = useState("createdAt");
  const [sortOrder, setSortOrder] = useState("desc");
  const [minPrice, setMinPrice] = useState<string>("");
  const [maxPrice, setMaxPrice] = useState<string>("");

  const handleSearch = () => {
    onSearch(searchTerm);
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === "Enter") {
      handleSearch();
    }
  };

  const handleSortChange = (sort: string, order: string) => {
    setSortBy(sort);
    setSortOrder(order);
    onSortChange(`${sort}:${order}`);
  };

  const handlePriceFilter = () => {
    const min = minPrice ? parseFloat(minPrice) : null;
    const max = maxPrice ? parseFloat(maxPrice) : null;
    onPriceChange(min, max);
  };

  return (
    <div className="mb-8 space-y-4">
      {/* Поиск */}
      <div className="flex gap-3">
        <input
          type="text"
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          onKeyPress={handleKeyPress}
          placeholder="Поиск товаров..."
          disabled={loading}
          className="flex-1 rounded-2xl border border-gray-200 px-5 py-3 outline-none focus:border-blue-500"
        />
        <button
          onClick={handleSearch}
          disabled={loading}
          className="rounded-2xl bg-blue-600 px-6 py-3 font-medium text-white transition hover:bg-blue-700 disabled:opacity-50"
        >
          🔍 Найти
        </button>
      </div>

      {/* Фильтры */}
      <div className="flex flex-wrap gap-4 items-end">
        {/* Сортировка */}
        <div>
          <label className="block text-sm text-gray-600 mb-1">Сортировка</label>
          <select
            value={`${sortBy}:${sortOrder}`}
            onChange={(e) => {
              const [newSort, newOrder] = e.target.value.split(":");
              handleSortChange(newSort, newOrder);
            }}
            disabled={loading}
            className="rounded-xl border border-gray-200 px-4 py-2 outline-none focus:border-blue-500"
          >
            <option value="createdAt:desc">Новые сначала</option>
            <option value="createdAt:asc">Старые сначала</option>
            <option value="price:asc">Цена: по возрастанию</option>
            <option value="price:desc">Цена: по убыванию</option>
            <option value="name:asc">Название: А-Я</option>
            <option value="name:desc">Название: Я-А</option>
          </select>
        </div>

        {/* Цена от */}
        <div>
          <label className="block text-sm text-gray-600 mb-1">Цена от (₽)</label>
          <input
            type="number"
            value={minPrice}
            onChange={(e) => setMinPrice(e.target.value)}
            placeholder="0"
            disabled={loading}
            className="w-32 rounded-xl border border-gray-200 px-4 py-2 outline-none focus:border-blue-500"
          />
        </div>

        {/* Цена до */}
        <div>
          <label className="block text-sm text-gray-600 mb-1">Цена до (₽)</label>
          <input
            type="number"
            value={maxPrice}
            onChange={(e) => setMaxPrice(e.target.value)}
            placeholder="999999"
            disabled={loading}
            className="w-32 rounded-xl border border-gray-200 px-4 py-2 outline-none focus:border-blue-500"
          />
        </div>

        <button
          onClick={handlePriceFilter}
          disabled={loading}
          className="rounded-xl bg-gray-100 px-5 py-2 font-medium text-gray-700 transition hover:bg-gray-200 disabled:opacity-50"
        >
          Применить
        </button>

        <button
          onClick={() => {
            setMinPrice("");
            setMaxPrice("");
            onPriceChange(null, null);
          }}
          disabled={loading}
          className="rounded-xl text-gray-500 px-3 py-2 hover:text-gray-700"
        >
          Сбросить
        </button>
      </div>
    </div>
  );
}