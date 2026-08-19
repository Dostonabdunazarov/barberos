import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { motion } from "framer-motion";
import type { Master } from "../types";
import { Card } from "./ui/Card";
import { StarRating } from "./ui/misc";
import { useMasterReviews } from "../lib/hooks";

/** Карточка мастера с агрегированным рейтингом; ведёт на профиль. */
export function MasterCard({ master }: { master: Master }) {
  const { t } = useTranslation();
  const { data: reviews } = useMasterReviews(master.id);
  const rating = reviews?.rating;

  return (
    <motion.div whileHover={{ y: -4 }} transition={{ type: "spring", stiffness: 300, damping: 25 }}>
      <Link to={`/masters/${master.id}`} className="block">
        <Card className="overflow-hidden transition-colors hover:border-accent-500/30">
          <div className="aspect-[4/3] w-full overflow-hidden bg-ink-800">
            {master.photoUrl ? (
              <img
                src={master.photoUrl}
                alt={master.name}
                loading="lazy"
                className="h-full w-full object-cover"
              />
            ) : (
              <div className="flex h-full w-full items-center justify-center font-display text-5xl text-ink-600">
                {master.name.charAt(0)}
              </div>
            )}
          </div>
          <div className="p-4">
            <h3 className="font-display text-lg text-fg">{master.name}</h3>
            {master.bio && <p className="mt-1 line-clamp-2 text-sm text-fg-muted">{master.bio}</p>}
            <div className="mt-3 flex items-center gap-2 text-sm">
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
          </div>
        </Card>
      </Link>
    </motion.div>
  );
}
