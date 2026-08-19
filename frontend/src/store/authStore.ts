import { create } from "zustand";
import type { AuthUser } from "../types";
import { UserRole } from "../types";

interface AuthState {
  user: AuthUser | null;
  accessToken: string | null;
  /** false, пока не завершилась стартовая попытка refresh (silent login). */
  initialized: boolean;
  setAuth: (user: AuthUser, accessToken: string) => void;
  setInitialized: () => void;
  clear: () => void;
}

/**
 * Состояние авторизации персонала. Access-токен живёт только в памяти
 * (не в localStorage — защита от XSS); при перезагрузке восстанавливается
 * через silent refresh по httpOnly cookie.
 */
export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  accessToken: null,
  initialized: false,
  setAuth: (user, accessToken) => set({ user, accessToken }),
  setInitialized: () => set({ initialized: true }),
  clear: () => set({ user: null, accessToken: null }),
}));

export const isAdmin = (user: AuthUser | null) => user?.role === UserRole.Admin;
