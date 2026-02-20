using Finbuckle.MultiTenant.AspNetCore.Extensions;
using Microsoft.AspNetCore.Builder;

namespace FSH.Modules.Multitenancy;

public static class Extensions
{
    public static WebApplication UseHeroMultiTenantDatabases(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseMultiTenant();

        return app;
    }
}
