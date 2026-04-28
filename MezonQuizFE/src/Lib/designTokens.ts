/**
 * Design tokens from DESIGN.md (Clarity & Knowledge system)
 * Source: StitchDesign/DESIGN.md
 */

export const dt = {
  // ── Colors ─────────────────────────────────────────────────────────────
  colors: {
    // Background
    background: '#f8f9ff',
    surface: '#f8f9ff',
    surfaceBright: '#f8f9ff',
    surfaceDim: '#d0dbed',
    surfaceContainerLowest: '#ffffff',
    surfaceContainerLow: '#eff4ff',
    surfaceContainer: '#e6eeff',
    surfaceContainerHigh: '#dee9fc',
    surfaceContainerHighest: '#d9e3f6',
    surfaceVariant: '#d9e3f6',
    surfaceTint: '#4d44e3',

    // On-surface text
    onBackground: '#121c2a',
    onSurface: '#121c2a',
    onSurfaceVariant: '#464555',
    inverseSurface: '#27313f',
    inverseOnSurface: '#eaf1ff',

    // Borders
    outline: '#777587',
    outlineVariant: '#c7c4d8',

    // Primary (Indigo)
    primary: '#3525cd',
    onPrimary: '#ffffff',
    primaryContainer: '#4f46e5',
    onPrimaryContainer: '#dad7ff',
    inversePrimary: '#c3c0ff',
    primaryFixed: '#e2dfff',
    primaryFixedDim: '#c3c0ff',
    onPrimaryFixed: '#0f0069',
    onPrimaryFixedVariant: '#3323cc',

    // Secondary (Cyan)
    secondary: '#00687a',
    onSecondary: '#ffffff',
    secondaryContainer: '#57dffe',
    onSecondaryContainer: '#006172',
    secondaryFixed: '#acedff',
    secondaryFixedDim: '#4cd7f6',
    onSecondaryFixed: '#001f26',
    onSecondaryFixedVariant: '#004e5c',

    // Tertiary (Purple)
    tertiary: '#571ac0',
    onTertiary: '#ffffff',
    tertiaryContainer: '#6f3dd9',
    onTertiaryContainer: '#e3d5ff',
    tertiaryFixed: '#e9ddff',
    tertiaryFixedDim: '#d0bcff',
    onTertiaryFixed: '#23005c',
    onTertiaryFixedVariant: '#5516be',

    // Error
    error: '#ba1a1a',
    onError: '#ffffff',
    errorContainer: '#ffdad6',
    onErrorContainer: '#93000a',
  },

  // ── Typography ──────────────────────────────────────────────────────────
  typography: {
    fontFamily: '"Inter", system-ui, -apple-system, sans-serif',
    display: {
      fontSize: '3rem',      // 48px
      fontWeight: 700,
      lineHeight: 1.1,
      letterSpacing: '-0.02em',
    },
    h1: {
      fontSize: '2rem',      // 32px
      fontWeight: 700,
      lineHeight: 1.2,
      letterSpacing: '-0.015em',
    },
    h2: {
      fontSize: '1.5rem',    // 24px
      fontWeight: 600,
      lineHeight: 1.3,
    },
    h3: {
      fontSize: '1.25rem',   // 20px
      fontWeight: 600,
      lineHeight: 1.4,
    },
    bodyLg: {
      fontSize: '1.125rem',  // 18px
      fontWeight: 400,
      lineHeight: 1.6,
    },
    bodyMd: {
      fontSize: '1rem',      // 16px
      fontWeight: 400,
      lineHeight: 1.5,
    },
    labelSm: {
      fontSize: '0.875rem',  // 14px
      fontWeight: 500,
      lineHeight: 1.2,
      letterSpacing: '0.05em',
    },
    button: {
      fontSize: '1rem',      // 16px
      fontWeight: 600,
      lineHeight: 1,
    },
  },

  // ── Border Radius ───────────────────────────────────────────────────────
  radius: {
    sm: '0.25rem',
    default: '0.5rem',
    md: '0.75rem',
    lg: '1rem',
    xl: '1.5rem',
    full: '9999px',
  },

  // ── Spacing (8px scale) ─────────────────────────────────────────────────
  spacing: {
    unit: '8px',
    stackSm: '8px',
    stackMd: '16px',
    stackLg: '32px',
    gutter: '24px',
    margin: '32px',
    sectionPadding: '64px',
    containerMax: '1120px',
  },

  // ── Shadows (soft, low-opacity indigo tint) ─────────────────────────────
  shadows: {
    card: '0 20px 30px -10px rgba(79, 70, 229, 0.08)',
    cardHover: '0 24px 40px -8px rgba(79, 70, 229, 0.16)',
    ctaButton: '0 8px 24px rgba(53, 37, 205, 0.3)',
  },
} as const;
