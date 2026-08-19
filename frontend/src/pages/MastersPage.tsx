import { useTranslation } from "react-i18next";
import { useMasters } from "../lib/hooks";
import { MasterCard } from "../components/MasterCard";
import { LoadingState, ErrorState } from "../components/ui/misc";
import { apiErrorMessage } from "../lib/api";

export function MastersPage() {
  const { t } = useTranslation();
  const { data, isLoading, error, refetch } = useMasters();

  return (
    <div>
      <h1 className="font-display text-3xl text-fg sm:text-4xl">{t("home.mastersTitle")}</h1>
      <p className="mt-2 text-fg-muted">{t("home.mastersSubtitle")}</p>

      {isLoading ? (
        <LoadingState />
      ) : error ? (
        <ErrorState message={apiErrorMessage(error)} onRetry={refetch} />
      ) : data && data.length > 0 ? (
        <div className="mt-8 grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
          {data.map((m) => (
            <MasterCard key={m.id} master={m} />
          ))}
        </div>
      ) : (
        <p className="mt-8 text-center text-fg-subtle">{t("masters.empty")}</p>
      )}
    </div>
  );
}
