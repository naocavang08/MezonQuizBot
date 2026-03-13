import { useMemo, useState } from 'react'
import {
  Avatar,
  Box,
  Button,
  Chip,
  Divider,
  GlobalStyles,
  IconButton,
  InputAdornment,
  Menu,
  MenuItem,
  Stack,
  TextField,
  Typography,
  useMediaQuery,
} from '@mui/material'
import { alpha } from '@mui/material/styles'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { hasAnyPermission, PERMISSIONS } from '../Lib/Utils/permissions'
import useAuthStore from '../Stores/login.store'
import useThemeStore from '../Stores/theme.store'
import { MdDarkMode, MdLightMode, MdLogout, MdSettings } from 'react-icons/md'

type NavItem = {
  label: string
  path: string
  icon: string
  badge?: string
  requiredPermissions?: string[]
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
        requiredPermissions: [PERMISSIONS.USERS_LIST],
      },
      {
        label: 'Roles',
        path: '/app/roles',
        icon: 'M12 14l9-5-9-5-9 5 9 5zm0 0l6.16-3.422M12 14v7',
        requiredPermissions: [PERMISSIONS.ROLES_LIST],
      },
      {
        label: 'Quizzes',
        path: '/app/quizzes',
        icon: 'M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z',
        requiredPermissions: [PERMISSIONS.QUIZZES_LIST],
      },
      {
        label: 'Categories',
        path: '/app/categories',
        icon: 'M3 7v10a2 2 0 002 2h14a2 2 0 002-2V9a2 2 0 00-2-2h-6l-2-2H5a2 2 0 00-2 2z',
        requiredPermissions: [PERMISSIONS.CATEGORIES_LIST],
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
        requiredPermissions: [PERMISSIONS.QUIZZES_VIEW],
      },
      {
        label: 'My Quizzes',
        path: '/app/my-quizzes',
        icon: 'M4 6h16M4 12h16M4 18h10',
        requiredPermissions: [PERMISSIONS.QUIZZES_VIEW],
      },
      {
        label: 'Create Quiz',
        path: '/app/create-quiz',
        icon: 'M12 4v16m8-8H4',
        badge: 'New',
        requiredPermissions: [PERMISSIONS.QUIZZES_CREATE],
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
  const permissionName = useAuthStore((state) => state.permissionName)
  const hasSystemRole = useAuthStore((state) => state.hasSystemRole)
  const [mobileSidebarOpen, setMobileSidebarOpen] = useState(false)
  const isResponsive = useMediaQuery('(max-width:900px)')
  const themeMode = useThemeStore((state) => state.themeMode)
  const toggleTheme = useThemeStore((state) => state.toggleTheme)
  const [userMenuAnchor, setUserMenuAnchor] = useState<null | HTMLElement>(null)

  // Get colors from current theme - use palette from theme provider if available
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

  const currentTitle =
    pageTitleMap[location.pathname] ??
    (location.pathname.includes('/settings') ? 'Quiz Settings' : location.pathname.includes('/sessions/') ? 'Session Room' : 'Workspace')

  const currentThemeIcon =
    themeMode === 'light'
      ? <MdLightMode size={16} color={colors.textSecondary} />
      : <MdDarkMode size={16} color={colors.textSecondary} />

  const filteredNavSections = useMemo(
    () =>
      navSections
        .map((section) => ({
          ...section,
          items: section.items.filter((item) =>
            hasAnyPermission(permissionName, item.requiredPermissions, hasSystemRole),
          ),
        }))
        .filter((section) => section.items.length > 0),
    [permissionName, hasSystemRole],
  )

  const canCreateQuiz = hasAnyPermission(
    permissionName,
    [PERMISSIONS.QUIZZES_CREATE],
    hasSystemRole,
  )

  return (
    <>
      <GlobalStyles
        styles={{
          '*': { boxSizing: 'border-box' },
          '#root': { width: '100%', height: '100%' },
          body: {
            background: colors.bodyBg,
          },
          '::-webkit-scrollbar': { width: '6px', height: '6px' },
          '::-webkit-scrollbar-thumb': { backgroundColor: '#1e293b', borderRadius: '10px' },
        }}
      />
      <Box sx={{ color: '#cbd5e1', display: 'flex', height: '100vh', overflow: 'hidden' }}>
        {isResponsive && mobileSidebarOpen ? (
          <Box
            onClick={() => setMobileSidebarOpen(false)}
            sx={{
              position: 'fixed',
              inset: 0,
              bgcolor: 'rgba(2,6,23,0.55)',
              zIndex: 1190,
            }}
          />
        ) : null}

        <Box
          component="aside"
          sx={{
            width: 270,
            flexShrink: 0,
            borderRight: `1px solid ${colors.border}`,
            bgcolor: colors.sideBg,
            display: !isResponsive || mobileSidebarOpen ? 'flex' : 'none',
            position: isResponsive ? 'fixed' : 'relative',
            left: 0,
            top: 0,
            bottom: 0,
            zIndex: 1200,
            boxShadow: isResponsive ? '0 24px 48px rgba(2,6,23,0.45)' : 'none',
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
              <Typography sx={{ fontWeight: 700, color: colors.textPrimary, lineHeight: 1.1 }}>Mezon Quiz</Typography>
              <Typography sx={{ color: colors.textSecondary, fontSize: 10, mt: 0.5, textTransform: 'uppercase', letterSpacing: 1.1 }}>
                Workspace
              </Typography>
            </Box>
          </Stack>

          <Box sx={{ px: 2, pb: 2, overflowY: 'auto', flex: 1 }}>
            <Stack spacing={3}>
              {filteredNavSections.map((section) => (
                <Box key={section.title}>
                  <Typography sx={{ color: colors.textSecondary, fontSize: 11, textTransform: 'uppercase', fontWeight: 700, letterSpacing: 1.1, px: 1, mb: 1.5 }}>
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
                          onClick={() => {
                            if (isResponsive) {
                              setMobileSidebarOpen(false)
                            }
                          }}
                          fullWidth
                          disableRipple
                          sx={{
                            justifyContent: 'space-between',
                            textTransform: 'none',
                            py: 1,
                            px: 1.5,
                            borderRadius: 2,
                            minHeight: 40,
                            color: active ? colors.textPrimary : colors.textSecondary,
                            bgcolor: active ? alpha('#0ea5e9', 0.22) : 'transparent',
                            border: active ? '1px solid rgba(14,165,233,0.45)' : '1px solid transparent',
                            '&:hover': {
                              bgcolor: active ? alpha('#0ea5e9', 0.28) : alpha('#fff', 0.05),
                            },
                          }}
                        >
                          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                            <SvgIconPath path={item.icon} color={active ? '#38bdf8' : colors.textSecondary} />
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
              borderTop: `1px solid ${colors.border}`,
            }}
          >
            <Stack direction="row" spacing={1.2} sx={{ alignItems: 'center' }}>
              <Avatar
                src={user?.avatarUrl}
                alt={user?.username}
                sx={{
                    bgcolor: "#0ea5e9",
                    width: 36,
                    height: 36,
                    fontWeight: 700,
                    fontSize: 13
                }}
                >
                {!user?.avatarUrl &&
                    (user?.username?.slice(0, 2) || "U").toUpperCase()}
              </Avatar>
              <Box>
                <Typography sx={{ fontSize: 14, color: colors.textPrimary, fontWeight: 600, lineHeight: 1.2 }}>
                  {user?.displayName || user?.username || 'Quiz User'}
                </Typography>
                <Typography sx={{ color: colors.textSecondary, fontSize: 11, lineHeight: 1.1 }}>Signed in</Typography>
              </Box>
            </Stack>
            <Button
              size="small"
              onClick={() => {
                clearAuth()
                navigate('/login', { replace: true })
              }}
              sx={{ color: colors.textSecondary, textTransform: 'none', minWidth: 0, px: 1 }}
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
              borderBottom: `1px solid ${colors.border}`,
              alignItems: 'center',
              justifyContent: 'space-between',
              gap: 1.5,
            }}
          >
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flex: 1, minWidth: 0 }}>
              {isResponsive ? (
                <IconButton
                  onClick={() => setMobileSidebarOpen((prev) => !prev)}
                  sx={{ color: colors.textPrimary, border: `1px solid ${colors.border}`, borderRadius: 2 }}
                >
                  <SvgIconPath
                    path={mobileSidebarOpen ? 'M6 6l12 12M18 6L6 18' : 'M4 6h16M4 12h16M4 18h16'}
                    size={18}
                    color={colors.textPrimary}
                  />
                </IconButton>
              ) : null}

              <TextField
                size="small"
                placeholder={`Search in ${currentTitle}`}
                sx={{ maxWidth: 420, width: '100%' }}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <SvgIconPath path="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" size={16} color={colors.textSecondary} />
                    </InputAdornment>
                  ),
                }}
              />
            </Stack>

            <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexShrink: 0 }}>
              {canCreateQuiz ? (
                <Button
                  variant="contained"
                  startIcon={<SvgIconPath path="M12 4v16m8-8H4" size={14} color={colors.textPrimary} />}
                  sx={{ textTransform: 'none', borderRadius: 2, fontWeight: 700 }}
                  onClick={() => navigate('/app/create-quiz')}
                >
                  Create Quiz
                </Button>
              ) : null}

              <IconButton
                onClick={toggleTheme}
                sx={{
                    border: `1px solid ${colors.border}`,
                    color: colors.textPrimary,
                    width: 34,
                    height: 34,
                }}
                >
                {currentThemeIcon}
              </IconButton>

              <IconButton
                onClick={(event) => setUserMenuAnchor(event.currentTarget)}
                sx={{
                    p: 0,
                    width: 34,
                    height: 34,
                    border: `1px solid ${colors.border}`,
                    borderRadius: "50%",
                }}
                >
                <Avatar
                    sx={{
                    bgcolor: "#0ea5e9",
                    width: "100%",
                    height: "100%",
                    fontWeight: 700,
                    fontSize: 12,
                    }}
                >
                    {user?.avatarUrl ? (
                    <img
                        src={user.avatarUrl}
                        alt={user.username}
                        style={{ width: "100%", height: "100%", objectFit: "cover" }}
                    />
                    ) : (
                    (user?.username?.slice(0, 2) || "U").toUpperCase()
                    )}
                </Avatar>
              </IconButton>
              <Menu
                anchorEl={userMenuAnchor}
                open={Boolean(userMenuAnchor)}
                onClose={() => setUserMenuAnchor(null)}
                anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
                transformOrigin={{ vertical: 'top', horizontal: 'right' }}
              >
                <MenuItem disabled sx={{ flexDirection: "column", alignItems: "flex-start" }}>
                    <Typography
                        variant="body2"
                        fontWeight={600}
                        color={colors.textPrimary}
                    >
                        {user?.displayName || user?.username || "Quiz User"}
                    </Typography>

                    <Typography
                        variant="caption"
                        color={colors.textSecondary}
                    >
                        {user?.email}
                    </Typography>
                </MenuItem>
                <Divider sx={{ my: 0.5 }} />
                <MenuItem
                  onClick={() => {
                    setUserMenuAnchor(null)
                    navigate('/app/my-quizzes')
                  }}
                >
                  <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                    <MdSettings size={16} color={colors.textSecondary} />
                    <Typography variant="body2">Setting</Typography>
                  </Stack>
                </MenuItem>

                <Divider sx={{ my: 0.5 }} />
                <MenuItem
                  onClick={() => {
                    setUserMenuAnchor(null)
                    clearAuth()
                    navigate('/login', { replace: true })
                  }}
                >
                  <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                    <MdLogout size={16} color={colors.textSecondary} />
                    <Typography variant="body2">Logout</Typography>
                  </Stack>
                </MenuItem>
              </Menu>
            </Stack>
          </Stack>

          <Box sx={{ flex: 1, overflowY: 'auto', p: { xs: 2, md: 3 } }}>
            <Outlet />
          </Box>
        </Box>
      </Box>
    </>
  )
}

export default Layout
