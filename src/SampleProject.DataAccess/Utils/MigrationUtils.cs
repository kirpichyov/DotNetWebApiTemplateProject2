using Microsoft.EntityFrameworkCore.Migrations;

namespace SampleProject.DataAccess.Utils;

public static class MigrationUtils
{
    public static void AddOwnerPermissionJson(this MigrationBuilder migrationBuilder, string[] permissions)
    {
        var permissionsJoin = string.Join(", ", permissions.Select(p => $"\"{p}\""));
        
        migrationBuilder.Sql(
            $"""
             UPDATE workspace_roles
             SET permissions_json = permissions_json
                                    || '[{permissionsJoin}]'::jsonb
             WHERE is_immutable = true;
             """);
    }
}