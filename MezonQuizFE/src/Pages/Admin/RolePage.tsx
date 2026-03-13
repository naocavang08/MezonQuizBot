import { useState, useEffect } from "react";
import {
	Box,
	Button,
	Checkbox,
	Chip,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
	Paper,
	Stack,
	TextField,
	Typography,
	CircularProgress,
	FormControlLabel,
} from "@mui/material";
import AppSnackbar from "../../Components/AppSnackbar";
import useAppSnackbar from "../../Hooks/useAppSnackbar";
import { getAllRoles, getAllPermissions, getRolePermissions, assignPermissionsToRole, deleteRole, createRole } from "../../Api/role.api";
import type { RoleResponse, PermissionResponse, RoleRequest } from "../../Interface/role.dto";

const RolePage = () => {
	const [roles, setRoles] = useState<RoleResponse[]>([]);
	const [permissions, setPermissions] = useState<PermissionResponse[]>([]);
	const [loading, setLoading] = useState(false);
	const { snackbar, showError, showSuccess, closeSnackbar } = useAppSnackbar();

	const [openPermissionDialog, setOpenPermissionDialog] = useState(false);
	const [openCreateRoleDialog, setOpenCreateRoleDialog] = useState(false);
	const [selectedRole, setSelectedRole] = useState<RoleResponse | null>(null);
	const [selectedPermissions, setSelectedPermissions] = useState<string[]>([]);
	const [permissionLoading, setPermissionLoading] = useState(false);

	const [newRole, setNewRole] = useState<RoleRequest>({
		name: "",
		displayName: "",
		description: "",
		isSystem: false,
	});

	const fetchRoles = async () => {
		setLoading(true);
		try {
			const data = await getAllRoles();
			setRoles(data);
		} catch (err) {
			showError("Failed to load roles");
			console.error(err);
		} finally {
			setLoading(false);
		}
	};

	const fetchPermissions = async () => {
		try {
			const data = await getAllPermissions();
			setPermissions(data);
		} catch (err) {
			showError("Failed to load permissions");
			console.error(err);
		}
	};

	const fetchRolePermissions = async (roleId: string) => {
		setPermissionLoading(true);
		try {
			const data = await getRolePermissions(roleId);
			// Handle different response types
			if (Array.isArray(data)) {
				setSelectedPermissions(data);
			} else if (typeof data === "string") {
				// Try to parse if it's a JSON string
				if (data.startsWith("[")) {
					try {
						const parsed = JSON.parse(data);
						setSelectedPermissions(Array.isArray(parsed) ? parsed : []);
					} catch {
						setSelectedPermissions([data].filter(Boolean));
					}
				} else {
					setSelectedPermissions([data].filter(Boolean));
				}
			} else if (data && typeof data === "object") {
				setSelectedPermissions([]);
			} else {
				setSelectedPermissions([]);
			}
		} catch (err) {
			console.error("Failed to fetch role permissions:", err);
			setSelectedPermissions([]);
		} finally {
			setPermissionLoading(false);
		}
	};

	useEffect(() => {
		fetchRoles();
		fetchPermissions();
	}, []);

	const handleEditPermissions = async (role: RoleResponse) => {
		setSelectedRole(role);
		setOpenPermissionDialog(true);
		await fetchRolePermissions(role.id);
	};

	const handleSavePermissions = async () => {
		if (!selectedRole) return;

		setPermissionLoading(true);
		try {
			await assignPermissionsToRole({
				id: selectedRole.id,
				permissions: selectedPermissions,
			});
			showSuccess(`Permissions updated for ${selectedRole.displayName}`);
			setOpenPermissionDialog(false);
			setSelectedRole(null);
			setSelectedPermissions([]);

			await fetchRoles();
		} catch (err) {
			showError("Failed to save permissions");
			console.error(err);
		} finally {
			setPermissionLoading(false);
		}
	};

	const handleCreateRole = async () => {
		if (!newRole.name || !newRole.displayName) {
			showError("Name and Display Name are required");
			return;
		}

		setLoading(true);
		try {
			await createRole(newRole);
			showSuccess(`Role ${newRole.displayName} created successfully`);
			setOpenCreateRoleDialog(false);
			setNewRole({
				name: "",
				displayName: "",
				description: "",
				isSystem: false,
			});
			await fetchRoles();
		} catch (err) {
			showError("Failed to create role");
			console.error(err);
		} finally {
			setLoading(false);
		}
	};

	const handleDeleteRole = async (roleId: string, roleName: string) => {
		if (window.confirm(`Are you sure you want to delete "${roleName}"?`)) {
			setLoading(true);
			try {
				await deleteRole(roleId);
				showSuccess(`Role ${roleName} deleted successfully`);
				await fetchRoles();
			} catch (err) {
				showError("Failed to delete role");
				console.error(err);
			} finally {
				setLoading(false);
			}
		}
	};

	const handlePermissionChange = (permissionId: string) => {
		setSelectedPermissions((prev) =>
			prev.includes(permissionId)
				? prev.filter((p) => p !== permissionId)
				: [...prev, permissionId]
		);
	};

	const handleClosePermissionDialog = () => {
		setOpenPermissionDialog(false);
		setSelectedRole(null);
		setSelectedPermissions([]);
	};

	if (loading && roles.length === 0) {
		return (
			<Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
				<CircularProgress />
			</Box>
		);
	}

	return (
		<Box>
			<Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
				<Typography variant="h5" fontWeight={700}>
					Role Management
				</Typography>
				<Button
					variant="contained"
					onClick={() => setOpenCreateRoleDialog(true)}
				>
					Add Role
				</Button>
			</Box>

			<Stack spacing={1.5}>
				{roles.length === 0 ? (
					<Typography color="textSecondary">No roles found</Typography>
				) : (
					roles.map((role) => (
						<Paper
							key={role.id}
							sx={{
								p: 2,
								border: "1px solid #e5e7eb",
								boxShadow: "none",
								display: "flex",
								justifyContent: "space-between",
								alignItems: "center",
							}}
						>
							<Box flex={1}>
								<Typography fontWeight={600}>{role.displayName}</Typography>
								<Typography variant="body2" color="textSecondary">
									{role.name}
								</Typography>
								{role.description && (
									<Typography variant="body2" color="textSecondary" sx={{ mt: 0.5 }}>
										{role.description}
									</Typography>
								)}
							</Box>

							<Stack direction="row" spacing={1} alignItems="center">
								<Chip
									label={role.isSystem ? "System" : "Custom"}
									color={role.isSystem ? "primary" : "default"}
									size="small"
								/>
								<Button
									variant="outlined"
									size="small"
									onClick={() => handleEditPermissions(role)}
								>
									Permissions
								</Button>
								<Button
									variant="outlined"
									color="error"
									size="small"
									onClick={() => handleDeleteRole(role.id, role.displayName || role.name)}
									disabled={role.isSystem}
									title={role.isSystem ? "Cannot delete system roles" : ""}
								>
									Delete
								</Button>
							</Stack>
						</Paper>
					))
				)}
			</Stack>

			<Dialog
				open={openPermissionDialog}
				onClose={handleClosePermissionDialog}
				maxWidth="sm"
				fullWidth
			>
				<DialogTitle>
					Edit Permissions - {selectedRole?.displayName}
				</DialogTitle>
				<DialogContent>
					{permissionLoading ? (
						<Box display="flex" justifyContent="center" py={3}>
							<CircularProgress />
						</Box>
					) : (
						<Stack spacing={2} sx={{ mt: 1 }}>
							{permissions.length === 0 ? (
								<Typography color="textSecondary">No permissions available</Typography>
							) : (
								permissions.map((permission) => (
									<FormControlLabel
										key={permission.id}
										checked={selectedPermissions.includes(permission.id)}
										onChange={() => handlePermissionChange(permission.id)}
										control={<Checkbox />}
										label={
											<Box>
												<Typography variant="body2" fontWeight={500}>
													{permission.resource} - {permission.action}
												</Typography>
												{permission.description && (
													<Typography variant="caption" color="textSecondary">
														{permission.description}
													</Typography>
												)}
											</Box>
										}
									/>
								))
							)}
						</Stack>
					)}
				</DialogContent>
				<DialogActions>
					<Button onClick={handleClosePermissionDialog}>Cancel</Button>
					<Button
						onClick={handleSavePermissions}
						variant="contained"
						disabled={permissionLoading}
					>
						Save
					</Button>
				</DialogActions>
			</Dialog>

			<Dialog
				open={openCreateRoleDialog}
				onClose={() => setOpenCreateRoleDialog(false)}
				maxWidth="sm"
				fullWidth
			>
				<DialogTitle>Create New Role</DialogTitle>
				<DialogContent>
					<Stack spacing={2} sx={{ mt: 2 }}>
						<TextField
							label="Name"
							placeholder="e.g., editor"
							fullWidth
							value={newRole.name}
							onChange={(e) =>
								setNewRole({ ...newRole, name: e.target.value })
							}
							disabled={loading}
						/>
						<TextField
							label="Display Name"
							placeholder="e.g., Editor"
							fullWidth
							value={newRole.displayName}
							onChange={(e) =>
								setNewRole({ ...newRole, displayName: e.target.value })
							}
							disabled={loading}
						/>
						<TextField
							label="Description"
							placeholder="Optional description"
							fullWidth
							multiline
							rows={3}
							value={newRole.description}
							onChange={(e) =>
								setNewRole({ ...newRole, description: e.target.value })
							}
							disabled={loading}
						/>
					</Stack>
				</DialogContent>
				<DialogActions>
					<Button onClick={() => setOpenCreateRoleDialog(false)}>Cancel</Button>
					<Button
						onClick={handleCreateRole}
						variant="contained"
						disabled={loading}
					>
						Create
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

export default RolePage;
