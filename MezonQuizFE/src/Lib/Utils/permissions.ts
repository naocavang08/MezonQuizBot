export const PERMISSIONS = {
  USERS_LIST: 'users.list',
  USERS_VIEW: 'users.view',
  USERS_CREATE: 'users.create',
  USERS_UPDATE: 'users.update',
  USERS_DELETE: 'users.delete',
  USERS_ASSIGN_ROLE: 'users.assign_role',
  ROLES_LIST: 'roles.list',
  ROLES_VIEW: 'roles.view',
  ROLES_CREATE: 'roles.create',
  ROLES_UPDATE: 'roles.update',
  ROLES_DELETE: 'roles.delete',
  ROLES_ASSIGN_PERMISSION: 'roles.assign_permission',
  QUIZZES_ADMIN_LIST: 'quizzes.admin_list',
  QUIZZES_CREATOR_LIST: 'quizzes.creator_list',
  QUIZZES_PLAYER_LIST: 'quizzes.player_list',
  QUIZZES_ADMIN_VIEW: 'quizzes.admin_view',
  QUIZZES_CREATOR_VIEW: 'quizzes.creator_view',
  QUIZZES_PLAYER_VIEW: 'quizzes.player_view',
  QUIZZES_CREATE: 'quizzes.create',
  QUIZZES_UPDATE: 'quizzes.update',
  QUIZZES_DELETE: 'quizzes.delete',
  QUIZZES_PUBLISH: 'quizzes.publish',
  QUIZZES_MODERATE: 'quizzes.moderate',
  CATEGORIES_ADMIN_LIST: 'categories.admin_list',
  CATEGORIES_CREATOR_LIST: 'categories.creator_list',
  CATEGORIES_PLAYER_LIST: 'categories.player_list',
  CATEGORIES_CREATE: 'categories.create',
  CATEGORIES_UPDATE: 'categories.update',
  CATEGORIES_DELETE: 'categories.delete',
  SESSIONS_ADMIN_LIST: 'sessions.admin_list',
  SESSIONS_CREATOR_LIST: 'sessions.creator_list',
  SESSIONS_PLAYER_LIST: 'sessions.player_list',
  SESSIONS_ADMIN_VIEW: 'sessions.admin_view',
  SESSIONS_CREATOR_VIEW: 'sessions.creator_view',
  SESSIONS_PLAYER_VIEW: 'sessions.player_view',
  SESSIONS_CREATE: 'sessions.create',
  SESSIONS_MODERATE: 'sessions.moderate',
  SESSIONS_DELETE: 'sessions.delete',
  REPORTS_VIEW: 'reports.view',
  REPORTS_EXPORT: 'reports.export',
  AUDIT_LOGS_LIST: 'audit_logs.list',
  AUDIT_LOGS_VIEW: 'audit_logs.view',
} as const

export const ACCESS_PERMISSIONS = {
  DASHBOARD: [PERMISSIONS.REPORTS_VIEW],
  QUIZ_MANAGEMENT_PAGE: [PERMISSIONS.QUIZZES_CREATOR_LIST],
  QUIZ_WORKSPACE: [
    PERMISSIONS.QUIZZES_ADMIN_LIST,
    PERMISSIONS.QUIZZES_CREATOR_LIST,
    PERMISSIONS.QUIZZES_PLAYER_LIST,
  ],
  CATEGORY_PAGE: [
    PERMISSIONS.CATEGORIES_ADMIN_LIST,
    PERMISSIONS.CATEGORIES_CREATOR_LIST,
    PERMISSIONS.CATEGORIES_PLAYER_LIST,
  ],
  SESSION_ROOM: [
    PERMISSIONS.SESSIONS_ADMIN_VIEW,
    PERMISSIONS.SESSIONS_CREATOR_VIEW,
    PERMISSIONS.SESSIONS_PLAYER_VIEW,
  ],
} as const

export function hasAnyPermission(
  userPermissions: string[],
  requiredPermissions?: readonly string[],
  hasSystemRole = false,
): boolean {
  if (hasSystemRole) {
    return true
  }

  if (!requiredPermissions || requiredPermissions.length === 0) {
    return true
  }

  return requiredPermissions.some((permission) => userPermissions.includes(permission))
}

export function resolveDefaultAppPath(
  userPermissions: string[],
  hasSystemRole = false,
): string {
  if (hasSystemRole && hasAnyPermission(userPermissions, ACCESS_PERMISSIONS.DASHBOARD, hasSystemRole)) {
    return '/app/dashboard'
  }

  if (hasAnyPermission(userPermissions, [PERMISSIONS.QUIZZES_CREATOR_LIST], hasSystemRole)) {
    return '/app/my-quizzes'
  }

  if (hasAnyPermission(userPermissions, ACCESS_PERMISSIONS.QUIZ_WORKSPACE, hasSystemRole)) {
    return '/app/find-quizzes'
  }

  if (hasSystemRole && hasAnyPermission(userPermissions, ACCESS_PERMISSIONS.CATEGORY_PAGE, hasSystemRole)) {
    return '/app/categories'
  }

  if (hasSystemRole && hasAnyPermission(userPermissions, [PERMISSIONS.USERS_LIST], hasSystemRole)) {
    return '/app/users'
  }

  if (hasSystemRole && hasAnyPermission(userPermissions, [PERMISSIONS.ROLES_LIST], hasSystemRole)) {
    return '/app/roles'
  }

  return '/app/find-quizzes'
}
