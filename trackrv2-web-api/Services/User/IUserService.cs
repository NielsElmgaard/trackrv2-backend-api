

using trackrv2_shared;
using trackrv2_shared.DTOs.User;

namespace trackrv2_web_api.Services.User;

public interface IUserService
{
    Task<UserProfileResponse> RegisterUserAsync(
        UserRequest userRequest);

    Task UpdateUserAsync(Guid id, UserInfoUpdateRequest userRequest);

    Task DeleteUserAsync(Guid id);

    Task UpdateUserPasswordAsync(Guid id, string newPassword);

    Task UpdateUserRolesAsync(Guid id, UpdateUserRolesRequest request);

    // Admin role required
    Task<UserProfileResponse> GetUserByIdAsync(Guid id);

    // Admin role required
    Task<UserProfileResponse> GetSingleUserBySearchAsync(
        SingleUserSearchRequest searchRequest);

    // Admin role required
    Task<List<UserOverviewResponse>> GetUsersAsync(Guid? id,
        string? username,
        string? firstName,string? middleName, string? lastName, string? email, long? phoneNumber, string? nationality,
        Role? role, DateTime? createdAt,
        DateTime? lastUpdated);
}