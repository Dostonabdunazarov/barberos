import { lazy, Suspense, useEffect } from "react";
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { QueryClientProvider } from "@tanstack/react-query";
import { queryClient } from "./lib/queryClient";
import { bootstrapAuth } from "./lib/api";
import { Layout } from "./components/Layout";
import { HomePage } from "./pages/HomePage";
import { ServicesPage } from "./pages/ServicesPage";
import { MastersPage } from "./pages/MastersPage";
import { MasterProfilePage } from "./pages/MasterProfilePage";
import { BookingPage } from "./pages/BookingPage";
import { ManageBookingPage } from "./pages/ManageBookingPage";
import { LoginPage } from "./pages/LoginPage";
import { RequireAuth } from "./components/RequireAuth";
import { LoadingState } from "./components/ui/misc";

// Кабинеты персонала — отдельными чанками (публичному посетителю не нужны).
const DashboardPage = lazy(() =>
  import("./pages/DashboardPage").then((m) => ({ default: m.DashboardPage })),
);
const AdminPage = lazy(() => import("./pages/AdminPage").then((m) => ({ default: m.AdminPage })));

export default function App() {
  // Silent login по httpOnly refresh-cookie при загрузке приложения.
  useEffect(() => {
    void bootstrapAuth();
  }, []);

  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Layout>
          <Routes>
            {/* Публичные */}
            <Route path="/" element={<HomePage />} />
            <Route path="/services" element={<ServicesPage />} />
            <Route path="/masters" element={<MastersPage />} />
            <Route path="/masters/:id" element={<MasterProfilePage />} />
            <Route path="/booking" element={<BookingPage />} />
            <Route path="/booking/:token" element={<ManageBookingPage />} />
            <Route path="/login" element={<LoginPage />} />

            {/* Персонал */}
            <Route
              path="/dashboard"
              element={
                <RequireAuth>
                  <Suspense fallback={<LoadingState />}>
                    <DashboardPage />
                  </Suspense>
                </RequireAuth>
              }
            />
            <Route
              path="/admin/*"
              element={
                <RequireAuth requireAdmin>
                  <Suspense fallback={<LoadingState />}>
                    <AdminPage />
                  </Suspense>
                </RequireAuth>
              }
            />

            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </Layout>
      </BrowserRouter>
    </QueryClientProvider>
  );
}
