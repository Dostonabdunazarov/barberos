import { useState } from "react";
import { useTranslation } from "react-i18next";
import { AdminServices } from "../components/admin/AdminServices";
import { AdminMasters } from "../components/admin/AdminMasters";
import { AdminBookings } from "../components/admin/AdminBookings";
import { AdminReviews } from "../components/admin/AdminReviews";
import { AdminAnalytics } from "../components/admin/AdminAnalytics";
import { cn } from "../lib/utils";

type Tab = "services" | "masters" | "bookings" | "reviews" | "analytics";
const TABS: Tab[] = ["services", "masters", "bookings", "reviews", "analytics"];

export function AdminPage() {
  const { t } = useTranslation();
  const [tab, setTab] = useState<Tab>("services");

  return (
    <div>
      <h1 className="font-display text-3xl text-fg sm:text-4xl">{t("admin.title")}</h1>

      <div className="mt-6 flex flex-wrap gap-1 border-b border-white/10">
        {TABS.map((tb) => (
          <button
            key={tb}
            onClick={() => setTab(tb)}
            className={cn(
              "-mb-px border-b-2 px-4 py-2.5 text-sm font-medium transition-colors",
              tab === tb
                ? "border-accent-500 text-accent-400"
                : "border-transparent text-fg-muted hover:text-fg",
            )}
          >
            {t(`admin.tabs.${tb}`)}
          </button>
        ))}
      </div>

      <div className="mt-8">
        {tab === "services" && <AdminServices />}
        {tab === "masters" && <AdminMasters />}
        {tab === "bookings" && <AdminBookings />}
        {tab === "reviews" && <AdminReviews />}
        {tab === "analytics" && <AdminAnalytics />}
      </div>
    </div>
  );
}
