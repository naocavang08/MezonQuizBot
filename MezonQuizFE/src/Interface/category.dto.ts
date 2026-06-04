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

export type createCategoryResponse = CategoryDto;
export type updateCategoryResponse = CategoryDto;
export type deleteCategoryResponse = void;