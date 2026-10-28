-- ============================================================
-- Feature: Módulo de Laptops - Fase 6, Tarea 19
-- Propósito: SOLO LECTURA. Lista los modelos del grupo Laptops cuya descripción trae
--            RAM y/o almacenamiento (ej. '8ram', '8gb', '256gb', '1tb', '8GB/256GB SSD') y
--            propone, para REVISION MANUAL:
--              - descripción limpia (sin capacidades ni tipos)
--              - RAM (GB) y tipo (DDR3/DDR4/DDR5)
--              - almacenamiento (GB) y tipo (SSD SATA / SSD NVMe / HDD)
--            No modifica ninguna tabla: solo usa tablas temporales (#...) y SELECT.
--            La actualización real (renombrar modelos y crear Equipo_Configuracion para los
--            equipos existentes) se hará en un segundo script, solo tras aprobar este resultado.
--
-- Cómo interpreta la descripción (SQL Server no tiene regex, se usa un tokenizador):
--   * Separa por espacios y por / , ( ) + ; |
--   * Número + unidad pegados ('8gb', '8ram', '256ssd', '1tb') o separados ('256 gb', '8 gb ram').
--   * 1 TB = 1024 GB.
--   * EXPLICITO: la unidad dice ram/ssd/hdd/nvme o es TB.  INFERIDO: solo 'gb' y se decide por
--     el tamaño (<= 32 GB = RAM, >= 120 GB = almacenamiento, 64 GB = AMBIGUO).
--   * Solo se aceptan capacidades usuales (2, 4, 8, 16, 24, 32, 64, 128, 256, 512, 1024 ...)
--     para no confundir números de modelo (ej. 'Latitude 5400').
--   * Tipo de almacenamiento: 'nvme' -> SSD NVMe, 'ssd' -> SSD SATA (por defecto), 'hdd' -> HDD.
--
-- Estado de cada modelo:
--   COMPLETA            1 RAM y 1 almacenamiento detectados
--   SOLO_RAM            solo se detectó la RAM
--   SOLO_ALMACENAMIENTO solo se detectó el almacenamiento
--   REVISAR             más de una RAM/almacenamiento o capacidad ambigua (64 GB)
--   SIN_DATOS           no se detectó nada (se lista aparte)
--
-- Requiere: CREATE_Laptops_Fase1_Grupos.sql, CREATE_Equipo_Configuracion.sql
-- Autor: MVSystem
-- Fecha: 2026-10-28
-- ============================================================
SET NOCOUNT ON;

DECLARE @ID_Grupo_Laptops INT = (SELECT TOP 1 ID_Grupo FROM dbo.Grupos_Cata WHERE Descripcion = 'Laptops');

IF @ID_Grupo_Laptops IS NULL
    THROW 50400, 'No existe el grupo Laptops. Ejecute CREATE_Laptops_Fase1_Grupos.sql.', 1;

IF OBJECT_ID('tempdb..#Modelos') IS NOT NULL DROP TABLE #Modelos;
IF OBJECT_ID('tempdb..#Tokens')  IS NOT NULL DROP TABLE #Tokens;
IF OBJECT_ID('tempdb..#Num')     IS NOT NULL DROP TABLE #Num;
IF OBJECT_ID('tempdb..#Cand')    IS NOT NULL DROP TABLE #Cand;
IF OBJECT_ID('tempdb..#Resumen') IS NOT NULL DROP TABLE #Resumen;

-- ------------------------------------------------------------
-- 1. Modelos del grupo Laptops, con la descripción normalizada
-- ------------------------------------------------------------
SELECT M.ID_Modelo,
       Marca = Ma.Descripcion,
       Descripcion_Actual = M.Descripcion,
       Norm = CAST(LTRIM(RTRIM(
                REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(
                    ISNULL(M.Descripcion, ''),
                    '/', ' '), ',', ' '), '(', ' '), ')', ' '), '+', ' '), ';', ' '), '|', ' '), '&', ' '), '<', ' '), '>', ' ')
              )) AS VARCHAR(400))
INTO #Modelos
FROM dbo.Modelos M
INNER JOIN dbo.Marcas Ma ON Ma.ID_Marca = M.ID_Marca
WHERE M.ID_Grupo = @ID_Grupo_Laptops;

WHILE EXISTS (SELECT 1 FROM #Modelos WHERE Norm LIKE '%  %')
    UPDATE #Modelos SET Norm = REPLACE(Norm, '  ', ' ') WHERE Norm LIKE '%  %';

-- ------------------------------------------------------------
-- 2. Tokens con su posición (se conserva el texto original)
-- ------------------------------------------------------------
;WITH X AS (
    SELECT ID_Modelo, Doc = CAST('<t>' + REPLACE(Norm, ' ', '</t><t>') + '</t>' AS XML)
      FROM #Modelos
     WHERE Norm <> ''
)
SELECT X.ID_Modelo,
       Ord = T.c.value('for $i in . return count(../t[. << $i]) + 1', 'int'),
       Token = T.c.value('.', 'varchar(100)'),
       TokenL = LOWER(T.c.value('.', 'varchar(100)'))
INTO #Tokens
FROM X
CROSS APPLY X.Doc.nodes('/t') T(c);

-- Tokens que empiezan con dígito: parte numérica y sufijo ('8gb' -> 8 / 'gb')
SELECT ID_Modelo, Ord, Token, TokenL,
       Num = LEFT(TokenL, PATINDEX('%[^0-9.]%', TokenL + 'x') - 1),
       Suf = SUBSTRING(TokenL, PATINDEX('%[^0-9.]%', TokenL + 'x'), 50)
INTO #Num
FROM #Tokens
WHERE TokenL LIKE '[0-9]%';

-- ------------------------------------------------------------
-- 3. Candidatos de capacidad
-- ------------------------------------------------------------
CREATE TABLE #Cand (
    ID_Modelo INT NOT NULL,
    Ord_Ini   INT NOT NULL,
    Ord_Fin   INT NOT NULL,
    N         DECIMAL(10,2) NOT NULL,
    Es_TB     BIT NOT NULL,
    Marca_Tipo VARCHAR(10) NULL      -- 'ram' | 'ssd' | 'hdd' | 'nvme' si la unidad lo dice
);

-- 3a. Número y unidad pegados: 8gb, 8ram, 256ssd, 1tb, 512gbssd
INSERT INTO #Cand (ID_Modelo, Ord_Ini, Ord_Fin, N, Es_TB, Marca_Tipo)
SELECT a.ID_Modelo, a.Ord, a.Ord, TRY_CONVERT(DECIMAL(10,2), a.Num),
       CASE WHEN a.Suf LIKE 'tb%' THEN 1 ELSE 0 END,
       CASE WHEN a.Suf LIKE '%ram'  THEN 'ram'
            WHEN a.Suf LIKE '%ssd'  THEN 'ssd'
            WHEN a.Suf LIKE '%hdd'  THEN 'hdd'
            WHEN a.Suf LIKE '%nvme' THEN 'nvme' END
FROM #Num a
WHERE a.Suf IN ('gb','g','tb','ram','gbram','ssd','gbssd','hdd','gbhdd','nvme','gbnvme','tbssd','tbhdd','tbnvme')
  AND TRY_CONVERT(DECIMAL(10,2), a.Num) IS NOT NULL;

-- 3b. Número y unidad separados: '256 gb', '8 gb ram', '512 ssd'
INSERT INTO #Cand (ID_Modelo, Ord_Ini, Ord_Fin, N, Es_TB, Marca_Tipo)
SELECT a.ID_Modelo, a.Ord,
       CASE WHEN c.TokenL IS NOT NULL THEN a.Ord + 2 ELSE a.Ord + 1 END,
       TRY_CONVERT(DECIMAL(10,2), a.Num),
       CASE WHEN b.TokenL = 'tb' THEN 1 ELSE 0 END,
       COALESCE(c.TokenL, CASE WHEN b.TokenL IN ('ram','ssd','hdd','nvme') THEN b.TokenL END)
FROM #Num a
INNER JOIN #Tokens b
        ON b.ID_Modelo = a.ID_Modelo AND b.Ord = a.Ord + 1
       AND b.TokenL IN ('gb','g','tb','ram','ssd','hdd','nvme')
LEFT JOIN #Tokens c
       ON c.ID_Modelo = a.ID_Modelo AND c.Ord = a.Ord + 2
      AND b.TokenL IN ('gb','g','tb')
      AND c.TokenL IN ('ram','ssd','hdd','nvme')
WHERE a.Suf = ''
  AND TRY_CONVERT(DECIMAL(10,2), a.Num) IS NOT NULL;

-- Solo capacidades usuales (evita confundir números de modelo como 5400)
DELETE FROM #Cand
 WHERE CASE WHEN Es_TB = 1 THEN N * 1024 ELSE N END
       NOT IN (2, 4, 6, 8, 12, 16, 20, 24, 32, 40, 48, 64, 96, 120, 128, 240, 250, 256, 320, 480, 500, 512, 640, 750, 960, 1000, 1024, 2048, 4096);

-- Clasificación de cada candidato
SELECT K.ID_Modelo, K.Ord_Ini, K.Ord_Fin, K.Valor_GB,
       Clase = CASE WHEN K.Marca_Tipo = 'ram' THEN 'RAM'
                    WHEN K.Marca_Tipo IN ('ssd','hdd','nvme') OR K.Es_TB = 1 THEN 'ALM'
                    WHEN K.Valor_GB <= 32 THEN 'RAM'
                    WHEN K.Valor_GB >= 120 THEN 'ALM'
                    ELSE 'AMBIGUO' END,
       Metodo = CASE WHEN K.Marca_Tipo IS NOT NULL OR K.Es_TB = 1 THEN 'EXPLICITO' ELSE 'INFERIDO' END
INTO #Resumen
FROM (SELECT ID_Modelo, Ord_Ini, Ord_Fin, Es_TB, Marca_Tipo,
             Valor_GB = CAST(CASE WHEN Es_TB = 1 THEN N * 1024 ELSE N END AS INT)
        FROM #Cand) K;

-- ------------------------------------------------------------
-- 4. Propuesta por modelo
-- ------------------------------------------------------------
IF OBJECT_ID('tempdb..#Propuesta') IS NOT NULL DROP TABLE #Propuesta;
IF OBJECT_ID('tempdb..#P0') IS NOT NULL DROP TABLE #P0;

SELECT M.ID_Modelo, M.Marca, M.Descripcion_Actual,
       Descripcion_Limpia = LTRIM(RTRIM(ISNULL((
            SELECT ' ' + T.Token
              FROM #Tokens T
             WHERE T.ID_Modelo = M.ID_Modelo
               AND NOT EXISTS (SELECT 1 FROM #Resumen R
                                WHERE R.ID_Modelo = T.ID_Modelo AND T.Ord BETWEEN R.Ord_Ini AND R.Ord_Fin)
               AND T.TokenL NOT IN ('ddr3','ddr4','ddr5','lpddr3','lpddr4','lpddr4x','lpddr5',
                                    'ssd','hdd','nvme','sata','m.2','ram','emmc','pcie')
             ORDER BY T.Ord
             FOR XML PATH(''), TYPE).value('.', 'varchar(400)'), M.Descripcion_Actual))),
       Ram_Candidatos = (SELECT COUNT(*) FROM #Resumen R WHERE R.ID_Modelo = M.ID_Modelo AND R.Clase = 'RAM'),
       Alm_Candidatos = (SELECT COUNT(*) FROM #Resumen R WHERE R.ID_Modelo = M.ID_Modelo AND R.Clase = 'ALM'),
       Ambiguos       = (SELECT COUNT(*) FROM #Resumen R WHERE R.ID_Modelo = M.ID_Modelo AND R.Clase = 'AMBIGUO'),
       Ram_GB = (SELECT MAX(R.Valor_GB) FROM #Resumen R WHERE R.ID_Modelo = M.ID_Modelo AND R.Clase = 'RAM'),
       Ram_Tipo = CASE WHEN LOWER(M.Descripcion_Actual) LIKE '%ddr5%' THEN 'DDR5'
                       WHEN LOWER(M.Descripcion_Actual) LIKE '%ddr4%' THEN 'DDR4'
                       WHEN LOWER(M.Descripcion_Actual) LIKE '%ddr3%' THEN 'DDR3' END,
       Almacenamiento_GB = (SELECT MAX(R.Valor_GB) FROM #Resumen R WHERE R.ID_Modelo = M.ID_Modelo AND R.Clase = 'ALM'),
       Almacenamiento_Tipo = CASE WHEN LOWER(M.Descripcion_Actual) LIKE '%nvme%' OR LOWER(M.Descripcion_Actual) LIKE '%m.2%' THEN 'SSD NVMe'
                                  WHEN LOWER(M.Descripcion_Actual) LIKE '%ssd%' THEN 'SSD SATA'
                                  WHEN LOWER(M.Descripcion_Actual) LIKE '%hdd%' THEN 'HDD' END,
       Detalle = ISNULL(STUFF((
            SELECT '; ' + CAST(R.Valor_GB AS VARCHAR(10)) + 'GB ' + R.Clase + ' (' + LOWER(R.Metodo) + ')'
              FROM #Resumen R
             WHERE R.ID_Modelo = M.ID_Modelo
             ORDER BY R.Ord_Ini
             FOR XML PATH(''), TYPE).value('.', 'varchar(400)'), 1, 2, ''), ''),
       Equipos_Total = (SELECT COUNT(*) FROM dbo.Equipos E WHERE E.ID_Modelo = M.ID_Modelo),
       Equipos_Disponibles = (SELECT COUNT(*) FROM dbo.Equipos E WHERE E.ID_Modelo = M.ID_Modelo AND E.Disponible = 'S'),
       Equipos_Con_Configuracion = (SELECT COUNT(*) FROM dbo.Equipos E
                                      INNER JOIN dbo.Equipo_Configuracion C ON C.Imei = E.Imei
                                     WHERE E.ID_Modelo = M.ID_Modelo)
INTO #P0
FROM #Modelos M;

SELECT P.*,
       Estado = CAST(CASE WHEN P.Ram_Candidatos = 0 AND P.Alm_Candidatos = 0 AND P.Ambiguos = 0 THEN 'SIN_DATOS'
                          WHEN P.Ambiguos > 0 OR P.Ram_Candidatos > 1 OR P.Alm_Candidatos > 1 THEN 'REVISAR'
                          WHEN P.Ram_Candidatos = 1 AND P.Alm_Candidatos = 1 THEN 'COMPLETA'
                          WHEN P.Ram_Candidatos = 1 THEN 'SOLO_RAM'
                          ELSE 'SOLO_ALMACENAMIENTO' END AS VARCHAR(20))
INTO #Propuesta
FROM #P0 P;

-- ------------------------------------------------------------
-- 5. Resultados
-- ------------------------------------------------------------
-- 5a. Resumen por estado
SELECT Estado, Modelos = COUNT(*), Equipos = SUM(Equipos_Total), Equipos_Sin_Configuracion = SUM(Equipos_Total - Equipos_Con_Configuracion)
  FROM #Propuesta
 GROUP BY Estado
 ORDER BY Estado;

-- 5b. Propuesta (para revisar). Los de mayor impacto primero.
SELECT ID_Modelo, Marca, Descripcion_Actual, Descripcion_Limpia,
       Ram_GB, Ram_Tipo, Almacenamiento_GB, Almacenamiento_Tipo,
       Estado, Detalle, Equipos_Total, Equipos_Disponibles, Equipos_Con_Configuracion
  FROM #Propuesta
 WHERE Estado <> 'SIN_DATOS'
 ORDER BY CASE Estado WHEN 'REVISAR' THEN 1 WHEN 'SOLO_RAM' THEN 2 WHEN 'SOLO_ALMACENAMIENTO' THEN 3 ELSE 4 END,
          Marca, Descripcion_Actual;

-- 5c. Modelos del grupo Laptops sin capacidades en la descripción (nada que migrar)
SELECT ID_Modelo, Marca, Descripcion_Actual, Equipos_Total, Equipos_Disponibles
  FROM #Propuesta
 WHERE Estado = 'SIN_DATOS'
 ORDER BY Marca, Descripcion_Actual;

DROP TABLE #Propuesta;
DROP TABLE #P0;
DROP TABLE #Resumen;
DROP TABLE #Cand;
DROP TABLE #Num;
DROP TABLE #Tokens;
DROP TABLE #Modelos;
GO
