import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios';
import queryString from 'query-string';
import type { LoginResponse } from '../Interface/login.dto';
import { PUBLIC_HOME_PATH } from '../Lib/Utils/permissions';
import useAuthStore from '../Stores/login.store';
import { getRefreshToken, getTokenAccess } from '../Lib/Utils/localStorage';
import { getApiBaseUrl } from './apiBaseUrl';

const baseURL = axios.create({
  baseURL: getApiBaseUrl(),
  timeout: 20000,
  headers: {
    'Content-Type': 'application/json',
  },
  paramsSerializer: (params) => queryString.stringify(params),
});

let refreshPromise: Promise<string | null> | null = null;

type ErrorPayload = {
  message?: string;
  Message?: string;
  detail?: string;
  title?: string;
  errors?: Record<string, string[] | string | undefined>;
};

const extractValidationMessage = (errors?: ErrorPayload['errors']) => {
  if (!errors) {
    return null;
  }

  for (const value of Object.values(errors)) {
    if (Array.isArray(value) && value.length > 0) {
      return value[0];
    }

    if (typeof value === 'string' && value.trim().length > 0) {
      return value;
    }
  }

  return null;
};

export const getApiErrorMessage = (error: unknown, fallbackMessage: string) => {
  if (axios.isAxiosError(error)) {
    const payload = error.response?.data as ErrorPayload | string | undefined;

    if (typeof payload === 'string' && payload.trim().length > 0) {
      return payload;
    }

    if (payload && typeof payload === 'object') {
      const directMessage =
        payload.message?.trim() ||
        payload.Message?.trim() ||
        payload.detail?.trim() ||
        extractValidationMessage(payload.errors) ||
        payload.title?.trim();

      if (directMessage) {
        return directMessage;
      }
    }

    if (typeof error.message === 'string' && error.message.trim().length > 0) {
      return error.message;
    }
  }

  if (error instanceof Error && error.message.trim().length > 0) {
    return error.message;
  }

  return fallbackMessage;
};

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

const clearAuthAndRedirectToPublicHome = () => {
  useAuthStore.getState().clearAuth();

  if (window.location.pathname !== PUBLIC_HOME_PATH) {
    window.location.href = PUBLIC_HOME_PATH;
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
      clearAuthAndRedirectToPublicHome();
    }

    return Promise.reject(error);
  },
);

export default baseURL;
