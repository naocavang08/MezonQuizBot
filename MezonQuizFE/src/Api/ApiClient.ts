import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios';
import queryString from 'query-string';
import type { LoginResponse } from '../Interface/login.dto';
import useAuthStore from '../Stores/login.store';
import { getRefreshToken, getTokenAccess } from '../Lib/Utils/localStorage';

const baseURL = axios.create({
  baseURL: import.meta.env.VITE_QUIZ_API_URL ?? '',
  timeout: 5000,
  headers: {
    'Content-Type': 'application/json',
  },
  paramsSerializer: (params) => queryString.stringify(params),
});

let refreshPromise: Promise<string | null> | null = null;

baseURL.interceptors.request.use((request) => {
  const accessToken = getTokenAccess();

  request.headers = request.headers ?? {};

  if (accessToken) {
    request.headers['Authorization'] = `Bearer ${accessToken}`;
  } else {
    delete request.headers['Authorization'];
  }

  return request;
});

const clearAuthAndRedirectToLogin = () => {
  useAuthStore.getState().clearAuth();

  if (window.location.pathname !== '/login') {
    window.location.href = '/login';
  }
};

const performTokenRefresh = async (): Promise<string | null> => {
  const refreshToken = getRefreshToken();
  if (!refreshToken) {
    return null;
  }

  try {
    const response = await baseURL.post<LoginResponse>(
      '/api/Login/refresh',
      { refreshToken },
      { headers: { 'X-Skip-Auth-Refresh': 'true' } },
    );

    const payload = response.data;
    if (!payload?.token || !payload?.refreshToken) {
      return null;
    }

    useAuthStore.getState().setTokenBundle(payload.token, payload.refreshToken, payload.expiresIn);
    return payload.token;
  } catch {
    return null;
  }
};

baseURL.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as (InternalAxiosRequestConfig & { _retry?: boolean }) | undefined;
    const statusCode = error.response?.status;
    const skipAuthRefresh = String(originalRequest?.headers?.['X-Skip-Auth-Refresh'] ?? '') === 'true';

    if (statusCode === 401 && originalRequest && !originalRequest._retry && !skipAuthRefresh) {
      originalRequest._retry = true;

      refreshPromise ??= performTokenRefresh().finally(() => {
        refreshPromise = null;
      });

      const newAccessToken = await refreshPromise;

      if (newAccessToken) {
        originalRequest.headers = originalRequest.headers ?? {};
        originalRequest.headers['Authorization'] = `Bearer ${newAccessToken}`;
        return baseURL(originalRequest);
      }
    }

    if (statusCode === 401) {
      clearAuthAndRedirectToLogin();
    }

    return Promise.reject(error);
  },
);

export default baseURL;
