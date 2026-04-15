import type { LoginResponse } from "../Interface/login.dto";
import apiClient from "./ApiClient";

export const login = (body: {
  username: string;
  password: string;
}) => {
  return apiClient
    .post<LoginResponse>('/api/Login', body)
    .then((res) => res.data);
};

export const mezonCallbackLogin = (body: {
  code: string;
  state: string;
}) => {
  return apiClient
    .post<LoginResponse>('/api/Login/mezon-callback', body)
    .then((res) => res.data);
};

export const refreshLogin = (refreshToken: string) => {
  return apiClient
    .post<LoginResponse>('/api/Login/refresh', { refreshToken }, { headers: { 'X-Skip-Auth-Refresh': 'true' } })
    .then((res) => res.data);
};

export const mezonAuthorize = () => {
  return apiClient
    .get<{ authorizeUrl?: string; AuthorizeUrl?: string }>('/api/Login/mezon-authorize')
    .then((res) => res.data);
};
