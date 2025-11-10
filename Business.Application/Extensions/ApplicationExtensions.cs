using Business.Application.Services;
using Business.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Business.Application.Extensions;

public static class ApplicationExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApplication()
        {
            services.AddScoped<IPaymentsService, PaymentsService>();
            return services;
        }
        
        public IServiceCollection AddOrderProcessingService()
        {
            services.AddScoped<IPaymentFeeCalculator, PaymentFeeCalculator>();
            services.AddScoped<IOrderProcessingService, OrderProcessingService>();
            return services;
        }
    }
}