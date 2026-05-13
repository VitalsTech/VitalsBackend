using UserService.Application.Interfaces;
using UserService.Application.Mappings;
using UserService.Application.Services;
using UserService.Infrastructure;
using UserService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace UserService.API
{
    public class Program
    {
        public static void Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);

            // Add services
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Add Infrastructure (DbContext, Repositories)
            builder.Services.AddInfrastructure(builder.Configuration);

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
            }

            // Configure pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
