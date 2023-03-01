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
using Rotativa.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllersWithViews().AddNewtonsoftJson(x => x.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore).AddRazorRuntimeCompilation();
builder.Services.AddMvc(opt => opt.EnableActionInvokers = false);
builder.Services.AddCors();
builder.Services.AddDbContext<SEGURIDADContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString(@"MVSystemSeguridad")));
builder.Services.AddEndpointsApiExplorer();

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

#region CONFIGURE SERVICES
//(_ => new AccesoDatos(builder.Configuration.GetConnectionString("MVSystem")));
builder.Services.AddTransient<IAccesoDatos, AccesoDatos>();
builder.Services.AddScoped<Catalogos_Negocio>();
builder.Services.AddScoped<Clientes_Negocio>();
builder.Services.AddScoped<Vendedores_Negocio>();
builder.Services.AddScoped<Proveedores_Negocio>();
builder.Services.AddScoped<Accesorios_Negocio>();
builder.Services.AddScoped<Equipos_Negocio>();
builder.Services.AddScoped<Facturas_Negocio>();
builder.Services.AddScoped<Cotizacion_Negocio>();
builder.Services.AddScoped<Marcas_Negocio>();
builder.Services.AddScoped<SeguridadService>();
builder.Services.AddScoped<JwtService>();
#endregion

var app = builder.Build();

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
app.UseCors(Builder => Builder.WithOrigins("*").WithMethods("GET", "POST", "PUT").AllowAnyHeader());

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

RotativaConfiguration.Setup((Microsoft.AspNetCore.Hosting.IHostingEnvironment)app.Environment);

app.Run();
