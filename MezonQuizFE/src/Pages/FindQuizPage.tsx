import { memo, useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
    Button,
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
import type { CategoryDto } from "../Interface/category.dto";
import type { AvailableQuizDto } from "../Interface/quiz.dto";
import CategoryIconBadge from "../Lib/Utils/categoryIconBadge";

const PAGE_SIZE = 9;
const ALL_GROUP_PREVIEW_SIZE = 4;
const CARD_RADIUS = 1;


type QuizGroup = {
    categoryId: string;
    categoryName: string;
    iconKey?: string;
    items: AvailableQuizDto[];
    totalCount: number;
};

type CachedQuizData =
    | { mode: "all"; groupedItems: QuizGroup[] }
    | { mode: "single"; items: AvailableQuizDto[]; totalPages: number };

type QuizCardProps = {
    quizId: string;
    title: string;
    categoryName: string;
    iconKey?: string;
    index: number;
    isDark: boolean;
    strongText: string;
    softText: string;
    onOpenQuiz: (quizId: string) => void;
};

const QuizCard = memo((props: QuizCardProps) => {
    const {
        quizId,
        title,
        categoryName,
        iconKey,
        index,
        isDark,
        strongText,
        softText,
        onOpenQuiz,
    } = props;

    const handleOpen = useCallback(() => {
        onOpenQuiz(quizId);
    }, [onOpenQuiz, quizId]);

    return (
        <Card
            onClick={handleOpen}
            sx={{
                borderRadius: CARD_RADIUS,
                border: "1px solid",
                borderColor: "text.secondary",
                background: isDark
                    ? "linear-gradient(160deg, rgba(15,23,42,0.94) 0%, rgba(10,18,33,0.92) 100%)"
                    : "linear-gradient(160deg, rgba(255,255,255,0.96) 0%, rgba(241,245,249,0.94) 100%)",
                transition: "transform 0.25s ease, box-shadow 0.25s ease",
                cursor: "pointer",
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
                        {title}
                    </Typography>
                    <Chip
                        size="small"
                        variant="outlined"
                        sx={{
                            width: "fit-content",
                            borderColor: "text.secondary",
                            "& .MuiChip-label": {
                                px: 1,
                                py: 0.25,
                            },
                        }}
                        label={(
                            <Stack direction="row" spacing={0.6} alignItems="center">
                                <CategoryIconBadge iconKey={iconKey} size={18} fallback={null} />
                                <Typography sx={{ fontSize: "0.82rem", color: softText }}>
                                    {categoryName}
                                </Typography>
                            </Stack>
                        )}
                    />
                </Stack>
            </CardContent>
        </Card>
    );
});

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
    const emptyCardSx = {
        borderRadius: CARD_RADIUS,
        border: `1px dashed ${alpha(theme.palette.text.secondary, 0.45)}`,
        background: isDark
            ? "rgba(15, 23, 42, 0.88)"
            : alpha(theme.palette.background.paper, 0.96),
    };
    const categoryPanelSx = {
        borderRadius: CARD_RADIUS,
        border: `1px solid ${alpha(theme.palette.text.secondary, 0.25)}`,
        background: isDark
            ? "rgba(15, 23, 42, 0.9)"
            : alpha(theme.palette.background.paper, 0.95),
    };
    const [categories, setCategories] = useState<CategoryDto[]>([]);
    const [selectedCategory, setSelectedCategory] = useState<string>("all");
    const [searchTitle, setSearchTitle] = useState("");
    const [page, setPage] = useState(1);

    const [items, setItems] = useState<AvailableQuizDto[]>([]);
    const [groupedItems, setGroupedItems] = useState<QuizGroup[]>([]);
    const [totalPages, setTotalPages] = useState(0);

    const [isLoading, setIsLoading] = useState(false);
    const {snackbar, showError, closeSnackbar } = useAppSnackbar();
    const quizCacheRef = useRef<Map<string, CachedQuizData>>(new Map());

    const categoryParam = selectedCategory === "all" ? undefined : selectedCategory;
    const normalizedSearchTitle = searchTitle.trim().toLowerCase();

    const categoryById = useMemo(() => {
        const map = new Map<string, CategoryDto>();
        categories.forEach((category) => {
            map.set(category.id, category);
        });
        return map;
    }, [categories]);

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
        setPage(1);
    }, [selectedCategory]);

    const filteredGroupedItems = useMemo(() => {
        if (!normalizedSearchTitle) {
            return groupedItems;
        }

        return groupedItems
            .map((group) => ({
                ...group,
                items: group.items.filter((quiz) =>
                    quiz.title.toLowerCase().includes(normalizedSearchTitle)
                ),
            }))
            .filter((group) => group.items.length > 0);
    }, [groupedItems, normalizedSearchTitle]);

    const filteredItems = useMemo(() => {
        if (!normalizedSearchTitle) {
            return items;
        }

        return items.filter((quiz) =>
            quiz.title.toLowerCase().includes(normalizedSearchTitle)
        );
    }, [items, normalizedSearchTitle]);

    const handleOpenQuiz = useCallback((quizId: string) => {
        navigate(`/app/find-quizzes/${quizId}`);
    }, [navigate]);

    const fetchQuizzes = useCallback(async (forceRefresh = false) => {
        const cacheKey = selectedCategory === "all"
            ? "all"
            : `${selectedCategory}:${page}`;
        if (forceRefresh) {
            quizCacheRef.current.delete(cacheKey);
        }

        const cachedData = quizCacheRef.current.get(cacheKey);

        if (cachedData) {
            if (cachedData.mode === "all") {
                setGroupedItems(cachedData.groupedItems);
                setItems([]);
                setTotalPages(0);
                return;
            }

            setItems(cachedData.items);
            setGroupedItems([]);
            setTotalPages(cachedData.totalPages);
            return;
        }

        setIsLoading(true);

        try {
            if (selectedCategory === "all") {
                const categoryRequests = categories.map(async (category) => {
                    const response = await getAvailableQuizzes({
                        category: category.id,
                        page: 1,
                        pageSize: ALL_GROUP_PREVIEW_SIZE,
                    });

                    return {
                        categoryId: category.id,
                        categoryName: category.name,
                        iconKey: category.icon,
                        items: response.items,
                        totalCount: response.totalCount,
                    } satisfies QuizGroup;
                });

                const groups = (await Promise.all(categoryRequests)).filter(
                    (group) => group.totalCount > 0
                );

                quizCacheRef.current.set(cacheKey, {
                    mode: "all",
                    groupedItems: groups,
                });
                setGroupedItems(groups);
                setItems([]);
                setTotalPages(0);
                return;
            }

            const data = await getAvailableQuizzes({
                category: categoryParam,
                page,
                pageSize: PAGE_SIZE,
            });

            quizCacheRef.current.set(cacheKey, {
                mode: "single",
                items: data.items,
                totalPages: data.totalPages,
            });
            setItems(data.items);
            setGroupedItems([]);
            setTotalPages(data.totalPages);
        } catch {
            showError("Can not load quiz list right now. Please try again.");
            setItems([]);
            setGroupedItems([]);
            setTotalPages(0);
        } finally {
            setIsLoading(false);
        }
    }, [categories, categoryParam, page, selectedCategory, showError]);

    useEffect(() => {
        void fetchQuizzes();
        // Intentionally include all query states to refetch as user changes filters.
    }, [fetchQuizzes]);

    useEffect(() => {
        // Categories are part of "all" query shape, so invalidate previous "all" cache when they change.
        quizCacheRef.current.delete("all");
    }, [categories]);

    const handleCategoryChange = useCallback((categoryId: string) => {
        setSelectedCategory((current) => {
            if (current === categoryId) {
                return current;
            }
            return categoryId;
        });
    }, []);

    return (
        <Box
            sx={{
                minHeight: "100vh",
                py: { xs: 4, md: 7 }
            }}
        >
            <Container maxWidth="lg">
                <Box
                    sx={{
                        display: "grid",
                        gap: 2,
                        gridTemplateColumns: {
                            xs: "1fr",
                            md: "minmax(0, 1fr) minmax(240px, 280px)",
                        },
                        alignItems: "start",
                    }}
                >
                    <Stack spacing={2}>
                        <Box
                            sx={{
                                p: { xs: 2.5, md: 4 },
                                borderRadius: 1,
                                background: panelBackground,
                                border: `1px solid ${panelBorder}`,
                                boxShadow: panelShadow,
                            }}
                        >
                            <Stack spacing={2.2}>
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
                                                void fetchQuizzes(true);
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
                                            borderRadius: CARD_RADIUS,
                                            backgroundColor: isDark
                                                ? "rgba(15, 23, 42, 0.95)"
                                                : alpha(theme.palette.background.paper, 0.98),
                                        },
                                        "& .MuiOutlinedInput-input": {
                                            color: theme.palette.text.primary,
                                        },
                                    }}
                                />
                            </Stack>
                        </Box>

                        <Box>
                            {isLoading ? (
                                <Stack alignItems="center" justifyContent="center" py={8} spacing={1.5}>
                                    <CircularProgress />
                                    <Typography color="text.secondary">Loading public quizzes...</Typography>
                                </Stack>
                            ) : selectedCategory === "all" ? (
                                filteredGroupedItems.length === 0 ? (
                                    <Card sx={emptyCardSx}>
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
                                    <Stack spacing={2.2}>
                                        {filteredGroupedItems.map((group) => (
                                            <Box
                                                key={group.categoryId}
                                                sx={{
                                                    py: 0.5,
                                                }}
                                            >
                                                <Stack spacing={1.5}>
                                                    <Stack
                                                        direction={{ xs: "column", sm: "row" }}
                                                        alignItems={{ xs: "flex-start", sm: "center" }}
                                                        justifyContent="space-between"
                                                        spacing={1}
                                                    >
                                                        <Stack direction="row" spacing={0.9} alignItems="center">
                                                            <CategoryIconBadge iconKey={group.iconKey} size={20} fallback={null} />
                                                            <Typography variant="h6" sx={{ fontWeight: 700, color: strongText }}>
                                                                {group.categoryName}
                                                            </Typography>
                                                        </Stack>
                                                    </Stack>

                                                    <Box
                                                        sx={{
                                                            display: "grid",
                                                            gridTemplateColumns: {
                                                                xs: "1fr",
                                                                sm: "repeat(2, minmax(0, 1fr))",
                                                            },
                                                            gap: 2,
                                                        }}
                                                    >
                                                        {group.items.map((quiz, index) => (
                                                            <QuizCard
                                                                key={quiz.id}
                                                                quizId={quiz.id}
                                                                title={quiz.title}
                                                                categoryName={group.categoryName}
                                                                iconKey={group.iconKey}
                                                                index={index}
                                                                isDark={isDark}
                                                                strongText={strongText}
                                                                softText={softText}
                                                                onOpenQuiz={handleOpenQuiz}
                                                            />
                                                        ))}
                                                    </Box>

                                                        {group.totalCount > ALL_GROUP_PREVIEW_SIZE && (
                                                            <Box>
                                                                <Button
                                                                    variant="outlined"
                                                                    size="small"
                                                                    onClick={() => {
                                                                        setSelectedCategory(group.categoryId);
                                                                        setPage(1);
                                                                    }}
                                                                >
                                                                    View more ({group.totalCount - ALL_GROUP_PREVIEW_SIZE}+)
                                                                </Button>
                                                            </Box>
                                                        )}
                                                </Stack>
                                            </Box>
                                        ))}
                                    </Stack>
                                )
                            ) : filteredItems.length === 0 ? (
                                <Card sx={emptyCardSx}>
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
                                            },
                                            gap: 2,
                                        }}
                                    >
                                        {filteredItems.map((quiz, index) => {
                                            const quizCategory = quiz.categoryId ? categoryById.get(quiz.categoryId) : undefined;

                                            return (
                                                <QuizCard
                                                    key={quiz.id}
                                                    quizId={quiz.id}
                                                    title={quiz.title}
                                                    categoryName={quizCategory?.name || "Uncategorized"}
                                                    iconKey={quizCategory?.icon}
                                                    index={index}
                                                    isDark={isDark}
                                                    strongText={strongText}
                                                    softText={softText}
                                                    onOpenQuiz={handleOpenQuiz}
                                                />
                                            );
                                        })}
                                    </Box>

                                    {totalPages > 1 && !normalizedSearchTitle && (
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
                        </Box>
                    </Stack>

                    <Card sx={categoryPanelSx}>
                        <CardContent>
                            <Stack spacing={1}>
                                <Typography
                                    variant="subtitle2"
                                    sx={{ color: softText, fontWeight: 800, letterSpacing: "0.04em" }}
                                >
                                    CATEGORIES
                                </Typography>
                                <Chip
                                    label="All"
                                    clickable
                                    color={selectedCategory === "all" ? "primary" : "default"}
                                    variant={selectedCategory === "all" ? "filled" : "outlined"}
                                    sx={{
                                        justifyContent: "flex-start",
                                        width: "100%",
                                        color: selectedCategory === "all" ? "#fff" : softText,
                                    }}
                                    onClick={() => {
                                        handleCategoryChange("all");
                                    }}
                                />
                                {categories.map((category) => {
                                    const selected = selectedCategory === category.id;

                                    return (
                                        <Chip
                                            key={category.id}
                                            label={(
                                                <Stack direction="row" spacing={0.8} alignItems="center">
                                                    <CategoryIconBadge iconKey={category.icon} size={18} fallback={null} />
                                                    <Box component="span">{category.name}</Box>
                                                </Stack>
                                            )}
                                            clickable
                                            color={selected ? "primary" : "default"}
                                            variant={selected ? "filled" : "outlined"}
                                            sx={{
                                                justifyContent: "flex-start",
                                                width: "100%",
                                                color: selected ? "#fff" : softText,
                                            }}
                                            onClick={() => {
                                                handleCategoryChange(category.id);
                                            }}
                                        />
                                    );
                                })}
                            </Stack>
                        </CardContent>
                    </Card>
                </Box>
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
