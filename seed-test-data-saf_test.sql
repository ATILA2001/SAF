/* ============================================================================
   SAF — DATOS DE PRUEBA para la base SAF_TEST
   ----------------------------------------------------------------------------
   Siembra filas de test en las tablas PROPIAS de SAF para poder probar los
   cruces de las vistas PAGOS y STATUS CONTABILIDAD.

   ⚠ CORRE SOLO EN SAF_TEST. No toca IVC (prod, solo lectura).
   ⚠ Las columnas que se DERIVAN de IVC no se pueden sembrar desde acá:
       - BUZÓN SADE / FECHA SADE  ← IVC.dbo.PASES_SADE
       - FECHA DE PAGO NO CAF     ← IVC.dbo.SIGAF_OP
     Con estos expedientes ficticios (9000000x/26) esas columnas quedan vacías
     en Pagos: es lo esperado (no existen en la bajada de IVC).

   Re-ejecutable: borra primero las filas de prueba y las vuelve a insertar.
   Todo lo de prueba usa tipo_dev = 'TST' y expedientes 9000000x/26.
   ============================================================================ */
USE [SAF_TEST];
SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRAN;

    /* ---- 0) Limpieza de corridas anteriores -------------------------------- */
    DELETE FROM StatusContabilidadExtras WHERE tipo_dev = 'TST';
    DELETE FROM DevengadosExtra          WHERE tipo_dev = 'TST';
    DELETE FROM Devengados               WHERE tipo_dev = 'TST';
    DELETE FROM ExpedientesCaf    WHERE expediente_financiera IN
        ('90000001/26','90000002/26','90000003/26','90000004/26','90000005/26');
    DELETE FROM ExpedientesSeguro WHERE expediente_financiera IN
        ('90000001/26','90000002/26','90000003/26','90000004/26','90000005/26');

    /* ---- 1) Lookups: tramitadores (NO vienen sembrados por migración) ------ */
    IF NOT EXISTS (SELECT 1 FROM TramitadoresCuentasPagar WHERE nombre = N'CP Ana (TEST)')
        INSERT INTO TramitadoresCuentasPagar (nombre, orden, activo) VALUES (N'CP Ana (TEST)', 1, 1);
    IF NOT EXISTS (SELECT 1 FROM TramitadoresCuentasPagar WHERE nombre = N'CP Beto (TEST)')
        INSERT INTO TramitadoresCuentasPagar (nombre, orden, activo) VALUES (N'CP Beto (TEST)', 2, 1);
    IF NOT EXISTS (SELECT 1 FROM TramitadoresLiquidaciones WHERE nombre = N'Liq Diego (TEST)')
        INSERT INTO TramitadoresLiquidaciones (nombre, orden, activo) VALUES (N'Liq Diego (TEST)', 1, 1);
    IF NOT EXISTS (SELECT 1 FROM TramitadoresLiquidaciones WHERE nombre = N'Liq Elena (TEST)')
        INSERT INTO TramitadoresLiquidaciones (nombre, orden, activo) VALUES (N'Liq Elena (TEST)', 2, 1);

    /* ---- IDs de lookups a variables (robusto ante cualquier identity) ------ */
    DECLARE @dgAvanzar     int = (SELECT id FROM StatusDgayfOpciones    WHERE nombre = N'avanzar');
    DECLARE @dgAvanzarCaf  int = (SELECT id FROM StatusDgayfOpciones    WHERE nombre = N'avanzar CAF');
    DECLARE @dgNoAvanzar   int = (SELECT id FROM StatusDgayfOpciones    WHERE nombre = N'no avanzar');
    DECLARE @opFirmada     int = (SELECT id FROM StatusOpOpciones       WHERE nombre = N'OP Firmada');
    DECLARE @opBono        int = (SELECT id FROM StatusOpOpciones       WHERE nombre = N'Pasado al pago BONO');
    DECLARE @opAnulado     int = (SELECT id FROM StatusOpOpciones       WHERE nombre = N'anulado');
    DECLARE @scFacturaPed  int = (SELECT id FROM StatusContableOpciones WHERE nombre = N'Factura Pedida');
    DECLARE @scLiquid      int = (SELECT id FROM StatusContableOpciones WHERE nombre = N'Liquidaciones');
    DECLARE @scSeguros     int = (SELECT id FROM StatusContableOpciones WHERE nombre = N'Seguros');
    DECLARE @scPresupuesto int = (SELECT id FROM StatusContableOpciones WHERE nombre = N'Presupuesto');
    DECLARE @tcpAna    int = (SELECT id FROM TramitadoresCuentasPagar   WHERE nombre = N'CP Ana (TEST)');
    DECLARE @tcpBeto   int = (SELECT id FROM TramitadoresCuentasPagar   WHERE nombre = N'CP Beto (TEST)');
    DECLARE @tliqDiego int = (SELECT id FROM TramitadoresLiquidaciones  WHERE nombre = N'Liq Diego (TEST)');
    DECLARE @tliqElena int = (SELECT id FROM TramitadoresLiquidaciones  WHERE nombre = N'Liq Elena (TEST)');

    DECLARE @now datetime2 = SYSUTCDATETIME();

    /* ---- 2) Devengados (tabla ancla; clave de negocio tipo_dev + nro_dev) --- */
    INSERT INTO Devengados (tipo_dev, nro_dev, fecha_imputacion, expediente, empresa, importe_pp, fecha_importacion) VALUES
        ('TST', 900001, '2026-07-01', '90000001/26', N'COOP TEST UNO',   1000000.00, @now),
        ('TST', 900002, '2026-07-02', '90000002/26', N'COOP TEST DOS',   2000000.00, @now),
        ('TST', 900003, '2026-07-03', '90000003/26', N'FEMYP TEST SRL',  3000000.00, @now),
        ('TST', 900004, '2026-07-04', '90000004/26', N'PRINT TEST SRL',  4000000.00, @now),
        ('TST', 900005, '2026-07-05', '90000005/26', N'COOP TEST CINCO', 5000000.00, @now);

    /* ---- 3) DevengadosExtra (datos propios de PAGOS, uno por devengado) -----
       status_op 'OP Firmada'/'Pasado al pago BONO' -> Status Contable = 'OP Lista'.
       buzon_sade acá es la copia local (la vista Status Contabilidad la lee;
       la vista Pagos NO: Pagos usa el buzón EN VIVO de IVC.PASES_SADE).           */
    INSERT INTO DevengadosExtra
        (tipo_dev, nro_dev, status_dgayf_opcion_id, status_op_opcion_id, fecha_firma_op,
         observaciones, ccoo, fecha_ccoo, fecha_notificacion, fecha_sade, buzon_sade, caf_si_no,
         fecha_creacion, fecha_modificacion) VALUES
        ('TST',900001,@dgAvanzar,    @opFirmada,'2026-07-06',N'Obs pago uno',   'CCOO-0001','2026-07-06','2026-07-07','2026-07-05',N'TESORERIA (TEST)',    0,@now,@now),
        ('TST',900002,@dgAvanzarCaf, @opBono,   '2026-07-08',N'Obs pago dos',   'CCOO-0002','2026-07-08','2026-07-09','2026-07-07',N'DGCYC (TEST)',        1,@now,@now),
        ('TST',900003,@dgNoAvanzar,  @opAnulado,NULL,        N'Obs pago tres',  'CCOO-0003',NULL,        NULL,        NULL,        N'LIQUIDACIONES (TEST)',0,@now,@now),
        ('TST',900004,@dgAvanzarCaf, @opFirmada,'2026-07-05',N'Obs pago cuatro','CCOO-0004','2026-07-05','2026-07-06',NULL,        NULL,                   1,@now,@now),
        ('TST',900005,@dgAvanzar,    NULL,      NULL,        N'Obs pago cinco', NULL,       NULL,        NULL,        NULL,        NULL,                   0,@now,@now);

    /* ---- 4) StatusContabilidadExtras (datos propios de STATUS CONTABILIDAD) - */
    INSERT INTO StatusContabilidadExtras
        (tipo_dev, nro_dev, fecha_pedido_factura2, reiterar_pedido_factura3, fecha_ingreso_factura,
         status_contable_opcion_id, observaciones_cuentas_pagar, tramitador_cuentas_pagar_opcion_id,
         tramitador_liquidaciones_opcion_id, observaciones_liquidaciones, falta_poliza,
         ultimo_movimiento_sade, fecha_creacion, fecha_modificacion) VALUES
        ('TST',900001,'2026-07-10','2026-07-15','2026-07-18',@scFacturaPed, N'Ctas a pagar uno',  @tcpAna, @tliqDiego,N'Liquidaciones uno',0,'2026-07-16',@now,@now),
        ('TST',900002,'2026-07-11','2026-07-16',NULL,        @scLiquid,     N'Ctas a pagar dos',  @tcpBeto,@tliqElena,N'Liquidaciones dos',1,'2026-07-12',@now,@now),
        ('TST',900003,NULL,        NULL,        NULL,        @scSeguros,    N'Ctas a pagar tres', @tcpAna, @tliqElena,NULL,                0,'2026-06-30',@now,@now),
        ('TST',900004,'2026-07-09',NULL,        '2026-07-19',@scPresupuesto,NULL,                 NULL,    NULL,      NULL,                1,NULL,        @now,@now),
        ('TST',900005,NULL,        NULL,        NULL,        NULL,          NULL,                 NULL,    NULL,      NULL,                0,NULL,        @now,@now);

    /* ---- 5) ExpedientesCaf (FECHA DE PAGO CAF) ------------------------------
       Cruza por expediente_financiera = Devengado.Expediente.
       En Pagos SOLO se muestra cuando StatusDGAyF = 'avanzar CAF'.               */
    INSERT INTO ExpedientesCaf
        (anio, beneficiario, cargado, cc_pagadora, cuenta, expediente, expediente_financiera,
         fecha_creacion, fecha_modificacion, fecha_pago, iibb, importe_neto, op, pase, revisado) VALUES
        -- dev 900002 (avanzar CAF): DOS filas para probar MAX(fecha_pago) => 2026-07-20
        (2026,N'COOP TEST DOS','2026-07-17',N'CC PAGADORA TEST','CTA-0002','EX-2026-90000002-GCABA-IVC','90000002/26',@now,@now,'2026-07-18', 50000.00,1950000.00,'OP-CAF-0002A','PASE-0002A','2026-07-17'),
        (2026,N'COOP TEST DOS','2026-07-19',N'CC PAGADORA TEST','CTA-0002','EX-2026-90000002-GCABA-IVC','90000002/26',@now,@now,'2026-07-20', 50000.00,1950000.00,'OP-CAF-0002B','PASE-0002B','2026-07-19'),
        -- dev 900004 (avanzar CAF): una fila
        (2026,N'PRINT TEST SRL','2026-07-20',N'CC PAGADORA TEST','CTA-0004','EX-2026-90000004-GCABA-IVC','90000004/26',@now,@now,'2026-07-21',100000.00,3900000.00,'OP-CAF-0004','PASE-0004','2026-07-20'),
        -- dev 900001 (avanzar, NO es CAF): fila puesta a propósito; NO debe verse en Pagos
        (2026,N'COOP TEST UNO','2026-07-15',N'CC PAGADORA TEST','CTA-0001','EX-2026-90000001-GCABA-IVC','90000001/26',@now,@now,'2026-07-16', 30000.00, 970000.00,'OP-CAF-0001','PASE-0001','2026-07-15');

    /* ---- 6) ExpedientesSeguro (SEGUROS TESO) --------------------------------
       Cruza por expediente_financiera = Devengado.Expediente. Requiere 'seguro'. */
    INSERT INTO ExpedientesSeguro
        (beneficiario, estado, expediente, expediente_financiera, fecha_creacion, fecha_modificacion,
         importe_neto, op, seguro) VALUES
        (N'COOP TEST UNO',  N'Vigente',  'EX-2026-90000001-GCABA-IVC','90000001/26',@now,@now,1000000.00,'OP-SEG-0001',N'Poliza 123-TEST'),
        (N'FEMYP TEST SRL', N'Pendiente','EX-2026-90000003-GCABA-IVC','90000003/26',@now,@now,3000000.00,'OP-SEG-0003',N'Poliza 456-TEST'),
        (N'COOP TEST CINCO',N'Vigente',  'EX-2026-90000005-GCABA-IVC','90000005/26',@now,@now,5000000.00,'OP-SEG-0005',N'Poliza 789-TEST');

    COMMIT TRAN;
    PRINT 'OK: datos de prueba insertados en SAF_TEST (tipo_dev = TST).';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    THROW;
END CATCH;

/* ============================================================================
   VERIFICACIÓN RÁPIDA (opcional): descomentá para ver los cruces resueltos.
   ----------------------------------------------------------------------------
SELECT d.tipo_dev, d.nro_dev, d.expediente, d.empresa, d.importe_pp,
       dg.nombre  AS status_dgayf,
       op.nombre  AS status_op,
       de.buzon_sade,
       sc.nombre  AS status_contable_tablero,
       tcp.nombre AS tramitador_ctas_pagar,
       tl.nombre  AS tramitador_liquidaciones,
       caf.fecha_pago_caf,
       seg.seguro AS seguros_teso
FROM Devengados d
LEFT JOIN DevengadosExtra de           ON de.tipo_dev = d.tipo_dev AND de.nro_dev = d.nro_dev
LEFT JOIN StatusDgayfOpciones dg       ON dg.id = de.status_dgayf_opcion_id
LEFT JOIN StatusOpOpciones op          ON op.id = de.status_op_opcion_id
LEFT JOIN StatusContabilidadExtras sce ON sce.tipo_dev = d.tipo_dev AND sce.nro_dev = d.nro_dev
LEFT JOIN StatusContableOpciones sc    ON sc.id = sce.status_contable_opcion_id
LEFT JOIN TramitadoresCuentasPagar tcp ON tcp.id = sce.tramitador_cuentas_pagar_opcion_id
LEFT JOIN TramitadoresLiquidaciones tl ON tl.id = sce.tramitador_liquidaciones_opcion_id
LEFT JOIN (SELECT expediente_financiera, MAX(fecha_pago) AS fecha_pago_caf
             FROM ExpedientesCaf GROUP BY expediente_financiera) caf
       ON caf.expediente_financiera = d.expediente
LEFT JOIN ExpedientesSeguro seg        ON seg.expediente_financiera = d.expediente
WHERE d.tipo_dev = 'TST'
ORDER BY d.nro_dev;
   ============================================================================ */
