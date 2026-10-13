/*
    Cambio preparado (no ejecutar sin validar en el ambiente correspondiente).
    Resuelve LiteralF desde la cabecera de la misma cotización y sucursal,
    en lugar de usar una cabecera con Numero_Factura fijo = 2.

    La API debe enviar @ID_Sucursal en la llamada a Detalle_Cotizacion_Insert.
*/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[Detalle_Cotizacion_Insert]
    @Numero_Factura int,
    @ID_Sucursal int,
    @ID_Equipo varchar(30),
    @Descripcion varchar(100),
    @ID_Tipo int,
    @ID_Garantia int,
    @ID_Tipo_Factura int,
    @ID_Vendedor int,
    @SubTotal money,
    @cantidad int = null,
    @precio money = null,
    @Descuento money = null,
    @Itbis money = null,
    @Total money = null,
    @Estado char(1),
    @Usuario varchar(50),
    @fecha_registro datetime = null
AS
BEGIN
    IF @fecha_registro IS NULL
    BEGIN
        SET @fecha_registro = DATEADD(hour, -4, GETDATE());
    END;

    DECLARE @literal char(1);
    DECLARE @numero_factura_literal varchar(20);

    SELECT TOP (1)
        @literal = c.Literal
    FROM dbo.Cotizacion AS c
    WHERE c.Numero_Factura = @Numero_Factura
      AND c.ID_Sucursal = @ID_Sucursal
      AND c.Estado = 'A'
      AND LTRIM(RTRIM(c.Usuario)) = LTRIM(RTRIM(@Usuario))
    ORDER BY c.Codigo DESC;

    IF @literal IS NULL
    BEGIN
        RAISERROR('No se encontró la cabecera activa de la cotización para la sucursal indicada.', 16, 1);
        RETURN;
    END;

    SET @numero_factura_literal = RTRIM(@literal) + CONVERT(varchar(10), @Numero_Factura);

    IF @ID_Tipo = 1
    BEGIN
        INSERT INTO dbo.Equipos_Historico
        (
            Imei, ID_Modelo, ID_Tecnologia, ID_Garantia, ID_Condicion,
            ID_Estado_Bloqueo, ID_Almacen, ID_Suplidor, Numero_de_Factura,
            Precio_Por_Mayor, Precio_Detalle, Costo_Equipo, Desbloqueado,
            Disponible, Nota_Adicional, Nota_Historico, Usuario, Fecha_registro,
            Numero_Invetario, Numero_Registro, Usuario_Historico, Fecha_registro_Historico
        )
        SELECT
            Imei, ID_Modelo, ID_Tecnologia, ID_Garantia, ID_Condicion,
            ID_Estado_Bloqueo, ID_Almacen, ID_Suplidor, Numero_de_Factura,
            Precio_Por_Mayor, Precio_Detalle, Costo_Equipo, Desbloqueado,
            Disponible, Nota_Adicional,
            'Cotización # ' + CONVERT(varchar(50), @Numero_Factura),
            Usuario, Fecha_registro, Numero_Invetario, Numero_Registro,
            @Usuario, @fecha_registro
        FROM dbo.Equipos WITH (NOLOCK)
        WHERE Imei = @ID_Equipo;

        INSERT INTO dbo.Detalle_Cotizacion
        (
            Numero_Factura, ID_Equipo, Descripcion, ID_Tipo, ID_Garantia,
            cantidad, precio, SubTotal, Descuento, literalF, Itbis, Total,
            Estado, Usuario, Fecha_registro
        )
        VALUES
        (
            @Numero_Factura, @ID_Equipo, @Descripcion, @ID_Tipo, @ID_Garantia,
            @cantidad, @precio, @SubTotal, @Descuento, @numero_factura_literal,
            @Itbis, @Total, @Estado, @Usuario, @fecha_registro
        );

        RETURN;
    END;

    IF @ID_Tipo = 2
    BEGIN
        INSERT INTO dbo.Accesorios_Historico
        (
            ID_Accesorio, Imei_Equipo, ID_Modelo, ID_Almacen, Numero_de_Factura,
            Descripcion_Accesorio, Cantidad, Can_Max, Can_Min, Precio_Por_Mayor,
            Precio_Detalle, Costo_Equipo, Disponible, Disponible_Detalle, Estado,
            Usuario, Fecha_registro, Numero_Invetario, Codigo_Barra,
            Estado_Historico, Usuario_Historico, Fecha_registro_Historico
        )
        SELECT
            ID_Accesorio, Imei_Equipo, ID_Modelo, ID_Almacen, Numero_de_Factura,
            Descripcion_Accesorio, Cantidad, Can_Max, Can_Min, Precio_Por_Mayor,
            Precio_Detalle, Costo_Equipo, Disponible, Disponible_Detalle, Estado,
            Usuario, Fecha_registro, Numero_Invetario, Codigo_Barra,
            'A', @Usuario, DATEADD(hour, -4, GETDATE())
        FROM dbo.Accesorios
        WHERE ID_Accesorio = @ID_Equipo;

        INSERT INTO dbo.Detalle_Cotizacion
        (
            Numero_Factura, ID_Equipo, Descripcion, ID_Tipo, ID_Garantia,
            cantidad, precio, SubTotal, Descuento, literalF, Itbis, Total,
            Estado, Usuario, Fecha_registro
        )
        VALUES
        (
            @Numero_Factura, @ID_Equipo, @Descripcion, @ID_Tipo, @ID_Garantia,
            @cantidad, @precio, @SubTotal, @Descuento, @numero_factura_literal,
            @Itbis, @Total, @Estado, @Usuario, @fecha_registro
        );
    END;
END;
GO
