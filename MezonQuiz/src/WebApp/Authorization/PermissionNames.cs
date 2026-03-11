namespace WebApp.Authorization;

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
    }

    public static class Quizzes
    {
        public const string List = "quizzes.list";
        public const string View = "quizzes.view";
        public const string Create = "quizzes.create";
        public const string Update = "quizzes.update";
        public const string Delete = "quizzes.delete";
        public const string Publish = "quizzes.publish";
        public const string Moderate = "quizzes.moderate";
    }

    public static class Sessions
    {
        public const string List = "sessions.list";
        public const string View = "sessions.view";
        public const string Create = "sessions.create";
        public const string Start = "sessions.start";
        public const string End = "sessions.end";
        public const string Delete = "sessions.delete";
    }

    public static class Reports {
        public const string View = "reports.view";
        public const string Export = "reports.export";
    }

    public static class AuditLogs {
        public const string List = "audit_logs.list";
        public const string View = "audit_logs.view";
    }

    public static class Settings {
        public const string View = "settings.view";
        public const string Update = "settings.update";
    }
}
