import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { motion } from "framer-motion";
import { useServices, useMasters } from "../lib/hooks";
import { ServiceCard } from "../components/ServiceCard";
import { MasterCard } from "../components/MasterCard";
import { LoadingState } from "../components/ui/misc";
import { LogoLockup } from "../components/Logo";

function Section({
  title,
  subtitle,
  children,
}: {
  title: string;
  subtitle?: string;
  children: React.ReactNode;
}) {
  return (
    <section className="mt-20">
      <div className="mb-8 text-center">
        <h2 className="font-display text-3xl text-fg sm:text-4xl">{title}</h2>
        {subtitle && <p className="mt-2 text-fg-muted">{subtitle}</p>}
      </div>
      {children}
    </section>
  );
}

export function HomePage() {
  const { t } = useTranslation();
  const services = useServices();
  const masters = useMasters();

  return (
    <div>
      {/* Hero */}
      <section className="flex min-h-[70vh] items-center justify-center py-16">
        <motion.div
          initial={{ opacity: 0, y: 24 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.6, ease: "easeOut" }}
          className="card-plate mx-auto max-w-2xl rounded-3xl px-8 py-12 text-center sm:px-14"
        >
          <LogoLockup className="mx-auto w-full max-w-sm" />
          <h1 className="mt-4 font-display text-3xl leading-tight text-fg sm:text-4xl">
            <span className="text-gradient-accent">{t("home.heroTitle")}</span>
          </h1>
          <p className="mx-auto mt-3 max-w-md text-lg text-fg-muted">{t("home.heroSubtitle")}</p>
          <Link
            to="/booking"
            className="mt-8 inline-flex rounded-2xl bg-accent-500 px-8 py-4 font-semibold text-ink-950 shadow-lg shadow-accent-700/25 transition-colors hover:bg-accent-400"
          >
            {t("home.heroCta")}
          </Link>
        </motion.div>
      </section>

      {/* Услуги */}
      <Section title={t("home.servicesTitle")} subtitle={t("home.servicesSubtitle")}>
        {services.isLoading ? (
          <LoadingState />
        ) : services.data && services.data.length > 0 ? (
          <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {services.data.map((s) => (
              <ServiceCard key={s.id} service={s} />
            ))}
          </div>
        ) : (
          <p className="text-center text-fg-subtle">{t("services.empty")}</p>
        )}
      </Section>

      {/* Мастера */}
      <Section title={t("home.mastersTitle")} subtitle={t("home.mastersSubtitle")}>
        {masters.isLoading ? (
          <LoadingState />
        ) : masters.data && masters.data.length > 0 ? (
          <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {masters.data.map((m) => (
              <MasterCard key={m.id} master={m} />
            ))}
          </div>
        ) : (
          <p className="text-center text-fg-subtle">{t("masters.empty")}</p>
        )}
      </Section>
    </div>
  );
}
