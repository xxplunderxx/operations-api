using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Operations.Tests;

public sealed class OperationsApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("CsvData:DataDirectory", Path.Combine(AppContext.BaseDirectory, "Data"));
    }
}
