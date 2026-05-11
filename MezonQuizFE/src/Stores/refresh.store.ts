import { create } from "zustand";

interface RefreshStore {
  refreshKeys: Record<string, number>;
  triggerRefresh: (key: string) => void;
  getRefreshKey: (key: string) => number;
}

const useRefreshStore = create<RefreshStore>((set, get) => ({
  refreshKeys: {},
  triggerRefresh: (key) =>
    set((state) => ({
      refreshKeys: {
        ...state.refreshKeys,
        [key]: (state.refreshKeys[key] ?? 0) + 1,
      },
    })),
  getRefreshKey: (key) => get().refreshKeys[key] ?? 0,
}));

export default useRefreshStore;
