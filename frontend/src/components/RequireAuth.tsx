import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useAuthStore, isAdmin } from "../store/authStore";
import { LoadingState } from "./ui/misc";

/**
 * Гейт для страниц персонала. Пока идёт silent refresh (initialized=false) —
 * показываем загрузку, чтобы не сбросить на /login до восстановления сессии.
 */
export function RequireAuth({
  children,
  requireAdmin = false,
}: {
  children: ReactNode;
  requireAdmin?: boolean;
}) {
  const { user, initialized } = useAuthStore();
  const location = useLocation();

  if (!initialized) return <LoadingState />;
  if (!user) return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  if (requireAdmin && !isAdmin(user)) return <Navigate to="/dashboard" replace />;

  return <>{children}</>;
}
