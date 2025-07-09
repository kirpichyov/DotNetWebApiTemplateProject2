using SampleProject.Application.Models.Users;
using SampleProject.Core.Models.Entities;

namespace SampleProject.Application.Mapping;

public static class ModelMappingExtensions
{
    public static CurrentUserDataResponse ToCurrentUserDataResponse(this User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new CurrentUserDataResponse
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Role = EnumMapper.ToRoleModel(user.Role),
        };
    }
}