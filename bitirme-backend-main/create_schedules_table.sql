-- Schedules tablosunu oluştur

CREATE TABLE IF NOT EXISTS "Schedules" (
    "id" SERIAL PRIMARY KEY,
    "teacher_id" INTEGER NOT NULL,
    "day" VARCHAR(10) NOT NULL,
    "time_slot" VARCHAR(20) NOT NULL,
    "created_at" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updated_at" TIMESTAMP WITHOUT TIME ZONE,
    CONSTRAINT "FK_Schedules_users_teacher_id" FOREIGN KEY ("teacher_id") REFERENCES "users" ("id") ON DELETE CASCADE
);

-- Index ekle (performans için)
CREATE INDEX IF NOT EXISTS "IX_Schedules_teacher_id" ON "Schedules" ("teacher_id");

