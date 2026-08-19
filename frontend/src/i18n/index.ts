import i18n from "i18next";
import { initReactI18next } from "react-i18next";
import LanguageDetector from "i18next-browser-languagedetector";
import ru from "./locales/ru.json";
import uz from "./locales/uz.json";

export const SUPPORTED_LANGS = ["ru", "uz"] as const;
export type Lang = (typeof SUPPORTED_LANGS)[number];

/**
 * i18n: RU по умолчанию, UZ (латиница). Выбор сохраняется в localStorage.
 * Форматирование дат/цен — отдельно в lib/utils.ts (по текущему языку).
 */
i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: {
      ru: { translation: ru },
      uz: { translation: uz },
    },
    fallbackLng: "ru",
    supportedLngs: SUPPORTED_LANGS,
    nonExplicitSupportedLngs: true,
    interpolation: { escapeValue: false },
    detection: {
      order: ["localStorage", "navigator"],
      lookupLocalStorage: "lang",
      caches: ["localStorage"],
    },
  });

export default i18n;
