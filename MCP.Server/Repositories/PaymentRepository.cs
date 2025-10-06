
using MCP.Server.Data;
using MCP.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MCP.Server.Repositories;

public class PaymentRepository(CheckoutsDbContext context) : IPaymentRepository
{
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