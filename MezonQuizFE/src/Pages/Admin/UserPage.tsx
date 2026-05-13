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
	IconButton,
	InputAdornment,
	Paper,
	Stack,
	Switch,
	Table,
	TableBody,
	TableCell,
	TableContainer,
	TableHead,
	TableRow,
	TextField,
	Typography,
} from "@mui/material";
import { useForm } from "react-hook-form";
import { MdVisibility, MdVisibilityOff } from "react-icons/md";
import AppSnackbar from "../../Components/AppSnackbar";
import useAppSnackbar from "../../Hooks/useAppSnackbar";
import useRefresh from "../../Hooks/useRefresh";
import RefreshButton from "../../Components/RefreshButton";
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
import { getApiErrorMessage } from "../../Api/ApiClient";
import { getAllRoles } from "../../Api/role.api";
import type { RoleResponse } from "../../Interface/role.dto";
import type { CreateUserRequest, UpdateUserRequest, UserResponse } from "../../Interface/user.dto";

const getFirstErrorMessage = (errors: Record<string, { message?: string } | undefined>) =>
	Object.values(errors).find((error) => error?.message)?.message ?? "Invalid user data.";

const UserPage = () => {
	const { refreshKey } = useRefresh();
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
	const [showPassword, setShowPassword] = useState(false);

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
	const createMethods = useForm<CreateUserRequest>({
		defaultValues: {
			email: "",
			username: "",
			password: "",
			displayName: "",
			avatarUrl: "",
		},
	});
	const editMethods = useForm<UpdateUserRequest>({
		defaultValues: {
			email: "",
			displayName: "",
			avatarUrl: "",
			isActive: true,
		},
	});

	const fetchUsers = useCallback(async () => {
		setLoading(true);
		try {
			const data = await getAllUsers();
			setUsers(data);
		} catch (error) {
			showError(getApiErrorMessage(error, "Failed to load users."));
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
		} catch (error) {
			showError(getApiErrorMessage(error, "Failed to load roles."));
		}
	}, [canAssignRole, showError]);

	useEffect(() => {
		void fetchUsers();
		void fetchRoles();
	}, [fetchRoles, fetchUsers, refreshKey]);

	const handleOpenEditDialog = (user: UserResponse) => {
		if (!canUpdateUser) {
			showError("You do not have permission to update users.");
			return;
		}

		setSelectedUser(user);
		setEditForm({
			email: user.email ?? "",
			displayName: user.displayName ?? "",
			avatarUrl: user.avatarUrl ?? "",
			isActive: user.isActive,
		});
		editMethods.reset({
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
				showError("Failed to upload avatar.");
				return;
			}

			if (target === "create") {
				setCreateForm((prev) => ({ ...prev, avatarUrl: uploadedUrl }));
				createMethods.setValue("avatarUrl", uploadedUrl, { shouldValidate: false });
			} else {
				setEditForm((prev) => ({ ...prev, avatarUrl: uploadedUrl }));
				editMethods.setValue("avatarUrl", uploadedUrl, { shouldValidate: false });
			}

			showSuccess("Avatar uploaded successfully.");
		} catch (error) {
			showError(getApiErrorMessage(error, "Failed to upload avatar."));
		} finally {
			if (target === "create") {
				setUploadingCreateAvatar(false);
			} else {
				setUploadingEditAvatar(false);
			}
		}
	};

	const handleCreateUser = createMethods.handleSubmit(async (data) => {
		if (!canCreateUser) {
			showError("You do not have permission to create users.");
			return;
		}

		setLoading(true);
		try {
			await createUser({
				email: data.email?.trim(),
				username: data.username.trim(),
				password: data.password,
				displayName: data.displayName?.trim() || undefined,
				avatarUrl: data.avatarUrl?.trim() || undefined,
			});
			showSuccess("Create user successfully.");
			setOpenCreateDialog(false);
			setCreateForm({
				email: "",
				username: "",
				password: "",
				displayName: "",
				avatarUrl: "",
			});
			createMethods.reset({
				email: "",
				username: "",
				password: "",
				displayName: "",
				avatarUrl: "",
			});
			await fetchUsers();
		} catch (error) {
			showError(getApiErrorMessage(error, "Failed to create user."));
		} finally {
			setLoading(false);
		}
	}, () => showError(getFirstErrorMessage(createMethods.formState.errors as Record<string, { message?: string } | undefined>)));

	const handleUpdateUser = editMethods.handleSubmit(async (data) => {
		if (!canUpdateUser) {
			showError("You do not have permission to update users.");
			return;
		}

		if (!selectedUser) {
			return;
		}

		setLoading(true);
		try {
			await updateUser(selectedUser.id, {
				email: data.email?.trim(),
				displayName: data.displayName?.trim() || undefined,
				avatarUrl: data.avatarUrl?.trim() || undefined,
				isActive: data.isActive,
			});
			showSuccess("Update user successfully.");
			setOpenEditDialog(false);
			setSelectedUser(null);
			await fetchUsers();
		} catch (error) {
			showError(getApiErrorMessage(error, "Failed to update user."));
		} finally {
			setLoading(false);
		}
	}, () => showError(getFirstErrorMessage(editMethods.formState.errors as Record<string, { message?: string } | undefined>)));

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
			showSuccess("Delete user successfully.");
			await fetchUsers();
		} catch (error) {
			showError(getApiErrorMessage(error, "Failed to delete user."));
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
		} catch (error) {
			setSelectedRoleIds([]);
			showError(getApiErrorMessage(error, "Failed to load user roles."));
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
			showSuccess("Assign roles successfully.");
			setOpenAssignRoleDialog(false);
			setSelectedUser(null);
			setSelectedRoleIds([]);
		} catch (error) {
			showError(getApiErrorMessage(error, "Failed to assign roles."));
		} finally {
			setLoading(false);
		}
	};

	return (
		<Box>
			<Box
				sx={{
					display: 'flex',
					flexDirection: { xs: 'column', sm: 'row' },
					justifyContent: 'space-between',
					alignItems: { xs: 'stretch', sm: 'center' },
					gap: 2,
					mb: 3
				}}
			>
				<Stack direction="row" spacing={1} alignItems="center">
					<Typography variant="h5" fontWeight={700}>
						User Management
					</Typography>
					<RefreshButton size="small" disabled={loading} />
				</Stack>
				{canCreateUser ? (
					<Button
						variant="contained"
						fullWidth={false}
						sx={{ width: { xs: '100%', sm: 'auto' } }}
						onClick={() => {
							setCreateForm({
								email: "",
								username: "",
								password: "",
								displayName: "",
								avatarUrl: "",
							});
							createMethods.reset({
								email: "",
								username: "",
								password: "",
								displayName: "",
								avatarUrl: "",
							});
							setOpenCreateDialog(true);
						}}
					>
						Add User
					</Button>
				) : null}
			</Box>

			<TableContainer component={Paper} variant="outlined" sx={{ boxShadow: "none", width: '100%', overflowX: 'auto' }}>
				{loading && users.length === 0 ? (
					<Box py={6} display="flex" justifyContent="center">
						<CircularProgress />
					</Box>
				) : (
					<Table sx={{ minWidth: { xs: 500, sm: 650 } }}>
						<TableHead>
							<TableRow>
								<TableCell width={60}>Avatar</TableCell>
								<TableCell>Username</TableCell>
								<TableCell sx={{ display: { xs: 'none', md: 'table-cell' } }}>Display Name</TableCell>
								<TableCell sx={{ display: { xs: 'none', lg: 'table-cell' } }}>Email</TableCell>
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
									<TableCell>
										<Typography variant="body2" fontWeight={600}>
											{user.username}
										</Typography>
										<Typography variant="caption" color="text.secondary" sx={{ display: { xs: 'block', lg: 'none' } }}>
											{user.email}
										</Typography>
									</TableCell>
									<TableCell sx={{ display: { xs: 'none', md: 'table-cell' } }}>{user.displayName || "-"}</TableCell>
									<TableCell sx={{ display: { xs: 'none', lg: 'table-cell' } }}>{user.email || "-"}</TableCell>
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
									<TableCell colSpan={6} align="center" sx={{ py: 4 }}>
										No users found.
									</TableCell>
								</TableRow>
							)}
						</TableBody>
					</Table>
				)}
			</TableContainer>

			<Dialog open={openCreateDialog && canCreateUser} onClose={() => setOpenCreateDialog(false)} maxWidth="sm" fullWidth>
				<DialogTitle>Create User</DialogTitle>
				<DialogContent>
					<Stack spacing={2} sx={{ mt: 1 }}>
						<TextField
							label="Username"
							{...createMethods.register("username", {
								required: "Username is required.",
								validate: (value) => value.trim().length > 0 || "Username is required.",
							})}
							value={createForm.username}
							onChange={(e) => {
								setCreateForm((prev) => ({ ...prev, username: e.target.value }));
								createMethods.setValue("username", e.target.value, { shouldValidate: false });
							}}
							fullWidth
						/>
						<TextField
							label="Password"
							type={showPassword ? "text" : "password"}
							{...createMethods.register("password", {
								required: "Password is required.",
								validate: (value) => (value?.trim().length ?? 0) > 0 || "Password is required.",
							})}
							value={createForm.password}
							onChange={(e) => {
								setCreateForm((prev) => ({ ...prev, password: e.target.value }));
								createMethods.setValue("password", e.target.value, { shouldValidate: false });
							}}
							fullWidth
							slotProps={{
								input: {
									endAdornment: (
										<InputAdornment position="end">
											<IconButton
												aria-label="toggle password visibility"
												onClick={() => setShowPassword((prev) => !prev)}
												onMouseDown={(e) => e.preventDefault()}
												edge="end"
											>
												{showPassword ? <MdVisibilityOff /> : <MdVisibility />}
											</IconButton>
										</InputAdornment>
									)
								},
							}}
						/>
						<TextField
							label="Email"
							{...createMethods.register("email", {
								required: "Valid email is required.",
								pattern: {
									value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
									message: "Valid email is required.",
								},
								validate: (value) => (value?.trim().length ?? 0) > 0 || "Valid email is required.",
							})}
							value={createForm.email}
							onChange={(e) => {
								setCreateForm((prev) => ({ ...prev, email: e.target.value }));
								createMethods.setValue("email", e.target.value, { shouldValidate: false });
							}}
							fullWidth
						/>
						<TextField
							label="Display Name"
							{...createMethods.register("displayName")}
							value={createForm.displayName}
							onChange={(e) => {
								setCreateForm((prev) => ({ ...prev, displayName: e.target.value }));
								createMethods.setValue("displayName", e.target.value, { shouldValidate: false });
							}}
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
							{...editMethods.register("email", {
								required: "Valid email is required.",
								pattern: {
									value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
									message: "Valid email is required.",
								},
								validate: (value) => (value?.trim().length ?? 0) > 0 || "Valid email is required.",
							})}
							value={editForm.email}
							onChange={(e) => {
								setEditForm((prev) => ({ ...prev, email: e.target.value }));
								editMethods.setValue("email", e.target.value, { shouldValidate: false });
							}}
							fullWidth
						/>
						<TextField
							label="Display Name"
							{...editMethods.register("displayName")}
							value={editForm.displayName}
							onChange={(e) => {
								setEditForm((prev) => ({ ...prev, displayName: e.target.value }));
								editMethods.setValue("displayName", e.target.value, { shouldValidate: false });
							}}
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
									onChange={(e) => {
										setEditForm((prev) => ({ ...prev, isActive: e.target.checked }));
										editMethods.setValue("isActive", e.target.checked, { shouldValidate: false });
									}}
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
