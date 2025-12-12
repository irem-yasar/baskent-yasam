import React, { useState } from "react";
import { Link } from "react-router-dom";

type Reason = "question" | "exam" | "other";

const TeacherAppointmentPage: React.FC = () => {
  const [lecturerName, setLecturerName] = useState("");
  const [course, setCourse] = useState("");
  const [reason, setReason] = useState<Reason>("question");
  const [otherReason, setOtherReason] = useState("");
  const [date, setDate] = useState("");
  const [time, setTime] = useState("");
  const [note, setNote] = useState("");

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    const finalReason =
      reason === "other"
        ? otherReason
        : reason === "question"
        ? "Soru sorma"
        : "Sınav kağıdına bakma";

    console.log({
      lecturerName,
      course,
      reason: finalReason,
      date,
      time,
      note,
    });

    alert("Randevu talebiniz alındı");
  };

  const isOtherSelected = reason === "other";

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col">
      {/* Üst bar */}
      <header className="w-full border-b bg-[#d71920] text-white">
        <div className="max-w-6xl mx-auto flex items-center justify-between px-6 py-6">
          <h1 className="text-2xl font-semibold">Öğretim Elemanıyla Görüşme</h1>
          <Link to="/ogrenci" className="text-sm underline hover:opacity-90">
            Öğrenci anasayfasına dön
          </Link>
        </div>
      </header>

      {/* İçerik */}
      <main className="flex-1 flex items-center justify-center px-4 py-8">
        <form
          onSubmit={handleSubmit}
          className="w-full max-w-lg bg-white rounded-2xl shadow-md border border-slate-200 p-6 space-y-4"
        >
          <h2 className="text-xl font-semibold text-slate-900 mb-2">
            Randevu Talep Formu
          </h2>
          <p className="text-sm text-slate-600 mb-4">
            Görüşme yapmak istediğiniz öğretim elemanını, ders bilgisini ve
            uygun olduğunuz tarih/saat bilgisini giriniz.
          </p>

          {/* Öğretim elemanı adı */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">
              Öğretim Elemanı
            </label>
            <input
              //öğretim elemanlarını listele!!!!!! öğrenciler dersi kendisi seçsin!!!!!
              type="text"
              value={lecturerName}
              onChange={(e) => setLecturerName(e.target.value)}
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              required
            />
          </div>

          {/* Ders */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">
              Ders
            </label>
            <input
              type="text"
              value={course}
              onChange={(e) => setCourse(e.target.value)}
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            />
          </div>

          {/* Görüşme sebebi */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">
              Görüşme sebebi
            </label>
            <div className="flex flex-col gap-2 text-sm text-slate-700">
              <label className="inline-flex items-center gap-2">
                <input
                  type="radio"
                  name="reason"
                  value="question"
                  checked={reason === "question"}
                  onChange={() => setReason("question")}
                />
                <span>Soru sorma</span>
              </label>
              <label className="inline-flex items-center gap-2">
                <input
                  type="radio"
                  name="reason"
                  value="exam"
                  checked={reason === "exam"}
                  onChange={() => setReason("exam")}
                />
                <span>Sınav kağıdına bakma</span>
              </label>
              <label className="inline-flex items-center gap-2">
                <input
                  type="radio"
                  name="reason"
                  value="other"
                  checked={reason === "other"}
                  onChange={() => setReason("other")}
                />
                <span>Diğer</span>
              </label>
            </div>

            {isOtherSelected && (
              <textarea
                className="mt-2 w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                rows={2}
                //kaç elemanlık metin giriliyosa ekle
                value={otherReason}
                onChange={(e) => setOtherReason(e.target.value)}
              />
            )}
          </div>

          {/* Tarih - Saat */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">
                Tarih
              </label>
              <input
                type="date"
                value={date}
                onChange={(e) => setDate(e.target.value)}
                className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                required
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">
                Saat
              </label>
              <input
                type="time"
                value={time}
                onChange={(e) => setTime(e.target.value)}
                className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                required
              />
            </div>
          </div>

          {/* Gönder butonu */}
          <div className="pt-2">
            <button
              type="submit"
              className="w-full rounded-lg bg-blue-600 text-white text-sm font-medium py-2.5 hover:bg-blue-700 transition"
            >
              Randevu talebi gönder
            </button>
          </div>
        </form>
      </main>
    </div>
  );
};

export default TeacherAppointmentPage;
