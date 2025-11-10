namespace Business.Domain.Entities;

public class PaymentMethodFee
{
    public int Id { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal FeePercentage { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}