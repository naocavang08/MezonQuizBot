import { useEffect, useState } from "react";
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
import { createQuizSession } from "../Api/session.api";
import { getQuizzes } from "../Api/quiz.api";
import type { CategoryDto } from "../Interface/category.dto";
import { QuizStatus, type ListQuizDto } from "../Interface/quiz.dto";
import useAuthStore from "../Stores/login.store";

const statusLabel: Record<ListQuizDto["status"], string> = {
  [QuizStatus.Draft]: "Draft",
  [QuizStatus.Published]: "Published",
  [QuizStatus.Archived]: "Archived",
};

const statusBorderColor: Record<ListQuizDto["status"], string> = {
  [QuizStatus.Draft]: "warning.main",
  [QuizStatus.Published]: "success.main",
  [QuizStatus.Archived]: "text.disabled",
};

const MyQuizPage = () => {
  const navigate = useNavigate();
  const userId = useAuthStore((state) => state.user?.id);
  const [quizzes, setQuizzes] = useState<ListQuizDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingCategories, setIsLoadingCategories] = useState(false);
  const [creatingSessionQuizId, setCreatingSessionQuizId] = useState<string | null>(null);
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
        const data = await getQuizzes({
          page,
          pageSize,
          category: selectedCategory || undefined,
          title: searchTitle || undefined,
        });

        if (isMounted) {
          setQuizzes(Array.isArray(data?.items) ? data.items : []);
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

    fetchQuizzes();

    return () => {
      isMounted = false;
    };
  }, [page, pageSize, selectedCategory, searchTitle]);

  const handleCreateSession = async (quizId: string, quizStatus: ListQuizDto["status"]) => {
    if (!userId) {
      showError("User is not available. Please login again.");
      return;
    }

    if (quizStatus !== QuizStatus.Published) {
      showError("Only published quiz can create a session.");
      return;
    }

    try {
      setCreatingSessionQuizId(quizId);

      const response = await createQuizSession({
        quizId,
      });

      const createdSessionId = response.session?.id;
      if (createdSessionId) {
        navigate(`/app/sessions/${createdSessionId}`);
      } else {
        showError(response.message || "Session created but missing session id.");
      }
    } catch {
      showError("Can not create session for this quiz right now.");
    } finally {
      setCreatingSessionQuizId(null);
    }
  };

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
        <Stack spacing={2}>
          {quizzes.map((quiz) => (
            <Box
              key={quiz.id}
              onClick={() => navigate(`/app/my-quizzes/${quiz.id}/settings`)}
              data-status={statusLabel[quiz.status] ?? "Unknown"}
              sx={{
                border: "1px solid",
                borderColor: statusBorderColor[quiz.status] ?? "divider",
                borderRadius: 2,
                p: 2,
                backgroundColor: "transparent",
                cursor: "pointer",
                transition: "all 0.2s ease",
                '&:hover': {
                  borderColor: statusBorderColor[quiz.status] ?? "primary.main",
                  transform: "translateY(-1px)",
                },
              }}
            >
              <Typography variant="h6" fontWeight={600}>
                {quiz.title}
              </Typography>
              <Typography variant="body2" sx={{ color: statusBorderColor[quiz.status] ?? "text.secondary", fontWeight: 600 }}>
                Status: {statusLabel[quiz.status] ?? "Unknown"}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Quiz ID: {quiz.id}
              </Typography>
              <Stack direction={{ xs: "column", sm: "row" }} spacing={1.2} sx={{ mt: 1.5 }}>
                <Button
                  size="small"
                  variant="contained"
                  disabled={quiz.status !== QuizStatus.Published || creatingSessionQuizId === quiz.id}
                  onClick={(event) => {
                    event.stopPropagation();
                    void handleCreateSession(quiz.id, quiz.status);
                  }}
                >
                  {creatingSessionQuizId === quiz.id ? "Creating..." : "Create Session"}
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