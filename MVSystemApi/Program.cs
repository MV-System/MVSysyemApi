using Microsoft.OpenApi.Models;
using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using MVSystemApi.Model_Negocio;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers().AddNewtonsoftJson(x => x.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);
builder.Services.AddMvc();
builder.Services.AddCors();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Api MvSystem REST",
        Version = "v1"
    });
});


#region CONFIGURE SERVICES
builder.Services.AddScoped<IAccesoDatos>(_ => new AccesoDatos(builder.Configuration.GetConnectionString("MVSystem")));
builder.Services.AddScoped<Catalogos_Negocio>();
builder.Services.AddScoped<Clientes_Negocio>();
builder.Services.AddScoped<Vendedores_Negocio>();
builder.Services.AddScoped<Proveedores_Negocio>();
builder.Services.AddScoped<Accesorios_Negocio>();
builder.Services.AddScoped<Equipos_Negocio>();
builder.Services.AddScoped<Facturas_Negocio>();
builder.Services.AddScoped<Marcas_Negocio>();
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

app.UseCors(Builder => Builder.WithOrigins("*").WithMethods("GET", "POST", "PUT").AllowAnyHeader());
app.UseHttpsRedirection();

app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();
