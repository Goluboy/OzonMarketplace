'use client'

import { Search, User, LayoutDashboard } from 'lucide-react'
import Link from "next/link"
import { useState } from "react"
import { useRouter } from "next/navigation"
import { useAuth } from "../../../../../providers/AuthProvider"
import { useSearch } from "../../../../../contexts/SearchContext"
import { HeaderMenu } from "./Header-menu.data"
import { useEffect } from "react";

function cn(...classes: (string | boolean | undefined)[]) {
  return classes.filter(Boolean).join(' ')
}

export function Header() {
  const { isAuthenticated, user, login, logout, loading } = useAuth();
  const { searchQuery, setSearchQuery } = useSearch();
  const router = useRouter();
  const [localSearch, setLocalSearch] = useState(searchQuery);

  const canAccessAdmin = user?.role === 'admin' || user?.role === 'seller';

  const handleSearch = () => {
    const query = localSearch.trim();

    setSearchQuery(query);

    if (query) {
      router.push(
        "/?search=" +
        encodeURIComponent(query)
      );
    } else {
      router.push("/");
    }
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleSearch();
    }
  };

  useEffect(() => {
    setLocalSearch(searchQuery);
  }, [searchQuery]);

  return (
    <header className="grid grid-cols-[2fr_7fr_2.3fr] gap-7 items-center mt-3 mx-5 mb-20">
      {/* Логотип */}
      <div>
        <Link href={"/"}>
          <span className="uppercase text-xl font-bold">marketplace</span>
        </Link>
      </div>

      {/* Поиск */}
      <div className="rounded-lg p-1 flex items-center bg-[#540303]">
        <input
          type="text"
          value={localSearch}
          onChange={(e) => setLocalSearch(e.target.value)}
          onKeyPress={handleKeyPress}
          placeholder="Поиск товаров..."
          className="bg-white rounded-lg px-4 py-1.5 w-full outline-none"
        />
        <button onClick={handleSearch} className="px-6">
          <Search color="#fff" size={20} />
        </button>
      </div>

      {/* Правая часть */}
      <div className="flex gap-6 items-center ml-2 justify-end">
        {!loading && (
          <>
            {!isAuthenticated ? (
              <Link href="/login">
                <button
                  className="flex items-center flex-col hover:opacity-70 transition-opacity"
                >
                  <User size={20} />
                  <span className="text-sm font-medium">Войти</span>
                </button>
              </Link>
            ) : (
              <div className="flex items-center gap-4">
                <div className="text-right">
                  <div className="text-sm font-medium">
                    {user?.name || user?.email?.split('@')[0]}
                  </div>
                  {user?.role === 'admin' && (
                    <div className="text-xs text-red-600 font-medium">Администратор</div>
                  )}
                  {user?.role === 'seller' && (
                    <div className="text-xs text-blue-600 font-medium">Продавец</div>
                  )}
                  {user?.role === 'customer' && (
                    <div className="text-xs text-gray-500">Покупатель</div>
                  )}
                </div>
                <button
                  onClick={logout}
                  className="text-sm hover:opacity-70 transition-opacity"
                >
                  Выйти
                </button>
              </div>
            )}
          </>
        )}

        {/* Ссылка на админку - показываем только админам и продавцам */}
        {canAccessAdmin && (
          <Link
            href="/admin"
            className={cn(
              "flex items-center flex-col transition-opacity hover:opacity-100 opacity-50"
            )}
          >
            <LayoutDashboard size={20} />
            <span className="text-sm font-medium">Админка</span>
          </Link>
        )}

        {/* Обычное меню */}
        {HeaderMenu.map((item) => (
          <Link
            key={item.title}
            href={item.href}
            className={cn(
              "flex items-center flex-col transition-opacity hover:opacity-100 opacity-50"
            )}
          >
            <item.icon size={20} />
            <span className="text-sm font-medium">{item.title}</span>
          </Link>
        ))}
      </div>
    </header>
  )
}