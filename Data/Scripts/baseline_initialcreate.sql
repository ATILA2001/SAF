-- ─────────────────────────────────────────────────────────────────────────────
-- Baseline de migraciones para la base de SAF (DefaultConnection).
-- Correr SOLO si las tablas ya existen pero __EFMigrationsHistory no registra
-- la migración InitialCreate (síntoma: 'database update' intenta CREATE TABLE
-- [Devengados] y falla con "Ya hay un objeto con el nombre 'Devengados'").
--
-- Marca InitialCreate como aplicada; luego 'dotnet ef database update' corre
-- únicamente la migración DropFechaDePagoNoCaf.
--
-- ⚠ Ejecutar contra la base de SAF (DefaultConnection), NO contra IVC_TEST.
-- ─────────────────────────────────────────────────────────────────────────────

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId]    nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32)  NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory]
               WHERE [MigrationId] = N'20260623234330_InitialCreate')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260623234330_InitialCreate', N'10.0.8');
GO
