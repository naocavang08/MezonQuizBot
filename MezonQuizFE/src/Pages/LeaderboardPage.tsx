import { useCallback, useEffect, useMemo, useState, type ChangeEvent, type ReactNode } from "react";
import {
	Avatar,
	Box,
	Button,
	Chip,
	CircularProgress,
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
import { MdEmojiEvents, MdMilitaryTech, MdStars, MdTrendingUp } from "react-icons/md";
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

type RankTier = {
	label: string;
	icon: ReactNode;
	chipColor: "warning" | "error" | "secondary" | "info" | "default";
	rowBackground: string;
	borderColor: string;
};

const getRankTier = (rank: number): RankTier => {
	if (rank === 1) {
		return {
			label: "Top 1",
			icon: <MdEmojiEvents size={18} color="#ca8a04" />,
			chipColor: "warning",
			rowBackground: "linear-gradient(90deg, rgba(251,191,36,0.18), rgba(251,191,36,0.05))",
			borderColor: "rgba(202,138,4,0.5)",
		};
	}

	if (rank <= 3) {
		return {
			label: "Top 3",
			icon: <MdMilitaryTech size={18} color="#f97316" />,
			chipColor: "error",
			rowBackground: "linear-gradient(90deg, rgba(249,115,22,0.16), rgba(249,115,22,0.04))",
			borderColor: "rgba(249,115,22,0.45)",
		};
	}

	if (rank <= 5) {
		return {
			label: "Top 5",
			icon: <MdStars size={18} color="#7c3aed" />,
			chipColor: "secondary",
			rowBackground: "linear-gradient(90deg, rgba(124,58,237,0.14), rgba(124,58,237,0.04))",
			borderColor: "rgba(124,58,237,0.42)",
		};
	}

	if (rank <= 10) {
		return {
			label: "Top 10",
			icon: <MdTrendingUp size={18} color="#0284c7" />,
			chipColor: "info",
			rowBackground: "linear-gradient(90deg, rgba(14,165,233,0.14), rgba(14,165,233,0.04))",
			borderColor: "rgba(2,132,199,0.38)",
		};
	}

	return {
		label: "Ranked",
		icon: <MdTrendingUp size={18} color="#64748b" />,
		chipColor: "default",
		rowBackground: "transparent",
		borderColor: "transparent",
	};
};

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

	const topLegendItems = useMemo(
		() => [
			getRankTier(1),
			getRankTier(3),
			getRankTier(5),
			getRankTier(10),
		],
		[],
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
			<Paper
				variant="outlined"
				sx={{
					mb: 2.5,
					p: 2,
					background:
						"radial-gradient(circle at 0% 0%, rgba(14,165,233,0.16), transparent 48%), radial-gradient(circle at 95% 0%, rgba(251,146,60,0.15), transparent 45%)",
					boxShadow: "none",
				}}
			>
				<Stack
					direction={{ xs: "column", md: "row" }}
					justifyContent="space-between"
					alignItems={{ xs: "flex-start", md: "center" }}
					spacing={1.5}
				>
					<Box>
						<Typography variant="h5" fontWeight={900}>
							Player Leaderboard
						</Typography>
						<Typography variant="body2" color="text.secondary" mt={0.5}>
							See the top players based on their quiz performance and activity.
						</Typography>
					</Box>
					<Stack direction="row" spacing={1.25} alignItems="center" flexWrap="wrap" useFlexGap>
						<Chip
							label={`Active: ${(summary?.totalActiveUsers ?? 0).toLocaleString("en-US")}`}
							size="small"
							variant="outlined"
						/>
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
			</Paper>

			<Paper variant="outlined" sx={{ p: 2, boxShadow: "none", mb: 2 }}>
				<Stack spacing={1.25}>
					<Stack direction={{ xs: "column", md: "row" }} spacing={1.5}>
						<Box sx={{ flex: 1 }}>
							<TextField
								fullWidth
								label="Search by display name"
								value={searchInput}
								onChange={(event) => setSearchInput(event.target.value)}
							/>
						</Box>
						<Box sx={{ width: { xs: "100%", md: 180 } }}>
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
						</Box>
						<Box sx={{ width: { xs: "100%", md: 180 } }}>
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
						</Box>
						<Box sx={{ width: { xs: "100%", md: 180 } }}>
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
						</Box>
						<Box sx={{ width: { xs: "100%", md: 180 } }}>
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
						</Box>
					</Stack>
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
					<Paper variant="outlined" sx={{ p: 1.5, boxShadow: "none" }}>
						<Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
							{topLegendItems.map((item) => (
								<Chip
									key={item.label}
									icon={<Avatar sx={{ width: 20, height: 20, bgcolor: "transparent" }}>{item.icon}</Avatar>}
									label={item.label}
									variant="outlined"
									color={item.chipColor}
									size="small"
								/>
							))}
						</Stack>
					</Paper>

					<Paper variant="outlined" sx={{ boxShadow: "none" }}>
						<Table size="small">
							<TableHead>
								<TableRow>
									<TableCell>Rank</TableCell>
									<TableCell>Tier</TableCell>
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
								{rows.map((row) => {
									const tier = getRankTier(row.rank);

									return (
									<TableRow
										key={row.userId}
										hover
										sx={{
											background: tier.rowBackground,
											borderLeft: `3px solid ${tier.borderColor}`,
										}}
									>
										<TableCell>
											<Stack direction="row" spacing={1} alignItems="center">
												{tier.icon}
												<Typography fontWeight={row.rank <= 10 ? 700 : 500}>#{row.rank}</Typography>
											</Stack>
										</TableCell>
										<TableCell>
											<Chip
												size="small"
												label={tier.label}
												color={tier.chipColor}
												variant={row.rank <= 10 ? "filled" : "outlined"}
											/>
										</TableCell>
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
									);
								})}
								{rows.length === 0 ? (
									<TableRow>
										<TableCell colSpan={10} align="center">
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
