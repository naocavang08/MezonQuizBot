const ACCESS_TOKEN_KEY = 'accessToken';
const HAS_SYSTEM_ROLE_KEY = 'hasSystemRole';

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
