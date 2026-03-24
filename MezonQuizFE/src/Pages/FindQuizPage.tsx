import { useCallback, useEffect, useMemo, useState } from "react";
import {
    Box,
    Card,
    CardContent,
    Chip,
    CircularProgress,
    Container,
    IconButton,
    InputAdornment,
    Pagination,
    Stack,
    TextField,
    Tooltip,
    Typography,
} from "@mui/material";
import { alpha, useTheme } from "@mui/material/styles";
import AppSnackbar from "../Components/AppSnackbar";
import useAppSnackbar from "../Hooks/useAppSnackbar";
import { MdRefresh, MdSearch } from "react-icons/md";
import { useNavigate } from "react-router-dom";
import { getAllCategories } from "../Api/category.api";
import { getAvailableQuizzes } from "../Api/quiz.api";
import { joinQuizSession } from "../Api/session.api";
import type { CategoryDto } from "../Interface/category.dto";
import type { AvailableQuizDto } from "../Interface/quiz.dto";
import useAuthStore from "../Stores/login.store";

const PAGE_SIZE = 9;

const FindQuizPage = () => {
    const navigate = useNavigate();
    const theme = useTheme();
    const isDark = theme.palette.mode === "dark";
    const panelBackground = isDark
        ? "linear-gradient(150deg, rgba(10,22,40,0.97) 0%, rgba(7,14,26,0.95) 100%)"
        : "linear-gradient(150deg, rgba(255,255,255,0.96) 0%, rgba(241,245,249,0.96) 100%)";
    const panelBorder = alpha(theme.palette.text.secondary, isDark ? 0.35 : 0.25);
    const panelShadow = isDark
        ? "0 20px 60px rgba(0, 0, 0, 0.35)"
        : "0 18px 42px rgba(15, 23, 42, 0.12)";
    const strongText = theme.palette.text.primary;
    const softText = theme.palette.text.secondary;

    const userId = useAuthStore((state) => state.user?.id);
    const [categories, setCategories] = useState<CategoryDto[]>([]);
    const [selectedCategory, setSelectedCategory] = useState<string>("all");
    const [searchTitle, setSearchTitle] = useState("");
    const [page, setPage] = useState(1);

    const [items, setItems] = useState<AvailableQuizDto[]>([]);
    const [totalPages, setTotalPages] = useState(0);
    const [totalCount, setTotalCount] = useState(0);

    const [isLoading, setIsLoading] = useState(false);
    const [sessionId, setSessionId] = useState("");
    const [isJoining, setIsJoining] = useState(false);
    const { snackbar, showError, showSuccess, closeSnackbar } = useAppSnackbar();

    const categoryParam = selectedCategory === "all" ? undefined : selectedCategory;
    const titleParam = searchTitle.trim() || undefined;

    const activeFilterCount = useMemo(() => {
        let count = 0;
        if (categoryParam) count += 1;
        if (titleParam) count += 1;
        return count;
    }, [categoryParam, titleParam]);

    useEffect(() => {
        const loadCategories = async () => {
            try {
                const data = await getAllCategories();
                setCategories(data);
            } catch {
                // Keep the page functional even if category API is unavailable.
                setCategories([]);
            }
        };

        void loadCategories();
    }, []);

    useEffect(() => {
        const timer = window.setTimeout(() => {
            setPage(1);
        }, 250);

        return () => window.clearTimeout(timer);
    }, [searchTitle, selectedCategory]);

    const fetchQuizzes = useCallback(async () => {
        setIsLoading(true);

        try {
            const data = await getAvailableQuizzes({
                category: categoryParam,
                title: titleParam,
                page,
                pageSize: PAGE_SIZE,
            });

            setItems(data.items);
            setTotalPages(data.totalPages);
            setTotalCount(data.totalCount);
        } catch {
            showError("Can not load quiz list right now. Please try again.");
            setItems([]);
            setTotalPages(0);
            setTotalCount(0);
        } finally {
            setIsLoading(false);
        }
    }, [categoryParam, page, titleParam, showError]);

    useEffect(() => {
        void fetchQuizzes();
        // Intentionally include all query states to refetch as user changes filters.
    }, [fetchQuizzes]);

    const handleJoinSession = async () => {
        const normalizedSessionId = sessionId.trim();
        if (!normalizedSessionId) {
            showError("Please enter a session ID.");
            return;
        }

        if (!userId) {
            showError("User is not available. Please login again.");
            return;
        }

        try {
            setIsJoining(true);

            const response = await joinQuizSession(normalizedSessionId, { userId });
            showSuccess(response.message || "Joined session successfully.");
            navigate(`/app/sessions/${normalizedSessionId}`);
        } catch {
            showError("Can not join this session now. Please check session ID and try again.");
        } finally {
            setIsJoining(false);
        }
    };

    return (
        <Box
            sx={{
                minHeight: "100vh",
                py: { xs: 4, md: 7 }
            }}
        >
            <Container maxWidth="lg">
                <Stack spacing={4}>
                    <Box
                        sx={{
                            p: { xs: 2.5, md: 4 },
                            borderRadius: 4,
                            background: panelBackground,
                            border: `1px solid ${panelBorder}`,
                            boxShadow: panelShadow,
                        }}
                    >
                        <Stack spacing={2.5}>
                            <Stack
                                direction={{ xs: "column", sm: "row" }}
                                justifyContent="space-between"
                                alignItems={{ xs: "flex-start", sm: "center" }}
                                spacing={1}
                            >
                                <Box>
                                    <Typography
                                        sx={{
                                            fontSize: { xs: "1.8rem", md: "2.35rem" },
                                            lineHeight: 1.1,
                                            fontWeight: 800,
                                            letterSpacing: "-0.03em",
                                            color: strongText,
                                        }}
                                    >
                                        Discover Quiz
                                    </Typography>
                                    <Typography sx={{ mt: 1, color: softText }}>
                                        Search by title, filter by category, and start practicing quickly.
                                    </Typography>
                                </Box>

                                <Tooltip title="Reload list">
                                    <IconButton
                                        onClick={() => {
                                            void fetchQuizzes();
                                        }}
                                        sx={{
                                            color: strongText,
                                            bgcolor: isDark
                                                ? "rgba(15, 23, 42, 0.95)"
                                                : alpha(theme.palette.background.paper, 0.98),
                                            border: `1px solid ${alpha(theme.palette.text.secondary, 0.3)}`,
                                            "&:hover": {
                                                bgcolor: isDark
                                                    ? "rgba(30, 41, 59, 0.95)"
                                                    : alpha(theme.palette.background.paper, 0.9),
                                            },
                                        }}
                                    >
                                        <MdRefresh />
                                    </IconButton>
                                </Tooltip>
                            </Stack>

                            <TextField
                                placeholder="Search by quiz title..."
                                value={searchTitle}
                                onChange={(event) => {
                                    setSearchTitle(event.target.value);
                                }}
                                fullWidth
                                InputProps={{
                                    startAdornment: (
                                        <InputAdornment position="start">
                                            <MdSearch color={theme.palette.text.secondary} />
                                        </InputAdornment>
                                    ),
                                }}
                                sx={{
                                    "& .MuiOutlinedInput-root": {
                                        borderRadius: 3,
                                        backgroundColor: isDark
                                            ? "rgba(15, 23, 42, 0.95)"
                                            : alpha(theme.palette.background.paper, 0.98),
                                    },
                                    "& .MuiOutlinedInput-input": {
                                        color: theme.palette.text.primary,
                                    },
                                }}
                            />

                            <Box>
                                <Typography
                                    variant="subtitle2"
                                    sx={{ mb: 1.2, color: softText, fontWeight: 700 }}
                                >
                                    Category
                                </Typography>
                                <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap">
                                    <Chip
                                        label="All"
                                        clickable
                                        color={selectedCategory === "all" ? "primary" : "default"}
                                        variant={selectedCategory === "all" ? "filled" : "outlined"}
                                        sx={{ color: selectedCategory === "all" ? "#fff" : softText }}
                                        onClick={() => {
                                            setSelectedCategory("all");
                                        }}
                                    />
                                    {categories.map((category) => {
                                        const selected = selectedCategory === category.id;

                                        return (
                                            <Chip
                                                key={category.id}
                                                label={category.name}
                                                clickable
                                                color={selected ? "primary" : "default"}
                                                variant={selected ? "filled" : "outlined"}
                                                sx={{ color: selected ? "#fff" : softText }}
                                                onClick={() => {
                                                    setSelectedCategory(category.id);
                                                }}
                                            />
                                        );
                                    })}
                                </Stack>
                            </Box>

                            <Typography sx={{ fontSize: "0.92rem", color: softText }}>
                                {totalCount} quizzes found
                                {activeFilterCount > 0 ? ` • ${activeFilterCount} active filter(s)` : ""}
                            </Typography>

                            <Stack
                                direction={{ xs: "column", sm: "row" }}
                                spacing={1.2}
                                alignItems={{ xs: "stretch", sm: "center" }}
                            >
                                <TextField
                                    size="small"
                                    label="Join by Session ID"
                                    placeholder="Paste session id"
                                    value={sessionId}
                                    onChange={(event) => {
                                        setSessionId(event.target.value);
                                    }}
                                    sx={{ maxWidth: { xs: "100%", sm: 360 } }}
                                />
                                <Chip
                                    label={isJoining ? "Joining..." : "Join Session"}
                                    color="primary"
                                    clickable={!isJoining}
                                    sx={{ color: "#fff", fontWeight: 600 }}
                                    onClick={() => {
                                        if (!isJoining) {
                                            void handleJoinSession();
                                        }
                                    }}
                                />
                            </Stack>
                        </Stack>
                    </Box>

                    {isLoading ? (
                        <Stack alignItems="center" justifyContent="center" py={8} spacing={1.5}>
                            <CircularProgress />
                            <Typography color="text.secondary">Loading public quizzes...</Typography>
                        </Stack>
                    ) : items.length === 0 ? (
                        <Card
                            sx={{
                                borderRadius: 3,
                                border: `1px dashed ${alpha(theme.palette.text.secondary, 0.45)}`,
                                background: isDark
                                    ? "rgba(15, 23, 42, 0.88)"
                                    : alpha(theme.palette.background.paper, 0.96),
                            }}
                        >
                            <CardContent>
                                <Typography variant="h6" fontWeight={700} sx={{ color: strongText }}>
                                    No quizzes matched your filter
                                </Typography>
                                <Typography sx={{ mt: 1, color: softText }}>
                                    Try another category or use fewer keywords.
                                </Typography>
                            </CardContent>
                        </Card>
                    ) : (
                        <>
                            <Box
                                sx={{
                                    display: "grid",
                                    gridTemplateColumns: {
                                        xs: "1fr",
                                        sm: "repeat(2, minmax(0, 1fr))",
                                        md: "repeat(3, minmax(0, 1fr))",
                                    },
                                    gap: 2,
                                }}
                            >
                                {items.map((quiz, index) => {
                                    return (
                                        <Card
                                            key={quiz.id}
                                            sx={{
                                                borderRadius: 3,
                                                border: `1px solid ${alpha(theme.palette.text.secondary, 0.3)}`,
                                                background: isDark
                                                    ? "linear-gradient(160deg, rgba(15,23,42,0.94) 0%, rgba(10,18,33,0.92) 100%)"
                                                    : "linear-gradient(160deg, rgba(255,255,255,0.96) 0%, rgba(241,245,249,0.94) 100%)",
                                                transition: "transform 0.25s ease, box-shadow 0.25s ease",
                                                animation: `fade-up 300ms ease ${Math.min(index * 45, 280)}ms both`,
                                                "@keyframes fade-up": {
                                                    from: { opacity: 0, transform: "translateY(10px)" },
                                                    to: { opacity: 1, transform: "translateY(0)" },
                                                },
                                                "&:hover": {
                                                    transform: "translateY(-3px)",
                                                    boxShadow: isDark
                                                        ? "0 14px 28px rgba(2, 6, 23, 0.45)"
                                                        : "0 12px 24px rgba(15, 23, 42, 0.18)",
                                                },
                                            }}
                                        >
                                            <CardContent>
                                                <Stack spacing={1.3}>
                                                    <Typography variant="h6" sx={{ fontWeight: 700, color: strongText }}>
                                                        {quiz.title}
                                                    </Typography>
                                                    <Typography
                                                        sx={{
                                                            fontSize: "0.8rem",
                                                            color: softText,
                                                            wordBreak: "break-all",
                                                        }}
                                                    >
                                                        Quiz ID: {quiz.id}
                                                    </Typography>
                                                </Stack>
                                            </CardContent>
                                        </Card>
                                    );
                                })}
                            </Box>

                            {totalPages > 1 && (
                                <Stack alignItems="center" pt={1}>
                                    <Pagination
                                        page={page}
                                        count={totalPages}
                                        color="primary"
                                        shape="rounded"
                                        onChange={(_event, value) => {
                                            setPage(value);
                                        }}
                                    />
                                </Stack>
                            )}
                        </>
                    )}
                </Stack>
            </Container>

            <AppSnackbar
                open={snackbar.open}
                message={snackbar.message}
                severity={snackbar.severity}
                onClose={closeSnackbar}
            />
        </Box>
    );
};

export default FindQuizPage;