using UserService.Application.Interfaces;
using UserService.Application.Mappings;
using UserService.Application.Services;
using UserService.API.Middleware;
using UserService.API.Swagger;
using UserService.API.Validators;
using UserService.Infrastructure;
using UserService.Infrastructure.Data;
using FluentValidation;
using FluentValidation.AspNetCore;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Vitals.AspNetCore.Authentication;

namespace UserService.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);

            // Add services
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SchemaFilter<ValidationSchemaFilter>();
                options.OperationFilter<RequestExampleOperationFilter>();
            });

            // Add Infrastructure (DbContext, Repositories)
            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddValidatorsFromAssemblyContaining<CreateUserWithProfileRequestValidator>();
            builder.Services.AddFluentValidationRulesToSwagger();
            builder.Services.AddVitalsAuthentication(builder.Configuration, builder.Environment);

            // Add Application Services
            builder.Services.AddScoped<IEncryptionService, EncryptionService>();
            builder.Services.AddScoped<IPermissionService, PermissionService>();
            builder.Services.AddScoped<IAdminUserService, AdminService>();
            builder.Services.AddScoped<IMultiProfileUserService, MultiProfileUserService>();


            // Add AutoMapper
            builder.Services.AddAutoMapper(typeof(MappingProfile));

            // Configure Encryption Settings
            builder.Services.Configure<EncryptionSettings>(builder.Configuration.GetSection("Encryption"));

            var app = builder.Build();

            // Apply migrations on startup
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Database.Migrate();
                PatientAddressSchema.EnsureColumns(dbContext);
            }

            await DoctorScheduleSeeder.SeedAsync(app.Services);

            // Configure pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseVitalsInternalServiceAuth();
            app.UseAuthorization();
            app.UseMiddleware<GlobalExceptionHandler>();
            app.MapControllers();

            await app.RunAsync();
        }
    }
}
