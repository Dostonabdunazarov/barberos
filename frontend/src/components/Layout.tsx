import { type ReactNode, useEffect, useRef } from "react";
import { Link, NavLink, useNavigate, useLocation } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { useAuthStore, isAdmin } from "../store/authStore";
import { api } from "../lib/api";
import { LanguageSwitcher } from "./LanguageSwitcher";
import { LogoMark } from "./Logo";
import { cn } from "../lib/utils";

/**
 * Многослойная сцена-фон с эффектом глубины: дальний свет ламп (боке),
 * панели/зеркала, виньетка, зерно. Слои двигаются с разной скоростью от мыши
 * (параллакс) — front-контент «парит» над сценой. Самодостаточно (без внешних фото);
 * реальное фото при желании кладётся первым слоем в .scene.
 */
function Scene() {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    const reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    if (reduce) return;

    let raf = 0;
    function onMove(e: PointerEvent) {
      cancelAnimationFrame(raf);
      raf = requestAnimationFrame(() => {
        // Нормируем к [-1, 1] относительно центра экрана.
        const x = (e.clientX / window.innerWidth - 0.5) * 2;
        const y = (e.clientY / window.innerHeight - 0.5) * 2;
        el!.style.setProperty("--par-x", x.toFixed(3));
        el!.style.setProperty("--par-y", y.toFixed(3));
      });
    }
    window.addEventListener("pointermove", onMove, { passive: true });
    return () => {
      window.removeEventListener("pointermove", onMove);
      cancelAnimationFrame(raf);
    };
  }, []);

  return (
    <div className="scene" ref={ref} aria-hidden>
      <div className="scene__photo" />
      <div className="scene__glow" />
      <div className="scene__vignette" />
      <div className="scene__grain" />
    </div>
  );
}

function NavItem({
  to,
  label,
  end,
  big,
}: {
  to: string;
  label: string;
  end?: boolean;
  big?: boolean;
}) {
  return (
    <NavLink
      to={to}
      end={end}
      className={({ isActive }) =>
        cn(
          "transition-colors hover:text-fg",
          big ? "text-base font-medium" : "text-sm",
          isActive ? "text-accent-400" : "text-fg-muted",
        )
      }
    >
      {label}
    </NavLink>
  );
}

export function Layout({ children }: { children: ReactNode }) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const location = useLocation();
  const { user, clear } = useAuthStore();

  // На странице входа прячем верхний навбар — фокус на форме, шапка «уходит» вниз в футер.
  const isLogin = location.pathname === "/login";
  const year = new Date().getFullYear();

  async function handleLogout() {
    try {
      await api.post("/auth/logout");
    } catch {
      /* игнорируем — всё равно чистим локально */
    }
    clear();
    navigate("/");
  }

  return (
    <div className="relative flex min-h-screen flex-col overflow-x-clip">
      <Scene />

      {!isLogin && (
      <header className="sticky top-0 z-40">
        {/* Тёмный градиент под хедером — движется вместе с ним при скролле,
            держит меню читаемым над любым контентом (без сплошной панели). */}
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 -z-10 h-32 bg-linear-to-b from-black/85 via-black/55 to-transparent"
        />
        <nav className="mx-auto flex max-w-6xl flex-wrap items-center gap-x-4 gap-y-2 px-4 py-3.5 sm:flex-nowrap sm:gap-6 sm:px-6">
          <Link to="/" className="flex shrink-0 items-center" aria-label={t("brand")}>
            <LogoMark className="h-12 w-auto drop-shadow-[0_2px_10px_rgba(212,169,95,0.25)] sm:h-14" />
          </Link>

          <div className="hidden items-center gap-5 sm:flex">
            <NavItem to="/" label={t("nav.home")} end big />
            <NavItem to="/services" label={t("nav.services")} big />
            <NavItem to="/masters" label={t("nav.masters")} big />
          </div>

          <div className="ml-auto flex min-w-0 shrink items-center gap-2 sm:gap-3">
            <LanguageSwitcher />
            <Link
              to="/booking"
              className="shrink-0 rounded-lg bg-accent-500 px-3 py-2 text-sm font-semibold text-ink-950 transition-colors hover:bg-accent-400 sm:px-3.5"
            >
              {t("nav.book")}
            </Link>

            {user ? (
              // Вторичные ссылки авторизованного: на мобиле переносятся во вторую
              // строку (flex-wrap выше), на десктопе — в один ряд. Это убирает
              // горизонтальный скролл после входа.
              <div className="flex shrink-0 items-center gap-3">
                <NavItem to="/dashboard" label={t("nav.dashboard")} />
                {isAdmin(user) && <NavItem to="/admin" label={t("nav.admin")} />}
                <button
                  onClick={handleLogout}
                  className="text-sm text-fg-muted transition-colors hover:text-fg"
                >
                  {t("nav.logout")}
                </button>
              </div>
            ) : (
              <NavItem to="/login" label={t("nav.login")} />
            )}
          </div>
        </nav>
      </header>
      )}

      <main
        className={
          isLogin
            ? "flex flex-1 flex-col items-center justify-center px-4 py-10"
            : "mx-auto w-full max-w-6xl flex-1 px-4 py-8 sm:px-6"
        }
      >
        {children}
      </main>

      <footer className="mt-16 border-t border-white/5 py-8">
        <div className="mx-auto flex max-w-6xl flex-col items-center gap-2 px-4 text-center sm:px-6">
          <Link to="/" className="flex items-center gap-2">
            <LogoMark className="h-7 w-7" />
            <span className="font-display text-lg tracking-wide text-gradient-accent">
              {t("brand")}
            </span>
          </Link>
          <p className="text-sm text-fg-muted">{t("footer.tagline")}</p>
          <p className="text-xs text-fg-subtle">
            © {year} {t("brand")}. {t("footer.rights")}.
          </p>
          {isLogin && <p className="mt-1 text-xs text-fg-subtle">{t("footer.staffOnly")}</p>}
        </div>
      </footer>
    </div>
  );
}
