import type { PermissionResponse, RoleRequest, RoleResponse } from "../Interface/role.dto";
import apiClient from "./ApiClient";

export const getAllRoles = () => {
  return apiClient
    .get<RoleResponse[]>('/api/Role')
    .then((res) => {
      return res.data;
    });
};

export const createRole = (data: RoleRequest) => {
  return apiClient
    .post<RoleResponse>('/api/Role', data)
    .then((res) => {
      return res.data;
    });
};

export const deleteRole = (id: string) => {
  return apiClient
    .delete(`/api/Role/${id}`)
    .then((res) => {
      return res.data;
    });
};

export const getAllPermissions = () => {
  return apiClient
    .get<PermissionResponse[]>('/api/Role/permissions')
    .then((res) => {
        return res.data;
    });
};

export const getRolePermissions = (id: string) => {
  return apiClient
    .get<string>(`/api/Role/${id}/permissions`)
    .then((res) => {
        return res.data;
    });
};

export const assignPermissionsToRole = (params: { id: string; permissions: string[] }) => {
  return apiClient
    .post(`/api/Role/${params.id}/permissions`, params.permissions)
    .then((res) => {
        return res.data;
    });
};