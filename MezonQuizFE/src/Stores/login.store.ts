import { create } from "zustand";
import type { LoginResponse, User } from "../Interface/Login.dto";
import {
	getHasSystemRole,
	getTokenAccess,
	getUser,
	removeHasSystemRole,
	removeTokenAccess,
	removeUser,
	setHasSystemRole,
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

const useAuthStore = create<AuthState>((set) => ({
	token: initialToken,
	user: initialUser,
	roleName: [],
	permissionName: [],
	hasSystemRole: initialHasSystemRole,
	isAuthenticated: Boolean(initialToken),
	setAuth: ({ token, user, roleName, permissionName, hasSystemRole }) => {
		const resolvedHasSystemRole = hasSystemRole ?? false;
		setTokenAccess(token);
		setHasSystemRole(resolvedHasSystemRole);
		setUser(user);
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
		removeUser();
		set({ token: null, user: null, roleName: [], permissionName: [], hasSystemRole: false, isAuthenticated: false });
	},
}));

export default useAuthStore;
