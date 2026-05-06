import { useCallback, useEffect, useMemo, useState } from "react";
import {
  Box,
  Button,
  Card,
  CardContent,
  CircularProgress,
  Container,
  Divider,
  Stack,
  Typography,
} from "@mui/material";
import { useNavigate, useParams } from "react-router-dom";
import { MdArrowBack, MdContentCopy, MdStars, MdLeaderboard, MdQuiz, MdSchedule } from "react-icons/md";
import AppSnackbar from "../../Components/AppSnackbar";
import useAppSnackbar from "../../Hooks/useAppSnackbar";
import useSessionRealtime from "../../Hooks/useSessionRealtime";
import { getAllCategories } from "../../Api/category.api";
import { getAvailableQuiz, getQuiz } from "../../Api/quiz.api";
import { getBotLink, getQuizSessions } from "../../Api/session.api";
import type { CategoryDto } from "../../Interface/category.dto";
import type { AvailableQuizDto } from "../../Interface/quiz.dto";
import { SessionStatusValue, type BotLinkDto, type QuizSessionDto } from "../../Interface/session.dto";
import CategoryIconBadge from "../../Lib/Utils/categoryIconBadge";
import { dt } from "../../Lib/designTokens";
import catQrImage from "../../assets/hinh-nen-meo-9.jpg";

const sessionStatusLabel: Record<number, string> = {
  [SessionStatusValue.Waiting]: "Waiting",
  [SessionStatusValue.Active]: "Active",
  [SessionStatusValue.Paused]: "Paused",
  [SessionStatusValue.Finished]: "Finished",
  [SessionStatusValue.Cancelled]: "Cancelled",
};

const QuizDetailPage = () => {
  const navigate = useNavigate();
  const { quizId } = useParams<{ quizId: string }>();
  const { snackbar, showError, showSuccess, closeSnackbar } = useAppSnackbar();

  const [quiz, setQuiz] = useState<AvailableQuizDto | null>(null);
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [sessions, setSessions] = useState<QuizSessionDto[]>([]);
  const [questionCount, setQuestionCount] = useState<number | null>(null);
  const [botLink, setBotLink] = useState<BotLinkDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingSessions, setIsLoadingSessions] = useState(false);

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

      const [quizData, categoryData, sessionData, botLinkData] = await Promise.all([
        getAvailableQuiz(quizId),
        getAllCategories(),
        getQuizSessions({ quizId, page: 1, pageSize: 50 }),
        getBotLink().catch(() => null),
      ]);

      setQuiz(quizData);
      setCategories(Array.isArray(categoryData) ? categoryData : []);
      setSessions(Array.isArray(sessionData.items) ? sessionData.items : []);
      setBotLink(botLinkData ?? null);

      try {
        const quizDetail = await getQuiz(quizId);
        setQuestionCount(Array.isArray(quizDetail.questions) ? quizDetail.questions.length : 0);
      } catch {
        setQuestionCount(quizData.questionCount ?? null);
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

  const openBotLink = () => {
    if (!botLink?.deepLink) {
      return;
    }

    window.open(botLink.deepLink, "_blank", "noopener,noreferrer");
  };

  useEffect(() => {
    void loadData();
  }, [loadData]);

  useSessionRealtime({
    quizId,
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
          <Button variant="outlined" onClick={() => navigate("/quizzes")}>
            Back to Quizzes
          </Button>
        </Stack>
        <AppSnackbar open={snackbar.open} message={snackbar.message} severity={snackbar.severity} onClose={closeSnackbar} />
      </Container>
    );
  }

  return (
    <Box sx={{ py: { xs: 3, md: 5 }, bgcolor: dt.colors.background }}>
      <Container maxWidth="lg">
        <Stack spacing={3}>
          <Stack direction={{ xs: "column", md: "row" }} spacing={3} alignItems={{ xs: "stretch", md: "flex-start" }}>
            <Stack spacing={2.5} sx={{ flex: 1 }}>
              <Stack spacing={1.5}>
                <Stack direction="row" alignItems="center" spacing={1}>
                  <Button
                    variant="text"
                    startIcon={<MdArrowBack />}
                    onClick={() => navigate("/quizzes")}
                    sx={{ textTransform: "none", color: dt.colors.onSurfaceVariant }}
                  >
                    Back to Quizzes
                  </Button>
                </Stack>

                <Stack direction="row" spacing={1} alignItems="center">
                  <CategoryIconBadge iconKey={quizCategory?.icon} size={20} fallback={null} />
                  <Typography sx={{ fontFamily: dt.typography.fontFamily, ...dt.typography.labelSm, color: dt.colors.onSurfaceVariant }}>
                    {quizCategory?.name || "Uncategorized"}
                  </Typography>
                </Stack>

                <Typography sx={{ ...dt.typography.h1, fontFamily: dt.typography.fontFamily, color: dt.colors.onSurface }}>
                  {quiz.title}
                </Typography>
                <Typography sx={{ ...dt.typography.bodyLg, fontFamily: dt.typography.fontFamily, color: dt.colors.onSurfaceVariant }}>
                  {quiz.description?.trim() || "No description."}
                </Typography>
              </Stack>

            <Stack spacing={1.5}>
                <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" alignItems={{ xs: "flex-start", sm: "center" }}>
                    <Typography sx={{ ...dt.typography.h2, fontFamily: dt.typography.fontFamily, color: dt.colors.onSurface }}>
                    Quiz Session List
                    </Typography>
                    <Typography sx={{ ...dt.typography.labelSm, fontFamily: dt.typography.fontFamily, color: dt.colors.outline }}>
                    {sessions.length} Sessions
                    </Typography>
                </Stack>
                <Divider />

                {isLoadingSessions ? (
                    <Stack direction="row" justifyContent="center" sx={{ py: 2 }}>
                    <CircularProgress size={24} />
                    </Stack>
                ) : null}

                {!isLoadingSessions && sessions.length === 0 ? (
                    <Typography sx={{ ...dt.typography.bodyMd, fontFamily: dt.typography.fontFamily, color: dt.colors.onSurfaceVariant }}>
                    No session available for this quiz.
                    </Typography>
                ) : null}

                {!isLoadingSessions && sessions.length > 0 ? (
                    <Stack spacing={1}>
                    {sessions.map((session, index) => {
                        return (
                        <Box
                            key={session.id}
                            sx={{
                            border: `1px solid ${dt.colors.outlineVariant}`,
                            borderRadius: dt.radius.default,
                            p: 1.5,
                            bgcolor: dt.colors.surfaceContainerLowest,
                            }}
                        >
                            <Stack spacing={1}>
                            <Stack direction="row" justifyContent="space-between" alignItems="center">
                                <Typography sx={{ ...dt.typography.bodyMd, fontFamily: dt.typography.fontFamily, fontWeight: 700 }}>
                                Session {String(index + 1).padStart(2, "0")}
                                </Typography>
                                <Typography sx={{ ...dt.typography.labelSm, fontFamily: dt.typography.fontFamily, color: dt.colors.outline }}>
                                {sessionStatusLabel[session.status] ?? "Unknown"}
                                </Typography>
                            </Stack>

                            <Typography sx={{ ...dt.typography.bodyMd, fontFamily: dt.typography.fontFamily, color: dt.colors.onSurfaceVariant }}>
                                Participants: {session.participantCount} | Code: {session.code || "N/A"}
                            </Typography>
                            <Typography sx={{ ...dt.typography.labelSm, fontFamily: dt.typography.fontFamily, color: dt.colors.outline }}>
                                Created: {new Date(session.createdAt).toLocaleString()}
                            </Typography>

                            <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
                                <Button
                                size="small"
                                variant="outlined"
                                startIcon={<MdContentCopy />}
                                disabled={!session.code}
                                onClick={() => void copyValue(session.code, "Session code copied.")}
                                >
                                Copy Code
                                </Button>
                                {session.status === SessionStatusValue.Finished ? (
                                <Button
                                    size="small"
                                    variant="contained"
                                    startIcon={<MdLeaderboard />}
                                    onClick={() => navigate(`/quizzes/${quizId}/sessions/${session.id}/leaderboard`)}
                                >
                                    Leaderboard
                                </Button>
                                ) : null}
                            </Stack>
                            </Stack>
                        </Box>
                        );
                    })}
                    </Stack>
                ) : null}
                </Stack>
            </Stack>

            <Box sx={{ width: { xs: "100%", md: 340 }, position: { md: "sticky" }, top: { md: 150 } }}>
              <Card
                sx={{
                  overflow: "hidden",
                  borderRadius: dt.radius.lg,
                  border: `1px solid ${dt.colors.outlineVariant}`,
                  boxShadow: dt.shadows.card,
                  bgcolor: dt.colors.surfaceContainerLowest,
                }}
              >
                <Box
                  sx={{
                    px: 2.5,
                    py: 2,
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "space-between",
                    background: `linear-gradient(135deg, ${dt.colors.primaryContainer} 0%, ${dt.colors.primary} 100%)`,
                    color: dt.colors.onPrimary,
                  }}
                >
                  <Typography
                    sx={{
                      fontFamily: dt.typography.fontFamily,
                      ...dt.typography.labelSm,
                      textTransform: "uppercase",
                      letterSpacing: "0.08em",
                      color: dt.colors.onPrimary,
                      fontWeight: 700,
                    }}
                  >
                    Quiz Access
                  </Typography>
                  <Typography sx={{ fontFamily: dt.typography.fontFamily, ...dt.typography.h2, color: dt.colors.onPrimary }}>
                    Free
                  </Typography>
                </Box>

                <CardContent sx={{ p: 2.5, bgcolor: dt.colors.surfaceContainerLowest }}>
                  <Stack spacing={2.5}>
                    <Stack spacing={1.25}>
                      <Stack
                        direction="row"
                        justifyContent="space-between"
                        alignItems="center"
                        sx={{ pb: 1, borderBottom: `1px solid ${dt.colors.outlineVariant}` }}
                      >
                        <Stack direction="row" spacing={1} alignItems="center">
                          <MdQuiz size={18} color={dt.colors.onSurfaceVariant} />
                          <Typography sx={{ fontFamily: dt.typography.fontFamily, ...dt.typography.bodyMd, color: dt.colors.onSurfaceVariant }}>
                            Questions
                          </Typography>
                        </Stack>
                        <Typography sx={{ fontFamily: dt.typography.fontFamily, ...dt.typography.bodyMd, color: dt.colors.onSurface, fontWeight: 700 }}>
                          {questionCount ?? quiz.questionCount ?? 0}
                        </Typography>
                      </Stack>

                      <Stack
                        direction="row"
                        justifyContent="space-between"
                        alignItems="center"
                        sx={{ pb: 1, borderBottom: `1px solid ${dt.colors.outlineVariant}` }}
                      >
                        <Stack direction="row" spacing={1} alignItems="center">
                          <MdStars size={18} color={dt.colors.onSurfaceVariant} />
                          <Typography sx={{ fontFamily: dt.typography.fontFamily, ...dt.typography.bodyMd, color: dt.colors.onSurfaceVariant }}>
                            Points
                          </Typography>
                        </Stack>
                        <Typography sx={{ fontFamily: dt.typography.fontFamily, ...dt.typography.bodyMd, color: dt.colors.onSurface, fontWeight: 700 }}>
                          {quiz.totalPoints}
                        </Typography>
                      </Stack>

                      <Stack
                        direction="row"
                        justifyContent="space-between"
                        alignItems="center"
                        sx={{ pb: 1, borderBottom: `1px solid ${dt.colors.outlineVariant}` }}
                      >
                        <Stack direction="row" spacing={1} alignItems="center">
                          <MdSchedule size={18} color={dt.colors.onSurfaceVariant} />
                          <Typography sx={{ fontFamily: dt.typography.fontFamily, ...dt.typography.bodyMd, color: dt.colors.onSurfaceVariant }}>
                            Sessions
                          </Typography>
                        </Stack>
                        <Typography sx={{ fontFamily: dt.typography.fontFamily, ...dt.typography.bodyMd, color: dt.colors.onSurface, fontWeight: 700 }}>
                          {sessions.length}
                        </Typography>
                      </Stack>

                      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ pb: 1 }}>
                        <Stack direction="row" spacing={1} alignItems="center">
                          <MdLeaderboard size={18} color={dt.colors.onSurfaceVariant} />
                          <Typography sx={{ fontFamily: dt.typography.fontFamily, ...dt.typography.bodyMd, color: dt.colors.onSurfaceVariant }}>
                            Leaderboard
                          </Typography>
                        </Stack>
                        <Typography sx={{ fontFamily: dt.typography.fontFamily, ...dt.typography.bodyMd, color: dt.colors.primary, fontWeight: 700 }}>
                          {sessions.some((session) => session.status === SessionStatusValue.Finished) ? "Available" : "Pending"}
                        </Typography>
                      </Stack>
                    </Stack>

                    <Stack spacing={2.25} alignItems="center">
                      <Box
                        sx={{
                          p: 3,
                          borderRadius: "28px",
                          bgcolor: dt.colors.surfaceContainerLowest,
                          border: "1px solid rgba(255,255,255,0.82)",
                          boxShadow: "0 18px 50px rgba(15, 23, 42, 0.1)",
                          transition: "transform 0.3s ease",
                          "&:hover": {
                            transform: { md: "scale(1.02)" },
                          },
                        }}
                      >
                        {isLoading ? (
                          <Box
                            sx={{
                              width: { xs: 160, md: 192 },
                              height: { xs: 160, md: 192 },
                              display: "flex",
                              alignItems: "center",
                              justifyContent: "center",
                            }}
                          >
                            <CircularProgress size={32} />
                          </Box>
                        ) : botLink?.qrCodeUrl ? (
                          <Box
                            component="img"
                            src={botLink.qrCodeUrl}
                            alt="Mezon Quiz Bot QR code"
                            sx={{
                              width: { xs: 160, md: 192 },
                              height: { xs: 160, md: 192 },
                              display: "block",
                              objectFit: "contain",
                              borderRadius: "16px",
                            }}
                          />
                        ) : (
                          <Box
                            sx={{
                              width: { xs: 160, md: 192 },
                              height: { xs: 160, md: 192 },
                              display: "flex",
                              alignItems: "center",
                              justifyContent: "center",
                              textAlign: "center",
                              px: 2,
                            }}
                          >
                            <Typography
                              sx={{
                                fontFamily: dt.typography.fontFamily,
                                color: dt.colors.onSurfaceVariant,
                                fontSize: "0.9rem",
                                fontWeight: 600,
                              }}
                            >
                              QR code unavailable
                            </Typography>
                          </Box>
                        )}
                      </Box>

                      <Box
                        component="button"
                        type="button"
                        onClick={openBotLink}
                        disabled={!botLink?.deepLink}
                        sx={{
                          display: "flex",
                          alignItems: "center",
                          gap: 1.5,
                          px: 2.5,
                          py: 1.75,
                          borderRadius: "20px",
                          bgcolor: dt.colors.surfaceContainerLowest,
                          border: `1px solid ${dt.colors.outlineVariant}`,
                          boxShadow: "0 10px 30px rgba(15, 23, 42, 0.06)",
                          width: "100%",
                          cursor: botLink?.deepLink ? "pointer" : "default",
                          appearance: "none",
                          textAlign: "left",
                          transition: "transform 0.2s ease, box-shadow 0.2s ease",
                          "&:hover": botLink?.deepLink ? {
                            transform: { md: "translateY(-1px)" },
                            boxShadow: "0 14px 34px rgba(15, 23, 42, 0.1)",
                          } : undefined,
                          "&:disabled": {
                            opacity: 0.7,
                          },
                        }}
                      >
                        <Box
                          sx={{
                            width: 40,
                            height: 40,
                            borderRadius: "12px",
                            overflow: "hidden",
                            flexShrink: 0,
                          }}
                        >
                          <Box
                            component="img"
                            src={catQrImage}
                            alt="Mezon Quiz Bot"
                            sx={{
                              width: "100%",
                              height: "100%",
                              objectFit: "cover",
                            }}
                          />
                        </Box>
                        <Typography
                          sx={{
                            fontFamily: dt.typography.fontFamily,
                            fontWeight: 700,
                            color: dt.colors.onSurface,
                          }}
                        >
                          Practice with Mezon Bot
                        </Typography>
                      </Box>
                    </Stack>

                    <Typography
                      sx={{
                        textAlign: "center",
                        fontFamily: dt.typography.fontFamily,
                        ...dt.typography.labelSm,
                        color: dt.colors.outline,
                      }}
                    >
                      Join through bot chat using the session code or QR from the list.
                    </Typography>
                  </Stack>
                </CardContent>
              </Card>
            </Box>
          </Stack>
        </Stack>
      </Container>

      <AppSnackbar open={snackbar.open} message={snackbar.message} severity={snackbar.severity} onClose={closeSnackbar} />
    </Box>
  );
};

export default QuizDetailPage;
