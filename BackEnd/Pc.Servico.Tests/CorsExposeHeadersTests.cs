using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Pc.WebApi.Configuration;
using Xunit;

namespace Pc.Servico.Tests
{
    public class CorsExposeHeadersTests
    {
        [Fact]
        public async Task AppPolicy_Expoe_X_Error_Code()
        {
            var services = new ServiceCollection();
            services.AddCors(options => AppCors.Registrar(options, _ => true));
            await using var provider = services.BuildServiceProvider();
            var policies = provider.GetRequiredService<ICorsPolicyProvider>();

            var policy = await policies.GetPolicyAsync(new DefaultHttpContext(), AppCors.Nome);

            Assert.NotNull(policy);
            Assert.Contains("X-Error-Code", policy!.ExposedHeaders);
        }
    }
}
