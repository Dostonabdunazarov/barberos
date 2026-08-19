import type { HTMLAttributes } from "react";
import { cn } from "../../lib/utils";

/** Стеклянная карточка — базовая поверхность контента (с кромочным бликом). */
export function Card({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return <div className={cn("glass glass-sheen rounded-2xl", className)} {...props} />;
}

export function CardStrong({ className, ...props }: HTMLAttributes<HTMLDivElement>) {
  return <div className={cn("glass-strong glass-sheen rounded-2xl", className)} {...props} />;
}
