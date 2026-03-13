import { useMemo } from 'react'
import { BrowserRouter } from 'react-router-dom'
import { ThemeProvider, createTheme, CssBaseline, alpha } from '@mui/material'
import AppRoutes from './Routes/Index'
import useThemeStore from './Stores/theme.store'

function App() {
  const themeMode = useThemeStore((state) => state.themeMode)

  const colors = useMemo(
    () =>
      themeMode === 'dark'
        ? {
            defaultBg: '#08111f',
            paperBg: '#0e1a2b',
            sideBg: '#0a1626',
            textPrimary: '#e2e8f0',
            textSecondary: '#94a3b8',
            border: 'rgba(148,163,184,0.18)',
            fieldBg: 'rgba(15, 23, 42, 0.8)',
            bodyBg:
              'radial-gradient(circle at 10% 0%, rgba(14,165,233,0.22), transparent 36%), radial-gradient(circle at 95% 0%, rgba(251,146,60,0.16), transparent 40%), #08111f',
          }
        : {
            defaultBg: '#eef4fb',
            paperBg: '#ffffff',
            sideBg: '#f8fbff',
            textPrimary: '#0f172a',
            textSecondary: '#475569',
            border: 'rgba(71,85,105,0.22)',
            fieldBg: 'rgba(255, 255, 255, 0.95)',
            bodyBg:
              'radial-gradient(circle at 10% 0%, rgba(14,165,233,0.16), transparent 38%), radial-gradient(circle at 95% 0%, rgba(251,146,60,0.12), transparent 40%), #eef4fb',
          },
    [themeMode],
  )

  const theme = useMemo(
    () =>
      createTheme({
        palette: {
          mode: themeMode,
          primary: { main: '#0ea5e9' },
          secondary: { main: '#fb923c' },
          background: {
            default: colors.defaultBg,
            paper: colors.paperBg,
          },
          text: {
            primary: colors.textPrimary,
            secondary: colors.textSecondary,
          },
        },
        shape: { borderRadius: 14 },
        typography: {
          fontFamily: 'Segoe UI, Tahoma, Geneva, Verdana, sans-serif',
        },
        components: {
          MuiPaper: {
            styleOverrides: {
              root: {
                backgroundImage: 'none',
                backgroundColor: colors.paperBg,
                border: `1px solid ${colors.border}`,
              },
            },
          },
          MuiCard: {
            styleOverrides: {
              root: {
                backgroundImage: 'none',
                backgroundColor: colors.paperBg,
                border: `1px solid ${colors.border}`,
              },
            },
          },
          MuiOutlinedInput: {
            styleOverrides: {
              root: {
                backgroundColor: colors.fieldBg,
                '& fieldset': { borderColor: alpha(colors.textSecondary, 0.35) },
                '&:hover fieldset': { borderColor: alpha(colors.textSecondary, 0.55) },
                '&.Mui-focused fieldset': { borderColor: '#0ea5e9' },
              },
            },
          },
          MuiTableCell: {
            styleOverrides: {
              root: {
                borderBottom: `1px solid ${alpha(colors.textSecondary, 0.2)}`,
              },
              head: {
                color: colors.textPrimary,
                fontWeight: 700,
              },
            },
          },
        },
      }),
    [colors, themeMode],
  )

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <BrowserRouter>
        <AppRoutes />
      </BrowserRouter>
    </ThemeProvider>
  )
}

export default App
