import baseURL from './ApiClient';
import type { GlobalSearchResult } from '../Interface/search.dto';

export const searchGlobal = async (keyword: string, limit = 5): Promise<GlobalSearchResult> => {
  const response = await baseURL.get<GlobalSearchResult>('/api/search/global', {
    params: { keyword, limit },
  });
  return response.data;
};
