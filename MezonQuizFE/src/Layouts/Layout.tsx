import {
  Avatar,
  Box,
  Button,
  Chip,
  CssBaseline,
  GlobalStyles,
  Stack,
  ThemeProvider,
  Typography,
  createTheme,
} from '@mui/material'
import { alpha } from '@mui/material/styles'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import useAuthStore from '../Stores/login.store'

type NavItem = {
  label: string
  path: string
  icon: string
  badge?: string
}

type NavSection = {
  title: string
  items: NavItem[]
}

const navSections: NavSection[] = [
  {
    title: 'Administration',
    items: [
      {
        label: 'Dashboard',
        path: '/app/dashboard',
        icon: 'M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z',
      },
      {
        label: 'Users',
        path: '/app/users',
        icon: 'M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z',
      },
      {
        label: 'Roles',
        path: '/app/roles',
        icon: 'M12 14l9-5-9-5-9 5 9 5zm0 0l6.16-3.422M12 14v7',
      },
      {
        label: 'Quizzes',
        path: '/app/quizzes',
        icon: 'M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z',
      },
      {
        label: 'Categories',
        path: '/app/categories',
        icon: 'M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z',
      },
    ],
  },
  {
    title: 'Quiz Workspace',
    items: [
      {
        label: 'Find Quizzes',
        path: '/app/find-quizzes',
        icon: 'M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z',
      },
      {
        label: 'My Quizzes',
        path: '/app/my-quizzes',
        icon: 'M4 6h16M4 12h16M4 18h10',
      },
      {
        label: 'Create Quiz',
        path: '/app/create-quiz',
        icon: 'M12 4v16m8-8H4',
        badge: 'New',
      },
    ],
  },
]

const pageTitleMap: Record<string, string> = {
  '/app/dashboard': 'Dashboard',
  '/app/users': 'User Management',
  '/app/roles': 'Role Management',
  '/app/quizzes': 'Quiz Management',
  '/app/categories': 'Category Management',
  '/app/find-quizzes': 'Find Quizzes',
  '/app/my-quizzes': 'My Quizzes',
  '/app/create-quiz': 'Create Quiz',
}

const theme = createTheme({
  palette: {
    mode: 'dark',
    primary: { main: '#0ea5e9' },
    secondary: { main: '#fb923c' },
    background: {
      default: '#08111f',
      paper: '#0e1a2b',
    },
    text: {
      primary: '#e2e8f0',
      secondary: '#94a3b8',
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
          backgroundColor: '#0e1a2b',
          border: '1px solid rgba(148,163,184,0.18)',
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          backgroundImage: 'none',
          backgroundColor: '#0e1a2b',
          border: '1px solid rgba(148,163,184,0.18)',
        },
      },
    },
    MuiOutlinedInput: {
      styleOverrides: {
        root: {
          backgroundColor: 'rgba(15, 23, 42, 0.8)',
          '& fieldset': { borderColor: 'rgba(148,163,184,0.35)' },
          '&:hover fieldset': { borderColor: 'rgba(148,163,184,0.55)' },
          '&.Mui-focused fieldset': { borderColor: '#0ea5e9' },
        },
      },
    },
    MuiTableCell: {
      styleOverrides: {
        root: {
          borderBottom: '1px solid rgba(148,163,184,0.2)',
        },
        head: {
          color: '#cbd5e1',
          fontWeight: 700,
        },
      },
    },
  },
})

function SvgIconPath({ path, size = 20, color = '#64748b' }: { path: string; size?: number; color?: string }) {
  return (
    <Box
      component="svg"
      viewBox="0 0 24 24"
      sx={{ width: size, height: size, display: 'block', color }}
      fill="none"
      stroke="currentColor"
    >
      <path d={path} strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" />
    </Box>
  )
}

const Layout = () => {
  const navigate = useNavigate()
  const location = useLocation()
  const clearAuth = useAuthStore((state) => state.clearAuth)
  const user = useAuthStore((state) => state.user)

  const currentTitle =
    pageTitleMap[location.pathname] ??
    (location.pathname.includes('/settings') ? 'Quiz Settings' : location.pathname.includes('/sessions/') ? 'Session Room' : 'Workspace')

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <GlobalStyles
        styles={{
          '*': { boxSizing: 'border-box' },
          '#root': { width: '100%', height: '100%' },
          body: {
            background:
              'radial-gradient(circle at 10% 0%, rgba(14,165,233,0.22), transparent 36%), radial-gradient(circle at 95% 0%, rgba(251,146,60,0.16), transparent 40%), #08111f',
          },
          '::-webkit-scrollbar': { width: '6px', height: '6px' },
          '::-webkit-scrollbar-thumb': { backgroundColor: '#1e293b', borderRadius: '10px' },
        }}
      />
      <Box sx={{ color: '#cbd5e1', display: 'flex', height: '100vh', overflow: 'hidden' }}>
        <Box
          component="aside"
          sx={{
            width: 270,
            flexShrink: 0,
            borderRight: '1px solid rgba(148,163,184,0.18)',
            bgcolor: '#0a1626',
            display: { xs: 'none', md: 'flex' },
            flexDirection: 'column',
          }}
        >
          <Stack direction="row" spacing={1.5} sx={{ p: 3, alignItems: 'center' }}>
            <Box
              sx={{
                width: 32,
                height: 32,
                borderRadius: 1.5,
                background: 'linear-gradient(135deg, #0ea5e9 0%, #0284c7 100%)',
                display: 'grid',
                placeItems: 'center',
              }}
            >
              <SvgIconPath path="M13 10V3L4 14h7v7l9-11h-7z" color="#fff" />
            </Box>
            <Box>
              <Typography sx={{ fontWeight: 700, color: '#f8fafc', lineHeight: 1.1 }}>Mezon Quiz</Typography>
              <Typography sx={{ color: '#64748b', fontSize: 10, mt: 0.5, textTransform: 'uppercase', letterSpacing: 1.1 }}>
                Workspace
              </Typography>
            </Box>
          </Stack>

          <Box sx={{ px: 2, pb: 2, overflowY: 'auto', flex: 1 }}>
            <Stack spacing={3}>
              {navSections.map((section) => (
                <Box key={section.title}>
                  <Typography sx={{ color: '#64748b', fontSize: 11, textTransform: 'uppercase', fontWeight: 700, letterSpacing: 1.1, px: 1, mb: 1.5 }}>
                    {section.title}
                  </Typography>
                  <Stack spacing={0.5}>
                    {section.items.map((item) => {
                      const active =
                        location.pathname === item.path ||
                        (item.path === '/app/my-quizzes' && location.pathname.includes('/app/my-quizzes/'))

                      return (
                        <Button
                          key={item.path}
                          component={NavLink}
                          to={item.path}
                          fullWidth
                          disableRipple
                          sx={{
                            justifyContent: 'space-between',
                            textTransform: 'none',
                            py: 1,
                            px: 1.5,
                            borderRadius: 2,
                            minHeight: 40,
                            color: active ? '#fff' : '#cbd5e1',
                            bgcolor: active ? alpha('#0ea5e9', 0.22) : 'transparent',
                            border: active ? '1px solid rgba(14,165,233,0.45)' : '1px solid transparent',
                            '&:hover': {
                              bgcolor: active ? alpha('#0ea5e9', 0.28) : alpha('#fff', 0.05),
                            },
                          }}
                        >
                          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                            <SvgIconPath path={item.icon} color={active ? '#38bdf8' : '#64748b'} />
                            <Typography sx={{ fontSize: 14, fontWeight: active ? 700 : 500 }}>{item.label}</Typography>
                          </Stack>
                          {item.badge ? (
                            <Chip
                              label={item.badge}
                              size="small"
                              sx={{
                                bgcolor: '#0ea5e9',
                                color: '#fff',
                                height: 20,
                                minWidth: 36,
                                fontSize: 10,
                                fontWeight: 700,
                              }}
                            />
                          ) : null}
                        </Button>
                      )
                    })}
                  </Stack>
                </Box>
              ))}
            </Stack>
          </Box>

          <Stack
            direction="row"
            sx={{
              alignItems: 'center',
              justifyContent: 'space-between',
              px: 2,
              py: 1.5,
              borderTop: '1px solid rgba(148,163,184,0.18)',
            }}
          >
            <Stack direction="row" spacing={1.2} sx={{ alignItems: 'center' }}>
              <Avatar sx={{ bgcolor: '#0ea5e9', width: 36, height: 36, fontWeight: 700, fontSize: 13 }}>
                {(user?.username?.slice(0, 2) || 'U').toUpperCase()}
              </Avatar>
              <Box>
                <Typography sx={{ fontSize: 14, color: '#fff', fontWeight: 600, lineHeight: 1.2 }}>
                  {user?.displayName || user?.username || 'Quiz User'}
                </Typography>
                <Typography sx={{ color: '#64748b', fontSize: 11, lineHeight: 1.1 }}>Signed in</Typography>
              </Box>
            </Stack>
            <Button
              size="small"
              onClick={() => {
                clearAuth()
                navigate('/login', { replace: true })
              }}
              sx={{ color: '#94a3b8', textTransform: 'none', minWidth: 0, px: 1 }}
            >
              Logout
            </Button>
          </Stack>
        </Box>

        <Box component="main" sx={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
          <Stack
            direction="row"
            sx={{
              minHeight: 68,
              px: { xs: 2, md: 4 },
              borderBottom: '1px solid rgba(148,163,184,0.18)',
              alignItems: 'center',
              justifyContent: 'space-between',
            }}
          >
            <Box>
              <Typography sx={{ color: '#f8fafc', fontSize: 24, fontWeight: 800 }}>{currentTitle}</Typography>
              <Typography sx={{ color: '#64748b', fontSize: 12 }}>Manage your quiz operations from one place</Typography>
            </Box>
          </Stack>

          <Box sx={{ flex: 1, overflowY: 'auto', p: { xs: 2, md: 3 } }}>
            <Box
              sx={{
                borderRadius: 4,
                border: '1px solid rgba(148,163,184,0.2)',
                background:
                  'linear-gradient(160deg, rgba(14,26,43,0.95) 0%, rgba(8,17,31,0.92) 100%)',
                p: { xs: 2, md: 3 },
                minHeight: '100%',
              }}
            >
              <Outlet />
            </Box>
          </Box>
        </Box>
      </Box>
    </ThemeProvider>
  )
}

export default Layout
