import { useEffect, useState } from "react";
import {
	Box,
	Button,
	CircularProgress,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
	MenuItem,
	Paper,
	Select,
	Stack,
	Table,
	TableBody,
	TableCell,
	TableHead,
	TableRow,
	TextField,
	Typography,
} from "@mui/material";
import { useForm } from "react-hook-form";
import { getApiErrorMessage } from "../../Api/ApiClient";
import AppSnackbar from "../../Components/AppSnackbar";
import useAppSnackbar from "../../Hooks/useAppSnackbar";
import useRefresh from "../../Hooks/useRefresh";
import RefreshButton from "../../Components/RefreshButton";
import useAuthStore from "../../Stores/login.store";
import { hasAnyPermission, PERMISSIONS } from "../../Lib/Utils/permissions";
import { createCategory, deleteCategory, getAllCategories, updateCategory } from "../../Api/category.api";
import type { CategoryDto, SaveCategoryDto } from "../../Interface/category.dto";
import { CATEGORY_ICON_OPTIONS, getCategoryIconOption } from "../../Lib/Utils/categoryIconOptions";
import CategoryIconBadge from "../../Lib/Utils/categoryIconBadge";

const defaultForm: SaveCategoryDto = {
	name: "",
	slug: "",
	icon: "",
	sortOrder: 0,
};

const getFirstErrorMessage = (errors: Record<string, { message?: string } | undefined>) =>
	Object.values(errors).find((error) => error?.message)?.message ?? "Invalid category data.";

const CategoryPage = () => {
	const { refreshKey } = useRefresh();
	const [categories, setCategories] = useState<CategoryDto[]>([]);
	const [loading, setLoading] = useState(false);
	const { snackbar, showError, showSuccess, closeSnackbar } = useAppSnackbar();
	const permissionName = useAuthStore((state) => state.permissionName);
	const hasSystemRole = useAuthStore((state) => state.hasSystemRole);
	const canCreateCategory = hasAnyPermission(permissionName, [PERMISSIONS.CATEGORIES_CREATE], hasSystemRole);
	const canUpdateCategory = hasAnyPermission(permissionName, [PERMISSIONS.CATEGORIES_UPDATE], hasSystemRole);
	const canDeleteCategory = hasAnyPermission(permissionName, [PERMISSIONS.CATEGORIES_DELETE], hasSystemRole);
	const hasAnyRowAction = canUpdateCategory || canDeleteCategory;

	const [openCreateDialog, setOpenCreateDialog] = useState(false);
	const [openEditDialog, setOpenEditDialog] = useState(false);
	const [selectedCategory, setSelectedCategory] = useState<CategoryDto | null>(null);

	const [createForm, setCreateForm] = useState<SaveCategoryDto>(defaultForm);
	const [editForm, setEditForm] = useState<SaveCategoryDto>(defaultForm);
	const createMethods = useForm<SaveCategoryDto>({ defaultValues: defaultForm });
	const editMethods = useForm<SaveCategoryDto>({ defaultValues: defaultForm });

	const fetchCategories = async () => {
		setLoading(true);
		try {
			const data = await getAllCategories();
			setCategories(data);
		} catch (error) {
			showError(getApiErrorMessage(error, "Failed to fetch categories."));
		} finally {
			setLoading(false);
		}
	};

	useEffect(() => {
		fetchCategories();
	}, [refreshKey]);

	const normalizeForm = (form: SaveCategoryDto): SaveCategoryDto => ({
		name: form.name.trim(),
		slug: form.slug.trim(),
		icon: form.icon?.trim() || undefined,
		sortOrder: typeof form.sortOrder === "number" ? form.sortOrder : 0,
	});

	const handleCreateCategory = createMethods.handleSubmit(async (data) => {
		if (!canCreateCategory) {
			showError("You do not have permission to create categories.");
			return;
		}

		const createIconKey = data.icon?.trim();
		if (createIconKey && !getCategoryIconOption(createIconKey)) {
			showError("Icon is not valid. Please select from the list.");
			return;
		}

		setLoading(true);
		try {
			await createCategory(normalizeForm(data));
			showSuccess("Create category successfully.");
			setOpenCreateDialog(false);
			setCreateForm(defaultForm);
			createMethods.reset(defaultForm);
			await fetchCategories();
		} catch (error) {
			showError(getApiErrorMessage(error, "Failed to create category."));
		} finally {
			setLoading(false);
		}
	}, () => showError(getFirstErrorMessage(createMethods.formState.errors as Record<string, { message?: string } | undefined>)));

	const handleOpenEditDialog = (category: CategoryDto) => {
		if (!canUpdateCategory) {
			showError("You do not have permission to update categories.");
			return;
		}

		setSelectedCategory(category);
		setEditForm({
			name: category.name,
			slug: category.slug,
			icon: category.icon ?? "",
			sortOrder: category.sortOrder ?? 0,
		});
		editMethods.reset({
			name: category.name,
			slug: category.slug,
			icon: category.icon ?? "",
			sortOrder: category.sortOrder ?? 0,
		});
		setOpenEditDialog(true);
	};

	const handleUpdateCategory = editMethods.handleSubmit(async (data) => {
		if (!canUpdateCategory) {
			showError("You do not have permission to update categories.");
			return;
		}

		if (!selectedCategory) {
			return;
		}

		const editIconKey = data.icon?.trim();
		if (editIconKey && !getCategoryIconOption(editIconKey)) {
			showError("Icon is not valid. Please select from the list.");
			return;
		}

		setLoading(true);
		try {
			await updateCategory(selectedCategory.id, normalizeForm(data));
			showSuccess("Update category successfully.");
			setOpenEditDialog(false);
			setSelectedCategory(null);
			await fetchCategories();
		} catch (error) {
			showError(getApiErrorMessage(error, "Failed to update category."));
		} finally {
			setLoading(false);
		}
	}, () => showError(getFirstErrorMessage(editMethods.formState.errors as Record<string, { message?: string } | undefined>)));

	const handleDeleteCategory = async (categoryId: string, categoryName: string) => {
		if (!canDeleteCategory) {
			showError("You do not have permission to delete categories.");
			return;
		}

		if (!window.confirm(`Are you sure you want to delete the category \\"${categoryName}\\"?`)) {
			return;
		}

		setLoading(true);
		try {
			await deleteCategory(categoryId);
			showSuccess("Delete category successfully.");
			await fetchCategories();
		} catch (error) {
			showError(getApiErrorMessage(error, "Failed to delete category."));
		} finally {
			setLoading(false);
		}
	};

	return (
		<Box>
			<Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
				<Stack direction="row" spacing={1} alignItems="center">
					<Typography variant="h5" fontWeight={700}>
						Category Management
					</Typography>
					<RefreshButton size="small" disabled={loading} />
				</Stack>
				{canCreateCategory ? (
					<Button variant="contained" onClick={() => {
						setCreateForm(defaultForm);
						createMethods.reset(defaultForm);
						setOpenCreateDialog(true);
					}}>
						Add Category
					</Button>
				) : null}
			</Box>

			<Paper variant="outlined" sx={{ boxShadow: "none" }}>
				{loading && categories.length === 0 ? (
					<Box py={6} display="flex" justifyContent="center">
						<CircularProgress />
					</Box>
				) : (
					<Table>
						<TableHead>
							<TableRow>
								<TableCell>Name</TableCell>
								<TableCell>Slug</TableCell>
								<TableCell>Icon</TableCell>
								<TableCell>Sort Order</TableCell>
								{hasAnyRowAction ? <TableCell align="right">Actions</TableCell> : null}
							</TableRow>
						</TableHead>
						<TableBody>
							{categories.map((category) => (
								<TableRow key={category.id} hover>
									<TableCell>{category.name}</TableCell>
									<TableCell>{category.slug}</TableCell>
									<TableCell>
										<Stack direction="row" spacing={1} alignItems="center">
											<CategoryIconBadge iconKey={category.icon} fallback="-" />
											<Typography variant="body2" color="text.secondary">
												{category.icon || "No icon"}
											</Typography>
										</Stack>
									</TableCell>
									<TableCell>{category.sortOrder ?? 0}</TableCell>
									{hasAnyRowAction ? (
										<TableCell align="right">
											<Stack direction="row" spacing={1} justifyContent="flex-end">
												{canUpdateCategory ? (
													<Button size="small" variant="outlined" onClick={() => handleOpenEditDialog(category)}>
														Edit
													</Button>
												) : null}
												{canDeleteCategory ? (
													<Button
														size="small"
														variant="outlined"
														color="error"
														onClick={() => handleDeleteCategory(category.id, category.name)}
													>
														Delete
													</Button>
												) : null}
											</Stack>
										</TableCell>
									) : null}
								</TableRow>
							))}
							{categories.length === 0 && (
								<TableRow>
									<TableCell colSpan={hasAnyRowAction ? 5 : 4} align="center">
										Chưa có category nào.
									</TableCell>
								</TableRow>
							)}
						</TableBody>
					</Table>
				)}
			</Paper>

			<Dialog open={openCreateDialog && canCreateCategory} onClose={() => setOpenCreateDialog(false)} maxWidth="sm" fullWidth>
				<DialogTitle>Create Category</DialogTitle>
				<DialogContent>
					<Stack spacing={2} sx={{ mt: 1 }}>
						<TextField
							label="Name"
							{...createMethods.register("name", {
								required: "Category name is required.",
								validate: (value) => value.trim().length > 0 || "Category name is required.",
							})}
							value={createForm.name}
							onChange={(e) => {
								setCreateForm((prev) => ({ ...prev, name: e.target.value }));
								createMethods.setValue("name", e.target.value, { shouldValidate: false });
							}}
							fullWidth
						/>
						<TextField
							label="Slug"
							{...createMethods.register("slug", {
								required: "Slug is required.",
								validate: (value) => value.trim().length > 0 || "Slug is required.",
							})}
							value={createForm.slug}
							onChange={(e) => {
								setCreateForm((prev) => ({ ...prev, slug: e.target.value }));
								createMethods.setValue("slug", e.target.value, { shouldValidate: false });
							}}
							fullWidth
						/>
						<Select
							displayEmpty
							fullWidth
							value={createForm.icon || ""}
							onChange={(e) => {
								setCreateForm((prev) => ({ ...prev, icon: String(e.target.value) }));
								createMethods.setValue("icon", String(e.target.value), { shouldValidate: false });
							}}
							renderValue={(value) => {
								const selectedIcon = String(value);
								const selectedOption = getCategoryIconOption(selectedIcon);

								if (!selectedOption) {
									return <Typography color="text.secondary">Select icon</Typography>;
								}

								return (
									<Stack direction="row" spacing={1} alignItems="center">
										<CategoryIconBadge iconKey={selectedOption.key} fallback={null} />
										<Typography>{selectedOption.label}</Typography>
									</Stack>
								);
							}}
						>
							<MenuItem value="">
								No icon
							</MenuItem>
							{CATEGORY_ICON_OPTIONS.map((option) => (
								<MenuItem key={option.key} value={option.key}>
									<Stack direction="row" spacing={1} alignItems="center">
										<CategoryIconBadge iconKey={option.key} fallback={null} />
										<Typography>{option.label}</Typography>
									</Stack>
								</MenuItem>
							))}
						</Select>
						<TextField
							label="Sort Order"
							type="number"
							value={createForm.sortOrder ?? 0}
							{...createMethods.register("sortOrder")}
							onChange={(e) => {
								const value = Number(e.target.value) || 0;
								setCreateForm((prev) => ({ ...prev, sortOrder: value }));
								createMethods.setValue("sortOrder", value, { shouldValidate: false });
							}}
							fullWidth
						/>
					</Stack>
				</DialogContent>
				<DialogActions>
					<Button onClick={() => setOpenCreateDialog(false)}>Cancel</Button>
					<Button onClick={handleCreateCategory} variant="contained" disabled={loading}>
						Create
					</Button>
				</DialogActions>
			</Dialog>

			<Dialog open={openEditDialog && canUpdateCategory} onClose={() => setOpenEditDialog(false)} maxWidth="sm" fullWidth>
				<DialogTitle>Edit Category</DialogTitle>
				<DialogContent>
					<Stack spacing={2} sx={{ mt: 1 }}>
						<TextField
							label="Name"
							{...editMethods.register("name", {
								required: "Category name is required.",
								validate: (value) => value.trim().length > 0 || "Category name is required.",
							})}
							value={editForm.name}
							onChange={(e) => {
								setEditForm((prev) => ({ ...prev, name: e.target.value }));
								editMethods.setValue("name", e.target.value, { shouldValidate: false });
							}}
							fullWidth
						/>
						<TextField
							label="Slug"
							{...editMethods.register("slug", {
								required: "Slug is required.",
								validate: (value) => value.trim().length > 0 || "Slug is required.",
							})}
							value={editForm.slug}
							onChange={(e) => {
								setEditForm((prev) => ({ ...prev, slug: e.target.value }));
								editMethods.setValue("slug", e.target.value, { shouldValidate: false });
							}}
							fullWidth
						/>
						<Select
							displayEmpty
							fullWidth
							value={editForm.icon || ""}
							onChange={(e) => {
								setEditForm((prev) => ({ ...prev, icon: String(e.target.value) }));
								editMethods.setValue("icon", String(e.target.value), { shouldValidate: false });
							}}
							renderValue={(value) => {
								const selectedIcon = String(value);
								const selectedOption = getCategoryIconOption(selectedIcon);

								if (!selectedOption) {
									return <Typography color="text.secondary">Select icon</Typography>;
								}

								return (
									<Stack direction="row" spacing={1} alignItems="center">
										<CategoryIconBadge iconKey={selectedOption.key} fallback={null} />
										<Typography>{selectedOption.label}</Typography>
									</Stack>
								);
							}}
						>
							<MenuItem value="">
								No icon
							</MenuItem>
							{CATEGORY_ICON_OPTIONS.map((option) => (
								<MenuItem key={option.key} value={option.key}>
									<Stack direction="row" spacing={1} alignItems="center">
										<CategoryIconBadge iconKey={option.key} fallback={null} />
										<Typography>{option.label}</Typography>
									</Stack>
								</MenuItem>
							))}
						</Select>
						<TextField
							label="Sort Order"
							type="number"
							value={editForm.sortOrder ?? 0}
							{...editMethods.register("sortOrder")}
							onChange={(e) => {
								const value = Number(e.target.value) || 0;
								setEditForm((prev) => ({ ...prev, sortOrder: value }));
								editMethods.setValue("sortOrder", value, { shouldValidate: false });
							}}
							fullWidth
						/>
					</Stack>
				</DialogContent>
				<DialogActions>
					<Button onClick={() => setOpenEditDialog(false)}>Cancel</Button>
					<Button onClick={handleUpdateCategory} variant="contained" disabled={loading}>
						Save
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

export default CategoryPage;
