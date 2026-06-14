import { api } from './api';
import { Money } from './product.service';

export interface PublicProductCard {
  id: string;
  name: string;
  price: Money;
  discountPrice?: Money;
  imageUrl: string | null;
  categoryId: number;
  categoryName?: string;
}

export interface PublicProductCursorPagedResult {
  items: PublicProductCard[];
  nextCursor: string | null;
  pageSize: number;
}

export interface GetPublicProductsParams {
  search?: string;
  categoryId?: number;
  minPrice?: number;
  maxPrice?: number;
  sortBy?: 'name' | 'price' | 'createdAt';
  sortOrder?: 'asc' | 'desc';
  cursor?: string | null;
  pageSize?: number;
}

class PublicService {
  private readonly baseUrl = '/api/products';

  async getProducts(params: GetPublicProductsParams = {}): Promise<PublicProductCursorPagedResult> {
    try {
      const queryParams = new URLSearchParams();
      
      if (params.search) queryParams.append('search', params.search);
      if (params.categoryId) queryParams.append('categoryId', params.categoryId.toString());
      if (params.minPrice) queryParams.append('minPrice', params.minPrice.toString());
      if (params.maxPrice) queryParams.append('maxPrice', params.maxPrice.toString());
      if (params.sortBy) queryParams.append('sortBy', params.sortBy);
      if (params.sortOrder) queryParams.append('sortOrder', params.sortOrder);
      if (params.cursor) queryParams.append('cursor', params.cursor);
      if (params.pageSize) queryParams.append('pageSize', params.pageSize.toString());
      
      const queryString = queryParams.toString();
      const url = queryString ? `${this.baseUrl}?${queryString}` : this.baseUrl;
      
      const result = await api.get<PublicProductCursorPagedResult>(url, false);
      
      const itemsWithDiscounts = await Promise.all(
        result.items.map(async (item) => {
          try {
            const discount = await this.getProductDiscount(item.id);
            if (discount && discount.isActive) {
              return { ...item, discountPrice: discount.salePrice };
            }
          } catch {
          }
          return item;
        })
      );
      
      return {
        ...result,
        items: itemsWithDiscounts,
      };
    } catch (error) {
      console.error('Ошибка загрузки товаров:', error);
      throw error;
    }
  }

  async getProductDiscount(productId: string): Promise<{ salePrice: Money; isActive: boolean } | null> {
    try {
      const response = await api.get<{ salePrice: Money; isActive: boolean }>(
        `/api/pricing/products/${productId}`,
        false
      );
      return response;
    } catch (error) {
      return null;
    }
  }
}

export const publicService = new PublicService();