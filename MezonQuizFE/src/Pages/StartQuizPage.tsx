import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
    Box,
    Button,
    Card,
    CardContent,
    Chip,
    CircularProgress,
    IconButton,
    Stack,
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableRow,
    Tooltip,
    Typography,
} from "@mui/material";
import { useNavigate, useParams } from "react-router-dom";
import { HubConnectionBuilder, LogLevel, type HubConnection } from "@microsoft/signalr";
import AppSnackbar from "../Components/AppSnackbar";
import useAppSnackbar from "../Hooks/useAppSnackbar";
import { MdRefresh } from "react-icons/md";
import {
    finishQuizSession,
    getCurrentSessionQuestion,
    getSessionDetails,
    getSessionLeaderboard,
    nextSessionQuestion,
    pauseQuizSession,
    resumeQuizSession,
    startQuizSession,
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
    return "/hubs/quiz-session";
};

const StartQuizPage = () => {
    const { sessionId = "" } = useParams();
    const navigate = useNavigate();
    const userId = useAuthStore((state) => state.user?.id);

    const [session, setSession] = useState<QuizSessionDto | null>(null);
    const [leaderboard, setLeaderboard] = useState<SessionParticipantDto[]>([]);
    const [currentQuestion, setCurrentQuestion] = useState<QuizSessionQuestionDto | null>(null);
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

    const loadCurrentQuestion = useCallback(async () => {
        if (!sessionId) {
            return;
        }

        try {
            const data = await getCurrentSessionQuestion(sessionId);
            const previousQuestionIndex = questionIndexRef.current;

            setCurrentQuestion(data);

            if (previousQuestionIndex !== data.questionIndex) {
                questionIndexRef.current = data.questionIndex;
            } else if (questionIndexRef.current === null) {
                questionIndexRef.current = data.questionIndex;
            }
        } catch {
            setCurrentQuestion(null);
            questionIndexRef.current = null;
        }
    }, [sessionId]);

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
                questionIndexRef.current = null;
            }
        } catch {
            showError("Can not load quiz room right now.");
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
        }, 5000);

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
        if (!session || !userId) {
            return;
        }

        if (!isHost) {
            navigate(`/app/sessions/${sessionId}/play`, { replace: true });
        }
    }, [isHost, navigate, session, sessionId, userId]);

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

    const canStart = isHost && session?.status === SessionStatusValue.Waiting;
    const canPause = isHost && session?.status === SessionStatusValue.Active;
    const canResume = isHost && session?.status === SessionStatusValue.Paused;
    const canNext = isHost && session?.status === SessionStatusValue.Active;
    const canFinish =
        isHost &&
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
                            Start Quiz
                        </Typography>
                        <Typography color="text.secondary" sx={{ mt: 0.5 }}>
                            Control the session and monitor the leaderboard.
                        </Typography>
                    </Box>
                    <Stack direction="row" spacing={1} alignItems="center">
                        <Button
                            variant="text"
                            onClick={() => {
                                navigate(`/app/sessions/${sessionId}`);
                            }}
                        >
                            Back to Waiting Room
                        </Button>
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

                                {currentQuestion ? (
                                    <Stack spacing={1}>
                                        <Typography variant="subtitle2" color="text.secondary">
                                            Current Question Preview
                                        </Typography>
                                        <Typography variant="h6" fontWeight={700}>
                                            {currentQuestion.content}
                                        </Typography>
                                    </Stack>
                                ) : null}
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
                                            <TableCell>Player</TableCell>
                                            <TableCell align="right">Score</TableCell>
                                            <TableCell align="right">Correct</TableCell>
                                            <TableCell align="right">Answers</TableCell>
                                        </TableRow>
                                    </TableHead>
                                    <TableBody>
                                        {leaderboard.map((participant, index) => (
                                            <TableRow key={`${participant.userId}-${index}`}>
                                                <TableCell>{participant.rank ?? index + 1}</TableCell>
                                                <TableCell>{participant.displayName || participant.userId}</TableCell>
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

export default StartQuizPage;
