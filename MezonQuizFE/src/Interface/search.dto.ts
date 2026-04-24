export interface SearchItem {
  id: string;
  title: string;
  type: string;
  url: string;
  description?: string;
}

export interface GlobalSearchResult {
  quizzes: SearchItem[];
  categories: SearchItem[];
}
