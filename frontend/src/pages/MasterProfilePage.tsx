import { useTranslation } from "react-i18next";
import { Link, useParams } from "react-router-dom";
import { useMaster, useMasterReviews, useMasterWorks } from "../lib/hooks";
import { Card } from "../components/ui/Card";
import { Button } from "../components/ui/Button";
import { LoadingState, ErrorState, StarRating } from "../components/ui/misc";
import { apiErrorMessage } from "../lib/api";
import { useLocale } from "../i18n/useLocale";
import { formatDate, telHref } from "../lib/utils";

export function MasterProfilePage() {
  const { t } = useTranslation();
  const locale = useLocale();
  const { id } = useParams<{ id: string }>();
  const master = useMaster(id);
  const reviews = useMasterReviews(id);
  const works = useMasterWorks(id);

  if (master.isLoading) return <LoadingState />;
  if (master.error || !master.data)
    return <ErrorState message={apiErrorMessage(master.error)} onRetry={master.refetch} />;

  const m = master.data;
  const rating = reviews.data?.rating;

  return (
    <div className="mx-auto max-w-3xl">
      <Card className="overflow-hidden">
        <div className="grid gap-0 sm:grid-cols-[240px_1fr]">
          <div className="aspect-square w-full overflow-hidden bg-ink-800 sm:aspect-auto">
            {m.photoUrl ? (
              <img src={m.photoUrl} alt={m.name} className="h-full w-full object-cover" />
            ) : (
              <div className="flex h-full min-h-48 w-full items-center justify-center font-display text-6xl text-ink-600">
                {m.name.charAt(0)}
              </div>
            )}
          </div>
          <div className="p-6">
            <h1 className="font-display text-3xl text-fg">{m.name}</h1>
            <div className="mt-2 flex items-center gap-2 text-sm">
              {rating && rating.count > 0 ? (
                <>
                  <StarRating value={rating.average ?? 0} />
                  <span className="text-fg-subtle">
                    {(rating.average ?? 0).toFixed(1)} · {rating.count} {t("masters.reviews")}
                  </span>
                </>
              ) : (
                <span className="text-fg-subtle">{t("masters.noRating")}</span>
              )}
            </div>
            {m.bio && <p className="mt-4 text-fg-muted">{m.bio}</p>}
            {m.publicPhone && (
              <a
                href={telHref(m.publicPhone)}
                className="mt-4 inline-flex items-center gap-2 text-sm text-fg-muted transition-colors hover:text-accent-400"
                aria-label={t("masters.callMaster")}
              >
                <PhoneIcon />
                <span dir="ltr">{m.publicPhone}</span>
              </a>
            )}
            <Link to="/booking" state={{ masterId: m.id }} className="mt-6 inline-block">
              <Button>{t("masters.bookWith")}</Button>
            </Link>
          </div>
        </div>
      </Card>

      {/* Работы (портфолио) — секция скрыта, если фото нет */}
      {works.data && works.data.length > 0 && (
        <section className="mt-10">
          <h2 className="font-display text-2xl text-fg">{t("works.title")}</h2>
          <p className="mt-1 text-sm text-fg-subtle">{t("works.sectionSubtitle")}</p>
          <div className="mt-4 grid grid-cols-2 gap-3 sm:grid-cols-3">
            {works.data.map((w) => (
              <a
                key={w.id}
                href={w.url}
                target="_blank"
                rel="noreferrer"
                className="group overflow-hidden rounded-xl border border-white/10"
              >
                <img
                  src={w.url}
                  alt=""
                  loading="lazy"
                  className="aspect-square w-full object-cover transition-transform duration-300 group-hover:scale-105"
                />
              </a>
            ))}
          </div>
        </section>
      )}

      {/* Отзывы */}
      <section className="mt-10">
        <h2 className="font-display text-2xl text-fg">{t("home.reviewsTitle")}</h2>
        {reviews.isLoading ? (
          <LoadingState />
        ) : reviews.data && reviews.data.items.length > 0 ? (
          <div className="mt-4 space-y-4">
            {reviews.data.items.map((r) => (
              <Card key={r.id} className="p-5">
                <div className="flex items-center justify-between">
                  <span className="font-medium text-fg">{r.guestName}</span>
                  <StarRating value={r.rating} />
                </div>
                {r.comment && <p className="mt-2 text-fg-muted">{r.comment}</p>}
                <p className="mt-2 text-xs text-fg-subtle">{formatDate(r.createdAt, locale)}</p>
              </Card>
            ))}
          </div>
        ) : (
          <p className="mt-4 text-fg-subtle">{t("masters.noRating")}</p>
        )}
      </section>
    </div>
  );
}

/** Иконка трубки для публичного контакта мастера. */
function PhoneIcon() {
  return (
    <svg
      viewBox="0 0 24 24"
      aria-hidden="true"
      className="h-4 w-4 shrink-0"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M22 16.9v3a2 2 0 0 1-2.2 2 19.8 19.8 0 0 1-8.6-3.1 19.5 19.5 0 0 1-6-6A19.8 19.8 0 0 1 2.1 4.2 2 2 0 0 1 4.1 2h3a2 2 0 0 1 2 1.7c.1 1 .4 1.9.7 2.8a2 2 0 0 1-.5 2.1L8.1 9.9a16 16 0 0 0 6 6l1.3-1.2a2 2 0 0 1 2.1-.5c.9.3 1.8.6 2.8.7a2 2 0 0 1 1.7 2Z" />
    </svg>
  );
}
