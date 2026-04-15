import { create } from "zustand";
import type { LoginResponse, User } from "../Interface/login.dto";
import {
	getAccessTokenExpiresAt,
	getHasSystemRole,
	getPermissionNames,
	getRefreshToken,
	getRoleNames,
	getTokenAccess,
	getUser,
	removeAccessTokenExpiresAt,
	removeHasSystemRole,
	removePermissionNames,
	removeRefreshToken,
	removeRoleNames,
	removeTokenAccess,
	removeUser,
	setAccessTokenExpiresAt,
	setHasSystemRole,
	setPermissionNames,
	setRefreshToken,
	setRoleNames,
	setTokenAccess,
	setUser,
} from "../Lib/Utils/localStorage";

type AuthState = {
	token: string | null;
	refreshToken: string | null;
	accessTokenExpiresAt: number | null;
	user: User | null;
	roleName: string[];
	permissionName: string[];
	hasSystemRole: boolean;
	isAuthenticated: boolean;
	setAuth: (payload: LoginResponse) => void;
	setTokenBundle: (token: string, refreshToken: string, expiresIn: number) => void;
	clearAuth: () => void;
};

const initialToken = getTokenAccess();
const initialRefreshToken = getRefreshToken();
const initialAccessTokenExpiresAt = getAccessTokenExpiresAt();
const initialHasSystemRole = getHasSystemRole();
const initialUser = getUser();
const initialRoleNames = getRoleNames();
const initialPermissionNames = getPermissionNames();

const useAuthStore = create<AuthState>((set) => ({
	token: initialToken,
	refreshToken: initialRefreshToken,
	accessTokenExpiresAt: initialAccessTokenExpiresAt,
	user: initialUser,
	roleName: initialRoleNames,
	permissionName: initialPermissionNames,
	hasSystemRole: initialHasSystemRole,
	isAuthenticated: Boolean(initialToken),
	setAuth: ({ token, refreshToken, expiresIn, user, roleName, permissionName, hasSystemRole }) => {
		const resolvedHasSystemRole = hasSystemRole ?? false;
		const resolvedRoleName = roleName ?? [];
		const resolvedPermissionName = permissionName ?? [];
		const expiresAt = Date.now() + Math.max(0, expiresIn) * 1000;
		setTokenAccess(token);
		setRefreshToken(refreshToken);
		setAccessTokenExpiresAt(expiresAt);
		setHasSystemRole(resolvedHasSystemRole);
		setUser(user);
		setRoleNames(resolvedRoleName);
		setPermissionNames(resolvedPermissionName);
		set({
			token,
			refreshToken,
			accessTokenExpiresAt: expiresAt,
			user,
			roleName: resolvedRoleName,
			permissionName: resolvedPermissionName,
			hasSystemRole: resolvedHasSystemRole,
			isAuthenticated: Boolean(token)
		});
	},
	setTokenBundle: (token: string, refreshToken: string, expiresIn: number) => {
		const expiresAt = Date.now() + Math.max(0, expiresIn) * 1000;
		setTokenAccess(token);
		setRefreshToken(refreshToken);
		setAccessTokenExpiresAt(expiresAt);
		set({
			token,
			refreshToken,
			accessTokenExpiresAt: expiresAt,
			isAuthenticated: Boolean(token),
		});
	},
	clearAuth: () => {
		removeTokenAccess();
		removeRefreshToken();
		removeAccessTokenExpiresAt();
		removeHasSystemRole();
		removeUser();
		removeRoleNames();
		removePermissionNames();
		set({ token: null, refreshToken: null, accessTokenExpiresAt: null, user: null, roleName: [], permissionName: [], hasSystemRole: false, isAuthenticated: false });
	},
}));

export default useAuthStore;
