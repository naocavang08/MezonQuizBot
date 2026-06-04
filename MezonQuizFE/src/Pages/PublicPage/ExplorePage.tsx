import { useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Card,
  CardActionArea,
  CardContent,
  CircularProgress,
  Container,
  Stack,
  Typography,
} from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { MdAutoAwesome, MdTimer, MdBarChart, MdArrowForward, MdBolt, MdCheckCircle, MdContentCopy, MdStar } from 'react-icons/md';
import { getAllCategories } from '../../Api/category.api';
import { getBotLink } from '../../Api/session.api';
import type { CategoryDto } from '../../Interface/category.dto';
import type { BotLinkDto } from '../../Interface/session.dto';
import CategoryIconBadge from '../../Lib/Utils/categoryIconBadge';
import { getCategoryIconOption } from '../../Lib/Utils/categoryIconOptions';
import { resolveDefaultAppPath } from '../../Lib/Utils/permissions';
import useAuthStore from '../../Stores/login.store';
import { dt } from '../../Lib/designTokens';
import catQrImage from '../../assets/hinh-nen-meo-9.jpg';

const FEATURES = [
  {
    icon: <MdAutoAwesome size={22} />,
    title: 'Adaptive Learning',
    description:
      'Our algorithms adjust difficulty in real-time based on your response patterns, ensuring optimal challenge.',
    iconBg: dt.colors.primaryFixed,          
    iconColor: dt.colors.onPrimaryFixedVariant,
  },
  {
    icon: <MdTimer size={22} />,
    title: 'Timed Assessments',
    description:
      'Develop speed and accuracy under pressure with our calibrated countdown system designed for calm focus.',
    iconBg: dt.colors.secondaryFixed,        
    iconColor: dt.colors.onSecondaryFixedVariant,
  },
  {
    icon: <MdBarChart size={22} />,
    title: 'Detailed Insights',
    description:
      'Deep-dive into your performance with granular data on knowledge gaps and retention milestones.',
    iconBg: dt.colors.tertiaryFixed,         
    iconColor: dt.colors.onTertiaryFixedVariant,
  },
];

const HERO_STATS = [
  {
    value: '24/7',
    label: 'Bot Availability',
    accentBg: 'rgba(34, 197, 94, 0.12)',
    accentColor: '#16a34a',
    icon: <MdCheckCircle size={22} />,
  },
  {
    value: '100+',
    label: 'Quiz Categories',
    accentBg: 'rgba(59, 130, 246, 0.12)',
    accentColor: '#2563eb',
    icon: <MdBarChart size={22} />,
  },
  {
    value: '1 Click',
    label: 'Quick Start Flow',
    accentBg: 'rgba(139, 92, 246, 0.12)',
    accentColor: '#7c3aed',
    icon: <MdBolt size={22} />,
  },
];

const AnnounceBadge = () => (
  <Box
    sx={{
      display: 'inline-flex',
      alignItems: 'center',
      px: 2,
      py: 0.75,
      borderRadius: dt.radius.full,
      bgcolor: dt.colors.secondaryFixed,
      border: `1px solid ${dt.colors.secondaryContainer}4D`,
      ...dt.typography.labelSm,
      fontFamily: dt.typography.fontFamily,
      color: dt.colors.onSecondaryFixed,
    }}
  >
    <Box
      component="span"
      sx={{
        display: 'inline-block',
        width: 8,
        height: 8,
        borderRadius: '50%',
        bgcolor: dt.colors.primary,
        mr: 1,
        flexShrink: 0,
        animation: 'pulseAnimate 2s ease-in-out infinite',
        '@keyframes pulseAnimate': {
          '0%, 100%': { opacity: 1, transform: 'scale(1)' },
          '50%': { opacity: 0.45, transform: 'scale(0.85)' },
        },
      }}
    />
    Quiz workflows built for Mezon communities
  </Box>
);

const HeroQuizPreview = () => (
  <Box
    sx={{
      position: 'relative',
      width: '100%',
      maxWidth: 540,
      ml: { lg: 'auto' },
    }}
  >
    <Box
      sx={{
        position: 'absolute',
        inset: '18% 10% auto',
        height: 260,
        borderRadius: dt.radius.xl,
        background: 'rgba(53, 37, 205, 0.18)',
        filter: 'blur(72px)',
        zIndex: 0,
      }}
    />
    <Box sx={{ position: 'relative', zIndex: 1 }}>
      <Box
        sx={{
          borderRadius: '28px',
          background: 'linear-gradient(180deg, #1c1237 0%, #4a214f 100%)',
          p: { xs: 1.5, md: 2 },
          boxShadow: '0 28px 60px rgba(32, 18, 68, 0.28)',
        }}
      >
        <Box
          sx={{
            display: 'flex',
            borderRadius: '18px',
            overflow: 'hidden',
            backgroundColor: '#1f2024',
            border: '1px solid rgba(255,255,255,0.05)',
            minHeight: 372,
          }}
        >
          <Box sx={{ width: 5, bgcolor: '#22c55e', flexShrink: 0 }} />
          <Box sx={{ flex: 1, p: { xs: 3, md: 4 } }}>
            <Typography
              sx={{
                fontFamily: dt.typography.fontFamily,
                fontWeight: 700,
                fontSize: { xs: '1.1rem', md: '1.35rem' },
                lineHeight: 1.35,
                color: '#f8fafc',
              }}
            >
              [SINGLE CHOICE] Toán học cơ bản | Question 1/15
            </Typography>
            <Typography
              sx={{
                fontFamily: dt.typography.fontFamily,
                fontSize: '1rem',
                color: 'rgba(226,232,240,0.76)',
                mt: 1.75,
              }}
            >
              Số nào là số nguyên tố?
            </Typography>
            <Box
              sx={{
                position: 'relative',
                mt: 4,
                p: 3,
                borderRadius: '12px',
                backgroundColor: '#2b2d33',
                border: '1px solid rgba(255,255,255,0.08)',
                color: '#e5e7eb',
                fontFamily: '"JetBrains Mono", "Fira Code", monospace',
                fontSize: { xs: '1.35rem', md: '1.5rem' },
                fontWeight: 600,
                lineHeight: 1.55,
              }}
            >
              <Box
                sx={{
                  position: 'absolute',
                  top: 12,
                  right: 12,
                  color: 'rgba(226,232,240,0.72)',
                  display: 'inline-flex',
                }}
              >
                <MdContentCopy size={20} />
              </Box>
              <Box>1 - 13</Box>
              <Box>2 - 12</Box>
              <Box>3 - 9</Box>
              <Box>4 - 15</Box>
            </Box>
            <Typography
              sx={{
                fontFamily: dt.typography.fontFamily,
                color: 'rgba(148,163,184,0.88)',
                fontSize: '0.95rem',
                mt: 7,
              }}
            >
              (Chọn đáp án đúng tương ứng phía bên dưới!  )
            </Typography>
          </Box>
        </Box>
      </Box>
      <Stack direction="row" spacing={1.5} sx={{ mt: 1.5 }}>
        {[1, 2, 3, 4].map((item) => (
          <Box
            key={item}
            sx={{
              minWidth: 58,
              px: 2.5,
              py: 1.75,
              borderRadius: '8px',
              bgcolor: '#5c67f2',
              color: '#ffffff',
              textAlign: 'center',
              fontFamily: dt.typography.fontFamily,
              fontWeight: 700,
              fontSize: '1.6rem',
              lineHeight: 1,
              boxShadow: '0 10px 24px rgba(92, 103, 242, 0.35)',
            }}
          >
            {item}
          </Box>
        ))}
      </Stack>
    </Box>
  </Box>
);

type QrPreviewCardProps = {
  qrCodeUrl: string;
  deepLink: string;
  isLoading: boolean;
};

const QrPreviewCard = ({ qrCodeUrl, deepLink, isLoading }: QrPreviewCardProps) => {
  const handleOpenBotLink = () => {
    if (!deepLink) {
      return;
    }

    window.open(deepLink, '_blank', 'noopener,noreferrer');
  };

  return (
    <Stack spacing={2.25} alignItems="center">
      <Box
        sx={{
          p: 3,
          borderRadius: '28px',
          bgcolor: dt.colors.surfaceContainerLowest,
          border: '1px solid rgba(255,255,255,0.82)',
          boxShadow: '0 18px 50px rgba(15, 23, 42, 0.1)',
          transform: { md: 'rotate(2deg)' },
          transition: 'transform 0.3s ease',
          '&:hover': {
            transform: { md: 'rotate(0deg) scale(1.02)' },
          },
        }}
      >
        {isLoading ? (
          <Box
            sx={{
              width: { xs: 160, md: 192 },
              height: { xs: 160, md: 192 },
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            <CircularProgress size={32} />
          </Box>
        ) : qrCodeUrl ? (
          <Box
            component="img"
            src={qrCodeUrl}
            alt="Mezon Quiz Bot QR code"
            sx={{
              width: { xs: 160, md: 192 },
              height: { xs: 160, md: 192 },
              display: 'block',
              objectFit: 'contain',
              borderRadius: '16px',
            }}
          />
        ) : (
          <Box
            sx={{
              width: { xs: 160, md: 192 },
              height: { xs: 160, md: 192 },
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              textAlign: 'center',
              px: 2,
            }}
          >
            <Typography
              sx={{
                fontFamily: dt.typography.fontFamily,
                color: dt.colors.onSurfaceVariant,
                fontSize: '0.9rem',
                fontWeight: 600,
              }}
            >
              QR code unavailable
            </Typography>
          </Box>
        )}
      </Box>

      <Box
        component="button"
        type="button"
        onClick={handleOpenBotLink}
        disabled={!deepLink}
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: 1.5,
          px: 2.5,
          py: 1.75,
          borderRadius: '20px',
          bgcolor: dt.colors.surfaceContainerLowest,
          border: `1px solid ${dt.colors.outlineVariant}`,
          boxShadow: '0 10px 30px rgba(15, 23, 42, 0.06)',
          width: '100%',
          appearance: 'none',
          textAlign: 'left',
          cursor: deepLink ? 'pointer' : 'default',
          transition: 'transform 0.2s ease, box-shadow 0.2s ease',
          '&:hover': deepLink
            ? {
                transform: { md: 'translateY(-1px)' },
                boxShadow: '0 14px 34px rgba(15, 23, 42, 0.1)',
              }
            : undefined,
          '&:disabled': {
            opacity: 0.7,
          },
        }}
      >
        <Box
          sx={{
            width: 40,
            height: 40,
            borderRadius: '12px',
            overflow: 'hidden',
            flexShrink: 0,
          }}
        >
          <Box
            component="img"
            src={catQrImage}
            alt="Mezon Quiz Bot"
            sx={{
              width: '100%',
              height: '100%',
              objectFit: 'cover',
            }}
          />
        </Box>
        <Typography
          sx={{
            fontFamily: dt.typography.fontFamily,
            fontWeight: 700,
            color: dt.colors.onSurface,
          }}
        >
          Practice with Mezon Bot
        </Typography>
      </Box>
    </Stack>
  );
};

const Explore = () => {
  const navigate = useNavigate();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const permissionName = useAuthStore((s) => s.permissionName);
  const hasSystemRole = useAuthStore((s) => s.hasSystemRole);
  const roleName = useAuthStore((s) => s.roleName);
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [isCategoryLoading, setIsCategoryLoading] = useState(true);
  const [categoryError, setCategoryError] = useState<string | null>(null);
  const [botLink, setBotLink] = useState<BotLinkDto | null>(null);
  const [isBotQrLoading, setIsBotQrLoading] = useState(true);
  const accentPresets = [
    {
      chipBg: `${dt.colors.primaryFixed}4D`,
      chipColor: dt.colors.onPrimaryFixedVariant,
    },
    {
      chipBg: `${dt.colors.secondaryFixed}4D`,
      chipColor: dt.colors.onSecondaryFixedVariant,
    },
    {
      chipBg: `${dt.colors.tertiaryFixed}4D`,
      chipColor: dt.colors.onTertiaryFixedVariant,
    },
  ];

  const sortedCategories = useMemo(
    () =>
      [...categories].sort((a, b) => {
        const aOrder = a.sortOrder ?? Number.MAX_SAFE_INTEGER;
        const bOrder = b.sortOrder ?? Number.MAX_SAFE_INTEGER;
        if (aOrder !== bOrder) {
          return aOrder - bOrder;
        }
        return a.name.localeCompare(b.name);
      }),
    [categories],
  );
  const visibleCategories = useMemo(() => sortedCategories.slice(0, 4), [sortedCategories]);

  const fetchCategories = async () => {
    setIsCategoryLoading(true);
    setCategoryError(null);
    try {
      const data = await getAllCategories();
      setCategories(data);
    } catch {
      setCategoryError('Không thể tải danh sách category. Vui lòng thử lại.');
    } finally {
      setIsCategoryLoading(false);
    }
  };

  const fetchBotLink = async () => {
    setIsBotQrLoading(true);
    try {
      const data = await getBotLink();
      setBotLink(data ?? null);
    } catch {
      setBotLink(null);
    } finally {
      setIsBotQrLoading(false);
    }
  };

  useEffect(() => {
    fetchCategories();
    fetchBotLink();
  }, []);

  const defaultAppPath = resolveDefaultAppPath(permissionName, hasSystemRole, roleName);
  const handleGetStarted = () => navigate(isAuthenticated ? defaultAppPath : '/login');

  return (
    <>
      {/* ══ HERO ════════════════════════════════════════════════════════════ */}
      <Box
        component="section"
        sx={{
          pt: { xs: '72px', md: '104px' },
          pb: { xs: '40px', md: '72px' },
          px: dt.spacing.gutter,
        }}
      >
        <Container sx={{ maxWidth: `${dt.spacing.containerMax} !important` }}>
          <Box
            sx={{
              borderRadius: { xs: '28px', md: '44px' },
              px: { xs: 3, md: 7 },
              py: { xs: 4, md: 7 },
              border: '1px solid rgba(255,255,255,0.65)',
              background:
                'radial-gradient(circle at top left, rgba(220,252,231,0.95) 0%, rgba(239,246,255,0.98) 42%, rgba(255,255,255,1) 100%)',
              boxShadow: '0 24px 70px rgba(79, 70, 229, 0.08)',
            }}
          >
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', lg: 'minmax(0, 1fr) minmax(420px, 520px)' },
                gap: { xs: 5, md: 8 },
                alignItems: 'center',
              }}
            >
              <Stack spacing={4} alignItems="flex-start">
                <AnnounceBadge />

                <Typography
                  sx={{
                    fontFamily: dt.typography.fontFamily,
                    fontSize: { xs: '2.55rem', md: '3.5rem' },
                    fontWeight: 700,
                    lineHeight: 1.08,
                    letterSpacing: '-0.03em',
                    color: dt.colors.onSurface,
                    maxWidth: 620,
                  }}
                >
                  Run smarter{' '}
                  <Box component="span" sx={{ color: '#22c55e' }}>
                    quizzes
                  </Box>{' '}
                  for your Mezon server
                </Typography>

                <Typography
                  sx={{
                    fontFamily: dt.typography.fontFamily,
                    ...dt.typography.bodyLg,
                    color: dt.colors.onSurfaceVariant,
                    maxWidth: 560,
                  }}
                >
                  Launch category-based quizzes, keep members engaged, and guide practice from one
                  clean flow. MezonQuizBot helps communities host quick challenges without cluttered
                  setup screens.
                </Typography>

                <Stack direction="row" useFlexGap flexWrap="wrap" gap={1.25}>
                  {[
                    { label: 'PUBLIC QUIZ', bg: '#3b82f6' },
                    { label: 'CATEGORY FLOW', bg: '#f43f5e' },
                    { label: 'MEZON BOT', bg: '#f97316' },
                    { label: 'LIVE PRACTICE', bg: '#8b5cf6' },
                  ].map((tag) => (
                    <Box
                      key={tag.label}
                      sx={{
                        px: 1.5,
                        py: 0.85,
                        borderRadius: dt.radius.md,
                        bgcolor: tag.bg,
                        color: '#fff',
                        fontFamily: dt.typography.fontFamily,
                        fontSize: '0.76rem',
                        fontWeight: 800,
                        letterSpacing: '0.04em',
                      }}
                    >
                      {tag.label}
                    </Box>
                  ))}
                </Stack>

                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2.25} alignItems={{ sm: 'center' }}>
                  <Button
                    onClick={handleGetStarted}
                    endIcon={<MdArrowForward />}
                    startIcon={<MdBolt />}
                    sx={{
                      px: 3.5,
                      py: 1.8,
                      borderRadius: '18px',
                      bgcolor: '#10b981',
                      color: '#fff',
                      fontFamily: dt.typography.fontFamily,
                      ...dt.typography.button,
                      textTransform: 'none',
                      boxShadow: '0 18px 34px rgba(16, 185, 129, 0.28)',
                      '&:hover': {
                        bgcolor: '#059669',
                        boxShadow: '0 22px 38px rgba(16, 185, 129, 0.34)',
                      },
                    }}
                  >
                    Open quiz hub
                  </Button>

                  <Button
                    onClick={() => navigate('/categories')}
                    sx={{
                      px: 0,
                      color: dt.colors.onSurface,
                      fontFamily: dt.typography.fontFamily,
                      ...dt.typography.button,
                      textTransform: 'none',
                      justifyContent: 'flex-start',
                      '&:hover': { bgcolor: 'transparent', color: dt.colors.primary },
                    }}
                  >
                    Browse categories
                  </Button>
                </Stack>

                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={3} useFlexGap flexWrap="wrap" pt={1}>
                  <Stack direction="row" spacing={1.25} alignItems="center">
                    <Stack direction="row" spacing={0.25} sx={{ color: '#fb923c' }}>
                      {Array.from({ length: 5 }).map((_, index) => (
                        <MdStar key={index} size={18} />
                      ))}
                    </Stack>
                    <Typography
                      sx={{
                        fontFamily: dt.typography.fontFamily,
                        fontSize: '0.95rem',
                        color: dt.colors.onSurfaceVariant,
                      }}
                    >
                      4.8 average rating
                    </Typography>
                  </Stack>

                  <Stack direction="row" spacing={1} alignItems="center">
                    <MdCheckCircle size={18} color="#22c55e" />
                    <Typography
                      sx={{
                        fontFamily: dt.typography.fontFamily,
                        fontSize: '0.95rem',
                        color: dt.colors.onSurfaceVariant,
                      }}
                    >
                      Quick to launch, easy to join
                    </Typography>
                  </Stack>
                </Stack>
              </Stack>

              <HeroQuizPreview />
            </Box>
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', md: 'repeat(3, minmax(0, 1fr))' },
                gap: 2,
                mt: { xs: 3, md: 4 },
              }}
            >
              {HERO_STATS.map((item) => (
                <Box
                  key={item.label}
                  sx={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: 2,
                    px: 3,
                    py: 2.5,
                    borderRadius: '20px',
                    bgcolor: 'rgba(255,255,255,0.52)',
                    border: '1px solid rgba(255,255,255,0.72)',
                    backdropFilter: 'blur(10px)',
                    boxShadow: '0 18px 40px rgba(15, 23, 42, 0.06)',
                  }}
                >
                  <Box
                    sx={{
                      width: 48,
                      height: 48,
                      borderRadius: '14px',
                      bgcolor: item.accentBg,
                      color: item.accentColor,
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      flexShrink: 0,
                    }}
                  >
                    {item.icon}
                  </Box>

                  <Box>
                    <Typography
                      sx={{
                        fontFamily: dt.typography.fontFamily,
                        fontWeight: 800,
                        fontSize: '1.35rem',
                        lineHeight: 1.1,
                        color: dt.colors.onSurface,
                      }}
                    >
                      {item.value}
                    </Typography>
                    <Typography
                      sx={{
                        fontFamily: dt.typography.fontFamily,
                        fontSize: '0.72rem',
                        fontWeight: 800,
                        letterSpacing: '0.12em',
                        textTransform: 'uppercase',
                        color: dt.colors.onSurfaceVariant,
                        mt: 0.5,
                      }}
                    >
                      {item.label}
                    </Typography>
                  </Box>
                </Box>
              ))}
            </Box>
          </Box>
        </Container>
      </Box>

      {/* ══ FEATURES ════════════════════════════════════════════════════════ */}
      <Box
        component="section"
        id="explore"
        sx={{
          py: { xs: '48px', md: dt.spacing.sectionPadding },
          px: dt.spacing.gutter,
          bgcolor: `${dt.colors.surfaceContainerLowest}60`, // white/40
        }}
      >
        <Container sx={{ maxWidth: `${dt.spacing.containerMax} !important` }}>
          {/* Section header */}
          <Box sx={{ mb: dt.spacing.stackLg }}>
            <Typography
              sx={{
                fontFamily: dt.typography.fontFamily,
                ...dt.typography.h2,
                color: dt.colors.onSurface,
              }}
            >
              Precision-engineered for focus
            </Typography>
            <Typography
              sx={{
                fontFamily: dt.typography.fontFamily,
                ...dt.typography.bodyMd,
                color: dt.colors.onSurfaceVariant,
                mt: dt.spacing.stackSm,
              }}
            >
              Built for serious learners who value clarity.
            </Typography>
          </Box>

          {/* 3-column grid — cards with ambient indigo shadow per elevation spec */}
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: { xs: '1fr', md: 'repeat(3, 1fr)' },
              gap: dt.spacing.gutter,
            }}
          >
            {FEATURES.map((f) => (
              <Card
                key={f.title}
                variant="outlined"
                sx={{
                  borderRadius: dt.radius.lg,          // 1rem for card pods
                  bgcolor: dt.colors.surfaceContainerLowest,
                  borderColor: `${dt.colors.outlineVariant}66`,
                  boxShadow: dt.shadows.card,          // rgba(79,70,229,0.08)
                  transition: 'transform 0.3s ease, box-shadow 0.3s ease',
                  '&:hover': {
                    transform: 'translateY(-4px)',
                    boxShadow: dt.shadows.cardHover,
                  },
                }}
              >
                <CardContent sx={{ p: 4 }}>
                  {/* Icon pod — 48×48, rounded-md (0.75rem) */}
                  <Box
                    sx={{
                      width: 48,
                      height: 48,
                      borderRadius: dt.radius.md,
                      bgcolor: f.iconBg,
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      color: f.iconColor,
                      mb: 3,
                    }}
                  >
                    {f.icon}
                  </Box>

                  {/* h3: 20px / 600 */}
                  <Typography
                    sx={{
                      fontFamily: dt.typography.fontFamily,
                      ...dt.typography.h3,
                      color: dt.colors.onSurface,
                      mb: 1.5,
                    }}
                  >
                    {f.title}
                  </Typography>

                  {/* body-md: 16px / 400 / 1.5 */}
                  <Typography
                    sx={{
                      fontFamily: dt.typography.fontFamily,
                      ...dt.typography.bodyMd,
                      color: dt.colors.onSurfaceVariant,
                    }}
                  >
                    {f.description}
                  </Typography>
                </CardContent>
              </Card>
            ))}
          </Box>
        </Container>
      </Box>

      <Box
        component="section"
        sx={{
          py: { xs: '48px', md: dt.spacing.sectionPadding },
          px: dt.spacing.gutter,
        }}
      >
        <Container sx={{ maxWidth: `${dt.spacing.containerMax} !important` }}>
          <Box
            sx={{
              borderRadius: { xs: '28px', md: '36px' },
              px: { xs: 3, md: 6 },
              py: { xs: 4, md: 6 },
              background: 'linear-gradient(180deg, rgba(255,255,255,0.96) 0%, rgba(239,244,255,0.96) 100%)',
              border: `1px solid ${dt.colors.outlineVariant}`,
              boxShadow: '0 24px 60px rgba(79, 70, 229, 0.06)',
            }}
          >
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', md: 'minmax(0, 1fr) 280px' },
                gap: { xs: 4, md: 6 },
                alignItems: 'center',
              }}
            >
              <Stack spacing={2.5}>
                <Box
                  sx={{
                    display: 'inline-flex',
                    alignItems: 'center',
                    px: 2,
                    py: 0.75,
                    inlineSize: 'fit-content',
                    borderRadius: dt.radius.full,
                    bgcolor: dt.colors.secondaryFixed,
                    border: `1px solid ${dt.colors.secondaryContainer}4D`,
                    ...dt.typography.labelSm,
                    fontFamily: dt.typography.fontFamily,
                    color: dt.colors.onSecondaryFixed,
                  }}
                >
                  <Box
                    component="span"
                    sx={{
                      display: 'inline-block',
                      width: 8,
                      height: 8,
                      borderRadius: '50%',
                      bgcolor: dt.colors.primary,
                      mr: 1,
                      flexShrink: 0,
                      animation: 'pulseAnimate 2s ease-in-out infinite',
                      '@keyframes pulseAnimate': {
                        '0%, 100%': { opacity: 1, transform: 'scale(1)' },
                        '50%': { opacity: 0.45, transform: 'scale(0.85)' },
                      },
                    }}
                  />
                  New experience
                </Box>

                <Typography
                  sx={{
                    fontFamily: dt.typography.fontFamily,
                    ...dt.typography.h2,
                    color: dt.colors.onSurface,
                  }}
                >
                  Practice on the go
                </Typography>

                <Typography
                  sx={{
                    fontFamily: dt.typography.fontFamily,
                    ...dt.typography.bodyLg,
                    color: dt.colors.onSurfaceVariant,
                    maxWidth: 640,
                  }}
                >
                  Open MezonQuizBot from your phone and jump into a quick public session. Scan the
                  code to explore categories, launch quizzes faster, and keep the same smooth flow
                  across devices.
                </Typography>

                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={3} useFlexGap flexWrap="wrap" pt={1}>
                  <Stack direction="row" spacing={1} alignItems="center">
                    <MdCheckCircle size={18} color="#22c55e" />
                    <Typography
                      sx={{
                        fontFamily: dt.typography.fontFamily,
                        color: dt.colors.onSurfaceVariant,
                        fontWeight: 600,
                      }}
                    >
                      Mezon app install required
                    </Typography>
                  </Stack>

                  <Stack direction="row" spacing={1} alignItems="center">
                    <MdCheckCircle size={18} color="#22c55e" />
                    <Typography
                      sx={{
                        fontFamily: dt.typography.fontFamily,
                        color: dt.colors.onSurfaceVariant,
                        fontWeight: 600,
                      }}
                    >
                      Works with public quiz pages and bot-driven sessions
                    </Typography>
                  </Stack>
                </Stack>
              </Stack>

              <QrPreviewCard qrCodeUrl={botLink?.qrCodeUrl ?? ''} deepLink={botLink?.deepLink ?? ''} isLoading={isBotQrLoading} />
            </Box>
          </Box>
        </Container>
      </Box>

      {/* ══ CATEGORIES ══════════════════════════════════════════════════════ */}
      <Box
        component="section"
        id="categories"
        sx={{
          py: { xs: '48px', md: dt.spacing.sectionPadding },
          px: dt.spacing.gutter,
        }}
      >
        <Container sx={{ maxWidth: `${dt.spacing.containerMax} !important` }}>
          {/* Section header row */}
          <Stack
            direction={{ xs: 'column', md: 'row' }}
            justifyContent="space-between"
            alignItems={{ xs: 'flex-start', md: 'flex-end' }}
            mb={dt.spacing.stackLg}
            spacing={2}
          >
            <Box>
              <Typography
                sx={{
                  fontFamily: dt.typography.fontFamily,
                  ...dt.typography.h2,
                  color: dt.colors.onSurface,
                }}
              >
                Curated Categories
              </Typography>
              <Typography
                sx={{
                  fontFamily: dt.typography.fontFamily,
                  ...dt.typography.bodyMd,
                  color: dt.colors.onSurfaceVariant,
                  mt: 0.5,
                }}
              >
                Explore professionally authored question banks.
              </Typography>
            </Box>

            {/* "View All" link — primary color, button weight */}
            <Button
              endIcon={<MdArrowForward />}
              onClick={() => navigate('/categories')}
              sx={{
                fontFamily: dt.typography.fontFamily,
                ...dt.typography.button,
                color: dt.colors.primary,
                textTransform: 'none',
                px: 0,
                flexShrink: 0,
                '&:hover': { bgcolor: 'transparent', opacity: 0.8 },
              }}
            >
              View All Categories
            </Button>
          </Stack>

          {isCategoryLoading ? (
            <Box py={6} display="flex" justifyContent="center">
              <CircularProgress />
            </Box>
          ) : null}

          {!isCategoryLoading && categoryError ? (
            <Alert
              severity="error"
              action={
                <Button color="inherit" size="small" onClick={fetchCategories}>
                  Thử lại
                </Button>
              }
            >
              {categoryError}
            </Alert>
          ) : null}

          {!isCategoryLoading && !categoryError && sortedCategories.length === 0 ? (
            <Box
              sx={{
                borderRadius: dt.radius.lg,
                border: `1px solid ${dt.colors.outlineVariant}`,
                p: 4,
                bgcolor: dt.colors.surfaceContainerLowest,
                textAlign: 'center',
              }}
            >
              <Typography sx={{ ...dt.typography.bodyMd, color: dt.colors.onSurfaceVariant }}>
                Chưa có danh mục nào được tạo.
              </Typography>
            </Box>
          ) : null}

          {!isCategoryLoading && !categoryError && sortedCategories.length > 0 ? (
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', lg: 'repeat(4, 1fr)' },
                gap: dt.spacing.stackMd,
              }}
            >
              {visibleCategories.map((category, index) => {
                const accent = accentPresets[index % accentPresets.length];
                const option = getCategoryIconOption(category.icon);
                const WatermarkIcon = option?.icon;

                return (
                  <Card
                    key={category.id}
                    variant="outlined"
                    sx={{
                      borderRadius: dt.radius.md,
                      overflow: 'hidden',
                    }}
                  >
                    <CardActionArea
                      onClick={() => navigate(`/quizzes?category=${encodeURIComponent(category.id)}`)}
                      sx={{
                        p: 3,
                        minHeight: 220,
                        position: 'relative',
                        display: 'flex',
                        flexDirection: 'column',
                        alignItems: 'flex-start',
                        border: `1px solid ${dt.colors.outlineVariant}`,
                        borderRadius: dt.radius.md,
                        bgcolor: dt.colors.surfaceContainerLowest,
                        transition: 'all 0.3s ease',
                        overflow: 'hidden',
                        '&:hover': {
                          borderColor: dt.colors.primary,
                          boxShadow: '0 20px 25px -5px rgba(15,23,42,0.18)',
                        },
                      }}
                    >
                      {WatermarkIcon ? (
                        <Box
                          sx={{
                            position: 'absolute',
                            right: -14,
                            bottom: -14,
                            opacity: 0.03,
                            color: dt.colors.onSurface,
                            transition: 'transform 0.5s ease',
                            '.MuiCardActionArea-root:hover &': { transform: 'scale(1.1)' },
                          }}
                        >
                          <WatermarkIcon size={96} />
                        </Box>
                      ) : null}

                      <CardContent sx={{ p: 0, width: '100%' }}>
                        <Stack direction="row" spacing={1} alignItems="center" mb={2}>
                          <Box
                            sx={{
                              px: 1,
                              py: 0.5,
                              borderRadius: dt.radius.sm,
                              bgcolor: accent.chipBg,
                              color: accent.chipColor,
                              fontFamily: dt.typography.fontFamily,
                              ...dt.typography.labelSm,
                              fontSize: '10px',
                              lineHeight: 1,
                              letterSpacing: '0.08em',
                              textTransform: 'uppercase',
                            }}
                          >
                            {category.slug}
                          </Box>
                          <CategoryIconBadge iconKey={category.icon} size={24} />
                        </Stack>

                        <Typography sx={{ ...dt.typography.h3, color: dt.colors.onSurface, mb: 1 }}>
                          {category.name}
                        </Typography>

                        <Typography
                          sx={{
                            ...dt.typography.bodyMd,
                            fontSize: '0.875rem',
                            color: dt.colors.onSurfaceVariant,
                            lineHeight: 1.6,
                          }}
                        >
                          Tập câu hỏi theo chủ đề {category.name}.
                        </Typography>

                        <Typography
                          sx={{
                            ...dt.typography.bodyMd,
                            color: dt.colors.onSurfaceVariant,
                            mt: 3,
                            display: 'flex',
                            alignItems: 'center',
                            gap: 0.5,
                            fontSize: '0.875rem',
                          }}
                        >
                          📝 Quizzes by category
                        </Typography>
                      </CardContent>
                    </CardActionArea>
                  </Card>
                );
                })}
            </Box>
          ) : null}
        </Container>
      </Box>

      {/* ══ CTA BANNER ══════════════════════════════════════════════════════ */}
      <Box
        component="section"
        sx={{
          py: { xs: '48px', md: dt.spacing.sectionPadding },
          px: dt.spacing.gutter,
        }}
      >
        <Container sx={{ maxWidth: `${dt.spacing.containerMax} !important` }}>
          <Box
            sx={{
              position: 'relative',
              borderRadius: dt.radius.xl,              // 1.5rem per design
              // Linear gradient — indigo primary-container → primary
              background: `linear-gradient(135deg, ${dt.colors.primaryContainer} 0%, ${dt.colors.primary} 100%)`,
              p: { xs: 6, md: '80px' },
              textAlign: 'center',
              overflow: 'hidden',
            }}
          >
            {/* Decorative glows — white/10, blurred circles */}
            {[{ top: -80, left: -80 }, { bottom: -80, right: -80 }].map((pos, i) => (
              <Box
                key={i}
                sx={{
                  position: 'absolute',
                  width: 320,
                  height: 320,
                  borderRadius: '50%',
                  bgcolor: 'rgba(255,255,255,0.08)',
                  filter: 'blur(40px)',
                  ...pos,
                }}
              />
            ))}

            {/* Display heading — white */}
            <Typography
              sx={{
                position: 'relative',
                fontFamily: dt.typography.fontFamily,
                fontSize: { xs: '2rem', md: dt.typography.display.fontSize },
                fontWeight: dt.typography.display.fontWeight,
                lineHeight: dt.typography.display.lineHeight,
                letterSpacing: dt.typography.display.letterSpacing,
                color: dt.colors.onPrimary,
                mb: 3,
              }}
            >
              Ready to test your mastery?
            </Typography>

            {/* body-lg subtitle — lighter white (on-primary-container) */}
            <Typography
              sx={{
                position: 'relative',
                fontFamily: dt.typography.fontFamily,
                ...dt.typography.bodyLg,
                color: dt.colors.onPrimaryContainer,
                maxWidth: 520,
                mx: 'auto',
                mb: 5,
              }}
            >
              Join over 50,000 students and professionals already improving their expertise
              through Mezon Quiz.
            </Typography>

            {/* Action buttons */}
            <Stack
              direction={{ xs: 'column', sm: 'row' }}
              spacing={2}
              justifyContent="center"
              sx={{ position: 'relative' }}
            >
              {/* Inverse primary button — white bg, primary text */}
              <Button
                onClick={handleGetStarted}
                sx={{
                  bgcolor: dt.colors.surfaceContainerLowest,
                  color: dt.colors.primary,
                  fontFamily: dt.typography.fontFamily,
                  ...dt.typography.button,
                  borderRadius: dt.radius.default,
                  px: 4,
                  py: 1.75,
                  textTransform: 'none',
                  boxShadow: '0 8px 30px rgba(0,0,0,0.15)',
                  transition: 'all 0.2s',
                  '&:hover': {
                    bgcolor: dt.colors.surfaceContainerLow,
                    transform: 'scale(1.03)',
                  },
                  '&:active': { transform: 'scale(0.97)' },
                }}
              >
                Get Started Free
              </Button>

              {/* Ghost outlined — white border/text */}
              <Button
                variant="outlined"
                sx={{
                  color: dt.colors.onPrimary,
                  borderColor: 'rgba(255,255,255,0.35)',
                  fontFamily: dt.typography.fontFamily,
                  ...dt.typography.button,
                  borderRadius: dt.radius.default,
                  px: 4,
                  py: 1.75,
                  textTransform: 'none',
                  transition: 'all 0.2s',
                  '&:hover': {
                    bgcolor: 'rgba(255,255,255,0.1)',
                    borderColor: 'rgba(255,255,255,0.65)',
                  },
                }}
              >
                Talk to Learning Advisor
              </Button>
            </Stack>
          </Box>
        </Container>
      </Box>
    </>
  );
};

export default Explore;
