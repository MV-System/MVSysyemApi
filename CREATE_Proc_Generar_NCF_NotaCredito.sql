/*
    Proc_Generar_NCF_NotaCredito

    Genera y reserva de forma atómica el siguiente e-NCF para un tipo de comprobante
    (usado para Notas de Crédito, TIPO_NCF = 34), SIN insertar ni depender de la tabla
    Facturas ni de su trigger Trg_Genera_NCF.

    Replica la misma lógica de secuencia/estructura que Proc_Generar_NCF, pero usando
    un UPDATE ... OUTPUT atómico para eliminar la condición de carrera que existe en el
    patrón original (SELECT con nolock seguido de UPDATE separado).
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[Proc_Generar_NCF_NotaCredito]
    @Tipo_NCF int
AS
SET NOCOUNT ON

DECLARE @Resultado TABLE (Secuencia int, NFCTipoEstructura varchar(20))

UPDATE TIPO_NFC
SET NFCTipoSecuencia = isnull(NFCTipoSecuencia,0) + 1
OUTPUT inserted.NFCTipoSecuencia, inserted.NFCTipoEstructura
INTO @Resultado
WHERE NFCTipoNumero = @Tipo_NCF

DECLARE @Secuencia int, @Estructura varchar(20), @NCF varchar(20)
SELECT @Secuencia = Secuencia, @Estructura = NFCTipoEstructura FROM @Resultado
SET @NCF = @Estructura + RIGHT('0000000000' + convert(varchar, @Secuencia), 10)

SELECT @NCF as NCF, @Secuencia as Secuencia
GO
