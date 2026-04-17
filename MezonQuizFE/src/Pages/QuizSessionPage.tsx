import { useCallback, useEffect, useState } from "react";
import {
    Box,
    Button,
    Card,
    CardContent,
    Chip,
    CircularProgress,
    Divider,
    Link,
    Stack,
    Typography,
} from "@mui/material";
import { useNavigate, useParams } from "react-router-dom";
import AppSnackbar from "../Components/AppSnackbar";
import useAppSnackbar from "../Hooks/useAppSnackbar";
import { createQuizSession, deleteQuizSession, getQuizSessions } from "../Api/session.api";
import { getQuiz } from "../Api/quiz.api";
import { QuizStatus } from "../Interface/quiz.dto";
import { SessionStatusValue, type QuizSessionDto } from "../Interface/session.dto";
import useAuthStore from "../Stores/login.store";

const sessionStatusLabel: Record<number, string> = {
    [SessionStatusValue.Waiting]: "Waiting",
    [SessionStatusValue.Active]: "Active",
    [SessionStatusValue.Paused]: "Paused",
    [SessionStatusValue.Finished]: "Finished",
    [SessionStatusValue.Cancelled]: "Cancelled",
};

const sessionStatusColor: Record<number, "default" | "info" | "success" | "warning" | "error"> = {
    [SessionStatusValue.Waiting]: "info",
    [SessionStatusValue.Active]: "success",
    [SessionStatusValue.Paused]: "warning",
    [SessionStatusValue.Finished]: "default",
    [SessionStatusValue.Cancelled]: "error",
};

const QuizSessionPage = () => {
    const navigate = useNavigate();
    const { quizId = "" } = useParams<{ quizId: string }>();
    const userId = useAuthStore((state) => state.user?.id);

    const [quizTitle, setQuizTitle] = useState("");
    const [quizStatus, setQuizStatus] = useState<number | null>(null);
    const [sessions, setSessions] = useState<QuizSessionDto[]>([]);
    const [isLoadingPage, setIsLoadingPage] = useState(true);
    const [isLoadingSessions, setIsLoadingSessions] = useState(false);
    const [isCreatingSession, setIsCreatingSession] = useState(false);
    const [deletingSessionId, setDeletingSessionId] = useState<string | null>(null);
    const { snackbar, showError, showSuccess, closeSnackbar } = useAppSnackbar();

    const loadSessions = useCallback(async () => {
        if (!quizId) {
            return;
        }

        try {
            setIsLoadingSessions(true);
            const data = await getQuizSessions({
                hostId: userId || undefined,
                quizId,
                page: 1,
                pageSize: 50,
            });

            setSessions(Array.isArray(data.items) ? data.items : []);
        } catch {
            showError("Could not load created sessions for this quiz.");
        } finally {
            setIsLoadingSessions(false);
        }
    }, [quizId, userId, showError]);

    useEffect(() => {
        let isMounted = true;

        const loadPage = async () => {
            if (!quizId) {
                showError("Quiz id is missing.");
                setIsLoadingPage(false);
                return;
            }

            try {
                setIsLoadingPage(true);
                const [quizData, sessionsData] = await Promise.all([
                    getQuiz(quizId),
                    getQuizSessions({
                        hostId: userId || undefined,
                        quizId,
                        page: 1,
                        pageSize: 50,
                    }),
                ]);

                if (!isMounted) {
                    return;
                }

                setQuizTitle(quizData.title ?? "");
                setQuizStatus(quizData.status ?? null);
                setSessions(Array.isArray(sessionsData.items) ? sessionsData.items : []);
            } catch {
                if (isMounted) {
                    showError("Could not load quiz sessions.");
                }
            } finally {
                if (isMounted) {
                    setIsLoadingPage(false);
                }
            }
        };

        void loadPage();

        return () => {
            isMounted = false;
        };
    }, [quizId, userId, showError]);

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

    const handleCreateSession = async () => {
        if (!userId) {
            showError("User is not available. Please login again.");
            return;
        }

        if (!quizId) {
            showError("Quiz id is missing.");
            return;
        }

        if (quizStatus !== QuizStatus.Published) {
            showError("Only published quiz can create a session.");
            return;
        }

        try {
            setIsCreatingSession(true);
            const response = await createQuizSession({ quizId });
            showSuccess(response.message || "Session created successfully.");
            await loadSessions();
        } catch {
            showError("Can not create session for this quiz right now.");
        } finally {
            setIsCreatingSession(false);
        }
    };

    const handleDeleteSession = async (sessionId: string) => {
        if (!userId) {
            showError("User is not available. Please login again.");
            return;
        }

        const confirmed = window.confirm("Are you sure you want to delete this session?");
        if (!confirmed) {
            return;
        }

        try {
            setDeletingSessionId(sessionId);
            const result = await deleteQuizSession(sessionId);
            showSuccess(result.message || "Session deleted.");
            await loadSessions();
        } catch {
            showError("Failed to delete session.");
        } finally {
            setDeletingSessionId(null);
        }
    };

    if (isLoadingPage) {
        return (
            <Stack direction="row" justifyContent="center" sx={{ py: 8 }}>
                <CircularProgress />
            </Stack>
        );
    }

    return (
        <Box sx={{ mt: 2 }}>
            <Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" spacing={2} mb={3}>
                <Typography variant="h4" fontWeight={700} mb={1}>
                    Quiz Sessions {quizTitle ? `- ${quizTitle}` : ""}
                </Typography>
                <Stack direction="row" spacing={1}>
                    <Button variant="outlined" onClick={() => navigate(`/app/my-quizzes/${quizId}/settings`)}>
                        Open Quiz Setting
                    </Button>
                    <Button variant="outlined" onClick={() => navigate("/app/my-quizzes")}>
                        Back to My Quizzes
                    </Button>
                </Stack>
            </Stack>

            <Card variant="outlined" sx={{ mb: 3, backgroundColor: "transparent" }}>
                <CardContent>
                    <Stack spacing={1.5}>
                        <Stack
                            direction={{ xs: "column", sm: "row" }}
                            justifyContent="space-between"
                            alignItems={{ xs: "flex-start", sm: "center" }}
                            spacing={1}
                        >
                            <Stack spacing={0.5}>
                                <Typography variant="h6" fontWeight={700}>
                                    Created Sessions
                                </Typography>
                                <Typography variant="body2" color="text.secondary">
                                    Total: {sessions.length}
                                </Typography>
                            </Stack>

                            <Button
                                variant="contained"
                                size="small"
                                disabled={isCreatingSession}
                                onClick={() => {
                                    void handleCreateSession();
                                }}
                            >
                                {isCreatingSession ? "Creating..." : "Create Session"}
                            </Button>
                        </Stack>

                        {isLoadingSessions ? (
                            <Stack direction="row" justifyContent="center" sx={{ py: 2 }}>
                                <CircularProgress size={24} />
                            </Stack>
                        ) : null}

                        {!isLoadingSessions && sessions.length === 0 ? (
                            <Typography variant="body2" color="text.secondary">
                                No sessions created for this quiz yet.
                            </Typography>
                        ) : null}

                        {!isLoadingSessions && sessions.length > 0 ? (
                            <Stack spacing={1.2}>
                                {sessions.map((session) => {
                                    const deepLink = session.deepLink ?? "";
                                    const qrCodeUrl = session.qrCodeUrl ?? "";
                                    return (
                                        <Box
                                            key={session.id}
                                        >
                                            <Divider sx={{ my: 1.5 }} />
                                            
                                            <Stack spacing={0.8}>
                                                <Typography variant="subtitle2" fontWeight={700}>
                                                    Session {session.id}
                                                </Typography>
                                                <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                                                    <Typography variant="body2" color="text.secondary">
                                                        Status:
                                                    </Typography>
                                                    <Chip
                                                        label={sessionStatusLabel[session.status] ?? "Unknown"}
                                                        color={sessionStatusColor[session.status] ?? "default"}
                                                        size="small"
                                                        variant="outlined"
                                                    />
                                                    <Typography variant="body2" color="text.secondary">
                                                        Participants: {session.participantCount}
                                                    </Typography>
                                                </Stack>
                                                <Typography variant="body2" color="text.secondary">
                                                    Code: {session.code || "N/A"}
                                                </Typography>
                                                <Typography variant="body2" color="text.secondary">
                                                    Created: {new Date(session.createdAt).toLocaleString()}
                                                </Typography>
                                                {qrCodeUrl ? (
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

                                                <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
                                                    <Button
                                                        size="small"
                                                        variant="contained"
                                                        onClick={() => {
                                                            navigate(`/app/my-quizzes/${quizId}/sessions/${session.id}`);
                                                        }}
                                                    >
                                                        Open Session Room
                                                    </Button>
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
                                                        color="error"
                                                        disabled={deletingSessionId === session.id}
                                                        onClick={() => {
                                                            void handleDeleteSession(session.id);
                                                        }}
                                                    >
                                                        {deletingSessionId === session.id ? "Deleting..." : "Delete Session"}
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
                                            </Stack>
                                        </Box>
                                    );
                                })}
                            </Stack>
                        ) : null}
                    </Stack>
                </CardContent>
            </Card>

            <AppSnackbar
                open={snackbar.open}
                message={snackbar.message}
                severity={snackbar.severity}
                onClose={closeSnackbar}
            />
        </Box>
    );
};

export default QuizSessionPage;
