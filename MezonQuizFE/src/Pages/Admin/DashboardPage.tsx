import { Box, Grid, Paper, Typography } from "@mui/material";

const stats = [
	{ label: "Total Users", value: 128 },
	{ label: "Total Roles", value: 5 },
	{ label: "Total Quizzes", value: 34 },
	{ label: "Running Sessions", value: 3 }
];

const DashboardPage = () => {
	return (
		<Box>
			<Typography variant="h5" fontWeight={700} mb={2}>
				Admin Dashboard
			</Typography>

			<Grid container spacing={2}>
				{stats.map((stat) => (
					<Grid size={{ xs: 12, sm: 6, md: 3 }} key={stat.label}>
						<Paper sx={{ p: 2, border: "1px solid #e5e7eb", boxShadow: "none" }}>
							<Typography variant="body2" color="text.secondary">
								{stat.label}
							</Typography>
							<Typography variant="h5" fontWeight={700} mt={0.5}>
								{stat.value}
							</Typography>
						</Paper>
					</Grid>
				))}
			</Grid>
		</Box>
	);
};

export default DashboardPage;
