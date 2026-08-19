import { useState } from "react";
import { useTranslation } from "react-i18next";
import { motion } from "framer-motion";
import { Card } from "./ui/Card";
import { Button } from "./ui/Button";
import { Textarea, Field } from "./ui/Input";
import { Spinner } from "./ui/misc";
import { useCreateReview } from "../lib/hooks";
import { apiErrorMessage } from "../lib/api";
import { cn } from "../lib/utils";

/** Интерактивный выбор рейтинга звёздами. */
function StarPicker({ value, onChange }: { value: number; onChange: (v: number) => void }) {
  const [hover, setHover] = useState(0);
  return (
    <div className="flex gap-1 text-3xl" onMouseLeave={() => setHover(0)}>
      {[1, 2, 3, 4, 5].map((i) => (
        <button
          key={i}
          type="button"
          onClick={() => onChange(i)}
          onMouseEnter={() => setHover(i)}
          className={cn(
            "transition-colors",
            i <= (hover || value) ? "text-accent-400" : "text-ink-600 hover:text-accent-500/50",
          )}
          aria-label={`${i}`}
        >
          ★
        </button>
      ))}
    </div>
  );
}

export function ReviewForm({ token }: { token: string }) {
  const { t } = useTranslation();
  const createReview = useCreateReview(token);
  const [rating, setRating] = useState(5);
  const [comment, setComment] = useState("");

  if (createReview.isSuccess) {
    return (
      <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
        <Card className="p-6 text-center">
          <div className="text-3xl">🙏</div>
          <h3 className="mt-2 font-display text-xl text-fg">{t("review.successTitle")}</h3>
          <p className="mt-1 text-sm text-fg-muted">{t("review.successText")}</p>
        </Card>
      </motion.div>
    );
  }

  return (
    <Card className="p-6">
      <h3 className="font-display text-xl text-fg">{t("review.title")}</h3>
      <form
        className="mt-4 space-y-4"
        onSubmit={(e) => {
          e.preventDefault();
          createReview.mutate({ rating, comment: comment.trim() || undefined });
        }}
      >
        <Field label={t("review.rating")}>
          <StarPicker value={rating} onChange={setRating} />
        </Field>
        <Field label={t("review.comment")}>
          <Textarea
            value={comment}
            onChange={(e) => setComment(e.target.value)}
            placeholder={t("review.commentPlaceholder")}
            maxLength={2000}
          />
        </Field>

        {createReview.isError && (
          <p className="text-sm text-red-400">{apiErrorMessage(createReview.error)}</p>
        )}

        <Button type="submit" disabled={createReview.isPending}>
          {createReview.isPending && <Spinner className="h-4 w-4" />}
          {t("review.submit")}
        </Button>
      </form>
    </Card>
  );
}
