import { api } from './api';
import { Money } from './product.service';

export interface ProductDiscountDto {
  productId: string;
  sellerId: string;
  salePrice: Money;
  isActive: boolean;
  updatedAt: string;
}

export interface SetDiscountRequest {
  salePrice: Money;
}

class PricingService {
  private readonly baseUrl = '/api/pricing';

  async getProductDiscount(productId: string): Promise<ProductDiscountDto> {
    try {
      const discount = await api.get<ProductDiscountDto>(
        `${this.baseUrl}/products/${productId}`,
        true
      );
      return discount;
    } catch (error) {
      console.error('Ошибка получения скидки:', error);
      throw error;
    }
  }

  async createDiscount(productId: string, data: SetDiscountRequest): Promise<ProductDiscountDto> {
    try {
      const discount = await api.post<ProductDiscountDto>(
        `${this.baseUrl}/products/${productId}`,
        data,
        true
      );
      return discount;
    } catch (error) {
      console.error('Ошибка создания скидки:', error);
      throw error;
    }
  }

  async updateDiscount(productId: string, data: SetDiscountRequest): Promise<ProductDiscountDto> {
    try {
      const discount = await api.put<ProductDiscountDto>(
        `${this.baseUrl}/products/${productId}`,
        data,
        true
      );
      return discount;
    } catch (error) {
      console.error('Ошибка обновления скидки:', error);
      throw error;
    }
  }

  async activateDiscount(productId: string): Promise<ProductDiscountDto> {
    try {
      const discount = await api.post<ProductDiscountDto>(
        `${this.baseUrl}/products/${productId}/activate`,
        undefined,
        true
      );
      return discount;
    } catch (error) {
      console.error('Ошибка активации скидки:', error);
      throw error;
    }
  }

  async deactivateDiscount(productId: string): Promise<ProductDiscountDto> {
    try {
      const discount = await api.post<ProductDiscountDto>(
        `${this.baseUrl}/products/${productId}/deactivate`,
        undefined,
        true
      );
      return discount;
    } catch (error) {
      console.error('Ошибка деактивации скидки:', error);
      throw error;
    }
  }
}

export const pricingService = new PricingService();