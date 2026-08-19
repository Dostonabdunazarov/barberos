import { useState } from "react";
import { useTranslation } from "react-i18next";
import { useMasters } from "../../lib/hooks";
import { useStaffBookings } from "../../lib/staffHooks";
import { Select } from "../ui/Input";
import { StaffBookingCard } from "../StaffBookingCard";
import { LoadingState, ErrorState } from "../ui/misc";
import { apiErrorMessage } from "../../lib/api";
import { BookingStatus } from "../../types";

const STATUSES = [
  BookingStatus.Confirmed,
  BookingStatus.Completed,
  BookingStatus.Cancelled,
  BookingStatus.NoShow,
];

export function AdminBookings() {
  const { t } = useTranslation();
  const masters = useMasters(true);
  const [masterId, setMasterId] = useState<string>("");
  const [status, setStatus] = useState<string>("");
  const [page, setPage] = useState(1);

  const bookings = useStaffBookings({
    masterId: masterId || undefined,
    status: status ? (Number(status) as BookingStatus) : undefined,
    page,
    pageSize: 20,
  });

  const totalPages = bookings.data ? Math.max(1, Math.ceil(bookings.data.total / bookings.data.pageSize)) : 1;

  return (
    <div>
      <h2 className="mb-5 font-display text-2xl text-fg">{t("admin.bookingsTitle")}</h2>

      <div className="mb-5 flex flex-wrap gap-3">
        <Select
          value={masterId}
          onChange={(e) => {
            setMasterId(e.target.value);
            setPage(1);
          }}
          className="w-56"
        >
          <option value="">{t("common.all")} — {t("nav.masters")}</option>
          {masters.data?.map((m) => (
            <option key={m.id} value={m.id}>
              {m.name}
            </option>
          ))}
        </Select>
        <Select
          value={status}
          onChange={(e) => {
            setStatus(e.target.value);
            setPage(1);
          }}
          className="w-48"
        >
          <option value="">{t("common.all")}</option>
          {STATUSES.map((s) => (
            <option key={s} value={s}>
              {t(`status.${s}`)}
            </option>
          ))}
        </Select>
      </div>

      {bookings.isLoading ? (
        <LoadingState />
      ) : bookings.error ? (
        <ErrorState message={apiErrorMessage(bookings.error)} onRetry={bookings.refetch} />
      ) : bookings.data && bookings.data.items.length > 0 ? (
        <>
          <div className="space-y-3">
            {bookings.data.items.map((b) => (
              <StaffBookingCard key={b.id} booking={b} showMaster />
            ))}
          </div>
          {totalPages > 1 && (
            <div className="mt-6 flex items-center justify-center gap-4 text-sm">
              <button
                disabled={page <= 1}
                onClick={() => setPage((p) => p - 1)}
                className="text-fg-muted disabled:opacity-40 hover:text-fg"
              >
                ←
              </button>
              <span className="text-fg-subtle">
                {page} / {totalPages}
              </span>
              <button
                disabled={page >= totalPages}
                onClick={() => setPage((p) => p + 1)}
                className="text-fg-muted disabled:opacity-40 hover:text-fg"
              >
                →
              </button>
            </div>
          )}
        </>
      ) : (
        <p className="py-10 text-center text-fg-subtle">{t("dashboard.noBookings")}</p>
      )}
    </div>
  );
}
