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
  QUIZZES_LIST: 'quizzes.list',
  QUIZZES_VIEW: 'quizzes.view',
  QUIZZES_CREATE: 'quizzes.create',
  QUIZZES_UPDATE: 'quizzes.update',
  QUIZZES_DELETE: 'quizzes.delete',
  QUIZZES_PUBLISH: 'quizzes.publish',
  QUIZZES_MODERATE: 'quizzes.moderate',
  CATEGORIES_LIST: 'categories.list',
  CATEGORIES_CREATE: 'categories.create',
  CATEGORIES_UPDATE: 'categories.update',
  CATEGORIES_DELETE: 'categories.delete',
  SESSIONS_VIEW: 'sessions.view',
} as const

export function hasAnyPermission(
  userPermissions: string[],
  requiredPermissions?: string[],
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
