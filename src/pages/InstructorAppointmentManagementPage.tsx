import React, { useState } from "react";

const days = ["Pzt", "Sal", "Çar", "Per", "Cum"];
const times = [
  "09:00",
  "10:00",
  "11:00",
  "12:00",
  "13:00",
  "14:00",
  "15:00",
  "16:00",
];

type TabType = "requests" | "myAppointments";

const InstructorAppointmentManagement: React.FC = () => {
  const [activeTab, setActiveTab] = useState<TabType>("requests");
  const [selectedSlots, setSelectedSlots] = useState<string[]>([]);

  const toggleSlot = (key: string) => {
    setSelectedSlots((prev) =>
      prev.includes(key) ? prev.filter((k) => k !== key) : [...prev, key]
    );
  };

  return (
    <div className="min-h-screen bg-slate-50 p-8">
      <h1 className="text-2xl font-semibold mb-6">Randevu Yönetimi</h1>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* SOL TARAF */}
        <section className="lg:col-span-2 bg-white rounded-xl border shadow">
          {/* Sekmeler */}
          <div className="flex border-b">
            <button
              onClick={() => setActiveTab("requests")}
              className={`flex-1 py-3 text-sm font-medium ${
                activeTab === "requests"
                  ? "border-b-2 border-[#d71920] text-[#d71920]"
                  : "text-slate-500"
              }`}
            >
              Gelen Talepler
            </button>

            <button
              onClick={() => setActiveTab("myAppointments")}
              className={`flex-1 py-3 text-sm font-medium ${
                activeTab === "myAppointments"
                  ? "border-b-2 border-[#d71920] text-[#d71920]"
                  : "text-slate-500"
              }`}
            >
              Randevularım
            </button>
          </div>

          {/* İçerik */}
          <div className="p-6">
            {activeTab === "requests" && (
              <div className="space-y-4">
                <p className="text-slate-500 text-sm">
                  Henüz gelen randevu talebi yok.
                </p>
                {/* Backend gelince burası map ile dolacak */}
              </div>
            )}

            {activeTab === "myAppointments" && (
              <div className="space-y-4">
                <p className="text-slate-500 text-sm">
                  Onayladığınız randevular burada listelenecek.
                </p>
                {/* 
                  Backend'den:
                  WHERE status = 'approved'
                  ORDER BY scheduled_at ASC
                */}
              </div>
            )}
          </div>
        </section>

        {/* SAĞ TARAF – DERS PROGRAMI */}
        <section className="bg-white rounded-xl border p-4 shadow">
          <h2 className="text-sm font-semibold mb-3 text-center">
            Haftalık Ders Programı
          </h2>

          <div className="grid grid-cols-6 text-xs gap-1">
            <div />
            {days.map((d) => (
              <div key={d} className="text-center font-medium">
                {d}
              </div>
            ))}

            {times.map((t) => (
              <React.Fragment key={t}>
                <div className="text-right pr-1 text-[11px]">{t}</div>
                {days.map((d) => {
                  const key = `${d}-${t}`;
                  const active = selectedSlots.includes(key);

                  return (
                    <button
                      key={key}
                      onClick={() => toggleSlot(key)}
                      className={`h-6 rounded border transition
                        ${
                          active
                            ? "bg-[#d71920] border-[#d71920]"
                            : "bg-slate-100 hover:bg-slate-200"
                        }`}
                    />
                  );
                })}
              </React.Fragment>
            ))}
          </div>

          <p className="text-[11px] text-slate-500 mt-3 text-center">
            Müsait olduğunuz saatleri işaretleyin
          </p>
        </section>
      </div>
    </div>
  );
};

export default InstructorAppointmentManagement;
