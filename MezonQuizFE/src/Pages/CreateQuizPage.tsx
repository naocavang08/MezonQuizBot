import { useEffect, useMemo, useState } from "react";
import {
    Box,
    Button,
    Card,
    CardContent,
    Divider,
    FormControl,
    FormControlLabel,
    FormLabel,
    IconButton,
    MenuItem,
    Radio,
    RadioGroup,
    Select,
    Stack,
    Switch,
    TextField,
    Typography,
} from "@mui/material";
import AppSnackbar from "../Components/AppSnackbar";
import useAppSnackbar from "../Hooks/useAppSnackbar";
import { MdAdd, MdDelete } from "react-icons/md";
import { useNavigate } from "react-router-dom";
import { getAllCategories } from "../Api/category.api";
import { createQuiz } from "../Api/quiz.api";
import type { CategoryDto } from "../Interface/category.dto";
import {
    QuestionType,
    QuizStatus,
    QuizVisibility,
    type QuizDto,
    type QuizOptionDto,
    type QuizQuestionDto,
} from "../Interface/quiz.dto";

type FormState = {
    title: string;
    description: string;
	categoryId: string;
    visibility: QuizDto["visibility"];
    status: QuizDto["status"];
    settings: QuizDto["settings"];
    questions: QuizQuestionDto[];
};

const makeDefaultOptions = (type: QuizQuestionDto["questionType"]): QuizOptionDto[] => {
    if (type === QuestionType.TrueFalse) {
        return [
            { index: 1, content: "True", isCorrect: true },
            { index: 2, content: "False", isCorrect: false },
        ];
    }

    return [
        { index: 1, content: "", isCorrect: true },
        { index: 2, content: "", isCorrect: false },
    ];
};

const makeDefaultQuestion = (index: number): QuizQuestionDto => ({
    index,
    content: "",
    mediaUrl: "",
    timeLimitSeconds: 30,
    points: 10,
    questionType: QuestionType.SingleChoice,
    options: makeDefaultOptions(QuestionType.SingleChoice),
});

const visibilityLabel: Record<QuizDto["visibility"], string> = {
    [QuizVisibility.Private]: "Private",
    [QuizVisibility.Public]: "Public",
    [QuizVisibility.Unlisted]: "Unlisted",
};

const statusLabel: Record<QuizDto["status"], string> = {
    [QuizStatus.Draft]: "Draft",
    [QuizStatus.Published]: "Published",
    [QuizStatus.Archived]: "Archived",
};

const questionTypeLabel: Record<QuizQuestionDto["questionType"], string> = {
    [QuestionType.SingleChoice]: "Single Choice",
    [QuestionType.MultipleChoice]: "Multiple Choice",
    [QuestionType.TrueFalse]: "True/False",
};

const CreateQuizPage = () => {
	const navigate = useNavigate();
	const [categories, setCategories] = useState<CategoryDto[]>([]);
	const [isLoadingCategories, setIsLoadingCategories] = useState(false);
	const [isSubmitting, setIsSubmitting] = useState(false);
	const { snackbar, showError, showSuccess, closeSnackbar } = useAppSnackbar();
	const [form, setForm] = useState<FormState>({
		title: "",
		description: "",
		categoryId: "",
		visibility: QuizVisibility.Private,
		status: QuizStatus.Draft,
		settings: {
			shuffleQuestions: false,
			shuffleOptions: false,
			showCorrectAnswer: true,
			maxAttempts: 1,
		},
		questions: [makeDefaultQuestion(1)],
	});

	useEffect(() => {
		let isMounted = true;

		const loadCategories = async () => {
			try {
				setIsLoadingCategories(true);
				const data = await getAllCategories();
				if (isMounted) {
					setCategories(data);
				}
			} catch {
				if (isMounted) {
					showError("Failed to load categories.");
				}
			} finally {
				if (isMounted) {
					setIsLoadingCategories(false);
				}
			}
		};

		loadCategories();

		return () => {
			isMounted = false;
		};
	}, [showError]);

	const totalPoints = useMemo(
		() => form.questions.reduce((sum, question) => sum + Number(question.points || 0), 0),
		[form.questions]
	);

	const setQuestionField = <K extends keyof QuizQuestionDto>(
		questionIndex: number,
		field: K,
		value: QuizQuestionDto[K]
	) => {
		setForm((prev) => {
			const questions = [...prev.questions];
			questions[questionIndex] = {
				...questions[questionIndex],
				[field]: value,
			};
			return { ...prev, questions };
		});
	};

	const setOptionField = <K extends keyof QuizOptionDto>(
		questionIndex: number,
		optionIndex: number,
		field: K,
		value: QuizOptionDto[K]
	) => {
		setForm((prev) => {
			const questions = [...prev.questions];
			const options = [...questions[questionIndex].options];
			options[optionIndex] = {
				...options[optionIndex],
				[field]: value,
			};
			questions[questionIndex] = {
				...questions[questionIndex],
				options,
			};
			return { ...prev, questions };
		});
	};

	const addQuestion = () => {
		setForm((prev) => ({
			...prev,
			questions: [...prev.questions, makeDefaultQuestion(prev.questions.length + 1)],
		}));
	};

	const removeQuestion = (questionIndex: number) => {
		setForm((prev) => {
			if (prev.questions.length <= 1) {
				return prev;
			}

			const questions = prev.questions.filter((_, index) => index !== questionIndex);
			return {
				...prev,
				questions: questions.map((question, index) => ({
					...question,
					index: index + 1,
					options: question.options.map((option, optionIdx) => ({
						...option,
						index: optionIdx + 1,
					})),
				})),
			};
		});
	};

	const handleQuestionTypeChange = (questionIndex: number, type: QuizQuestionDto["questionType"]) => {
		setForm((prev) => {
			const questions = [...prev.questions];
			const currentQuestion = questions[questionIndex];
			const normalizedOptions =
				type === QuestionType.TrueFalse
					? makeDefaultOptions(QuestionType.TrueFalse)
					: currentQuestion.options.length >= 2
						? currentQuestion.options.map((option, idx) => ({
							...option,
							index: idx + 1,
							isCorrect:
								type === QuestionType.SingleChoice
									? idx === 0
										? true
										: false
									: option.isCorrect,
						}))
						: makeDefaultOptions(type);

			questions[questionIndex] = {
				...currentQuestion,
				questionType: type,
				options: normalizedOptions,
			};

			return { ...prev, questions };
		});
	};

	const addOption = (questionIndex: number) => {
		setForm((prev) => {
			const questions = [...prev.questions];
			const question = questions[questionIndex];
			if (question.questionType === QuestionType.TrueFalse) {
				return prev;
			}

			const options = [
				...question.options,
				{ index: question.options.length + 1, content: "", isCorrect: false },
			];

			questions[questionIndex] = { ...question, options };
			return { ...prev, questions };
		});
	};

	const removeOption = (questionIndex: number, optionIndex: number) => {
		setForm((prev) => {
			const questions = [...prev.questions];
			const question = questions[questionIndex];
			if (question.options.length <= 2 || question.questionType === QuestionType.TrueFalse) {
				return prev;
			}

			const options = question.options
				.filter((_, idx) => idx !== optionIndex)
				.map((option, idx) => ({ ...option, index: idx + 1 }));

			if (question.questionType === QuestionType.SingleChoice && !options.some((option) => option.isCorrect)) {
				options[0] = { ...options[0], isCorrect: true };
			}

			questions[questionIndex] = { ...question, options };
			return { ...prev, questions };
		});
	};

	const setCorrectOption = (questionIndex: number, optionIndex: number, checked: boolean) => {
		setForm((prev) => {
			const questions = [...prev.questions];
			const question = questions[questionIndex];

			let options = [...question.options];

			if (question.questionType === QuestionType.SingleChoice || question.questionType === QuestionType.TrueFalse) {
				options = options.map((option, idx) => ({
					...option,
					isCorrect: idx === optionIndex,
				}));
			} else {
				options[optionIndex] = {
					...options[optionIndex],
					isCorrect: checked,
				};
			}

			questions[questionIndex] = { ...question, options };
			return { ...prev, questions };
		});
	};

	const validateBeforeSubmit = (): string | null => {
		if (!form.title.trim()) {
			return "Quiz title is required.";
		}

		if (form.questions.length === 0) {
			return "At least one question is required.";
		}

		for (let qIndex = 0; qIndex < form.questions.length; qIndex += 1) {
			const question = form.questions[qIndex];
			if (!question.content.trim()) {
				return `Question ${qIndex + 1}: content is required.`;
			}

			if (question.timeLimitSeconds < 10 || question.timeLimitSeconds > 30) {
				return `Question ${qIndex + 1}: time limit must be between 10 and 30 seconds.`;
			}

			if (question.points < 1 || question.points > 20) {
				return `Question ${qIndex + 1}: points must be between 1 and 20.`;
			}

			if (question.options.length < 2) {
				return `Question ${qIndex + 1}: at least 2 options are required.`;
			}

			if (question.options.some((option) => !option.content.trim())) {
				return `Question ${qIndex + 1}: option content cannot be empty.`;
			}

			const correctCount = question.options.filter((option) => option.isCorrect).length;
			if (question.questionType === QuestionType.SingleChoice && correctCount !== 1) {
				return `Question ${qIndex + 1}: single choice must have exactly 1 correct option.`;
			}

			if (question.questionType === QuestionType.MultipleChoice && correctCount < 2) {
				return `Question ${qIndex + 1}: multiple choice must have at least 2 correct options.`;
			}

			if (
				question.questionType === QuestionType.TrueFalse
				&& (question.options.length !== 2 || correctCount !== 1)
			) {
				return `Question ${qIndex + 1}: true/false requires exactly 2 options with 1 correct answer.`;
			}
		}

		if (form.settings.maxAttempts < 1 || form.settings.maxAttempts > 5) {
			return "Max attempts must be between 1 and 5.";
		}

		return null;
	};

	const onSubmit = async () => {
		const validationError = validateBeforeSubmit();
		if (validationError) {
			showError(validationError);
			return;
		}

		const payload: QuizDto = {
			title: form.title.trim(),
			description: form.description.trim() || undefined,
			categoryId: form.categoryId || undefined,
			visibility: form.visibility,
			status: form.status,
			settings: form.settings,
			questions: form.questions.map((question, questionIndex) => ({
				...question,
				index: questionIndex + 1,
				content: question.content.trim(),
				mediaUrl: question.mediaUrl?.trim() || undefined,
				options: question.options.map((option, optionIndex) => ({
					...option,
					index: optionIndex + 1,
					content: option.content.trim(),
				})),
			})),
		};

		try {
			setIsSubmitting(true);
			const result = await createQuiz(payload);
			showSuccess(result.message || "Quiz created successfully.");

			setTimeout(() => {
				navigate("/app/my-quizzes", { replace: true });
			}, 800);
		} catch {
			showError("Failed to create quiz. Please check your data and try again.");
		} finally {
			setIsSubmitting(false);
		}
	};

	return (
		<Box sx={{ mt: 2 }}>
			<Stack direction={{ xs: "column", sm: "row" }} justifyContent="space-between" spacing={2} mb={3}>
				<Typography variant="h4" fontWeight={700} mb={1}>
				Create Quiz
				</Typography>
				<Button variant="outlined" onClick={() => navigate("/app/my-quizzes")}>
					Back to My Quizzes
				</Button>
			</Stack>

			<Card variant="outlined" sx={{ mb: 3, backgroundColor: "transparent" }}>
				<CardContent>
					<Stack spacing={2}>
						<TextField
							label="Quiz Title"
							fullWidth
							required
							value={form.title}
							onChange={(event) => setForm((prev) => ({ ...prev, title: event.target.value }))}
						/>

						<TextField
							label="Description"
							fullWidth
							multiline
							minRows={2}
							value={form.description}
							onChange={(event) => setForm((prev) => ({ ...prev, description: event.target.value }))}
						/>

						<FormControl fullWidth>
							<FormLabel>Category</FormLabel>
							<Select
								value={form.categoryId}
								onChange={(event) =>
									setForm((prev) => ({ ...prev, categoryId: String(event.target.value) }))
								}
								disabled={isLoadingCategories}
							>
								<MenuItem value="">No Category</MenuItem>
								{categories.map((category) => (
									<MenuItem key={category.id} value={category.id}>
										{category.name}
									</MenuItem>
								))}
							</Select>
						</FormControl>

						<Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
							<FormControl fullWidth>
								<FormLabel>Visibility</FormLabel>
								<Select
									value={form.visibility}
									onChange={(event) =>
										setForm((prev) => ({ ...prev, visibility: Number(event.target.value) as QuizDto["visibility"] }))
									}
								>
									{Object.values(QuizVisibility).map((value) => (
										<MenuItem key={value} value={value}>
											{visibilityLabel[value as QuizDto["visibility"]]}
										</MenuItem>
									))}
								</Select>
							</FormControl>

							<FormControl fullWidth>
								<FormLabel>Status</FormLabel>
								<Select
									value={form.status}
									onChange={(event) =>
										setForm((prev) => ({ ...prev, status: Number(event.target.value) as QuizDto["status"] }))
									}
								>
									{Object.values(QuizStatus).map((value) => (
										<MenuItem key={value} value={value}>
											{statusLabel[value as QuizDto["status"]]}
										</MenuItem>
									))}
								</Select>
							</FormControl>
						</Stack>

						<Divider />

						<Typography variant="h6" fontWeight={600}>Quiz Settings</Typography>
						<Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
							<FormControlLabel
								control={<Switch checked={form.settings.shuffleQuestions} onChange={(event) => setForm((prev) => ({ ...prev, settings: { ...prev.settings, shuffleQuestions: event.target.checked } }))} />}
								label="Shuffle Questions"
							/>
							<FormControlLabel
								control={<Switch checked={form.settings.shuffleOptions} onChange={(event) => setForm((prev) => ({ ...prev, settings: { ...prev.settings, shuffleOptions: event.target.checked } }))} />}
								label="Shuffle Options"
							/>
							<FormControlLabel
								control={<Switch checked={form.settings.showCorrectAnswer} onChange={(event) => setForm((prev) => ({ ...prev, settings: { ...prev.settings, showCorrectAnswer: event.target.checked } }))} />}
								label="Show Correct Answer"
							/>
						</Stack>

						<TextField
							type="number"
							label="Max Attempts"
							inputProps={{ min: 1, max: 5 }}
							value={form.settings.maxAttempts}
							onChange={(event) =>
								setForm((prev) => ({
									...prev,
									settings: {
										...prev.settings,
										maxAttempts: Number(event.target.value || 1),
									},
								}))
							}
						/>
					</Stack>
				</CardContent>
			</Card>

			<Stack spacing={2} mb={3}>
				<Stack direction="row" justifyContent="space-between" alignItems="center">
					<Typography variant="h5" fontWeight={700}>Questions</Typography>
				</Stack>

				{form.questions.map((question, questionIndex) => (
					<Stack key={`question-${questionIndex}`} spacing={1.5}>
						<Card variant="outlined" sx={{ backgroundColor: "transparent" }}>
							<CardContent>
								<Stack spacing={2}>
									<Stack direction="row" justifyContent="space-between" alignItems="center">
										<Typography variant="h6" fontWeight={600}>
											Question {questionIndex + 1}
										</Typography>
										<IconButton
											color="error"
											onClick={() => removeQuestion(questionIndex)}
											disabled={form.questions.length === 1}
										>
											<MdDelete />
										</IconButton>
									</Stack>

									<TextField
										label="Question Content"
										fullWidth
										required
										value={question.content}
										onChange={(event) => setQuestionField(questionIndex, "content", event.target.value)}
									/>

									<TextField
										label="Media URL (optional)"
										fullWidth
										value={question.mediaUrl ?? ""}
										onChange={(event) => setQuestionField(questionIndex, "mediaUrl", event.target.value)}
									/>

									<Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
										<TextField
											type="number"
											label="Time Limit (10-30s)"
											inputProps={{ min: 10, max: 30 }}
											value={question.timeLimitSeconds}
											onChange={(event) => setQuestionField(questionIndex, "timeLimitSeconds", Number(event.target.value || 10))}
										/>
										<TextField
											type="number"
											label="Points (1-20)"
											inputProps={{ min: 1, max: 20 }}
											value={question.points}
											onChange={(event) => setQuestionField(questionIndex, "points", Number(event.target.value || 1))}
										/>
										<FormControl fullWidth>
											<FormLabel>Question Type</FormLabel>
											<Select
												value={question.questionType}
												onChange={(event) =>
													handleQuestionTypeChange(
														questionIndex,
														Number(event.target.value) as QuizQuestionDto["questionType"]
													)
												}
											>
												{Object.values(QuestionType).map((value) => (
													<MenuItem key={value} value={value}>
														{questionTypeLabel[value as QuizQuestionDto["questionType"]]}
													</MenuItem>
												))}
											</Select>
										</FormControl>
									</Stack>

									<Divider />
									<Stack direction="row" justifyContent="space-between" alignItems="center">
										<Typography variant="subtitle1" fontWeight={600}>Options</Typography>
										<Button
											startIcon={<MdAdd />}
											variant="text"
											onClick={() => addOption(questionIndex)}
											disabled={question.questionType === QuestionType.TrueFalse}
										>
											Add Option
										</Button>
									</Stack>

									{question.questionType === QuestionType.SingleChoice || question.questionType === QuestionType.TrueFalse ? (
										<RadioGroup
											value={question.options.findIndex((option) => option.isCorrect)}
											onChange={(event) => setCorrectOption(questionIndex, Number(event.target.value), true)}
										>
											<Stack spacing={1}>
												{question.options.map((option, optionIndex) => (
                                                <Stack key={`option-${questionIndex}-${optionIndex}`} direction="row" spacing={1} alignItems="center">
                                                    <FormControlLabel value={optionIndex} control={<Radio />} label="" sx={{ mr: 0 }} />
                                                    <TextField
                                                        fullWidth
                                                        label={`Option ${optionIndex + 1}`}
                                                        value={option.content}
                                                        onChange={(event) => setOptionField(questionIndex, optionIndex, "content", event.target.value)}
                                                        disabled={question.questionType === QuestionType.TrueFalse}
                                                    />
                                                    {question.questionType === QuestionType.SingleChoice && (
                                                        <IconButton
                                                            color="error"
                                                            onClick={() => removeOption(questionIndex, optionIndex)}
                                                            disabled={question.options.length <= 2}
                                                        >
                                                            <MdDelete />
                                                        </IconButton>
                                                    )}
                                                </Stack>
                                            ))}
											</Stack>
										</RadioGroup>
									) : (
										<Stack spacing={1}>
											{question.options.map((option, optionIndex) => (
												<Stack key={`option-${questionIndex}-${optionIndex}`} direction="row" spacing={1} alignItems="center">
													<Switch
														checked={option.isCorrect}
														onChange={(event) => setCorrectOption(questionIndex, optionIndex, event.target.checked)}
													/>
													<TextField
														fullWidth
														label={`Option ${optionIndex + 1}`}
														value={option.content}
														onChange={(event) => setOptionField(questionIndex, optionIndex, "content", event.target.value)}
													/>
													<IconButton
														color="error"
														onClick={() => removeOption(questionIndex, optionIndex)}
														disabled={question.options.length <= 2}
													>
														<MdDelete />
													</IconButton>
												</Stack>
											))}
										</Stack>
									)}
								</Stack>
							</CardContent>
						</Card>

						{questionIndex === form.questions.length - 1 ? (
							<Stack direction="row" justifyContent="center">
								<IconButton
									onClick={addQuestion}
									aria-label="Add question"
									sx={{
										width: 44,
										height: 44,
										border: "1px dashed",
										borderColor: "divider",
										backgroundColor: "background.paper",
									}}
								>
									<MdAdd />
								</IconButton>
							</Stack>
						) : null}
					</Stack>
				))}
			</Stack>

			<Stack direction={{ xs: "column", sm: "row" }} spacing={2} justifyContent="space-between" alignItems={{ xs: "stretch", sm: "center" }}>
				<Typography variant="body1" color="text.secondary">
					Total points: {totalPoints}
				</Typography>
				<Button variant="contained" size="large" onClick={onSubmit} disabled={isSubmitting}>
					{isSubmitting ? "Creating..." : "Create Quiz"}
				</Button>
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

export default CreateQuizPage;
