export interface LoginResponse {
  token: string;
  user: User;
}

export interface User {
  Id: string;
  UserName: string;
  Email?: string;
  DisplayName?: string;
  AvatarUrl?: string;
}
