using WebApp.Application.Auth.Users.Dtos;

namespace WebApp.Application.Auth.Users
{
    public interface IUserService
    {
        Task<List<UserDto>> GetAllUsersAsync();
        Task<UserDto> GetUserByIdAsync(Guid id);
        Task<UserDto> CreateUserAsync(CreateUserRequestDto request);
        Task<UserDto> UpdateUserAsync(Guid id, UpdateUserRequestDto request);
        Task<(bool Success, string Message, string? Url)> UploadAvatarAsync(IFormFile? file, HttpRequest request);
        Task DeleteUserAsync(Guid id);

        // Role management
        Task<List<Guid>> GetUserRoleIdsAsync(Guid userId);
        Task AssignRolesToUserAsync(Guid assignedId, Guid userId, List<Guid> roleIds);
    }
}
