import { useEffect, useRef, useState } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { Box, Typography, CircularProgress, Card, CardActionArea, CardContent, Grid, Chip } from '@mui/material';
import { searchGlobal } from '../../Api/search.api';
import type { GlobalSearchResult, SearchItem } from '../../Interface/search.dto';
import useThemeStore from '../../Stores/theme.store';
import useAuthStore from '../../Stores/login.store';
import { ACCESS_PERMISSIONS, hasAnyPermission } from '../../Lib/Utils/permissions';

const GlobalSearchPage = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const query = searchParams.get('q') || '';
  const [results, setResults] = useState<GlobalSearchResult | null>(null);
  const [loading, setLoading] = useState(false);
  const searchRequestIdRef = useRef(0);
  const themeMode = useThemeStore((state) => state.themeMode);

  const permissionName = useAuthStore((state) => state.permissionName);
  const hasSystemRole = useAuthStore((state) => state.hasSystemRole);

  const canViewCategories = hasAnyPermission(permissionName, ACCESS_PERMISSIONS.CATEGORY_PAGE, hasSystemRole);
  const canViewQuizzes = hasAnyPermission(permissionName, ACCESS_PERMISSIONS.QUIZ_WORKSPACE, hasSystemRole);

  const colors = themeMode === 'dark' ? {
    paperBg: '#0e1a2b',
    textPrimary: '#e2e8f0',
    textSecondary: '#94a3b8',
    border: 'rgba(148,163,184,0.18)',
  } : {
    paperBg: '#ffffff',
    textPrimary: '#0f172a',
    textSecondary: '#475569',
    border: 'rgba(71,85,105,0.22)',
  };

  useEffect(() => {
    const requestId = searchRequestIdRef.current + 1;
    searchRequestIdRef.current = requestId;

    const fetchSearchResults = async () => {
      if (!query.trim()) {
        setResults(null);
        setLoading(false);
        return;
      }
      setLoading(true);
      try {
        const data = await searchGlobal(query, 50);
        if (searchRequestIdRef.current !== requestId) {
          return;
        }
        setResults(data);
      } catch (error) {
        if (searchRequestIdRef.current === requestId) {
          console.error("Failed to search", error);
        }
      } finally {
        if (searchRequestIdRef.current === requestId) {
          setLoading(false);
        }
      }
    };

    fetchSearchResults();
  }, [query]);

  const ResultCard = ({ item }: { item: SearchItem }) => (
    <Card
      variant="outlined"
      sx={{
        bgcolor: colors.paperBg,
        borderColor: colors.border,
        borderRadius: 2,
        height: '100%',
        transition: 'transform 0.2s, box-shadow 0.2s',
        '&:hover': {
          transform: 'translateY(-2px)',
          boxShadow: '0 4px 20px rgba(0,0,0,0.1)',
          borderColor: '#0ea5e9'
        }
      }}
    >
      <CardActionArea onClick={() => navigate(item.url)} sx={{ height: '100%' }}>
        <CardContent>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
            <Typography variant="h6" sx={{ color: colors.textPrimary, fontSize: '1.1rem', fontWeight: 600 }}>
              {item.title}
            </Typography>
            <Chip
              label={item.type}
              size="small"
              sx={{
                bgcolor: item.type === 'Quiz' ? 'rgba(14,165,233,0.1)' : item.type === 'User' ? 'rgba(168,85,247,0.1)' : 'rgba(34,197,94,0.1)',
                color: item.type === 'Quiz' ? '#0ea5e9' : item.type === 'User' ? '#a855f7' : '#22c55e',
                fontWeight: 600,
                fontSize: '0.7rem'
              }}
            />
          </Box>
          {item.description && (
            <Typography variant="body2" sx={{ color: colors.textSecondary }}>
              {item.description}
            </Typography>
          )}
        </CardContent>
      </CardActionArea>
    </Card>
  );

  const renderSection = (title: string, items: SearchItem[]) => {
    if (!items || items.length === 0) return null;
    return (
      <Box sx={{ mb: 4 }}>
        <Typography variant="h6" sx={{ color: colors.textPrimary, fontWeight: 700, mb: 2 }}>
          {title} ({items.length})
        </Typography>
        <Grid container spacing={2}>
          {items.map(item => (
            <Grid size={{ xs: 12, sm: 6, md: 4 }} key={item.id}>
              <ResultCard item={item} />
            </Grid>
          ))}
        </Grid>
      </Box>
    );
  };

  const hasResults = results && (
    (canViewQuizzes && results.quizzes.length > 0) ||
    (canViewCategories && results.categories.length > 0)
  );

  return (
    <Box sx={{ maxWidth: 1200, mx: 'auto', width: '100%' }}>
      <Box sx={{ mb: 4 }}>
        <Typography variant="h4" sx={{ color: colors.textPrimary, fontWeight: 800, mb: 1 }}>
          Search Results
        </Typography>
        <Typography variant="body1" sx={{ color: colors.textSecondary }}>
          Showing results for <Box component="span" sx={{ fontWeight: 700, color: '#0ea5e9' }}>"{query}"</Box>
        </Typography>
      </Box>

      {loading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
          <CircularProgress sx={{ color: '#0ea5e9' }} />
        </Box>
      ) : !query.trim() ? (
        <Box sx={{ py: 8, textAlign: 'center' }}>
          <Typography variant="h6" sx={{ color: colors.textSecondary }}>
            Please enter a search term to see results.
          </Typography>
        </Box>
      ) : !hasResults ? (
        <Box sx={{ py: 8, textAlign: 'center' }}>
          <Typography variant="h6" sx={{ color: colors.textSecondary, mb: 1 }}>
            No results found
          </Typography>
          <Typography variant="body2" sx={{ color: colors.textSecondary }}>
            Try adjusting your search or check your spelling.
          </Typography>
        </Box>
      ) : (
        <Box>
          {canViewQuizzes && results && renderSection('Quizzes', results.quizzes)}
          {canViewCategories && results && renderSection('Categories', results.categories)}
        </Box>
      )}
    </Box>
  );
};

export default GlobalSearchPage;
