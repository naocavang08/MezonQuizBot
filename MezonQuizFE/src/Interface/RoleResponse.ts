export interface RoleRequest {
  name: string;
  displayName?: string;
  description?: string;
  isSystem: boolean;
}

export interface RoleResponse extends RoleRequest {
  id: string;
}

export interface PermissionResponse {
  id: string;
  resource: string;
  action: string;
  description?: string;
}