import { useCallback, useEffect, useMemo, useState } from "react";
import {
	Box,
	Button,
	CircularProgress,
	Grid,
	LinearProgress,
	Paper,
	Stack,
	Table,
	TableBody,
	TableCell,
	TableHead,
	TableRow,
	Typography,
} from "@mui/material";
import AppSnackbar from "../../Components/AppSnackbar";
import useAppSnackbar from "../../Hooks/useAppSnackbar";
import { getDashboardSummary } from "../../Api/dashboard.api";
import type {
	DashboardDailyStatDto,
	DashboardStatusCountDto,
	DashboardSummaryDto,
} from "../../Interface/dashboard.dto";

const MAX_BAR_HEIGHT = 140;

const formatDate = (isoDate: string) => {
	if (!isoDate) {
		return "-";
	}

	const parsed = new Date(isoDate);
	if (Number.isNaN(parsed.getTime())) {
		return "-";
	}

	return new Intl.DateTimeFormat("en-GB", {
		day: "2-digit",
		month: "short",
		year: "numeric",
		hour: "2-digit",
		minute: "2-digit",
	}).format(parsed);
};

const formatDay = (isoDate: string) => {
	const parsed = new Date(isoDate);
	if (Number.isNaN(parsed.getTime())) {
		return "-";
	}

	return new Intl.DateTimeFormat("en-GB", {
		day: "2-digit",
		month: "short",
	}).format(parsed);
};

const renderStatusDistribution = (items: DashboardStatusCountDto[]) => {
	const total = items.reduce((sum, item) => sum + item.count, 0);

	if (items.length === 0 || total === 0) {
		return (
			<Typography variant="body2" color="text.secondary">
				No data yet.
			</Typography>
		);
	}

	return (
		<Stack spacing={1.25}>
			{items.map((item) => {
				const percent = Math.round((item.count / total) * 100);
				return (
					<Box key={item.label}>
						<Stack direction="row" justifyContent="space-between" alignItems="center" mb={0.5}>
							<Typography variant="body2" fontWeight={600}>
								{item.label}
							</Typography>
							<Typography variant="caption" color="text.secondary">
								{item.count} ({percent}%)
							</Typography>
						</Stack>
						<LinearProgress
							variant="determinate"
							value={percent}
							sx={{
								height: 8,
								borderRadius: 2,
								backgroundColor: "rgba(148,163,184,0.25)",
							}}
						/>
					</Box>
				);
			})}
		</Stack>
	);
};

const renderDailyActivityChart = (items: DashboardDailyStatDto[]) => {
	if (items.length === 0) {
		return (
			<Typography variant="body2" color="text.secondary">
				No activity in selected period.
			</Typography>
		);
	}

	const maxValue = Math.max(
		1,
		...items.map((item) => Math.max(item.users, item.quizzes, item.sessions)),
	);

	return (
		<Stack direction="row" spacing={1.25} alignItems="end" sx={{ overflowX: "auto", pt: 1 }}>
			{items.map((item) => {
				const usersHeight = Math.max(4, Math.round((item.users / maxValue) * MAX_BAR_HEIGHT));
				const quizzesHeight = Math.max(4, Math.round((item.quizzes / maxValue) * MAX_BAR_HEIGHT));
				const sessionsHeight = Math.max(4, Math.round((item.sessions / maxValue) * MAX_BAR_HEIGHT));

				return (
					<Stack key={item.date} spacing={0.75} alignItems="center" minWidth={64}>
						<Stack direction="row" spacing={0.5} alignItems="end" sx={{ height: MAX_BAR_HEIGHT + 4 }}>
							<Box
								sx={{
									width: 10,
									height: usersHeight,
									borderRadius: 1,
									bgcolor: "#0ea5e9",
								}}
							/>
							<Box
								sx={{
									width: 10,
									height: quizzesHeight,
									borderRadius: 1,
									bgcolor: "#22c55e",
								}}
							/>
							<Box
								sx={{
									width: 10,
									height: sessionsHeight,
									borderRadius: 1,
									bgcolor: "#f59e0b",
								}}
							/>
						</Stack>
						<Typography variant="caption" color="text.secondary">
							{formatDay(item.date)}
						</Typography>
					</Stack>
				);
			})}
		</Stack>
	);
};

const DashboardPage = () => {
	const [summary, setSummary] = useState<DashboardSummaryDto | null>(null);
	const [loading, setLoading] = useState(false);
	const [refreshing, setRefreshing] = useState(false);
	const { snackbar, closeSnackbar, showError } = useAppSnackbar();

	const kpiCards = useMemo(() => {
		if (!summary) {
			return [];
		}

		return [
			{ label: "Users", value: summary.kpis.users, color: "#0ea5e9" },
			{ label: "Quizzes", value: summary.kpis.quizzes, color: "#22c55e" },
			{ label: "Categories", value: summary.kpis.categories, color: "#a855f7" },
			{ label: "Sessions", value: summary.kpis.sessions, color: "#f59e0b" },
			{ label: "Participants", value: summary.kpis.participants, color: "#ec4899" },
			{ label: "Answers", value: summary.kpis.answers, color: "#6366f1" },
		];
	}, [summary]);

	const loadDashboard = useCallback(async (isManualRefresh = false) => {
		if (isManualRefresh) {
			setRefreshing(true);
		} else {
			setLoading(true);
		}

		try {
			const data = await getDashboardSummary({ days: 7 });
			setSummary(data);
		} catch {
			showError("Failed to load dashboard summary.");
		} finally {
			setLoading(false);
			setRefreshing(false);
		}
	}, [showError]);

	useEffect(() => {
		void loadDashboard();
	}, [loadDashboard]);

	return (
		<Box>
			<Stack
				direction={{ xs: "column", md: "row" }}
				justifyContent="space-between"
				alignItems={{ xs: "flex-start", md: "center" }}
				spacing={1.5}
				mb={2.5}
			>
				<Box>
					<Typography variant="h5" fontWeight={800}>
						Admin Dashboard
					</Typography>
					<Typography variant="body2" color="text.secondary" mt={0.5}>
						System overview, content health, and recent activity.
					</Typography>
				</Box>
				<Stack direction="row" spacing={1.25} alignItems="center">
					{summary?.generatedAt ? (
						<Typography variant="caption" color="text.secondary">
							Updated: {formatDate(summary.generatedAt)}
						</Typography>
					) : null}
					<Button
						variant="contained"
						onClick={() => void loadDashboard(true)}
						disabled={loading || refreshing}
					>
						{refreshing ? "Refreshing..." : "Refresh"}
					</Button>
				</Stack>
			</Stack>

			{loading ? (
				<Paper variant="outlined" sx={{ p: 5, display: "flex", justifyContent: "center" }}>
					<CircularProgress />
				</Paper>
			) : !summary ? (
				<Paper variant="outlined" sx={{ p: 3 }}>
					<Typography color="text.secondary">No dashboard data found.</Typography>
				</Paper>
			) : (
				<Stack spacing={2}>
					<Grid container spacing={1.5}>
						{kpiCards.map((card) => (
							<Grid key={card.label} size={{ xs: 12, sm: 6, md: 4, lg: 2 }}>
								<Paper
									variant="outlined"
									sx={{
										p: 2,
										borderTop: `3px solid ${card.color}`,
										boxShadow: "none",
									}}
								>
									<Typography variant="body2" color="text.secondary">
										{card.label}
									</Typography>
									<Typography variant="h4" fontWeight={800} mt={0.25}>
										{card.value.toLocaleString("en-US")}
									</Typography>
								</Paper>
							</Grid>
						))}
					</Grid>

					<Grid container spacing={1.5}>
						<Grid size={{ xs: 12, md: 6 }}>
							<Paper variant="outlined" sx={{ p: 2.25, height: "100%", boxShadow: "none" }}>
								<Typography variant="h6" fontWeight={700} mb={1.25}>
									Quiz Status Distribution
								</Typography>
								{renderStatusDistribution(summary.quizStatusDistribution)}
							</Paper>
						</Grid>
						<Grid size={{ xs: 12, md: 6 }}>
							<Paper variant="outlined" sx={{ p: 2.25, height: "100%", boxShadow: "none" }}>
								<Typography variant="h6" fontWeight={700} mb={1.25}>
									Session Status Distribution
								</Typography>
								{renderStatusDistribution(summary.sessionStatusDistribution)}
							</Paper>
						</Grid>
					</Grid>

					<Grid container spacing={1.5}>
						<Grid size={{ xs: 12, lg: 7 }}>
							<Paper variant="outlined" sx={{ p: 2.25, boxShadow: "none" }}>
								<Typography variant="h6" fontWeight={700} mb={0.5}>
									Activity in Last 7 Days
								</Typography>
								<Stack direction="row" spacing={1.75} mb={1.5} mt={0.5}>
									<Stack direction="row" spacing={0.5} alignItems="center">
										<Box sx={{ width: 10, height: 10, borderRadius: 999, bgcolor: "#0ea5e9" }} />
										<Typography variant="caption" color="text.secondary">Users</Typography>
									</Stack>
									<Stack direction="row" spacing={0.5} alignItems="center">
										<Box sx={{ width: 10, height: 10, borderRadius: 999, bgcolor: "#22c55e" }} />
										<Typography variant="caption" color="text.secondary">Quizzes</Typography>
									</Stack>
									<Stack direction="row" spacing={0.5} alignItems="center">
										<Box sx={{ width: 10, height: 10, borderRadius: 999, bgcolor: "#f59e0b" }} />
										<Typography variant="caption" color="text.secondary">Sessions</Typography>
									</Stack>
								</Stack>
								{renderDailyActivityChart(summary.dailyStats)}
							</Paper>
						</Grid>
						<Grid size={{ xs: 12, lg: 5 }}>
							<Paper variant="outlined" sx={{ p: 2.25, boxShadow: "none" }}>
								<Typography variant="h6" fontWeight={700} mb={1.25}>
									Top Categories
								</Typography>
								<Table size="small">
									<TableHead>
										<TableRow>
											<TableCell>Category</TableCell>
											<TableCell align="right">Quizzes</TableCell>
										</TableRow>
									</TableHead>
									<TableBody>
										{summary.topCategories.map((item) => (
											<TableRow key={item.categoryName}>
												<TableCell>{item.categoryName}</TableCell>
												<TableCell align="right">{item.quizCount}</TableCell>
											</TableRow>
										))}
										{summary.topCategories.length === 0 ? (
											<TableRow>
												<TableCell colSpan={2} align="center">
													<Typography variant="body2" color="text.secondary">
														No categories yet.
													</Typography>
												</TableCell>
											</TableRow>
										) : null}
									</TableBody>
								</Table>
							</Paper>
						</Grid>
					</Grid>
				</Stack>
			)}

			<AppSnackbar
				open={snackbar.open}
				message={snackbar.message}
				severity={snackbar.severity}
				onClose={closeSnackbar}
			/>
		</Box>
	);
};

export default DashboardPage;
