import { useTranslation } from "react-i18next";
import { BookingStatus } from "../../types";
import { cn } from "../../lib/utils";

/** Крутилка загрузки. */
export function Spinner({ className }: { className?: string }) {
  return (
    <span
      className={cn(
        "inline-block h-5 w-5 animate-spin rounded-full border-2 border-current border-t-transparent",
        className,
      )}
      role="status"
      aria-label="loading"
    />
  );
}

/** Центрированное состояние загрузки на секцию/страницу. */
export function LoadingState({ label }: { label?: string }) {
  const { t } = useTranslation();
  return (
    <div className="flex flex-col items-center justify-center gap-3 py-16 text-fg-muted">
      <Spinner className="text-accent-500" />
      <span className="text-sm">{label ?? t("common.loading")}</span>
    </div>
  );
}

/** Состояние ошибки с кнопкой повтора. */
export function ErrorState({ message, onRetry }: { message: string; onRetry?: () => void }) {
  const { t } = useTranslation();
  return (
    <div className="flex flex-col items-center justify-center gap-3 py-16 text-center">
      <p className="text-red-400">{message}</p>
      {onRetry && (
        <button onClick={onRetry} className="text-sm text-accent-400 hover:text-accent-300 underline">
          {t("common.retry")}
        </button>
      )}
    </div>
  );
}

const STATUS_STYLE: Record<BookingStatus, string> = {
  [BookingStatus.Confirmed]: "bg-blue-500/15 text-blue-300 border-blue-500/30",
  [BookingStatus.Completed]: "bg-green-500/15 text-green-300 border-green-500/30",
  [BookingStatus.Cancelled]: "bg-neutral-500/15 text-neutral-400 border-neutral-500/30",
  [BookingStatus.NoShow]: "bg-red-500/15 text-red-300 border-red-500/30",
};

/** Бейдж статуса брони (локализованный). */
export function StatusBadge({ status }: { status: BookingStatus }) {
  const { t } = useTranslation();
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-medium",
        STATUS_STYLE[status],
      )}
    >
      {t(`status.${status}`)}
    </span>
  );
}

/** Отображение рейтинга звёздами (только чтение). */
export function StarRating({ value, className }: { value: number; className?: string }) {
  return (
    <span className={cn("inline-flex text-accent-400", className)} aria-label={`${value}/5`}>
      {[1, 2, 3, 4, 5].map((i) => (
        <span key={i} className={i <= Math.round(value) ? "opacity-100" : "opacity-25"}>
          ★
        </span>
      ))}
    </span>
  );
}

/** Иконка телефонной трубки (публичный контакт мастера). */
export function PhoneIcon({ className }: { className?: string }) {
  return (
    <svg
      viewBox="0 0 24 24"
      aria-hidden="true"
      className={cn("h-4 w-4 shrink-0", className)}
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M22 16.9v3a2 2 0 0 1-2.2 2 19.8 19.8 0 0 1-8.6-3.1 19.5 19.5 0 0 1-6-6A19.8 19.8 0 0 1 2.1 4.2 2 2 0 0 1 4.1 2h3a2 2 0 0 1 2 1.7c.1 1 .4 1.9.7 2.8a2 2 0 0 1-.5 2.1L8.1 9.9a16 16 0 0 0 6 6l1.3-1.2a2 2 0 0 1 2.1-.5c.9.3 1.8.6 2.8.7a2 2 0 0 1 1.7 2Z" />
    </svg>
  );
}
