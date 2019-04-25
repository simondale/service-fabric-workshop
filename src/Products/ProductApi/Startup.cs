using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductApi.Services;
using Swashbuckle.AspNetCore.Swagger;

namespace ProductApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddSwaggerGen(options => options.SwaggerDoc("v1", new Info { Title = "Products", Description = "Products API" }));
            services.AddSingleton<IProductRepository, ProductRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();
            app.UseSwagger(options => options.PreSerializeFilters.Add((d, r) =>
            {
                ServiceEventSource.Current.Message($"Referer: {r.Headers["Referer"]}");
                if (r.Headers.TryGetValue("Referer", out var value))
                {
                    var referer = new PathString(new Uri(value.ToString()).AbsolutePath);
                    if (referer.StartsWithSegments(new PathString("/swagger")))
                    {
                        d.BasePath = "/";
                    }
                    else
                    {
                        var applicationService = Environment.GetEnvironmentVariable("Fabric_ServiceName").Replace("fabric:", "");
                        if (referer.StartsWithSegments(new PathString(applicationService)))
                        {
                            d.BasePath = applicationService;
                        }
                    }
                }
            }));
            app.UseSwaggerUI(options => options.SwaggerEndpoint("v1/swagger.json", "Products API"));
            app.UseMvc();
        }
    }
}
