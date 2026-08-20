import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { useMasters } from "../lib/hooks";
import { useStaffBookings } from "../lib/staffHooks";
import { useAuthStore, isAdmin } from "../store/authStore";
import { StaffBookingCard } from "../components/StaffBookingCard";
import { ScheduleEditor } from "../components/ScheduleEditor";
import { WorksManager } from "../components/WorksManager";
import { ContactEditor } from "../components/ContactEditor";
import { LoadingState, ErrorState } from "../components/ui/misc";
import { cn } from "../lib/utils";
import { apiErrorMessage } from "../lib/api";

type Range = "today" | "week";
type Tab = "bookings" | "schedule" | "photos" | "contact";

/** Границы диапазона [from, to) в UTC ISO по локальным дням. */
function rangeBounds(range: Range): { from: string; to: string } {
  const now = new Date();
  const start = new Date(now);
  start.setHours(0, 0, 0, 0);
  const end = new Date(start);
  end.setDate(start.getDate() + (range === "today" ? 1 : 7));
  return { from: start.toISOString(), to: end.toISOString() };
}

export function DashboardPage() {
  const { t } = useTranslation();
  const user = useAuthStore((s) => s.user);
  const admin = isAdmin(user);

  const masters = useMasters(true);
  // Мастер, привязанный к текущей учётке (для расписания).
  const myMaster = useMemo(
    () => masters.data?.find((m) => m.userId === user?.id),
    [masters.data, user?.id],
  );

  const [tab, setTab] = useState<Tab>("bookings");
  const [range, setRange] = useState<Range>("today");
  const bounds = rangeBounds(range);
  const bookings = useStaffBookings({ ...bounds, pageSize: 100 });

  const canManageSchedule = !!myMaster;

  return (
    <div>
      <h1 className="font-display text-3xl text-fg sm:text-4xl">{t("dashboard.title")}</h1>

      {/* Вкладки */}
      <div className="mt-6 flex gap-2 border-b border-white/10">
        <TabButton active={tab === "bookings"} onClick={() => setTab("bookings")}>
          {t("dashboard.bookings")}
        </TabButton>
        {canManageSchedule && (
          <TabButton active={tab === "schedule"} onClick={() => setTab("schedule")}>
            {t("dashboard.schedule")}
          </TabButton>
        )}
        {canManageSchedule && (
          <TabButton active={tab === "photos"} onClick={() => setTab("photos")}>
            {t("works.tab")}
          </TabButton>
        )}
        {canManageSchedule && (
          <TabButton active={tab === "contact"} onClick={() => setTab("contact")}>
            {t("dashboard.contact")}
          </TabButton>
        )}
      </div>

      {tab === "bookings" && (
        <div className="mt-6">
          <div className="mb-5 flex gap-2">
            {(["today", "week"] as Range[]).map((r) => (
              <button
                key={r}
                onClick={() => setRange(r)}
                className={cn(
                  "rounded-lg px-4 py-2 text-sm transition-colors",
                  range === r ? "bg-accent-500 text-ink-950" : "glass text-fg-muted",
                )}
              >
                {t(`dashboard.${r}`)}
              </button>
            ))}
          </div>

          {bookings.isLoading ? (
            <LoadingState />
          ) : bookings.error ? (
            <ErrorState message={apiErrorMessage(bookings.error)} onRetry={bookings.refetch} />
          ) : bookings.data && bookings.data.items.length > 0 ? (
            <div className="space-y-3">
              {bookings.data.items.map((b) => (
                <StaffBookingCard key={b.id} booking={b} showMaster={admin} />
              ))}
            </div>
          ) : (
            <p className="py-10 text-center text-fg-subtle">{t("dashboard.noBookings")}</p>
          )}
        </div>
      )}

      {tab === "schedule" && myMaster && (
        <div className="mt-6">
          <ScheduleEditor masterId={myMaster.id} />
        </div>
      )}

      {tab === "photos" && myMaster && (
        <div className="mt-6">
          <WorksManager masterId={myMaster.id} />
        </div>
      )}

      {tab === "contact" && myMaster && (
        <div className="mt-6">
          <ContactEditor master={myMaster} />
        </div>
      )}
    </div>
  );
}

function TabButton({
  active,
  onClick,
  children,
}: {
  active: boolean;
  onClick: () => void;
  children: React.ReactNode;
}) {
  return (
    <button
      onClick={onClick}
      className={cn(
        "-mb-px border-b-2 px-4 py-2.5 text-sm font-medium transition-colors",
        active
          ? "border-accent-500 text-accent-400"
          : "border-transparent text-fg-muted hover:text-fg",
      )}
    >
      {children}
    </button>
  );
}
