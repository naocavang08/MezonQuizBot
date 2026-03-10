import type { User } from "../../Interface/login.dto";

const ACCESS_TOKEN_KEY = 'accessToken';
const HAS_SYSTEM_ROLE_KEY = 'hasSystemRole';
const USER_KEY = 'user';

export const getTokenAccess = (): string | null => {
  try {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  } catch (error) {
    console.error('Error getting access token:', error);
    return null;
  }
};

export const setTokenAccess = (token: string): void => {
  try {
    localStorage.setItem(ACCESS_TOKEN_KEY, token);
  } catch (error) {
    console.error('Error setting access token:', error);
  }
};

export const removeTokenAccess = (): void => {
  try {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
  } catch (error) {
    console.error('Error removing access token:', error);
  }
};

export const getHasSystemRole = (): boolean => {
  try {
    return localStorage.getItem(HAS_SYSTEM_ROLE_KEY) === 'true';
  } catch (error) {
    console.error('Error getting hasSystemRole:', error);
    return false;
  }
};

export const setHasSystemRole = (hasSystemRole: boolean): void => {
  try {
    localStorage.setItem(HAS_SYSTEM_ROLE_KEY, String(hasSystemRole));
  } catch (error) {
    console.error('Error setting hasSystemRole:', error);
  }
};

export const removeHasSystemRole = (): void => {
  try {
    localStorage.removeItem(HAS_SYSTEM_ROLE_KEY);
  } catch (error) {
    console.error('Error removing hasSystemRole:', error);
  }
};

export const getUser = (): User | null => {
  try {
    const rawUser = localStorage.getItem(USER_KEY);
    if (!rawUser) {
      return null;
    }

    return JSON.parse(rawUser) as User;
  } catch (error) {
    console.error('Error getting user:', error);
    return null;
  }
};

export const setUser = (user: User): void => {
  try {
    localStorage.setItem(USER_KEY, JSON.stringify(user));
  } catch (error) {
    console.error('Error setting user:', error);
  }
};

export const removeUser = (): void => {
  try {
    localStorage.removeItem(USER_KEY);
  } catch (error) {
    console.error('Error removing user:', error);
  }
};
