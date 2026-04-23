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
import useSessionRealtime from "../Hooks/useSessionRealtime";
import { getAllCategories } from "../Api/category.api";
import { getAvailableQuiz, getQuiz } from "../Api/quiz.api";
import { getQuizSessions, joinQuizSession } from "../Api/session.api";
import type { CategoryDto } from "../Interface/category.dto";
import type { AvailableQuizDto } from "../Interface/quiz.dto";
import { SessionStatusValue, type QuizSessionDto } from "../Interface/session.dto";
import CategoryIconBadge from "../Lib/Utils/categoryIconBadge";
import useAuthStore from "../Stores/login.store";

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
  const userId = useAuthStore((state) => state.user?.id);

  const [quiz, setQuiz] = useState<AvailableQuizDto | null>(null);
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [sessions, setSessions] = useState<QuizSessionDto[]>([]);
  const [questionCount, setQuestionCount] = useState<number | null>(null);
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

  const loadData = useCallback(async (silent = false) => {
    if (!quizId) {
      showError("Quiz id is missing.");
      setIsLoading(false);
      return;
    }

    try {
      if (!silent) {
        setIsLoading(true);
      }
      setIsLoadingSessions(true);
      const [quizData, categoryData, sessionData] = await Promise.all([
        getAvailableQuiz(quizId),
        getAllCategories(),
        getQuizSessions({ quizId, page: 1, pageSize: 50 }),
      ]);

      setQuiz(quizData);
      setCategories(Array.isArray(categoryData) ? categoryData : []);
      setSessions(Array.isArray(sessionData.items) ? sessionData.items : []);
      try {
        const quizDetail = await getQuiz(quizId);
        setQuestionCount(Array.isArray(quizDetail.questions) ? quizDetail.questions.length : 0);
      } catch {
        setQuestionCount(null);
      }
    } catch {
      showError("Can not load quiz detail right now.");
      setQuiz(null);
      setCategories([]);
      setSessions([]);
      setQuestionCount(null);
    } finally {
      setIsLoadingSessions(false);
      if (!silent) {
        setIsLoading(false);
      }
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

  const handleJoinAndPlay = useCallback(async (session: QuizSessionDto) => {
    if (!quizId || !userId) {
      showError("User info is invalid.");
      return;
    }

    if (!session.code) {
      showError("Session code is invalid.");
      return;
    }

    if (session.status === SessionStatusValue.Waiting) {
      try {
        const response = await joinQuizSession(session.code, { userId });
        showSuccess(response.message || "Joined session successfully.");
      } catch {
        showError("Can not join this session right now.");
        return;
      }
    }

    navigate(`/app/find-quizzes/${quizId}/sessions/${session.id}/play`);
  }, [navigate, quizId, showError, showSuccess, userId]);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  useSessionRealtime({
    quizId: quizId,
    onSessionStateChanged: () => loadData(true),
    enabled: Boolean(quizId),
    joinGroup: true,
  });

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
            <CardContent sx={{ p: 2.5 }}>
              <Stack spacing={1.5}>
                <Typography variant="body1" sx={{ color: softText, lineHeight: 1.6 }}>
                  {quiz.description?.trim() || "No description."}
                </Typography>

                <Divider sx={{ opacity: 0.5 }} />

                <Stack direction="row" spacing={3}>
                  <Stack spacing={0.25}>
                    <Typography variant="body2" sx={{ color: softText, opacity: 0.7 }}>
                      Total Points
                    </Typography>
                    <Typography variant="body1" sx={{ color: softText, fontWeight: 500 }}>
                      {quiz.totalPoints}
                    </Typography>
                  </Stack>

                  <Stack spacing={0.25}>
                    <Typography variant="body2" sx={{ color: softText, opacity: 0.7 }}>
                      Questions
                    </Typography>
                    <Typography variant="body1" sx={{ color: softText, fontWeight: 500 }}>
                      {questionCount ?? "—"}
                    </Typography>
                  </Stack>
                </Stack>

                <Stack direction="row" spacing={1} alignItems="center">
                  <CategoryIconBadge iconKey={quizCategory?.icon} size={20} fallback={null} />
                  <Typography variant="body2" sx={{ color: softText, opacity: 0.7 }}>
                    {quizCategory?.name || "Uncategorized"}
                  </Typography>
                </Stack>
              </Stack>
            </CardContent>
          </Card>

          <Card variant="outlined">
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
                            <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
                              {!isFinishedSession ? (
                                <Button
                                  size="small"
                                  variant="contained"
                                  onClick={() => {
                                    void handleJoinAndPlay(session);
                                  }}
                                >
                                  Join & Play
                                </Button>
                              ) : null}
                              <Button
                                size="small"
                                variant={isFinishedSession ? "contained" : "outlined"}
                                onClick={() => {
                                  navigate(`/app/find-quizzes/${quizId}/sessions/${session.id}/leaderboard`);
                                }}
                              >
                                Show Result
                              </Button>
                            </Stack>
                          </Stack>
                        </Box>
                      );
                    })}
                  </Stack>
                ) : null}
              </Stack>
            </CardContent>
          </Card>
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
