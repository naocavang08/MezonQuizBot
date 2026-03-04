import { Chip, Paper, Table, TableBody, TableCell, TableHead, TableRow, Typography } from "@mui/material";

const users = [
	{ id: "U001", name: "Super Admin", email: "superadmin@ncc.asia", status: "Active" },
	{ id: "U002", name: "Alice", email: "alice@ncc.asia", status: "Active" },
	{ id: "U003", name: "Bob", email: "bob@ncc.asia", status: "Inactive" }
];

const UserPage = () => {
	return (
		<>
			<Typography variant="h5" fontWeight={700} mb={2}>
				User Management
			</Typography>

			<Paper sx={{ border: "1px solid #e5e7eb", boxShadow: "none" }}>
				<Table>
					<TableHead>
						<TableRow>
							<TableCell>ID</TableCell>
							<TableCell>Name</TableCell>
							<TableCell>Email</TableCell>
							<TableCell>Status</TableCell>
						</TableRow>
					</TableHead>
					<TableBody>
						{users.map((user) => (
							<TableRow key={user.id}>
								<TableCell>{user.id}</TableCell>
								<TableCell>{user.name}</TableCell>
								<TableCell>{user.email}</TableCell>
								<TableCell>
									<Chip
										label={user.status}
										color={user.status === "Active" ? "success" : "default"}
										size="small"
									/>
								</TableCell>
							</TableRow>
						))}
					</TableBody>
				</Table>
			</Paper>
		</>
	);
};

export default UserPage;
