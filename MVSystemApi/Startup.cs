using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using MVSystemApi.Model_Negocio;

namespace MVSystemApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
               .SetBasePath(env.ContentRootPath)
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            builder.AddEnvironmentVariables();

            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            services.AddControllers();
            ConfigureServicio(services);

            services.AddMvc(options =>
            {
               
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseCors(Builder => Builder.WithOrigins("*").WithMethods("GET", "POST", "PUT").AllowAnyHeader());
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/error");
            }
            app.UseCors();
            app.UseHttpsRedirection();
            app.UseRouting();


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        public void ConfigureServicio(IServiceCollection services)
        {
            services.AddScoped<IAccesoDatos>(_ => new AccesoDatos(Configuration.GetConnectionString("MVSystem")));

            services.AddScoped<Catalogos_Negocio>();
            services.AddScoped<Clientes_Negocio>();
            services.AddScoped<Vendedores_Negocio>(); 
            services.AddScoped<Proveedores_Negocio>();
            services.AddScoped<Accesorios_Negocio>();
            services.AddScoped<Equipos_Negocio>();
            services.AddScoped<Facturas_Negocio>();
            
        }
    }
}
