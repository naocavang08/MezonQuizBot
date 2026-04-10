import { useCallback, useEffect, useMemo, useState } from "react";
import {
  Box,
  Button,
  Card,
  CardContent,
  CircularProgress,
  Container,
  Divider,
  Link,
  Stack,
  Typography,
} from "@mui/material";
import { alpha, useTheme } from "@mui/material/styles";
import { useNavigate, useParams } from "react-router-dom";
import AppSnackbar from "../Components/AppSnackbar";
import useAppSnackbar from "../Hooks/useAppSnackbar";
import { getAllCategories } from "../Api/category.api";
import { getQuiz } from "../Api/quiz.api";
import { getQuizSessions } from "../Api/session.api";
import type { CategoryDto } from "../Interface/category.dto";
import type { Quiz } from "../Interface/quiz.dto";
import { SessionStatusValue, type QuizSessionDto } from "../Interface/session.dto";
import CategoryIconBadge from "../Lib/Utils/categoryIconBadge";

const sessionStatusLabel: Record<number, string> = {
  [SessionStatusValue.Waiting]: "Waiting",
  [SessionStatusValue.Active]: "Active",
  [SessionStatusValue.Paused]: "Paused",
  [SessionStatusValue.Finished]: "Finished",
  [SessionStatusValue.Cancelled]: "Cancelled",
};

const QuizDetailPage = () => {
  const theme = useTheme();
  const navigate = useNavigate();
  const { quizId } = useParams<{ quizId: string }>();
  const { snackbar, showError, showSuccess, closeSnackbar } = useAppSnackbar();

  const [quiz, setQuiz] = useState<Quiz | null>(null);
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [sessions, setSessions] = useState<QuizSessionDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingSessions, setIsLoadingSessions] = useState(false);

  const strongText = theme.palette.text.primary;
  const softText = theme.palette.text.secondary;

  const categoryById = useMemo(() => {
    const map = new Map<string, CategoryDto>();
    categories.forEach((category) => {
      map.set(category.id, category);
    });
    return map;
  }, [categories]);

  const quizCategory = quiz?.categoryId ? categoryById.get(quiz.categoryId) : undefined;

  const loadData = useCallback(async () => {
    if (!quizId) {
      showError("Quiz id is missing.");
      setIsLoading(false);
      return;
    }

    try {
      setIsLoading(true);
      setIsLoadingSessions(true);
      const [quizData, categoryData, sessionData] = await Promise.all([
        getQuiz(quizId),
        getAllCategories(),
        getQuizSessions({ quizId, page: 1, pageSize: 50 }),
      ]);

      setQuiz(quizData);
      setCategories(Array.isArray(categoryData) ? categoryData : []);
      setSessions(Array.isArray(sessionData.items) ? sessionData.items : []);
    } catch {
      showError("Can not load quiz detail right now.");
      setQuiz(null);
      setCategories([]);
      setSessions([]);
    } finally {
      setIsLoadingSessions(false);
      setIsLoading(false);
    }
  }, [quizId, showError]);

  const copyValue = async (value: string, message: string) => {
    if (!value) {
      return;
    }

    try {
      await navigator.clipboard.writeText(value);
      showSuccess(message);
    } catch {
      showError("Can not copy value right now.");
    }
  };

  useEffect(() => {
    void loadData();
  }, [loadData]);

  if (isLoading) {
    return (
      <Stack direction="row" justifyContent="center" sx={{ py: 8 }}>
        <CircularProgress />
      </Stack>
    );
  }

  if (!quiz) {
    return (
      <Container maxWidth="md" sx={{ py: 4 }}>
        <Stack spacing={2}>
          <Typography variant="h6">Quiz not found.</Typography>
          <Button variant="outlined" onClick={() => navigate("/app/find-quizzes")}>
            Back to Find Quizzes
          </Button>
        </Stack>
        <AppSnackbar
          open={snackbar.open}
          message={snackbar.message}
          severity={snackbar.severity}
          onClose={closeSnackbar}
        />
      </Container>
    );
  }

  return (
    <Box sx={{ py: { xs: 3, md: 5 } }}>
      <Container maxWidth="lg">
        <Stack spacing={3}>
          <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" spacing={1.5}>
            <Typography variant="h4" fontWeight={700} sx={{ color: strongText }}>
              {quiz.title}
            </Typography>
            <Button variant="outlined" onClick={() => navigate("/app/find-quizzes")}>
              Back
            </Button>
          </Stack>

          <Card
            variant="outlined"
            sx={{
              borderRadius: 1,
              border: `1px solid ${alpha(theme.palette.text.secondary, 0.24)}`,
              backgroundColor: "transparent",
            }}
          >
            <CardContent>
              <Stack spacing={2}>
                <Typography sx={{ color: softText }}>
                  {quiz.description?.trim() || "No description."}
                </Typography>

                <Stack direction="row" spacing={1} alignItems="center">
                  <CategoryIconBadge iconKey={quizCategory?.icon} size={22} fallback={null} />
                  <Typography variant="body2" sx={{ color: softText }}>
                    Category: {quizCategory?.name || "Uncategorized"}
                  </Typography>
                </Stack>
              </Stack>
            </CardContent>
          </Card>

            <CardContent>
              <Stack spacing={1.5}>
                <Stack
                  direction={{ xs: "column", sm: "row" }}
                  justifyContent="space-between"
                  alignItems={{ xs: "flex-start", sm: "center" }}
                >
                  <Typography variant="h6" fontWeight={700}>
                    Sessions
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Total: {sessions.length}
                  </Typography>
                </Stack>

                <Divider />

                {isLoadingSessions ? (
                  <Stack direction="row" justifyContent="center" sx={{ py: 2 }}>
                    <CircularProgress size={24} />
                  </Stack>
                ) : null}

                {!isLoadingSessions && sessions.length === 0 ? (
                  <Typography variant="body2" color="text.secondary">
                    No session available for this quiz.
                  </Typography>
                ) : null}

                {!isLoadingSessions && sessions.length > 0 ? (
                  <Stack spacing={1.2}>
                    {sessions.map((session) => {
                      const isFinishedSession = session.status === SessionStatusValue.Finished;
                      const deepLink = session.deepLink ?? "";
                      const qrCodeUrl = session.qrCodeUrl ?? "";

                      return (
                        <Box
                          key={session.id}
                          sx={{
                            border: "1px solid",
                            borderColor: "divider",
                            borderRadius: 2,
                            p: 1.5,
                          }}
                        >
                          <Stack spacing={0.8}>
                            <Typography variant="subtitle2" fontWeight={700}>
                              Session {session.id}
                            </Typography>
                            <Typography variant="body2" color="text.secondary">
                              Status: {sessionStatusLabel[session.status] ?? "Unknown"} | Participants: {session.participantCount}
                            </Typography>
                            {!isFinishedSession ? (
                              <Typography variant="body2" color="text.secondary">
                                Code: {session.code || "N/A"}
                              </Typography>
                            ) : null}
                            <Typography variant="body2" color="text.secondary">
                              Created: {new Date(session.createdAt).toLocaleString()}
                            </Typography>

                            {!isFinishedSession && qrCodeUrl ? (
                              <Box
                                component="img"
                                src={qrCodeUrl}
                                alt={`Session ${session.id} QR code`}
                                sx={{
                                  width: 96,
                                  height: 96,
                                  borderRadius: 1,
                                  border: "1px solid",
                                  borderColor: "divider",
                                  backgroundColor: "background.paper",
                                }}
                              />
                            ) : null}

                            {!isFinishedSession ? (
                              <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
                                <Button
                                  size="small"
                                  variant="outlined"
                                  disabled={!session.code}
                                  onClick={() => {
                                    void copyValue(session.code, "Session code copied.");
                                  }}
                                >
                                  Copy Session Code
                                </Button>
                                <Button
                                  size="small"
                                  variant="outlined"
                                  onClick={() => {
                                    void copyValue(session.id, "Session ID copied.");
                                  }}
                                >
                                  Copy Session ID
                                </Button>
                                <Button
                                  size="small"
                                  variant="outlined"
                                  disabled={!deepLink}
                                  onClick={() => {
                                    void copyValue(deepLink, "Session deep link copied.");
                                  }}
                                >
                                  Copy Deep Link
                                </Button>
                                {qrCodeUrl ? (
                                  <Link
                                    href={qrCodeUrl}
                                    target="_blank"
                                    rel="noopener noreferrer"
                                    underline="hover"
                                    sx={{ alignSelf: "center" }}
                                  >
                                    Open QR
                                  </Link>
                                ) : null}
                              </Stack>
                            ) : null}
                          </Stack>
                        </Box>
                      );
                    })}
                  </Stack>
                ) : null}
              </Stack>
            </CardContent>
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

export default QuizDetailPage;
