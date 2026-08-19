import { useTranslation } from "react-i18next";
import { useAnalytics } from "../../lib/staffHooks";
import { Card } from "../ui/Card";
import { LoadingState } from "../ui/misc";
import { BookingStatus } from "../../types";

export function AdminAnalytics() {
  const { t } = useTranslation();
  const { data, isLoading } = useAnalytics();

  if (isLoading) return <LoadingState />;
  if (!data) return null;

  const maxLoad = Math.max(1, ...data.masterLoad.map((m) => m.bookings));
  const maxPop = Math.max(1, ...data.popularServices.map((s) => s.bookings));

  return (
    <div>
      <h2 className="mb-5 font-display text-2xl text-fg">{t("admin.analyticsTitle")}</h2>

      {/* KPI */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <Card className="p-5">
          <div className="text-sm text-fg-subtle">{t("admin.totalBookings")}</div>
          <div className="mt-1 font-display text-3xl text-accent-400">{data.totalBookings}</div>
        </Card>
        {data.byStatus.map((s) => (
          <Card key={s.status} className="p-5">
            <div className="text-sm text-fg-subtle">{t(`status.${s.status as BookingStatus}`)}</div>
            <div className="mt-1 font-display text-3xl text-fg">{s.count}</div>
          </Card>
        ))}
      </div>

      {/* Загрузка мастеров */}
      <Card className="mt-6 p-6">
        <h3 className="font-display text-lg text-fg">{t("admin.masterLoad")}</h3>
        <div className="mt-4 space-y-3">
          {data.masterLoad.map((m) => (
            <div key={m.masterId}>
              <div className="mb-1 flex justify-between text-sm">
                <span className="text-fg">{m.masterName}</span>
                <span className="text-fg-subtle">
                  {m.bookings} · {m.busyMinutes} {t("admin.busyMinutes").toLowerCase()}
                </span>
              </div>
              <div className="h-2 overflow-hidden rounded-full bg-ink-700">
                <div
                  className="h-full rounded-full bg-accent-500"
                  style={{ width: `${(m.bookings / maxLoad) * 100}%` }}
                />
              </div>
            </div>
          ))}
          {data.masterLoad.length === 0 && <p className="text-sm text-fg-subtle">—</p>}
        </div>
      </Card>

      {/* Популярные услуги */}
      <Card className="mt-6 p-6">
        <h3 className="font-display text-lg text-fg">{t("admin.popularServices")}</h3>
        <div className="mt-4 space-y-3">
          {data.popularServices.map((s) => (
            <div key={s.serviceId}>
              <div className="mb-1 flex justify-between text-sm">
                <span className="text-fg">{s.serviceName}</span>
                <span className="text-fg-subtle">{s.bookings}</span>
              </div>
              <div className="h-2 overflow-hidden rounded-full bg-ink-700">
                <div
                  className="h-full rounded-full bg-accent-600"
                  style={{ width: `${(s.bookings / maxPop) * 100}%` }}
                />
              </div>
            </div>
          ))}
          {data.popularServices.length === 0 && <p className="text-sm text-fg-subtle">—</p>}
        </div>
      </Card>
    </div>
  );
}
