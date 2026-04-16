import { useCallback, useEffect, useState } from "react";
import {
	Box,
	Button,
	Chip,
	CircularProgress,
	Divider,
	FormControl,
	InputLabel,
	Link,
	MenuItem,
	Pagination,
	Paper,
	Select,
	Stack,
	Table,
	TableBody,
	TableCell,
	TableHead,
	TableRow,
	TextField,
	Typography,
} from "@mui/material";
import AppSnackbar from "../../Components/AppSnackbar";
import useAppSnackbar from "../../Hooks/useAppSnackbar";
import { getAllCategories } from "../../Api/category.api";
import { getQuiz, getAllQuizzes } from "../../Api/quiz.api";
import {
	finishQuizSession,
	getQuizSessions,
	pauseQuizSession,
	resumeQuizSession,
	startQuizSession,
} from "../../Api/session.api";
import type { CategoryDto } from "../../Interface/category.dto";
import { QuizStatus, type Quiz, type QuizDto } from "../../Interface/quiz.dto";
import { SessionStatusValue, type QuizSessionDto } from "../../Interface/session.dto";

const statusLabel: Record<QuizStatus, string> = {
	[QuizStatus.Draft]: "Draft",
	[QuizStatus.Published]: "Published",
	[QuizStatus.Archived]: "Archived",
};

const statusColor: Record<QuizStatus, "default" | "warning" | "success"> = {
	[QuizStatus.Draft]: "warning",
	[QuizStatus.Published]: "success",
	[QuizStatus.Archived]: "default",
};

const sessionStatusLabel: Record<number, string> = {
	[SessionStatusValue.Waiting]: "Waiting",
	[SessionStatusValue.Active]: "Active",
	[SessionStatusValue.Paused]: "Paused",
	[SessionStatusValue.Finished]: "Finished",
	[SessionStatusValue.Cancelled]: "Cancelled",
};

const QuizPage = () => {
	const [quizzes, setQuizzes] = useState<QuizDto[]>([]);
	const [selectedQuizId, setSelectedQuizId] = useState("");
	const [selectedQuiz, setSelectedQuiz] = useState<Quiz | null>(null);
	const [sessions, setSessions] = useState<QuizSessionDto[]>([]);
	const [loading, setLoading] = useState(false);
	const [isLoadingCategories, setIsLoadingCategories] = useState(false);
	const [detailsLoading, setDetailsLoading] = useState(false);
	const [isLoadingSessions, setIsLoadingSessions] = useState(false);
	const [sessionActionKey, setSessionActionKey] = useState("");
	const { snackbar, showError, showSuccess, closeSnackbar } = useAppSnackbar();
	const [categories, setCategories] = useState<CategoryDto[]>([]);
	const [selectedCategory, setSelectedCategory] = useState("");
	const [searchInput, setSearchInput] = useState("");
	const [searchTitle, setSearchTitle] = useState("");
	const [page, setPage] = useState(1);
	const [pageSize] = useState(10);
	const [totalPages, setTotalPages] = useState(0);
	const [totalCount, setTotalCount] = useState(0);

	const fetchQuizzes = useCallback(async () => {
		setLoading(true);
		try {
			const data = await getAllQuizzes({
				page,
				pageSize,
				title: searchTitle || undefined,
				category: selectedCategory || undefined,
			});
			setQuizzes(Array.isArray(data.items) ? data.items : []);
			setTotalPages(Number(data.totalPages ?? 0));
			setTotalCount(Number(data.totalCount ?? 0));
		} catch {
			showError("Could not load quizzes.");
		} finally {
			setLoading(false);
		}
	}, [page, pageSize, searchTitle, selectedCategory, showError]);

	const loadQuizSessions = useCallback(async (quizId: string) => {
		const sessionData = await getQuizSessions({ quizId, page: 1, pageSize: 50 });
		setSessions(Array.isArray(sessionData.items) ? sessionData.items : []);
	}, []);

	const handleSelectQuiz = async (quizId?: string) => {
		if (!quizId) {
			return;
		}

		setSelectedQuizId(quizId);
		setDetailsLoading(true);
		setIsLoadingSessions(true);
		try {
			const detailsPromise = getQuiz(quizId);
			const sessionPromise = getQuizSessions({ quizId, page: 1, pageSize: 50 });
			const [details, sessionData] = await Promise.all([
				detailsPromise,
				sessionPromise,
			]);
			setSelectedQuiz(details);
			setSessions(Array.isArray(sessionData.items) ? sessionData.items : []);
		} catch {
			showError("Could not load quiz details.");
			setSelectedQuiz(null);
			setSessions([]);
		} finally {
			setIsLoadingSessions(false);
			setDetailsLoading(false);
		}
	};

	const handleSessionAction = async (sessionId: string, action: "start" | "pause" | "resume" | "finish") => {
		if (!selectedQuizId) {
			return;
		}

		const actionKey = `${sessionId}:${action}`;
		setSessionActionKey(actionKey);

		try {
			if (action === "start") {
				const res = await startQuizSession(sessionId);
				showSuccess(res.message || "Session started successfully.");
			} else if (action === "pause") {
				const res = await pauseQuizSession(sessionId);
				showSuccess(res.message || "Session paused successfully.");
			} else if (action === "resume") {
				const res = await resumeQuizSession(sessionId);
				showSuccess(res.message || "Session resumed successfully.");
			} else {
				const res = await finishQuizSession(sessionId);
				showSuccess(res.message || "Session finished successfully.");
			}

			await loadQuizSessions(selectedQuizId);
		} catch {
			showError("Can not update session status right now.");
		} finally {
			setSessionActionKey("");
		}
	};

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

	useEffect(() => {
		const timer = window.setTimeout(() => {
			setSearchTitle(searchInput.trim());
			setPage(1);
		}, 400);

		return () => {
			window.clearTimeout(timer);
		};
	}, [searchInput]);

	useEffect(() => {
		let isMounted = true;

		const fetchCategories = async () => {
			try {
				setIsLoadingCategories(true);
				const data = await getAllCategories();
				if (isMounted) {
					setCategories(Array.isArray(data) ? data : []);
				}
			} catch {
				if (isMounted) {
					setCategories([]);
				}
			} finally {
				if (isMounted) {
					setIsLoadingCategories(false);
				}
			}
		};

		void fetchCategories();

		return () => {
			isMounted = false;
		};
	}, []);

	useEffect(() => {
		setSelectedQuizId("");
		setSelectedQuiz(null);
		setSessions([]);
		void fetchQuizzes();
	}, [fetchQuizzes]);

	return (
		<Box>
			<Typography variant="h5" fontWeight={700} mb={2}>
				Quiz Management
			</Typography>

			<Box
				sx={{
					border: "1px solid",
					borderColor: "divider",
					borderRadius: 1,
					p: 2,
				}}
			>
				<Stack direction={{ xs: "column", md: "row" }} spacing={2} mb={2}>
					<TextField
						label="Search by title"
						value={searchInput}
						onChange={(event) => setSearchInput(event.target.value)}
						fullWidth
					/>

					<FormControl fullWidth sx={{ maxWidth: { xs: "100%", md: 320 } }}>
						<InputLabel id="admin-quiz-category-filter-label">Category</InputLabel>
						<Select
							labelId="admin-quiz-category-filter-label"
							label="Category"
							value={selectedCategory}
							onChange={(event) => {
								setSelectedCategory(String(event.target.value));
								setPage(1);
							}}
							disabled={isLoadingCategories}
						>
							<MenuItem value="">All Categories</MenuItem>
							{categories.map((category) => (
								<MenuItem key={category.id} value={category.id}>
									{category.name}
								</MenuItem>
							))}
						</Select>
					</FormControl>
				</Stack>

				<Stack direction={{ xs: "column", lg: "row" }} spacing={2} alignItems="stretch">
					<Stack spacing={1} sx={{ flex: 1, minWidth: 0 }}>
						<Paper variant="outlined" sx={{ boxShadow: "none", minHeight: 420 }}>
							{loading ? (
								<Box py={6} display="flex" justifyContent="center">
									<CircularProgress />
								</Box>
							) : (
								<Table>
									<TableHead>
										<TableRow>
											<TableCell>ID</TableCell>
											<TableCell>Title</TableCell>
											<TableCell>Status</TableCell>
										</TableRow>
									</TableHead>
									<TableBody>
										{quizzes.map((quiz) => {
											const isSelected = selectedQuizId === quiz.id;

											return (
												<TableRow
													key={quiz.id}
													hover
													onClick={() => handleSelectQuiz(quiz.id)}
													sx={{
														cursor: "pointer",
														backgroundColor: isSelected ? "action.selected" : undefined,
													}}
												>
													<TableCell>{quiz.id}</TableCell>
													<TableCell>{quiz.title}</TableCell>
													<TableCell>
														<Chip
															size="small"
															label={statusLabel[quiz.status] ?? "Unknown"}
															color={statusColor[quiz.status] ?? "default"}
														/>
													</TableCell>
												</TableRow>
											);
										})}
										{quizzes.length === 0 && (
											<TableRow>
												<TableCell colSpan={3} align="center">
													No quizzes found.
												</TableCell>
											</TableRow>
										)}
									</TableBody>
								</Table>
							)}
						</Paper>

						<Stack
							direction={{ xs: "column", sm: "row" }}
							justifyContent="space-between"
							alignItems={{ xs: "flex-start", sm: "center" }}
							spacing={1}
						>
							<Typography variant="body2" color="text.secondary">
								Total quizzes: {totalCount}
							</Typography>
							<Pagination
								page={page}
								count={Math.max(totalPages, 1)}
								onChange={(_, value) => setPage(value)}
								color="primary"
								shape="rounded"
							/>
						</Stack>
					</Stack>

					<Stack sx={{ flex: 1, minWidth: 0 }}>
						<Paper
							variant="outlined"
							sx={{
								boxShadow: "none",
								p: 2,
							}}
						>
					<Typography variant="h6" fontWeight={700} mb={1.5}>
						Quiz Details
					</Typography>

					{detailsLoading ? (
						<Stack direction="row" justifyContent="center" py={2}>
							<CircularProgress size={24} />
						</Stack>
					) : selectedQuiz ? (
						<Stack spacing={2}>
							<Stack spacing={0.5}>
								<Typography variant="body2">
									<strong>Title:</strong> {selectedQuiz.title}
								</Typography>
								<Typography variant="body2">
									<strong>Description:</strong> {selectedQuiz.description || "-"}
								</Typography>
								<Typography variant="body2">
									<strong>Questions:</strong> {selectedQuiz.questions.length}
								</Typography>
								<Typography variant="body2">
									<strong>Total Points:</strong> {selectedQuiz.totalPoints ?? 0}
								</Typography>
							</Stack>

							<Divider />

							<Stack
								direction={{ xs: "column", sm: "row" }}
								justifyContent="space-between"
								alignItems={{ xs: "flex-start", sm: "center" }}
							>
								<Typography variant="subtitle1" fontWeight={700}>
									Sessions
								</Typography>
								<Typography variant="body2" color="text.secondary">
									Total: {sessions.length}
								</Typography>
							</Stack>

							{isLoadingSessions ? (
								<Stack direction="row" justifyContent="center" py={2}>
									<CircularProgress size={22} />
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
										const isActionLoading = sessionActionKey.startsWith(`${session.id}:`);
										const canStart = session.status === SessionStatusValue.Waiting;
										const canPause = session.status === SessionStatusValue.Active;
										const canResume = session.status === SessionStatusValue.Paused;
										const canFinish =
											session.status === SessionStatusValue.Active ||
											session.status === SessionStatusValue.Paused;

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

													<Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
														{canStart ? (
															<Button
																size="small"
																variant="contained"
																onClick={() => {
																	void handleSessionAction(session.id, "start");
																}}
																disabled={isActionLoading}
															>
																Start
															</Button>
														) : null}
														{canPause ? (
															<Button
																size="small"
																variant="outlined"
																onClick={() => {
																	void handleSessionAction(session.id, "pause");
																}}
																disabled={isActionLoading}
															>
																Pause
															</Button>
														) : null}
														{canResume ? (
															<Button
																size="small"
																variant="outlined"
																onClick={() => {
																	void handleSessionAction(session.id, "resume");
																}}
																disabled={isActionLoading}
															>
																Resume
															</Button>
														) : null}
														{canFinish ? (
															<Button
																size="small"
																variant="outlined"
																color="error"
																onClick={() => {
																	void handleSessionAction(session.id, "finish");
																}}
																disabled={isActionLoading}
															>
																Finish
															</Button>
														) : null}
													</Stack>

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
												</Stack>
											</Box>
										);
									})}
								</Stack>
							) : null}
						</Stack>
					) : (
						<Typography variant="body2" color="text.secondary">
							Click a quiz row to load details and sessions.
						</Typography>
					)}
						</Paper>
					</Stack>
				</Stack>
			</Box>

			<AppSnackbar
				open={snackbar.open}
				message={snackbar.message}
				severity={snackbar.severity}
				onClose={closeSnackbar}
			/>
		</Box>
	);
};

export default QuizPage;
