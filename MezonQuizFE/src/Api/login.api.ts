import type { LoginResponse } from "../Interface/login.dto";
import { setTokenAccess } from "../Lib/Utils/localStorage";
import apiClient from "./ApiClient";

export const login = (body: {
  username: string;
  password: string;
}) => {
  return apiClient
    .post<LoginResponse>('/api/Login', body)
    .then((res) => {
      const token = res.data?.token;
      if (token) setTokenAccess(token);
      return res.data;
    });
};

export const mezonCallbackLogin = (body: {
  code: string;
  state: string;
}) => {
  return apiClient
    .post<LoginResponse>('/api/Login/mezon-callback', body)
    .then((res) => {
      const token = res.data?.token;
      if (token) setTokenAccess(token);
      return res.data;
    });
};

export const mezonAuthorize = () => {
  return apiClient
    .get<{ authorizeUrl?: string; AuthorizeUrl?: string }>('/api/Login/mezon-authorize')
    .then((res) => res.data);
};
