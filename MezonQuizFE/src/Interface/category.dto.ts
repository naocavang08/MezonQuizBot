export interface CategoryDto extends SaveCategoryDto {
  id: string;
  createdAt: string;
}

export interface SaveCategoryDto {
    name: string;
    slug: string;
    icon?: string;
    sortOrder?: number;
}

export interface createCategoryResponse {
    message: string;
}

export interface updateCategoryResponse {
    message: string;
}

export interface deleteCategoryResponse {
    message: string;
}