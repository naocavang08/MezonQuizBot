import { useCallback, useEffect, useMemo, useState } from "react";
import {
    Alert,
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
import { MdRefresh, MdSearch } from "react-icons/md";
import { getAllCategories } from "../../Api/category.api";
import { getPublicQuizzes } from "../../Api/publicquiz.api";
import type { CategoryDto } from "../../Interface/category.dto";
import type { ListPublicQuizDto } from "../../Interface/quiz.dto";

const PAGE_SIZE = 9;

const FindQuizPage = () => {
    const [categories, setCategories] = useState<CategoryDto[]>([]);
    const [selectedCategory, setSelectedCategory] = useState<string>("all");
    const [searchTitle, setSearchTitle] = useState("");
    const [page, setPage] = useState(1);

    const [items, setItems] = useState<ListPublicQuizDto[]>([]);
    const [totalPages, setTotalPages] = useState(0);
    const [totalCount, setTotalCount] = useState(0);

    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

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
        setError(null);

        try {
            const data = await getPublicQuizzes({
                category: categoryParam,
                title: titleParam,
                page,
                pageSize: PAGE_SIZE,
            });

            setItems(data.items);
            setTotalPages(data.totalPages);
            setTotalCount(data.totalCount);
        } catch {
            setError("Can not load quiz list right now. Please try again.");
            setItems([]);
            setTotalPages(0);
            setTotalCount(0);
        } finally {
            setIsLoading(false);
        }
    }, [categoryParam, page, titleParam]);

    useEffect(() => {
        void fetchQuizzes();
        // Intentionally include all query states to refetch as user changes filters.
    }, [fetchQuizzes]);

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
                            background: "rgba(255, 255, 255, 0.72)",
                            backdropFilter: "blur(6px)",
                            border: "1px solid rgba(19, 36, 67, 0.08)",
                            boxShadow: "0 20px 60px rgba(7, 24, 55, 0.09)",
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
                                            color: "#12336b",
                                        }}
                                    >
                                        Discover Quiz
                                    </Typography>
                                    <Typography color="text.secondary" sx={{ mt: 1 }}>
                                        Search by title, filter by category, and start practicing quickly.
                                    </Typography>
                                </Box>

                                <Tooltip title="Reload list">
                                    <IconButton
                                        onClick={() => {
                                            void fetchQuizzes();
                                        }}
                                        sx={{
                                            bgcolor: "#ffffff",
                                            border: "1px solid rgba(18, 51, 107, 0.2)",
                                            "&:hover": {
                                                bgcolor: "#eef4ff",
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
                                            <MdSearch />
                                        </InputAdornment>
                                    ),
                                }}
                                sx={{
                                    "& .MuiOutlinedInput-root": {
                                        borderRadius: 3,
                                        backgroundColor: "#fff",
                                    },
                                }}
                            />

                            <Box>
                                <Typography
                                    variant="subtitle2"
                                    sx={{ mb: 1.2, color: "text.secondary", fontWeight: 700 }}
                                >
                                    Category
                                </Typography>
                                <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap">
                                    <Chip
                                        label="All"
                                        clickable
                                        color={selectedCategory === "all" ? "primary" : "default"}
                                        variant={selectedCategory === "all" ? "filled" : "outlined"}
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
                                                onClick={() => {
                                                    setSelectedCategory(category.id);
                                                }}
                                            />
                                        );
                                    })}
                                </Stack>
                            </Box>

                            <Typography color="text.secondary" sx={{ fontSize: "0.92rem" }}>
                                {totalCount} quizzes found
                                {activeFilterCount > 0 ? ` • ${activeFilterCount} active filter(s)` : ""}
                            </Typography>
                        </Stack>
                    </Box>

                    {error && <Alert severity="error">{error}</Alert>}

                    {isLoading ? (
                        <Stack alignItems="center" justifyContent="center" py={8} spacing={1.5}>
                            <CircularProgress />
                            <Typography color="text.secondary">Loading public quizzes...</Typography>
                        </Stack>
                    ) : items.length === 0 ? (
                        <Card
                            sx={{
                                borderRadius: 3,
                                border: "1px dashed rgba(18, 51, 107, 0.28)",
                                backgroundColor: "rgba(255, 255, 255, 0.66)",
                            }}
                        >
                            <CardContent>
                                <Typography variant="h6" fontWeight={700}>
                                    No quizzes matched your filter
                                </Typography>
                                <Typography color="text.secondary" sx={{ mt: 1 }}>
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
                                                border: "1px solid rgba(18, 51, 107, 0.12)",
                                                background:
                                                    "linear-gradient(160deg, rgba(255,255,255,0.94) 0%, rgba(246,251,255,0.92) 100%)",
                                                transition: "transform 0.25s ease, box-shadow 0.25s ease",
                                                animation: `fade-up 300ms ease ${Math.min(index * 45, 280)}ms both`,
                                                "@keyframes fade-up": {
                                                    from: { opacity: 0, transform: "translateY(10px)" },
                                                    to: { opacity: 1, transform: "translateY(0)" },
                                                },
                                                "&:hover": {
                                                    transform: "translateY(-3px)",
                                                    boxShadow: "0 14px 28px rgba(8, 25, 56, 0.14)",
                                                },
                                            }}
                                        >
                                            <CardContent>
                                                <Stack spacing={1.3}>
                                                    <Typography variant="h6" sx={{ fontWeight: 700 }}>
                                                        {quiz.title}
                                                    </Typography>
                                                    <Typography
                                                        sx={{
                                                            fontSize: "0.8rem",
                                                            color: "text.secondary",
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
        </Box>
    );
};

export default FindQuizPage;