using Business.Domain.Entities;
using Business.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Business.Infrastructure.Database;

public class PaymentMethodFeeRepository(CheckoutsDbContext context) : IPaymentMethodFeeRepository
{
    public async Task<IEnumerable<PaymentMethodFee>> GetAllActiveFeesAsync(CancellationToken cancellationToken = default)
    {
        return await context.PaymentMethodFees
            .Where(f => f.IsActive)
            .OrderBy(f => f.PaymentMethod)
            .ToListAsync(cancellationToken);
    }

    public async Task<PaymentMethodFee?> GetFeeByPaymentMethodAsync(string paymentMethod, CancellationToken cancellationToken = default)
    {
        return await context.PaymentMethodFees
            .FirstOrDefaultAsync(f => f.PaymentMethod == paymentMethod && f.IsActive, cancellationToken);
    }

    public async Task<PaymentMethodFee> CreateOrUpdateFeeAsync(PaymentMethodFee fee, CancellationToken cancellationToken = default)
    {
        var existingFee = await context.PaymentMethodFees
            .FirstOrDefaultAsync(f => f.PaymentMethod == fee.PaymentMethod, cancellationToken);

        if (existingFee != null)
        {
            existingFee.FeePercentage = fee.FeePercentage;
            existingFee.IsActive = fee.IsActive;
            existingFee.UpdatedAt = DateTime.UtcNow;
            
            context.PaymentMethodFees.Update(existingFee);
            await context.SaveChangesAsync(cancellationToken);
            return existingFee;
        }
        else
        {
            fee.CreatedAt = DateTime.UtcNow;
            fee.UpdatedAt = DateTime.UtcNow;
            
            context.PaymentMethodFees.Add(fee);
            await context.SaveChangesAsync(cancellationToken);
            return fee;
        }
    }

    public async Task<bool> DeactivateFeeAsync(string paymentMethod, CancellationToken cancellationToken = default)
    {
        var fee = await context.PaymentMethodFees
            .FirstOrDefaultAsync(f => f.PaymentMethod == paymentMethod, cancellationToken);

        if (fee == null)
            return false;

        fee.IsActive = false;
        fee.UpdatedAt = DateTime.UtcNow;
        
        context.PaymentMethodFees.Update(fee);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}