using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MVSystemApi.Handlers;
using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using MVSystemApi.Model_Negocio;
using MVSystemApi.Model_Negocio.Seguridad;
using MVSystemApi.ModelsEF;
using MVSystemApi.Services;
using Rotativa.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers().AddNewtonsoftJson(x => {
    x.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    x.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
});
builder.Services.AddControllersWithViews().AddNewtonsoftJson(x => {
    x.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    x.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
}).AddRazorRuntimeCompilation();
builder.Services.AddMvc(opt => opt.EnableActionInvokers = false);
builder.Services.AddCors();
builder.Services.AddDbContext<SEGURIDADContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString(@"MVSystemSeguridad")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient<WhatsAppService>();

// Configurar HttpClientFactory para FacEDGIService con X-Api-Key header
builder.Services.AddHttpClient("FacEDGI")
    .ConfigureHttpClient((client) =>
    {
        var config = builder.Configuration;
        client.BaseAddress = new Uri(config["FacEDGI:BaseUrl"] ?? "https://api.easyconexion.com/ecf-rd");
        client.Timeout = TimeSpan.FromSeconds(int.Parse(config["FacEDGI:Timeout"] ?? "30"));
        client.DefaultRequestHeaders.Add("X-Api-Key", config["FacEDGI:ApiKey"]);
    });

builder.Services.AddScoped<IFacEDGIService, FacEDGIService>();
builder.Services.AddSingleton<WhatsAppService>();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Api MvSystem REST",
        Version = "v1"
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(jwtBearerOptions => jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters()
{
    ValidateLifetime = true,
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer = builder.Configuration["Jwt:Issuer"],
    ValidAudience = builder.Configuration["Jwt:Issuer"],
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
    ClockSkew = TimeSpan.Zero,
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthorizationHandler, RolesAuthorizationHandler>();
builder.Services.AddScoped<IRoleService, RoleService>();

// Configurar opciones de Facturaci�n
builder.Services.Configure<FacturacionSettings>(builder.Configuration.GetSection("Facturacion"));

#region CONFIGURE SERVICES
//(_ => new AccesoDatos(builder.Configuration.GetConnectionString("MVSystem")));
builder.Services.AddTransient<IAccesoDatos, AccesoDatos>();
builder.Services.AddTransient<AccesoDatos>();
builder.Services.AddTransient<ISyncService, SyncService>();
builder.Services.AddScoped<Catalogos_Negocio>();
builder.Services.AddScoped<Clientes_Negocio>();
builder.Services.AddScoped<Vendedores_Negocio>();
builder.Services.AddScoped<Proveedores_Negocio>();
builder.Services.AddScoped<Accesorios_Negocio>();
builder.Services.AddScoped<Equipos_Negocio>();
builder.Services.AddScoped<Facturas_Negocio>();
builder.Services.AddScoped<Cotizacion_Negocio>();
builder.Services.AddScoped<Marcas_Negocio>();
builder.Services.AddScoped<Utilidades_Negocio>();
builder.Services.AddScoped<SeguridadService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<PreFacturas_Negocio>();
builder.Services.AddScoped<Inventario_Negocio>();

// Servicios de Facturaci�n Electr�nica (DGI)
builder.Services.AddScoped<IFacEDGIService, FacEDGIService>();

// Servicios de Seguridad
builder.Services.AddScoped<UsuariosService>();
builder.Services.AddScoped<PermisosService>();
builder.Services.AddScoped<RolesService>();  // RolesService hereda de PerfilesService
builder.Services.AddScoped<AsignacionesService>();
builder.Services.AddScoped<ITracking, Tracking_Negocio>();
builder.Services.AddScoped<DepartamentosService>();
builder.Services.AddScoped<ModulosService>();
builder.Services.AddScoped<SistemasService>();

#endregion

var app = builder.Build();

var whatsappService = app.Services.GetRequiredService<WhatsAppService>();
var autoStart = builder.Configuration.GetValue<bool>("WhatsApp:AutoStart");

// Desactivar servicio WhatsApp al iniciar la API
autoStart = false;

if (autoStart)
{
    _ = Task.Run(async () =>
    {
        await Task.Delay(3000);
        await whatsappService.IniciarServidorWhatsApp();
    });
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Api MvSystem REST");
        c.RoutePrefix = string.Empty;
    });
}
else
    app.UseExceptionHandler("/error");

app.UseStaticFiles();
app.UseRouting();
app.UseCors(Builder => Builder.WithOrigins("*").WithMethods("GET", "POST", "PUT", "DELETE").AllowAnyHeader());

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Films}/{action=Index}/{id?}").RequireAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers().RequireAuthorization();
});

#pragma warning disable CS0618
RotativaConfiguration.Setup((Microsoft.AspNetCore.Hosting.IHostingEnvironment)app.Environment);
#pragma warning restore CS0618

app.Run();
