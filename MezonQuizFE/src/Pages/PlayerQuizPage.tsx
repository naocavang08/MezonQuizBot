import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
    Box,
    Button,
    Card,
    CardContent,
    Chip,
    CircularProgress,
    LinearProgress,
    Stack,
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableRow,
    Typography,
} from "@mui/material";
import { useNavigate, useParams } from "react-router-dom";
import AppSnackbar from "../Components/AppSnackbar";
import useAppSnackbar from "../Hooks/useAppSnackbar";
import {
    getCurrentSessionQuestion,
    getSessionDetails,
    getSessionLeaderboard,
    submitSessionAnswer,
} from "../Api/session.api";
import {
    SessionStatusValue,
    type QuizSessionDto,
    type QuizSessionQuestionDto,
    type SessionParticipantDto,
} from "../Interface/session.dto";
import { QuestionType } from "../Interface/quiz.dto";
import useAuthStore from "../Stores/login.store";
import { isSameLeaderboard, isSameQuestion, isSameSession } from "../Lib/Utils/sessionRender";
import useSessionRealtime from "../Hooks/useSessionRealtime";

const PlayerQuizPage = () => {
    const { quizId = "", sessionId = "" } = useParams();
    const navigate = useNavigate();
    const userId = useAuthStore((state) => state.user?.id);

    const [session, setSession] = useState<QuizSessionDto | null>(null);
    const [leaderboard, setLeaderboard] = useState<SessionParticipantDto[]>([]);
    const [currentQuestion, setCurrentQuestion] = useState<QuizSessionQuestionDto | null>(null);
    const [selectedOptions, setSelectedOptions] = useState<number[]>([]);
    const [questionStartedAtMs, setQuestionStartedAtMs] = useState<number | null>(null);
    const [remainingSeconds, setRemainingSeconds] = useState(0);
    const [submittedQuestionIndex, setSubmittedQuestionIndex] = useState<number | null>(null);
    const [isSubmittingAnswer, setIsSubmittingAnswer] = useState(false);
    const [isLoading, setIsLoading] = useState(true);
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

            setCurrentQuestion((previous) => (isSameQuestion(previous, data) ? previous : data));

            if (previousQuestionIndex !== data.questionIndex) {
                setSelectedOptions([]);
                setSubmittedQuestionIndex(null);
                setQuestionStartedAtMs(Date.now());
                setRemainingSeconds(data.timeLimitSeconds);
            } else {
                setQuestionStartedAtMs((previousStartedAt) => previousStartedAt ?? Date.now());
                setRemainingSeconds((previousRemaining) =>
                    previousRemaining === data.timeLimitSeconds ? previousRemaining : data.timeLimitSeconds
                );
            }

            questionIndexRef.current = data.questionIndex;
        } catch {
            setCurrentQuestion(null);
            setQuestionStartedAtMs(null);
            setRemainingSeconds(0);
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

            const normalizedLeaderboard = Array.isArray(leaderboardData) ? leaderboardData : [];

            setSession((previous) => (isSameSession(previous, sessionData) ? previous : sessionData));
            setLeaderboard((previous) =>
                isSameLeaderboard(previous, normalizedLeaderboard) ? previous : normalizedLeaderboard
            );

            if (
                sessionData.status === SessionStatusValue.Active ||
                sessionData.status === SessionStatusValue.Paused
            ) {
                await loadCurrentQuestion();
            } else {
                setCurrentQuestion(null);
                setSelectedOptions([]);
                setQuestionStartedAtMs(null);
                setRemainingSeconds(0);
                setSubmittedQuestionIndex(null);
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

    useSessionRealtime({
        sessionId,
        onSessionStateChanged: () => loadSession(true),
    });

    useEffect(() => {
        if (!currentQuestion || questionStartedAtMs === null) {
            return;
        }

        if (submittedQuestionIndex === currentQuestion.questionIndex) {
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
    }, [currentQuestion, questionStartedAtMs, session?.status, submittedQuestionIndex]);

    useEffect(() => {
        if (!session || !sessionId || !userId) {
            return;
        }

        if (isHost) {
            navigate(`/app/my-quizzes/${quizId}/sessions/${sessionId}/start-quiz`, { replace: true });
        }
    }, [isHost, navigate, quizId, session, sessionId, userId]);

    useEffect(() => {
        if (!session || !userId || isHost) {
            return;
        }

        const isLiveSession =
            session.status === SessionStatusValue.Active || session.status === SessionStatusValue.Paused;
        if (!isLiveSession) {
            return;
        }

        const stillInSession = leaderboard.some((participant) => participant.userId === userId);
        if (!stillInSession) {
            showError("You were removed from this session by host.");
            navigate("/app/find-quizzes", { replace: true });
        }
    }, [isHost, leaderboard, navigate, session, showError, userId]);

    const submitAnswer = async () => {
        if (!sessionId || !userId) {
            showError("User info is invalid.");
            return;
        }

        const isMultipleChoice = currentQuestion?.questionType === QuestionType.MultipleChoice;
        if (selectedOptions.length === 0) {
            showError("Please choose an answer first.");
            return;
        }

        if (submittedQuestionIndex === currentQuestion?.questionIndex) {
            showSuccess("You already submitted this question.");
            return;
        }

        try {
            setIsSubmittingAnswer(true);
            const responseTimeMs =
                questionStartedAtMs !== null ? Math.max(0, Date.now() - questionStartedAtMs) : undefined;
            const response = await submitSessionAnswer(sessionId, {
                userId,
                selectedOption: selectedOptions[0],
                selectedOptions: isMultipleChoice ? selectedOptions : undefined,
                responseTimeMs,
            });
            showSuccess(response.message || "Answer submitted.");
            if (typeof currentQuestion?.questionIndex === "number") {
                setSubmittedQuestionIndex(currentQuestion.questionIndex);
                setRemainingSeconds(0);
            }
            await loadSession(true);
        } catch {
            showError("Can not submit answer for this question.");
        } finally {
            setIsSubmittingAnswer(false);
        }
    };

    const isQuizLive =
        session?.status === SessionStatusValue.Active || session?.status === SessionStatusValue.Paused;

    const formatCompletion = (participant: SessionParticipantDto) => {
        if (typeof participant.completionDurationSeconds === "number") {
            const total = Math.max(participant.completionDurationSeconds, 0);
            const minutes = Math.floor(total / 60);
            const seconds = total % 60;
            return `${minutes}m ${seconds}s`;
        }

        return "-";
    };

    return (
        <Box sx={{ mt: 2 }}>
            <Stack spacing={2.5}>
                <Box>
                    <Typography variant="h4" fontWeight={800}>
                        Quiz Player
                    </Typography>
                    <Typography color="text.secondary" sx={{ mt: 0.5 }}>
                        Join the live session and answer questions in realtime.
                    </Typography>
                </Box>

                {isLoading ? (
                    <Stack alignItems="center" py={8}>
                        <CircularProgress />
                    </Stack>
                ) : null}

                {!isLoading && session ? (
                    <Card>
                        <CardContent>
                            <Stack spacing={1.2}>
                                <Typography variant="h6" fontWeight={700}>
                                    {session.quizTitle}
                                </Typography>
                                <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                                    <Chip
                                        label={
                                            session.status === SessionStatusValue.Waiting
                                                ? "Waiting"
                                                : session.status === SessionStatusValue.Active
                                                  ? "Active"
                                                  : session.status === SessionStatusValue.Paused
                                                    ? "Paused"
                                                    : session.status === SessionStatusValue.Finished
                                                      ? "Finished"
                                                      : "Cancelled"
                                        }
                                        color="primary"
                                        variant="outlined"
                                    />
                                    <Typography variant="body2" color="text.secondary">
                                        Participants: {session.participantCount}
                                    </Typography>
                                </Stack>
                            </Stack>
                        </CardContent>
                    </Card>
                ) : null}

                {!isLoading && isQuizLive ? (
                    <Card>
                        <CardContent>
                            <Stack spacing={2}>
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
                                                Question #{currentQuestion.questionIndex + 1} - {currentQuestion.points} points
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

                                        {currentQuestion.questionType === QuestionType.MultipleChoice ? (
                                            <Typography variant="body2" color="text.secondary">
                                                Multiple choice: select all correct answers.
                                            </Typography>
                                        ) : null}

                                        <Stack spacing={1}>
                                            {currentQuestion.options.map((option) => (
                                                <Button
                                                    key={option.index}
                                                    variant={selectedOptions.includes(option.index) ? "contained" : "outlined"}
                                                    disabled={submittedQuestionIndex === currentQuestion.questionIndex}
                                                    onClick={() => {
                                                        if (currentQuestion.questionType === QuestionType.MultipleChoice) {
                                                            setSelectedOptions((prev) =>
                                                                prev.includes(option.index)
                                                                    ? prev.filter((item) => item !== option.index)
                                                                    : [...prev, option.index]
                                                            );
                                                            return;
                                                        }

                                                        setSelectedOptions([option.index]);
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
                ) : (
                    !isLoading && (
                        <Card>
                            <CardContent>
                                <Typography color="text.secondary">
                                    The host has not started the quiz yet.
                                </Typography>
                            </CardContent>
                        </Card>
                    )
                )}

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
                                            <TableCell align="right">Progress</TableCell>
                                            <TableCell align="right">Completed In</TableCell>
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
                                                <TableCell align="right">{participant.currentQuestionIndex}</TableCell>
                                                <TableCell align="right">{formatCompletion(participant)}</TableCell>
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

export default PlayerQuizPage;
