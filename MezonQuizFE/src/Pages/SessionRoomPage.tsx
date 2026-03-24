import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
    Box,
    Button,
    Card,
    CardContent,
    Chip,
    CircularProgress,
    IconButton,
    Link,
    Stack,
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableRow,
    LinearProgress,
    Tooltip,
    Typography,
} from "@mui/material";
import AppSnackbar from "../Components/AppSnackbar";
import useAppSnackbar from "../Hooks/useAppSnackbar";
import { MdContentCopy, MdRefresh } from "react-icons/md";
import { useParams } from "react-router-dom";
import { HubConnectionBuilder, LogLevel, type HubConnection } from "@microsoft/signalr";
import {
    finishQuizSession,
    getCurrentSessionQuestion,
    getSessionDetails,
    getSessionLeaderboard,
    nextSessionQuestion,
    pauseQuizSession,
    resumeQuizSession,
    startQuizSession,
    submitSessionAnswer,
} from "../Api/session.api";
import {
    SessionStatusValue,
    type QuizSessionDto,
    type QuizSessionQuestionDto,
    type SessionParticipantDto,
    type SessionStateChangedDto,
} from "../Interface/session.dto";
import useAuthStore from "../Stores/login.store";

const statusLabel: Record<number, string> = {
    [SessionStatusValue.Waiting]: "Waiting",
    [SessionStatusValue.Active]: "Active",
    [SessionStatusValue.Paused]: "Paused",
    [SessionStatusValue.Finished]: "Finished",
    [SessionStatusValue.Cancelled]: "Cancelled",
};

const resolveHubUrl = () => {
    const base = (import.meta.env.VITE_QUIZ_API_URL ?? "").replace(/\/+$/, "");
    return `${base}/hubs/quiz-session`;
};

const SessionRoomPage = () => {
    const { sessionId = "" } = useParams();
    const userId = useAuthStore((state) => state.user?.id);

    const [session, setSession] = useState<QuizSessionDto | null>(null);
    const [leaderboard, setLeaderboard] = useState<SessionParticipantDto[]>([]);
    const [currentQuestion, setCurrentQuestion] = useState<QuizSessionQuestionDto | null>(null);
    const [selectedOption, setSelectedOption] = useState<number | null>(null);
    const [questionStartedAtMs, setQuestionStartedAtMs] = useState<number | null>(null);
    const [remainingSeconds, setRemainingSeconds] = useState(0);
    const [submittedQuestionIndex, setSubmittedQuestionIndex] = useState<number | null>(null);
    const [isSubmittingAnswer, setIsSubmittingAnswer] = useState(false);
    const [isLoading, setIsLoading] = useState(true);
    const [isActionLoading, setIsActionLoading] = useState(false);
    const { snackbar, showError, showSuccess, closeSnackbar } = useAppSnackbar();
    const questionIndexRef = useRef<number | null>(null);

    const isHost = useMemo(() => {
        if (!session || !userId) {
            return false;
        }

        return session.hostId === userId;
    }, [session, userId]);

    const shareLink = useMemo(() => session?.deepLink ?? "", [session?.deepLink]);
    const qrCodeUrl = useMemo(() => session?.qrCodeUrl ?? "", [session?.qrCodeUrl]);

    const copyToClipboard = async (value: string, successText: string) => {
        if (!value.trim()) {
            return;
        }

        try {
            await navigator.clipboard.writeText(value);
            showSuccess(successText);
        } catch {
            showError("Can not copy to clipboard right now.");
        }
    };

    const loadCurrentQuestion = useCallback(async () => {
        if (!sessionId) {
            return;
        }

        try {
            const data = await getCurrentSessionQuestion(sessionId);
            const previousQuestionIndex = questionIndexRef.current;

            setCurrentQuestion(data);

            if (previousQuestionIndex !== data.questionIndex) {
                setSelectedOption(null);
                setSubmittedQuestionIndex(null);
                setQuestionStartedAtMs(Date.now());
                setRemainingSeconds(data.timeLimitSeconds);
            } else if (questionStartedAtMs === null) {
                setQuestionStartedAtMs(Date.now());
                setRemainingSeconds(data.timeLimitSeconds);
            }

            questionIndexRef.current = data.questionIndex;
        } catch {
            setCurrentQuestion(null);
            setQuestionStartedAtMs(null);
            setRemainingSeconds(0);
        }
    }, [questionStartedAtMs, sessionId]);

    const loadSession = useCallback(async (silent = false) => {
        if (!sessionId) {
            showError("Session id is invalid.");
            setIsLoading(false);
            return;
        }

        if (!silent) {
            setIsLoading(true);
        }

        try {
            const [sessionData, leaderboardData] = await Promise.all([
                getSessionDetails(sessionId),
                getSessionLeaderboard(sessionId),
            ]);

            setSession(sessionData);
            setLeaderboard(Array.isArray(leaderboardData) ? leaderboardData : []);

            if (
                sessionData.status === SessionStatusValue.Active ||
                sessionData.status === SessionStatusValue.Paused
            ) {
                await loadCurrentQuestion();
            } else {
                setCurrentQuestion(null);
                setSelectedOption(null);
                setQuestionStartedAtMs(null);
                setRemainingSeconds(0);
                setSubmittedQuestionIndex(null);
                questionIndexRef.current = null;
            }
        } catch {
            showError("Can not load session room right now.");
        } finally {
            if (!silent) {
                setIsLoading(false);
            }
        }
    }, [loadCurrentQuestion, sessionId, showError]);

    useEffect(() => {
        void loadSession();
    }, [loadSession]);

    useEffect(() => {
        if (!sessionId) {
            return;
        }

        let connection: HubConnection | null = null;
        let isDisposed = false;

        const connectHub = async () => {
            try {
                const hub = new HubConnectionBuilder()
                    .withUrl(resolveHubUrl())
                    .configureLogging(LogLevel.Warning)
                    .withAutomaticReconnect()
                    .build();

                hub.on("SessionStateChanged", (payload: SessionStateChangedDto) => {
                    if (isDisposed || payload.sessionId !== sessionId) {
                        return;
                    }

                    void loadSession(true);
                });

                await hub.start();
                await hub.invoke("JoinSessionGroup", sessionId);
                connection = hub;
            } catch {
                // Keep fallback refresh active if realtime connection fails.
            }
        };

        void connectHub();

        const timer = window.setInterval(() => {
            void loadSession(true);
        }, 15000);

        return () => {
            isDisposed = true;
            window.clearInterval(timer);

            if (connection) {
                void connection.invoke("LeaveSessionGroup", sessionId).catch(() => undefined);
                void connection.stop().catch(() => undefined);
            }
        };
    }, [loadSession, sessionId]);

    useEffect(() => {
        if (!currentQuestion || questionStartedAtMs === null) {
            return;
        }

        const updateCountdown = () => {
            if (session?.status === SessionStatusValue.Paused) {
                return;
            }

            const elapsedSeconds = Math.floor((Date.now() - questionStartedAtMs) / 1000);
            const nextRemaining = Math.max(currentQuestion.timeLimitSeconds - elapsedSeconds, 0);
            setRemainingSeconds(nextRemaining);
        };

        updateCountdown();
        const timer = window.setInterval(updateCountdown, 1000);

        return () => {
            window.clearInterval(timer);
        };
    }, [currentQuestion, questionStartedAtMs, session?.status]);

    const runHostAction = async (action: "start" | "pause" | "resume" | "finish" | "next") => {
        if (!sessionId || !userId) {
            showError("Host info is invalid.");
            return;
        }

        try {
            setIsActionLoading(true);

            let response;

            if (action === "start") {
                response = await startQuizSession(sessionId);
            } else if (action === "next") {
                response = await nextSessionQuestion(sessionId);
            } else if (action === "pause") {
                response = await pauseQuizSession(sessionId);
            } else if (action === "resume") {
                response = await resumeQuizSession(sessionId);
            } else {
                response = await finishQuizSession(sessionId);
            }

            showSuccess(response.message || "Session updated successfully.");
            await loadSession(true);
        } catch {
            showError("Can not update session status right now.");
        } finally {
            setIsActionLoading(false);
        }
    };

    const submitAnswer = async () => {
        if (!sessionId || !userId) {
            showError("User info is invalid.");
            return;
        }

        if (selectedOption === null) {
            showError("Please choose an answer first.");
            return;
        }

        if (submittedQuestionIndex === currentQuestion?.questionIndex) {
            showSuccess("You already submitted this question.");
            return;
        }

        try {
            setIsSubmittingAnswer(true);
            // Payload theo SubmitAnswerDto: { userId, selectedOption, responseTimeMs? }
            const responseTimeMs =
                questionStartedAtMs !== null ? Math.max(0, Date.now() - questionStartedAtMs) : undefined;
            const response = await submitSessionAnswer(sessionId, {
                userId,
                selectedOption,
                responseTimeMs,
            });
            showSuccess(response.message || "Answer submitted.");
            if (typeof currentQuestion?.questionIndex === "number") {
                setSubmittedQuestionIndex(currentQuestion.questionIndex);
            }
            await loadSession(true);
        } catch {
            showError("Can not submit answer for this question.");
        } finally {
            setIsSubmittingAnswer(false);
        }
    };

    const canStart = isHost && session?.status === SessionStatusValue.Waiting;
    const canPause = isHost && session?.status === SessionStatusValue.Active;
    const canResume = isHost && session?.status === SessionStatusValue.Paused;
    const canNext = isHost && session?.status === SessionStatusValue.Active;
    const canFinish =
        isHost &&
        (session?.status === SessionStatusValue.Active || session?.status === SessionStatusValue.Paused);
    const isParticipantQuizLive =
        !isHost &&
        (session?.status === SessionStatusValue.Active || session?.status === SessionStatusValue.Paused);

    return (
        <Box sx={{ mt: 2 }}>
            <Stack spacing={2.5}>
                <Stack
                    direction={{ xs: "column", md: "row" }}
                    justifyContent="space-between"
                    alignItems={{ xs: "flex-start", md: "center" }}
                    spacing={1.5}
                >
                    <Box>
                        <Typography variant="h4" fontWeight={800}>
                            Session Room
                        </Typography>
                        <Typography color="text.secondary" sx={{ mt: 0.5 }}>
                            Monitor room status and leaderboard in realtime.
                        </Typography>
                    </Box>
                    <Tooltip title="Refresh now">
                        <IconButton
                            onClick={() => {
                                void loadSession();
                            }}
                        >
                            <MdRefresh />
                        </IconButton>
                    </Tooltip>
                </Stack>

                {isLoading ? (
                    <Stack alignItems="center" py={8}>
                        <CircularProgress />
                    </Stack>
                ) : null}

                {!isLoading && session ? (
                    <Card>
                        <CardContent>
                            <Stack spacing={2}>
                                <Stack direction={{ xs: "column", md: "row" }} spacing={1.2} alignItems={{ xs: "flex-start", md: "center" }}>
                                    <Typography variant="h6" fontWeight={700}>
                                        {session.quizTitle}
                                    </Typography>
                                    <Chip label={statusLabel[session.status] ?? "Unknown"} color="primary" variant="outlined" />
                                    {isHost ? <Chip label="Host" color="success" size="small" /> : null}
                                </Stack>

                                <Stack spacing={1}>
                                    <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                                        <Typography variant="body2" color="text.secondary">
                                            Session ID: {session.id}
                                        </Typography>
                                        <Button
                                            size="small"
                                            variant="text"
                                            startIcon={<MdContentCopy />}
                                            onClick={() => {
                                                void copyToClipboard(session.id, "Session ID copied.");
                                            }}
                                        >
                                            Copy Session ID
                                        </Button>
                                    </Stack>

                                    <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                                        <Typography variant="body2" color="text.secondary">
                                            Deep Link: {shareLink || "N/A"}
                                        </Typography>
                                        <Button
                                            size="small"
                                            variant="text"
                                            startIcon={<MdContentCopy />}
                                            onClick={() => {
                                                void copyToClipboard(shareLink, "Deep link copied.");
                                            }}
                                        >
                                            Copy Deep Link
                                        </Button>
                                    </Stack>

                                    <Stack spacing={0.8}>
                                        <Typography variant="body2" color="text.secondary">
                                            QR Code: {qrCodeUrl ? "Available" : "N/A"}
                                        </Typography>
                                        {qrCodeUrl ? (
                                            <Stack direction={{ xs: "column", sm: "row" }} spacing={1.2} alignItems={{ xs: "flex-start", sm: "center" }}>
                                                <Box
                                                    component="img"
                                                    src={qrCodeUrl}
                                                    alt="Session QR code"
                                                    sx={{
                                                        width: 140,
                                                        height: 140,
                                                        borderRadius: 1,
                                                        border: "1px solid",
                                                        borderColor: "divider",
                                                        objectFit: "cover",
                                                        backgroundColor: "background.paper",
                                                    }}
                                                />
                                                <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                                                    <Button
                                                        size="small"
                                                        variant="text"
                                                        startIcon={<MdContentCopy />}
                                                        onClick={() => {
                                                            void copyToClipboard(qrCodeUrl, "QR code URL copied.");
                                                        }}
                                                    >
                                                        Copy QR URL
                                                    </Button>
                                                    <Link href={qrCodeUrl} target="_blank" rel="noopener noreferrer" underline="hover">
                                                        Open QR
                                                    </Link>
                                                </Stack>
                                            </Stack>
                                        ) : null}
                                    </Stack>
                                </Stack>

                                <Stack direction={{ xs: "column", sm: "row" }} spacing={1.2}>
                                    <Button
                                        variant="contained"
                                        disabled={!canStart || isActionLoading}
                                        onClick={() => {
                                            void runHostAction("start");
                                        }}
                                    >
                                        Start
                                    </Button>
                                    <Button
                                        variant="contained"
                                        disabled={!canNext || isActionLoading}
                                        onClick={() => {
                                            void runHostAction("next");
                                        }}
                                    >
                                        Next Question
                                    </Button>
                                    <Button
                                        variant="outlined"
                                        disabled={!canPause || isActionLoading}
                                        onClick={() => {
                                            void runHostAction("pause");
                                        }}
                                    >
                                        Pause
                                    </Button>
                                    <Button
                                        variant="outlined"
                                        disabled={!canResume || isActionLoading}
                                        onClick={() => {
                                            void runHostAction("resume");
                                        }}
                                    >
                                        Resume
                                    </Button>
                                    <Button
                                        color="error"
                                        variant="contained"
                                        disabled={!canFinish || isActionLoading}
                                        onClick={() => {
                                            void runHostAction("finish");
                                        }}
                                    >
                                        Finish
                                    </Button>
                                </Stack>

                                <Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
                                    <Typography variant="body2" color="text.secondary">
                                        Participants: {session.participantCount}
                                    </Typography>
                                    <Typography variant="body2" color="text.secondary">
                                        Current Question: {session.currentQuestion}
                                    </Typography>
                                    <Typography variant="body2" color="text.secondary">
                                        Created: {new Date(session.createdAt).toLocaleString()}
                                    </Typography>
                                </Stack>
                            </Stack>
                        </CardContent>
                    </Card>
                ) : null}

                {!isLoading && isParticipantQuizLive ? (
                    <Card>
                        <CardContent>
                            <Stack spacing={2}>
                                <Typography variant="h6" fontWeight={700}>
                                    Quiz Player
                                </Typography>

                                {!currentQuestion ? (
                                    <Stack spacing={1}>
                                        <LinearProgress />
                                        <Typography color="text.secondary">
                                            Waiting for current question from host...
                                        </Typography>
                                    </Stack>
                                ) : (
                                    <Stack spacing={2}>
                                        <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                                            <Typography variant="subtitle2" color="text.secondary">
                                                Question #{currentQuestion.questionIndex + 1} • {currentQuestion.points} points
                                            </Typography>
                                            <Chip
                                                size="small"
                                                color={remainingSeconds <= 5 ? "warning" : "default"}
                                                label={`Time left: ${remainingSeconds}s`}
                                            />
                                            {submittedQuestionIndex === currentQuestion.questionIndex ? (
                                                <Chip size="small" color="success" label="Answer submitted" />
                                            ) : null}
                                        </Stack>
                                        <LinearProgress
                                            variant="determinate"
                                            value={Math.max(
                                                0,
                                                Math.min(
                                                    100,
                                                    (remainingSeconds / Math.max(currentQuestion.timeLimitSeconds, 1)) * 100,
                                                ),
                                            )}
                                        />
                                        <Typography variant="h6" fontWeight={700}>
                                            {currentQuestion.content}
                                        </Typography>

                                        {currentQuestion.mediaUrl ? (
                                            <Box
                                                component="img"
                                                src={currentQuestion.mediaUrl}
                                                alt="Question media"
                                                sx={{
                                                    width: "100%",
                                                    maxHeight: 280,
                                                    objectFit: "contain",
                                                    borderRadius: 1,
                                                    border: "1px solid",
                                                    borderColor: "divider",
                                                }}
                                            />
                                        ) : null}

                                        <Stack spacing={1}>
                                            {currentQuestion.options.map((option) => (
                                                <Button
                                                    key={option.index}
                                                    variant={selectedOption === option.index ? "contained" : "outlined"}
                                                    disabled={submittedQuestionIndex === currentQuestion.questionIndex}
                                                    onClick={() => {
                                                        setSelectedOption(option.index);
                                                    }}
                                                >
                                                    {option.content}
                                                </Button>
                                            ))}
                                        </Stack>

                                        <Stack direction="row" justifyContent="flex-end">
                                            <Button
                                                variant="contained"
                                                disabled={isSubmittingAnswer || submittedQuestionIndex === currentQuestion.questionIndex}
                                                onClick={() => {
                                                    void submitAnswer();
                                                }}
                                            >
                                                {isSubmittingAnswer ? "Submitting..." : "Submit answer"}
                                            </Button>
                                        </Stack>
                                    </Stack>
                                )}
                            </Stack>
                        </CardContent>
                    </Card>
                ) : null}

                {!isLoading ? (
                    <Card>
                        <CardContent>
                            <Typography variant="h6" fontWeight={700} sx={{ mb: 1.5 }}>
                                Leaderboard
                            </Typography>
                            {leaderboard.length === 0 ? (
                                <Typography color="text.secondary">No participants yet.</Typography>
                            ) : (
                                <Table size="small">
                                    <TableHead>
                                        <TableRow>
                                            <TableCell>Rank</TableCell>
                                            <TableCell>User ID</TableCell>
                                            <TableCell align="right">Score</TableCell>
                                            <TableCell align="right">Correct</TableCell>
                                            <TableCell align="right">Answers</TableCell>
                                        </TableRow>
                                    </TableHead>
                                    <TableBody>
                                        {leaderboard.map((participant, index) => (
                                            <TableRow key={`${participant.userId}-${index}`}>
                                                <TableCell>{participant.rank ?? index + 1}</TableCell>
                                                <TableCell>{participant.userId}</TableCell>
                                                <TableCell align="right">{participant.totalScore}</TableCell>
                                                <TableCell align="right">{participant.correctCount}</TableCell>
                                                <TableCell align="right">{participant.answersCount}</TableCell>
                                            </TableRow>
                                        ))}
                                    </TableBody>
                                </Table>
                            )}
                        </CardContent>
                    </Card>
                ) : null}
            </Stack>

            <AppSnackbar
                open={snackbar.open}
                message={snackbar.message}
                severity={snackbar.severity}
                onClose={closeSnackbar}
            />
        </Box>
    );
};

export default SessionRoomPage;
