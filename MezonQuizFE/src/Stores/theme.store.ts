import { create } from 'zustand'

type ThemeMode = 'light' | 'dark'

interface ThemeState {
  themeMode: ThemeMode
  setThemeMode: (mode: ThemeMode) => void
  toggleTheme: () => void
}

const useThemeStore = create<ThemeState>((set) => ({
  themeMode: 'dark',
  setThemeMode: (mode) => set({ themeMode: mode }),
  toggleTheme: () =>
    set((state) => ({
      themeMode: state.themeMode === 'light' ? 'dark' : 'light',
    })),
}))

export default useThemeStore
