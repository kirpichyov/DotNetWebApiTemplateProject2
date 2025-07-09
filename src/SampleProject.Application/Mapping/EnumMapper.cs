using Riok.Mapperly.Abstractions;
using SampleProject.Application.Models.Users;
using SampleProject.Core.Models.Enums;

namespace SampleProject.Application.Mapping;

[Mapper(RequiredEnumMappingStrategy = RequiredMappingStrategy.Source)]
public static partial class EnumMapper
{
    [MapEnum(EnumMappingStrategy.ByName)]
    public static partial RoleModel ToRoleModel(Role role);
    
    [MapEnum(EnumMappingStrategy.ByName)]
    public static partial Role ToRole(RoleModel permission);
}