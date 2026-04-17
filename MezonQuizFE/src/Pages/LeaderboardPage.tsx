import { useCallback, useEffect, useMemo, useState, type ChangeEvent } from "react";
import {
	Box,
	Button,
	CircularProgress,
	Grid,
	MenuItem,
	Pagination,
	Paper,
	Stack,
	Table,
	TableBody,
	TableCell,
	TableHead,
	TableRow,
	TextField,
	Typography,
} from "@mui/material";
import AppSnackbar from "../Components/AppSnackbar";
import UserIdentityCell from "../Components/UserIdentityCell";
import useAppSnackbar from "../Hooks/useAppSnackbar";
import { getTopUserAnalytics } from "../Api/dashboard.api";
import type {
	TopUserAnalyticsQuery,
	TopUserAnalyticsResponseDto,
} from "../Interface/dashboard.dto";

const SORT_OPTIONS = [
	{ value: "totalscore", label: "Total score" },
	{ value: "accuracy", label: "Accuracy" },
	{ value: "totalsessions", label: "Sessions" },
	{ value: "lastseenat", label: "Last seen" },
	{ value: "displayname", label: "Display name" },
];

const PAGE_SIZE = 20;

const formatDateTime = (isoDate: string) => {
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

const buildQueryParams = (query: TopUserAnalyticsQuery): TopUserAnalyticsQuery => {
	const search = (query.search ?? "").trim();
	return {
		page: query.page ?? 1,
		pageSize: query.pageSize ?? PAGE_SIZE,
		sortBy: query.sortBy ?? "totalscore",
		sortDirection: query.sortDirection ?? "desc",
		search: search || undefined,
		dateFrom: query.dateFrom || undefined,
		dateTo: query.dateTo || undefined,
		minSessions: query.minSessions && query.minSessions > 0 ? query.minSessions : undefined,
	};
};

const LeaderboardPage = () => {
	const [data, setData] = useState<TopUserAnalyticsResponseDto | null>(null);
	const [loading, setLoading] = useState(false);
	const [refreshing, setRefreshing] = useState(false);
	const [searchInput, setSearchInput] = useState("");
	const [query, setQuery] = useState<TopUserAnalyticsQuery>({
		page: 1,
		pageSize: PAGE_SIZE,
		sortBy: "totalscore",
		sortDirection: "desc",
	});
	const { snackbar, closeSnackbar, showError } = useAppSnackbar();

	const loadData = useCallback(async (manualRefresh = false) => {
		if (manualRefresh) {
			setRefreshing(true);
		} else {
			setLoading(true);
		}

		try {
			const response = await getTopUserAnalytics(buildQueryParams(query));
			setData(response);
		} catch {
			showError("Failed to load top user analytics.");
		} finally {
			setLoading(false);
			setRefreshing(false);
		}
	}, [query, showError]);

	useEffect(() => {
		void loadData();
	}, [loadData]);

	const rows = data?.items ?? [];
	const summary = data?.summary;
	const pagination = data?.pagination;

	const kpiCards = useMemo(
		() => [
			{
				label: "Active Users",
				value: summary?.totalActiveUsers ?? 0,
				color: "#0ea5e9",
			},
			{
				label: "Participations",
				value: summary?.totalParticipations ?? 0,
				color: "#22c55e",
			},
			{
				label: "Sessions",
				value: summary?.totalSessions ?? 0,
				color: "#f59e0b",
			},
			{
				label: "Avg Accuracy",
				value: `${(summary?.averageAccuracy ?? 0).toFixed(2)}%`,
				color: "#a855f7",
			},
			{
				label: "Avg Score/User",
				value: (summary?.averageScorePerUser ?? 0).toLocaleString("en-US", {
					maximumFractionDigits: 2,
				}),
				color: "#ec4899",
			},
		],
		[summary],
	);

	const applyFilters = () => {
		setQuery((current) => ({
			...current,
			search: searchInput,
			page: 1,
		}));
	};

	const clearFilters = () => {
		setSearchInput("");
		setQuery({
			page: 1,
			pageSize: PAGE_SIZE,
			sortBy: "totalscore",
			sortDirection: "desc",
		});
	};

	const onPageChange = (_: ChangeEvent<unknown>, page: number) => {
		setQuery((current) => ({
			...current,
			page,
		}));
	};

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
						Leaderboard
					</Typography>
					<Typography variant="body2" color="text.secondary" mt={0.5}>
						Bảng xếp hạng người chơi trên toàn bộ phiên quiz.
					</Typography>
				</Box>
				<Stack direction="row" spacing={1.25} alignItems="center">
					{data?.generatedAt ? (
						<Typography variant="caption" color="text.secondary">
							Updated: {formatDateTime(data.generatedAt)}
						</Typography>
					) : null}
					<Button
						variant="contained"
						onClick={() => void loadData(true)}
						disabled={loading || refreshing}
					>
						{refreshing ? "Refreshing..." : "Refresh"}
					</Button>
				</Stack>
			</Stack>

			<Paper variant="outlined" sx={{ p: 2, boxShadow: "none", mb: 2 }}>
				<Stack spacing={1.25}>
					<Grid container spacing={1.5}>
						<Grid size={{ xs: 12, md: 4 }}>
							<TextField
								fullWidth
								label="Search by display name"
								value={searchInput}
								onChange={(event) => setSearchInput(event.target.value)}
							/>
						</Grid>
						<Grid size={{ xs: 12, md: 2 }}>
							<TextField
								fullWidth
								label="From"
								type="date"
								InputLabelProps={{ shrink: true }}
								value={query.dateFrom ?? ""}
								onChange={(event) =>
									setQuery((current) => ({
										...current,
										dateFrom: event.target.value || undefined,
										page: 1,
									}))
								}
							/>
						</Grid>
						<Grid size={{ xs: 12, md: 2 }}>
							<TextField
								fullWidth
								label="To"
								type="date"
								InputLabelProps={{ shrink: true }}
								value={query.dateTo ?? ""}
								onChange={(event) =>
									setQuery((current) => ({
										...current,
										dateTo: event.target.value || undefined,
										page: 1,
									}))
								}
							/>
						</Grid>
						<Grid size={{ xs: 12, md: 2 }}>
							<TextField
								fullWidth
								label="Min sessions"
								type="number"
								inputProps={{ min: 0 }}
								value={query.minSessions ?? ""}
								onChange={(event) =>
									setQuery((current) => ({
										...current,
										minSessions: event.target.value ? Number(event.target.value) : undefined,
										page: 1,
									}))
								}
							/>
						</Grid>
						<Grid size={{ xs: 12, md: 2 }}>
							<TextField
								fullWidth
								select
								label="Sort by"
								value={query.sortBy ?? "totalscore"}
								onChange={(event) =>
									setQuery((current) => ({
										...current,
										sortBy: event.target.value,
										page: 1,
									}))
								}
							>
								{SORT_OPTIONS.map((option) => (
									<MenuItem key={option.value} value={option.value}>
										{option.label}
									</MenuItem>
								))}
							</TextField>
						</Grid>
					</Grid>
					<Stack direction="row" spacing={1.25} justifyContent="space-between" alignItems="center">
						<TextField
							select
							label="Direction"
							sx={{ width: 180 }}
							value={query.sortDirection ?? "desc"}
							onChange={(event) =>
								setQuery((current) => ({
									...current,
									sortDirection: event.target.value as "asc" | "desc",
									page: 1,
								}))
							}
						>
							<MenuItem value="desc">Descending</MenuItem>
							<MenuItem value="asc">Ascending</MenuItem>
						</TextField>
						<Stack direction="row" spacing={1.25}>
							<Button variant="outlined" onClick={clearFilters} disabled={loading || refreshing}>
								Reset
							</Button>
							<Button variant="contained" onClick={applyFilters} disabled={loading || refreshing}>
								Apply filters
							</Button>
						</Stack>
					</Stack>
				</Stack>
			</Paper>

			{loading ? (
				<Paper variant="outlined" sx={{ p: 5, display: "flex", justifyContent: "center" }}>
					<CircularProgress />
				</Paper>
			) : (
				<Stack spacing={2}>
					<Grid container spacing={1.5}>
						{kpiCards.map((card) => (
							<Grid key={card.label} size={{ xs: 12, sm: 6, md: 4, lg: 3 }}>
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
									<Typography variant="h5" fontWeight={800} mt={0.25}>
										{typeof card.value === "number"
											? card.value.toLocaleString("en-US")
											: card.value}
									</Typography>
								</Paper>
							</Grid>
						))}
					</Grid>

					<Paper variant="outlined" sx={{ boxShadow: "none" }}>
						<Table size="small">
							<TableHead>
								<TableRow>
									<TableCell>Rank</TableCell>
									<TableCell>User</TableCell>
									<TableCell align="right">Score</TableCell>
									<TableCell align="right">Accuracy</TableCell>
									<TableCell align="right">Correct</TableCell>
									<TableCell align="right">Answers</TableCell>
									<TableCell align="right">Sessions</TableCell>
									<TableCell align="right">First Seen</TableCell>
									<TableCell align="right">Last Seen</TableCell>
								</TableRow>
							</TableHead>
							<TableBody>
								{rows.map((row) => (
									<TableRow key={row.userId} hover>
										<TableCell>{row.rank}</TableCell>
										<TableCell>
											<UserIdentityCell
												userId={row.userId}
												displayName={row.displayName}
												avatarUrl={row.avatarUrl}
											/>
										</TableCell>
										<TableCell align="right">{row.totalScore.toLocaleString("en-US")}</TableCell>
										<TableCell align="right">{row.accuracyRate.toFixed(2)}%</TableCell>
										<TableCell align="right">{row.totalCorrectAnswers.toLocaleString("en-US")}</TableCell>
										<TableCell align="right">{row.totalAnswers.toLocaleString("en-US")}</TableCell>
										<TableCell align="right">{row.totalSessions.toLocaleString("en-US")}</TableCell>
										<TableCell align="right">{formatDateTime(row.firstSeenAt)}</TableCell>
										<TableCell align="right">{formatDateTime(row.lastSeenAt)}</TableCell>
									</TableRow>
								))}
								{rows.length === 0 ? (
									<TableRow>
										<TableCell colSpan={9} align="center">
											<Typography variant="body2" color="text.secondary" sx={{ py: 3 }}>
												No users matched the selected filters.
											</Typography>
										</TableCell>
									</TableRow>
								) : null}
							</TableBody>
						</Table>
					</Paper>

					<Stack direction="row" justifyContent="space-between" alignItems="center">
						<Typography variant="body2" color="text.secondary">
							Showing {(pagination?.totalCount ?? 0).toLocaleString("en-US")} user(s)
						</Typography>
						<Pagination
							color="primary"
							page={pagination?.page ?? 1}
							count={Math.max(pagination?.totalPages ?? 1, 1)}
							onChange={onPageChange}
							disabled={loading || refreshing || (pagination?.totalPages ?? 0) <= 1}
						/>
					</Stack>
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

export default LeaderboardPage;
