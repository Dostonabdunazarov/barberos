import { useTranslation } from "react-i18next";
import type { Booking } from "../types";
import { BookingStatus } from "../types";
import { Card } from "./ui/Card";
import { Button } from "./ui/Button";
import { StatusBadge } from "./ui/misc";
import { useUpdateBookingStatus } from "../lib/staffHooks";
import { useLocale } from "../i18n/useLocale";
import { formatDateTime } from "../lib/utils";

/** Карточка брони для персонала: контакты гостя + действия смены статуса. */
export function StaffBookingCard({ booking, showMaster }: { booking: Booking; showMaster?: boolean }) {
  const { t } = useTranslation();
  const locale = useLocale();
  const updateStatus = useUpdateBookingStatus();

  // Действия доступны только для активной (Confirmed) брони.
  const isActive = booking.status === BookingStatus.Confirmed;

  return (
    <Card className="p-4">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div>
          <div className="flex items-center gap-2">
            <span className="font-medium text-fg">{booking.guestName}</span>
            <StatusBadge status={booking.status} />
          </div>
          <a href={`tel:${booking.guestPhone}`} className="text-sm text-accent-400 hover:underline">
            {booking.guestPhone}
          </a>
        </div>
        <div className="text-right text-sm">
          <div className="text-fg">{formatDateTime(booking.startAt, locale)}</div>
          <div className="text-fg-subtle">{booking.serviceName}</div>
          {showMaster && <div className="text-fg-subtle">{booking.masterName}</div>}
        </div>
      </div>

      {isActive && (
        <div className="mt-3 flex flex-wrap gap-2 border-t border-white/5 pt-3">
          <Button
            size="sm"
            onClick={() => updateStatus.mutate({ id: booking.id, status: BookingStatus.Completed })}
            disabled={updateStatus.isPending}
          >
            {t("dashboard.markCompleted")}
          </Button>
          <Button
            size="sm"
            variant="secondary"
            onClick={() => updateStatus.mutate({ id: booking.id, status: BookingStatus.NoShow })}
            disabled={updateStatus.isPending}
          >
            {t("dashboard.markNoShow")}
          </Button>
          <Button
            size="sm"
            variant="ghost"
            onClick={() => updateStatus.mutate({ id: booking.id, status: BookingStatus.Cancelled })}
            disabled={updateStatus.isPending}
          >
            {t("dashboard.markCancelled")}
          </Button>
        </div>
      )}
    </Card>
  );
}
