import { api } from './api';

export interface Category {
  id: number;
  name: string;
  path: string;
}

export interface CreateCategoryRequest {
  name: string;
  path: string;
}

export interface UpdateCategoryRequest {
  name: string;
  path: string;
}

class CategoryService {
  private readonly baseUrl = '/api/categories';

  async getCategories(): Promise<Category[]> {
    try {
      const categories = await api.get<Category[]>(this.baseUrl, false);
      return categories;
    } catch (error) {
      console.error('Ошибка загрузки категорий:', error);
      throw error;
    }
  }

  async createCategory(data: CreateCategoryRequest): Promise<Category> {
    try {
      const category = await api.post<Category>(this.baseUrl, data, true);
      return category;
    } catch (error) {
      console.error('Ошибка создания категории:', error);
      throw error;
    }
  }

  async updateCategory(id: number, data: UpdateCategoryRequest): Promise<Category> {
    try {
      const category = await api.put<Category>(`${this.baseUrl}/${id}`, data, true);
      return category;
    } catch (error) {
      console.error('Ошибка обновления категории:', error);
      throw error;
    }
  }

  async deleteCategory(id: number): Promise<void> {
    try {
      await api.delete(`${this.baseUrl}/${id}`, true);
    } catch (error) {
      console.error('Ошибка удаления категории:', error);
      throw error;
    }
  }
}

export const categoryService = new CategoryService();