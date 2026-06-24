using PaymentService.Application.Interfaces;
using PaymentService.Domain.Entities;
using PaymentService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace PaymentService.Infrastructure.Repositories;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _db;

    public PaymentRepository(PaymentDbContext db) => _db = db;

    public Task<PaymentTransaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Payments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task SaveAsync(PaymentTransaction payment, CancellationToken cancellationToken = default)
    {
        if (_db.Entry(payment).State == EntityState.Detached)
        {
            var existing = await _db.Payments.FindAsync([payment.Id], cancellationToken).ConfigureAwait(false);
            if (existing is null)
                _db.Payments.Add(payment);
            else
                _db.Entry(existing).CurrentValues.SetValues(payment);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
