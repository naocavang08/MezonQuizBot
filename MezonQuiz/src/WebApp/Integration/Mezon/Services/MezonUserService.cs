using Microsoft.EntityFrameworkCore;
using PbChannelMessage = Mezon.Net.Internal.Api.ChannelMessage;
using WebApp.Data;

namespace WebApp.Integration.Mezon.Services;

public class MezonUserService
{
    private readonly ILogger<MezonUserService> _logger;

    public MezonUserService(ILogger<MezonUserService> logger)
    {
        _logger = logger;
    }

    public async Task<Domain.Entites.User> ResolveOrCreateJoinUserAsync(AppDbContext dbContext, PbChannelMessage message, string senderId)
    {
        var incomingUsername = (message.Username ?? string.Empty).Trim();
        var normalizedIncomingUsername = incomingUsername.ToLowerInvariant();

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u =>
                u.MezonUserId == senderId ||
                (!string.IsNullOrWhiteSpace(incomingUsername) &&
                 u.Username.ToLower() == normalizedIncomingUsername));

        if (user is null)
        {
            var baseUsername = !string.IsNullOrWhiteSpace(incomingUsername)
                ? incomingUsername
                : $"mezon_{senderId}";

            var uniqueUsername = await GenerateUniqueUsernameAsync(dbContext, baseUsername);
            var now = DateTime.UtcNow;

            user = new Domain.Entites.User
            {
                MezonUserId = senderId,
                Username = uniqueUsername,
                DisplayName = string.IsNullOrWhiteSpace(message.DisplayName) ? null : message.DisplayName.Trim(),
                AvatarUrl = string.IsNullOrWhiteSpace(message.Avatar) ? null : message.Avatar.Trim(),
                IsActive = true,
                LastLoginAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Provisioned local user {UserId} for Mezon sender {SenderId} during /join.",
                user.Id,
                senderId);

            return user;
        }

        var hasChanges = false;
        if (string.IsNullOrWhiteSpace(user.MezonUserId))
        {
            user.MezonUserId = senderId;
            hasChanges = true;

            _logger.LogInformation(
                "Linked local user {UserId} with Mezon user id {SenderId} during /join.",
                user.Id,
                senderId);
        }

        if (!user.IsActive)
        {
            user.IsActive = true;
            hasChanges = true;
        }

        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        hasChanges = true;

        if (hasChanges)
        {
            await dbContext.SaveChangesAsync();
        }

        return user;
    }

    private static async Task<string> GenerateUniqueUsernameAsync(AppDbContext dbContext, string baseUsername)
    {
        var sanitizedBase = string.IsNullOrWhiteSpace(baseUsername)
            ? "mezon_user"
            : baseUsername.Trim();

        if (sanitizedBase.Length > 255)
        {
            sanitizedBase = sanitizedBase.Substring(0, 255);
        }

        var uniqueUsername = sanitizedBase;
        var suffix = 1;

        while (await dbContext.Users.AnyAsync(u => u.Username == uniqueUsername))
        {
            var suffixText = $"_{suffix}";
            var maxBaseLength = Math.Max(1, 255 - suffixText.Length);
            var shortenedBase = sanitizedBase.Length > maxBaseLength
                ? sanitizedBase.Substring(0, maxBaseLength)
                : sanitizedBase;

            uniqueUsername = $"{shortenedBase}{suffixText}";
            suffix++;
        }

        return uniqueUsername;
    }
}
