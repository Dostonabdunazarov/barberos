import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { motion } from "framer-motion";
import type { CreateBookingResult, Master, Service } from "../types";
import { CardStrong } from "./ui/Card";
import { Button } from "./ui/Button";
import { useLocale } from "../i18n/useLocale";
import { formatDateTime, formatPrice } from "../lib/utils";

export function BookingSuccess({
  result,
  service,
  master,
  startAt,
}: {
  result: CreateBookingResult;
  service?: Service;
  master?: Master;
  startAt: string;
}) {
  const { t } = useTranslation();
  const locale = useLocale();
  const [copied, setCopied] = useState(false);

  const manageUrl = `${window.location.origin}/booking/${result.manageToken}`;

  async function copy() {
    try {
      await navigator.clipboard.writeText(manageUrl);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      /* clipboard может быть недоступен — ссылку видно на экране */
    }
  }

  return (
    <div className="mx-auto max-w-lg">
      <motion.div
        initial={{ opacity: 0, scale: 0.96 }}
        animate={{ opacity: 1, scale: 1 }}
        transition={{ duration: 0.35 }}
      >
        <CardStrong className="p-8 text-center">
          <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-green-500/15 text-3xl text-green-400">
            ✓
          </div>
          <h1 className="mt-5 font-display text-3xl text-fg">{t("booking.successTitle")}</h1>
          <p className="mt-2 text-fg-muted">{t("booking.successText")}</p>

          <div className="mt-6 space-y-1 rounded-xl bg-ink-800/60 p-4 text-left text-sm">
            {service && (
              <div className="flex justify-between">
                <span className="text-fg-subtle">{t("booking.stepService")}</span>
                <span className="text-fg">{service.name}</span>
              </div>
            )}
            {master && (
              <div className="flex justify-between">
                <span className="text-fg-subtle">{t("booking.stepMaster")}</span>
                <span className="text-fg">{master.name}</span>
              </div>
            )}
            <div className="flex justify-between">
              <span className="text-fg-subtle">{t("manage.when")}</span>
              <span className="text-fg">{formatDateTime(startAt, locale)}</span>
            </div>
            {service && (
              <div className="flex justify-between border-t border-white/5 pt-1">
                <span className="text-fg-subtle">{t("manage.price")}</span>
                <span className="text-accent-400">{formatPrice(service.price, locale)}</span>
              </div>
            )}
          </div>

          <div className="mt-6 text-left">
            <label className="text-sm font-medium text-fg-muted">{t("booking.manageLink")}</label>
            <div className="mt-1.5 flex gap-2">
              <input
                readOnly
                value={manageUrl}
                onFocus={(e) => e.target.select()}
                className="w-full truncate rounded-xl bg-ink-800/70 border border-white/10 px-3 py-2.5 text-sm text-fg-muted"
              />
              <Button variant="secondary" size="sm" onClick={copy} className="shrink-0">
                {copied ? t("booking.copied") : t("booking.copyLink")}
              </Button>
            </div>
          </div>

          <p className="mt-5 text-xs text-fg-subtle">{t("booking.cancelNote")}</p>

          <Link to={`/booking/${result.manageToken}`} className="mt-6 inline-block">
            <Button variant="secondary">{t("booking.goToBooking")}</Button>
          </Link>
        </CardStrong>
      </motion.div>
    </div>
  );
}
