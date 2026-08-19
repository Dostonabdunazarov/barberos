import { useState } from "react";
import { useTranslation } from "react-i18next";
import { useReviewsForModeration, useModerateReview } from "../../lib/staffHooks";
import { Card } from "../ui/Card";
import { Button } from "../ui/Button";
import { LoadingState, StarRating } from "../ui/misc";
import { useLocale } from "../../i18n/useLocale";
import { formatDate } from "../../lib/utils";
import { cn } from "../../lib/utils";

export function AdminReviews() {
  const { t } = useTranslation();
  const locale = useLocale();
  // false = ожидают модерации, true = опубликованные.
  const [published, setPublished] = useState(false);
  const { data, isLoading } = useReviewsForModeration(published);
  const moderate = useModerateReview();

  return (
    <div>
      <h2 className="mb-5 font-display text-2xl text-fg">{t("admin.reviewsTitle")}</h2>

      <div className="mb-5 flex gap-2">
        {[
          { v: false, label: t("admin.pending") },
          { v: true, label: t("admin.published") },
        ].map((tab) => (
          <button
            key={String(tab.v)}
            onClick={() => setPublished(tab.v)}
            className={cn(
              "rounded-lg px-4 py-2 text-sm transition-colors",
              published === tab.v ? "bg-accent-500 text-ink-950" : "glass text-fg-muted",
            )}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {isLoading ? (
        <LoadingState />
      ) : data && data.items.length > 0 ? (
        <div className="space-y-3">
          {data.items.map((r) => (
            <Card key={r.id} className="p-5">
              <div className="flex flex-wrap items-start justify-between gap-2">
                <div>
                  <div className="flex items-center gap-2">
                    <span className="font-medium text-fg">{r.guestName}</span>
                    <StarRating value={r.rating} />
                  </div>
                  <span className="text-sm text-fg-subtle">
                    {r.masterName} · {formatDate(r.createdAt, locale)}
                  </span>
                </div>
                {r.isPublished ? (
                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={() => moderate.mutate({ id: r.id, isPublished: false })}
                    disabled={moderate.isPending}
                  >
                    {t("admin.unpublish")}
                  </Button>
                ) : (
                  <Button
                    size="sm"
                    onClick={() => moderate.mutate({ id: r.id, isPublished: true })}
                    disabled={moderate.isPending}
                  >
                    {t("admin.publish")}
                  </Button>
                )}
              </div>
              {r.comment && <p className="mt-2 text-fg-muted">{r.comment}</p>}
            </Card>
          ))}
        </div>
      ) : (
        <p className="py-10 text-center text-fg-subtle">—</p>
      )}
    </div>
  );
}
