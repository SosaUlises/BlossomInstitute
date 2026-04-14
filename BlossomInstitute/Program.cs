using BlossomInstitute;
using BlossomInstitute.Application;
using BlossomInstitute.Application.DataBase.CloudinaryService.Commands.UploadFile;
using BlossomInstitute.Common;
using BlossomInstitute.Infraestructure;
using BlossomInstitute.Infraestructure.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrWhiteSpace(origin))
                    return false;

                return origin == "http://localhost:3000"
                    || origin == "http://127.0.0.1:3000"
                    || origin == "http://192.168.18.9:3000"
                    || origin == "https://blossom-institute.vercel.app"
                    || origin.EndsWith(".vercel.app");
            })
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.Configure<CloudinaryStorageOptions>(
    builder.Configuration.GetSection("CloudinaryStorage"));

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

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();