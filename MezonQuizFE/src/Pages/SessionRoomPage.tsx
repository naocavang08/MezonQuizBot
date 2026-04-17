import { useCallback, useEffect, useMemo, useState } from "react";
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
    Tooltip,
    Typography,
} from "@mui/material";
import AppSnackbar from "../Components/AppSnackbar";
import UserIdentityCell from "../Components/UserIdentityCell";
import useAppSnackbar from "../Hooks/useAppSnackbar";
import { MdContentCopy, MdRefresh } from "react-icons/md";
import { useNavigate, useParams } from "react-router-dom";
import { HubConnectionBuilder, LogLevel, type HubConnection } from "@microsoft/signalr";
import { getSessionDetails, getSessionLeaderboard, clearSessionParticipant, startQuizSession } from "../Api/session.api";
import {
    SessionStatusValue,
    type QuizSessionDto,
    type SessionParticipantDto,
    type SessionStateChangedDto,
} from "../Interface/session.dto";
import useAuthStore from "../Stores/login.store";
import { isSameLeaderboard, isSameSession } from "../Lib/Utils/sessionRender";

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

const SessionRoomPage = () => {
    const { sessionId = "" } = useParams();
    const navigate = useNavigate();
    const userId = useAuthStore((state) => state.user?.id);

    const [session, setSession] = useState<QuizSessionDto | null>(null);
    const [leaderboard, setLeaderboard] = useState<SessionParticipantDto[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [isActionLoading, setIsActionLoading] = useState(false);
    const [clearingUserId, setClearingUserId] = useState<string | null>(null);
    const { snackbar, showError, showSuccess, closeSnackbar } = useAppSnackbar();

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

            const normalizedLeaderboard = Array.isArray(leaderboardData) ? leaderboardData : [];

            setSession((previous) => (isSameSession(previous, sessionData) ? previous : sessionData));
            setLeaderboard((previous) =>
                isSameLeaderboard(previous, normalizedLeaderboard) ? previous : normalizedLeaderboard
            );
        } catch {
            showError("Can not load session room right now.");
        } finally {
            if (!silent) {
                setIsLoading(false);
            }
        }
    }, [sessionId, showError]);

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
            return;
        }

        if (session.status !== SessionStatusValue.Waiting) {
            navigate(`/app/sessions/${sessionId}/start-quiz`, { replace: true });
        }
    }, [isHost, navigate, session, sessionId, userId]);

    const startSession = async () => {
        if (!sessionId || !userId) {
            showError("Host info is invalid.");
            return;
        }

        try {
            setIsActionLoading(true);
            const response = await startQuizSession(sessionId);
            showSuccess(response.message || "Session started successfully.");
            await loadSession(true);
            navigate(`/app/sessions/${sessionId}/start-quiz`);
        } catch {
            showError("Can not update session status right now.");
        } finally {
            setIsActionLoading(false);
        }
    };

    const clearParticipant = async (participantUserId: string) => {
        if (!sessionId || !isHost) {
            return;
        }

        try {
            setClearingUserId(participantUserId);
            const response = await clearSessionParticipant(sessionId, { userId: participantUserId });
            showSuccess(response.message || "Participant cleared successfully.");
            await loadSession(true);
        } catch {
            showError("Can not clear this participant right now.");
        } finally {
            setClearingUserId(null);
        }
    };

    const canStart = isHost && session?.status === SessionStatusValue.Waiting;
    const canGoToStartRoom = isHost && session?.status !== SessionStatusValue.Waiting;

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
                            Share the link, wait for players, then start the quiz.
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
                                            Session ID
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
                                            Session Code
                                        </Typography>
                                        <Button
                                            size="small"
                                            variant="text"
                                            startIcon={<MdContentCopy />}
                                            disabled={!session.code}
                                            onClick={() => {
                                                void copyToClipboard(session.code, "Session code copied.");
                                            }}
                                        >
                                            Copy Session Code
                                        </Button>
                                    </Stack>

                                    <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                                        <Typography variant="body2" color="text.secondary">
                                            Deep Link
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
                                            void startSession();
                                        }}
                                    >
                                        Start
                                    </Button>
                                    <Button
                                        variant="outlined"
                                        disabled={!canGoToStartRoom}
                                        onClick={() => {
                                            navigate(`/app/sessions/${sessionId}/start-quiz`);
                                        }}
                                    >
                                        Go to Start Room
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
                                Participants
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
                                            {isHost ? <TableCell align="right">Action</TableCell> : null}
                                        </TableRow>
                                    </TableHead>
                                    <TableBody>
                                        {leaderboard.map((participant, index) => (
                                            <TableRow key={`${participant.userId}-${index}`}>
                                                <TableCell>{participant.rank ?? index + 1}</TableCell>
                                                <TableCell>
                                                    <UserIdentityCell
                                                        userId={participant.userId}
                                                        displayName={participant.displayName}
                                                        avatarUrl={participant.avatarUrl}
                                                    />
                                                </TableCell>
                                                <TableCell align="right">{participant.totalScore}</TableCell>
                                                <TableCell align="right">{participant.correctCount}</TableCell>
                                                <TableCell align="right">{participant.answersCount}</TableCell>
                                                {isHost ? (
                                                    <TableCell align="right">
                                                        <Button
                                                            size="small"
                                                            color="error"
                                                            variant="outlined"
                                                            disabled={
                                                                clearingUserId === participant.userId ||
                                                                participant.userId === session?.hostId
                                                            }
                                                            onClick={() => {
                                                                void clearParticipant(participant.userId);
                                                            }}
                                                        >
                                                            {clearingUserId === participant.userId ? "Clearing..." : "Clear"}
                                                        </Button>
                                                    </TableCell>
                                                ) : null}
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
