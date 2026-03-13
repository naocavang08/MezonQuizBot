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
			<Typography variant="h5" fontWeight={800} mb={2.5}>
				Admin Dashboard
			</Typography>

			<Grid container spacing={2}>
				{stats.map((stat) => (
					<Grid size={{ xs: 12, sm: 6, md: 3 }} key={stat.label}>
						<Paper
							sx={{
								p: 2.5,
								border: "1px solid rgba(148,163,184,0.2)",
								boxShadow: "none",
								background: "linear-gradient(145deg, rgba(14,26,43,0.95) 0%, rgba(8,17,31,0.92) 100%)",
							}}
						>
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
