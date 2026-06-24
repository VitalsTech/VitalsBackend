using IntegrationService.Infrastructure;
using Microsoft.OpenApi.Models;
using Vitals.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Integration Service", Version = "v1" });
});

builder.Services.AddIntegrationInfrastructure(builder.Configuration);
builder.Services.AddVitalsAuthentication(builder.Configuration, builder.Environment);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseVitalsInternalServiceAuth();
app.UseAuthorization();
app.MapControllers();
app.Run();
