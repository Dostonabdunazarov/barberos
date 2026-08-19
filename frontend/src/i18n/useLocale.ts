import { useTranslation } from "react-i18next";

/** BCP-47 локали для Intl-форматирования по языку интерфейса. */
const INTL_LOCALE: Record<string, string> = {
  ru: "ru-RU",
  uz: "uz-Latn-UZ",
};

/**
 * Возвращает текущую BCP-47 локаль для Intl (даты/цены).
 * Отделено от i18n-кода языка ("ru"/"uz"), т.к. Intl нужен полный тег.
 */
export function useLocale(): string {
  const { i18n } = useTranslation();
  const lng = i18n.resolvedLanguage ?? i18n.language ?? "ru";
  return INTL_LOCALE[lng] ?? "ru-RU";
}
