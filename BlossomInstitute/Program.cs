using BlossomInstitute;
using BlossomInstitute.Application;
using BlossomInstitute.Common;
using BlossomInstitute.Infraestructure;
using BlossomInstitute.Infraestructure.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services
    .AddWebApi()
    .AddCommon()
    .AddApplication()
    .AddInfraestructure(builder.Configuration);

var app = builder.Build();

try
{
    await IdentityDataSeed.SeedRolesAsync(app);
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Error ejecutando seed de Identity");
    throw;
}


app.UseSwagger();
app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Blossom Institute v1");
        c.RoutePrefix = string.Empty;
    });

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
