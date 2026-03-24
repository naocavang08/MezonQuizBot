import type { CategoryDto, createCategoryResponse, deleteCategoryResponse, SaveCategoryDto, updateCategoryResponse } from "../Interface/category.dto";
import apiClient from "./ApiClient";

export const getAllCategories = () => {
    return apiClient.get<CategoryDto[]>('/api/Category')
        .then((res) => {
            return res.data;
        });
}

export const getCategoryById = (categoryId: string) => {
    return apiClient.get<CategoryDto>(`/api/Category/${categoryId}`)
        .then((res) => {
            return res.data;
        });
}

export const createCategory = (body: SaveCategoryDto) => {
    return apiClient.post<createCategoryResponse>('/api/Category', body)
        .then((res) => {
            return res.data;
        });
}

export const updateCategory = (categoryId: string, body: SaveCategoryDto) => {
    return apiClient.put<updateCategoryResponse>(`/api/Category/${categoryId}`, body)
        .then((res) => {
            return res.data;
        });
}

export const deleteCategory = (categoryId: string) => {
    return apiClient.delete<deleteCategoryResponse>(`/api/Category/${categoryId}`)
        .then((res) => {
            return res.data;
        });
}