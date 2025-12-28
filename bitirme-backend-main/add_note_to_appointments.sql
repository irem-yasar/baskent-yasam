-- Appointments tablosuna Note kolonu ekle

-- PostgreSQL'de Note kolonunu ekle (eğer yoksa)
ALTER TABLE "Appointments" 
ADD COLUMN IF NOT EXISTS "Note" VARCHAR(500) NULL;

-- Mevcut kayıtlar için Note NULL olarak kalacak (opsiyonel alan)

