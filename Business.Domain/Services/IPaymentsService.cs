namespace Business.Domain.Services;

public interface IPaymentsService
{
    Task<object> GetRecentPayments(int count = 7);
}