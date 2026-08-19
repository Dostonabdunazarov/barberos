import { forwardRef, type InputHTMLAttributes, type SelectHTMLAttributes, type TextareaHTMLAttributes } from "react";
import { cn } from "../../lib/utils";

const base =
  "w-full rounded-xl bg-ink-800/70 border border-white/10 px-4 py-3 text-fg placeholder:text-fg-subtle " +
  "transition-colors focus:border-accent-500/60 focus:bg-ink-800 outline-none";

export const Input = forwardRef<HTMLInputElement, InputHTMLAttributes<HTMLInputElement>>(
  ({ className, ...props }, ref) => (
    <input ref={ref} className={cn(base, className)} {...props} />
  ),
);
Input.displayName = "Input";

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaHTMLAttributes<HTMLTextAreaElement>>(
  ({ className, ...props }, ref) => (
    <textarea ref={ref} className={cn(base, "min-h-24 resize-y", className)} {...props} />
  ),
);
Textarea.displayName = "Textarea";

export const Select = forwardRef<HTMLSelectElement, SelectHTMLAttributes<HTMLSelectElement>>(
  ({ className, ...props }, ref) => (
    <select ref={ref} className={cn(base, "appearance-none cursor-pointer", className)} {...props} />
  ),
);
Select.displayName = "Select";

/** Обёртка «подпись + поле + ошибка» для форм. */
export function Field({
  label,
  error,
  children,
  htmlFor,
}: {
  label: string;
  error?: string;
  htmlFor?: string;
  children: React.ReactNode;
}) {
  return (
    <div className="space-y-1.5">
      <label htmlFor={htmlFor} className="block text-sm font-medium text-fg-muted">
        {label}
      </label>
      {children}
      {error && <p className="text-sm text-red-400">{error}</p>}
    </div>
  );
}
