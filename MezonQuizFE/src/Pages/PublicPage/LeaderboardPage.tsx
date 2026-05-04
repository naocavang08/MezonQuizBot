import { useCallback, useEffect, useMemo, useState } from "react";
import {
  Avatar,
  Box,
  Button,
  Chip,
  CircularProgress,
  Container,
  Divider,
  Paper,
  Stack,
  Typography,
} from "@mui/material";
import { useNavigate, useParams } from "react-router-dom";
import {
  MdArrowBack,
  MdBolt,
  MdEmojiEvents,
  MdGroups,
  MdHistory,
  MdMilitaryTech,
  MdRefresh,
  MdSchedule,
  MdTimer,
  MdWorkspacePremium,
} from "react-icons/md";
import AppSnackbar from "../../Components/AppSnackbar";
import useAppSnackbar from "../../Hooks/useAppSnackbar";
import useSessionRealtime from "../../Hooks/useSessionRealtime";
import useAuthStore from "../../Stores/login.store";
import { getSessionDetails, getSessionLeaderboard } from "../../Api/session.api";
import type { QuizSessionDto, SessionParticipantDto } from "../../Interface/session.dto";
import { SessionStatusValue } from "../../Interface/session.dto";
import { canAccessApp, resolveDefaultAppPath } from "../../Lib/Utils/permissions";
import { isSameLeaderboard, isSameSession } from "../../Lib/Utils/sessionRender";
import { dt } from "../../Lib/designTokens";

const statusLabel: Record<number, string> = {
  [SessionStatusValue.Waiting]: "Waiting",
  [SessionStatusValue.Active]: "Active",
  [SessionStatusValue.Paused]: "Paused",
  [SessionStatusValue.Finished]: "Finished",
  [SessionStatusValue.Cancelled]: "Cancelled",
};

const formatCompletion = (participant: SessionParticipantDto) => {
  if (typeof participant.completionDurationSeconds !== "number") {
    return "-";
  }

  const total = Math.max(participant.completionDurationSeconds, 0);
  const minutes = Math.floor(total / 60);
  const seconds = total % 60;
  return `${minutes}m ${seconds}s`;
};

const formatSessionTime = (value?: string) => {
  if (!value) {
    return "Not started";
  }

  return new Date(value).toLocaleString();
};

const formatJoinedDate = (value?: string) => {
  if (!value) {
    return "Unknown";
  }

  return new Date(value).toLocaleDateString();
};

const getPodiumTone = (rank: number) => {
  if (rank === 1) {
    return {
      background: `linear-gradient(180deg, ${dt.colors.surfaceContainerLowest} 0%, #eef2ff 100%)`,
      borderColor: "#c7d2fe",
      ringColor: dt.colors.primaryContainer,
      badgeColor: dt.colors.primaryContainer,
      textColor: dt.colors.primary,
      height: { xs: "auto", md: 320 },
      scale: { xs: 1, md: 1.05 },
      icon: <MdWorkspacePremium size={28} color="#eab308" />,
    };
  }

  if (rank === 2) {
    return {
      background: `linear-gradient(180deg, ${dt.colors.surfaceContainerLowest} 0%, #f8fafc 100%)`,
      borderColor: "#cbd5e1",
      ringColor: "#cbd5e1",
      badgeColor: "#94a3b8",
      textColor: dt.colors.onSurface,
      height: { xs: "auto", md: 286 },
      scale: 1,
      icon: <MdMilitaryTech size={22} color="#64748b" />,
    };
  }

  return {
    background: `linear-gradient(180deg, ${dt.colors.surfaceContainerLowest} 0%, #fff7ed 100%)`,
    borderColor: "#fed7aa",
    ringColor: "#fdba74",
    badgeColor: "#fb923c",
    textColor: dt.colors.onSurface,
    height: { xs: "auto", md: 276 },
    scale: 1,
    icon: <MdMilitaryTech size={22} color="#f97316" />,
  };
};

const PodiumCard = ({ participant, rank }: { participant: SessionParticipantDto; rank: number }) => {
  const tone = getPodiumTone(rank);

  return (
    <Paper
      variant="outlined"
      sx={{
        position: "relative",
        px: 3,
        pb: 3,
        pt: { xs: 8, md: 9 },
        borderRadius: "24px",
        borderColor: tone.borderColor,
        background: tone.background,
        boxShadow: dt.shadows.card,
        height: tone.height,
        transform: { md: `scale(${typeof tone.scale === "number" ? tone.scale : 1})` },
      }}
    >
      <Stack alignItems="center" spacing={2} textAlign="center" sx={{ height: "100%", justifyContent: "center" }}>
        <Box sx={{ position: "absolute", top: -38, left: "50%", transform: "translateX(-50%)" }}>
          <Box sx={{ position: "relative" }}>
            <Avatar
              src={participant.avatarUrl}
              sx={{
                width: rank === 1 ? 96 : 80,
                height: rank === 1 ? 96 : 80,
                border: "4px solid white",
                boxShadow: "0 18px 36px rgba(15, 23, 42, 0.12)",
                outline: `4px solid ${tone.ringColor}`,
              }}
            >
              {(participant.displayName || participant.userId || "U").trim().charAt(0).toUpperCase()}
            </Avatar>
            <Box sx={{ position: "absolute", top: -6, right: -8 }}>{tone.icon}</Box>
          </Box>
          <Avatar sx={{ width: 30, height: 30, mt: -1.5, mx: "auto", bgcolor: tone.badgeColor, fontWeight: 800 }}>
            {rank}
          </Avatar>
        </Box>

        <Stack spacing={0.75} alignItems="center">
          <Typography
            sx={{
              fontFamily: dt.typography.fontFamily,
              ...dt.typography.h3,
              color: dt.colors.onSurface,
              fontWeight: 700,
              maxWidth: 220,
            }}
          >
            {participant.displayName || participant.userId}
          </Typography>
          <Typography
            sx={{
              fontFamily: dt.typography.fontFamily,
              ...dt.typography.labelSm,
              color: tone.textColor,
              fontWeight: 800,
              letterSpacing: "0.08em",
            }}
          >
            {participant.totalScore} XP
          </Typography>
        </Stack>

        <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap justifyContent="center">
          <Chip
            icon={<MdBolt size={16} />}
            label={`${participant.correctCount} correct`}
            size="small"
            sx={{ bgcolor: "rgba(255,255,255,0.8)", color: dt.colors.secondary }}
          />
          <Chip
            icon={<MdTimer size={16} />}
            label={formatCompletion(participant)}
            size="small"
            sx={{ bgcolor: "rgba(255,255,255,0.8)", color: dt.colors.onSurfaceVariant }}
          />
        </Stack>
      </Stack>
    </Paper>
  );
};

const PublicLeaderboardPage = () => {
  const navigate = useNavigate();
  const { quizId = "", sessionId = "" } = useParams();
  const { snackbar, closeSnackbar, showError } = useAppSnackbar();
  const currentUser = useAuthStore((state) => state.user);
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const roleName = useAuthStore((state) => state.roleName);
  const permissionName = useAuthStore((state) => state.permissionName);
  const hasSystemRole = useAuthStore((state) => state.hasSystemRole);

  const [session, setSession] = useState<QuizSessionDto | null>(null);
  const [leaderboard, setLeaderboard] = useState<SessionParticipantDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const loadData = useCallback(async (silent = false) => {
    if (!sessionId) {
      showError("Session id is invalid.");
      setIsLoading(false);
      return;
    }

    try {
      if (!silent) {
        setIsLoading(true);
      }

      const [sessionData, leaderboardData] = await Promise.all([
        getSessionDetails(sessionId),
        getSessionLeaderboard(sessionId),
      ]);
      const normalizedLeaderboard = Array.isArray(leaderboardData) ? leaderboardData : [];
      setSession((previous) => (isSameSession(previous, sessionData) ? previous : sessionData));
      setLeaderboard((previous) => (isSameLeaderboard(previous, normalizedLeaderboard) ? previous : normalizedLeaderboard));
    } catch {
      showError("Can not load leaderboard right now.");
      setSession(null);
      setLeaderboard([]);
    } finally {
      if (!silent) {
        setIsLoading(false);
      }
    }
  }, [sessionId, showError]);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  useSessionRealtime({
    sessionId,
    onSessionStateChanged: () => loadData(true),
    enabled: Boolean(sessionId),
    joinGroup: true,
  });

  const orderedLeaderboard = useMemo(() => {
    return leaderboard.map((participant, index) => ({
      participant,
      rank: participant.rank ?? index + 1,
    }));
  }, [leaderboard]);

  const podiumEntries = useMemo(() => {
    const podiumOrder = [2, 1, 3];
    return podiumOrder
      .map((rank) => orderedLeaderboard.find((entry) => entry.rank === rank))
      .filter((entry): entry is { participant: SessionParticipantDto; rank: number } => Boolean(entry));
  }, [orderedLeaderboard]);

  const currentUserEntry = useMemo(() => {
    if (!currentUser?.id) {
      return null;
    }

    return orderedLeaderboard.find(({ participant }) => participant.userId === currentUser.id) ?? null;
  }, [currentUser?.id, orderedLeaderboard]);

  const nextRankGap = useMemo(() => {
    if (!currentUserEntry || currentUserEntry.rank <= 1) {
      return null;
    }

    const nextHigherEntry = orderedLeaderboard.find(({ rank }) => rank === currentUserEntry.rank - 1);
    if (!nextHigherEntry) {
      return null;
    }

    return Math.max(0, nextHigherEntry.participant.totalScore - currentUserEntry.participant.totalScore);
  }, [currentUserEntry, orderedLeaderboard]);

  const defaultAppPath = resolveDefaultAppPath(permissionName, hasSystemRole, roleName);
  const hasAppAccess = canAccessApp(permissionName, hasSystemRole, roleName);

  return (
    <Box
      sx={{
        py: { xs: 3, md: 6 },
        minHeight: "100vh",
        bgcolor: dt.colors.background,
        backgroundImage:
          "radial-gradient(circle at top left, rgba(79, 70, 229, 0.12), transparent 32%), radial-gradient(circle at top right, rgba(87, 223, 254, 0.16), transparent 26%)",
      }}
    >
      <Container maxWidth="lg">
        <Stack spacing={4}>
          <Stack spacing={2} alignItems="center" textAlign="center">
            {isLoading ? (
              <CircularProgress />
            ) : null}
            <Typography
              sx={{
                fontFamily: dt.typography.fontFamily,
                ...dt.typography.display,
                color: dt.colors.onSurface,
                fontSize: { xs: "2.5rem", md: dt.typography.display.fontSize },
              }}
            >
              {session?.quizTitle?.trim()}
            </Typography>

            <Stack direction="row" spacing={1.25} flexWrap="wrap" useFlexGap justifyContent="center">
              {session ? (
                <Chip
                  label={`Status: ${statusLabel[session.status] ?? "Unknown"}`}
                  sx={{ color: dt.colors.onSurface, bgcolor: dt.colors.surfaceContainerLowest, border: `1px solid ${dt.colors.outlineVariant}` }}
                />
              ) : null}
              {session?.code ? (
                <Chip
                  label={`Code: ${session.code}`}
                  sx={{ color: dt.colors.onSurface, bgcolor: dt.colors.surfaceContainerLowest, border: `1px solid ${dt.colors.outlineVariant}` }}
                />
              ) : null}
            </Stack>

            <Stack direction="row" spacing={1}>
              <Button variant="outlined" startIcon={<MdRefresh />} onClick={() => void loadData()}>
                Refresh
              </Button>
              <Button variant="text" startIcon={<MdArrowBack />} onClick={() => navigate(`/quizzes/${quizId}`)}>
                Back to Quiz
              </Button>
            </Stack>
          </Stack>

          {session ? (
            <Paper
              variant="outlined"
              sx={{
                p: { xs: 2, md: 3 },
                borderRadius: "24px",
                borderColor: dt.colors.outlineVariant,
                bgcolor: "rgba(255,255,255,0.78)",
                backdropFilter: "blur(10px)",
                boxShadow: dt.shadows.card,
              }}
            >
              <Stack direction={{ xs: "column", md: "row" }} spacing={2} divider={<Divider flexItem orientation="vertical" sx={{ display: { xs: "none", md: "block" } }} />}>
                <Stack direction="row" spacing={1.25} alignItems="center" sx={{ flex: 1 }}>
                  <Avatar sx={{ bgcolor: "#eef2ff", color: dt.colors.primary }}>
                    <MdGroups />
                  </Avatar>
                  <Box>
                    <Typography sx={{ fontFamily: dt.typography.fontFamily, ...dt.typography.labelSm, color: dt.colors.outline }}>
                      Participants
                    </Typography>
                    <Typography sx={{ fontFamily: dt.typography.fontFamily, ...dt.typography.h2, color: dt.colors.onSurface }}>
                      {session.participantCount}
                    </Typography>
                  </Box>
                </Stack>

                <Stack direction="row" spacing={1.25} alignItems="center" sx={{ flex: 1 }}>
                  <Avatar sx={{ bgcolor: "#ecfeff", color: dt.colors.secondary }}>
                    <MdSchedule />
                  </Avatar>
                  <Box>
                    <Typography sx={{ fontFamily: dt.typography.fontFamily, ...dt.typography.labelSm, color: dt.colors.outline }}>
                      Started
                    </Typography>
                    <Typography sx={{ fontFamily: dt.typography.fontFamily, ...dt.typography.bodyMd, color: dt.colors.onSurface, fontWeight: 700 }}>
                      {formatSessionTime(session.startedAt)}
                    </Typography>
                  </Box>
                </Stack>

                <Stack direction="row" spacing={1.25} alignItems="center" sx={{ flex: 1 }}>
                  <Avatar sx={{ bgcolor: "#fff7ed", color: "#ea580c" }}>
                    <MdTimer />
                  </Avatar>
                  <Box>
                    <Typography sx={{ fontFamily: dt.typography.fontFamily, ...dt.typography.labelSm, color: dt.colors.outline }}>
                      Finished
                    </Typography>
                    <Typography sx={{ fontFamily: dt.typography.fontFamily, ...dt.typography.bodyMd, color: dt.colors.onSurface, fontWeight: 700 }}>
                      {formatSessionTime(session.finishedAt)}
                    </Typography>
                  </Box>
                </Stack>
              </Stack>
            </Paper>
          ) : null}

          {isLoading ? (
            <Stack direction="row" justifyContent="center" sx={{ py: 8 }}>
              <CircularProgress />
            </Stack>
          ) : null}

          {!isLoading && leaderboard.length === 0 ? (
            <Paper
              variant="outlined"
              sx={{
                p: 4,
                borderRadius: "24px",
                textAlign: "center",
                borderColor: dt.colors.outlineVariant,
                bgcolor: dt.colors.surfaceContainerLowest,
              }}
            >
              <Typography sx={{ fontFamily: dt.typography.fontFamily, ...dt.typography.h2, color: dt.colors.onSurface }}>
                No participant results yet
              </Typography>
              <Typography sx={{ mt: 1, fontFamily: dt.typography.fontFamily, ...dt.typography.bodyMd, color: dt.colors.onSurfaceVariant }}>
                This leaderboard is tied to the current session, so results will appear after participants submit answers here.
              </Typography>
            </Paper>
          ) : null}

          {!isLoading && leaderboard.length > 0 ? (
            <>
              {podiumEntries.length > 0 ? (
                <Box
                  sx={{
                    display: "grid",
                    gridTemplateColumns: { xs: "1fr", md: "repeat(3, minmax(0, 1fr))" },
                    gap: 3,
                    alignItems: "end",
                    pt: { xs: 0, md: 4 },
                  }}
                >
                  {podiumEntries.map((entry) => (
                    <Box
                      key={`${entry.participant.userId}-${entry.rank}`}
                      sx={{
                        order: { xs: entry.rank, md: entry.rank === 1 ? 2 : entry.rank === 2 ? 1 : 3 },
                      }}
                    >
                      <PodiumCard participant={entry.participant} rank={entry.rank} />
                    </Box>
                  ))}
                </Box>
              ) : null}

              <Paper
                variant="outlined"
                sx={{
                  overflow: "hidden",
                  borderRadius: "24px",
                  borderColor: dt.colors.outlineVariant,
                  bgcolor: dt.colors.surfaceContainerLowest,
                  boxShadow: dt.shadows.card,
                }}
              >
                <Box
                  sx={{
                    display: "grid",
                    gridTemplateColumns: { xs: "56px minmax(0, 1fr) 92px", md: "72px minmax(0, 1.5fr) 130px 120px 120px" },
                    gap: 2,
                    px: { xs: 2, md: 3 },
                    py: 2,
                    bgcolor: "rgba(248, 250, 252, 0.8)",
                    borderBottom: `1px solid ${dt.colors.outlineVariant}`,
                  }}
                >
                  <Typography sx={{ fontFamily: dt.typography.fontFamily, ...dt.typography.labelSm, color: dt.colors.outline, textAlign: "center" }}>
                    Rank
                  </Typography>
                  <Typography sx={{ fontFamily: dt.typography.fontFamily, ...dt.typography.labelSm, color: dt.colors.outline }}>
                    Player
                  </Typography>
                  <Typography sx={{ fontFamily: dt.typography.fontFamily, ...dt.typography.labelSm, color: dt.colors.outline, textAlign: "right" }}>
                    XP
                  </Typography>
                  <Typography sx={{ display: { xs: "none", md: "block" }, fontFamily: dt.typography.fontFamily, ...dt.typography.labelSm, color: dt.colors.outline, textAlign: "center" }}>
                    Correct
                  </Typography>
                  <Typography sx={{ display: { xs: "none", md: "block" }, fontFamily: dt.typography.fontFamily, ...dt.typography.labelSm, color: dt.colors.outline, textAlign: "right" }}>
                    Completed
                  </Typography>
                </Box>

                {orderedLeaderboard.map(({ participant, rank }, index) => (
                  <Box
                    key={`${participant.userId}-${rank}-${index}`}
                    sx={{
                      display: "grid",
                      gridTemplateColumns: { xs: "56px minmax(0, 1fr) 92px", md: "72px minmax(0, 1.5fr) 130px 120px 120px" },
                      gap: 2,
                      px: { xs: 2, md: 3 },
                      py: 2,
                      alignItems: "center",
                      borderBottom: index === orderedLeaderboard.length - 1 ? "none" : `1px solid rgba(226, 232, 240, 0.72)`,
                      bgcolor: rank <= 3 ? "rgba(248, 250, 252, 0.5)" : "transparent",
                      transition: "background-color 0.2s ease",
                      "&:hover": {
                        bgcolor: "rgba(241, 245, 249, 0.72)",
                      },
                    }}
                  >
                    <Stack direction="row" spacing={0.75} alignItems="center" justifyContent="center">
                      {rank <= 3 ? <MdEmojiEvents color={rank === 1 ? "#eab308" : rank === 2 ? "#94a3b8" : "#fb923c"} size={18} /> : null}
                      <Typography sx={{ fontFamily: dt.typography.fontFamily, fontWeight: rank <= 10 ? 800 : 700, color: rank <= 3 ? dt.colors.primary : dt.colors.onSurfaceVariant }}>
                        {rank}
                      </Typography>
                    </Stack>

                    <Stack direction="row" spacing={1.5} alignItems="center" minWidth={0}>
                      <Avatar src={participant.avatarUrl} sx={{ width: 42, height: 42 }}>
                        {(participant.displayName || participant.userId || "U").trim().charAt(0).toUpperCase()}
                      </Avatar>
                      <Box minWidth={0}>
                        <Typography
                          sx={{
                            fontFamily: dt.typography.fontFamily,
                            ...dt.typography.bodyMd,
                            color: dt.colors.onSurface,
                            fontWeight: 700,
                            overflow: "hidden",
                            textOverflow: "ellipsis",
                            whiteSpace: "nowrap",
                          }}
                        >
                          {participant.displayName || participant.userId}
                        </Typography>
                        <Typography
                          sx={{
                            fontFamily: dt.typography.fontFamily,
                            fontSize: "0.8rem",
                            color: dt.colors.onSurfaceVariant,
                            overflow: "hidden",
                            textOverflow: "ellipsis",
                            whiteSpace: "nowrap",
                          }}
                        >
                          Joined {formatJoinedDate(participant.joinedAt)}
                        </Typography>
                      </Box>
                    </Stack>

                    <Box textAlign="right">
                      <Typography sx={{ fontFamily: dt.typography.fontFamily, ...dt.typography.bodyMd, color: dt.colors.primary, fontWeight: 800 }}>
                        {participant.totalScore}
                      </Typography>
                      <Typography sx={{ fontFamily: dt.typography.fontFamily, fontSize: "0.75rem", color: dt.colors.outline }}>
                        {participant.answersCount} answers
                      </Typography>
                    </Box>

                    <Box sx={{ display: { xs: "none", md: "block" }, textAlign: "center" }}>
                      <Typography sx={{ fontFamily: dt.typography.fontFamily, ...dt.typography.bodyMd, color: dt.colors.secondary, fontWeight: 700 }}>
                        {participant.correctCount}
                      </Typography>
                      <Typography sx={{ fontFamily: dt.typography.fontFamily, fontSize: "0.75rem", color: dt.colors.outline }}>
                        Q{participant.currentQuestionIndex}
                      </Typography>
                    </Box>

                    <Box sx={{ display: { xs: "none", md: "block" }, textAlign: "right" }}>
                      <Typography sx={{ fontFamily: dt.typography.fontFamily, ...dt.typography.bodyMd, color: dt.colors.onSurface, fontWeight: 700 }}>
                        {formatCompletion(participant)}
                      </Typography>
                      <Typography sx={{ fontFamily: dt.typography.fontFamily, fontSize: "0.75rem", color: dt.colors.outline }}>
                        Session only
                      </Typography>
                    </Box>
                  </Box>
                ))}
              </Paper>

              {isAuthenticated && currentUser ? (
                <Paper
                  variant="outlined"
                  sx={{
                    px: { xs: 2, md: 3 },
                    py: 2.25,
                    borderRadius: "24px",
                    borderColor: dt.colors.primary,
                    bgcolor: dt.colors.primaryContainer,
                    color: dt.colors.onPrimary,
                    boxShadow: "0 20px 40px rgba(53, 37, 205, 0.18)",
                  }}
                >
                  <Stack direction={{ xs: "column", md: "row" }} justifyContent="space-between" spacing={2} alignItems={{ xs: "flex-start", md: "center" }}>
                    <Stack direction="row" spacing={2} alignItems="center">
                      <Box>
                        <Typography sx={{ fontFamily: dt.typography.fontFamily, fontSize: "0.75rem", textTransform: "uppercase", opacity: 0.82 }}>
                          Your Rank
                        </Typography>
                        <Typography sx={{ fontFamily: dt.typography.fontFamily, ...dt.typography.h2, color: dt.colors.onPrimary }}>
                          {currentUserEntry ? currentUserEntry.rank : "-"}
                        </Typography>
                      </Box>
                      <Divider flexItem orientation="vertical" sx={{ borderColor: "rgba(255,255,255,0.2)", display: { xs: "none", md: "block" } }} />
                      <Stack direction="row" spacing={1.5} alignItems="center">
                        <Avatar src={currentUser.avatarUrl} sx={{ width: 42, height: 42, bgcolor: "rgba(255,255,255,0.14)", border: "1px solid rgba(255,255,255,0.22)" }}>
                          {(currentUser.displayName || currentUser.username || "U").slice(0, 1).toUpperCase()}
                        </Avatar>
                        <Box>
                          <Typography sx={{ fontFamily: dt.typography.fontFamily, ...dt.typography.bodyMd, color: dt.colors.onPrimary, fontWeight: 700 }}>
                            {`You (${currentUser.displayName || currentUser.username})`}
                          </Typography>
                          <Stack direction="row" spacing={0.75} alignItems="center">
                            <MdHistory size={14} />
                            <Typography sx={{ fontFamily: dt.typography.fontFamily, fontSize: "0.8rem", opacity: 0.82 }}>
                              {currentUserEntry ? `Joined ${formatJoinedDate(currentUserEntry.participant.joinedAt)}` : "Not ranked in this session"}
                            </Typography>
                          </Stack>
                        </Box>
                      </Stack>
                    </Stack>

                    <Stack direction="row" spacing={{ xs: 2, md: 4 }} alignItems={{ xs: "flex-start", md: "center" }} sx={{ width: { xs: "100%", md: "auto" }, justifyContent: { xs: "space-between", md: "flex-end" } }}>
                      <Box textAlign={{ xs: "left", md: "right" }}>
                        <Typography sx={{ fontFamily: dt.typography.fontFamily, fontSize: "0.75rem", textTransform: "uppercase", opacity: 0.82 }}>
                          Next Rank in
                        </Typography>
                        <Typography sx={{ fontFamily: dt.typography.fontFamily, ...dt.typography.bodyMd, color: dt.colors.onPrimary, fontWeight: 700 }}>
                          {currentUserEntry && currentUserEntry.rank > 1 && nextRankGap !== null ? `${nextRankGap} XP` : "-"}
                        </Typography>
                      </Box>
                      <Box textAlign={{ xs: "left", md: "right" }}>
                        <Typography sx={{ fontFamily: dt.typography.fontFamily, fontSize: "0.75rem", textTransform: "uppercase", opacity: 0.82 }}>
                          Total XP
                        </Typography>
                        <Typography sx={{ fontFamily: dt.typography.fontFamily, ...dt.typography.h2, color: dt.colors.onPrimary }}>
                          {currentUserEntry?.participant.totalScore ?? 0}
                        </Typography>
                      </Box>
                      {hasAppAccess ? (
                        <Button
                          variant="contained"
                          onClick={() => navigate(isAuthenticated ? defaultAppPath : "/login")}
                          sx={{
                            bgcolor: dt.colors.surfaceContainerLowest,
                            color: dt.colors.primary,
                            boxShadow: "none",
                            textTransform: "none",
                            "&:hover": {
                              bgcolor: dt.colors.surfaceContainerLow,
                              boxShadow: "none",
                            },
                          }}
                        >
                          Open App
                        </Button>
                      ) : null}
                    </Stack>
                  </Stack>
                </Paper>
              ) : null}
            </>
          ) : null}
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

export default PublicLeaderboardPage;
