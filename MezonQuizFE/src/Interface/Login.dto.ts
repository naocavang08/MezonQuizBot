export interface LoginResponse {
  token: string;
  user: User;
  roleName?: string[];
  permissionName?: string[];
  hasSystemRole?: boolean;
}

export interface User {
  id: string;
  username: string;
  email?: string;
  displayName?: string;
  avatarUrl?: string;
}

