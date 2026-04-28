import { useCallback, useEffect, useMemo, useState } from "react";
import {
  Alert,
  Box,
  Button,
  Card,
  CardActionArea,
  CardContent,
  CircularProgress,
  Container,
  Pagination,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { useNavigate, useSearchParams } from "react-router-dom";
import { MdArrowForward, MdListAlt, MdStars } from "react-icons/md";
import { getAllCategories } from "../../Api/category.api";
import { getAvailableQuizzes } from "../../Api/quiz.api";
import type { CategoryDto } from "../../Interface/category.dto";
import type { AvailableQuizDto } from "../../Interface/quiz.dto";
import CategoryIconBadge from "../../Lib/Utils/categoryIconBadge";
import { dt } from "../../Lib/designTokens";

const PAGE_SIZE = 9;
const ALL_GROUP_PREVIEW_SIZE = 4;
const categoryButtonSx = (selected: boolean) => ({
  justifyContent: "flex-start",
  textTransform: "none",
  borderRadius: dt.radius.default,
  fontFamily: dt.typography.fontFamily,
  ...dt.typography.button,
  borderWidth: 1.5,
  borderColor: selected ? dt.colors.primary : dt.colors.outlineVariant,
  bgcolor: selected ? dt.colors.primary : dt.colors.surfaceContainerLowest,
  color: selected ? dt.colors.onPrimary : dt.colors.onSurface,
  "&:hover": {
    borderWidth: 1.5,
    borderColor: selected ? dt.colors.primary : dt.colors.primary,
    bgcolor: selected ? dt.colors.primary : dt.colors.secondaryContainer,
    color: selected ? dt.colors.onPrimary : dt.colors.onSecondaryContainer,
  },
});

const secondaryOutlinedButtonSx = {
  fontFamily: dt.typography.fontFamily,
  ...dt.typography.button,
  textTransform: "none",
  borderRadius: dt.radius.default,
  borderWidth: 1.5,
  borderColor: dt.colors.outlineVariant,
  color: dt.colors.onSurface,
  bgcolor: dt.colors.surfaceContainerLowest,
  "&:hover": {
    borderWidth: 1.5,
    borderColor: dt.colors.primary,
    bgcolor: dt.colors.secondaryContainer,
    color: dt.colors.onSecondaryContainer,
  },
};

const emptyStateCardSx = {
  borderRadius: dt.radius.md,
  borderColor: dt.colors.outlineVariant,
  bgcolor: dt.colors.surfaceContainerLowest,
  boxShadow: dt.shadows.card,
};

type QuizGroup = {
  categoryId: string;
  categoryName: string;
  iconKey?: string;
  items: AvailableQuizDto[];
  totalCount: number;
};

type QuizPreviewCardProps = {
  quiz: AvailableQuizDto;
  categoryById: Map<string, CategoryDto>;
  onOpen: (quizId: string) => void;
};

const QuizPreviewCard = ({ quiz, categoryById, onOpen }: QuizPreviewCardProps) => {
  const quizCategory = quiz.categoryId ? categoryById.get(quiz.categoryId) : undefined;

  return (
    <Card
      key={quiz.id}
      variant="outlined"
      sx={{
        borderRadius: dt.radius.md,
        overflow: "hidden",
      }}
    >
      <CardActionArea
        onClick={() => onOpen(quiz.id)}
        sx={{
          p: dt.spacing.stackLg,
          minHeight: 280,
          border: `1px solid ${dt.colors.outlineVariant}`,
          borderRadius: dt.radius.md,
          bgcolor: dt.colors.surfaceContainerLowest,
          transition: "all 0.3s ease",
          display: "flex",
          flexDirection: "column",
          alignItems: "stretch",
          justifyContent: "space-between",
          boxShadow: dt.shadows.card,
          "&:hover": {
            borderColor: dt.colors.primary,
            boxShadow: dt.shadows.cardHover,
            transform: "translateY(-4px)",
          },
        }}
      >
        <CardContent sx={{ p: 0, width: "100%" }}>
          <Stack spacing={2}>
            <Stack direction="row" justifyContent="space-between" alignItems="flex-start">
              <Box
                sx={{
                  display: "inline-flex",
                  alignItems: "center",
                  px: 1.5,
                  py: 0.75,
                  borderRadius: dt.radius.full,
                  bgcolor: `${dt.colors.secondaryContainer}33`,
                  color: dt.colors.onSecondaryContainer,
                  ...dt.typography.labelSm,
                  fontFamily: dt.typography.fontFamily,
                  fontSize: "12px",
                  fontWeight: 700,
                  textTransform: "uppercase",
                }}
              >
                {quizCategory?.slug || "General"}
              </Box>
              <Box sx={{ color: dt.colors.outline }}>
                <CategoryIconBadge iconKey={quizCategory?.icon} size={22} fallback={null} />
              </Box>
            </Stack>

            <Typography
              sx={{
                fontFamily: dt.typography.fontFamily,
                ...dt.typography.h2,
                color: dt.colors.onSurface,
                lineHeight: 1.25,
              }}
            >
              {quiz.title}
            </Typography>

            <Typography
              sx={{
                fontFamily: dt.typography.fontFamily,
                ...dt.typography.bodyMd,
                color: dt.colors.onSurfaceVariant,
                minHeight: "3em",
                display: "-webkit-box",
                WebkitLineClamp: 2,
                WebkitBoxOrient: "vertical",
                overflow: "hidden",
              }}
            >
              {quiz.description?.trim() ||
                `Expand your knowledge in ${quizCategory?.name || "this category"} with this public quiz.`}
            </Typography>

            <Stack
              sx={{
                borderTop: `1px solid ${dt.colors.outlineVariant}`,
                pt: dt.spacing.stackMd,
                mt: dt.spacing.stackSm,
              }}
              direction="row"
              alignItems="center"
              spacing={2}
            >
              <Stack direction="row" spacing={0.5} alignItems="center">
                <MdListAlt size={18} color={dt.colors.onSurfaceVariant} />
                <Typography sx={{ ...dt.typography.labelSm, color: dt.colors.onSurfaceVariant, fontFamily: dt.typography.fontFamily }}>
                  {quiz.questionCount} Qs
                </Typography>
              </Stack>
              <Stack direction="row" spacing={0.5} alignItems="center">
                <MdStars size={18} color={dt.colors.onSurfaceVariant} />
                <Typography sx={{ ...dt.typography.labelSm, color: dt.colors.onSurfaceVariant, fontFamily: dt.typography.fontFamily }}>
                  {quiz.totalPoints} pts
                </Typography>
              </Stack>
              <Typography
                sx={{
                  ...dt.typography.labelSm,
                  fontFamily: dt.typography.fontFamily,
                  color: dt.colors.primary,
                  fontWeight: 700,
                  ml: "auto",
                }}
              >
                Start now
              </Typography>
            </Stack>

            <Button
              variant="outlined"
              fullWidth
              endIcon={<MdArrowForward />}
              sx={{
                mt: dt.spacing.stackSm,
                borderWidth: 2,
                borderColor: dt.colors.primary,
                color: dt.colors.primary,
                fontFamily: dt.typography.fontFamily,
                ...dt.typography.button,
                textTransform: "none",
                borderRadius: dt.radius.default,
                "&:hover": {
                  borderColor: dt.colors.primary,
                  bgcolor: dt.colors.primary,
                  color: dt.colors.onPrimary,
                },
              }}
            >
              Start Quiz
            </Button>
          </Stack>
        </CardContent>
      </CardActionArea>
    </Card>
  );
};

const QuizPage = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [selectedCategory, setSelectedCategory] = useState<string>(() => searchParams.get("category") || "all");
  const [searchTitle, setSearchTitle] = useState("");
  const [page, setPage] = useState(1);

  const [quizzes, setQuizzes] = useState<AvailableQuizDto[]>([]);
  const [groupedQuizzes, setGroupedQuizzes] = useState<QuizGroup[]>([]);
  const [totalPages, setTotalPages] = useState(0);
  const [isCategoryLoading, setIsCategoryLoading] = useState(true);
  const [isQuizLoading, setIsQuizLoading] = useState(false);
  const [categoryError, setCategoryError] = useState<string | null>(null);
  const [quizError, setQuizError] = useState<string | null>(null);

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

  const categoryById = useMemo(() => {
    const map = new Map<string, CategoryDto>();
    sortedCategories.forEach((category) => map.set(category.id, category));
    return map;
  }, [sortedCategories]);

  const normalizedSearchTitle = searchTitle.trim().toLowerCase();

  const filteredQuizzes = useMemo(() => {
    if (!normalizedSearchTitle) {
      return quizzes;
    }
    return quizzes.filter((quiz) => quiz.title.toLowerCase().includes(normalizedSearchTitle));
  }, [quizzes, normalizedSearchTitle]);

  const filteredGroupedQuizzes = useMemo(() => {
    if (!normalizedSearchTitle) {
      return groupedQuizzes;
    }

    return groupedQuizzes
      .map((group) => ({
        ...group,
        items: group.items.filter((quiz) => quiz.title.toLowerCase().includes(normalizedSearchTitle)),
      }))
      .filter((group) => group.items.length > 0);
  }, [groupedQuizzes, normalizedSearchTitle]);

  const fetchCategories = useCallback(async () => {
    setIsCategoryLoading(true);
    setCategoryError(null);
    try {
      const data = await getAllCategories();
      setCategories(data);
    } catch {
      setCategories([]);
      setCategoryError("Khong the tai danh sach category. Vui long thu lai.");
    } finally {
      setIsCategoryLoading(false);
    }
  }, []);

  const fetchQuizzes = useCallback(async () => {
    setIsQuizLoading(true);
    setQuizError(null);
    try {
      const data = await getAvailableQuizzes({
        category: selectedCategory,
        page,
        pageSize: PAGE_SIZE,
      });
      setGroupedQuizzes([]);
      setQuizzes(data.items);
      setTotalPages(data.totalPages);
    } catch {
      setQuizzes([]);
      setGroupedQuizzes([]);
      setTotalPages(0);
      setQuizError("Khong the tai danh sach quiz. Vui long thu lai.");
    } finally {
      setIsQuizLoading(false);
    }
  }, [selectedCategory, page]);

  const fetchGroupedQuizzes = useCallback(async () => {
    setIsQuizLoading(true);
    setQuizError(null);
    try {
      const groups = (
        await Promise.all(
          sortedCategories.map(async (category) => {
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
          }),
        )
      ).filter((group) => group.totalCount > 0);

      setGroupedQuizzes(groups);
      setQuizzes([]);
      setTotalPages(0);
    } catch {
      setQuizzes([]);
      setGroupedQuizzes([]);
      setTotalPages(0);
      setQuizError("Khong the tai danh sach quiz. Vui long thu lai.");
    } finally {
      setIsQuizLoading(false);
    }
  }, [sortedCategories]);

  useEffect(() => {
    void fetchCategories();
  }, [fetchCategories]);

  useEffect(() => {
    const categoryParam = searchParams.get("category");
    if (!categoryParam) {
      setSelectedCategory("all");
      return;
    }

    const matchedCategory = sortedCategories.find(
      (category) => category.id === categoryParam || category.slug === categoryParam,
    );

    setSelectedCategory(matchedCategory?.id || "all");
  }, [searchParams, sortedCategories]);

  useEffect(() => {
    setPage(1);
  }, [selectedCategory]);

  useEffect(() => {
    if (selectedCategory === "all") {
      return;
    }

    void fetchQuizzes();
  }, [fetchQuizzes, selectedCategory]);

  useEffect(() => {
    if (selectedCategory !== "all") {
      return;
    }

    void fetchGroupedQuizzes();
  }, [fetchGroupedQuizzes, selectedCategory]);

  return (
    <Box
      component="section"
      sx={{
        py: { xs: "48px", md: dt.spacing.sectionPadding },
        px: dt.spacing.gutter,
      }}
    >
      <Container sx={{ maxWidth: `${dt.spacing.containerMax} !important` }}>
        <Stack spacing={3}>
          <Box>
            <Typography
              sx={{
                fontFamily: dt.typography.fontFamily,
                ...dt.typography.h2,
                color: dt.colors.onSurface,
              }}
            >
              Public Quizzes
            </Typography>
            <Typography
              sx={{
                fontFamily: dt.typography.fontFamily,
                ...dt.typography.bodyMd,
                color: dt.colors.onSurfaceVariant,
                mt: 0.5,
              }}
            >
              Browse published quizzes and start practicing right away.
            </Typography>
          </Box>

          <Box
            sx={{
              display: "grid",
              gap: dt.spacing.stackMd,
              gridTemplateColumns: { xs: "1fr", md: "260px minmax(0, 1fr)" },
              alignItems: "start",
            }}
          >
            <Card
              variant="outlined"
              sx={{
                borderRadius: dt.radius.md,
                borderColor: dt.colors.outlineVariant,
                bgcolor: dt.colors.surfaceContainerLowest,
                position: { md: "sticky" },
                top: { md: 96 },
              }}
            >
              <CardContent sx={{ p: 2 }}>
                <Stack spacing={1}>
                  <Typography
                    sx={{
                      fontFamily: dt.typography.fontFamily,
                      ...dt.typography.labelSm,
                      color: dt.colors.onSurfaceVariant,
                      letterSpacing: "0.08em",
                    }}
                  >
                    CATEGORIES
                  </Typography>

                  <Button
                    onClick={() => setSelectedCategory("all")}
                    variant={selectedCategory === "all" ? "contained" : "outlined"}
                    sx={categoryButtonSx(selectedCategory === "all")}
                  >
                    All Categories
                  </Button>

                  {sortedCategories.map((category) => {
                    const selected = selectedCategory === category.id;
                    return (
                      <Button
                        key={category.id}
                        onClick={() => setSelectedCategory(category.id)}
                        variant={selected ? "contained" : "outlined"}
                        sx={{
                          gap: 1,
                          ...categoryButtonSx(selected),
                        }}
                      >
                        <CategoryIconBadge iconKey={category.icon} size={18} fallback={null} />
                        {category.name}
                      </Button>
                    );
                  })}

                  {isCategoryLoading ? (
                    <Stack direction="row" spacing={1} alignItems="center" mt={1}>
                      <CircularProgress size={18} />
                      <Typography sx={{ ...dt.typography.bodyMd, color: dt.colors.onSurfaceVariant, fontFamily: dt.typography.fontFamily }}>
                        Loading categories...
                      </Typography>
                    </Stack>
                  ) : null}
                </Stack>
              </CardContent>
            </Card>

            <Stack spacing={2}>
              <TextField
                placeholder="Search by quiz title..."
                value={searchTitle}
                onChange={(event) => setSearchTitle(event.target.value)}
                fullWidth
                sx={{
                  "& .MuiInputBase-input": {
                    color: dt.colors.onSurface,
                    fontFamily: dt.typography.fontFamily,
                  },
                  "& .MuiInputBase-input::placeholder": {
                    color: dt.colors.onSurfaceVariant,
                    opacity: 1,
                  },
                  "& .MuiOutlinedInput-root": {
                    borderRadius: dt.radius.default,
                    bgcolor: dt.colors.surfaceContainerLowest,
                    fontFamily: dt.typography.fontFamily,
                    color: dt.colors.onSurface,
                    "& fieldset": {
                      borderColor: dt.colors.outlineVariant,
                    },
                    "&:hover fieldset": {
                      borderColor: dt.colors.primary,
                    },
                    "&.Mui-focused fieldset": {
                      borderColor: dt.colors.primary,
                    },
                  },
                }}
              />

              {categoryError ? (
                <Alert
                  severity="error"
                  action={
                    <Button
                      color="inherit"
                      size="small"
                      onClick={fetchCategories}
                      sx={{ fontFamily: dt.typography.fontFamily, ...dt.typography.button, textTransform: "none" }}
                    >
                      Retry
                    </Button>
                  }
                >
                  {categoryError}
                </Alert>
              ) : null}

              {quizError ? (
                <Alert
                  severity="error"
                  action={
                    <Button
                      color="inherit"
                      size="small"
                      onClick={selectedCategory === "all" ? fetchGroupedQuizzes : fetchQuizzes}
                      sx={{ fontFamily: dt.typography.fontFamily, ...dt.typography.button, textTransform: "none" }}
                    >
                      Retry
                    </Button>
                  }
                >
                  {quizError}
                </Alert>
              ) : null}

              {isQuizLoading ? (
                <Stack py={8} alignItems="center" spacing={1}>
                  <CircularProgress />
                  <Typography sx={{ ...dt.typography.bodyMd, color: dt.colors.onSurfaceVariant, fontFamily: dt.typography.fontFamily }}>
                    Loading quizzes...
                  </Typography>
                </Stack>
              ) : null}

              {!isQuizLoading &&
              !quizError &&
              ((selectedCategory === "all" && filteredGroupedQuizzes.length === 0) ||
                (selectedCategory !== "all" && filteredQuizzes.length === 0)) ? (
                <Card variant="outlined" sx={emptyStateCardSx}>
                  <CardContent>
                    <Typography sx={{ ...dt.typography.h3, color: dt.colors.onSurface, fontFamily: dt.typography.fontFamily }}>
                      No quizzes matched your filter
                    </Typography>
                    <Typography sx={{ ...dt.typography.bodyMd, color: dt.colors.onSurfaceVariant, mt: 1, fontFamily: dt.typography.fontFamily }}>
                      Try another category or use fewer keywords.
                    </Typography>
                  </CardContent>
                </Card>
              ) : null}

              {!isQuizLoading && !quizError && selectedCategory === "all" && filteredGroupedQuizzes.length > 0 ? (
                <>
                  <Stack spacing={3}>
                    {filteredGroupedQuizzes.map((group) => (
                      <Stack key={group.categoryId} spacing={1.5}>
                        <Stack direction="row" spacing={1} alignItems="center">
                          <CategoryIconBadge iconKey={group.iconKey} size={20} fallback={null} />
                          <Typography sx={{ ...dt.typography.h3, color: dt.colors.onSurface, fontFamily: dt.typography.fontFamily }}>
                            {group.categoryName}
                          </Typography>
                        </Stack>

                        <Box
                          sx={{
                            display: "grid",
                            gridTemplateColumns: { xs: "1fr", sm: "repeat(2, minmax(0, 1fr))" },
                            gap: dt.spacing.stackMd,
                            maxWidth: 860,
                          }}
                        >
                          {group.items.map((quiz) => {
                            return (
                              <QuizPreviewCard
                                key={quiz.id}
                                quiz={quiz}
                                categoryById={categoryById}
                                onOpen={(quizId) => navigate(`/app/find-quizzes/${quizId}`)}
                              />
                            );
                          })}
                        </Box>

                        {group.totalCount > ALL_GROUP_PREVIEW_SIZE ? (
                          <Box>
                            <Button
                              variant="outlined"
                              size="small"
                              onClick={() => setSelectedCategory(group.categoryId)}
                              sx={secondaryOutlinedButtonSx}
                            >
                              View more ({group.totalCount - ALL_GROUP_PREVIEW_SIZE}+)
                            </Button>
                          </Box>
                        ) : null}
                      </Stack>
                    ))}
                  </Stack>
                </>
              ) : null}

              {!isQuizLoading && !quizError && selectedCategory !== "all" && filteredQuizzes.length > 0 ? (
                <>
                  <Box
                    sx={{
                      display: "grid",
                      gridTemplateColumns: { xs: "1fr", sm: "repeat(2, minmax(0, 1fr))" },
                      gap: dt.spacing.stackMd,
                      maxWidth: 860,
                    }}
                  >
                    {filteredQuizzes.map((quiz) => {
                      return (
                        <QuizPreviewCard
                          key={quiz.id}
                          quiz={quiz}
                          categoryById={categoryById}
                          onOpen={(quizId) => navigate(`/app/find-quizzes/${quizId}`)}
                        />
                      );
                    })}
                  </Box>

                  {totalPages > 1 && !normalizedSearchTitle ? (
                    <Stack alignItems="center" pt={1}>
                      <Pagination
                        page={page}
                        count={totalPages}
                        color="primary"
                        shape="rounded"
                        sx={{
                          "& .MuiPaginationItem-root": {
                            fontFamily: dt.typography.fontFamily,
                            color: dt.colors.onSurface,
                            borderColor: dt.colors.outlineVariant,
                          },
                          "& .MuiPaginationItem-root:hover": {
                            bgcolor: dt.colors.secondaryContainer,
                            color: dt.colors.onSecondaryContainer,
                          },
                          "& .MuiPaginationItem-root.Mui-selected": {
                            bgcolor: dt.colors.primary,
                            color: dt.colors.onPrimary,
                          },
                          "& .MuiPaginationItem-root.Mui-selected:hover": {
                            bgcolor: dt.colors.primary,
                            color: dt.colors.onPrimary,
                          },
                        }}
                        onChange={(_event, value) => setPage(value)}
                      />
                    </Stack>
                  ) : null}
                </>
              ) : null}
            </Stack>
          </Box>
        </Stack>
      </Container>
    </Box>
  );
};

export default QuizPage;
