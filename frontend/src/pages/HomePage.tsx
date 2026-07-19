import { Link } from "react-router-dom";

export function HomePage() {
  return (
    <div className="mx-auto max-w-3xl p-8 text-center">
      <h1 className="text-4xl font-bold">Barberos</h1>
      <p className="mt-2 text-neutral-500">Онлайн-запись в барбершоп</p>
      <Link
        to="/booking"
        className="mt-6 inline-block rounded-lg bg-neutral-900 px-6 py-3 text-white hover:bg-neutral-700"
      >
        Записаться
      </Link>
    </div>
  );
}
