export interface CartItem {
  id: string;
  name: string;
  image: string;
  price: number;
  discountPrice?: number;
  quantity: number;
}