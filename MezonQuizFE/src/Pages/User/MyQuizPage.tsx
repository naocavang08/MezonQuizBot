import { useEffect, useState } from "react";
import { Alert, Box, Button, CircularProgress, Stack, Typography } from "@mui/material";
import { useNavigate } from "react-router-dom";
import { getQuizzes } from "../../Api/quiz.api";
import type { ListQuizDto } from "../../Interface/Quiz.dto";

const MyQuizPage = () => {
  const navigate = useNavigate();
  const [quizzes, setQuizzes] = useState<ListQuizDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    const fetchQuizzes = async () => {
      try {
        setIsLoading(true);
        setError(null);
        const data = await getQuizzes();

        if (isMounted) {
          setQuizzes(Array.isArray(data) ? data : []);
        }
      } catch {
        if (isMounted) {
          setError("Could not load your quizzes. Please try again.");
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
  }, []);

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
          onClick={() => navigate("/user/create-quiz")}
          sx={{ alignSelf: { xs: "flex-start", sm: "auto" } }}
        >
          Create Quiz
        </Button>
      </Stack>

      {isLoading ? (
        <Stack direction="row" justifyContent="center" sx={{ py: 6 }}>
          <CircularProgress />
        </Stack>
      ) : null}

      {!isLoading && error ? <Alert severity="error">{error}</Alert> : null}

      {!isLoading && !error && quizzes.length === 0 ? (
        <Typography variant="body1" color="text.secondary">
          You do not have any quizzes yet.
        </Typography>
      ) : null}

      {!isLoading && !error && quizzes.length > 0 ? (
        <Stack spacing={2}>
          {quizzes.map((quiz) => (
            <Box
              key={quiz.id}
              onClick={() => navigate(`/user/my-quizzes/${quiz.id}/settings`)}
              sx={{
                border: "1px solid",
                borderColor: "divider",
                borderRadius: 2,
                p: 2,
                backgroundColor: "transparent",
                cursor: "pointer",
                transition: "all 0.2s ease",
                '&:hover': {
                  borderColor: "primary.main",
                  transform: "translateY(-1px)",
                },
              }}
            >
              <Typography variant="h6" fontWeight={600}>
                {quiz.title}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Quiz ID: {quiz.id}
              </Typography>
            </Box>
          ))}
        </Stack>
      ) : null}
    </Box>
  );
};

export default MyQuizPage;