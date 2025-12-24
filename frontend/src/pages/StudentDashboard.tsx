import React from "react";
import { useNavigate } from "react-router-dom";
import { logout, getCurrentUser } from "../services/authService";

const StudentDashboard: React.FC = () => {
  const navigate = useNavigate();
  const user = getCurrentUser();

  const handleLogout = () => {
    logout();
    navigate("/");
  };

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col">
      {/* Üst bar */}
      <header className="w-full border-b bg-[#d71920] text-white">
        <div className="max-w-6xl mx-auto flex items-center justify-between px-6 py-6">
          <h1 className="text-2xl font-semibold">Başkent Yaşam – Öğrenci</h1>
          <div className="flex items-center gap-4 text-base">
            <span>Hoş geldiniz, {user?.name || "Öğrenci"}</span>
            <button
              onClick={handleLogout}
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
    </div>
  );
};

export default StudentDashboard;
