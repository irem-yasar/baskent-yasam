import React, { useState, useEffect } from "react";
import { Link } from "react-router-dom";
import { getStudentAppointments, Appointment, ApiError } from "../services/appointmentService";
import { getNotifications, markAsRead, Notification } from "../services/notificationService";

const StudentAppointmentsPage: React.FC = () => {
  const [appointments, setAppointments] = useState<Appointment[]>([]);
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>("");
  const [showNotifications, setShowNotifications] = useState(false);

  useEffect(() => {
    loadAppointments();
    loadNotifications();
    // Her 30 saniyede bir bildirimleri yenile
    const interval = setInterval(loadNotifications, 30000);
    return () => clearInterval(interval);
  }, []);

  // Dropdown dışına tıklandığında kapat
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      const target = event.target as HTMLElement;
      if (showNotifications && !target.closest('.notification-dropdown-container')) {
        setShowNotifications(false);
      }
    };

    if (showNotifications) {
      document.addEventListener('mousedown', handleClickOutside);
      return () => document.removeEventListener('mousedown', handleClickOutside);
    }
  }, [showNotifications]);

  const loadAppointments = async () => {
    setLoading(true);
    setError("");
    try {
      const data = await getStudentAppointments();
      setAppointments(data);
    } catch (err) {
      const apiError = err as ApiError;
      setError(apiError.message || "Randevular yüklenirken bir hata oluştu");
    } finally {
      setLoading(false);
    }
  };

  const loadNotifications = async () => {
    try {
      const data = await getNotifications();
      setNotifications(data);
    } catch (err) {
      console.error("Bildirimler yüklenirken hata:", err);
    }
  };

  const handleMarkAsRead = async (notificationId: number) => {
    try {
      await markAsRead(notificationId);
      await loadNotifications(); // Listeyi yenile
    } catch (err) {
      console.error("Bildirim okundu olarak işaretlenirken hata:", err);
    }
  };

  // Randevuları duruma göre filtrele
  const pendingAppointments = appointments.filter(apt => apt.status === "pending");
  const approvedAppointments = appointments.filter(apt => apt.status === "approved");
  const rejectedAppointments = appointments.filter(apt => apt.status === "rejected");

  // Okunmamış bildirim sayısı
  const unreadCount = notifications.filter(n => !n.isRead).length;

  // Tarih formatla
  const formatDate = (dateString: string) => {
    try {
      const date = new Date(dateString);
      return date.toLocaleDateString("tr-TR", {
        year: "numeric",
        month: "long",
        day: "numeric",
      });
    } catch {
      return dateString;
    }
  };

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col">
      {/* Üst bar */}
      <header className="w-full border-b bg-[#d71920] text-white">
        <div className="max-w-6xl mx-auto flex items-center justify-between px-6 py-6">
          <h1 className="text-2xl font-semibold">Randevularım</h1>
          <div className="flex items-center gap-4">
            {/* Bildirim ikonu */}
            <div className="relative notification-dropdown-container">
              <button
                onClick={() => setShowNotifications(!showNotifications)}
                className="relative p-2 hover:bg-red-700 rounded-lg transition"
                title="Bildirimler"
              >
                <svg
                  xmlns="http://www.w3.org/2000/svg"
                  className="h-6 w-6"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"
                  />
                </svg>
                {unreadCount > 0 && (
                  <span className="absolute top-0 right-0 bg-yellow-400 text-red-600 text-xs font-bold rounded-full h-5 w-5 flex items-center justify-center">
                    {unreadCount}
                  </span>
                )}
              </button>

              {/* Bildirim dropdown */}
              {showNotifications && (
                <div className="absolute right-0 mt-2 w-80 bg-white rounded-lg shadow-lg border border-slate-200 z-50 max-h-96 overflow-y-auto">
                  <div className="p-4 border-b border-slate-200">
                    <h3 className="font-semibold text-slate-900">Bildirimler</h3>
                  </div>
                  {notifications.length === 0 ? (
                    <div className="p-4 text-slate-500 text-sm">
                      Bildirim bulunmamaktadır.
                    </div>
                  ) : (
                    <div className="divide-y divide-slate-200">
                      {notifications.map((notification) => (
                        <div
                          key={notification.id}
                          className={`p-4 hover:bg-slate-50 cursor-pointer ${
                            !notification.isRead ? "bg-blue-50" : ""
                          }`}
                          onClick={() => {
                            if (!notification.isRead) {
                              handleMarkAsRead(notification.id);
                            }
                          }}
                        >
                          <div className="flex items-start justify-between">
                            <div className="flex-1">
                              <p className="font-semibold text-slate-900 text-sm">
                                {notification.title}
                              </p>
                              <p className="text-sm text-slate-600 mt-1">
                                {notification.message}
                              </p>
                              <p className="text-xs text-slate-400 mt-1">
                                {new Date(notification.createdAt).toLocaleString("tr-TR")}
                              </p>
                            </div>
                            {!notification.isRead && (
                              <span className="ml-2 h-2 w-2 bg-blue-500 rounded-full"></span>
                            )}
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              )}
            </div>

            <Link
              to="/ogrenci"
              className="text-white hover:underline text-sm"
            >
              Öğrenci anasayfasına dön
            </Link>
          </div>
        </div>
      </header>

      <main className="flex-1 max-w-6xl mx-auto w-full px-6 py-8">
        {error && (
          <div className="mb-4 p-4 bg-red-100 border border-red-400 text-red-700 rounded-lg">
            {error}
          </div>
        )}

        {loading ? (
          <div className="text-center py-8">
            <p className="text-slate-600">Yükleniyor...</p>
          </div>
        ) : (
          <div className="space-y-6">
            {/* Bekleyen Randevular */}
            <section className="bg-white rounded-xl border p-6 shadow">
              <h2 className="text-lg font-semibold mb-4 text-slate-900">
                Bekleyen Randevular ({pendingAppointments.length})
              </h2>
              {pendingAppointments.length === 0 ? (
                <p className="text-slate-500 text-sm">
                  Bekleyen randevu bulunmamaktadır.
                </p>
              ) : (
                <div className="space-y-4">
                  {pendingAppointments.map((apt) => (
                    <div
                      key={apt.id}
                      className="border border-slate-200 rounded-lg p-4 bg-slate-50"
                    >
                      <div className="flex items-start justify-between">
                        <div className="flex-1">
                          <p className="font-semibold text-slate-900">
                            {apt.course || "Ders belirtilmemiş"}
                          </p>
                          <p className="text-sm text-slate-600 mt-1">
                            {apt.reason}
                          </p>
                          <p className="text-sm text-slate-600 mt-1">
                            {formatDate(apt.date)} - {apt.time}
                          </p>
                        </div>
                        <span className="px-3 py-1 bg-yellow-100 text-yellow-800 text-xs font-semibold rounded-full">
                          Beklemede
                        </span>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </section>

            {/* Onaylanan Randevular */}
            <section className="bg-white rounded-xl border p-6 shadow">
              <h2 className="text-lg font-semibold mb-4 text-slate-900">
                Onaylanan Randevular ({approvedAppointments.length})
              </h2>
              {approvedAppointments.length === 0 ? (
                <p className="text-slate-500 text-sm">
                  Onaylanan randevu bulunmamaktadır.
                </p>
              ) : (
                <div className="space-y-4">
                  {approvedAppointments.map((apt) => (
                    <div
                      key={apt.id}
                      className="border border-slate-200 rounded-lg p-4 bg-green-50"
                    >
                      <div className="flex items-start justify-between">
                        <div className="flex-1">
                          <p className="font-semibold text-slate-900">
                            {apt.course || "Ders belirtilmemiş"}
                          </p>
                          <p className="text-sm text-slate-600 mt-1">
                            {apt.reason}
                          </p>
                          <p className="text-sm text-slate-600 mt-1">
                            {formatDate(apt.date)} - {apt.time}
                          </p>
                        </div>
                        <span className="px-3 py-1 bg-green-100 text-green-800 text-xs font-semibold rounded-full">
                          Onaylandı
                        </span>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </section>

            {/* Reddedilen Randevular */}
            <section className="bg-white rounded-xl border p-6 shadow">
              <h2 className="text-lg font-semibold mb-4 text-slate-900">
                Reddedilen Randevular ({rejectedAppointments.length})
              </h2>
              {rejectedAppointments.length === 0 ? (
                <p className="text-slate-500 text-sm">
                  Reddedilen randevu bulunmamaktadır.
                </p>
              ) : (
                <div className="space-y-4">
                  {rejectedAppointments.map((apt) => (
                    <div
                      key={apt.id}
                      className="border border-slate-200 rounded-lg p-4 bg-red-50"
                    >
                      <div className="flex items-start justify-between">
                        <div className="flex-1">
                          <p className="font-semibold text-slate-900">
                            {apt.course || "Ders belirtilmemiş"}
                          </p>
                          <p className="text-sm text-slate-600 mt-1">
                            {apt.reason}
                          </p>
                          <p className="text-sm text-slate-600 mt-1">
                            {formatDate(apt.date)} - {apt.time}
                          </p>
                          {/* Reddetme sebebi */}
                          {apt.rejectionReason && (
                            <div className="mt-3 p-3 bg-red-100 border border-red-200 rounded-lg">
                              <p className="text-xs font-semibold text-red-800 mb-1">
                                Reddetme Sebebi:
                              </p>
                              <p className="text-sm text-red-700">
                                {apt.rejectionReason}
                              </p>
                            </div>
                          )}
                        </div>
                        <span className="px-3 py-1 bg-red-100 text-red-800 text-xs font-semibold rounded-full">
                          Reddedildi
                        </span>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </section>
          </div>
        )}
      </main>
    </div>
  );
};

export default StudentAppointmentsPage;

