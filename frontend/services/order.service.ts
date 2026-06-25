import { authService } from "./auth.service";

const getBaseUrl = () => {
  if (typeof window === 'undefined') {
    return 'http://nginx-gateway';
  }
  return '';
};

const API_URL = getBaseUrl();
export interface CreateOrderRequest {
  customerName: string;
  customerEmail: string;
  deliveryAddress: string;

  items: {
    productId: string;
    quantity: number;
  }[];
}

export interface CreateOrderResponse {
  orderId: string;
}

export interface OrderDetails {
  id: string;
  status: string;

  createdAt: string;
  updatedAt: string;

  customerName: string;
  customerEmail: string;

  deliveryAddress: string;

  totalAmount: {
    amount: string;
    currency: string;
  };

  items: {
    productId: string;
    productName: string;

    quantity: number;

    priceAtPurchase: {
      amount: string;
      currency: string;
    };
  }[];
}

export interface OrdersResponse {
  items: OrderDetails[];

  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AdminOrder {
  id: string;
  customerId: string;

  customerName: string;
  customerEmail: string;

  deliveryAddress: string;

  status: string;

  createdAt: string;
  updatedAt: string;

  totalAmount: {
    amount: string;
    currency: string;
  };

  items: {
    productId: string;
    productName: string;

    quantity: number;

    priceAtPurchase: {
      amount: string;
      currency: string;
    };
  }[];
}

export interface AdminOrdersResponse {
  items: AdminOrder[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export async function createOrder(
  data: CreateOrderRequest
): Promise<CreateOrderResponse> {
  await authService.updateToken();

  const token = authService.getToken();

  const response = await fetch(
    `${API_URL}/api/orders`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",

        ...(token
          ? {
              Authorization: `Bearer ${token}`,
            }
          : {}),
      },
      body: JSON.stringify(data),
    }
  );

  if (!response.ok) {
    const text = await response.text();

    console.error("Ошибка создания заказа:", text);

    throw new Error("Не удалось создать заказ");
  }

  return response.json();
}

export async function getOrderById(
  orderId: string
): Promise<OrderDetails> {
  await authService.updateToken();

  const token = authService.getToken();

  const response = await fetch(
    `${API_URL}/api/orders/${orderId}`,
    {
      headers: {
        ...(token
          ? {
              Authorization: `Bearer ${token}`,
            }
          : {}),
      },
    }
  );

  if (!response.ok) {
    throw new Error("Не удалось загрузить заказ");
  }

  return response.json();
}

export async function getOrders(
  page: number = 1,
  pageSize: number = 20
): Promise<OrderDetails[]> {
  await authService.updateToken();

  const token = authService.getToken();

  const response = await fetch(
    `${API_URL}/api/orders?page=${page}&pageSize=${pageSize}`,
    {
      headers: {
        ...(token
          ? {
              Authorization: `Bearer ${token}`,
            }
          : {}),
      },
    }
  );

  if (!response.ok) {
    throw new Error("Не удалось загрузить заказы");
  }

  //console.log(authService.getToken());
  const data = await response.json();
  return data;
}

export async function getAdminOrders() {
  await authService.updateToken();

  const token = authService.getToken();

  const response = await fetch(
    `${API_URL}/api/admin/orders`,
    {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  );

  if (!response.ok) {
    throw new Error(
      "Не удалось получить список заказов"
    );
  }

  return response.json();
}

export async function updateOrderStatus(
  orderId: string,
  newStatus: string,
  comment = ""
) {
  await authService.updateToken();

  const token = authService.getToken();

  const response = await fetch(
    `${API_URL}/api/admin/orders/${orderId}/status`,
    {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify({
        newStatus,
        comment,
      }),
    }
  );

  if (!response.ok) {
    throw new Error(
      "Не удалось обновить статус"
    );
  }

  return response.json();
}

export async function forceCancelOrder(
  orderId: string,
  reason = "Отменено администратором"
) {
  await authService.updateToken();

  const token = authService.getToken();

  const response = await fetch(
    `${API_URL}/api/admin/orders/${orderId}/force-cancel`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify({
        reason,
      }),
    }
  );

  if (!response.ok) {
    throw new Error(
      "Не удалось отменить заказ"
    );
  }

  return response.json();
}

export async function getAdminOrderById(
  id: string
): Promise<AdminOrder> {
  await authService.updateToken();

  const token = authService.getToken();

  const response = await fetch(
    `${API_URL}/api/admin/orders/${id}`,
    {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  );

  if (!response.ok) {
    throw new Error(
      "Не удалось загрузить заказ"
    );
  }

  return response.json();
}
