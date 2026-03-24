import { useCallback, useEffect, useState } from "react";
import {
	Box,
	Chip,
	CircularProgress,
	FormControl,
	InputLabel,
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
import type { CategoryDto } from "../../Interface/category.dto";
import { QuizStatus, type Quiz, type QuizDto } from "../../Interface/quiz.dto";

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

const QuizPage = () => {
	const [quizzes, setQuizzes] = useState<QuizDto[]>([]);
	const [selectedQuiz, setSelectedQuiz] = useState<Quiz | null>(null);
	const [loading, setLoading] = useState(false);
	const [isLoadingCategories, setIsLoadingCategories] = useState(false);
	const [detailsLoading, setDetailsLoading] = useState(false);
    const { snackbar, showError, closeSnackbar } = useAppSnackbar();
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

	const handleSelectQuiz = async (quizId?: string) => {
		if (!quizId) {
			return;
		}

		setDetailsLoading(true);
		try {
			const details = await getQuiz(quizId);
			setSelectedQuiz(details);
		} catch {
			showError("Could not load quiz details.");
		} finally {
			setDetailsLoading(false);
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
		setSelectedQuiz(null);
		void fetchQuizzes();
	}, [fetchQuizzes]);

	return (
		<Box>
			<Typography variant="h5" fontWeight={700} mb={2}>
				Quiz Management
			</Typography>

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

			<Paper variant="outlined" sx={{ boxShadow: "none" }}>
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
							{quizzes.map((quiz) => (
								<TableRow
									key={quiz.id}
									hover
									onClick={() => handleSelectQuiz(quiz.id)}
									sx={{ cursor: "pointer" }}
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
							))}
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
				pt={1}
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

			<Paper variant="outlined" sx={{ boxShadow: "none", mt: 2, p: 2 }}>
				<Typography variant="h6" fontWeight={700} mb={1}>
					Quiz Details
				</Typography>

				{detailsLoading ? (
					<Stack direction="row" justifyContent="center" py={2}>
						<CircularProgress size={24} />
					</Stack>
				) : selectedQuiz ? (
					<Stack spacing={0.5}>
						<Typography variant="body2"><strong>Title:</strong> {selectedQuiz.title}</Typography>
						<Typography variant="body2"><strong>Description:</strong> {selectedQuiz.description || "-"}</Typography>
						<Typography variant="body2"><strong>Questions:</strong> {selectedQuiz.questions.length}</Typography>
						<Typography variant="body2"><strong>Total Points:</strong> {selectedQuiz.totalPoints ?? 0}</Typography>
					</Stack>
				) : (
					<Typography variant="body2" color="text.secondary">
						Click a quiz row to load details.
					</Typography>
				)}
			</Paper>

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
