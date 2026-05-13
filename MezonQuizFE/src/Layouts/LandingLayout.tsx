import { useState, type MouseEvent } from 'react';
import {
  Avatar,
  AppBar,
  Box,
  Button,
  Container,
  Divider,
  Drawer,
  IconButton,
  Link,
  Menu,
  MenuItem,
  Stack,
  Toolbar,
  Typography,
} from '@mui/material';
import { Outlet, Link as RouterLink, useNavigate } from 'react-router-dom';
import { MdMenu, MdClose } from 'react-icons/md';
import { canAccessApp, PUBLIC_HOME_PATH, resolveDefaultAppPath } from '../Lib/Utils/permissions';
import useAuthStore from '../Stores/login.store';
import { dt } from '../Lib/designTokens';

const NAV_LINKS = [
  { label: 'Explore', to: '/explore' },
  { label: 'Categories', to: '/categories' },
  { label: 'Quizzes', to: '/quizzes' },
];

const LandingLayout = () => {
  const navigate = useNavigate();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const roleName = useAuthStore((s) => s.roleName);
  const permissionName = useAuthStore((s) => s.permissionName);
  const hasSystemRole = useAuthStore((s) => s.hasSystemRole);
  const user = useAuthStore((s) => s.user);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [profileMenuAnchor, setProfileMenuAnchor] = useState<HTMLElement | null>(null);
  const logout = useAuthStore((s) => s.logout);
  const defaultAppPath = resolveDefaultAppPath(permissionName, hasSystemRole, roleName);
  const hasAppAccess = canAccessApp(permissionName, hasSystemRole, roleName);
  const isProfileMenuOpen = Boolean(profileMenuAnchor);

  const handleGetStarted = () => {
    navigate(isAuthenticated ? defaultAppPath : '/login');
  };

  const handleOpenProfileMenu = (event: MouseEvent<HTMLElement>) => {
    setProfileMenuAnchor(event.currentTarget);
  };

  const handleCloseProfileMenu = () => {
    setProfileMenuAnchor(null);
  };

  const handleGoToApp = () => {
    handleCloseProfileMenu();
    navigate(defaultAppPath);
  };

  const handleLogout = async () => {
    handleCloseProfileMenu();
    await logout();
    navigate(PUBLIC_HOME_PATH, { replace: true });
  };

  return (
    <Box
      sx={{
        minHeight: '100vh',
        background: `radial-gradient(circle at top right, ${dt.colors.surfaceContainerLow} 0%, ${dt.colors.surfaceContainerLowest} 60%)`,
        color: dt.colors.onBackground,
        fontFamily: dt.typography.fontFamily,
      }}
    >
      {/* ── Header ── */}
      <AppBar
        position="fixed"
        elevation={0}
        sx={{
          bgcolor: 'rgba(248, 249, 255, 0.82)',
          backdropFilter: 'blur(14px)',
          borderBottom: `1px solid ${dt.colors.outlineVariant}`,
          color: dt.colors.onBackground,
        }}
      >
        <Container sx={{ maxWidth: dt.spacing.containerMax + ' !important' }}>
          <Toolbar sx={{ justifyContent: 'space-between', py: 0.75 }}>
            <Typography
              component={RouterLink}
              to="/explore"
              sx={{
                fontFamily: dt.typography.fontFamily,
                fontWeight: 800,
                fontSize: '1.25rem',
                letterSpacing: '-0.02em',
                color: dt.colors.primary,
                textDecoration: 'none',
                '&:hover': { opacity: 0.85 },
              }}
            >
              Mezon Quiz
            </Typography>

            <Stack
              direction="row"
              spacing={4}
              sx={{ display: { xs: 'none', md: 'flex' }, alignItems: 'center' }}
            >
              {NAV_LINKS.map((link) => (
                <Link
                  key={link.label}
                  component={RouterLink}
                  to={link.to}
                  underline="none"
                  sx={{
                    ...dt.typography.labelSm,
                    color: dt.colors.onSurfaceVariant,
                    transition: 'color 0.2s',
                    '&:hover': { color: dt.colors.primary },
                  }}
                >
                  {link.label}
                </Link>
              ))}
            </Stack>

            <Stack direction="row" spacing={1} alignItems="center">
              {isAuthenticated ? (
                <>
                  <IconButton
                    onClick={handleOpenProfileMenu}
                    sx={{
                      display: { xs: 'none', md: 'inline-flex' },
                      p: 0.5,
                      borderRadius: dt.radius.full,
                      border: `1px solid ${dt.colors.outlineVariant}`,
                      bgcolor: dt.colors.surfaceContainerLowest,
                      '&:hover': {
                        bgcolor: dt.colors.surfaceContainerLow,
                      },
                    }}
                  >
                    <Avatar
                      src={user?.avatarUrl}
                      alt={user?.displayName || user?.username || 'User'}
                      sx={{
                        width: 38,
                        height: 38,
                        bgcolor: dt.colors.primaryContainer,
                        color: dt.colors.onPrimary,
                        fontFamily: dt.typography.fontFamily,
                        fontWeight: 700,
                        fontSize: '0.9rem',
                      }}
                    >
                      {(user?.displayName || user?.username || 'U').slice(0, 1).toUpperCase()}
                    </Avatar>
                  </IconButton>
                  <Menu
                    anchorEl={profileMenuAnchor}
                    open={isProfileMenuOpen}
                    onClose={handleCloseProfileMenu}
                    anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
                    transformOrigin={{ vertical: 'top', horizontal: 'right' }}
                    slotProps={{
                      paper: {
                        sx: {
                          mt: 1,
                          minWidth: 180,
                          borderRadius: dt.radius.md,
                          border: `1px solid ${dt.colors.outlineVariant}`,
                          boxShadow: dt.shadows.card,
                          bgcolor: dt.colors.surfaceContainerLowest,
                        },
                      },
                    }}
                  >
                    <Box sx={{ px: 2, py: 1.5 }}>
                      <Typography
                        sx={{
                          ...dt.typography.bodyMd,
                          fontFamily: dt.typography.fontFamily,
                          fontWeight: 700,
                          color: dt.colors.onSurface,
                          lineHeight: 1.3,
                        }}
                      >
                        {user?.displayName || user?.username || 'Quiz User'}
                      </Typography>
                      {user?.email ? (
                        <Typography
                          sx={{
                            ...dt.typography.labelSm,
                            fontFamily: dt.typography.fontFamily,
                            color: dt.colors.onSurfaceVariant,
                            lineHeight: 1.4,
                            wordBreak: 'break-word',
                          }}
                        >
                          {user.email}
                        </Typography>
                      ) : null}
                    </Box>
                    <Divider sx={{ borderColor: dt.colors.outlineVariant }} />
                    {hasAppAccess ? (
                      <MenuItem
                        onClick={handleGoToApp}
                        sx={{
                          ...dt.typography.bodyMd,
                          fontFamily: dt.typography.fontFamily,
                          color: dt.colors.onSurface,
                        }}
                      >
                        Go to App
                      </MenuItem>
                    ) : null}
                    <MenuItem
                      onClick={handleLogout}
                      sx={{
                        ...dt.typography.bodyMd,
                        fontFamily: dt.typography.fontFamily,
                        color: dt.colors.onSurface,
                      }}
                    >
                      Logout
                    </MenuItem>
                  </Menu>
                </>
              ) : null}
              {!isAuthenticated ? (
                <Button
                  onClick={handleGetStarted}
                  sx={{
                    display: { xs: 'none', md: 'inline-flex' },
                    bgcolor: dt.colors.primaryContainer,
                    color: dt.colors.onPrimary,
                    fontFamily: dt.typography.fontFamily,
                    ...dt.typography.button,
                    borderRadius: dt.radius.default,
                    px: 2.5,
                    py: 1.1,
                    textTransform: 'none',
                    boxShadow: 'none',
                    transition: 'all 0.2s',
                    '&:hover': {
                      bgcolor: dt.colors.primary,
                      transform: 'translateY(-2px)',
                      boxShadow: dt.shadows.ctaButton,
                    },
                    '&:active': { transform: 'scale(0.97)' },
                  }}
                >
                  Get Started
                </Button>
              ) : null}

              <IconButton
                onClick={() => setDrawerOpen(true)}
                sx={{ display: { md: 'none' }, color: dt.colors.onSurface }}
              >
                <MdMenu size={22} />
              </IconButton>
            </Stack>
          </Toolbar>
        </Container>
      </AppBar>

      <Drawer
        anchor="right"
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        PaperProps={{
          sx: {
            width: 260,
            p: 3,
            bgcolor: dt.colors.surfaceContainerLowest,
            borderLeft: `1px solid ${dt.colors.outlineVariant}`,
          },
        }}
      >
        <Stack direction="row" justifyContent="space-between" alignItems="center" mb={3}>
          <Typography
            sx={{
              fontWeight: 800,
              fontSize: '1.1rem',
              color: dt.colors.primary,
              fontFamily: dt.typography.fontFamily,
            }}
          >
            Mezon Quiz
          </Typography>
          <IconButton onClick={() => setDrawerOpen(false)} sx={{ color: dt.colors.onSurface }}>
            <MdClose />
          </IconButton>
        </Stack>

        <Stack spacing={2.5}>
          {NAV_LINKS.map((link) => (
            <Link
              key={link.label}
              component={RouterLink}
              to={link.to}
              underline="none"
              onClick={() => setDrawerOpen(false)}
              sx={{
                ...dt.typography.bodyMd,
                fontFamily: dt.typography.fontFamily,
                fontWeight: 500,
                color: dt.colors.onSurfaceVariant,
                '&:hover': { color: dt.colors.primary },
              }}
            >
              {link.label}
            </Link>
          ))}
          <Divider sx={{ borderColor: dt.colors.outlineVariant }} />
          {!isAuthenticated || hasAppAccess ? (
            <Button
              onClick={() => { setDrawerOpen(false); handleGetStarted(); }}
              fullWidth
              sx={{
                bgcolor: dt.colors.primaryContainer,
                color: dt.colors.onPrimary,
                fontFamily: dt.typography.fontFamily,
                ...dt.typography.button,
                borderRadius: dt.radius.default,
                py: 1.25,
                textTransform: 'none',
                boxShadow: 'none',
                '&:hover': { bgcolor: dt.colors.primary },
              }}
            >
              {isAuthenticated ? 'Go to App' : 'Get Started'}
            </Button>
          ) : null}
        </Stack>
      </Drawer>

      <Box component="main" sx={{ pt: 8 }}>
        <Outlet />
      </Box>

      <Box
        component="footer"
        sx={{
          borderTop: `1px solid ${dt.colors.outlineVariant}`,
          bgcolor: dt.colors.surfaceContainerLowest,
          py: 6,
        }}
      >
        <Container sx={{ maxWidth: dt.spacing.containerMax + ' !important' }}>
          <Stack
            direction={{ xs: 'column', md: 'row' }}
            justifyContent="space-between"
            alignItems="center"
            spacing={2}
          >
            <Typography
              sx={{
                fontFamily: dt.typography.fontFamily,
                fontWeight: 800,
                fontSize: '1.1rem',
                color: dt.colors.primary,
              }}
            >
              Mezon Quiz
            </Typography>

            <Stack direction="row" spacing={4}>
              {['Privacy', 'Terms', 'Contact'].map((item) => (
                <Link
                  key={item}
                  href="#"
                  underline="none"
                  sx={{
                    ...dt.typography.labelSm,
                    color: dt.colors.outline,
                    '&:hover': { color: dt.colors.primary },
                    transition: 'color 0.2s',
                  }}
                >
                  {item}
                </Link>
              ))}
            </Stack>

            <Typography
              sx={{ ...dt.typography.labelSm, color: dt.colors.outline, fontFamily: dt.typography.fontFamily }}
            >
              © {new Date().getFullYear()} Mezon Quiz. All rights reserved.
            </Typography>
          </Stack>
        </Container>
      </Box>
    </Box>
  );
};

export default LandingLayout;
