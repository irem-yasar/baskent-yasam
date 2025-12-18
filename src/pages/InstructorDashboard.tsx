import React from "react";
import { useNavigate } from "react-router-dom";

const InstructorDashboard: React.FC = () => {
  const navigate = useNavigate();

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col">
      {/* Üst bar */}
      <header className="w-full border-b bg-[#d71920] text-white">
        <div className="max-w-6xl mx-auto flex items-center justify-between px-6 py-6">
          <h1 className="text-2xl font-semibold">
            Başkent Yaşam – Öğretim Elemanı
          </h1>
          <button
            onClick={() => navigate("/")}
            className="hover:underline text-sm"
          >
            Çıkış yap
          </button>
        </div>
      </header>

      {/* Kartlar */}
      <main className="flex-1 flex items-center justify-center px-6 py-10">
        <div className="w-full max-w-6xl grid grid-cols-1 md:grid-cols-2 gap-8">
          {/* Randevu Yönetimi */}
          <button
            onClick={() => navigate("/randevu-yonetimi")}
            className="text-left bg-white rounded-2xl border p-6 shadow hover:shadow-lg transition"
          >
            <h3 className="text-lg font-semibold mb-2">Randevu Yönetimi</h3>
            <p className="text-slate-600">
              Öğrencilerden gelen randevuları yönetin.
            </p>
          </button>

          {/* Diğerleri (aynı) */}
          <div className="bg-white rounded-2xl border p-6 shadow">
            Kütüphane Doluluk
          </div>

          <div className="bg-white rounded-2xl border p-6 shadow">
            Kafeterya Sipariş
          </div>

          <div className="bg-white rounded-2xl border p-6 shadow">
            Otopark Durumu
          </div>
        </div>
      </main>
    </div>
  );
};

export default InstructorDashboard;
