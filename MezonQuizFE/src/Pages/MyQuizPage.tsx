import { useEffect, useMemo, useState } from "react";
import {
  Box,
  Button,
  CircularProgress,
  FormControl,
  InputLabel,
  MenuItem,
  Pagination,
  Select,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import AppSnackbar from "../Components/AppSnackbar";
import useAppSnackbar from "../Hooks/useAppSnackbar";
import { useNavigate } from "react-router-dom";
import { getAllCategories } from "../Api/category.api";
import { getAllQuizzes } from "../Api/quiz.api";
import type { CategoryDto } from "../Interface/category.dto";
import { QuizStatus, QuizVisibility, type QuizDto } from "../Interface/quiz.dto";
import useAuthStore from "../Stores/login.store";

const QUIZ_VISIBILITY_LABELS: Record<QuizVisibility, string> = {
  [QuizVisibility.Private]: "Private",
  [QuizVisibility.Public]: "Public",
  [QuizVisibility.Unlisted]: "Unlisted",
};

const QUIZ_STATUS_LABELS: Record<QuizStatus, string> = {
  [QuizStatus.Draft]: "Draft",
  [QuizStatus.Published]: "Published",
  [QuizStatus.Archived]: "Archived",
};

const MyQuizPage = () => {
  const navigate = useNavigate();
  const userId = useAuthStore((state) => state.user?.id);
  const [quizzes, setQuizzes] = useState<QuizDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingCategories, setIsLoadingCategories] = useState(false);
  const { snackbar, showError, closeSnackbar } = useAppSnackbar();
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [selectedCategory, setSelectedCategory] = useState("");
  const [searchInput, setSearchInput] = useState("");
  const [searchTitle, setSearchTitle] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [totalPages, setTotalPages] = useState(0);
  const [totalCount, setTotalCount] = useState(0);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setSearchTitle(searchInput.trim());
      setPage(1);
    }, 400);

    return () => {
      window.clearTimeout(timer);
    };
  }, [searchInput]);

  useEffect(() => {
    let isMounted = true;

    const fetchCategories = async () => {
      try {
        setIsLoadingCategories(true);
        const data = await getAllCategories();
        if (isMounted) {
          setCategories(Array.isArray(data) ? data : []);
        }
      } catch {
        if (isMounted) {
          setCategories([]);
        }
      } finally {
        if (isMounted) {
          setIsLoadingCategories(false);
        }
      }
    };

    void fetchCategories();

    return () => {
      isMounted = false;
    };
  }, []);

  useEffect(() => {
    let isMounted = true;

    const fetchQuizzes = async () => {
      try {
        setIsLoading(true);
        // Truyền userId để API lọc quiz của người dùng hiện tại (QuizListQueryParams.userId)
        const data = await getAllQuizzes({
          userId: userId || undefined,
          page,
          pageSize,
          category: selectedCategory || undefined,
          title: searchTitle || undefined,
        });

        if (isMounted) {
          const items = Array.isArray(data?.items) ? data.items : [];
          const normalized = items.filter((item): item is QuizDto => Boolean(item?.id));
          setQuizzes(normalized);
          setTotalPages(Number(data?.totalPages ?? 0));
          setTotalCount(Number(data?.totalCount ?? 0));
        }
      } catch {
        if (isMounted) {
          showError("Could not load your quizzes. Please try again.");
        }
      } finally {
        if (isMounted) {
          setIsLoading(false);
        }
      }
    };

    void fetchQuizzes();

    return () => {
      isMounted = false;
    };
  }, [page, pageSize, selectedCategory, searchTitle, userId, showError]);

  const categoryNameById = useMemo(() => {
    const map = new Map<string, string>();
    categories.forEach((category) => {
      if (category.id) {
        map.set(category.id, category.name || "Unknown");
      }
    });
    return map;
  }, [categories]);

  return (
    <Box sx={{ mt: 2 }}>
      <Stack
        direction={{ xs: "column", sm: "row" }}
        alignItems={{ xs: "stretch", sm: "center" }}
        justifyContent="space-between"
        spacing={2}
        mb={3}
      >
        <Typography variant="h4" fontWeight={700}>
          My Quizzes
        </Typography>

        <Button
          variant="contained"
          onClick={() => navigate("/app/create-quiz")}
          sx={{ alignSelf: { xs: "flex-start", sm: "auto" } }}
        >
          Create Quiz
        </Button>
      </Stack>

      <Stack direction={{ xs: "column", md: "row" }} spacing={2} mb={3}>
        <TextField
          label="Search by title"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
          fullWidth
        />

        <FormControl fullWidth sx={{ maxWidth: { xs: "100%", md: 320 } }}>
          <InputLabel id="quiz-category-filter-label">Category</InputLabel>
          <Select
            labelId="quiz-category-filter-label"
            label="Category"
            value={selectedCategory}
            onChange={(event) => {
              setSelectedCategory(String(event.target.value));
              setPage(1);
            }}
            disabled={isLoadingCategories}
          >
            <MenuItem value="">All Categories</MenuItem>
            {categories.map((category) => (
              <MenuItem key={category.id} value={category.id}>
                {category.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      </Stack>

      {isLoading ? (
        <Stack direction="row" justifyContent="center" sx={{ py: 6 }}>
          <CircularProgress />
        </Stack>
      ) : null}

      {!isLoading && quizzes.length === 0 ? (
        <Typography variant="body1" color="text.secondary">
          You do not have any quizzes yet.
        </Typography>
      ) : null}

      {!isLoading && quizzes.length > 0 ? (
        <Stack spacing={2.5}>
          {quizzes
            .filter((quiz): quiz is QuizDto & { id: string } => typeof quiz.id === "string")
            .map((quiz) => (
            <Box
              key={quiz.id}
              onClick={() => navigate(`/app/my-quizzes/${quiz.id}/settings`)}
              data-status={quiz.status ?? "Unknown"}
              sx={{
                border: "1px solid",
                borderColor: "divider",
                borderRadius: 2,
                p: { xs: 2, sm: 2.5 },
                backgroundColor: "background.paper",
                cursor: "pointer",
                boxShadow: "0 1px 2px rgba(16, 24, 40, 0.06)",
                transition: "all 0.2s ease",
                '&:hover': {
                  borderColor: "primary.main",
                  boxShadow: "0 8px 24px rgba(16, 24, 40, 0.12)",
                  transform: "translateY(-2px)",
                },
              }}
            >
              <Stack
                direction={{ xs: "column", md: "row" }}
                spacing={2}
                alignItems={{ xs: "flex-start", md: "center" }}
                justifyContent="space-between"
              >
                <Box sx={{ flex: 1, minWidth: 0 }}>
                  <Typography variant="h6" fontWeight={700} noWrap>
                    {quiz.title}
                  </Typography>
                  <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
                    {quiz.description || "No description"}
                  </Typography>
                  <Stack direction="row" spacing={2} flexWrap="wrap" sx={{ mt: 1 }}>
                    <Typography variant="caption" color="text.secondary">
                      Category: {quiz.categoryId ? (categoryNameById.get(quiz.categoryId) ?? "Unknown") : "Unknown"}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      Visibility: {QUIZ_VISIBILITY_LABELS[quiz.visibility] ?? "Unknown"}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      Points: {quiz.totalPoints ?? 0}
                    </Typography>
                    <Typography variant="caption" sx={{ color: "text.secondary", fontWeight: 600 }}>
                      Status: {QUIZ_STATUS_LABELS[quiz.status] ?? "Unknown"}
                    </Typography>
                  </Stack>
                </Box>

                <Stack direction={{ xs: "column", sm: "row" }} spacing={1} sx={{ mt: { xs: 1, md: 0 } }}>
                  <Button
                    size="small"
                    variant="contained"
                    disabled={quiz.status !== QuizStatus.Published}
                    onClick={(event) => {
                      event.stopPropagation();
                      navigate(`/app/my-quizzes/${quiz.id}/sessions`);
                    }}
                  >
                    Open Session List
                  </Button>
                  <Button
                    size="small"
                    variant="outlined"
                    onClick={(event) => {
                      event.stopPropagation();
                      navigate(`/app/my-quizzes/${quiz.id}/settings`);
                    }}
                  >
                    Open Settings
                  </Button>
                </Stack>
              </Stack>

              <Stack
                direction={{ xs: "column", sm: "row" }}
                spacing={{ xs: 0.5, sm: 2 }}
                sx={{ mt: 1.5, color: "text.secondary" }}
              >
                <Typography variant="caption">
                  Created: {quiz.createdAt || "Unknown"}
                </Typography>
                <Typography variant="caption">
                  Updated: {quiz.updatedAt || "Unknown"}
                </Typography>
                <Typography variant="caption">
                  ID: {quiz.id}
                </Typography>
              </Stack>
            </Box>
          ))}

          <Stack
            direction={{ xs: "column", sm: "row" }}
            justifyContent="space-between"
            alignItems={{ xs: "flex-start", sm: "center" }}
            spacing={1}
            pt={1}
          >
            <Typography variant="body2" color="text.secondary">
              Total quizzes: {totalCount}
            </Typography>
            <Pagination
              page={page}
              count={Math.max(totalPages, 1)}
              onChange={(_, value) => setPage(value)}
              color="primary"
              shape="rounded"
            />
          </Stack>
        </Stack>
      ) : null}

      <AppSnackbar
        open={snackbar.open}
        message={snackbar.message}
        severity={snackbar.severity}
        onClose={closeSnackbar}
      />
    </Box>
  );
};

export default MyQuizPage;
