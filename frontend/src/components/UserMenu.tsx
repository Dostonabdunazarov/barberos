import { useEffect, useRef, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { motion, AnimatePresence } from "framer-motion";
import { useAuthStore, isAdmin } from "../store/authStore";
import type { AuthUser } from "../types";
import { api } from "../lib/api";
import { cn } from "../lib/utils";

/** Инициалы для аватара: из имени, иначе из email. */
function initials(user: AuthUser): string {
  const src = user.name?.trim() || user.email;
  const parts = src.split(/[\s@._-]+/).filter(Boolean);
  const chars = parts.length >= 2 ? parts[0][0] + parts[1][0] : src.slice(0, 2);
  return chars.toUpperCase();
}

/**
 * Меню авторизованного пользователя — одна компактная кнопка (аватар + имя),
 * раскрывающая дропдаун: Кабинет, Админ (если админ), Выход. Заменяет три
 * ссылки в ряд, экономит место в шапке на мобиле. Паттерн — как LanguageSwitcher:
 * стеклянный поповер, закрытие по клику вне и Escape.
 */
export function UserMenu() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { user, clear } = useAuthStore();
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    function onDown(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    }
    function onKey(e: KeyboardEvent) {
      if (e.key === "Escape") setOpen(false);
    }
    document.addEventListener("mousedown", onDown);
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("mousedown", onDown);
      document.removeEventListener("keydown", onKey);
    };
  }, [open]);

  if (!user) return null;

  async function handleLogout() {
    setOpen(false);
    try {
      await api.post("/auth/logout");
    } catch {
      /* игнорируем — всё равно чистим локально */
    }
    clear();
    navigate("/");
  }

  const displayName = user.name?.trim() || user.email;
  const roleLabel = isAdmin(user) ? t("nav.admin") : t("role.master");

  return (
    <div className="relative" ref={ref}>
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        aria-haspopup="menu"
        aria-expanded={open}
        className="flex items-center gap-2 rounded-lg border border-white/10 py-1.5 pl-1.5 pr-2.5 text-sm text-fg-muted transition-colors hover:border-white/20 hover:text-fg"
      >
        <span className="grid h-7 w-7 shrink-0 place-items-center rounded-md bg-accent-500/20 text-xs font-semibold text-accent-300">
          {initials(user)}
        </span>
        {/* На мобиле — только аватар (экономим место); имя с sm. */}
        <span className="hidden max-w-32 truncate sm:inline">{displayName}</span>
        <svg
          width="10"
          height="10"
          viewBox="0 0 10 10"
          className={cn("shrink-0 transition-transform duration-200", open && "rotate-180")}
          aria-hidden
        >
          <path d="M1 3l4 4 4-4" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      </button>

      <AnimatePresence>
        {open && (
          <motion.div
            role="menu"
            initial={{ opacity: 0, y: -6, scale: 0.97 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: -6, scale: 0.97 }}
            transition={{ duration: 0.15 }}
            className="glass-strong absolute right-0 z-50 mt-2 w-56 overflow-hidden rounded-xl p-1"
          >
            {/* Шапка меню: кто вошёл. */}
            <div className="flex items-center gap-3 px-3 py-2.5">
              <span className="grid h-9 w-9 shrink-0 place-items-center rounded-lg bg-accent-500/20 text-sm font-semibold text-accent-300">
                {initials(user)}
              </span>
              <span className="min-w-0">
                <span className="block truncate text-sm font-medium text-fg">{displayName}</span>
                <span className="block truncate text-xs text-fg-subtle">{roleLabel}</span>
              </span>
            </div>

            <div className="my-1 h-px bg-white/8" />

            <Link
              to="/dashboard"
              role="menuitem"
              onClick={() => setOpen(false)}
              className="flex items-center gap-2.5 rounded-lg px-3 py-2 text-sm text-fg-muted transition-colors hover:bg-white/5 hover:text-fg"
            >
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden className="shrink-0 text-accent-400">
                <path d="M3 12l9-8 9 8M5 10v9a1 1 0 001 1h4v-6h4v6h4a1 1 0 001-1v-9" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" />
              </svg>
              {t("nav.dashboard")}
            </Link>

            {isAdmin(user) && (
              <Link
                to="/admin"
                role="menuitem"
                onClick={() => setOpen(false)}
                className="flex items-center gap-2.5 rounded-lg px-3 py-2 text-sm text-fg-muted transition-colors hover:bg-white/5 hover:text-fg"
              >
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden className="shrink-0 text-accent-400">
                  <path d="M12 3l7 3v5c0 4.5-3 8-7 10-4-2-7-5.5-7-10V6l7-3z" stroke="currentColor" strokeWidth="1.7" strokeLinejoin="round" />
                </svg>
                {t("nav.admin")}
              </Link>
            )}

            <div className="my-1 h-px bg-white/8" />

            <button
              type="button"
              role="menuitem"
              onClick={handleLogout}
              className="flex w-full items-center gap-2.5 rounded-lg px-3 py-2 text-sm text-fg-muted transition-colors hover:bg-red-500/10 hover:text-red-300"
            >
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" aria-hidden className="shrink-0">
                <path d="M15 4h3a1 1 0 011 1v14a1 1 0 01-1 1h-3M10 8l-4 4 4 4M6 12h10" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" />
              </svg>
              {t("nav.logout")}
            </button>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}
