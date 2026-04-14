namespace WebApp.Application.Auth.Authorization;

public static class PermissionNames
{
    public static class Users
    {
        public const string List = "users.list";
        public const string View = "users.view";
        public const string Create = "users.create";
        public const string Update = "users.update";
        public const string Delete = "users.delete";
        public const string AssignRole = "users.assign_role";
    }

    public static class Roles
    {
        public const string List = "roles.list";
        public const string View = "roles.view";
        public const string Create = "roles.create";
        public const string Update = "roles.update";
        public const string Delete = "roles.delete";
        public const string AssignPermission = "roles.assign_permission";
    }

    public static class Quizzes
    {
        public const string Admin_List = "quizzes.admin_list";
        public const string Creator_List = "quizzes.creator_list";
        public const string Player_List = "quizzes.player_list";
        public const string Admin_View = "quizzes.admin_view";
        public const string Creator_View = "quizzes.creator_view";
        public const string Player_View = "quizzes.player_view";
        public const string Create = "quizzes.create";
        public const string Update = "quizzes.update";
        public const string Delete = "quizzes.delete";
        public const string Publish = "quizzes.publish";
        public const string Moderate = "quizzes.moderate";
    }

    public static class Categories
    {
        public const string Admin_List = "categories.admin_list";
        public const string Creator_List = "categories.creator_list";
        public const string Player_List = "categories.player_list";
        public const string Create = "categories.create";
        public const string Update = "categories.update";
        public const string Delete = "categories.delete";
    }

    public static class Sessions
    {
        public const string Admin_List = "sessions.admin_list";
        public const string Creator_List = "sessions.creator_list";
        public const string Player_List = "sessions.player_list";
        public const string Admin_View = "sessions.admin_view";
        public const string Creator_View = "sessions.creator_view";
        public const string Player_View = "sessions.player_view";
        public const string Create = "sessions.create";
        public const string Moderate = "sessions.moderate";
        public const string Delete = "sessions.delete";
    }

    public static class Reports
    {
        public const string View = "reports.view";
        public const string Export = "reports.export";
    }

    public static class AuditLogs
    {
        public const string List = "audit_logs.list";
        public const string View = "audit_logs.view";
    }

    public static class Settings
    {
        public const string View = "settings.view";
        public const string Update = "settings.update";
    }
}
