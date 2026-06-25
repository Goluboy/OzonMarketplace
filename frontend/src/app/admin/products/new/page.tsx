"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { Header } from "@/app/component/layout/header/Header";
import { useAuth } from "../../../../../providers/AuthProvider";
import { categoryService, Category } from "../../../../../services/category.service";
import { productService } from "../../../../../services/product.service";

export default function NewProductPage() {
  const router = useRouter();
  const { isAuthenticated, user, getToken } = useAuth();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  const [name, setName] = useState("");
  const [sku, setSku] = useState("");
  const [price, setPrice] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [description, setDescription] = useState("");
  const [imageFiles, setImageFiles] = useState<File[]>([]);
  const [imagePreviews, setImagePreviews] = useState<string[]>([]);
  const [uploadingImages, setUploadingImages] = useState(false);
  
  const [categories, setCategories] = useState<Category[]>([]);
  const [loadingCategories, setLoadingCategories] = useState(true);
  
  const [showCategoryModal, setShowCategoryModal] = useState(false);
  const [newCategoryName, setNewCategoryName] = useState("");
  const [newCategoryPath, setNewCategoryPath] = useState("");
  const [creatingCategory, setCreatingCategory] = useState(false);
  const [categoryError, setCategoryError] = useState<string | null>(null);

  useEffect(() => {
    const loadCategories = async () => {
      try {
        setLoadingCategories(true);
        const data = await categoryService.getCategories();
        console.log("Загруженные категории:", data);
        setCategories(data);
      } catch (err) {
        console.error("Ошибка загрузки категорий:", err);
        setError("Не удалось загрузить категории. Пожалуйста, обновите страницу.");
        setCategories([]);
      } finally {
        setLoadingCategories(false);
      }
    };
    
    loadCategories();
  }, []);

  const uploadImages = async (files: File[]): Promise<string[]> => {
    const fileNames = files.map(file => file.name);
    const { filesMetadata } = await productService.getUploadUrls(fileNames);
    
    const uploadPromises = files.map(async (file, index) => {
      const metadata = filesMetadata[index];
      if (!metadata) {
        throw new Error(`Нет метаданных для файла ${file.name}`);
      }
      
      await productService.uploadFileToS3(metadata.uploadUrl, file);
      
      return metadata.publicUrl;
    });
    
    const publicUrls = await Promise.all(uploadPromises);
    return publicUrls;
  };

  const handleImageChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(e.target.files || []);
    
    if (files.length > 5) {
      setError("Можно загрузить не более 5 изображений");
      return;
    }
    
    const maxSize = 10 * 1024 * 1024;
    const oversizedFiles = files.filter(file => file.size > maxSize);
    if (oversizedFiles.length > 0) {
      setError(`Файл ${oversizedFiles[0].name} превышает лимит 10MB`);
      return;
    }
    
    setImageFiles(files);
    
    const previews = files.map(file => URL.createObjectURL(file));
    setImagePreviews(previews);
  };

  const validateForm = () => {
    if (!name.trim()) {
      setError("Введите название товара");
      return false;
    }
    if (!sku.trim()) {
      setError("Введите артикул");
      return false;
    }
    const skuNumber = parseInt(sku);
    if (isNaN(skuNumber) || skuNumber <= 0) {
      setError("Введите корректный артикул (целое положительное число)");
      return false;
    }
    if (!price.trim() || isNaN(Number(price)) || Number(price) <= 0) {
      setError("Введите корректную цену");
      return false;
    }
    if (!categoryId) {
      setError("Выберите категорию");
      return false;
    }
    if (!description.trim()) {
      setError("Введите описание товара");
      return false;
    }
    if (description.length > 2000) {
      setError("Описание не должно превышать 2000 символов");
      return false;
    }
    if (imageFiles.length === 0) {
      setError("Загрузите хотя бы одно изображение");
      return false;
    }
    return true;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!validateForm()) {
      return;
    }
    
    setLoading(true);
    setError(null);
    setUploadingImages(true);
    
    try {
      console.log("Начинаем загрузку изображений...");
      const imageUrls = await uploadImages(imageFiles);
      console.log("Изображения загружены:", imageUrls);
      
      const productData = {
        sku: parseInt(sku),
        name: name.trim(),
        description: description.trim(),
        price: {
          amount: Number(price).toFixed(2),
          currency: "RUB",
        },
        categoryId: parseInt(categoryId),
        images: imageUrls.map(url => ({ url })),
      };
      
      console.log("Отправляем данные товара:", productData);
      
      const createdProduct = await productService.createProduct(productData);
      console.log("Товар создан:", createdProduct);
      
      router.push("/admin/products?success=created");
      
    } catch (err) {
      console.error("Ошибка при создании товара:", err);
      
      // Обрабатываем разные типы ошибок
      if (err instanceof Error) {
        if (err.message.includes("401")) {
          setError("Ошибка авторизации. Пожалуйста, выйдите и войдите заново.");
        } else if (err.message.includes("403")) {
          setError("У вас нет прав на создание товара. Требуются права администратора или продавца.");
        } else if (err.message.includes("400")) {
          setError("Неверные данные. Проверьте артикул (должен быть уникальным) и другие поля.");
        } else {
          setError(err.message || "Ошибка при создании товара");
        }
      } else {
        setError("Неизвестная ошибка при создании товара");
      }
    } finally {
      setUploadingImages(false);
      setLoading(false);
    }
  };

  useEffect(() => {
    return () => {
      imagePreviews.forEach(preview => URL.revokeObjectURL(preview));
    };
  }, [imagePreviews]);

  // Создание новой категории
  const handleCreateCategory = async () => {
    if (!newCategoryName.trim()) {
      setCategoryError("Введите название категории");
      return;
    }
    if (!newCategoryPath.trim()) {
      setCategoryError("Введите путь категории (например: electronics.phones)");
      return;
    }

    const pathRegex = /^[a-zA-Z0-9._]+$/;
    if (!pathRegex.test(newCategoryPath)) {
      setCategoryError("Путь может содержать только латиницу, цифры, точки и нижнее подчеркивание");
      return;
    }

    setCreatingCategory(true);
    setCategoryError(null);

    try {
      const newCategory = await categoryService.createCategory({
        name: newCategoryName.trim(),
        path: newCategoryPath.trim(),
      });
      
      setCategories(prev => [...prev, newCategory]);
      setCategoryId(String(newCategory.id));
      
      setShowCategoryModal(false);
      setNewCategoryName("");
      setNewCategoryPath("");
      
    } catch (err) {
      console.error("Ошибка создания категории:", err);
      if (err instanceof Error && err.message.includes("401")) {
        setCategoryError("Ошибка авторизации. Пожалуйста, выйдите и войдите заново.");
      } else if (err instanceof Error && err.message.includes("403")) {
        setCategoryError("У вас нет прав на создание категорий. Требуются права администратора.");
      } else {
        setCategoryError(err instanceof Error ? err.message : "Не удалось создать категорию");
      }
    } finally {
      setCreatingCategory(false);
    }
  };

  if (!isAuthenticated) {
    return (
      <>
        <Header />
        <div className="container mx-auto py-8 px-4 text-center">
          <p className="text-red-600">Пожалуйста, войдите в систему</p>
        </div>
      </>
    );
  }

  if (user?.role !== 'admin' && user?.role !== 'seller') {
    return (
      <>
        <Header />
        <div className="container mx-auto py-8 px-4 text-center">
          <p className="text-red-600">У вас нет прав доступа к этой странице</p>
        </div>
      </>
    );
  }

  return (
    <>
      <Header />

      <div className="container mx-auto py-8 px-4">
        <div className="mb-8 flex items-center justify-between">
          <div>
            <h1 className="text-[32px] font-bold text-gray-900">
              📦 Создание товара
            </h1>
            <p className="mt-2 text-gray-500">
              Добавление нового товара в каталог
            </p>
          </div>

          <Link
            href="/admin/products"
            className="rounded-2xl bg-gray-100 px-5 py-3 font-medium text-gray-700 transition hover:bg-gray-200"
          >
            ← Назад
          </Link>
        </div>

        {error && (
          <div className="mb-6 rounded-2xl bg-red-100 p-4 text-red-700">
            ❌ {error}
          </div>
        )}

        <form onSubmit={handleSubmit}>
          <div className="rounded-3xl bg-white border p-8">
            <div className="grid gap-6">
              {/* Название */}
              <div>
                <label className="mb-2 block text-sm font-medium text-gray-700">
                  Название товара *
                </label>
                <input
                  type="text"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder="iPhone 14"
                  disabled={loading}
                  maxLength={200}
                  className="w-full rounded-2xl border border-gray-200 bg-white px-5 py-4 text-gray-900 outline-none focus:border-blue-500"
                />
                <p className="mt-1 text-xs text-gray-500">
                  {name.length}/200 символов
                </p>
              </div>

              {/* Артикул */}
              <div>
                <label className="mb-2 block text-sm font-medium text-gray-700">
                  Артикул (SKU) *
                </label>
                <input
                  type="number"
                  value={sku}
                  onChange={(e) => setSku(e.target.value)}
                  placeholder="1001"
                  disabled={loading}
                  className="w-full rounded-2xl border border-gray-200 bg-white px-5 py-4 text-gray-900 outline-none focus:border-blue-500"
                />
                <p className="mt-1 text-xs text-gray-500">
                  Уникальный целочисленный идентификатор товара
                </p>
              </div>

              {/* Цена */}
              <div>
                <label className="mb-2 block text-sm font-medium text-gray-700">
                  Цена (в рублях) *
                </label>
                <input
                  type="number"
                  step="0.01"
                  value={price}
                  onChange={(e) => setPrice(e.target.value)}
                  placeholder="99999.00"
                  disabled={loading}
                  className="w-full rounded-2xl border border-gray-200 bg-white px-5 py-4 text-gray-900 outline-none focus:border-blue-500"
                />
                <p className="mt-1 text-xs text-gray-500">
                  Используйте точку для копеек, например: 1299.99
                </p>
              </div>

              {/* Категория */}
              <div>
                <label className="mb-2 block text-sm font-medium text-gray-700">
                  Категория *
                </label>
                <div className="flex gap-3">
                  <select
                    value={categoryId}
                    onChange={(e) => setCategoryId(e.target.value)}
                    disabled={loading || loadingCategories}
                    className="flex-1 rounded-2xl border border-gray-200 bg-white px-5 py-4 text-gray-900 outline-none focus:border-blue-500"
                  >
                    <option value="">Выберите категорию</option>
                    {categories.map((cat) => (
                      <option key={cat.id} value={cat.id}>
                        {cat.name} ({cat.path})
                      </option>
                    ))}
                  </select>
                  
                  <button
                    type="button"
                    onClick={() => setShowCategoryModal(true)}
                    className="rounded-2xl bg-green-50 px-5 py-4 text-green-600 font-medium transition hover:bg-green-100 whitespace-nowrap"
                  >
                    + Новая категория
                  </button>
                </div>
                {loadingCategories && (
                  <p className="mt-1 text-sm text-gray-500">Загрузка категорий...</p>
                )}
                {!loadingCategories && categories.length === 0 && (
                  <p className="mt-1 text-sm text-yellow-600">
                    Нет категорий. Создайте первую через кнопку
                  </p>
                )}
              </div>

              {/* Описание */}
              <div>
                <label className="mb-2 block text-sm font-medium text-gray-700">
                  Описание * (до 2000 символов)
                </label>
                <textarea
                  rows={6}
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="Описание товара..."
                  disabled={loading}
                  maxLength={2000}
                  className="w-full rounded-2xl border border-gray-200 bg-white px-5 py-4 text-gray-900 outline-none resize-none focus:border-blue-500"
                />
                <p className="mt-1 text-xs text-gray-500">
                  {description.length}/2000 символов
                </p>
              </div>

              {/* Изображения */}
              <div>
                <label className="mb-2 block text-sm font-medium text-gray-700">
                  Изображения * (до 5 шт, до 10MB каждый)
                </label>
                <input
                  type="file"
                  multiple
                  accept="image/jpeg,image/png,image/webp,image/gif"
                  onChange={handleImageChange}
                  disabled={loading}
                  className="w-full rounded-2xl border border-gray-200 bg-white px-5 py-4 text-gray-900"
                />
                <p className="mt-1 text-xs text-gray-500">
                  Поддерживаются форматы: JPEG, PNG, WebP, GIF
                </p>
                
                {imagePreviews.length > 0 && (
                  <div className="mt-4">
                    <p className="text-sm font-medium text-gray-700 mb-2">
                      Загружено изображений: {imagePreviews.length}
                    </p>
                    <div className="flex flex-wrap gap-4">
                      {imagePreviews.map((preview, index) => (
                        <div key={index} className="relative group">
                          <img
                            src={preview}
                            alt={`Превью ${index + 1}`}
                            className="h-24 w-24 rounded-lg object-cover border"
                          />
                          <button
                            type="button"
                            onClick={() => {
                              setImageFiles(prev => prev.filter((_, i) => i !== index));
                              setImagePreviews(prev => prev.filter((_, i) => i !== index));
                              URL.revokeObjectURL(preview);
                            }}
                            className="absolute -top-2 -right-2 bg-red-500 text-white rounded-full w-6 h-6 flex items-center justify-center text-sm hover:bg-red-600 transition"
                          >
                            ×
                          </button>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>

              {/* Кнопки */}
              <div className="flex gap-4 pt-4">
                <button
                  type="submit"
                  disabled={loading || uploadingImages}
                  className="rounded-2xl bg-primary px-8 py-4 font-semibold text-white transition hover:bg-primary disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {uploadingImages 
                    ? "📤 Загрузка изображений..." 
                    : loading 
                      ? "Создание товара..." 
                      : "Создать товар"}
                </button>

                <Link
                  href="/admin/products"
                  className="rounded-2xl bg-gray-100 px-8 py-4 font-semibold text-gray-700 transition hover:bg-gray-200"
                >
                  Отмена
                </Link>
              </div>
            </div>
          </div>
        </form>
      </div>

      {/* Модальное окно для создания категории */}
      {showCategoryModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-3xl p-8 max-w-md w-full max-h-[90vh] overflow-y-auto">
            <h2 className="text-2xl font-bold mb-4">➕ Создать категорию</h2>
            
            {categoryError && (
              <div className="mb-4 rounded-xl bg-red-100 p-3 text-red-700 text-sm">
                ❌ {categoryError}
              </div>
            )}
            
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Название категории *
                </label>
                <input
                  type="text"
                  value={newCategoryName}
                  onChange={(e) => setNewCategoryName(e.target.value)}
                  placeholder="Например: Смартфоны"
                  className="w-full rounded-xl border border-gray-200 px-4 py-3 outline-none focus:border-primary"
                />
              </div>
              
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Путь категории *
                </label>
                <input
                  type="text"
                  value={newCategoryPath}
                  onChange={(e) => setNewCategoryPath(e.target.value)}
                  placeholder="Например: electronics.smartphones"
                  className="w-full rounded-xl border border-gray-200 px-4 py-3 outline-none focus:border-primary"
                />
                <p className="text-xs text-gray-500 mt-1">
                  Уникальный идентификатор. Используйте латиницу, цифры и точки.
                  <br />
                  Примеры: electronics, electronics.phones
                </p>
              </div>
            </div>
            
            <div className="flex gap-3 mt-6">
              <button
                onClick={handleCreateCategory}
                disabled={creatingCategory}
                className="flex-1 rounded-xl bg-primary py-3 font-medium text-white transition hover:bg-primary disabled:opacity-50"
              >
                {creatingCategory ? "Создание..." : "Создать"}
              </button>
              <button
                onClick={() => {
                  setShowCategoryModal(false);
                  setCategoryError(null);
                  setNewCategoryName("");
                  setNewCategoryPath("");
                }}
                className="flex-1 rounded-xl bg-gray-100 py-3 font-medium text-gray-700 transition hover:bg-gray-200"
              >
                Отмена
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}