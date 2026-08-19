import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { motion } from "framer-motion";
import type { Service } from "../types";
import { Card } from "./ui/Card";
import { useLocale } from "../i18n/useLocale";
import { formatPrice } from "../lib/utils";

/** Карточка услуги. onSelect — для флоу бронирования; иначе ведёт на /booking. */
export function ServiceCard({ service, onSelect }: { service: Service; onSelect?: () => void }) {
  const { t } = useTranslation();
  const locale = useLocale();

  const content = (
    <Card className="flex h-full flex-col p-5 transition-colors hover:border-accent-500/30">
      <h3 className="font-display text-xl text-fg">{service.name}</h3>
      {service.description && (
        <p className="mt-1 line-clamp-2 text-sm text-fg-muted">{service.description}</p>
      )}
      <div className="mt-4 flex items-end justify-between pt-2">
        <span className="text-sm text-fg-subtle">
          {t("services.duration", { count: service.durationMinutes })}
        </span>
        <span className="font-display text-lg text-accent-400">
          {formatPrice(service.price, locale)}
        </span>
      </div>
    </Card>
  );

  const wrapped = (
    <motion.div
      whileHover={{ y: -4 }}
      transition={{ type: "spring", stiffness: 300, damping: 25 }}
      className="h-full"
    >
      {content}
    </motion.div>
  );

  if (onSelect) {
    return (
      <button type="button" onClick={onSelect} className="block h-full w-full text-left">
        {wrapped}
      </button>
    );
  }
  return (
    <Link to="/booking" state={{ serviceId: service.id }} className="block h-full">
      {wrapped}
    </Link>
  );
}
