using Kafka.Consumer.Data;
using Kafka.Consumer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kafka.Consumer.Repositories;

public class PaymentRepository(CheckoutsDbContext context, ILogger<PaymentRepository> logger) : IPaymentRepository
{
    public async Task<Payment> SavePaymentAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if payment already exists to prevent duplicates
            var existingPayment = await context.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.OrderId == payment.OrderId || p.TransactionId == payment.TransactionId,
                    cancellationToken);

            if (existingPayment != null)
            {
                logger.LogWarning(
                    "Payment with OrderId {OrderId} or TransactionId {TransactionId} already exists. Skipping save.",
                    payment.OrderId, payment.TransactionId);
                return existingPayment;
            }

            payment.CreatedAt = DateTime.UtcNow;

            context.Payments.Add(payment);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully saved payment for OrderId {OrderId} with ID {Id}", payment.OrderId,
                payment.Id);
            return payment;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving payment for OrderId {OrderId}", payment.OrderId);
            throw;
        }
    }

    public async Task<Payment?> GetPaymentByOrderIdAsync(string orderId, CancellationToken cancellationToken = default)
    {
        return await context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);
    }

    public async Task<Payment?> GetPaymentByTransactionIdAsync(string transactionId,
        CancellationToken cancellationToken = default)
    {
        return await context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TransactionId == transactionId, cancellationToken);
    }

    public async Task<IEnumerable<Payment>> GetPaymentsByStatusAsync(string status,
        CancellationToken cancellationToken = default)
    {
        return await context.Payments
            .AsNoTracking()
            .Where(p => p.Status == status)
            .OrderByDescending(p => p.ProcessedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Payment>> GetRecentPaymentsAsync(int count = 10,
        CancellationToken cancellationToken = default)
    {
        return await context.Payments
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}