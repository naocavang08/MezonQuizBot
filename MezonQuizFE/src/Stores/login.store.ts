import { create } from "zustand";
import type { LoginResponse, User } from "../Interface/LoginResponse";
import {
	getTokenAccess,
	removeTokenAccess,
	setTokenAccess,
} from "../Lib/Utils/localStorage";

type AuthState = {
	token: string | null;
	user: User | null;
	isAuthenticated: boolean;
	setAuth: (payload: LoginResponse) => void;
	clearAuth: () => void;
};

const initialToken = getTokenAccess();

const useAuthStore = create<AuthState>((set) => ({
	token: initialToken,
	user: null,
	isAuthenticated: Boolean(initialToken),
	setAuth: ({ token, user }) => {
		setTokenAccess(token);
		set({ token, user, isAuthenticated: Boolean(token) });
	},
	clearAuth: () => {
		removeTokenAccess();
		set({ token: null, user: null, isAuthenticated: false });
	},
}));

export default useAuthStore;
