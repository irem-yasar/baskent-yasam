-- Appointments tablosunu oluştur (UserId/UserId1 kolonları OLMADAN)

-- Önce mevcut tabloyu sil (eğer varsa)
DROP TABLE IF EXISTS "Appointments" CASCADE;

-- Appointments tablosunu oluştur
CREATE TABLE "Appointments" (
    "Id" SERIAL PRIMARY KEY,
    "StudentId" INTEGER NOT NULL,
    "TeacherId" INTEGER NOT NULL,
    "Date" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "Time" INTERVAL NOT NULL,
    "Subject" VARCHAR(200) NOT NULL,
    "RequestReason" VARCHAR(500),
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Pending',
    "RejectionReason" VARCHAR(500),
    "CreatedAt" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITHOUT TIME ZONE,
    
    -- Foreign Key Constraints
    CONSTRAINT "FK_Appointments_Users_StudentId" 
        FOREIGN KEY ("StudentId") 
        REFERENCES "users"("id") 
        ON DELETE RESTRICT,
    
    CONSTRAINT "FK_Appointments_Users_TeacherId" 
        FOREIGN KEY ("TeacherId") 
        REFERENCES "users"("id") 
        ON DELETE RESTRICT
);

-- Index'ler
CREATE INDEX "IX_Appointments_StudentId" ON "Appointments"("StudentId");
CREATE INDEX "IX_Appointments_TeacherId" ON "Appointments"("TeacherId");
CREATE INDEX "IX_Appointments_Date" ON "Appointments"("Date");

