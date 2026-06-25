import { authService } from './auth.service';

const getBaseUrl = () => {
  return process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';
};

const API_BASE_URL = getBaseUrl();

interface ApiOptions extends RequestInit {
  requireAuth?: boolean;
}

async function request<T, D = unknown>(
  endpoint: string,
  options: ApiOptions & { body?: string | FormData }
): Promise<T> {
  const { requireAuth = true, ...fetchOptions } = options;
  
  const headers: Record<string, string> = {};
  
  if (!(fetchOptions.body instanceof FormData)) {
    headers['Content-Type'] = 'application/json';
  }
  
  if (requireAuth) {
    const token = authService.getToken();
    if (!token) {
      throw new Error('Не авторизован');
    }
    headers['Authorization'] = `Bearer ${token}`;
  }
  
  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    ...fetchOptions,
    headers,
  });
  
  if (!response.ok) {
    let errorMessage = `Ошибка ${response.status}`;
    try {
      const error = await response.json() as { title?: string; detail?: string };
      errorMessage = error.title || error.detail || errorMessage;
    } catch {
      errorMessage = `Ошибка ${response.status}: ${response.statusText}`;
    }
    throw new Error(errorMessage);
  }
  
  if (response.status === 204) {
    return {} as T;
  }
  
  return response.json() as Promise<T>;
}

function prepareBody(data: unknown): string | FormData | undefined {
  if (!data) return undefined;
  if (data instanceof FormData) return data;
  return JSON.stringify(data);
}

export const api = {
  get: <T>(endpoint: string, requireAuth: boolean = true): Promise<T> => 
    request<T>(endpoint, { method: 'GET', requireAuth }),
    
  post: <T>(endpoint: string, data?: unknown, requireAuth: boolean = true): Promise<T> => 
    request<T>(endpoint, { 
      method: 'POST', 
      body: prepareBody(data),
      requireAuth 
    }),
    
  put: <T>(endpoint: string, data?: unknown, requireAuth: boolean = true): Promise<T> => 
    request<T>(endpoint, { 
      method: 'PUT', 
      body: prepareBody(data),
      requireAuth 
    }),
    
  delete: <T>(endpoint: string, requireAuth: boolean = true): Promise<T> =>
    request<T>(endpoint, { method: 'DELETE', requireAuth }),
    
  patch: <T>(endpoint: string, data?: unknown, requireAuth: boolean = true): Promise<T> => 
    request<T>(endpoint, { 
      method: 'PATCH', 
      body: prepareBody(data),
      requireAuth 
    }),
};