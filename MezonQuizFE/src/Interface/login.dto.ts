export interface LoginResponse {
  token: string;
  refreshToken: string;
  expiresIn: number;
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

