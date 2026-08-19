import { useTranslation } from "react-i18next";
import { useParams } from "react-router-dom";
import { useManageBooking } from "../lib/hooks";
import { Card } from "../components/ui/Card";
import { LoadingState, ErrorState, StatusBadge } from "../components/ui/misc";
import { ReviewForm } from "../components/ReviewForm";
import { useLocale } from "../i18n/useLocale";
import { formatDateTime, formatPrice } from "../lib/utils";
import { BookingStatus } from "../types";

export function ManageBookingPage() {
  const { t } = useTranslation();
  const locale = useLocale();
  const { token } = useParams<{ token: string }>();
  const { data, isLoading, error, refetch } = useManageBooking(token);

  if (isLoading) return <LoadingState />;
  if (error || !data)
    return <ErrorState message={t("manage.notFound")} onRetry={refetch} />;

  const canReview = data.status === BookingStatus.Completed;

  return (
    <div className="mx-auto max-w-lg space-y-6">
      <h1 className="font-display text-3xl text-fg">{t("manage.title")}</h1>

      <Card className="p-6">
        <div className="flex items-center justify-between">
          <span className="font-display text-xl text-fg">{data.guestName}</span>
          <StatusBadge status={data.status} />
        </div>
        <dl className="mt-5 space-y-2.5 text-sm">
          <Row label={t("manage.master")} value={data.masterName} />
          <Row label={t("manage.service")} value={data.serviceName} />
          <Row label={t("manage.when")} value={formatDateTime(data.startAt, locale)} />
          <Row label={t("manage.price")} value={formatPrice(data.price, locale)} accent />
        </dl>
        <p className="mt-5 border-t border-white/5 pt-4 text-xs text-fg-subtle">
          {t("manage.cancelHint")}
        </p>
      </Card>

      {canReview ? (
        token && <ReviewForm token={token} />
      ) : (
        <p className="text-center text-sm text-fg-subtle">{t("manage.reviewOnlyCompleted")}</p>
      )}
    </div>
  );
}

function Row({ label, value, accent }: { label: string; value: string; accent?: boolean }) {
  return (
    <div className="flex justify-between gap-4">
      <dt className="text-fg-subtle">{label}</dt>
      <dd className={accent ? "text-accent-400" : "text-fg"}>{value}</dd>
    </div>
  );
}
