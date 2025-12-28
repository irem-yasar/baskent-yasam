-- Appointments tablosundan Note kolonunu sil

-- PostgreSQL'de Note kolonunu sil
ALTER TABLE "Appointments" 
DROP COLUMN IF EXISTS "Note";

