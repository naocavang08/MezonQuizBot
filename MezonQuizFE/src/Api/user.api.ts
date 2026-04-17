import type { CreateUserRequest, UpdateUserRequest, UserResponse } from "../Interface/user.dto";
import apiClient from "./ApiClient";

export const getAllUsers = () => {
	return apiClient
		.get<UserResponse[]>("/api/User")
		.then((res) => {
			return res.data;
		});
};

export const getUserById = (id: string) => {
	return apiClient
		.get<UserResponse>(`/api/User/${id}`)
		.then((res) => {
			return res.data;
		});
};

export const createUser = (data: CreateUserRequest) => {
	return apiClient
		.post<UserResponse>("/api/User", data)
		.then((res) => {
			return res.data;
		});
};

export const updateUser = (id: string, data: UpdateUserRequest) => {
	return apiClient
		.post<UserResponse>(`/api/User/${id}`, data)
		.then((res) => {
			return res.data;
		});
};

export const deleteUser = (id: string) => {
	return apiClient
		.delete(`/api/User/${id}`)
		.then((res) => {
			return res.data;
		});
};

export const getUserRoles = (id: string) => {
    return apiClient
        .get<string[]>(`/api/User/${id}/roles`)
        .then((res) => {            
            return res.data;
        });
}

export const assignRolesToUser = (params: { id: string; roleIds: string[] }) => {
	return apiClient
		.post(`/api/User/${params.id}/roles`, params.roleIds)
		.then((res) => {
			return res.data;
		});
};

export const uploadUserAvatar = (file: File) => {
	const formData = new FormData();
	formData.append("file", file);

	return apiClient
		.post<{ url?: string; Url?: string }>("/api/User/upload-avatar", formData, {
			headers: { "Content-Type": "multipart/form-data" },
		})
		.then((res) => res.data?.url ?? res.data?.Url ?? "");
};
