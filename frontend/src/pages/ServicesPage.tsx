import { useTranslation } from "react-i18next";
import { useServices } from "../lib/hooks";
import { ServiceCard } from "../components/ServiceCard";
import { LoadingState, ErrorState } from "../components/ui/misc";
import { apiErrorMessage } from "../lib/api";

export function ServicesPage() {
  const { t } = useTranslation();
  const { data, isLoading, error, refetch } = useServices();

  return (
    <div>
      <h1 className="font-display text-3xl text-fg sm:text-4xl">{t("home.servicesTitle")}</h1>
      <p className="mt-2 text-fg-muted">{t("home.servicesSubtitle")}</p>

      {isLoading ? (
        <LoadingState />
      ) : error ? (
        <ErrorState message={apiErrorMessage(error)} onRetry={refetch} />
      ) : data && data.length > 0 ? (
        <div className="mt-8 grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
          {data.map((s) => (
            <ServiceCard key={s.id} service={s} />
          ))}
        </div>
      ) : (
        <p className="mt-8 text-center text-fg-subtle">{t("services.empty")}</p>
      )}
    </div>
  );
}
