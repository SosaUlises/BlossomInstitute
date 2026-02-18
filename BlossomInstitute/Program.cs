using BlossomInstitute;
using BlossomInstitute.Application;
using BlossomInstitute.Common;
using BlossomInstitute.External;
using BlossomInstitute.Persistence;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddControllers();

builder.Services
    .AddWebApi()
    .AddCommon()
    .AddApplication()
    .AddExternal(builder.Configuration)
    .AddPersistence(builder.Configuration);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
