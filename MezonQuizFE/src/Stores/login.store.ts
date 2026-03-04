import { create } from "zustand";
import type { LoginResponse, User } from "../Interface/LoginResponse";
import {
	getHasSystemRole,
	getTokenAccess,
	removeHasSystemRole,
	removeTokenAccess,
	setHasSystemRole,
	setTokenAccess,
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

const useAuthStore = create<AuthState>((set) => ({
	token: initialToken,
	user: null,
	roleName: [],
	permissionName: [],
	hasSystemRole: initialHasSystemRole,
	isAuthenticated: Boolean(initialToken),
	setAuth: ({ token, user, roleName, permissionName, hasSystemRole }) => {
		const resolvedHasSystemRole = hasSystemRole ?? false;
		setTokenAccess(token);
		setHasSystemRole(resolvedHasSystemRole);
		set({
			token,
			user,
			roleName: roleName ?? [],
			permissionName: permissionName ?? [],
			hasSystemRole: resolvedHasSystemRole,
			isAuthenticated: Boolean(token)
		});
	},
	clearAuth: () => {
		removeTokenAccess();
		removeHasSystemRole();
		set({ token: null, user: null, roleName: [], permissionName: [], hasSystemRole: false, isAuthenticated: false });
	},
}));

export default useAuthStore;
