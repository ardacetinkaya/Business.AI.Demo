using Business.Application.Services;
using Business.Domain.Repositories;
using Business.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Business.Infrastructure.Extensions;

public static class DatabaseExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDatabase(string connectionString, IHostEnvironment environment)
        {
            services.AddDbContext<CheckoutsDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
                options.EnableSensitiveDataLogging(environment.IsDevelopment());
                options.EnableDetailedErrors(environment.IsDevelopment());
            });
            
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IPaymentMethodFeeRepository, PaymentMethodFeeRepository>();
            services.AddScoped<IOrdersPayment>(sp => sp.GetRequiredService<CheckoutsDbContext>());
            
            return services;
        }
        
        public IServiceCollection AddProductRepository()
        {
            services.AddSingleton<IProductRepository, ProductRepository>();
            return services;
        }
    }
}