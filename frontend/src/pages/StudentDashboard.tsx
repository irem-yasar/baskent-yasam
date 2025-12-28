import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { logout, getCurrentUser } from "../services/authService";
import { getNotifications, markAsRead, Notification } from "../services/notificationService";

const StudentDashboard: React.FC = () => {
  const navigate = useNavigate();
  const user = getCurrentUser();

  const [showLogoutModal, setShowLogoutModal] = useState(false);
  const [showNotifications, setShowNotifications] = useState(false);
  const [notifications, setNotifications] = useState<Notification[]>([]);
  
  const openLogoutModal = () => setShowLogoutModal(true);
  const closeLogoutModal = () => setShowLogoutModal(false);

  useEffect(() => {
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
      await loadNotifications();
    } catch (err) {
      console.error("Bildirim okundu olarak işaretlenirken hata:", err);
    }
  };

  const unreadCount = notifications.filter(n => !n.isRead).length;

  const handleLogout = () => {
    logout();
    setShowLogoutModal(false);
    navigate("/");
  };

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col">
      {/* Üst bar */}
      <header className="w-full border-b bg-[#d71920] text-white">
        <div className="max-w-6xl mx-auto flex items-center justify-between px-6 py-6">
          <h1 className="text-2xl font-semibold">Başkent Yaşam – Öğrenci</h1>
          <div className="flex items-center gap-4 text-base">
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

            <span>Hoş geldiniz, {user?.name || "Öğrenci"}</span>
            <button
              onClick={openLogoutModal}
              className="hover:underline text-sm text-white"
            >
              Çıkış yap
            </button>
          </div>
        </div>
      </header>

      {/* İçerik - ortalanmış */}
      <main className="flex-1 flex items-center justify-center px-6 py-10">
        <div className="w-full max-w-6xl">
          {/* Genişletilmiş grid */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-8 mx-auto">
            {/* Görüşme */}
            <section
              onClick={() => navigate("/randevu")}
              className="bg-white rounded-2xl border border-slate-200 p-6 shadow hover:shadow-lg transition min-h-[150px] cursor-pointer"
            >
              <h3 className="text-lg font-semibold text-slate-900 mb-2">
                Öğretim Elemanıyla Görüşme
              </h3>
              <p className="text-slate-600 text-base">
                Müsaitlik saatlerini görüp randevu talebi oluşturun.
              </p>
            </section>

            {/* Kütüphane */}
            <section className="bg-white rounded-2xl border border-slate-200 p-6 shadow hover:shadow-lg transition min-h-[150px] cursor-default">
              <h3 className="text-lg font-semibold text-slate-900 mb-2">
                Kütüphane Doluluk
              </h3>
              <p className="text-slate-600 text-base">
                Kütüphanedeki anlık doluluk oranını görüntüleyin.
              </p>
            </section>

            {/* Kafeterya */}
            <section
              onClick={() => navigate("/kafeterya")}
              className="bg-white rounded-2xl border border-slate-200 p-6 shadow hover:shadow-lg transition min-h-[150px] cursor-pointer"
            >
              <h3 className="text-lg font-semibold text-slate-900 mb-2">
                Kafeterya Sipariş
              </h3>
              <p className="text-slate-600 text-base">
                Menüden yemek seçip ileriki bir saat için sipariş verin.
              </p>
            </section>

            {/* Otopark */}
            <section className="bg-white rounded-2xl border border-slate-200 p-6 shadow hover:shadow-lg transition min-h-[150px] cursor-default">
              <h3 className="text-lg font-semibold text-slate-900 mb-2">
                Otopark Durumu
              </h3>
              <p className="text-slate-600 text-base">
                Otoparktaki anlık doluluk oranını görüntüleyin.
              </p>
            </section>
          </div>
        </div>
      </main>

      {showLogoutModal && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50"
          onClick={closeLogoutModal}
        >
          <div
            className="bg-white rounded-lg p-6 w-full max-w-md mx-4"
            role="dialog"
            aria-modal="true"
            aria-labelledby="logout-title"
            onClick={(e) => e.stopPropagation()}
          >
            <h2 id="logout-title" className="text-lg font-semibold mb-4">
              Çıkış Yap
            </h2>
            <p className="text-slate-600 mb-6">
              Çıkış yapmak istediğinizden emin misiniz?
            </p>
            <div className="flex justify-end gap-3">
              <button
                onClick={closeLogoutModal}
                className="px-4 py-2 rounded-md border"
              >
                İptal
              </button>
              <button
                onClick={() => {
                  handleLogout();
                }}
                className="px-4 py-2 rounded-md bg-[#d71920] text-white"
              >
                Çıkış Yap
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default StudentDashboard;
