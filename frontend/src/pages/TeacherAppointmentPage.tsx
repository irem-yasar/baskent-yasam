import React, { useState, useEffect } from "react";
import { Link, useNavigate } from "react-router-dom";
import { createAppointment, getStudentAppointments, Appointment, ApiError } from "../services/appointmentService";
import { getTeachers, Teacher } from "../services/teacherService";
import { isAuthenticated } from "../services/authService";

type Reason = "question" | "exam" | "other";

const TeacherAppointmentPage: React.FC = () => {
  const navigate = useNavigate();
  
  // Token kontrolü - sayfa yüklenirken kontrol et
  useEffect(() => {
    if (!isAuthenticated()) {
      console.warn("Token bulunamadı, login sayfasına yönlendiriliyor...");
      navigate("/");
      return;
    }
  }, [navigate]);
  const [teacherId, setTeacherId] = useState<number | "">("");
  const [course, setCourse] = useState("");
  const [reason, setReason] = useState<Reason>("question");
  const [otherReason, setOtherReason] = useState("");
  const [date, setDate] = useState("");
  const [time, setTime] = useState("");
  const [note, setNote] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>("");
  const [teachers, setTeachers] = useState<Teacher[]>([]);
  const [loadingTeachers, setLoadingTeachers] = useState(false);
  
  // Randevu listesi için state'ler
  const [appointments, setAppointments] = useState<Appointment[]>([]);
  const [loadingAppointments, setLoadingAppointments] = useState(false);

  // Öğretmen listesini yükle
  useEffect(() => {
    const loadTeachers = async () => {
      setLoadingTeachers(true);
      try {
        const data = await getTeachers();
        console.log("Teachers:", data); // DEBUG: Öğretmen listesini logla
        setTeachers(data);
      } catch (err: any) {
        console.error("Öğretmenler yüklenirken hata:", err);
        // 401 hatası durumunda axios interceptor zaten yönlendirme yapacak
        // Diğer hatalar için kullanıcıya bilgi ver
        if (err?.status !== 401) {
          setError("Öğretmen listesi yüklenemedi. Lütfen sayfayı yenileyin.");
        }
      } finally {
        setLoadingTeachers(false);
      }
    };
    loadTeachers();
  }, []);

  // Randevu listesini yükle
  useEffect(() => {
    loadAppointments();
  }, []);

  const loadAppointments = async () => {
    setLoadingAppointments(true);
    try {
      const data = await getStudentAppointments();
      setAppointments(data);
    } catch (err: any) {
      console.error("Randevular yüklenirken hata:", err);
      // 401 hatası durumunda axios interceptor zaten yönlendirme yapacak
      // Diğer hatalar için sessizce devam et
      if (err?.status === 401) {
        // Token geçersiz, interceptor yönlendirecek
        return;
      }
    } finally {
      setLoadingAppointments(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      const finalReason =
        reason === "other"
          ? otherReason
          : reason === "question"
          ? "Soru sorma"
          : "Sınav kağıdına bakma";

      // Token kontrolü
      const token = localStorage.getItem("token");
      if (!token) {
        throw {
          message: "Oturum süreniz dolmuş. Lütfen tekrar giriş yapın.",
        } as ApiError;
      }

      // Saat validation: 09:00-17:00 arası ve 30 dk aralıklarla
      if (!time) {
        setError("Lütfen bir saat seçin.");
        setLoading(false);
        return;
      }

      const [hourStr, minuteStr] = time.split(":");
      const hour = parseInt(hourStr, 10);
      const minute = parseInt(minuteStr, 10);

      if (
        hour < 9 ||
        hour > 17 ||
        (hour === 17 && minute !== 0) ||
        (minute !== 0 && minute !== 30)
      ) {
        setError(
          "Lütfen 09:00 ile 17:00 arasında, 30 dakika aralıklarla bir saat seçin."
        );
        setLoading(false);
        return;
      }

      // TeacherId kontrolü
      if (teacherId === "" || typeof teacherId !== "number" || teacherId <= 0) {
        setError("Lütfen bir öğretim elemanı seçin.");
        setLoading(false);
        return;
      }

      console.log("Submitting with teacherId:", teacherId); // DEBUG: Gönderilen ID'yi logla
      console.log("Available teachers:", teachers); // DEBUG: Mevcut öğretmen listesini logla

      // Backend artık studentId'yi JWT token'dan otomatik alıyor
      // teacherId gönderiyoruz (formdan seçilen teacherId)
      // "Diğer" seçeneğinde yazılan metin reason olarak gönderiliyor (finalReason içinde)
      
      await createAppointment({
        teacherId: Number(teacherId),
        course,
        reason: finalReason, // "Diğer" seçeneğinde yazılan metin buraya gider
        date,
        time,
      });

      alert("Randevu talebiniz başarıyla oluşturuldu!");

      // Formu temizle
      setTeacherId("");
      setCourse("");
      setReason("question");
      setOtherReason("");
      setDate("");
      setTime("");
      setNote("");

      // Randevu listesini yenile
      await loadAppointments();
    } catch (err) {
      const apiError = err as ApiError;
      // Hata mesajını göster (validation hataları için)
      const errorMsg =
        apiError.message || "Randevu oluşturulurken bir hata oluştu";
      setError(errorMsg);
      console.error("Randevu oluşturma hatası:", errorMsg);
    } finally {
      setLoading(false);
    }
  };

  const isOtherSelected = reason === "other";

  const timeOptions = React.useMemo(() => {
    const list: string[] = [];
    for (let hour = 9; hour <= 17; hour++) {
      const minutes = hour === 17 ? [0] : [0, 30];
      for (const minute of minutes) {
        const hh = hour.toString().padStart(2, '0');
        const mm = minute.toString().padStart(2, '0');
        list.push(`${hh}:${mm}`);
      }
    }
    return list;
  }, []);

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

      {/* İçerik - 2 kolonlu layout */}
      <main className="flex-1 max-w-7xl mx-auto w-full px-6 py-8">
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Sol taraf - Randevu Talep Formu */}
          <div>
            <form
              onSubmit={handleSubmit}
              className="bg-white rounded-2xl shadow-md border border-slate-200 p-6 space-y-4"
            >
          <h2 className="text-xl font-semibold text-slate-900 mb-2">
            Randevu Talep Formu
          </h2>
          <p className="text-sm text-slate-600 mb-4">
            Görüşme yapmak istediğiniz öğretim elemanını, ders bilgisini ve
            uygun olduğunuz tarih/saat bilgisini giriniz.
          </p>

          {error && (
            <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
              {error}
            </div>
          )}

          {/* Öğretim elemanı adı */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">
              Öğretim Elemanı
            </label>
            {loadingTeachers ? (
              <div className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-500">
                Öğretmenler yükleniyor...
              </div>
            ) : teachers.length > 0 ? (
              <select
                value={teacherId}
                onChange={(e) => {
                  const selectedId = e.target.value === "" ? "" : Number(e.target.value);
                  console.log("Selected teacherId:", selectedId); // DEBUG: Seçilen ID'yi logla
                  setTeacherId(selectedId);
                }}
                className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                required
              >
                <option value="">Öğretim elemanı seçiniz</option>
                {teachers.map((teacher) => (
                  <option key={teacher.id} value={teacher.id}>
                    {teacher.email || teacher.name}
                  </option>
                ))}
              </select>
            ) : (
              <div className="w-full rounded-lg border border-red-300 px-3 py-2 text-sm text-red-600">
                Öğretmen listesi yüklenemedi. Lütfen sayfayı yenileyin.
              </div>
            )}
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
              <select
                value={time}
                onChange={(e) => { setTime(e.target.value); setError(""); }}
                className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                required
              >
                <option value="">Saat seçiniz</option>
                {timeOptions.map((t) => (
                  <option key={t} value={t}>
                    {t}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {/* Gönder butonu */}
          <div className="pt-2">
            <button
              type="submit"
              disabled={loading}
              className="w-full rounded-lg bg-blue-600 text-white text-sm font-medium py-2.5 hover:bg-blue-700 transition disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {loading ? "Gönderiliyor..." : "Randevu talebi gönder"}
            </button>
          </div>
        </form>
          </div>

          {/* Sağ taraf - Randevularım */}
          <div>
            <div className="bg-white rounded-2xl shadow-md border border-slate-200 p-6">
              <h2 className="text-xl font-semibold text-slate-900 mb-4">
                Randevularım
              </h2>

              {loadingAppointments ? (
                <div className="text-center py-8">
                  <p className="text-slate-500 text-sm">Yükleniyor...</p>
                </div>
              ) : (
                <div className="space-y-4">
                  {appointments.length === 0 ? (
                    <p className="text-slate-500 text-sm">
                      Henüz randevu talebiniz bulunmamaktadır.
                    </p>
                  ) : (
                    <>
                      {/* Bekleyen Randevular */}
                      {appointments.filter(apt => apt.status === "pending").length > 0 && (
                        <div className="mb-4">
                          <h3 className="text-sm font-semibold text-slate-700 mb-2">
                            Bekleyen ({appointments.filter(apt => apt.status === "pending").length})
                          </h3>
                          <div className="space-y-2">
                            {appointments
                              .filter(apt => apt.status === "pending")
                              .map((apt) => (
                                <div
                                  key={apt.id}
                                  className="border border-slate-200 rounded-lg p-3 bg-yellow-50"
                                >
                                  <p className="font-semibold text-slate-900 text-sm">
                                    {apt.course || "Ders belirtilmemiş"}
                                  </p>
                                  <p className="text-xs text-slate-600 mt-1">
                                    {apt.reason}
                                  </p>
                                  <p className="text-xs text-slate-600">
                                    {new Date(apt.date).toLocaleDateString("tr-TR")} - {apt.time}
                                  </p>
                                  <span className="inline-block mt-2 px-2 py-1 bg-yellow-100 text-yellow-800 text-xs font-semibold rounded">
                                    Beklemede
                                  </span>
                                </div>
                              ))}
                          </div>
                        </div>
                      )}

                      {/* Onaylanan Randevular */}
                      {appointments.filter(apt => apt.status === "approved").length > 0 && (
                        <div className="mb-4">
                          <h3 className="text-sm font-semibold text-slate-700 mb-2">
                            Onaylanan ({appointments.filter(apt => apt.status === "approved").length})
                          </h3>
                          <div className="space-y-2">
                            {appointments
                              .filter(apt => apt.status === "approved")
                              .map((apt) => (
                                <div
                                  key={apt.id}
                                  className="border border-slate-200 rounded-lg p-3 bg-green-50"
                                >
                                  <p className="font-semibold text-slate-900 text-sm">
                                    {apt.course || "Ders belirtilmemiş"}
                                  </p>
                                  <p className="text-xs text-slate-600 mt-1">
                                    {apt.reason}
                                  </p>
                                  <p className="text-xs text-slate-600">
                                    {new Date(apt.date).toLocaleDateString("tr-TR")} - {apt.time}
                                  </p>
                                  <span className="inline-block mt-2 px-2 py-1 bg-green-100 text-green-800 text-xs font-semibold rounded">
                                    Onaylandı
                                  </span>
                                </div>
                              ))}
                          </div>
                        </div>
                      )}

                      {/* Reddedilen Randevular */}
                      {appointments.filter(apt => apt.status === "rejected").length > 0 && (
                        <div>
                          <h3 className="text-sm font-semibold text-slate-700 mb-2">
                            Reddedilen ({appointments.filter(apt => apt.status === "rejected").length})
                          </h3>
                          <div className="space-y-2">
                            {appointments
                              .filter(apt => apt.status === "rejected")
                              .map((apt) => (
                                <div
                                  key={apt.id}
                                  className="border border-slate-200 rounded-lg p-3 bg-red-50"
                                >
                                  <p className="font-semibold text-slate-900 text-sm">
                                    {apt.course || "Ders belirtilmemiş"}
                                  </p>
                                  <p className="text-xs text-slate-600 mt-1">
                                    {apt.reason}
                                  </p>
                                  <p className="text-xs text-slate-600">
                                    {new Date(apt.date).toLocaleDateString("tr-TR")} - {apt.time}
                                  </p>
                                  {apt.rejectionReason && (
                                    <div className="mt-2 p-2 bg-red-100 border border-red-200 rounded text-xs">
                                      <p className="font-semibold text-red-800 mb-1">
                                        Reddetme Sebebi:
                                      </p>
                                      <p className="text-red-700">
                                        {apt.rejectionReason}
                                      </p>
                                    </div>
                                  )}
                                  <span className="inline-block mt-2 px-2 py-1 bg-red-100 text-red-800 text-xs font-semibold rounded">
                                    Reddedildi
                                  </span>
                                </div>
                              ))}
                          </div>
                        </div>
                      )}
                    </>
                  )}
                </div>
              )}
            </div>
          </div>
        </div>
      </main>
    </div>
  );
};

export default TeacherAppointmentPage;
