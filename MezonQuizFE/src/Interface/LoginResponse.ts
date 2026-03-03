export interface LoginResponse {
  token: string;
  user: User;
}

export interface User {
  id: string;
  username: string;
  email?: string;
  displayName?: string;
  avatarUrl?: string;
}
