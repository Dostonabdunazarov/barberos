/**
 * Вход по телефону + SMS-код.
 * TODO: шаг 1 — ввод телефона (POST /api/auth/request-otp),
 *       шаг 2 — ввод кода (POST /api/auth/verify-otp) → сохранить токены в authStore.
 */
export function LoginPage() {
  return (
    <div className="mx-auto max-w-sm p-8">
      <h1 className="text-2xl font-bold">Вход</h1>
      <p className="mt-2 text-neutral-500">Вход по номеру телефона и SMS-коду.</p>
    </div>
  );
}
