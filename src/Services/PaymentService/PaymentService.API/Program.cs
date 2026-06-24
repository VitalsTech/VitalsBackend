using PaymentService.Infrastructure;
using PaymentService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Vitals.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddPaymentInfrastructure(builder.Configuration);
builder.Services.AddVitalsAuthentication(builder.Configuration, builder.Environment);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<PaymentDbContext>().Database.Migrate();

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
