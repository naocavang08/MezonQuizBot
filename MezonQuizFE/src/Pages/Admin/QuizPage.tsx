import { Paper, Table, TableBody, TableCell, TableHead, TableRow, Typography } from "@mui/material";

const quizzes = [
	{ id: "Q001", title: "General Knowledge", status: "Published" },
	{ id: "Q002", title: "Math Basics", status: "Draft" },
	{ id: "Q003", title: "Science Quiz", status: "Published" }
];

const QuizPage = () => {
	return (
		<>
			<Typography variant="h5" fontWeight={700} mb={2}>
				Quiz Management
			</Typography>

			<Paper sx={{ border: "1px solid #e5e7eb", boxShadow: "none" }}>
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
							<TableRow key={quiz.id}>
								<TableCell>{quiz.id}</TableCell>
								<TableCell>{quiz.title}</TableCell>
								<TableCell>{quiz.status}</TableCell>
							</TableRow>
						))}
					</TableBody>
				</Table>
			</Paper>
		</>
	);
};

export default QuizPage;
