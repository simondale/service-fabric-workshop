using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;

namespace BasketApi
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
            services.AddSwaggerGen(options => options.SwaggerDoc("v1", new Info { Title = "Basket", Description = "Basket API" }));
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
            app.UseSwaggerUI(options => options.SwaggerEndpoint("v1/swagger.json", "Basket API"));
            app.UseMvc();
        }
    }
}
