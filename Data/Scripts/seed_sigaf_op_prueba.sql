-- ─────────────────────────────────────────────────────────────────────────────
-- Carga de PRUEBA de dbo.SIGAF_OP (base IVC_TEST) para validar en SAF el cruce
-- de la columna "Fecha Pago No CAF" (derivada = MAX(FECHA_PAGO) por expediente).
--
-- Genera una OP "pagada" por cada expediente de la ÚLTIMA fecha de DEVENGADOS
-- (el mismo conjunto que SAF importa al Sincronizar), así el cruce ilumina 1:1.
-- Para ~la mitad agrega una segunda fila con fecha más nueva, para comprobar que
-- SAF toma el MAX(FECHA_PAGO).
--
-- Idempotente: borra solo las filas marcadas USUARIO = 'CARGA_PRUEBA' y reinserta.
-- Para limpiar todo:  DELETE FROM dbo.SIGAF_OP WHERE USUARIO = 'CARGA_PRUEBA';
-- ─────────────────────────────────────────────────────────────────────────────
USE IVC_TEST;
GO

SET NOCOUNT ON;

DELETE FROM dbo.SIGAF_OP WHERE USUARIO = 'CARGA_PRUEBA';

DECLARE @ultFecha DATE = (SELECT MAX(CAST(FECHA_IMPUTACION AS DATE)) FROM dbo.DEVENGADOS);

-- Expedientes de la última fecha, con el mismo filtro que usa el Sync de SAF.
IF OBJECT_ID('tempdb..#exp') IS NOT NULL DROP TABLE #exp;
SELECT DISTINCT LTRIM(RTRIM(EE_FINANCIERA)) AS EE_FINANCIERA
INTO #exp
FROM dbo.DEVENGADOS
WHERE CAST(FECHA_IMPUTACION AS DATE) = @ultFecha
  AND TIPO_DEV NOT IN ('C55', 'CPS')
  AND IMPORTE_PP > 0
  AND EE_FINANCIERA IS NOT NULL
  AND LTRIM(RTRIM(EE_FINANCIERA)) <> '';

-- Fila base: una OP pagada por expediente, fecha "vieja" (hoy - 10 días).
INSERT INTO dbo.SIGAF_OP (EE_FINANCIERA, FECHA_PAGO, ESTADO_PAGO, IMP_NETO, M_PAGO, USUARIO)
SELECT EE_FINANCIERA, DATEADD(DAY, -10, CAST(GETDATE() AS DATE)), 'C', 100000.00, 'NOTA', 'CARGA_PRUEBA'
FROM #exp;

-- Segunda fila para ~la mitad: fecha MÁS NUEVA (hoy) → valida que SAF tome el MAX.
INSERT INTO dbo.SIGAF_OP (EE_FINANCIERA, FECHA_PAGO, ESTADO_PAGO, IMP_RET, M_PAGO, USUARIO)
SELECT EE_FINANCIERA, CAST(GETDATE() AS DATE), 'C', 5000.00, 'BANCO', 'CARGA_PRUEBA'
FROM #exp
WHERE ABS(CHECKSUM(EE_FINANCIERA)) % 2 = 0;

DROP TABLE #exp;

-- Control de lo cargado.
SELECT
    (SELECT COUNT(*)                    FROM dbo.SIGAF_OP WHERE USUARIO = 'CARGA_PRUEBA') AS filas_insertadas,
    (SELECT COUNT(DISTINCT EE_FINANCIERA) FROM dbo.SIGAF_OP WHERE USUARIO = 'CARGA_PRUEBA') AS expedientes,
    @ultFecha AS ultima_fecha_devengados;
GO
