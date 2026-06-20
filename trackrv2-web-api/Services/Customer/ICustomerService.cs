using BadmintonKiosken.Shared;
using BadmintonKiosken.Shared.DTOs.Customer;

namespace BadmintonKiosken.Api.Services.Customer;

public interface ICustomerService
{
    Task<CustomerProfileResponse> RegisterCustomer(
        CustomerRequest customerRequest);

    Task UpdateCustomerAsync(Guid id,CustomerRequest customerRequest);

    Task DeleteCustomerAsync(Guid id);

    Task UpdateCustomerPasswordAsync(Guid id, string newPassword);

    Task UpdateCustomerBalanceAsync(Guid id, decimal amountChanged);

    Task UpdateCustomerLoyaltyGroupAsync(Guid id, Loyalty newLoyaltyGroup);
    
// Admin role required
    Task<CustomerProfileResponse> GetCustomerById(Guid id);

// Admin role required
    Task<CustomerProfileResponse> GetSingleCustomerBySearch(
        SingleCustomerSearchRequest searchRequest);

    // Admin role required
    Task<List<CustomerOverviewResponse>> GetCustomers(Guid? id,
        string? username,
        string? fullName, string? email, long? phoneNumber, string? nationality,
        decimal? balance, Loyalty? loyaltyGroup, DateTime? createdAt,
        DateTime? lastUpdated);
}