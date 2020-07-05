using System;
using System.Linq;
using Chaucer.Backend.Filters;
using Chaucer.Backend.Health;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Chaucer.Backend
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
            services.AddControllers();
            services.AddHealthChecks()
                .AddCheck<Heartbeat>(
                    name: "Hearthbeat",
                    tags: new[] {"Chaucer", "Heartbeat",});
            services.AddApiVersioning(o =>
            {
                o.AssumeDefaultVersionWhenUnspecified = true;
                o.DefaultApiVersion = new ApiVersion(majorVersion: 1, minorVersion: 0);
            });
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc(
                    name: "v1",
                    info: new OpenApiInfo
                    {
                        Description = "Chaucer backend API",
                        Version = "1",
                        Title = "Chaucer v1",
                    });
                
                options.OperationFilter<RemoveVersionFromParameter>();
                options.DocumentFilter<ReplaceVersionWithExactValueInPath>();
                
                options.DocInclusionPredicate((version, desc) =>
                {
                    var versions = desc.CustomAttributes()
                        .OfType<ApiVersionAttribute>()
                        .SelectMany(attr => attr.Versions);
                    var versionMatches = versions.Any(v => version.EndsWith(v.ToString(), StringComparison.OrdinalIgnoreCase));
                    
                    var maps = desc.CustomAttributes()
                        .OfType<MapToApiVersionAttribute>()
                        .SelectMany(attr => attr.Versions)
                        .ToList();
                    var mapDoesNotMatch = !maps.Any() || maps.Any(v => version.EndsWith(v.ToString(), StringComparison.OrdinalIgnoreCase)); 
                    return versionMatches && mapDoesNotMatch;
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            // No need for HTTPS redirection in a docker container

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks(pattern: "/health");
            });
            
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/swagger/v1/swagger.json", name: "v1");
                // v2 example:
                // c.SwaggerEndpoint($"/swagger/v2/swagger.json", name: "v2");
                c.DisplayOperationId();
                c.DisplayRequestDuration();
            });
        }
    }
}