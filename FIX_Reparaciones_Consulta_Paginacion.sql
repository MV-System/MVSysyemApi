-- FIX: totalRecords y lastLine devolvían solo el tamaño de la página actual
-- porque COUNT(*) OVER() se calculaba DESPUÉS del filtro de paginación.
-- La solución es mover el COUNT al CTE, igual que Proc_Facturas_Consulta.

ALTER PROCEDURE [dbo].[Reparaciones_Consulta]
(
    @ID_Reparacion INT = 0,
    @Criterio NVARCHAR(100) = '',
    @Estatus INT = 0,
    @FechaInicio DATE = NULL,
    @FechaFinal DATE = NULL,
    @PageIndex INT = 1,
    @PageSize INT = 9999
)
AS
BEGIN
    SET NOCOUNT ON;

    -- DETALLE
    IF @ID_Reparacion > 0
    BEGIN
        SELECT 
            R.ID_Reparacion         AS idReparacion,
            R.Fecha_registro        AS fechaRegistro,
            Co.Nombres + ' ' + Co.Apellidos AS nombreCliente,
            T.Numero_Telefono       AS telefonoCliente,
            ET.Descripcion_Estado_Taller AS estatus,
            R.Precio                AS precio,
            R.Costo                 AS costo,
            R.Fecha_Entrega         AS fechaEntrega,
            R.Detalle_Reparacion,
            R.Observaciones,
            R.Usuario
        FROM Reparaciones R
            INNER JOIN Clientes C       ON C.ID_Cliente    = R.ID_Cliente
            INNER JOIN Contactos Co     ON Co.ID_Contacto  = C.ID_Contacto
            INNER JOIN Telefonos T      ON T.ID_Contacto   = Co.ID_Contacto
            INNER JOIN Estados_Taller_Cata ET ON ET.ID_Estado_Taller = R.ID_Estado_Taller
        WHERE R.ID_Reparacion = @ID_Reparacion
          AND R.estado = 'A';

        RETURN;
    END

    -----------------------------------
    -- LISTA PAGINADA
    -----------------------------------

    ;WITH CTE AS
    (
        SELECT 
            R.ID_Reparacion         AS idReparacion,
            R.Fecha_registro        AS fechaRegistro,
            Co.Nombres + ' ' + Co.Apellidos AS nombreCliente,
            T.Numero_Telefono       AS telefonoCliente,
            ET.Descripcion_Estado_Taller AS estatus,
            R.Precio                AS precio,
            R.Costo                 AS costo,
            R.Fecha_Entrega         AS fechaEntrega,
            R.Detalle_Reparacion,
            R.Observaciones,
            R.Usuario,
            ROW_NUMBER() OVER (ORDER BY R.ID_Reparacion DESC) AS line,
            -- FIX: COUNT antes del filtro de página, así refleja el total real
            COUNT(*) OVER ()        AS totalRecords
        FROM Reparaciones R
            INNER JOIN Clientes C       ON C.ID_Cliente    = R.ID_Cliente
            INNER JOIN Contactos Co     ON Co.ID_Contacto  = C.ID_Contacto
            INNER JOIN Telefonos T      ON T.ID_Contacto   = Co.ID_Contacto
            INNER JOIN Estados_Taller_Cata ET ON ET.ID_Estado_Taller = R.ID_Estado_Taller
        WHERE
            (
                @Criterio = '' OR 
                Co.Nombres        LIKE '%' + @Criterio + '%' OR
                Co.Apellidos      LIKE '%' + @Criterio + '%' OR
                T.Numero_Telefono LIKE '%' + @Criterio + '%' OR
                CONVERT(NVARCHAR(20), R.ID_Reparacion) LIKE '%' + @Criterio + '%'
            )
            AND (@Estatus    = 0    OR R.ID_Estado_Taller = @Estatus)
            AND (@FechaInicio IS NULL OR CONVERT(date, R.Fecha_registro) >= @FechaInicio)
            AND (@FechaFinal  IS NULL OR CONVERT(date, R.Fecha_registro) <= @FechaFinal)
            AND R.estado = 'A'
    )
    SELECT 
        idReparacion,
        fechaRegistro,
        nombreCliente,
        telefonoCliente,
        estatus,
        precio,
        costo,
        fechaEntrega,
        Detalle_Reparacion,
        Observaciones,
        Usuario,
        totalRecords,
        line,
        -- FIX: lastLine = fin real de la página, no puede superar totalRecords
        CASE 
            WHEN totalRecords < (@PageIndex * @PageSize) THEN totalRecords
            ELSE (@PageIndex * @PageSize)
        END AS lastLine
    FROM CTE
    WHERE line BETWEEN ((@PageIndex - 1) * @PageSize + 1) AND (@PageIndex * @PageSize)
    ORDER BY line;

END
