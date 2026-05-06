import { useCallback, useEffect, useMemo, useState } from "react";
import {
    Box,
    Button,
    Card,
    CardContent,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
    Divider,
    FormControl,
    FormControlLabel,
    FormLabel,
    IconButton,
    InputLabel,
    MenuItem,
    Radio,
    RadioGroup,
    Select,
    Stack,
    Switch,
    TextField,
    Typography,
} from "@mui/material";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import AppSnackbar from "../Components/AppSnackbar";
import useAppSnackbar from "../Hooks/useAppSnackbar";
import { MdAdd, MdDelete } from "react-icons/md";
import { useNavigate } from "react-router-dom";
import { z } from "zod";
import { createCategory, getAllCategories } from "../Api/category.api";
import { createQuiz, uploadQuizMedia } from "../Api/quiz.api";
import type { CategoryDto } from "../Interface/category.dto";
import { CATEGORY_ICON_OPTIONS, getCategoryIconOption } from "../Lib/Utils/categoryIconOptions";
import CategoryIconBadge from "../Lib/Utils/categoryIconBadge";
import {
    QuestionType,
    QuizStatus,
    QuizVisibility,
    type QuizOptionDto,
    type QuizQuestionDto,
	type SaveQuizDto,
} from "../Interface/quiz.dto";

type FormState = {
    title: string;
    description: string;
	categoryId: string;
    visibility: SaveQuizDto["visibility"];
    status: SaveQuizDto["status"];
    settings: SaveQuizDto["settings"];
    questions: QuizQuestionDto[];
};

type CategoryFormState = {
	name: string;
	slug: string;
	icon: string;
	sortOrder: number;
};

const defaultCategoryForm: CategoryFormState = {
	name: "",
	slug: "",
	icon: "",
	sortOrder: 0,
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

const visibilityLabel: Record<SaveQuizDto["visibility"], string> = {
    [QuizVisibility.Private]: "Private",
    [QuizVisibility.Public]: "Public",
    [QuizVisibility.Unlisted]: "Unlisted",
};

const statusLabel: Record<SaveQuizDto["status"], string> = {
    [QuizStatus.Draft]: "Draft",
    [QuizStatus.Published]: "Published",
    [QuizStatus.Archived]: "Archived",
};

const questionTypeLabel: Record<QuizQuestionDto["questionType"], string> = {
    [QuestionType.SingleChoice]: "Single Choice",
    [QuestionType.MultipleChoice]: "Multiple Choice",
    [QuestionType.TrueFalse]: "True/False",
};

const categorySchema = z.object({
	name: z.string().trim().min(1, "Category name is required."),
	slug: z.string().trim().min(1, "Slug is required."),
	icon: z.string(),
	sortOrder: z.number(),
});

const quizOptionSchema = z.object({
	id: z.number().optional(),
	index: z.number(),
	content: z.string().trim().min(1, "Option content is required."),
	isCorrect: z.boolean(),
});

const quizQuestionSchema = z.object({
	id: z.number().optional(),
	index: z.number(),
	content: z.string().trim().min(1, "Question content is required."),
	mediaUrl: z.string().optional(),
	timeLimitSeconds: z.number().min(10, "Time limit must be between 10 and 30 seconds.").max(30, "Time limit must be between 10 and 30 seconds."),
	points: z.number().min(1, "Points must be between 1 and 20.").max(20, "Points must be between 1 and 20."),
	questionType: z.union([
		z.literal(QuestionType.SingleChoice),
		z.literal(QuestionType.MultipleChoice),
		z.literal(QuestionType.TrueFalse),
	]),
	options: z.array(quizOptionSchema).min(2, "Questions must have at least 2 options."),
}).superRefine((question, ctx) => {
	const correctCount = question.options.filter((option) => option.isCorrect).length;

	if (question.questionType === QuestionType.TrueFalse) {
		if (question.options.length !== 2 || correctCount !== 1) {
			ctx.addIssue({ code: z.ZodIssueCode.custom, message: "True/False questions must have exactly 2 options and 1 correct answer." });
		}
		return;
	}

	if (question.questionType === QuestionType.SingleChoice && correctCount !== 1) {
		ctx.addIssue({ code: z.ZodIssueCode.custom, message: "Single choice questions must have exactly 1 correct answer." });
	}

	if (question.questionType === QuestionType.MultipleChoice && correctCount < 2) {
		ctx.addIssue({ code: z.ZodIssueCode.custom, message: "Multiple choice questions must have at least 2 correct answers." });
	}
});

const saveQuizSchema = z.object({
	title: z.string().trim().min(1, "Quiz title is required.").max(500, "Quiz title must not exceed 500 characters."),
	description: z.string().optional(),
	categoryId: z.string().optional(),
	questions: z.array(quizQuestionSchema),
	settings: z.object({
		shuffleQuestions: z.boolean(),
		shuffleOptions: z.boolean(),
		showCorrectAnswer: z.boolean(),
	}),
	visibility: z.union([
		z.literal(QuizVisibility.Private),
		z.literal(QuizVisibility.Public),
		z.literal(QuizVisibility.Unlisted),
	]),
	status: z.union([
		z.literal(QuizStatus.Draft),
		z.literal(QuizStatus.Published),
		z.literal(QuizStatus.Archived),
	]),
});

const CreateQuizPage = () => {
	const navigate = useNavigate();
	const [categories, setCategories] = useState<CategoryDto[]>([]);
	const [isLoadingCategories, setIsLoadingCategories] = useState(false);
	const [isSubmitting, setIsSubmitting] = useState(false);
    const [uploadingQuestionIndex, setUploadingQuestionIndex] = useState<number | null>(null);
	const [openCreateCategoryDialog, setOpenCreateCategoryDialog] = useState(false);
	const [createCategoryForm, setCreateCategoryForm] = useState<CategoryFormState>(defaultCategoryForm);
	const [isCreatingCategory, setIsCreatingCategory] = useState(false);
	const { snackbar, showError, showSuccess, closeSnackbar } = useAppSnackbar();
	const categoryMethods = useForm<CategoryFormState>({
		resolver: zodResolver(categorySchema),
		defaultValues: defaultCategoryForm,
	});
	const quizMethods = useForm<SaveQuizDto>({
		resolver: zodResolver(saveQuizSchema),
		defaultValues: {
			title: "",
			description: undefined,
			categoryId: undefined,
			visibility: QuizVisibility.Private,
			status: QuizStatus.Draft,
			settings: {
				shuffleQuestions: false,
				shuffleOptions: false,
				showCorrectAnswer: true,
			},
			questions: [makeDefaultQuestion(1)],
		},
	});
	const [form, setForm] = useState<FormState>({
		title: "",
		description: "",
		categoryId: "",
		visibility: QuizVisibility.Private,
		status: QuizStatus.Draft,
		settings: {
			shuffleQuestions: false,
			shuffleOptions: false,
			showCorrectAnswer: true
		},
		questions: [makeDefaultQuestion(1)],
	});

	const buildPayload = (): SaveQuizDto => ({
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
	});

	const loadCategories = useCallback(async () => {
		try {
			setIsLoadingCategories(true);
			const data = await getAllCategories();
			setCategories(Array.isArray(data) ? data : []);
			return Array.isArray(data) ? data : [];
		} catch {
			showError("Failed to load categories.");
			return [];
		} finally {
			setIsLoadingCategories(false);
		}
	}, [showError]);

	useEffect(() => {
		void loadCategories();
	}, [loadCategories]);

	const normalizeCategoryForm = (): CategoryFormState => ({
		name: createCategoryForm.name.trim(),
		slug: createCategoryForm.slug.trim(),
		icon: createCategoryForm.icon.trim(),
		sortOrder: Number(createCategoryForm.sortOrder) || 0,
	});

	const handleCreateCategory = async () => {
		const normalized = normalizeCategoryForm();
		categoryMethods.reset(normalized);
		if (normalized.icon && !getCategoryIconOption(normalized.icon)) {
			showError("Icon không hợp lệ. Vui lòng chọn từ danh sách.");
			return;
		}

		const isValid = await categoryMethods.trigger();
		if (!isValid) {
			const firstMessage = Object.values(categoryMethods.formState.errors).find((error) => error?.message)?.message;
			showError(firstMessage ?? "Invalid category data.");
			return;
		}

		try {
			setIsCreatingCategory(true);
			await createCategory(categoryMethods.getValues());

			const updatedCategories = await loadCategories();
			const createdCategory = updatedCategories.find(
				(category) => category.slug.toLowerCase() === normalized.slug.toLowerCase(),
			);

			if (createdCategory) {
				setForm((prev) => ({ ...prev, categoryId: createdCategory.id }));
			}

			showSuccess("Category created successfully.");
			setOpenCreateCategoryDialog(false);
			setCreateCategoryForm(defaultCategoryForm);
		} catch {
			showError("Failed to create category.");
		} finally {
			setIsCreatingCategory(false);
		}
	};

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

	const onSubmit = async () => {
		quizMethods.reset(buildPayload());
		const isValid = await quizMethods.trigger();
		if (!isValid) {
			const firstMessage = Object.values(quizMethods.formState.errors).find((error) => error?.message)?.message;
			showError(firstMessage ?? "Invalid quiz data.");
			return;
		}

		try {
			setIsSubmitting(true);
			const result = await createQuiz(quizMethods.getValues());
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

    const uploadQuestionMedia = async (questionIndex: number, file: File) => {
        try {
            setUploadingQuestionIndex(questionIndex);
            const uploadedUrl = await uploadQuizMedia(file);
            if (!uploadedUrl) {
                showError("Upload succeeded but no media URL returned.");
                return;
            }

            setQuestionField(questionIndex, "mediaUrl", uploadedUrl);
            showSuccess("Media uploaded successfully.");
        } catch {
            showError("Failed to upload media.");
        } finally {
            setUploadingQuestionIndex(null);
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

						<Stack
							direction={{ xs: "column", sm: "row" }}
							spacing={2}
							alignItems={{ xs: "stretch", sm: "flex-end" }}
							>
							<FormControl fullWidth size="small">
								<FormLabel sx={{ mb: 0.5 }}>Category</FormLabel>
								<Select
									value={form.categoryId}
									displayEmpty
									onChange={(event) =>
										setForm((prev) => ({
										...prev,
										categoryId: String(event.target.value),
										}))
									}
									disabled={isLoadingCategories}
								>
									<MenuItem value="">
										<Typography>No Category</Typography>
									</MenuItem>

									{categories.map((category) => (
										<MenuItem key={category.id} value={category.id}>
										{category.name}
										</MenuItem>
									))}
								</Select>
							</FormControl>

							<Button
								variant="contained"
								color="primary"
								onClick={() => setOpenCreateCategoryDialog(true)}
								sx={{
								minWidth: { xs: "100%", sm: 160 },
								height: 40,
								}}
							>
								+ Add
							</Button>
						</Stack>

						<Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
							<FormControl fullWidth>
								<FormLabel>Visibility</FormLabel>
								<Select
									value={form.visibility}
									onChange={(event) =>
										setForm((prev) => ({ ...prev, visibility: Number(event.target.value) as SaveQuizDto["visibility"] }))
									}
								>
									{Object.values(QuizVisibility).map((value) => (
										<MenuItem key={value} value={value}>
											{visibilityLabel[value as SaveQuizDto["visibility"]]}
										</MenuItem>
									))}
								</Select>
							</FormControl>

							<FormControl fullWidth>
								<FormLabel>Status</FormLabel>
								<Select
									value={form.status}
									onChange={(event) =>
										setForm((prev) => ({ ...prev, status: Number(event.target.value) as SaveQuizDto["status"] }))
									}
								>
									{Object.values(QuizStatus).map((value) => (
										<MenuItem key={value} value={value}>
											{statusLabel[value as SaveQuizDto["status"]]}
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

                                    <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
                                        <Button
                                            component="label"
                                            variant="outlined"
                                            disabled={uploadingQuestionIndex === questionIndex}
                                        >
                                            {uploadingQuestionIndex === questionIndex ? "Uploading..." : "Upload image"}
                                            <input
                                                hidden
                                                type="file"
                                                accept="image/*"
                                                onChange={(event) => {
                                                    const file = event.target.files?.[0];
                                                    if (!file) return;
                                                    void uploadQuestionMedia(questionIndex, file);
                                                    event.currentTarget.value = "";
                                                }}
                                            />
                                        </Button>
                                    </Stack>

									<Stack direction={{ xs: "column", sm: "row" }} spacing={2}>
										<TextField
											type="number"
											size="small"
											label="Time Limit (10-30s)"
											inputProps={{ min: 10, max: 30 }}
											value={question.timeLimitSeconds}
											onChange={(event) => setQuestionField(questionIndex, "timeLimitSeconds", Number(event.target.value || 10))}
										/>
										<TextField
											type="number"
											size="small"
											label="Points (1-20)"
											inputProps={{ min: 1, max: 20 }}
											value={question.points}
											onChange={(event) => setQuestionField(questionIndex, "points", Number(event.target.value || 1))}
										/>
										<FormControl fullWidth size="small">
											<InputLabel id={`question-type-label-${questionIndex}`}>Question Type</InputLabel>
											<Select
												labelId={`question-type-label-${questionIndex}`}
												label="Question Type"
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

			<Dialog
				open={openCreateCategoryDialog}
				onClose={() => setOpenCreateCategoryDialog(false)}
				maxWidth="sm"
				fullWidth
			>
				<DialogTitle>Create Category</DialogTitle>
				<DialogContent>
					<Stack spacing={2} sx={{ mt: 1 }}>
						<TextField
							label="Name"
							value={createCategoryForm.name}
							onChange={(event) => setCreateCategoryForm((prev) => ({ ...prev, name: event.target.value }))}
							fullWidth
						/>
						<TextField
							label="Slug"
							value={createCategoryForm.slug}
							onChange={(event) => setCreateCategoryForm((prev) => ({ ...prev, slug: event.target.value }))}
							fullWidth
						/>
						<Select
							displayEmpty
							fullWidth
							value={createCategoryForm.icon || ""}
							onChange={(event) => setCreateCategoryForm((prev) => ({ ...prev, icon: String(event.target.value) }))}
							renderValue={(value) => {
								const selectedIcon = String(value);
								const selectedOption = getCategoryIconOption(selectedIcon);

								if (!selectedOption) {
									return <Typography color="text.secondary">Select icon</Typography>;
								}

								return (
									<Stack direction="row" spacing={1} alignItems="center">
										<CategoryIconBadge iconKey={selectedOption.key} fallback={null} />
										<Typography>{selectedOption.label}</Typography>
									</Stack>
								);
							}}
						>
							<MenuItem value="">
								No icon
							</MenuItem>
							{CATEGORY_ICON_OPTIONS.map((option) => (
								<MenuItem key={option.key} value={option.key}>
									<Stack direction="row" spacing={1} alignItems="center">
										<CategoryIconBadge iconKey={option.key} fallback={null} />
										<Typography>{option.label}</Typography>
									</Stack>
								</MenuItem>
							))}
						</Select>
						<TextField
							label="Sort Order"
							type="number"
							value={createCategoryForm.sortOrder}
							onChange={(event) =>
								setCreateCategoryForm((prev) => ({ ...prev, sortOrder: Number(event.target.value) || 0 }))
							}
							fullWidth
						/>
					</Stack>
				</DialogContent>
				<DialogActions>
					<Button onClick={() => setOpenCreateCategoryDialog(false)}>Cancel</Button>
					<Button onClick={handleCreateCategory} variant="contained" disabled={isCreatingCategory}>
						{isCreatingCategory ? "Creating..." : "Create"}
					</Button>
				</DialogActions>
			</Dialog>
		</Box>
	);
};

export default CreateQuizPage;
