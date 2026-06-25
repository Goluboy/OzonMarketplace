import { api } from './api';
import { pricingService } from './pricing.service';

export interface Money {
  amount: string;
  currency: string;
}

export interface ProductPageDto extends ProductDto {
  discountPrice?: Money;
  discountActive?: boolean;
}

export interface ProductImage {
  url: string;
}

export interface ProductCardDto {
  id: string;
  name: string;
  price: Money;
  imageUrl: string | null;
  categoryId: number;
}

export interface ProductCursorPagedResult {
  items: ProductCardDto[];
  nextCursor: string | null;
  pageSize: number;
}

export interface CreateProductRequest {
  sku: number;
  name: string;
  description: string;
  price: Money;
  categoryId: number;
  images: ProductImage[];
}

export interface ProductDto {
  id: string;
  sku: number;
  sellerId: string;
  name: string;
  description: string;
  price: Money;
  categoryId: number;
  categoryName?: string;
  categoryPath?: string;
  images: ProductImage[];
  createdAt: string;
  updatedAt: string | null;
}

export interface GetProductsParams {
  search?: string;
  categoryId?: number;
  minPrice?: Money;
  maxPrice?: Money;
  sortBy?: 'name' | 'price' | 'createdAt';
  sortOrder?: 'asc' | 'desc';
  cursor?: string | null;
  pageSize?: number;
}

export interface UploadFileMetadata {
  fileName: string;
  uploadUrl: string;
  publicUrl: string;
}

export interface UploadFilesBatchRequest {
  fileNames: string[];
}

export interface UploadFilesBatchOutput {
  filesMetadata: UploadFileMetadata[];
}

class ProductService {
  private readonly baseUrl = '/api/products';

  async getProducts(params: GetProductsParams = {}): Promise<ProductCursorPagedResult> {
    try {
      const queryParams = new URLSearchParams();
      
      if (params.search) queryParams.append('search', params.search);
      if (params.categoryId) queryParams.append('categoryId', params.categoryId.toString());
      if (params.minPrice) queryParams.append('minPrice', JSON.stringify(params.minPrice));
      if (params.maxPrice) queryParams.append('maxPrice', JSON.stringify(params.maxPrice));
      if (params.sortBy) queryParams.append('sortBy', params.sortBy);
      if (params.sortOrder) queryParams.append('sortOrder', params.sortOrder);
      if (params.cursor) queryParams.append('cursor', params.cursor);
      if (params.pageSize) queryParams.append('pageSize', params.pageSize.toString());
      
      const queryString = queryParams.toString();
      const url = queryString ? `${this.baseUrl}?${queryString}` : this.baseUrl;

      const result = await api.get<ProductCursorPagedResult>(url, false);
      return result;
    } catch (error) {
      console.error('Ошибка загрузки товаров:', error);
      throw error;
    }
  }

  async getAllProductsForDiscounts(): Promise<ProductCardDto[]> {
    try {
      let allProducts: ProductCardDto[] = [];
      let cursor: string | null = null;
      let hasMore = true;
      let page = 1;
      const MAX_PAGES = 50;
      
      while (hasMore && page <= MAX_PAGES) {
        console.log(`Загрузка страницы ${page} товаров...`);
        
        const result = await this.getProducts({
          pageSize: 100,
          cursor: cursor || undefined,
          sortBy: 'createdAt',
          sortOrder: 'desc',
        });
        
        allProducts = [...allProducts, ...result.items];
        cursor = result.nextCursor;
        hasMore = !!result.nextCursor;
        page++;
      }
      
      console.log(`Всего загружено товаров: ${allProducts.length}`);
      return allProducts;
    } catch (error) {
      console.error('Ошибка загрузки всех товаров:', error);
      throw error;
    }
  }

  async getUploadUrls(fileNames: string[]): Promise<UploadFilesBatchOutput> {
    try {
      const result = await api.post<UploadFilesBatchOutput>(
        `${this.baseUrl}/upload-urls`,
        { fileNames } as UploadFilesBatchRequest,
        true
      );
      return result;
    } catch (error) {
      console.error('Ошибка получения URL для загрузки:', error);
      throw error;
    }
  }

  async uploadFileToS3(uploadUrl: string, file: File): Promise<void> {
    try {
      const response = await fetch(uploadUrl, {
        method: 'PUT',
        body: file,
        headers: {
          'Content-Type': file.type,
        },
      });

      if (!response.ok) {
        throw new Error(`Ошибка загрузки файла: ${response.status} ${response.statusText}`);
      }
    } catch (error) {
      console.error('Ошибка загрузки файла в S3:', error);
      throw error;
    }
  }

  async createProduct(data: CreateProductRequest): Promise<ProductDto> {
    try {
      const product = await api.post<ProductDto>(this.baseUrl, data, true);
      return product;
    } catch (error) {
      console.error('Ошибка создания товара:', error);
      throw error;
    }
  }

  async getProduct(id: string): Promise<ProductDto> {
    try {
      const product = await api.get<ProductDto>(`${this.baseUrl}/${id}`, false);
      return product;
    } catch (error) {
      console.error('Ошибка получения товара:', error);
      throw error;
    }
  }

  async updateProduct(id: string, data: Partial<CreateProductRequest>): Promise<ProductDto> {
    try {
      const product = await api.put<ProductDto>(`${this.baseUrl}/${id}`, data, true);
      return product;
    } catch (error) {
      console.error('Ошибка обновления товара:', error);
      throw error;
    }
  }

  async deleteProduct(id: string): Promise<void> {
    try {
      await api.delete(`${this.baseUrl}/${id}`, true);
    } catch (error) {
      console.error('Ошибка удаления товара:', error);
      throw error;
    }
  }

  async getProductForPage(id: string): Promise<ProductPageDto> {
    try {
      const product = await this.getProduct(id);
      
      try {
        const discount = await pricingService.getProductDiscount(id);
        if (discount?.isActive) {
          return {
            ...product,
            discountPrice: discount.salePrice,
            discountActive: true,
          };
        }
      } catch {
        
      }
      
      return product;
    } catch (error) {
      console.error('Ошибка получения товара:', error);
      throw error;
    }
  }
}

export const productService = new ProductService();