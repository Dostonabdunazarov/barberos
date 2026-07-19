import axios from "axios";

/**
 * HTTP-клиент. В dev запросы к /api проксируются на backend (см. vite.config.ts).
 * withCredentials — чтобы refresh-токен в httpOnly cookie ходил вместе с запросами.
 */
export const api = axios.create({
  baseURL: "/api",
  withCredentials: true,
});

// TODO: интерцептор для авто-обновления access-токена через /auth/refresh при 401.
