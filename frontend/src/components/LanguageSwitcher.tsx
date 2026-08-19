import { useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { motion, AnimatePresence } from "framer-motion";
import { SUPPORTED_LANGS } from "../i18n";
import { cn } from "../lib/utils";
import { FlagRU, FlagUZ } from "./Flags";

/** SVG-флаг по языку (полноцветный на всех ОС). */
function Flag({ lng, className }: { lng: string; className?: string }) {
  return lng === "ru" ? <FlagRU className={className} /> : <FlagUZ className={className} />;
}

/**
 * Переключатель языка (dropdown). Триггер показывает текущий язык,
 * список открывается по клику; закрывается кликом вне и Escape.
 * Выбор сохраняется через i18next LanguageDetector (localStorage).
 */
export function LanguageSwitcher() {
  const { i18n, t } = useTranslation();
  const current = (i18n.resolvedLanguage ?? "ru") as (typeof SUPPORTED_LANGS)[number];
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  // Закрытие по клику вне и по Escape.
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

  function choose(lng: string) {
    void i18n.changeLanguage(lng);
    setOpen(false);
  }

  return (
    <div className="relative" ref={ref}>
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-label={t("lang.switch")}
        className="flex items-center gap-1.5 rounded-lg border border-white/10 px-2.5 py-1.5 text-xs font-semibold uppercase text-fg-muted transition-colors hover:border-white/20 hover:text-fg"
      >
        <Flag lng={current} className="h-3 w-4.5" />
        <span>{t(`lang.${current}`)}</span>
        <svg
          width="10"
          height="10"
          viewBox="0 0 10 10"
          className={cn("transition-transform duration-200", open && "rotate-180")}
          aria-hidden
        >
          <path d="M1 3l4 4 4-4" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      </button>

      <AnimatePresence>
        {open && (
          <motion.ul
            role="listbox"
            initial={{ opacity: 0, y: -6, scale: 0.97 }}
            animate={{ opacity: 1, y: 0, scale: 1 }}
            exit={{ opacity: 0, y: -6, scale: 0.97 }}
            transition={{ duration: 0.15 }}
            className="glass-strong absolute right-0 z-50 mt-2 w-40 overflow-hidden rounded-xl p-1"
          >
            {SUPPORTED_LANGS.map((lng) => (
              <li key={lng} role="option" aria-selected={current === lng}>
                <button
                  type="button"
                  onClick={() => choose(lng)}
                  className={cn(
                    "flex w-full items-center justify-between rounded-lg px-3 py-2 text-sm transition-colors",
                    current === lng
                      ? "bg-accent-500/15 text-accent-300"
                      : "text-fg-muted hover:bg-white/5 hover:text-fg",
                  )}
                >
                  <span className="flex items-center gap-2">
                    <Flag lng={lng} className="h-3.5 w-5" />
                    {t(`lang.${lng}Full`)}
                  </span>
                  <span className="text-xs uppercase opacity-60">{t(`lang.${lng}`)}</span>
                </button>
              </li>
            ))}
          </motion.ul>
        )}
      </AnimatePresence>
    </div>
  );
}
