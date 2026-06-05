export const getApiBaseUrl = () => {
  const configuredUrl = import.meta.env.VITE_MEZON_API_URL?.trim() ?? '';
  return configuredUrl.replace(/\/+$/, '');
};
