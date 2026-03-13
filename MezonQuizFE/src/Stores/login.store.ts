import { create } from "zustand";
import type { LoginResponse, User } from "../Interface/login.dto";
import {
	getHasSystemRole,
	getPermissionNames,
	getRoleNames,
	getTokenAccess,
	getUser,
	removeHasSystemRole,
	removePermissionNames,
	removeRoleNames,
	removeTokenAccess,
	removeUser,
	setHasSystemRole,
	setPermissionNames,
	setRoleNames,
	setTokenAccess,
	setUser,
} from "../Lib/Utils/localStorage";

type AuthState = {
	token: string | null;
	user: User | null;
	roleName: string[];
	permissionName: string[];
	hasSystemRole: boolean;
	isAuthenticated: boolean;
	setAuth: (payload: LoginResponse) => void;
	clearAuth: () => void;
};

const initialToken = getTokenAccess();
const initialHasSystemRole = getHasSystemRole();
const initialUser = getUser();
const initialRoleNames = getRoleNames();
const initialPermissionNames = getPermissionNames();

const useAuthStore = create<AuthState>((set) => ({
	token: initialToken,
	user: initialUser,
	roleName: initialRoleNames,
	permissionName: initialPermissionNames,
	hasSystemRole: initialHasSystemRole,
	isAuthenticated: Boolean(initialToken),
	setAuth: ({ token, user, roleName, permissionName, hasSystemRole }) => {
		const resolvedHasSystemRole = hasSystemRole ?? false;
		const resolvedRoleName = roleName ?? [];
		const resolvedPermissionName = permissionName ?? [];
		setTokenAccess(token);
		setHasSystemRole(resolvedHasSystemRole);
		setUser(user);
		setRoleNames(resolvedRoleName);
		setPermissionNames(resolvedPermissionName);
		set({
			token,
			user,
			roleName: resolvedRoleName,
			permissionName: resolvedPermissionName,
			hasSystemRole: resolvedHasSystemRole,
			isAuthenticated: Boolean(token)
		});
	},
	clearAuth: () => {
		removeTokenAccess();
		removeHasSystemRole();
		removeUser();
		removeRoleNames();
		removePermissionNames();
		set({ token: null, user: null, roleName: [], permissionName: [], hasSystemRole: false, isAuthenticated: false });
	},
}));

export default useAuthStore;
