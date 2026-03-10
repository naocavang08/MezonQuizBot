import { useEffect, useState } from "react";
import {
	Alert,
	Box,
	Button,
	Checkbox,
	Chip,
	CircularProgress,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
	FormControlLabel,
	Paper,
	Stack,
	Switch,
	Table,
	TableBody,
	TableCell,
	TableHead,
	TableRow,
	TextField,
	Typography,
} from "@mui/material";
import {
	assignRolesToUser,
	createUser,
	deleteUser,
	getAllUsers,
	getUserRoles,
	updateUser,
} from "../../Api/user.api";
import { getAllRoles } from "../../Api/role.api";
import type { RoleResponse } from "../../Interface/role.dto";
import type { CreateUserRequest, UpdateUserRequest, UserResponse } from "../../Interface/user.dto";

const UserPage = () => {
	const [users, setUsers] = useState<UserResponse[]>([]);
	const [roles, setRoles] = useState<RoleResponse[]>([]);
	const [loading, setLoading] = useState(false);
	const [error, setError] = useState<string | null>(null);
	const [success, setSuccess] = useState<string | null>(null);

	const [openCreateDialog, setOpenCreateDialog] = useState(false);
	const [openEditDialog, setOpenEditDialog] = useState(false);
	const [openAssignRoleDialog, setOpenAssignRoleDialog] = useState(false);
	const [roleDialogLoading, setRoleDialogLoading] = useState(false);

	const [selectedUser, setSelectedUser] = useState<UserResponse | null>(null);
	const [selectedRoleIds, setSelectedRoleIds] = useState<string[]>([]);

	const [createForm, setCreateForm] = useState<CreateUserRequest>({
		email: "",
		username: "",
		password: "",
		displayName: "",
		avatarUrl: "",
	});

	const [editForm, setEditForm] = useState<UpdateUserRequest>({
		email: "",
		displayName: "",
		avatarUrl: "",
		isActive: true,
	});

	const fetchUsers = async () => {
		setLoading(true);
		try {
			const data = await getAllUsers();
			setUsers(data);
			setError(null);
		} catch {
			setError("Failed to load users.");
		} finally {
			setLoading(false);
		}
	};

	const fetchRoles = async () => {
		try {
			const data = await getAllRoles();
			setRoles(data);
		} catch {
			setError("Failed to load roles.");
		}
	};

	useEffect(() => {
		fetchUsers();
		fetchRoles();
	}, []);

	useEffect(() => {
		if (!success) {
			return;
		}

		const timer = setTimeout(() => setSuccess(null), 2500);
		return () => clearTimeout(timer);
	}, [success]);

	const handleOpenEditDialog = (user: UserResponse) => {
		setSelectedUser(user);
		setEditForm({
			email: user.email ?? "",
			displayName: user.displayName ?? "",
			avatarUrl: user.avatarUrl ?? "",
			isActive: user.isActive,
		});
		setOpenEditDialog(true);
	};

	const handleCreateUser = async () => {
		if (!createForm.username || !createForm.password) {
			setError("Username và Password là bắt buộc.");
			return;
		}

		setLoading(true);
		try {
			await createUser(createForm);
			setSuccess("Create user thành công.");
			setOpenCreateDialog(false);
			setCreateForm({
				email: "",
				username: "",
				password: "",
				displayName: "",
				avatarUrl: "",
			});
			await fetchUsers();
		} catch {
			setError("Create user thất bại.");
		} finally {
			setLoading(false);
		}
	};

	const handleUpdateUser = async () => {
		if (!selectedUser) {
			return;
		}

		setLoading(true);
		try {
			await updateUser(selectedUser.id, editForm);
			setSuccess("Update user thành công.");
			setOpenEditDialog(false);
			setSelectedUser(null);
			await fetchUsers();
		} catch {
			setError("Update user thất bại.");
		} finally {
			setLoading(false);
		}
	};

	const handleDeleteUser = async (id: string) => {
		if (!window.confirm("Bạn có chắc chắn muốn xoá user này?")) {
			return;
		}

		setLoading(true);
		try {
			await deleteUser(id);
			setSuccess("Delete user thành công.");
			await fetchUsers();
		} catch {
			setError("Delete user thất bại.");
		} finally {
			setLoading(false);
		}
	};

	const handleOpenAssignRoleDialog = async (user: UserResponse) => {
		setSelectedUser(user);
		setOpenAssignRoleDialog(true);
		setRoleDialogLoading(true);
		try {
			const roleIds = await getUserRoles(user.id);
			setSelectedRoleIds(Array.isArray(roleIds) ? roleIds : []);
		} catch {
			setSelectedRoleIds([]);
			setError("Không tải được danh sách role của user.");
		} finally {
			setRoleDialogLoading(false);
		}
	};

	const handleToggleRole = (roleId: string) => {
		setSelectedRoleIds((prev) =>
			prev.includes(roleId) ? prev.filter((id) => id !== roleId) : [...prev, roleId],
		);
	};

	const handleAssignRoles = async () => {
		if (!selectedUser) {
			return;
		}

		setLoading(true);
		try {
			await assignRolesToUser({ id: selectedUser.id, roleIds: selectedRoleIds });
			setSuccess("Assign roles thành công.");
			setOpenAssignRoleDialog(false);
			setSelectedUser(null);
			setSelectedRoleIds([]);
		} catch {
			setError("Assign roles thất bại.");
		} finally {
			setLoading(false);
		}
	};

	return (
		<Box>
			<Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
				<Typography variant="h5" fontWeight={700}>
					User Management
				</Typography>
				<Button variant="contained" onClick={() => setOpenCreateDialog(true)}>
					Add User
				</Button>
			</Box>

			{error && (
				<Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
					{error}
				</Alert>
			)}

			{success && (
				<Alert severity="success" sx={{ mb: 2 }} onClose={() => setSuccess(null)}>
					{success}
				</Alert>
			)}

			<Paper variant="outlined" sx={{ boxShadow: "none" }}>
				{loading && users.length === 0 ? (
					<Box py={6} display="flex" justifyContent="center">
						<CircularProgress />
					</Box>
				) : (
					<Table>
						<TableHead>
							<TableRow>
								<TableCell>Username</TableCell>
								<TableCell>Display Name</TableCell>
								<TableCell>Email</TableCell>
								<TableCell>Status</TableCell>
								<TableCell align="right">Actions</TableCell>
							</TableRow>
						</TableHead>
						<TableBody>
							{users.map((user) => (
								<TableRow key={user.id} hover>
									<TableCell>{user.username}</TableCell>
									<TableCell>{user.displayName || "-"}</TableCell>
									<TableCell>{user.email || "-"}</TableCell>
									<TableCell>
										<Chip
											label={user.isActive ? "Active" : "Inactive"}
											color={user.isActive ? "success" : "default"}
											size="small"
										/>
									</TableCell>
									<TableCell align="right">
										<Stack direction="row" spacing={1} justifyContent="flex-end">
											<Button size="small" variant="outlined" onClick={() => handleOpenEditDialog(user)}>
												Edit
											</Button>
											<Button
												size="small"
												variant="outlined"
												onClick={() => handleOpenAssignRoleDialog(user)}
											>
												Roles
											</Button>
											<Button
												size="small"
												variant="outlined"
												color="error"
												onClick={() => handleDeleteUser(user.id)}
											>
												Delete
											</Button>
										</Stack>
									</TableCell>
								</TableRow>
							))}
							{users.length === 0 && (
								<TableRow>
									<TableCell colSpan={5} align="center">
										No users found.
									</TableCell>
								</TableRow>
							)}
						</TableBody>
					</Table>
				)}
			</Paper>

			<Dialog open={openCreateDialog} onClose={() => setOpenCreateDialog(false)} maxWidth="sm" fullWidth>
				<DialogTitle>Create User</DialogTitle>
				<DialogContent>
					<Stack spacing={2} sx={{ mt: 1 }}>
						<TextField
							label="Username"
							value={createForm.username}
							onChange={(e) => setCreateForm((prev) => ({ ...prev, username: e.target.value }))}
							fullWidth
						/>
						<TextField
							label="Password"
							type="password"
							value={createForm.password}
							onChange={(e) => setCreateForm((prev) => ({ ...prev, password: e.target.value }))}
							fullWidth
						/>
						<TextField
							label="Email"
							value={createForm.email}
							onChange={(e) => setCreateForm((prev) => ({ ...prev, email: e.target.value }))}
							fullWidth
						/>
						<TextField
							label="Display Name"
							value={createForm.displayName}
							onChange={(e) => setCreateForm((prev) => ({ ...prev, displayName: e.target.value }))}
							fullWidth
						/>
						<TextField
							label="Avatar Url"
							value={createForm.avatarUrl}
							onChange={(e) => setCreateForm((prev) => ({ ...prev, avatarUrl: e.target.value }))}
							fullWidth
						/>
					</Stack>
				</DialogContent>
				<DialogActions>
					<Button onClick={() => setOpenCreateDialog(false)}>Cancel</Button>
					<Button variant="contained" onClick={handleCreateUser} disabled={loading}>
						Create
					</Button>
				</DialogActions>
			</Dialog>

			<Dialog open={openEditDialog} onClose={() => setOpenEditDialog(false)} maxWidth="sm" fullWidth>
				<DialogTitle>Update User</DialogTitle>
				<DialogContent>
					<Stack spacing={2} sx={{ mt: 1 }}>
						<TextField
							label="Email"
							value={editForm.email}
							onChange={(e) => setEditForm((prev) => ({ ...prev, email: e.target.value }))}
							fullWidth
						/>
						<TextField
							label="Display Name"
							value={editForm.displayName}
							onChange={(e) => setEditForm((prev) => ({ ...prev, displayName: e.target.value }))}
							fullWidth
						/>
						<TextField
							label="Avatar Url"
							value={editForm.avatarUrl}
							onChange={(e) => setEditForm((prev) => ({ ...prev, avatarUrl: e.target.value }))}
							fullWidth
						/>
						<FormControlLabel
							control={
								<Switch
									checked={editForm.isActive}
									onChange={(e) => setEditForm((prev) => ({ ...prev, isActive: e.target.checked }))}
								/>
							}
							label="Active"
						/>
					</Stack>
				</DialogContent>
				<DialogActions>
					<Button onClick={() => setOpenEditDialog(false)}>Cancel</Button>
					<Button variant="contained" onClick={handleUpdateUser} disabled={loading}>
						Save
					</Button>
				</DialogActions>
			</Dialog>

			<Dialog
				open={openAssignRoleDialog}
				onClose={() => {
					setOpenAssignRoleDialog(false);
					setSelectedRoleIds([]);
				}}
				maxWidth="sm"
				fullWidth
			>
				<DialogTitle>Assign Roles - {selectedUser?.username}</DialogTitle>
				<DialogContent>
					{roleDialogLoading ? (
						<Box py={4} display="flex" justifyContent="center">
							<CircularProgress size={24} />
						</Box>
					) : (
						<Stack spacing={1} sx={{ mt: 1 }}>
							{roles.map((role) => (
								<FormControlLabel
									key={role.id}
									control={<Checkbox checked={selectedRoleIds.includes(role.id)} onChange={() => handleToggleRole(role.id)} />}
									label={`${role.displayName || role.name} (${role.name})`}
								/>
							))}
							{roles.length === 0 && <Typography>No roles found.</Typography>}
						</Stack>
					)}
				</DialogContent>
				<DialogActions>
					<Button
						onClick={() => {
							setOpenAssignRoleDialog(false);
							setSelectedRoleIds([]);
						}}
					>
						Cancel
					</Button>
					<Button variant="contained" onClick={handleAssignRoles} disabled={loading || roleDialogLoading}>
						Save Roles
					</Button>
				</DialogActions>
			</Dialog>
		</Box>
	);
};

export default UserPage;
