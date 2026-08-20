import { forwardRef, useState, type InputHTMLAttributes, type SelectHTMLAttributes, type TextareaHTMLAttributes } from "react";
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

/**
 * Числовое поле, которое не «залипает» на 0.
 *
 * Обычный `<input type="number" value={n} onChange={e => set(+e.target.value)}>`
 * ломается: стирание последней цифры даёт "" → `+""` === 0 → в поле снова 0,
 * и следующая введённая цифра прилипает к нулю («050»). Здесь показываемая
 * строка живёт в собственном стейте, а наверх уходит `number | null`
 * (null = поле пустое). Значение извне подхватывается, только если оно
 * действительно отличается от того, что набрано, — чтобы не перетирать
 * промежуточный ввод вроде "1." или "-".
 */
export const NumberInput = forwardRef<
  HTMLInputElement,
  Omit<InputHTMLAttributes<HTMLInputElement>, "value" | "onChange" | "type"> & {
    value: number | null | undefined;
    onChange: (value: number | null) => void;
    /** Разрешить дробные значения (по умолчанию только целые). */
    decimal?: boolean;
  }
>(({ value, onChange, decimal = false, onBlur, min, max, step, className, ...props }, ref) => {
  const external = value === null || value === undefined || Number.isNaN(value) ? "" : String(value);
  const [text, setText] = useState(external);

  // Синхронизируемся с внешним значением, только когда оно реально разошлось
  // с набранным (сброс формы, загрузка данных на редактирование).
  const [lastExternal, setLastExternal] = useState(external);
  if (external !== lastExternal) {
    setLastExternal(external);
    if (parseNumeric(text) !== value) setText(external);
  }

  const pattern = decimal ? /^-?\d*[.,]?\d*$/ : /^-?\d*$/;

  return (
    <input
      ref={ref}
      type="text"
      inputMode={decimal ? "decimal" : "numeric"}
      autoComplete="off"
      value={text}
      min={min}
      max={max}
      step={step}
      onChange={(e) => {
        const next = e.target.value.trim();
        if (next !== "" && !pattern.test(next)) return; // отсекаем мусор, не трогая курсор
        setText(next);
        onChange(parseNumeric(next));
      }}
      onBlur={(e) => {
        // На выходе приводим к каноничному виду: "007" → "7", "1." → "1", "" остаётся пустым.
        const n = parseNumeric(text);
        setText(n === null ? "" : String(n));
        onBlur?.(e);
      }}
      className={cn(base, className)}
      {...props}
    />
  );
});
NumberInput.displayName = "NumberInput";

/** "" / "-" / "1." → null или число. */
function parseNumeric(text: string): number | null {
  if (text === "" || text === "-") return null;
  const n = Number(text.replace(",", "."));
  return Number.isFinite(n) ? n : null;
}
