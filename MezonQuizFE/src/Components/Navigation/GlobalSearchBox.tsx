import { useState, useEffect, useRef } from 'react';
import { TextField, InputAdornment, Box, Paper, Typography, CircularProgress, MenuItem, Stack, Divider, IconButton } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { useDebounce } from '../../Hooks/useDebounce';
import { searchGlobal } from '../../Api/search.api';
import type { GlobalSearchResult, SearchItem } from '../../Interface/search.dto';
import useAuthStore from '../../Stores/login.store';
import { ACCESS_PERMISSIONS, hasAnyPermission } from '../../Lib/Utils/permissions';

type SearchBoxColors = {
  textPrimary: string;
  textSecondary: string;
  fieldBg: string;
  paperBg: string;
  border: string;
};

interface GlobalSearchBoxProps {
  colors: SearchBoxColors;
  currentTitle: string;
}

const GlobalSearchBox = ({ colors, currentTitle }: GlobalSearchBoxProps) => {
  const navigate = useNavigate();
  const [searchValue, setSearchValue] = useState('');
  const [isOpen, setIsOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [results, setResults] = useState<GlobalSearchResult | null>(null);

  const permissionName = useAuthStore((state) => state.permissionName);
  const hasSystemRole = useAuthStore((state) => state.hasSystemRole);

  const canViewCategories = hasAnyPermission(permissionName, ACCESS_PERMISSIONS.CATEGORY_PAGE, hasSystemRole);
  const canViewQuizzes = hasAnyPermission(permissionName, ACCESS_PERMISSIONS.QUIZ_WORKSPACE, hasSystemRole);

  const searchContainerRef = useRef<HTMLDivElement>(null);
  const searchRequestIdRef = useRef(0);
  const debouncedSearchTerm = useDebounce(searchValue, 300);

  useEffect(() => {
    const requestId = searchRequestIdRef.current + 1;
    searchRequestIdRef.current = requestId;

    const fetchResults = async () => {
      if (!debouncedSearchTerm.trim()) {
        setResults(null);
        setIsOpen(false);
        setLoading(false);
        return;
      }

      setLoading(true);
      setIsOpen(true);
      try {
        const data = await searchGlobal(debouncedSearchTerm, 5);
        if (searchRequestIdRef.current !== requestId) {
          return;
        }
        setResults(data);
      } catch (error) {
        if (searchRequestIdRef.current === requestId) {
          console.error('Failed to search', error);
        }
      } finally {
        if (searchRequestIdRef.current === requestId) {
          setLoading(false);
        }
      }
    };

    fetchResults();
  }, [debouncedSearchTerm]);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (searchContainerRef.current && !searchContainerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleSearchEnter = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' && searchValue.trim() !== '') {
      setIsOpen(false);
      navigate(`/app/search?q=${encodeURIComponent(searchValue.trim())}`);
    }
  };

  const handleItemClick = (url: string) => {
    setIsOpen(false);
    setSearchValue('');
    navigate(url);
  };

  const renderSection = (title: string, items: SearchItem[]) => {
    if (!items || items.length === 0) return null;
    return (
      <Box sx={{ mb: 1 }}>
        <Typography variant="overline" sx={{ px: 2, color: colors.textSecondary, fontWeight: 700 }}>
          {title}
        </Typography>
        {items.map((item) => (
          <MenuItem
            key={item.id}
            onClick={() => handleItemClick(item.url)}
            sx={{ px: 2, py: 1 }}
          >
            <Box>
              <Typography variant="body2" sx={{ color: colors.textPrimary, fontWeight: 600 }}>
                {item.title}
              </Typography>
              {item.description && (
                <Typography variant="caption" sx={{ color: colors.textSecondary }}>
                  {item.description}
                </Typography>
              )}
            </Box>
          </MenuItem>
        ))}
      </Box>
    );
  };

  const hasResults = results && (
    (canViewQuizzes && results.quizzes.length > 0) || 
    (canViewCategories && results.categories.length > 0)
  );

  return (
    <Box ref={searchContainerRef} sx={{ position: 'relative', maxWidth: 420, width: '100%' }}>
      <TextField
        size="small"
        placeholder={`Search in ${currentTitle} (Press Enter to search all)`}
        value={searchValue}
        onChange={(e) => setSearchValue(e.target.value)}
        onKeyDown={handleSearchEnter}
        onFocus={() => {
          if (searchValue.trim()) setIsOpen(true);
        }}
        fullWidth
        sx={{
          '& .MuiInputBase-root': {
            color: colors.textPrimary,
            bgcolor: colors.fieldBg,
            borderRadius: 2
          }
        }}
        InputProps={{
          startAdornment: (
            <InputAdornment position="start">
              <Box
                component="svg"
                viewBox="0 0 24 24"
                sx={{ width: 16, height: 16, color: colors.textSecondary }}
                fill="none"
                stroke="currentColor"
              >
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </Box>
            </InputAdornment>
          ),
          endAdornment: searchValue ? (
            <InputAdornment position="end">
              <IconButton size="small" onClick={() => { setSearchValue(''); setIsOpen(false); }}>
                <Box
                  component="svg"
                  viewBox="0 0 24 24"
                  sx={{ width: 14, height: 14, color: colors.textSecondary }}
                  fill="none"
                  stroke="currentColor"
                >
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M6 18L18 6M6 6l12 12" />
                </Box>
              </IconButton>
            </InputAdornment>
          ) : null,
        }}
      />

      {isOpen && (
        <Paper
          elevation={8}
          sx={{
            position: 'absolute',
            top: '100%',
            left: 0,
            right: 0,
            mt: 1,
            maxHeight: 500,
            overflowY: 'auto',
            zIndex: 1300,
            bgcolor: colors.paperBg,
            border: `1px solid ${colors.border}`,
            borderRadius: 2,
            boxShadow: '0 10px 25px rgba(0,0,0,0.1)',
          }}
        >
          {loading ? (
            <Box sx={{ p: 3, display: 'flex', justifyContent: 'center' }}>
              <CircularProgress size={24} sx={{ color: '#0ea5e9' }} />
            </Box>
          ) : !hasResults && searchValue.trim() ? (
            <Box sx={{ p: 3, textAlign: 'center' }}>
              <Typography variant="body2" sx={{ color: colors.textSecondary }}>
                No results found for "{searchValue}"
              </Typography>
            </Box>
          ) : (
            <Stack divider={<Divider sx={{ borderColor: colors.border }} />}>
              {canViewQuizzes && results && renderSection('Quizzes', results.quizzes)}
              {canViewCategories && results && renderSection('Categories', results.categories)}

              {hasResults && (
                <MenuItem
                  onClick={() => {
                    setIsOpen(false);
                    navigate(`/app/search?q=${encodeURIComponent(searchValue.trim())}`);
                  }}
                  sx={{ justifyContent: 'center', py: 1.5, bgcolor: 'rgba(14, 165, 233, 0.05)' }}
                >
                  <Typography variant="body2" sx={{ color: '#0ea5e9', fontWeight: 600 }}>
                    View all results
                  </Typography>
                </MenuItem>
              )}
            </Stack>
          )}
        </Paper>
      )}
    </Box>
  );
};

export default GlobalSearchBox;
