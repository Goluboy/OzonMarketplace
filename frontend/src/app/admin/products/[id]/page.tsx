"use client";

import Link from "next/link";
import { Header } from "@/app/component/layout/header/Header";

export default function EditProductPage() {
  // Временно. Потом заменить на GET /api/products/{id}
  const product = {
    id: "1",
    name: "iPhone 14",
    brand: "Apple",
    price: 99999,
    stock: 15,
    category: "Смартфоны",
    description:
      "Смартфон Apple iPhone 14 с OLED дисплеем и двойной камерой.",
  };

  return (
    <>
      <Header />

      <div className="container mx-auto py-8">
        {/* Верхняя панель */}
        <div className="mb-8 flex items-center justify-between">
          <div>
            <h1 className="text-[32px] font-bold text-text">
              ✏️ Редактирование товара
            </h1>

            <p className="mt-2 text-text-secondary">
              Изменение информации о товаре
            </p>
          </div>

          <Link
            href="/admin/products"
            className="
              rounded-2xl
              bg-surface
              px-5
              py-3
              font-medium
              text-text
              transition
              hover:opacity-80
            "
          >
            ← Назад
          </Link>
        </div>

        {/* Форма */}
        <div className="rounded-3xl bg-surface p-8">
          <div className="grid gap-6">
            {/* Название */}
            <div>
              <label className="mb-2 block text-sm font-medium text-text-secondary">
                Название товара
              </label>

              <input
                type="text"
                defaultValue={product.name}
                className="
                  w-full
                  rounded-2xl
                  border
                  border-border
                  bg-background
                  px-5
                  py-4
                  text-text
                  outline-none
                "
              />
            </div>

            {/* Бренд */}
            <div>
              <label className="mb-2 block text-sm font-medium text-text-secondary">
                Бренд
              </label>

              <input
                type="text"
                defaultValue={product.brand}
                className="
                  w-full
                  rounded-2xl
                  border
                  border-border
                  bg-background
                  px-5
                  py-4
                  text-text
                  outline-none
                "
              />
            </div>

            {/* Цена */}
            <div>
              <label className="mb-2 block text-sm font-medium text-text-secondary">
                Цена
              </label>

              <input
                type="number"
                defaultValue={product.price}
                className="
                  w-full
                  rounded-2xl
                  border
                  border-border
                  bg-background
                  px-5
                  py-4
                  text-text
                  outline-none
                "
              />
            </div>

            {/* Количество */}
            <div>
              <label className="mb-2 block text-sm font-medium text-text-secondary">
                Остаток на складе
              </label>

              <input
                type="number"
                defaultValue={product.stock}
                className="
                  w-full
                  rounded-2xl
                  border
                  border-border
                  bg-background
                  px-5
                  py-4
                  text-text
                  outline-none
                "
              />
            </div>

            {/* Категория */}
            <div>
              <label className="mb-2 block text-sm font-medium text-text-secondary">
                Категория
              </label>

              <select
                defaultValue={product.category}
                className="
                  w-full
                  rounded-2xl
                  border
                  border-border
                  bg-background
                  px-5
                  py-4
                  text-text
                  outline-none
                "
              >
                <option>Смартфоны</option>
                <option>Ноутбуки</option>
                <option>Комплектующие</option>
                <option>Аксессуары</option>
              </select>
            </div>

            {/* Описание */}
            <div>
              <label className="mb-2 block text-sm font-medium text-text-secondary">
                Описание
              </label>

              <textarea
                rows={6}
                defaultValue={product.description}
                className="
                  w-full
                  resize-none
                  rounded-2xl
                  border
                  border-border
                  bg-background
                  px-5
                  py-4
                  text-text
                  outline-none
                "
              />
            </div>

            {/* Изображение */}
            <div>
              <label className="mb-2 block text-sm font-medium text-text-secondary">
                Обновить изображение
              </label>

              <input
                type="file"
                className="
                  w-full
                  rounded-2xl
                  border
                  border-border
                  bg-background
                  px-5
                  py-4
                  text-text
                "
              />
            </div>

            {/* Кнопки */}
            <div className="flex gap-4 pt-4">
              <button
                className="
                  rounded-2xl
                  bg-accent
                  px-8
                  py-4
                  font-semibold
                  text-white
                  transition
                  hover:opacity-90
                "
              >
                Сохранить изменения
              </button>

              <button
                className="
                  rounded-2xl
                  bg-red-500
                  px-8
                  py-4
                  font-semibold
                  text-white
                  transition
                  hover:bg-red-600
                "
              >
                Удалить товар
              </button>

              <Link
                href="/admin/products"
                className="
                  rounded-2xl
                  bg-surface-secondary
                  px-8
                  py-4
                  font-semibold
                  text-text
                  transition
                  hover:opacity-80
                "
              >
                Отмена
              </Link>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}