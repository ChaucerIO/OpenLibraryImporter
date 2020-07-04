using System;
using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Chaucer.Backend.Filters
{
    public class RemoveVersionFromParameter :
        IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var versionParam = operation.Parameters
                .Single(p => string.Equals("version", p.Name, StringComparison.Ordinal));
            operation.Parameters.Remove(versionParam);
        }
    }
}