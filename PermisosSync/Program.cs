using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== SINCRONIZACIÓN DE PERMISOS: EMPRESAS -> SEGURIDAD ===\n");

        // Conexiones
        string connectionEmpresa = "Server=cellcontrolserver.database.windows.net;Database=TECNO_STYLE;User Id=cellcontroladmin;Password=Laserpiente04;";
        string connectionSeguridad = "Server=cellcontroldesarrolloserver.database.windows.net;Database=SEGURIDAD;User Id=cellcontroldesarrollouser;Password=Laserpiente04;";

        try
        {
            // 0. Validar estructura de tablas
            Console.WriteLine("🔍 Validando estructura de tablas...\n");
            await ValidarEstructuraTablas(connectionEmpresa, connectionSeguridad);
            
            // 0b. Detectar tabla de Usuarios en SEGURIDAD
            Console.WriteLine("\n🔍 Detectando tabla de Usuarios en SEGURIDAD...");
            string tablaUsuariosSeguridad = await DetectarTablaUsuarios(connectionSeguridad);
            Console.WriteLine($"✅ Tabla de Usuarios encontrada: {tablaUsuariosSeguridad}\n");

            // 1. Obtener permisos de la empresa
            Console.WriteLine("📥 Leyendo permisos desde empresa...");
            var permisosEmpresa = await ObtenerPermisosDeEmpresa(connectionEmpresa, "TECNO_STYLE");
            Console.WriteLine($"✅ Se encontraron {permisosEmpresa.Count} relaciones usuario-permiso\n");

            // 2. Mapear usuarios por Login
            Console.WriteLine("🔗 Mapeando usuarios por Login...");
            var permisosConMapping = await MapearUsuariosPorLogin(connectionSeguridad, permisosEmpresa, tablaUsuariosSeguridad);
            Console.WriteLine($"✅ Se mapearon {permisosConMapping.Count} permisos\n");

            // 2b. Sincronizar Permisos que falten en SEGURIDAD
            Console.WriteLine("📤 Verificando Permisos en SEGURIDAD...");
            int permisosFaltantes = await SincronizarPermisos(connectionEmpresa, connectionSeguridad);
            Console.WriteLine($"✅ Se sincronizaron {permisosFaltantes} permisos en SEGURIDAD");
            
            // Mostrar diagnóstico de permisos
            Console.WriteLine("\n🔍 Diagnóstico: Permisos en empresa vs SEGURIDAD");
            await DiagnosticoPermisos(connectionEmpresa, connectionSeguridad);
            
            // Mostrar diagnóstico de IDs de permisos huérfanos
            Console.WriteLine("\n🔍 Diagnóstico: IDs de Permisos Huérfanos");
            await DiagnosticoPermisosHuerfanos(connectionEmpresa, connectionSeguridad);
            
            // Comparar tabla Permisos
            Console.WriteLine("\n🔍 Comparación: Tabla seg.Permisos");
            await CompararTablaPermisos(connectionEmpresa, connectionSeguridad);
            Console.WriteLine();

            // 3. Insertar en SEGURIDAD
            Console.WriteLine("📤 Insertando permisos de usuarios en SEGURIDAD...");
            try
            {
                int insertados = await InsertarPermisosEnSeguridad(connectionSeguridad, permisosConMapping);
                Console.WriteLine($"✅ Se insertaron {insertados} permisos de usuarios en SEGURIDAD\n");
            }
            catch (Exception exInsert)
            {
                Console.WriteLine($"❌ ERROR en inserción: {exInsert.Message}\n");
                Console.WriteLine("🔍 Verificando permisos problemáticos...");
                await VerificarPermisosProblematicos(connectionSeguridad, permisosConMapping);
                throw;
            }

            // 4. Verificación
            Console.WriteLine("✔️ SINCRONIZACIÓN COMPLETADA\n");
            await VerificarInsercion(connectionSeguridad);

            // 5. Asignar todos los permisos a fventurainverter
            Console.WriteLine("\n📋 Asignando todos los permisos a fventurainverter...");
            int permisosAsignados = await AsignarTodosPermisosAlUsuario(connectionSeguridad, "fventuramym");
            Console.WriteLine($"✅ Se asignaron {permisosAsignados} permisos a fventurainverter\n");

            // 6. Asignar todos los permisos a fventuracemm
            Console.WriteLine("\n📋 Asignando todos los permisos a fventuracemm...");
            int permisosAsignadosCemm = await AsignarTodosPermisosAlUsuario(connectionSeguridad, "fventuracemm");
            Console.WriteLine($"✅ Se asignaron {permisosAsignadosCemm} permisos a fventuracemm\n");

            // 7. Asignar todos los permisos a usuarios con perfil = 3
            Console.WriteLine("\n📋 Asignando todos los permisos a usuarios con perfil 3...");
            int permisosAsignadosPerfil3 = await AsignarPermisosAUsuariosConPerfil(connectionSeguridad, 3);
            Console.WriteLine($"✅ Se asignaron permisos a {permisosAsignadosPerfil3} usuarios con perfil 3\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR: {ex.Message}");
        }
    }

    static async Task<string> DetectarTablaUsuarios(string connectionString)
    {
        using (var conn = new SqlConnection(connectionString))
        {
            await conn.OpenAsync();

            // Buscar tabla Usuarios en cualquier esquema
            string query = @"
                SELECT SCHEMA_NAME(schema_id) + '.' + name as TablaCompleta
                FROM sys.tables
                WHERE name = 'Usuarios'";

            using (var cmd = new SqlCommand(query, conn))
            {
                var resultado = await cmd.ExecuteScalarAsync();
                if (resultado != null)
                {
                    return resultado.ToString();
                }
            }
        }

        throw new Exception("❌ No se encontró tabla 'Usuarios' en SEGURIDAD");
    }

    static async Task<int> SincronizarPermisos(string connectionInverter, string connectionSeguridad)
    {
        int insertados = 0;

        // Leer permisos de INVERTER
        var permisosInverter = new List<dynamic>();
        using (var conn = new SqlConnection(connectionInverter))
        {
            await conn.OpenAsync();
            string query = "SELECT Codigo, Nombre, Descripcion, Modulo, Basico, Estado FROM seg.Permisos ORDER BY Nombre";
            using (var cmd = new SqlCommand(query, conn))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        // Conversión segura de tipos
                        object basicoObj = reader["Basico"];
                        int? basico = null;
                        if (basicoObj != DBNull.Value)
                        {
                            if (int.TryParse(basicoObj.ToString(), out int basicoVal))
                                basico = basicoVal;
                        }

                        permisosInverter.Add(new
                        {
                            Codigo = (int)reader["Codigo"],
                            Nombre = reader["Nombre"].ToString(),
                            Descripcion = reader["Descripcion"] != DBNull.Value ? reader["Descripcion"].ToString() : "",
                            Modulo = reader["Modulo"] != DBNull.Value ? (int?)reader["Modulo"] : null,
                            Basico = basico,
                            Estado = reader["Estado"] != DBNull.Value ? (int?)reader["Estado"] : null
                        });
                    }
                }
            }
        }

        // Insertar en SEGURIDAD los que falten (validando por Nombre, SIN insertar el Código)
        using (var conn = new SqlConnection(connectionSeguridad))
        {
            await conn.OpenAsync();

            foreach (var permiso in permisosInverter)
            {
                // Verificar si existe por Nombre
                string queryVerificar = "SELECT Codigo FROM seg.Permisos WHERE Nombre = @Nombre";
                int? codigoExistente = null;

                using (var cmd = new SqlCommand(queryVerificar, conn))
                {
                    cmd.Parameters.AddWithValue("@Nombre", permiso.Nombre);
                    var resultado = await cmd.ExecuteScalarAsync();
                    if (resultado != null)
                        codigoExistente = (int)resultado;
                }

                // Si no existe por Nombre, insertar SIN el Código (IDENTITY se genera automáticamente)
                if (!codigoExistente.HasValue)
                {
                    string queryInsert = @"
                        INSERT INTO seg.Permisos (Nombre, Descripcion, Modulo, Basico, Estado, FechaRegistro, EstadoRegistro)
                        VALUES (@Nombre, @Descripcion, @Modulo, @Basico, @Estado, GETDATE(), 'A')";

                    using (var cmd = new SqlCommand(queryInsert, conn))
                    {
                        cmd.Parameters.AddWithValue("@Nombre", permiso.Nombre ?? "");
                        cmd.Parameters.AddWithValue("@Descripcion", permiso.Descripcion ?? "");
                        cmd.Parameters.AddWithValue("@Modulo", permiso.Modulo ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Basico", permiso.Basico ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Estado", permiso.Estado ?? 1);

                        int result = await cmd.ExecuteNonQueryAsync();
                        if (result > 0)
                        {
                            insertados++;
                            Console.WriteLine($"  ✅ Permiso '{permiso.Nombre}' creado en SEGURIDAD");
                        }
                    }
                }
            }
        }

        return insertados;
    }

    static async Task DiagnosticoPermisos(string connectionInverter, string connectionSeguridad)
    {
        // Obtener permisos que existen en INVERTER pero no en SEGURIDAD
        var permisosInverter = new List<(int Codigo, string Nombre)>();
        var permisosSeguridad = new List<(int Codigo, string Nombre)>();

        using (var conn = new SqlConnection(connectionInverter))
        {
            await conn.OpenAsync();
            string query = "SELECT Codigo, Nombre FROM seg.Permisos ORDER BY Codigo";
            using (var cmd = new SqlCommand(query, conn))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        permisosInverter.Add(((int)reader["Codigo"], reader["Nombre"].ToString()));
                    }
                }
            }
        }

        using (var conn = new SqlConnection(connectionSeguridad))
        {
            await conn.OpenAsync();
            string query = "SELECT Codigo, Nombre FROM seg.Permisos ORDER BY Codigo";
            using (var cmd = new SqlCommand(query, conn))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        permisosSeguridad.Add(((int)reader["Codigo"], reader["Nombre"].ToString()));
                    }
                }
            }
        }

        var nombresSeguridad = new HashSet<string>(permisosSeguridad.Select(p => p.Nombre));
        var permisosFaltantesPorNombre = permisosInverter.Where(p => !nombresSeguridad.Contains(p.Nombre)).ToList();
        
        if (permisosFaltantesPorNombre.Count > 0)
        {
            Console.WriteLine($"❌ {permisosFaltantesPorNombre.Count} permisos en INVERTER NO están en SEGURIDAD por Nombre:");
            foreach (var p in permisosFaltantesPorNombre)
            {
                Console.WriteLine($"   • INVERTER ID={p.Codigo}, Nombre='{p.Nombre}'");
            }
        }
        else
        {
            Console.WriteLine("✅ Todos los permisos de INVERTER están sincronizados en SEGURIDAD");
        }
    }

    static async Task DiagnosticoPermisosHuerfanos(string connectionInverter, string connectionSeguridad)
    {
        // Encontrar IDs de permisos que existen en PermisosUsuario pero no en Permisos
        var permisosHuerfanosInverter = new List<int>();
        var permisosHuerfanosSeguridad = new List<int>();

        using (var conn = new SqlConnection(connectionInverter))
        {
            await conn.OpenAsync();
            string query = @"
                SELECT DISTINCT pu.Permiso
                FROM seg.PermisosUsuario pu
                WHERE NOT EXISTS (SELECT 1 FROM seg.Permisos p WHERE p.Codigo = pu.Permiso)
                ORDER BY pu.Permiso";
            
            using (var cmd = new SqlCommand(query, conn))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        permisosHuerfanosInverter.Add((int)reader["Permiso"]);
                    }
                }
            }
        }

        using (var conn = new SqlConnection(connectionSeguridad))
        {
            await conn.OpenAsync();
            string query = @"
                SELECT DISTINCT pu.Permiso
                FROM seg.PermisosUsuario pu
                WHERE NOT EXISTS (SELECT 1 FROM seg.Permisos p WHERE p.Codigo = pu.Permiso)
                ORDER BY pu.Permiso";
            
            using (var cmd = new SqlCommand(query, conn))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        permisosHuerfanosSeguridad.Add((int)reader["Permiso"]);
                    }
                }
            }
        }

        if (permisosHuerfanosInverter.Count > 0)
        {
            Console.WriteLine($"❌ INVERTER tiene {permisosHuerfanosInverter.Count} Permiso(s) huérfano(s) en PermisosUsuario:");
            foreach (var id in permisosHuerfanosInverter)
            {
                Console.WriteLine($"   • Permiso ID={id} (existe en PermisosUsuario pero no en Permisos)");
            }
        }
        else
        {
            Console.WriteLine("✅ INVERTER: No hay permisos huérfanos");
        }

        if (permisosHuerfanosSeguridad.Count > 0)
        {
            Console.WriteLine($"❌ SEGURIDAD tiene {permisosHuerfanosSeguridad.Count} Permiso(s) huérfano(s) en PermisosUsuario:");
            foreach (var id in permisosHuerfanosSeguridad)
            {
                Console.WriteLine($"   • Permiso ID={id} (existe en PermisosUsuario pero no en Permisos)");
            }
        }
        else
        {
            Console.WriteLine("✅ SEGURIDAD: No hay permisos huérfanos");
        }
    }

    static async Task CompararTablaPermisos(string connectionInverter, string connectionSeguridad)
    {
        var permisosInverter = new Dictionary<string, int>(); // Nombre -> Codigo
        var permisosSeguridad = new Dictionary<string, int>(); // Nombre -> Codigo

        // Leer de INVERTER
        using (var conn = new SqlConnection(connectionInverter))
        {
            await conn.OpenAsync();
            string query = "SELECT Codigo, Nombre FROM seg.Permisos ORDER BY Nombre";
            using (var cmd = new SqlCommand(query, conn))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        string nombre = reader["Nombre"].ToString();
                        int codigo = (int)reader["Codigo"];
                        permisosInverter[nombre] = codigo;
                    }
                }
            }
        }

        // Leer de SEGURIDAD
        using (var conn = new SqlConnection(connectionSeguridad))
        {
            await conn.OpenAsync();
            string query = "SELECT Codigo, Nombre FROM seg.Permisos ORDER BY Nombre";
            using (var cmd = new SqlCommand(query, conn))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        string nombre = reader["Nombre"].ToString();
                        int codigo = (int)reader["Codigo"];
                        permisosSeguridad[nombre] = codigo;
                    }
                }
            }
        }

        // Comparar
        var soloEnInverter = permisosInverter.Where(p => !permisosSeguridad.ContainsKey(p.Key)).ToList();
        var soloEnSeguridad = permisosSeguridad.Where(p => !permisosInverter.ContainsKey(p.Key)).ToList();
        var enAmbas = permisosInverter.Where(p => permisosSeguridad.ContainsKey(p.Key)).ToList();

        Console.WriteLine($"📊 Total en INVERTER: {permisosInverter.Count} permisos");
        Console.WriteLine($"📊 Total en SEGURIDAD: {permisosSeguridad.Count} permisos");
        Console.WriteLine($"✅ En ambas bases: {enAmbas.Count} permisos");

        if (soloEnInverter.Count > 0)
        {
            Console.WriteLine($"\n⚠️  En INVERTER pero NO en SEGURIDAD ({soloEnInverter.Count}):");
            foreach (var p in soloEnInverter)
            {
                Console.WriteLine($"   • ID={p.Value}, Nombre='{p.Key}'");
            }
        }

        if (soloEnSeguridad.Count > 0)
        {
            Console.WriteLine($"\n⚠️  En SEGURIDAD pero NO en INVERTER ({soloEnSeguridad.Count}):");
            foreach (var p in soloEnSeguridad)
            {
                Console.WriteLine($"   • ID={p.Value}, Nombre='{p.Key}'");
            }
        }

        if (soloEnInverter.Count == 0 && soloEnSeguridad.Count == 0)
        {
            Console.WriteLine("\n✅ Las tablas Permisos están SINCRONIZADAS");
        }
        else
        {
            Console.WriteLine($"\n❌ Las tablas Permisos NO están completamente sincronizadas");
        }
    }

    static async Task ValidarEstructuraTablas(string connectionInverter, string connectionSeguridad)
    {
        Console.WriteLine("📋 ESTRUCTURA EN INVERTER:");
        await ObtenerEstructuraTabla(connectionInverter, "seg.PermisosUsuario");

        Console.WriteLine("\n📋 ESTRUCTURA EN SEGURIDAD:");
        await ObtenerEstructuraTabla(connectionSeguridad, "seg.PermisosUsuario");
    }

    static async Task ObtenerEstructuraTabla(string connectionString, string tabla)
    {
        using (var conn = new SqlConnection(connectionString))
        {
            await conn.OpenAsync();

            string query = @"
                SELECT 
                    COLUMN_NAME,
                    DATA_TYPE,
                    IS_NULLABLE,
                    COLUMN_DEFAULT
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = 'seg' AND TABLE_NAME = 'PermisosUsuario'
                ORDER BY ORDINAL_POSITION";

            using (var cmd = new SqlCommand(query, conn))
            {
                Console.WriteLine($"┌─ {tabla}");
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        string columna = reader["COLUMN_NAME"].ToString();
                        string tipo = reader["DATA_TYPE"].ToString();
                        string nullable = reader["IS_NULLABLE"].ToString() == "YES" ? "NULL" : "NOT NULL";
                        string defaultValue = reader["COLUMN_DEFAULT"] != DBNull.Value ? $"DEFAULT: {reader["COLUMN_DEFAULT"]}" : "";

                        Console.WriteLine($"│  • {columna,-20} {tipo,15} {nullable,10} {defaultValue}");
                    }
                }
                Console.WriteLine("└─");
            }
        }
    }

    static async Task VerificarPermisosProblematicos(string connectionString, List<PermisoUsuarioMapeado> permisos)
    {
        var permisosUnicos = permisos.Select(p => p.Permiso).Distinct().ToList();

        using (var conn = new SqlConnection(connectionString))
        {
            await conn.OpenAsync();

            foreach (var permisoId in permisosUnicos)
            {
                string query = "SELECT Codigo FROM seg.Permisos WHERE Codigo = @Codigo";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Codigo", permisoId);
                    var resultado = await cmd.ExecuteScalarAsync();
                    
                    if (resultado == null)
                    {
                        Console.WriteLine($"❌ Permiso ID={permisoId} NO existe en SEGURIDAD");
                    }
                }
            }
        }
    }

    static async Task<List<PermisoUsuario>> ObtenerPermisosDeEmpresa(string connectionString, string nombreEmpresa)
    {
        var permisos = new List<PermisoUsuario>();

        using (var conn = new SqlConnection(connectionString))
        {
            await conn.OpenAsync();

            string query = @"
                SELECT 
                    eu.Codigo AS IdUsuario,
                    eu.Login,
                    pu.Permiso
                FROM seg.PermisosUsuario pu
                JOIN seg.Usuarios eu ON pu.Usuario = eu.Codigo
                ORDER BY eu.Login";

            using (var cmd = new SqlCommand(query, conn))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        permisos.Add(new PermisoUsuario
                        {
                            IdUsuarioEmpresa = (int)reader["IdUsuario"],
                            Login = reader["Login"].ToString(),
                            Permiso = (int)reader["Permiso"],
                            Empresa = nombreEmpresa
                        });
                    }
                }
            }
        }

        return permisos;
    }

    static async Task<List<PermisoUsuarioMapeado>> MapearUsuariosPorLogin(string connectionString, List<PermisoUsuario> permisos, string tablaUsuarios)
    {
        var permisosConMapping = new List<PermisoUsuarioMapeado>();
        var usuariosMapeados = new Dictionary<string, int>();

        using (var conn = new SqlConnection(connectionString))
        {
            await conn.OpenAsync();

            // Obtener todos los logins únicos
            var loginsUnicos = permisos.Select(p => p.Login).Distinct().ToList();

            foreach (var login in loginsUnicos)
            {
                string query = $"SELECT Codigo FROM {tablaUsuarios} WHERE Login = @Login";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Login", login);

                    var resultado = await cmd.ExecuteScalarAsync();
                    if (resultado != null)
                    {
                        usuariosMapeados[login] = (int)resultado;
                    }
                    else
                    {
                        Console.WriteLine($"⚠️  Usuario {login} NO encontrado en SEGURIDAD");
                    }
                }
            }
        }

        // Crear lista de permisos mapeados
        foreach (var permiso in permisos)
        {
            if (usuariosMapeados.ContainsKey(permiso.Login))
            {
                permisosConMapping.Add(new PermisoUsuarioMapeado
                {
                    IdUsuarioSeguridad = usuariosMapeados[permiso.Login],
                    Login = permiso.Login,
                    Permiso = permiso.Permiso,
                    Empresa = permiso.Empresa
                });
            }
        }

        return permisosConMapping;
    }

    static async Task<int> InsertarPermisosEnSeguridad(string connectionString, List<PermisoUsuarioMapeado> permisos)
    {
        int insertados = 0;
        int saltados = 0;

        using (var conn = new SqlConnection(connectionString))
        {
            await conn.OpenAsync();

            foreach (var permiso in permisos)
            {
                // Verificar que el Permiso existe en seg.Permisos antes de insertar
                string queryVerificarPermiso = "SELECT Codigo FROM seg.Permisos WHERE Codigo = @Codigo";
                int? permisoExiste = null;

                using (var cmd = new SqlCommand(queryVerificarPermiso, conn))
                {
                    cmd.Parameters.AddWithValue("@Codigo", permiso.Permiso);
                    var resultado = await cmd.ExecuteScalarAsync();
                    if (resultado != null)
                        permisoExiste = (int)resultado;
                }

                if (!permisoExiste.HasValue)
                {
                    Console.WriteLine($"⚠️  Saltando permiso {permiso.Permiso} para usuario {permiso.Login} (no existe en SEGURIDAD)");
                    saltados++;
                    continue;
                }

                string query = @"
                    IF NOT EXISTS (SELECT 1 FROM seg.PermisosUsuario 
                                   WHERE Usuario = @Usuario AND Permiso = @Permiso)
                    BEGIN
                        INSERT INTO seg.PermisosUsuario (Usuario, Permiso, Estado, FechaRegistro, EstadoRegistro)
                        VALUES (@Usuario, @Permiso, 1, GETDATE(), 'A')
                    END";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Usuario", permiso.IdUsuarioSeguridad);
                    cmd.Parameters.AddWithValue("@Permiso", permiso.Permiso);

                    int result = await cmd.ExecuteNonQueryAsync();
                    if (result > 0)
                        insertados++;
                }
            }
        }

        Console.WriteLine($"✅ Se insertaron {insertados} permisos de usuarios");
        if (saltados > 0)
            Console.WriteLine($"⚠️  Se saltaron {saltados} permisos que no existen en SEGURIDAD");

        return insertados;
    }

    static async Task VerificarInsercion(string connectionString)
    {
        using (var conn = new SqlConnection(connectionString))
        {
            await conn.OpenAsync();

            string query = @"
                SELECT 
                    su.Login,
                    COUNT(*) AS [Total Permisos]
                FROM seg.PermisosUsuario spu
                JOIN [Usuarios] su ON spu.Usuario = su.Codigo
                GROUP BY su.Login
                ORDER BY su.Login";

            using (var cmd = new SqlCommand(query, conn))
            {
                Console.WriteLine("\n📊 VERIFICACIÓN FINAL:");
                Console.WriteLine("----------------------------------------");

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        string login = reader["Login"].ToString();
                        int totalPermisos = (int)reader["Total Permisos"];
                        Console.WriteLine($"{login,-30} : {totalPermisos,4} permisos");
                    }
                }
            }
        }
    }

    static async Task<int> AsignarTodosPermisosAlUsuario(string connectionString, string login)
    {
        int permisosAsignados = 0;

        using (var conn = new SqlConnection(connectionString))
        {
            await conn.OpenAsync();

            // 1. Obtener ID del usuario por Login
            string queryObtenerUsuario = "SELECT Codigo FROM [Usuarios] WHERE Login = @Login";
            int? idUsuario = null;

            using (var cmd = new SqlCommand(queryObtenerUsuario, conn))
            {
                cmd.Parameters.AddWithValue("@Login", login);
                var resultado = await cmd.ExecuteScalarAsync();
                if (resultado != null)
                    idUsuario = (int)resultado;
            }

            if (!idUsuario.HasValue)
            {
                Console.WriteLine($"❌ Usuario '{login}' no encontrado en SEGURIDAD");
                return 0;
            }

            Console.WriteLine($"✅ Usuario '{login}' encontrado con ID={idUsuario}");

            // 2. Obtener todos los permisos en SEGURIDAD
            var todosPermisos = new List<int>();
            string queryObtenerPermisos = "SELECT Codigo FROM seg.Permisos ORDER BY Codigo";

            using (var cmd = new SqlCommand(queryObtenerPermisos, conn))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        todosPermisos.Add((int)reader["Codigo"]);
                    }
                }
            }

            Console.WriteLine($"📊 Total de permisos en SEGURIDAD: {todosPermisos.Count}");

            // 3. Asignar cada permiso al usuario
            foreach (var permisoId in todosPermisos)
            {
                // Verificar si ya existe
                string queryVerificar = "SELECT 1 FROM seg.PermisosUsuario WHERE Usuario = @Usuario AND Permiso = @Permiso";
                bool yaExiste = false;

                using (var cmd = new SqlCommand(queryVerificar, conn))
                {
                    cmd.Parameters.AddWithValue("@Usuario", idUsuario.Value);
                    cmd.Parameters.AddWithValue("@Permiso", permisoId);
                    var resultado = await cmd.ExecuteScalarAsync();
                    yaExiste = resultado != null;
                }

                if (!yaExiste)
                {
                    string queryInsertar = @"
                        INSERT INTO seg.PermisosUsuario (Usuario, Permiso, Estado, FechaRegistro, EstadoRegistro)
                        VALUES (@Usuario, @Permiso, 1, GETDATE(), 'A')";

                    using (var cmd = new SqlCommand(queryInsertar, conn))
                    {
                        cmd.Parameters.AddWithValue("@Usuario", idUsuario.Value);
                        cmd.Parameters.AddWithValue("@Permiso", permisoId);

                        int result = await cmd.ExecuteNonQueryAsync();
                        if (result > 0)
                            permisosAsignados++;
                    }
                }
            }
        }

        return permisosAsignados;
    }

    static async Task<int> AsignarPermisosAUsuariosConPerfil(string connectionString, int perfil)
    {
        int usuariosActualizados = 0;

        using (var conn = new SqlConnection(connectionString))
        {
            await conn.OpenAsync();

            // 1. Obtener todos los usuarios con perfil = 3
            string queryUsuarios = @"
                SELECT Codigo, Login, Perfil
                FROM dbo.Usuarios
                WHERE Perfil = @Perfil";

            var usuariosConPerfil = new List<(int codigo, string login)>();

            using (var cmd = new SqlCommand(queryUsuarios, conn))
            {
                cmd.Parameters.AddWithValue("@Perfil", perfil);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        usuariosConPerfil.Add(((int)reader["Codigo"], reader["Login"].ToString()));
                    }
                }
            }

            Console.WriteLine($"📊 Se encontraron {usuariosConPerfil.Count} usuarios con perfil {perfil}");

            // 2. Para cada usuario, obtener todos los permisos en SEGURIDAD
            var todosPermisos = new List<int>();
            string queryPermisos = "SELECT DISTINCT Codigo FROM seg.Permisos ORDER BY Codigo";

            using (var cmd = new SqlCommand(queryPermisos, conn))
            {
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        todosPermisos.Add((int)reader["Codigo"]);
                    }
                }
            }

            Console.WriteLine($"📊 Total de permisos en SEGURIDAD: {todosPermisos.Count}");

            // 3. Asignar todos los permisos a cada usuario
            foreach (var (codigoUsuario, login) in usuariosConPerfil)
            {
                int permisosAsignados = 0;

                foreach (var permisoId in todosPermisos)
                {
                    // Verificar si ya existe
                    string queryVerificar = "SELECT 1 FROM seg.PermisosUsuario WHERE Usuario = @Usuario AND Permiso = @Permiso";
                    bool yaExiste = false;

                    using (var cmd = new SqlCommand(queryVerificar, conn))
                    {
                        cmd.Parameters.AddWithValue("@Usuario", codigoUsuario);
                        cmd.Parameters.AddWithValue("@Permiso", permisoId);
                        var resultado = await cmd.ExecuteScalarAsync();
                        yaExiste = resultado != null;
                    }

                    // Si no existe, insertar
                    if (!yaExiste)
                    {
                        string queryInsertar = @"
                            INSERT INTO seg.PermisosUsuario (Usuario, Permiso, Estado, FechaRegistro)
                            VALUES (@Usuario, @Permiso, 1, GETDATE())";

                        using (var cmd = new SqlCommand(queryInsertar, conn))
                        {
                            cmd.Parameters.AddWithValue("@Usuario", codigoUsuario);
                            cmd.Parameters.AddWithValue("@Permiso", permisoId);
                            try
                            {
                                await cmd.ExecuteNonQueryAsync();
                                permisosAsignados++;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"⚠️ Error asignando permiso {permisoId} a usuario {login}: {ex.Message}");
                            }
                        }
                    }
                }

                if (permisosAsignados > 0)
                {
                    Console.WriteLine($"✅ Usuario '{login}' - Se asignaron {permisosAsignados} permisos nuevos");
                    usuariosActualizados++;
                }
                else
                {
                    Console.WriteLine($"ℹ️ Usuario '{login}' - Ya tenía todos los permisos asignados");
                }
            }
        }

        return usuariosActualizados;
    }
}

class PermisoUsuario
{
    public int IdUsuarioEmpresa { get; set; }
    public string Login { get; set; }
    public int Permiso { get; set; }
    public string Empresa { get; set; }
}

class PermisoUsuarioMapeado
{
    public int IdUsuarioSeguridad { get; set; }
    public string Login { get; set; }
    public int Permiso { get; set; }
    public string Empresa { get; set; }
}
