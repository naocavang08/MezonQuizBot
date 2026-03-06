import { useEffect, useMemo, useState } from "react";
import {
	Alert,
	Box,
	Button,
	Card,
	CardContent,
	CircularProgress,
	IconButton,
	Stack,
	Switch,
	TextField,
	Typography,
} from "@mui/material";
import { MdDelete } from "react-icons/md";
import { useNavigate, useParams } from "react-router-dom";
import {
	deleteQuestion,
	deleteQuiz,
	getQuizDetails,
	updateQuestion,
	updateQuiz,
	updateQuizSettings,
} from "../../Api/quiz.api";
import {
	QuizStatus,
	type QuizDto,
	type QuizQuestionDto,
} from "../../Interface/Quiz.dto";

const statusLabel: Record<QuizDto["status"], string> = {
	[QuizStatus.Draft]: "Draft",
	[QuizStatus.Published]: "Published",
	[QuizStatus.Archived]: "Archived",
};

const QuizSettingPage = () => {
	const navigate = useNavigate();
	const { quizId } = useParams<{ quizId: string }>();

	const [quiz, setQuiz] = useState<QuizDto | null>(null);
	const [isLoading, setIsLoading] = useState(true);
	const [isSavingSettings, setIsSavingSettings] = useState(false);
	const [isUpdatingStatus, setIsUpdatingStatus] = useState(false);
	const [isDeletingQuiz, setIsDeletingQuiz] = useState(false);
	const [error, setError] = useState<string | null>(null);
	const [success, setSuccess] = useState<string | null>(null);

	const totalPoints = useMemo(() => {
		if (!quiz) {
			return 0;
		}
		return quiz.questions.reduce((sum, question) => sum + Number(question.points || 0), 0);
	}, [quiz]);

	useEffect(() => {
		let isMounted = true;

		const loadQuiz = async () => {
			if (!quizId) {
				setError("Quiz id is missing.");
				setIsLoading(false);
				return;
			}

			try {
				setIsLoading(true);
				setError(null);
				const data = await getQuizDetails(quizId);
				if (isMounted) {
					setQuiz(data);
				}
			} catch {
				if (isMounted) {
					setError("Could not load quiz details.");
				}
			} finally {
				if (isMounted) {
					setIsLoading(false);
				}
			}
		};

		loadQuiz();

		return () => {
			isMounted = false;
		};
	}, [quizId]);

	const setQuestionField = <K extends keyof QuizQuestionDto>(
		questionIndex: number,
		field: K,
		value: QuizQuestionDto[K]
	) => {
		setQuiz((prev) => {
			if (!prev) {
				return prev;
			}

			const questions = [...prev.questions];
			questions[questionIndex] = {
				...questions[questionIndex],
				[field]: value,
			};

			return { ...prev, questions };
		});
	};

	const saveSettings = async () => {
		if (!quiz || !quizId) {
			return;
		}

		try {
			setSuccess(null);
			setError(null);
			setIsSavingSettings(true);
			const result = await updateQuizSettings(quizId, quiz.settings);
			setSuccess(result.message || "Quiz settings updated.");
		} catch {
			setError("Failed to update quiz settings.");
		} finally {
			setIsSavingSettings(false);
		}
	};

	const updateStatus = async (status: QuizDto["status"]) => {
		if (!quiz || !quizId) {
			return;
		}

		try {
			setSuccess(null);
			setError(null);
			setIsUpdatingStatus(true);

			const payload: QuizDto = {
				...quiz,
				status,
			};

			const result = await updateQuiz(quizId, payload);
			setQuiz((prev) => (prev ? { ...prev, status } : prev));
			setSuccess(result.message || "Quiz status updated.");
		} catch {
			setError("Failed to update quiz status.");
		} finally {
			setIsUpdatingStatus(false);
		}
	};

	const removeQuiz = async () => {
		if (!quizId) {
			return;
		}

		const confirm = window.confirm("Are you sure you want to remove this quiz?");
		if (!confirm) {
			return;
		}

		try {
			setSuccess(null);
			setError(null);
			setIsDeletingQuiz(true);
			const result = await deleteQuiz(quizId);
			setSuccess(result.message || "Quiz removed.");
			navigate("/user/my-quizzes", { replace: true });
		} catch {
			setError("Failed to remove quiz.");
		} finally {
			setIsDeletingQuiz(false);
		}
	};

	const saveQuestion = async (questionIndex: number) => {
		if (!quiz || !quizId) {
			return;
		}

		const question = quiz.questions[questionIndex];
		if (!question?.content.trim()) {
			setError(`Question ${questionIndex + 1}: content is required.`);
			return;
		}

		try {
			setSuccess(null);
			setError(null);

			const payload: QuizQuestionDto = {
				...question,
				content: question.content.trim(),
			};

			const result = await updateQuestion(quizId, questionIndex + 1, payload);
			setSuccess(result.message || `Question ${questionIndex + 1} updated.`);
		} catch {
			setError(`Failed to update question ${questionIndex + 1}.`);
		}
	};

	const removeQuestionByIndex = async (questionIndex: number) => {
		if (!quiz || !quizId) {
			return;
		}

		if (quiz.questions.length <= 1) {
			setError("Quiz must have at least one question.");
			return;
		}

		const confirm = window.confirm(`Remove question ${questionIndex + 1}?`);
		if (!confirm) {
			return;
		}

		try {
			setSuccess(null);
			setError(null);
			const result = await deleteQuestion(quizId, questionIndex + 1);

			setQuiz((prev) => {
				if (!prev) {
					return prev;
				}
				const questions = prev.questions
					.filter((_, idx) => idx !== questionIndex)
					.map((item, idx) => ({ ...item, index: idx + 1 }));

				return { ...prev, questions };
			});

			setSuccess(result.message || `Question ${questionIndex + 1} removed.`);
		} catch {
			setError(`Failed to remove question ${questionIndex + 1}.`);
		}
	};

	if (isLoading) {
		return (
			<Stack direction="row" justifyContent="center" sx={{ py: 8 }}>
				<CircularProgress />
			</Stack>
		);
	}

	if (!quiz) {
		return <Alert severity="error">{error || "Quiz not found."}</Alert>;
	}

	return (
		<Box sx={{ mt: 2 }}>
			<Typography variant="h4" fontWeight={700} mb={2}>
				Quiz Setting - {quiz.title}
			</Typography>

			{error ? <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert> : null}
			{success ? <Alert severity="success" sx={{ mb: 2 }}>{success}</Alert> : null}

			<Stack direction={{ xs: "column", lg: "row" }} spacing={2} alignItems="stretch">
				<Card variant="outlined" sx={{ flex: 1, minWidth: 320, backgroundColor: "transparent" }}>
					<CardContent>
						<Stack spacing={2}>
							<Typography variant="h6" fontWeight={700}>P1 - Quiz Setting</Typography>

							<Stack direction={{ xs: "column", sm: "row" }} spacing={1} flexWrap="wrap" useFlexGap>
								<Button
									variant="contained"
									onClick={() => updateStatus(QuizStatus.Published)}
									disabled={isUpdatingStatus || isDeletingQuiz}
								>
									Publish
								</Button>
								<Button
									variant="outlined"
									color="warning"
									onClick={() => updateStatus(QuizStatus.Archived)}
									disabled={isUpdatingStatus || isDeletingQuiz}
								>
									Archive
								</Button>
								<Button
									variant="outlined"
									color="error"
									onClick={removeQuiz}
									disabled={isUpdatingStatus || isDeletingQuiz}
								>
									Remove
								</Button>
								<Button variant="text" onClick={() => navigate("/user/my-quizzes")}>Go back</Button>
							</Stack>

							<Typography variant="body2" color="text.secondary">
								Current status: {statusLabel[quiz.status]}
							</Typography>

							<TextField
								label="Max Attempts"
								type="number"
								inputProps={{ min: 1, max: 5 }}
								value={quiz.settings.maxAttempts}
								onChange={(event) =>
									setQuiz((prev) =>
										prev
											? {
													...prev,
													settings: {
														...prev.settings,
														maxAttempts: Number(event.target.value || 1),
													},
												}
											: prev
									)
								}
							/>

							<Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
								<Stack direction="row" spacing={1} alignItems="center">
									<Switch
										checked={quiz.settings.shuffleQuestions}
										onChange={(event) =>
											setQuiz((prev) =>
												prev
													? {
															...prev,
															settings: {
																...prev.settings,
																shuffleQuestions: event.target.checked,
															},
														}
													: prev
											)
										}
									/>
									<Typography variant="body2">Shuffle Questions</Typography>
								</Stack>

								<Stack direction="row" spacing={1} alignItems="center">
									<Switch
										checked={quiz.settings.shuffleOptions}
										onChange={(event) =>
											setQuiz((prev) =>
												prev
													? {
															...prev,
															settings: {
																...prev.settings,
																shuffleOptions: event.target.checked,
															},
														}
													: prev
											)
										}
									/>
									<Typography variant="body2">Shuffle Options</Typography>
								</Stack>

								<Stack direction="row" spacing={1} alignItems="center">
									<Switch
										checked={quiz.settings.showCorrectAnswer}
										onChange={(event) =>
											setQuiz((prev) =>
												prev
													? {
															...prev,
															settings: {
																...prev.settings,
																showCorrectAnswer: event.target.checked,
															},
														}
													: prev
											)
										}
									/>
									<Typography variant="body2">Show Correct Answer</Typography>
								</Stack>
							</Stack>

							<Button variant="outlined" onClick={saveSettings} disabled={isSavingSettings}>
								{isSavingSettings ? "Saving..." : "Save Quiz Setting"}
							</Button>

							<Typography variant="body2" color="text.secondary">
								Total points: {totalPoints}
							</Typography>
						</Stack>
					</CardContent>
				</Card>

				<Card variant="outlined" sx={{ flex: 2, minWidth: 320, backgroundColor: "transparent" }}>
					<CardContent>
						<Stack spacing={2}>
							<Typography variant="h6" fontWeight={700}>P2 - Questions</Typography>

							{quiz.questions.length === 0 ? (
								<Typography variant="body2" color="text.secondary">No questions in this quiz.</Typography>
							) : (
								<Stack spacing={2}>
									{quiz.questions.map((question, questionIndex) => (
										<Card key={`question-${question.index}`} variant="outlined" sx={{ backgroundColor: "transparent" }}>
											<CardContent>
												<Stack spacing={1.5}>
													<Stack direction="row" justifyContent="space-between" alignItems="center">
														<Typography variant="subtitle1" fontWeight={600}>
															Question {questionIndex + 1}
														</Typography>

														<IconButton color="error" onClick={() => removeQuestionByIndex(questionIndex)}>
															<MdDelete />
														</IconButton>
													</Stack>

													<TextField
														label="Question Content"
														fullWidth
														value={question.content}
														onChange={(event) =>
															setQuestionField(questionIndex, "content", event.target.value)
														}
													/>

													<TextField
														label="Media URL"
														fullWidth
														value={question.mediaUrl ?? ""}
														onChange={(event) =>
															setQuestionField(questionIndex, "mediaUrl", event.target.value)
														}
													/>

													<Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
														<TextField
															label="Time Limit (s)"
															type="number"
															inputProps={{ min: 10, max: 30 }}
															value={question.timeLimitSeconds}
															onChange={(event) =>
																setQuestionField(
																	questionIndex,
																	"timeLimitSeconds",
																	Number(event.target.value || 10)
																)
															}
														/>

														<TextField
															label="Points"
															type="number"
															inputProps={{ min: 1, max: 20 }}
															value={question.points}
															onChange={(event) =>
																setQuestionField(
																	questionIndex,
																	"points",
																	Number(event.target.value || 1)
																)
															}
														/>

														<Button variant="outlined" onClick={() => saveQuestion(questionIndex)}>
															Save
														</Button>
													</Stack>
												</Stack>
											</CardContent>
										</Card>
									))}
								</Stack>
							)}
						</Stack>
					</CardContent>
				</Card>
			</Stack>
		</Box>
	);
};

export default QuizSettingPage;
