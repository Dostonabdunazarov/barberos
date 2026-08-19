import { useTranslation } from "react-i18next";
import { useNavigate, useLocation, Navigate, Link } from "react-router-dom";
import { LogoLockup } from "../components/Logo";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { motion } from "framer-motion";
import { api, apiErrorMessage } from "../lib/api";
import { useAuthStore, isAdmin } from "../store/authStore";
import { CardStrong } from "../components/ui/Card";
import { Button } from "../components/ui/Button";
import { Input, Field } from "../components/ui/Input";
import { Spinner } from "../components/ui/misc";
import type { LoginResponse } from "../types";

const schema = z.object({
  email: z.string().trim().email(),
  password: z.string().min(1),
});
type Form = z.infer<typeof schema>;

export function LoginPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const location = useLocation();
  const { user, setAuth } = useAuthStore();

  const from = (location.state as { from?: string } | null)?.from;

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<Form>({ resolver: zodResolver(schema) });

  // Уже вошли — уводим из /login.
  if (user) return <Navigate to={from ?? (isAdmin(user) ? "/admin" : "/dashboard")} replace />;

  async function onSubmit(values: Form) {
    try {
      const { data } = await api.post<LoginResponse>("/auth/login", values);
      setAuth(data.user, data.accessToken);
      navigate(from ?? (isAdmin(data.user) ? "/admin" : "/dashboard"), { replace: true });
    } catch (err) {
      setError("root", { message: apiErrorMessage(err, t("auth.invalid")) });
    }
  }

  return (
    <div className="w-full max-w-sm">
      <Link to="/" className="mb-6 block">
        <LogoLockup className="mx-auto w-full max-w-xs drop-shadow-[0_4px_16px_rgba(212,169,95,0.2)]" />
      </Link>
      <motion.div initial={{ opacity: 0, y: 16 }} animate={{ opacity: 1, y: 0 }}>
        <CardStrong className="p-8">
          <h1 className="font-display text-2xl text-fg">{t("auth.loginTitle")}</h1>
          <p className="mt-1 text-sm text-fg-subtle">{t("auth.onlyStaff")}</p>

          <form onSubmit={handleSubmit(onSubmit)} className="mt-6 space-y-4">
            <Field label={t("auth.email")} error={errors.email && t("common.required")}>
              <Input type="email" autoComplete="username" {...register("email")} />
            </Field>
            <Field label={t("auth.password")} error={errors.password && t("common.required")}>
              <Input type="password" autoComplete="current-password" {...register("password")} />
            </Field>

            {errors.root && <p className="text-sm text-red-400">{errors.root.message}</p>}

            <Button type="submit" disabled={isSubmitting} className="w-full">
              {isSubmitting ? <Spinner className="h-4 w-4" /> : null}
              {isSubmitting ? t("auth.signingIn") : t("auth.signIn")}
            </Button>
          </form>
        </CardStrong>
      </motion.div>
    </div>
  );
}
