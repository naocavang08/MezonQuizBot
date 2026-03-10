import { useEffect, useState } from "react";
import {
	Alert,
	Box,
	Button,
	CircularProgress,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
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
import { createCategory, deleteCategory, getAllCategories, updateCategory } from "../../Api/category.api";
import type { CategoryDto, SaveCategoryDto } from "../../Interface/Category.dto";

const defaultForm: SaveCategoryDto = {
	name: "",
	slug: "",
	icon: "",
	sortOrder: 0,
};

const CategoryPage = () => {
	const [categories, setCategories] = useState<CategoryDto[]>([]);
	const [loading, setLoading] = useState(false);
	const [error, setError] = useState<string | null>(null);
	const [success, setSuccess] = useState<string | null>(null);

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
			setError(null);
		} catch {
			setError("Không thể tải danh sách category.");
		} finally {
			setLoading(false);
		}
	};

	useEffect(() => {
		fetchCategories();
	}, []);

	useEffect(() => {
		if (!success) {
			return;
		}

		const timer = setTimeout(() => setSuccess(null), 2500);
		return () => clearTimeout(timer);
	}, [success]);

	const normalizeForm = (form: SaveCategoryDto): SaveCategoryDto => ({
		name: form.name.trim(),
		slug: form.slug.trim(),
		icon: form.icon?.trim() || undefined,
		sortOrder: typeof form.sortOrder === "number" ? form.sortOrder : 0,
	});

	const validateForm = (form: SaveCategoryDto): boolean => {
		if (!form.name.trim() || !form.slug.trim()) {
			setError("Name và Slug là bắt buộc.");
			return false;
		}

		return true;
	};

	const handleCreateCategory = async () => {
		if (!validateForm(createForm)) {
			return;
		}

		setLoading(true);
		try {
			await createCategory(normalizeForm(createForm));
			setSuccess("Tạo category thành công.");
			setOpenCreateDialog(false);
			setCreateForm(defaultForm);
			await fetchCategories();
		} catch {
			setError("Tạo category thất bại.");
		} finally {
			setLoading(false);
		}
	};

	const handleOpenEditDialog = (category: CategoryDto) => {
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
		if (!selectedCategory) {
			return;
		}

		if (!validateForm(editForm)) {
			return;
		}

		setLoading(true);
		try {
			await updateCategory(selectedCategory.id, normalizeForm(editForm));
			setSuccess("Cập nhật category thành công.");
			setOpenEditDialog(false);
			setSelectedCategory(null);
			await fetchCategories();
		} catch {
			setError("Cập nhật category thất bại.");
		} finally {
			setLoading(false);
		}
	};

	const handleDeleteCategory = async (categoryId: string, categoryName: string) => {
		if (!window.confirm(`Bạn có chắc muốn xóa category \\"${categoryName}\\"?`)) {
			return;
		}

		setLoading(true);
		try {
			await deleteCategory(categoryId);
			setSuccess("Xóa category thành công.");
			await fetchCategories();
		} catch {
			setError("Xóa category thất bại.");
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
				<Button variant="contained" onClick={() => setOpenCreateDialog(true)}>
					Add Category
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
								<TableCell align="right">Actions</TableCell>
							</TableRow>
						</TableHead>
						<TableBody>
							{categories.map((category) => (
								<TableRow key={category.id} hover>
									<TableCell>{category.name}</TableCell>
									<TableCell>{category.slug}</TableCell>
									<TableCell>{category.icon || "-"}</TableCell>
									<TableCell>{category.sortOrder ?? 0}</TableCell>
									<TableCell align="right">
										<Stack direction="row" spacing={1} justifyContent="flex-end">
											<Button size="small" variant="outlined" onClick={() => handleOpenEditDialog(category)}>
												Edit
											</Button>
											<Button
												size="small"
												variant="outlined"
												color="error"
												onClick={() => handleDeleteCategory(category.id, category.name)}
											>
												Delete
											</Button>
										</Stack>
									</TableCell>
								</TableRow>
							))}
							{categories.length === 0 && (
								<TableRow>
									<TableCell colSpan={5} align="center">
										Chưa có category nào.
									</TableCell>
								</TableRow>
							)}
						</TableBody>
					</Table>
				)}
			</Paper>

			<Dialog open={openCreateDialog} onClose={() => setOpenCreateDialog(false)} maxWidth="sm" fullWidth>
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
						<TextField
							label="Icon"
							value={createForm.icon || ""}
							onChange={(e) => setCreateForm((prev) => ({ ...prev, icon: e.target.value }))}
							fullWidth
						/>
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

			<Dialog open={openEditDialog} onClose={() => setOpenEditDialog(false)} maxWidth="sm" fullWidth>
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
						<TextField
							label="Icon"
							value={editForm.icon || ""}
							onChange={(e) => setEditForm((prev) => ({ ...prev, icon: e.target.value }))}
							fullWidth
						/>
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
		</Box>
	);
};

export default CategoryPage;
