using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Pc.WebApi.Configuration
{
    public static class AppCors
    {
        public const string Nome = "AppPolicy";

        public static void Registrar(CorsOptions options, Func<string, bool> origemPermitida)
        {
            options.AddPolicy(Nome, policy =>
            {
                policy
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .SetPreflightMaxAge(TimeSpan.FromMinutes(10))
                    .SetIsOriginAllowed(origemPermitida)
                    .WithExposedHeaders("X-Error-Code");
            });
        }
    }
}
