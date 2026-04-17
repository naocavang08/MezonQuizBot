import { useCallback, useEffect, useState } from "react";
import {
	Avatar,
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
import AppSnackbar from "../../Components/AppSnackbar";
import useAppSnackbar from "../../Hooks/useAppSnackbar";
import useAuthStore from "../../Stores/login.store";
import { hasAnyPermission, PERMISSIONS } from "../../Lib/Utils/permissions";
import {
	assignRolesToUser,
	createUser,
	deleteUser,
	getAllUsers,
	getUserRoles,
	uploadUserAvatar,
	updateUser,
} from "../../Api/user.api";
import { getAllRoles } from "../../Api/role.api";
import type { RoleResponse } from "../../Interface/role.dto";
import type { CreateUserRequest, UpdateUserRequest, UserResponse } from "../../Interface/user.dto";

const UserPage = () => {
	const [users, setUsers] = useState<UserResponse[]>([]);
	const [roles, setRoles] = useState<RoleResponse[]>([]);
	const [loading, setLoading] = useState(false);
	const { snackbar, showError, showSuccess, closeSnackbar } = useAppSnackbar();
	const permissionName = useAuthStore((state) => state.permissionName);
	const hasSystemRole = useAuthStore((state) => state.hasSystemRole);
	const canCreateUser = hasAnyPermission(permissionName, [PERMISSIONS.USERS_CREATE], hasSystemRole);
	const canUpdateUser = hasAnyPermission(permissionName, [PERMISSIONS.USERS_UPDATE], hasSystemRole);
	const canDeleteUser = hasAnyPermission(permissionName, [PERMISSIONS.USERS_DELETE], hasSystemRole);
	const canAssignRole = hasAnyPermission(permissionName, [PERMISSIONS.USERS_ASSIGN_ROLE], hasSystemRole);
	const hasAnyRowAction = canUpdateUser || canDeleteUser || canAssignRole;

	const [openCreateDialog, setOpenCreateDialog] = useState(false);
	const [openEditDialog, setOpenEditDialog] = useState(false);
	const [openAssignRoleDialog, setOpenAssignRoleDialog] = useState(false);
	const [roleDialogLoading, setRoleDialogLoading] = useState(false);
	const [uploadingCreateAvatar, setUploadingCreateAvatar] = useState(false);
	const [uploadingEditAvatar, setUploadingEditAvatar] = useState(false);

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

	const isPasswordUser = (user: UserResponse) => Boolean(user.hasPassword) && !user.isOAuthUser;

	const fetchUsers = useCallback(async () => {
		setLoading(true);
		try {
			const data = await getAllUsers();
			setUsers(data);
		} catch {
			showError("Failed to load users.");
		} finally {
			setLoading(false);
		}
	}, [showError]);

	const fetchRoles = useCallback(async () => {
		if (!canAssignRole) {
			setRoles([]);
			return;
		}

		try {
			const data = await getAllRoles();
			setRoles(data);
		} catch {
			showError("Failed to load roles.");
		}
	}, [canAssignRole, showError]);

	useEffect(() => {
		void fetchUsers();
		void fetchRoles();
	}, [fetchRoles, fetchUsers]);

	const handleOpenEditDialog = (user: UserResponse) => {
		if (!canUpdateUser) {
			showError("You do not have permission to update users.");
			return;
		}

		if (!isPasswordUser(user)) {
			showError("User đăng nhập bằng OAuth2 không hỗ trợ edit trong màn hình này.");
			return;
		}

		setSelectedUser(user);
		setEditForm({
			email: user.email ?? "",
			displayName: user.displayName ?? "",
			avatarUrl: user.avatarUrl ?? "",
			isActive: user.isActive,
		});
		setOpenEditDialog(true);
	};

	const handleUploadAvatar = async (file: File, target: "create" | "edit") => {
		if (!file) {
			return;
		}

		try {
			if (target === "create") {
				setUploadingCreateAvatar(true);
			} else {
				setUploadingEditAvatar(true);
			}

			const uploadedUrl = await uploadUserAvatar(file);
			if (!uploadedUrl) {
				showError("Upload thành công nhưng không nhận được avatar URL.");
				return;
			}

			if (target === "create") {
				setCreateForm((prev) => ({ ...prev, avatarUrl: uploadedUrl }));
			} else {
				setEditForm((prev) => ({ ...prev, avatarUrl: uploadedUrl }));
			}

			showSuccess("Upload avatar thành công.");
		} catch {
			showError("Upload avatar thất bại.");
		} finally {
			if (target === "create") {
				setUploadingCreateAvatar(false);
			} else {
				setUploadingEditAvatar(false);
			}
		}
	};

	const handleCreateUser = async () => {
		if (!canCreateUser) {
			showError("You do not have permission to create users.");
			return;
		}

		if (!createForm.username || !createForm.password) {
			showError("Username và Password là bắt buộc.");
			return;
		}

		setLoading(true);
		try {
			await createUser(createForm);
			showSuccess("Create user thành công.");
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
			showError("Create user thất bại.");
		} finally {
			setLoading(false);
		}
	};

	const handleUpdateUser = async () => {
		if (!canUpdateUser) {
			showError("You do not have permission to update users.");
			return;
		}

		if (!selectedUser) {
			return;
		}

		setLoading(true);
		try {
			await updateUser(selectedUser.id, editForm);
			showSuccess("Update user thành công.");
			setOpenEditDialog(false);
			setSelectedUser(null);
			await fetchUsers();
		} catch {
			showError("Update user thất bại.");
		} finally {
			setLoading(false);
		}
	};

	const handleDeleteUser = async (id: string) => {
		if (!canDeleteUser) {
			showError("You do not have permission to delete users.");
			return;
		}

		if (!window.confirm("Bạn có chắc chắn muốn xoá user này?")) {
			return;
		}

		setLoading(true);
		try {
			await deleteUser(id);
			showSuccess("Delete user thành công.");
			await fetchUsers();
		} catch {
			showError("Delete user thất bại.");
		} finally {
			setLoading(false);
		}
	};

	const handleOpenAssignRoleDialog = async (user: UserResponse) => {
		if (!canAssignRole) {
			showError("You do not have permission to assign roles.");
			return;
		}

		setSelectedUser(user);
		setOpenAssignRoleDialog(true);
		setRoleDialogLoading(true);
		try {
			const roleIds = await getUserRoles(user.id);
			setSelectedRoleIds(Array.isArray(roleIds) ? roleIds : []);
		} catch {
			setSelectedRoleIds([]);
			showError("Không tải được danh sách role của user.");
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
		if (!canAssignRole) {
			showError("You do not have permission to assign roles.");
			return;
		}

		if (!selectedUser) {
			return;
		}

		setLoading(true);
		try {
			await assignRolesToUser({ id: selectedUser.id, roleIds: selectedRoleIds });
			showSuccess("Assign roles thành công.");
			setOpenAssignRoleDialog(false);
			setSelectedUser(null);
			setSelectedRoleIds([]);
		} catch {
			showError("Assign roles thất bại.");
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
				{canCreateUser ? (
					<Button variant="contained" onClick={() => setOpenCreateDialog(true)}>
						Add User
					</Button>
				) : null}
			</Box>

			<Paper variant="outlined" sx={{ boxShadow: "none" }}>
				{loading && users.length === 0 ? (
					<Box py={6} display="flex" justifyContent="center">
						<CircularProgress />
					</Box>
				) : (
					<Table>
						<TableHead>
							<TableRow>
								<TableCell>Avatar</TableCell>
								<TableCell>Username</TableCell>
								<TableCell>Display Name</TableCell>
								<TableCell>Email</TableCell>
								<TableCell>Status</TableCell>
								{hasAnyRowAction ? <TableCell align="right">Actions</TableCell> : null}
							</TableRow>
						</TableHead>
						<TableBody>
							{users.map((user) => (
								<TableRow key={user.id} hover>
									<TableCell>
										<Avatar
											src={user.avatarUrl}
											sx={{ width: 32, height: 32, bgcolor: "primary.main", fontSize: 12, fontWeight: 700 }}
										>
											{(user.displayName || user.username || "U").slice(0, 2).toUpperCase()}
										</Avatar>
									</TableCell>
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
									{hasAnyRowAction ? (
										<TableCell align="right">
											<Stack direction="row" spacing={1} justifyContent="flex-end">
												{canUpdateUser ? (
													<Button
														size="small"
														variant="outlined"
														disabled={!isPasswordUser(user)}
														onClick={() => handleOpenEditDialog(user)}
													>
														Edit
													</Button>
												) : null}
												{canAssignRole ? (
													<Button
														size="small"
														variant="outlined"
														onClick={() => handleOpenAssignRoleDialog(user)}
													>
														Roles
													</Button>
												) : null}
												{canDeleteUser ? (
													<Button
														size="small"
														variant="outlined"
														color="error"
														onClick={() => handleDeleteUser(user.id)}
													>
														Delete
													</Button>
												) : null}
											</Stack>
										</TableCell>
									) : null}
								</TableRow>
							))}
							{users.length === 0 && (
								<TableRow>
									<TableCell colSpan={hasAnyRowAction ? 6 : 5} align="center">
										No users found.
									</TableCell>
								</TableRow>
							)}
						</TableBody>
					</Table>
				)}
			</Paper>

			<Dialog open={openCreateDialog && canCreateUser} onClose={() => setOpenCreateDialog(false)} maxWidth="sm" fullWidth>
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
						<Stack direction="row" spacing={2} alignItems="center" flexWrap="wrap" useFlexGap>
							<Avatar
								src={createForm.avatarUrl}
								sx={{ width: 56, height: 56, bgcolor: "primary.main" }}
							>
								{(createForm.displayName || createForm.username || "U").slice(0, 2).toUpperCase()}
							</Avatar>
							<Button component="label" variant="outlined" disabled={uploadingCreateAvatar}>
								{uploadingCreateAvatar ? "Uploading..." : "Upload Avatar"}
								<input
									hidden
									type="file"
									accept="image/*"
									onChange={(event) => {
										const file = event.target.files?.[0];
										if (!file) return;
										void handleUploadAvatar(file, "create");
										event.currentTarget.value = "";
									}}
								/>
							</Button>
						</Stack>
						{createForm.avatarUrl ? (
							<TextField
								label="Avatar URL"
								value={createForm.avatarUrl}
								fullWidth
								InputProps={{ readOnly: true }}
							/>
						) : null}
					</Stack>
				</DialogContent>
				<DialogActions>
					<Button onClick={() => setOpenCreateDialog(false)}>Cancel</Button>
					<Button variant="contained" onClick={handleCreateUser} disabled={loading}>
						Create
					</Button>
				</DialogActions>
			</Dialog>

			<Dialog open={openEditDialog && canUpdateUser} onClose={() => setOpenEditDialog(false)} maxWidth="sm" fullWidth>
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
						<Stack direction="row" spacing={2} alignItems="center" flexWrap="wrap" useFlexGap>
							<Avatar
								src={editForm.avatarUrl}
								sx={{ width: 56, height: 56, bgcolor: "primary.main" }}
							>
								{(editForm.displayName || selectedUser?.username || "U").slice(0, 2).toUpperCase()}
							</Avatar>
							<Button component="label" variant="outlined" disabled={uploadingEditAvatar}>
								{uploadingEditAvatar ? "Uploading..." : "Upload Avatar"}
								<input
									hidden
									type="file"
									accept="image/*"
									onChange={(event) => {
										const file = event.target.files?.[0];
										if (!file) return;
										void handleUploadAvatar(file, "edit");
										event.currentTarget.value = "";
									}}
								/>
							</Button>
						</Stack>
						{editForm.avatarUrl ? (
							<TextField
								label="Avatar URL"
								value={editForm.avatarUrl}
								fullWidth
								InputProps={{ readOnly: true }}
							/>
						) : null}
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
					<Button variant="contained" onClick={handleUpdateUser} disabled={loading || uploadingEditAvatar}>
						Save
					</Button>
				</DialogActions>
			</Dialog>

			<Dialog
				open={openAssignRoleDialog && canAssignRole}
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

			<AppSnackbar
				open={snackbar.open}
				message={snackbar.message}
				severity={snackbar.severity}
				onClose={closeSnackbar}
			/>
		</Box>
	);
};

export default UserPage;
