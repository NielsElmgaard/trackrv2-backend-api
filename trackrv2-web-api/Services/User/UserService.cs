using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using trackrv2_efc;
using trackrv2_shared;
using trackrv2_shared.DTOs;
using trackrv2_shared.DTOs.User;

namespace trackrv2_web_api.Services.User;

public class UserService : IUserService
{
    private readonly TrackrContext _ctx;
    private readonly IMemoryCache _cache;
    private const string UserCachePrefix = "user_";

    private readonly IPasswordHasher<trackrv2_efc.Entities.User> _passwordHasher;

    public UserService(
        TrackrContext ctx,
        IPasswordHasher<trackrv2_efc.Entities.User> passwordHasher,
        IMemoryCache cache
    )
    {
        _ctx = ctx;
        _passwordHasher = passwordHasher;
        _cache = cache;
    }

    public async Task<UserProfileResponse> RegisterUserAsync(UserRequest userRequest)
    {
        var usernameExists = await _ctx
            .Users.AsNoTracking()
            .AnyAsync(u => u.Username == userRequest.Username);

        if (usernameExists)
        {
            throw new InvalidOperationException(
                $"Brugernavnet '{userRequest.Username}' er allerede taget."
            );
        }

        var emailExists = await _ctx
            .Users.AsNoTracking()
            .AnyAsync(u => u.Email == userRequest.Email);

        if (emailExists)
        {
            throw new InvalidOperationException(
                $"E-mailen '{userRequest.Email}' er allerede taget."
            );
        }

        var phoneNumberExists = await _ctx
            .Users.AsNoTracking()
            .AnyAsync(u => u.PhoneNumber == userRequest.PhoneNumber);

        if (phoneNumberExists)
        {
            throw new InvalidOperationException(
                $"Telefonnummeret '{userRequest.PhoneNumber}' er allerede taget."
            );
        }

        var user = new trackrv2_efc.Entities.User
        {
            Username = userRequest.Username,
            Email = userRequest.Email,
            Password = "",
            Nationality = userRequest.Nationality,
            FirstName = userRequest.FirstName,
            MiddleName = userRequest.MiddleName,
            LastName = userRequest.LastName,
            PhoneNumber = userRequest.PhoneNumber,
            Roles = Role.User,
        };
        user.Password = _passwordHasher.HashPassword(user, userRequest.Password);
        var addedUser = await _ctx.Users.AddAsync(user);
        await _ctx.SaveChangesAsync();
        var addedUserEntity = addedUser.Entity;
        // var fullName = $"{addedUserEntity.FirstName} {addedUserEntity.MiddleName}{(addedUserEntity.MiddleName != null ? " " : "")}{addedUserEntity.LastName}";

        return new UserProfileResponse(
            addedUserEntity.Id,
            addedUserEntity.Username,
            addedUserEntity.FirstName,
            addedUserEntity.MiddleName!,
            addedUserEntity.LastName,
            addedUserEntity.Email,
            addedUserEntity.PhoneNumber,
            addedUserEntity.Nationality,
            addedUserEntity.Roles,
            addedUserEntity.CreatedAt,
            addedUserEntity.LastUpdated,
            new List<TrackerOverviewResponse>()
        );
    }

    // Basic User Info update
    public async Task UpdateUserAsync(Guid id, UserInfoUpdateRequest userRequest)
    {
        var user = await _ctx.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            throw new KeyNotFoundException($"En bruger med id'et: '{id}' kunne ikke findes.");
        }

        // If updating Username make sure it is not taken already
        if (user.Username != userRequest.Username)
        {
            var usernameExists = await _ctx
                .Users.AsNoTracking()
                .AnyAsync(c => c.Username == userRequest.Username);

            if (usernameExists)
            {
                throw new InvalidOperationException(
                    $"Brugernavnet '{userRequest.Username}' er allerede taget."
                );
            }
        }

        // If updating Email make sure it is not taken already
        if (user.Email != userRequest.Email)
        {
            var emailExists = await _ctx
                .Users.AsNoTracking()
                .AnyAsync(c => c.Email == userRequest.Email);

            if (emailExists)
            {
                throw new InvalidOperationException(
                    $"E-mailen '{userRequest.Email}' er allerede taget."
                );
            }
        }

        // If updating PhoneNumber make sure it is not taken already
        if (user.PhoneNumber != userRequest.PhoneNumber)
        {
            var phoneNumberExists = await _ctx
                .Users.AsNoTracking()
                .AnyAsync(c => c.PhoneNumber == userRequest.PhoneNumber);

            if (phoneNumberExists)
            {
                throw new InvalidOperationException(
                    $"Telefonnummeret '{userRequest.PhoneNumber}' er allerede taget."
                );
            }
        }

        user.Username = userRequest.Username;
        user.Email = userRequest.Email;
        user.FirstName = userRequest.FirstName;
        user.MiddleName = userRequest.MiddleName;
        user.LastName = userRequest.LastName;
        user.Nationality = userRequest.Nationality;
        user.PhoneNumber = userRequest.PhoneNumber;

        await _ctx.SaveChangesAsync();

        string cacheKey = $"{UserCachePrefix}{id}";
        _cache.Remove(cacheKey);
    }

    public async Task<List<SearchUserResponse>> GetSearchUsersAsync(
        string? username,
        string? firstName,
        string? middleName,
        string? lastName,
        string? nationality
    )
    {
        var users = await GetManySearchUsers(
            username,
            firstName,
            middleName,
            lastName,
            nationality
        );

        return users
            .Select(user =>
            {
                return new SearchUserResponse(
                    user.Id,
                    user.Username,
                    user.FirstName,
                    user.MiddleName!,
                    user.LastName,
                    user.Nationality
                );
            })
            .ToList();
    }

    public async Task DeleteUserAsync(Guid id)
    {
        var existingUser = await _ctx.Users.FindAsync(id);
        if (existingUser != null)
        {
            _ctx.Users.Remove(existingUser);
            await _ctx.SaveChangesAsync();
        }

        string cacheKey = $"{UserCachePrefix}{id}";
        _cache.Remove(cacheKey);
    }

    public async Task UpdateUserPasswordAsync(Guid id, UpdatePasswordRequest request)
    {
        var user = await _ctx.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            throw new KeyNotFoundException($"En bruger med id'et: '{id}' kunne ikke findes.");
        }

        user.Password = _passwordHasher.HashPassword(user, request.Password);

        await _ctx.SaveChangesAsync();

        string cacheKey = $"{UserCachePrefix}{id}";
        _cache.Remove(cacheKey);
    }

    public async Task UpdateUserRolesAsync(Guid id, UpdateUserRolesRequest request)
    {
        var user = await _ctx.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            throw new KeyNotFoundException($"En bruger med id'et: '{id}' kunne ikke findes.");
        }

        user.Roles = request.Roles;

        await _ctx.SaveChangesAsync();

        string cacheKey = $"{UserCachePrefix}{id}";
        _cache.Remove(cacheKey);
    }

    public async Task<UserProfileResponse> GetUserByIdAsync(Guid id)
    {
        string cacheKey = $"{UserCachePrefix}{id}";

        return (
            await _cache.GetOrCreateAsync(
                cacheKey,
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

                    var user = await _ctx
                        .Users.AsNoTracking()
                        .Include(u => u.Trackers)
                        .FirstOrDefaultAsync(us => us.Id == id);

                    if (user == null)
                    {
                        throw new KeyNotFoundException(
                            $"En bruger med id'et: '{id}' kunne ikke findes."
                        );
                    }

                    // var fullName = $"{user.FirstName} {user.MiddleName}{(user.MiddleName != null ? " " : "")}{user.LastName}";

                    return new UserProfileResponse(
                        user.Id,
                        user.Username,
                        user.FirstName,
                        user.MiddleName!,
                        user.LastName,
                        user.Email,
                        user.PhoneNumber,
                        user.Nationality,
                        user.Roles,
                        user.CreatedAt,
                        user.LastUpdated,
                        user.Trackers.Select(t => new TrackerOverviewResponse(
                            t.Id,
                            t.Name,
                            t.CreatedAt,
                            t.LastUpdated
                        ))
                    );
                }
            )
        )!;
    }

    // Search for everything unique about a user
    public async Task<UserProfileResponse> GetSingleUserBySearchAsync(
        SingleUserSearchRequest searchRequest
    )
    {
        trackrv2_efc.Entities.User? user = null;

        // Username har højeste prioritet
        if (!string.IsNullOrEmpty(searchRequest.Username))
        {
            user = await _ctx
                .Users.AsNoTracking()
                .Include(us => us.Trackers)
                .FirstOrDefaultAsync(u => u.Username == searchRequest.Username);
        }

        // Ellers prøv med Email
        if (user == null && !string.IsNullOrEmpty(searchRequest.Email))
        {
            user = await _ctx
                .Users.AsNoTracking()
                .Include(us => us.Trackers)
                .FirstOrDefaultAsync(u => u.Email == searchRequest.Email);
        }

        // Ellers prøv med PhoneNumber
        if (user == null && searchRequest.PhoneNumber != 0)
        {
            user = await _ctx
                .Users.AsNoTracking()
                .Include(us => us.Trackers)
                .FirstOrDefaultAsync(u => u.PhoneNumber == searchRequest.PhoneNumber);
        }

        if (user == null)
        {
            throw new KeyNotFoundException($"Kunden kunne ikke findes.");
        }

        // var fullName = $"{user.FirstName} {user.MiddleName}{(user.MiddleName != null ? " " : "")}{user.LastName}";

        return new UserProfileResponse(
            user.Id,
            user.Username,
            user.FirstName,
            user.MiddleName!,
            user.LastName,
            user.Email,
            user.PhoneNumber,
            user.Nationality,
            user.Roles,
            user.CreatedAt,
            user.LastUpdated,
            user.Trackers.Select(t => new TrackerOverviewResponse(
                t.Id,
                t.Name,
                t.CreatedAt,
                t.LastUpdated
            ))
        );
    }

    public async Task<List<UserOverviewResponse>> GetUsersAsync(
        Guid? id,
        string? username,
        string? firstName,
        string? middleName,
        string? lastName,
        string? email,
        long? phoneNumber,
        string? nationality,
        Role? role,
        DateTime? createdAt,
        DateTime? lastUpdated
    )
    {
        var users = await GetManyUsers(
            id,
            username,
            firstName,
            middleName,
            lastName,
            email,
            phoneNumber,
            nationality,
            role,
            createdAt,
            lastUpdated
        );

        return users
            .Select(user =>
            {
                // var userFullName = $"{user.FirstName} {user.MiddleName}{(user.MiddleName != null ? " " : "")}{user.LastName}";
                return new UserOverviewResponse(
                    user.Id,
                    user.Username,
                    user.FirstName,
                    user.MiddleName!,
                    user.LastName,
                    user.Email,
                    user.PhoneNumber,
                    user.Nationality,
                    user.Roles,
                    user.CreatedAt,
                    user.LastUpdated
                );
            })
            .ToList();
    }

    // Filter helper method
    private async Task<List<trackrv2_efc.Entities.User>> GetManyUsers(
        Guid? id,
        string? username,
        string? firstName,
        string? middleName,
        string? lastName,
        string? email,
        long? phoneNumber,
        string? nationality,
        Role? role,
        DateTime? createdAt,
        DateTime? lastUpdated
    )
    {
        var query = _ctx.Users.AsNoTracking().AsQueryable();

        if (id.HasValue)
        {
            query = query.Where(u => u.Id == id.Value);
        }

        if (!string.IsNullOrWhiteSpace(username))
        {
            query = query.Where(u => EF.Functions.ILike(u.Username, username));
        }

        if (!string.IsNullOrWhiteSpace(firstName))
        {
            query = query.Where(u => EF.Functions.ILike(u.FirstName, firstName));
        }

        if (!string.IsNullOrWhiteSpace(middleName))
        {
            query = query.Where(u => EF.Functions.ILike(u.MiddleName!, middleName));
        }

        if (!string.IsNullOrWhiteSpace(lastName))
        {
            query = query.Where(u => EF.Functions.ILike(u.LastName, lastName));
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            query = query.Where(u => EF.Functions.ILike(u.Email, email));
        }

        if (phoneNumber.HasValue)
        {
            query = query.Where(u => u.PhoneNumber == phoneNumber.Value);
        }

        if (!string.IsNullOrWhiteSpace(nationality))
        {
            query = query.Where(u =>
                u.Nationality != null && EF.Functions.ILike(u.Nationality, nationality)
            );
        }

        if (role.HasValue)
        {
            query = query.Where(u => (u.Roles & role.Value) == role.Value);
        }

        if (createdAt.HasValue)
        {
            DateTime startDate = createdAt.Value.Date;
            DateTime endDate = startDate.AddDays(1);
            query = query.Where(u => u.CreatedAt >= startDate && u.CreatedAt < endDate);
        }

        if (lastUpdated.HasValue)
        {
            DateTime startDate = lastUpdated.Value.Date;
            DateTime endDate = startDate.AddDays(1);
            query = query.Where(u => u.LastUpdated >= startDate && u.LastUpdated < endDate);
        }

        return await query.ToListAsync();
    }

    // Search for users helper method
    private async Task<List<trackrv2_efc.Entities.User>> GetManySearchUsers(
        string? username,
        string? firstName,
        string? middleName,
        string? lastName,
        string? nationality
    )
    {
        var query = _ctx.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(username))
        {
            query = query.Where(u => EF.Functions.ILike(u.Username, username));
        }

        if (!string.IsNullOrWhiteSpace(firstName))
        {
            query = query.Where(u => EF.Functions.ILike(u.FirstName, firstName));
        }

        if (!string.IsNullOrWhiteSpace(middleName))
        {
            query = query.Where(u => EF.Functions.ILike(u.MiddleName!, middleName));
        }

        if (!string.IsNullOrWhiteSpace(lastName))
        {
            query = query.Where(u => EF.Functions.ILike(u.LastName, lastName));
        }

        if (!string.IsNullOrWhiteSpace(nationality))
        {
            query = query.Where(u =>
                u.Nationality != null && EF.Functions.ILike(u.Nationality, nationality)
            );
        }

        return await query.ToListAsync();
    }
}
