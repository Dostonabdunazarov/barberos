/**
 * Флоу бронирования: услуга → мастер → дата/слот → SMS-верификация → подтверждение.
 * TODO: реализовать шаги с использованием useQuery к /api/services, /api/masters,
 * /api/availability и /api/bookings.
 */
export function BookingPage() {
  return (
    <div className="mx-auto max-w-3xl p-8">
      <h1 className="text-2xl font-bold">Бронирование</h1>
      <p className="mt-2 text-neutral-500">
        Здесь будет пошаговый выбор услуги, мастера и времени.
      </p>
    </div>
  );
}
