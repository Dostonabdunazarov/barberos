import axios, {
  type AxiosError,
  type AxiosRequestConfig,
  type InternalAxiosRequestConfig,
} from "axios";
import { useAuthStore } from "../store/authStore";
import type { LoginResponse } from "../types";

/**
 * HTTP-клиент. В dev запросы к /api проксируются на backend (см. vite.config.ts).
 * withCredentials — чтобы refresh-токен в httpOnly cookie ходил вместе с запросами.
 */
export const api = axios.create({
  baseURL: "/api",
  withCredentials: true,
});

/** Отдельный «сырой» клиент для refresh — без интерцепторов, чтобы не зациклиться. */
const refreshClient = axios.create({ baseURL: "/api", withCredentials: true });

// ── Запрос: подставляем access-токен ─────────────────────────────────────────
api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = useAuthStore.getState().accessToken;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// ── Ответ: авто-refresh при 401 ──────────────────────────────────────────────
type RetriableConfig = AxiosRequestConfig & { _retry?: boolean };

// Единый общий промис refresh — параллельные 401 не плодят гонку.
let refreshPromise: Promise<string | null> | null = null;

async function doRefresh(): Promise<string | null> {
  try {
    const { data } = await refreshClient.post<LoginResponse>("/auth/refresh");
    useAuthStore.getState().setAuth(data.user, data.accessToken);
    return data.accessToken;
  } catch {
    useAuthStore.getState().clear();
    return null;
  }
}

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as (RetriableConfig & InternalAxiosRequestConfig) | undefined;
    const status = error.response?.status;
    const url = original?.url ?? "";

    // Не пытаемся рефрешить сами auth-эндпоинты и повторные попытки.
    const isAuthCall = url.includes("/auth/login") || url.includes("/auth/refresh");

    if (status === 401 && original && !original._retry && !isAuthCall) {
      original._retry = true;
      refreshPromise ??= doRefresh().finally(() => {
        refreshPromise = null;
      });
      const newToken = await refreshPromise;
      if (newToken) {
        original.headers = original.headers ?? {};
        original.headers.Authorization = `Bearer ${newToken}`;
        return api(original);
      }
    }
    return Promise.reject(error);
  },
);

/**
 * Silent login при старте приложения: пробуем восстановить сессию по cookie.
 * Всегда помечает стор как initialized (успех или нет).
 */
export async function bootstrapAuth(): Promise<void> {
  const store = useAuthStore.getState();
  try {
    const { data } = await refreshClient.post<LoginResponse>("/auth/refresh");
    store.setAuth(data.user, data.accessToken);
  } catch {
    store.clear();
  } finally {
    store.setInitialized();
  }
}

/** Достаёт человекочитаемое сообщение об ошибке из ответа API (ProblemDetails / {message}). */
export function apiErrorMessage(error: unknown, fallback = "Что-то пошло не так"): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as
      | { message?: string; detail?: string; title?: string; errors?: Record<string, string[]> }
      | undefined;
    if (data?.errors) {
      const first = Object.values(data.errors)[0]?.[0];
      if (first) return first;
    }
    return data?.detail || data?.message || data?.title || error.message || fallback;
  }
  return fallback;
}
