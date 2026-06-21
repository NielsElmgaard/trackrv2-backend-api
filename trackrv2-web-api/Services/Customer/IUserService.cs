

using trackrv2_shared;
using trackrv2_shared.DTOs.User;

namespace trackrv2_web_api.Services.User;

public interface IUserService
{
    Task<UserProfileResponse> RegisterUser(
        UserRequest userRequest);

    Task UpdateUserAsync(Guid id, UserRequest userRequest);

    Task DeleteUserAsync(Guid id);

    Task UpdateUserPasswordAsync(Guid id, string newPassword);

    Task UpdateUserRoleAsync(Guid id, Role newRole);

    // Admin role required
    Task<UserProfileResponse> GetUserById(Guid id);

    // Admin role required
    Task<UserProfileResponse> GetSingleUserBySearch(
        SingleUserSearchRequest searchRequest);

    // Admin role required
    Task<List<UserOverviewResponse>> GetUsers(Guid? id,
        string? username,
        string? fullName, string? email, long? phoneNumber, string? nationality,
        Role? role, DateTime? createdAt,
        DateTime? lastUpdated);
}