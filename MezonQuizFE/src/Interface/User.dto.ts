export interface CreateUserRequest {
  email?: string;
  username: string;
  password: string;
  displayName?: string;
  avatarUrl?: string;
}

export interface UpdateUserRequest {
  email?: string;
  displayName?: string;
  avatarUrl?: string;
  isActive: boolean;
}

export interface UserResponse {
  id: string;
  mezonUserId?: string;
  email?: string;
  username: string;
  displayName?: string;
  avatarUrl?: string;
  isActive: boolean;
  lastLoginAt?: string;
  createdAt: string;
  updatedAt: string;
}