using BadmintonKiosken.Core;
using BadmintonKiosken.Shared;
using BadmintonKiosken.Shared.DTOs.Customer;
using BadmintonKiosken.Shared.DTOs.CustomerAddress;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BadmintonKiosken.Api.Services.Customer;

public class CustomerService : ICustomerService
{
    private readonly BadmintonKioskenContext _ctx;
    private readonly IMemoryCache _cache;
    private const string CustomerCachePrefix = "customer_";

    private readonly IPasswordHasher<Core.Entities.Customer>
        _passwordHasher;

    public CustomerService(BadmintonKioskenContext ctx,
        IPasswordHasher<Core.Entities.Customer> passwordHasher,
        IMemoryCache cache)
    {
        _ctx = ctx;
        _passwordHasher = passwordHasher;
        _cache = cache;
    }

    public async Task<CustomerProfileResponse> RegisterCustomer(
        CustomerRequest customerRequest)
    {
        var existingCustomer = await
            _ctx.Customers.AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.Username == customerRequest.Username);

        if (existingCustomer != null)
        {
            throw new InvalidOperationException(
                $"Brugernavnet '{customerRequest.Username}' er allerede taget.");
        }

        var customer = new Core.Entities.Customer
        {
            Username = customerRequest.Username,
            Email = customerRequest.Email,
            Password = "",
            Nationality = customerRequest.Nationality,
            FirstName = customerRequest.FirstName,
            MiddleName = customerRequest.MiddleName,
            LastName = customerRequest.LastName,
            PhoneNumber = customerRequest.PhoneNumber,
            Balance = 0,
        };
        customer.Password =
            _passwordHasher.HashPassword(customer, customerRequest.Password);
        var addedCustomer = await _ctx.Customers.AddAsync(customer);
        await _ctx.SaveChangesAsync();
        var addedCustomerEntity = addedCustomer.Entity;
        var fullName =
            $"{addedCustomerEntity.FirstName} {addedCustomerEntity.MiddleName}{(addedCustomerEntity.MiddleName != null ? " " : "")}{addedCustomerEntity.LastName}";


        return new CustomerProfileResponse(addedCustomerEntity.Id,
            addedCustomerEntity.Username, fullName, customer.Email,
            addedCustomerEntity.PhoneNumber,
            customer.Nationality, addedCustomerEntity.Balance,
            addedCustomerEntity.LoyaltyGroup,
            new List<CustomerAddressResponse>(), addedCustomerEntity.CreatedAt,
            addedCustomerEntity.LastUpdated);
    }

    // Basic Customer Info update
    public async Task UpdateCustomerAsync(Guid id,
        CustomerRequest customerRequest)
    {
        var customer = await _ctx.Customers
            .FirstOrDefaultAsync(u => u.Id == id);
        if (customer == null)
        {
            throw new KeyNotFoundException(
                $"En kunde med id'et: '{id}' kunne ikke findes.");
        }

        // If updating Username make sure it is not taken already
        if (customer.Username != customerRequest.Username)
        {
            var usernameExists =
                await _ctx.Customers
                    .AsNoTracking()
                    .AnyAsync(c =>
                        c.Username == customerRequest.Username);

            if (usernameExists)
            {
                throw new InvalidOperationException(
                    $"Brugernavnet '{customerRequest.Username}' er allerede taget.");
            }
        }

        // If updating Email make sure it is not taken already
        if (customer.Email != customerRequest.Email)
        {
            var emailExists =
                await _ctx.Customers
                    .AsNoTracking()
                    .AnyAsync(c =>
                        c.Email == customerRequest.Email);

            if (emailExists)
            {
                throw new InvalidOperationException(
                    $"E-mailen '{customerRequest.Email}' er allerede taget.");
            }
        }

        // If updating PhoneNumber make sure it is not taken already
        if (customer.PhoneNumber != customerRequest.PhoneNumber)
        {
            var phoneNumberExists =
                await _ctx.Customers
                    .AsNoTracking()
                    .AnyAsync(c =>
                        c.PhoneNumber == customerRequest.PhoneNumber);

            if (phoneNumberExists)
            {
                throw new InvalidOperationException(
                    $"Telefonnummeret '{customerRequest.PhoneNumber}' er allerede taget.");
            }
        }

        customer.Username = customerRequest.Username;
        customer.Email = customerRequest.Email;
        customer.FirstName = customerRequest.FirstName;
        customer.MiddleName = customerRequest.MiddleName;
        customer.LastName = customerRequest.LastName;
        customer.Nationality = customerRequest.Nationality;
        customer.PhoneNumber = customerRequest.PhoneNumber;

        await _ctx.SaveChangesAsync();

        string cacheKey = $"{CustomerCachePrefix}{id}";
        _cache.Remove(cacheKey);
    }


    public async Task DeleteCustomerAsync(Guid id)
    {
        var existingUser = await _ctx.Customers.FindAsync(id);
        if (existingUser != null)
        {
            _ctx.Customers.Remove(existingUser);
            await _ctx.SaveChangesAsync();
        }

        string cacheKey = $"{CustomerCachePrefix}{id}";
        _cache.Remove(cacheKey);
    }

    public async Task UpdateCustomerPasswordAsync(Guid id, string newPassword)
    {
        var customer = await _ctx.Customers
            .FirstOrDefaultAsync(u => u.Id == id);
        if (customer == null)
        {
            throw new KeyNotFoundException(
                $"En kunde med id'et: '{id}' kunne ikke findes.");
        }

        customer.Password =
            _passwordHasher.HashPassword(customer, newPassword);

        await _ctx.SaveChangesAsync();

        string cacheKey = $"{CustomerCachePrefix}{id}";
        _cache.Remove(cacheKey);
    }

    public async Task UpdateCustomerBalanceAsync(Guid id, decimal amountChanged)
    {
        var customer = await _ctx.Customers
            .FirstOrDefaultAsync(u => u.Id == id);
        if (customer == null)
        {
            throw new KeyNotFoundException(
                $"En kunde med id'et: '{id}' kunne ikke findes.");
        }

        customer.Balance += amountChanged;

        await _ctx.SaveChangesAsync();

        string cacheKey = $"{CustomerCachePrefix}{id}";
        _cache.Remove(cacheKey);
    }

    public async Task UpdateCustomerLoyaltyGroupAsync(Guid id,
        Loyalty newLoyaltyGroup)
    {
        var customer = await _ctx.Customers
            .FirstOrDefaultAsync(u => u.Id == id);
        if (customer == null)
        {
            throw new KeyNotFoundException(
                $"En kunde med id'et: '{id}' kunne ikke findes.");
        }

        customer.LoyaltyGroup = newLoyaltyGroup;

        await _ctx.SaveChangesAsync();

        string cacheKey = $"{CustomerCachePrefix}{id}";
        _cache.Remove(cacheKey);
    }

    public async Task<CustomerProfileResponse> GetCustomerById(Guid id)
    {
        string cacheKey = $"{CustomerCachePrefix}{id}";

        return (await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

            var customer = await _ctx.Customers.AsNoTracking()
                .Include(ca => ca.SavedAddresses)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
            {
                throw new KeyNotFoundException(
                    $"En kunde med id'et: '{id}' kunne ikke findes.");
            }

            var fullName =
                $"{customer.FirstName} {customer.MiddleName}{(customer.MiddleName != null ? " " : "")}{customer.LastName}";

            return new CustomerProfileResponse(customer.Id,
                customer.Username, fullName, customer.Email,
                customer.PhoneNumber,
                customer.Nationality, customer.Balance,
                customer.LoyaltyGroup,
                customer.SavedAddresses.Select(a =>
                        new CustomerAddressResponse(a.Id, a.StreetAddress,
                            a.StreetNumber, a.AdditionalAddress, a.PostalCode,
                            a.City,
                            a.Country, a.IsDefault, a.CreatedAt, a.LastUpdated))
                    .ToList(), customer.CreatedAt,
                customer.LastUpdated);
        }))!;
    }

    // Search for everything unique about a customer
    public async Task<CustomerProfileResponse> GetSingleCustomerBySearch(
        SingleCustomerSearchRequest searchRequest)
    {
        Core.Entities.Customer? customer = null;

        // Username har højeste prioritet
        if (!string.IsNullOrEmpty(searchRequest.Username))
        {
            customer = await _ctx.Customers.AsNoTracking()
                .Include(c => c.SavedAddresses)
                .FirstOrDefaultAsync(u => u.Username == searchRequest.Username);
        }

        // Ellers prøv med Email
        if (customer == null && !string.IsNullOrEmpty(searchRequest.Email))
        {
            customer = await _ctx.Customers.AsNoTracking()
                .Include(c => c.SavedAddresses)
                .FirstOrDefaultAsync(u => u.Email == searchRequest.Email);
        }

        // Ellers prøv med PhoneNumber
        if (customer == null && searchRequest.PhoneNumber != 0)
        {
            customer = await _ctx.Customers.AsNoTracking()
                .Include(c => c.SavedAddresses)
                .FirstOrDefaultAsync(u =>
                    u.PhoneNumber == searchRequest.PhoneNumber);
        }

        if (customer == null)
        {
            throw new KeyNotFoundException(
                $"Kunden kunne ikke findes.");
        }

        var fullName =
            $"{customer.FirstName} {customer.MiddleName}{(customer.MiddleName != null ? " " : "")}{customer.LastName}";


        return new CustomerProfileResponse(customer.Id,
            customer.Username, fullName, customer.Email,
            customer.PhoneNumber,
            customer.Nationality, customer.Balance,
            customer.LoyaltyGroup,
            customer.SavedAddresses.Select(a =>
                    new CustomerAddressResponse(a.Id, a.StreetAddress,
                        a.StreetNumber, a.AdditionalAddress, a.PostalCode,
                        a.City,
                        a.Country, a.IsDefault, a.CreatedAt, a.LastUpdated))
                .ToList(), customer.CreatedAt,
            customer.LastUpdated);
    }

    public async Task<List<CustomerOverviewResponse>> GetCustomers(Guid? id,
        string? username,
        string? fullName, string? email, long? phoneNumber, string? nationality,
        decimal? balance, Loyalty? loyaltyGroup, DateTime? createdAt,
        DateTime? lastUpdated)
    {
        var customers = await GetManyCustomers(id, username, fullName, email,
            phoneNumber, nationality, balance, loyaltyGroup, createdAt,
            lastUpdated);


        return customers.Select(customer =>
        {
            var customerFullName =
                $"{customer.FirstName} {customer.MiddleName}{(customer.MiddleName != null ? " " : "")}{customer.LastName}";
            return new CustomerOverviewResponse(customer.Id,
                customer.Username, customerFullName, customer.Email,
                customer.PhoneNumber,
                customer.Nationality, customer.Balance,
                customer.LoyaltyGroup,
                customer.CreatedAt,
                customer.LastUpdated
            );
        }).ToList();
    }

    // Filter helper method
    private async Task<List<Core.Entities.Customer>> GetManyCustomers(Guid? id,
        string? username,
        string? fullName, string? email, long? phoneNumber, string? nationality,
        decimal? balance, Loyalty? loyaltyGroup, DateTime? createdAt,
        DateTime? lastUpdated)
    {
        var query = _ctx.Customers.AsNoTracking().AsQueryable();

        if (id.HasValue)
        {
            query = query.Where(c => c.Id == id.Value);
        }

        if (!string.IsNullOrWhiteSpace(username))
        {
            query = query.Where(c =>
                EF.Functions.ILike(c.Username, username));
        }

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            query = query.Where(c =>
                c.MiddleName != null &&
                (EF.Functions.ILike(c.FirstName, fullName) ||
                 EF.Functions.ILike(c.MiddleName, fullName) ||
                 EF.Functions.ILike(c.LastName, fullName)));
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            query = query.Where(c =>
                EF.Functions.ILike(c.Email, email));
        }

        if (phoneNumber.HasValue)
        {
            query = query.Where(c => c.PhoneNumber == phoneNumber.Value);
        }


        if (!string.IsNullOrWhiteSpace(nationality))
        {
            query = query.Where(c =>
                c.Nationality != null &&
                EF.Functions.ILike(c.Nationality, nationality));
        }

        if (balance.HasValue)
        {
            query = query.Where(c => c.Balance == balance.Value);
        }

        if (loyaltyGroup.HasValue)
        {
            query = query.Where(c => c.LoyaltyGroup == loyaltyGroup.Value);
        }

        if (createdAt.HasValue)
        {
            DateTime startDate = createdAt.Value.Date;
            DateTime endDate = startDate.AddDays(1);
            query = query.Where(c =>
                c.CreatedAt >= startDate && c.CreatedAt < endDate);
        }

        if (lastUpdated.HasValue)
        {
            DateTime startDate = lastUpdated.Value.Date;
            DateTime endDate = startDate.AddDays(1);
            query = query.Where(c =>
                c.LastUpdated >= startDate && c.LastUpdated < endDate);
        }


        return await query.ToListAsync();
    }
}