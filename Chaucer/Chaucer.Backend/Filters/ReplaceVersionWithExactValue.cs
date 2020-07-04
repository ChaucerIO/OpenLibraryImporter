using System;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Chaucer.Backend.Filters
{
    public class ReplaceVersionWithExactValueInPath :
        IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var oap = new OpenApiPaths();
            foreach (var (path, value) in swaggerDoc.Paths)
            {
                var newPath = path.Replace("{version}", swaggerDoc.Info.Version, StringComparison.OrdinalIgnoreCase);
                oap.Add(newPath, value);
            }
                
            swaggerDoc.Paths = oap;
        }
    }
}