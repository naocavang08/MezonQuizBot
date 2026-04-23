import { useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import {
    Avatar,
    Box,
    Button,
    Card,
    CardContent,
    Chip,
    CircularProgress,
    IconButton,
    Paper,
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
import AppSnackbar from "../Components/AppSnackbar";
import UserIdentityCell from "../Components/UserIdentityCell";
import useAppSnackbar from "../Hooks/useAppSnackbar";
import { getQuizLeaderboard } from "../Api/session.api";
import { getQuiz } from "../Api/quiz.api";
import {
    SessionStatusValue,
    type QuizSessionDto,
    type SessionParticipantDto,
} from "../Interface/session.dto";
import { isSameLeaderboard, isSameSession } from "../Lib/Utils/sessionRender";
import { MdEmojiEvents, MdMilitaryTech, MdStars, MdTrendingUp } from "react-icons/md";
import { MdRefresh } from "react-icons/md";
import useSessionRealtime from "../Hooks/useSessionRealtime";

const statusLabel: Record<number, string> = {
    [SessionStatusValue.Waiting]: "Waiting",
    [SessionStatusValue.Active]: "Active",
    [SessionStatusValue.Paused]: "Paused",
    [SessionStatusValue.Finished]: "Finished",
    [SessionStatusValue.Cancelled]: "Cancelled",
};

const formatCompletion = (participant: SessionParticipantDto) => {
    if (typeof participant.completionDurationSeconds === "number") {
        const total = Math.max(participant.completionDurationSeconds, 0);
        const minutes = Math.floor(total / 60);
        const seconds = total % 60;
        return `${minutes}m ${seconds}s`;
    }

    return "-";
};

type RankTier = {
    label: string;
    icon: ReactNode;
    chipColor: "warning" | "error" | "secondary" | "info" | "default";
    rowBackground: string;
    borderColor: string;
};

const getRankTier = (rank: number): RankTier => {
    if (rank === 1) {
        return {
            label: "Top 1",
            icon: <MdEmojiEvents size={18} color="#ca8a04" />,
            chipColor: "warning",
            rowBackground: "linear-gradient(90deg, rgba(251,191,36,0.18), rgba(251,191,36,0.05))",
            borderColor: "rgba(202,138,4,0.5)",
        };
    }

    if (rank <= 3) {
        return {
            label: "Top 3",
            icon: <MdMilitaryTech size={18} color="#f97316" />,
            chipColor: "error",
            rowBackground: "linear-gradient(90deg, rgba(249,115,22,0.16), rgba(249,115,22,0.04))",
            borderColor: "rgba(249,115,22,0.45)",
        };
    }

    if (rank <= 5) {
        return {
            label: "Top 5",
            icon: <MdStars size={18} color="#7c3aed" />,
            chipColor: "secondary",
            rowBackground: "linear-gradient(90deg, rgba(124,58,237,0.14), rgba(124,58,237,0.04))",
            borderColor: "rgba(124,58,237,0.42)",
        };
    }

    if (rank <= 10) {
        return {
            label: "Top 10",
            icon: <MdTrendingUp size={18} color="#0284c7" />,
            chipColor: "info",
            rowBackground: "linear-gradient(90deg, rgba(14,165,233,0.14), rgba(14,165,233,0.04))",
            borderColor: "rgba(2,132,199,0.38)",
        };
    }

    return {
        label: "Ranked",
        icon: <MdTrendingUp size={18} color="#64748b" />,
        chipColor: "default",
        rowBackground: "transparent",
        borderColor: "transparent",
    };
};

const QuizLeaderboardPage = () => {
    const navigate = useNavigate();
    const { quizId = "" } = useParams();
    const { snackbar, closeSnackbar, showError } = useAppSnackbar();
    const [quizTitle, setQuizTitle] = useState<string>("");
    const [leaderboard, setLeaderboard] = useState<SessionParticipantDto[]>([]);
    const [isLoading, setIsLoading] = useState(true);

    const topLegendItems = useMemo(
        () => [getRankTier(1), getRankTier(3), getRankTier(5), getRankTier(10)],
        []
    );

    const loadData = useCallback(async (silent = false) => {
        if (!quizId) {
            showError("Quiz id is invalid.");
            setIsLoading(false);
            return;
        }

        try {
            if (!silent) {
                setIsLoading(true);
            }
            const [quizData, leaderboardData] = await Promise.all([
                getQuiz(quizId).catch(() => null),
                getQuizLeaderboard(quizId),
            ]);
            if (quizData) {
                setQuizTitle(quizData.title);
            }
            const normalizedLeaderboard = Array.isArray(leaderboardData) ? leaderboardData : [];
            setLeaderboard((previous) =>
                isSameLeaderboard(previous, normalizedLeaderboard) ? previous : normalizedLeaderboard
            );
        } catch {
            showError("Can not load leaderboard right now.");
            setLeaderboard([]);
        } finally {
            if (!silent) {
                setIsLoading(false);
            }
        }
    }, [quizId, showError]);

    useEffect(() => {
        void loadData();
    }, [loadData]);

    return (
        <Box sx={{ py: 2 }}>
            <Stack spacing={2.5}>
                <Paper
                    variant="outlined"
                    sx={{
                        p: 2,
                        background:
                            "radial-gradient(circle at 0% 0%, rgba(14,165,233,0.16), transparent 48%), radial-gradient(circle at 95% 0%, rgba(251,146,60,0.15), transparent 45%)",
                        boxShadow: "none",
                    }}
                >
                    <Stack
                        direction={{ xs: "column", md: "row" }}
                        justifyContent="space-between"
                        alignItems={{ xs: "flex-start", md: "center" }}
                        spacing={1.5}
                    >
                        <Box>
                            <Typography variant="h5" fontWeight={900}>
                                Quiz Leaderboard
                            </Typography>
                            <Typography variant="body2" color="text.secondary" mt={0.5}>
                                See overall ranking results for {quizTitle || "this quiz"}.
                            </Typography>
                        </Box>
                        <Stack direction="row" spacing={1} alignItems="center">
                            <Tooltip title="Refresh now">
                                <IconButton
                                    onClick={() => {
                                        void loadData();
                                    }}
                                >
                                    <MdRefresh />
                                </IconButton>
                            </Tooltip>
                            <Button variant="outlined" onClick={() => navigate(`/app/find-quizzes/${quizId}`)}>
                                Back to Quiz
                            </Button>
                        </Stack>
                    </Stack>
                </Paper>

                {isLoading ? (
                    <Stack direction="row" justifyContent="center" sx={{ py: 6 }}>
                        <CircularProgress />
                    </Stack>
                ) : null}

                {!isLoading ? (
                    <Stack spacing={2}>
                        <Paper variant="outlined" sx={{ p: 1.5, boxShadow: "none" }}>
                            <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                                {topLegendItems.map((item) => (
                                    <Chip
                                        key={item.label}
                                        icon={<Avatar sx={{ width: 20, height: 20, bgcolor: "transparent" }}>{item.icon}</Avatar>}
                                        label={item.label}
                                        variant="outlined"
                                        color={item.chipColor}
                                        size="small"
                                    />
                                ))}
                            </Stack>
                        </Paper>
                        <Card variant="outlined" sx={{ boxShadow: "none" }}>
                            <CardContent>
                                <Typography variant="h6" fontWeight={700} sx={{ mb: 1.5 }}>
                                    Leaderboard
                                </Typography>
                                {leaderboard.length === 0 ? (
                                    <Typography color="text.secondary">No participant results yet.</Typography>
                                ) : (
                                    <Table size="small">
                                        <TableHead>
                                            <TableRow>
                                                <TableCell>Rank</TableCell>
                                                <TableCell>Player</TableCell>
                                                <TableCell align="right">Sessions</TableCell>
                                                <TableCell align="right">Score</TableCell>
                                                <TableCell align="right">Correct</TableCell>
                                                <TableCell align="right">Answers</TableCell>
                                                <TableCell align="right">Completed In</TableCell>
                                            </TableRow>
                                        </TableHead>
                                        <TableBody>
                                            {leaderboard.map((participant, index) => {
                                                const rank = participant.rank ?? index + 1;
                                                const tier = getRankTier(rank);
                                                return (
                                                    <TableRow
                                                        key={`${participant.userId}-${index}`}
                                                        hover
                                                        sx={{
                                                            background: tier.rowBackground,
                                                            borderLeft: `3px solid ${tier.borderColor}`,
                                                        }}
                                                    >
                                                        <TableCell>
                                                            <Stack direction="row" spacing={1} alignItems="center">
                                                                {tier.icon}
                                                                <Typography fontWeight={rank <= 10 ? 700 : 500}>#{rank}</Typography>
                                                            </Stack>
                                                        </TableCell>
                                                        <TableCell>
                                                            <UserIdentityCell
                                                                userId={participant.userId}
                                                                displayName={participant.displayName}
                                                                avatarUrl={participant.avatarUrl}
                                                            />
                                                        </TableCell>
                                                        <TableCell align="right">{participant.totalSessionsCount ?? 1}</TableCell>
                                                        <TableCell align="right">{participant.totalScore}</TableCell>
                                                        <TableCell align="right">{participant.correctCount}</TableCell>
                                                        <TableCell align="right">{participant.answersCount}</TableCell>
                                                        <TableCell align="right">{formatCompletion(participant)}</TableCell>
                                                    </TableRow>
                                                );
                                            })}
                                        </TableBody>
                                    </Table>
                                )}
                            </CardContent>
                        </Card>
                    </Stack>
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

export default QuizLeaderboardPage;
