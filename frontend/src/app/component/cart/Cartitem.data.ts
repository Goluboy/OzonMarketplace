export interface CartItemType {
  id: string;
  name: string;
  variant?: string;   
  image: string;
  pricePerUnit: number;
  discount?: number;
  quantity: number;
  installment?: boolean;
  postpay?: boolean;
  stock?: number;
  limitedQty?: boolean;
}

export const cartItems: CartItemType[] = [
  {
    id: "item-1",
    name: "Apple iPhone 14 Pro Max",
    variant: "Цвет: Deep Purple · 256 GB",
    image: "https://ir-9.ozone.ru/s3/multimedia-1-7/wc1000/7506066571.jpg",
    pricePerUnit: 99_999,
    postpay:true,
    quantity: 2,
    discount: 10,
  },
  {
    id: "item-2",
    name: "Apple AirPods Pro 2-го поколения",
    variant: "Цвет: White · USB-C",
    image: "https://ir-9.ozone.ru/s3/multimedia-1-2/wc1000/8043298418.jpg",
    pricePerUnit: 24_990,
    postpay:true,
    quantity: 1,
  },
  {
    id: "item-3",
    name: "Apple Watch Series 9",
    variant: "Корпус: 45mm · Ремешок: Starlight",
    image: "https://ir-9.ozone.ru/s3/multimedia-1-p/wc1000/7362654973.jpg",
    pricePerUnit: 29_985,
    postpay:true,
    quantity: 2,
  },
];