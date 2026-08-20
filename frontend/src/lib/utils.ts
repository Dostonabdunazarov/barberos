import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

/** Объединяет классы Tailwind, разрешая конфликты (последний побеждает). */
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

/** Таймзона барбершопа (совпадает с backend-конфигом Barbershop:TimeZone). */
export const SHOP_TZ = "Asia/Tashkent";

/** Форматирует UTC ISO-строку как время HH:mm в зоне барбершопа. */
export function formatTime(utcIso: string, locale: string): string {
  return new Intl.DateTimeFormat(locale, {
    hour: "2-digit",
    minute: "2-digit",
    timeZone: SHOP_TZ,
  }).format(new Date(utcIso));
}

/** Форматирует UTC ISO-строку как дату (напр. «17 авг. 2026») в зоне барбершопа. */
export function formatDate(utcIso: string, locale: string): string {
  return new Intl.DateTimeFormat(locale, {
    day: "numeric",
    month: "short",
    year: "numeric",
    timeZone: SHOP_TZ,
  }).format(new Date(utcIso));
}

/** Дата + время вместе. */
export function formatDateTime(utcIso: string, locale: string): string {
  return new Intl.DateTimeFormat(locale, {
    day: "numeric",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    timeZone: SHOP_TZ,
  }).format(new Date(utcIso));
}

/**
 * Номер для href="tel:" — из отображаемого номера убираем всё, кроме цифр
 * и ведущего плюса (пробелы/скобки/дефисы в tel: не нужны).
 */
export function telHref(phone: string): string {
  const digits = phone.replace(/[^\d+]/g, "");
  return `tel:${digits.startsWith("+") ? "+" : ""}${digits.replace(/\+/g, "")}`;
}

/**
 * Человекочитаемый номер: +998901234567 → +998 90 123-45-67.
 * Узбекские номера (12 цифр после +998) разбиваются по формату оператора;
 * всё остальное возвращаем как есть — вдруг мастер указал зарубежный номер.
 */
export function formatPhone(phone: string): string {
  const digits = phone.replace(/\D/g, "");
  if (digits.length === 12 && digits.startsWith("998")) {
    const [, code, a, b, c] = /^998(\d{2})(\d{3})(\d{2})(\d{2})$/.exec(digits)!;
    return `+998 ${code} ${a}-${b}-${c}`;
  }
  return phone;
}

/** Цена в сумах (UZS) без дробной части. */
export function formatPrice(amount: number, locale: string): string {
  return new Intl.NumberFormat(locale, {
    style: "currency",
    currency: "UZS",
    maximumFractionDigits: 0,
  }).format(amount);
}

/** YYYY-MM-DD для конкретной даты в зоне барбершопа (для запроса availability). */
export function toShopDateString(date: Date): string {
  // en-CA даёт формат YYYY-MM-DD
  return new Intl.DateTimeFormat("en-CA", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    timeZone: SHOP_TZ,
  }).format(date);
}
