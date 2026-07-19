/**
 * Личный кабинет клиента: активные записи и история.
 * TODO: useQuery к /api/bookings/my, кнопки отмены/переноса, форма отзыва.
 */
export function MyBookingsPage() {
  return (
    <div className="mx-auto max-w-3xl p-8">
      <h1 className="text-2xl font-bold">Мои записи</h1>
      <p className="mt-2 text-neutral-500">Активные записи и история визитов.</p>
    </div>
  );
}
