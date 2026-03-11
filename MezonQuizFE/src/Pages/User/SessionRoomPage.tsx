import { useCallback, useEffect, useMemo, useState } from "react";
import {
    Alert,
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
import { MdContentCopy, MdRefresh } from "react-icons/md";
import { useParams } from "react-router-dom";
import {
    finishQuizSession,
    getSessionDetails,
    getSessionLeaderboard,
    pauseQuizSession,
    resumeQuizSession,
    startQuizSession,
} from "../../Api/session.api";
import { SessionStatusValue, type QuizSessionDto, type SessionParticipantDto } from "../../Interface/session.dto";
import useAuthStore from "../../Stores/login.store";

const statusLabel: Record<number, string> = {
    [SessionStatusValue.Waiting]: "Waiting",
    [SessionStatusValue.Active]: "Active",
    [SessionStatusValue.Paused]: "Paused",
    [SessionStatusValue.Finished]: "Finished",
    [SessionStatusValue.Cancelled]: "Cancelled",
};

const SessionRoomPage = () => {
    const { sessionId = "" } = useParams();
    const userId = useAuthStore((state) => state.user?.id);

    const [session, setSession] = useState<QuizSessionDto | null>(null);
    const [leaderboard, setLeaderboard] = useState<SessionParticipantDto[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [isActionLoading, setIsActionLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [message, setMessage] = useState<string | null>(null);

    const isHost = useMemo(() => {
        if (!session || !userId) {
            return false;
        }

        return session.hostId === userId;
    }, [session, userId]);

    const shareLink = useMemo(() => {
        if (session?.deepLink) {
            return session.deepLink;
        }

        if (!sessionId) {
            return "";
        }

        return `${window.location.origin}/user/sessions/${sessionId}`;
    }, [session?.deepLink, sessionId]);

    const copyToClipboard = async (value: string, successText: string) => {
        if (!value.trim()) {
            return;
        }

        try {
            await navigator.clipboard.writeText(value);
            setMessage(successText);
        } catch {
            setError("Can not copy to clipboard right now.");
        }
    };

    const loadSession = useCallback(async (silent = false) => {
        if (!sessionId) {
            setError("Session id is invalid.");
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
            setError(null);
        } catch {
            setError("Can not load session room right now.");
        } finally {
            if (!silent) {
                setIsLoading(false);
            }
        }
    }, [sessionId]);

    useEffect(() => {
        void loadSession();
    }, [loadSession]);

    useEffect(() => {
        if (!sessionId) {
            return;
        }

        const timer = window.setInterval(() => {
            void loadSession(true);
        }, 5000);

        return () => {
            window.clearInterval(timer);
        };
    }, [loadSession, sessionId]);

    const runHostAction = async (action: "start" | "pause" | "resume" | "finish") => {
        if (!sessionId || !userId) {
            setError("Host info is invalid.");
            return;
        }

        try {
            setIsActionLoading(true);
            setMessage(null);
            setError(null);

            let response;

            if (action === "start") {
                response = await startQuizSession(sessionId);
            } else if (action === "pause") {
                response = await pauseQuizSession(sessionId);
            } else if (action === "resume") {
                response = await resumeQuizSession(sessionId);
            } else {
                response = await finishQuizSession(sessionId);
            }

            setMessage(response.message || "Session updated successfully.");
            await loadSession(true);
        } catch {
            setError("Can not update session status right now.");
        } finally {
            setIsActionLoading(false);
        }
    };

    const canStart = isHost && session?.status === SessionStatusValue.Waiting;
    const canPause = isHost && session?.status === SessionStatusValue.Active;
    const canResume = isHost && session?.status === SessionStatusValue.Paused;
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

                {error ? <Alert severity="error">{error}</Alert> : null}
                {message ? <Alert severity="success">{message}</Alert> : null}

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

                {!isLoading ? (
                    <Card>
                        <CardContent>
                            <Typography variant="h6" fontWeight={700} sx={{ mb: 1.5 }}>
                                Leaderboard (Auto refresh every 5s)
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
        </Box>
    );
};

export default SessionRoomPage;
