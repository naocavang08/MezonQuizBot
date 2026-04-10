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
import AppSnackbar from "../../Components/AppSnackbar";
import useAppSnackbar from "../../Hooks/useAppSnackbar";
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

const CategoryPage = () => {
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

	const fetchCategories = async () => {
		setLoading(true);
		try {
			const data = await getAllCategories();
			setCategories(data);
		} catch {
			showError("Không thể tải danh sách category.");
		} finally {
			setLoading(false);
		}
	};

	useEffect(() => {
		fetchCategories();
	}, []);

	const normalizeForm = (form: SaveCategoryDto): SaveCategoryDto => ({
		name: form.name.trim(),
		slug: form.slug.trim(),
		icon: form.icon?.trim() || undefined,
		sortOrder: typeof form.sortOrder === "number" ? form.sortOrder : 0,
	});

	const validateForm = (form: SaveCategoryDto): boolean => {
		if (!form.name.trim() || !form.slug.trim()) {
			showError("Name và Slug là bắt buộc.");
			return false;
		}

		const iconKey = form.icon?.trim();
		if (iconKey && !getCategoryIconOption(iconKey)) {
			showError("Icon không hợp lệ. Vui lòng chọn từ danh sách.");
			return false;
		}

		return true;
	};

	const handleCreateCategory = async () => {
		if (!canCreateCategory) {
			showError("You do not have permission to create categories.");
			return;
		}

		if (!validateForm(createForm)) {
			return;
		}

		setLoading(true);
		try {
			await createCategory(normalizeForm(createForm));
			showSuccess("Tạo category thành công.");
			setOpenCreateDialog(false);
			setCreateForm(defaultForm);
			await fetchCategories();
		} catch {
			showError("Tạo category thất bại.");
		} finally {
			setLoading(false);
		}
	};

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
		setOpenEditDialog(true);
	};

	const handleUpdateCategory = async () => {
		if (!canUpdateCategory) {
			showError("You do not have permission to update categories.");
			return;
		}

		if (!selectedCategory) {
			return;
		}

		if (!validateForm(editForm)) {
			return;
		}

		setLoading(true);
		try {
			await updateCategory(selectedCategory.id, normalizeForm(editForm));
			showSuccess("Cập nhật category thành công.");
			setOpenEditDialog(false);
			setSelectedCategory(null);
			await fetchCategories();
		} catch {
			showError("Cập nhật category thất bại.");
		} finally {
			setLoading(false);
		}
	};

	const handleDeleteCategory = async (categoryId: string, categoryName: string) => {
		if (!canDeleteCategory) {
			showError("You do not have permission to delete categories.");
			return;
		}

		if (!window.confirm(`Bạn có chắc muốn xóa category \\"${categoryName}\\"?`)) {
			return;
		}

		setLoading(true);
		try {
			await deleteCategory(categoryId);
			showSuccess("Xóa category thành công.");
			await fetchCategories();
		} catch {
			showError("Xóa category thất bại.");
		} finally {
			setLoading(false);
		}
	};

	return (
		<Box>
			<Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
				<Typography variant="h5" fontWeight={700}>
					Category Management
				</Typography>
				{canCreateCategory ? (
					<Button variant="contained" onClick={() => setOpenCreateDialog(true)}>
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
							value={createForm.name}
							onChange={(e) => setCreateForm((prev) => ({ ...prev, name: e.target.value }))}
							fullWidth
						/>
						<TextField
							label="Slug"
							value={createForm.slug}
							onChange={(e) => setCreateForm((prev) => ({ ...prev, slug: e.target.value }))}
							fullWidth
						/>
						<Select
							displayEmpty
							fullWidth
							value={createForm.icon || ""}
							onChange={(e) => setCreateForm((prev) => ({ ...prev, icon: String(e.target.value) }))}
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
							onChange={(e) =>
								setCreateForm((prev) => ({ ...prev, sortOrder: Number(e.target.value) || 0 }))
							}
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
							value={editForm.name}
							onChange={(e) => setEditForm((prev) => ({ ...prev, name: e.target.value }))}
							fullWidth
						/>
						<TextField
							label="Slug"
							value={editForm.slug}
							onChange={(e) => setEditForm((prev) => ({ ...prev, slug: e.target.value }))}
							fullWidth
						/>
						<Select
							displayEmpty
							fullWidth
							value={editForm.icon || ""}
							onChange={(e) => setEditForm((prev) => ({ ...prev, icon: String(e.target.value) }))}
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
							onChange={(e) => setEditForm((prev) => ({ ...prev, sortOrder: Number(e.target.value) || 0 }))}
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
