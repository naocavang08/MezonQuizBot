import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { getApiErrorMessage } from "../Api/ApiClient";
import { login, mezonAuthorize } from "../Api/login.api";
import { resolveDefaultAppPath } from "../Lib/Utils/permissions";
import useAuthStore from "../Stores/login.store";

export type LoginFormValues = {
	username: string;
	password: string;
};

const DEFAULT_ERROR_MESSAGE = "Failed to login. Please check your information.";
const MEZON_LOGIN_INIT_ERROR = "Failed to initialize Mezon login.";

const useLoginPage = () => {
	const navigate = useNavigate();
	const setAuth = useAuthStore((state) => state.setAuth);
	const [isSubmitting, setIsSubmitting] = useState(false);
	const [errorMessage, setErrorMessage] = useState<string | null>(null);

	const onSubmit = async (data: LoginFormValues) => {
		setErrorMessage(null);
		setIsSubmitting(true);

		try {
			const response = await login(data);

			if (!response?.token || !response?.refreshToken) {
				setErrorMessage(DEFAULT_ERROR_MESSAGE);
				return;
			}

			setAuth(response);
			navigate(resolveDefaultAppPath(response.permissionName ?? [], response.hasSystemRole ?? false, response.roleName ?? []), { replace: true });
		} catch (error: unknown) {
			setErrorMessage(getApiErrorMessage(error, DEFAULT_ERROR_MESSAGE));
		} finally {
			setIsSubmitting(false);
		}
	};

	const onLoginWithMezon = async () => {
		setErrorMessage(null);

		try {
			const response = await mezonAuthorize();
			const authorizeUrl = response?.authorizeUrl ?? response?.AuthorizeUrl;

			if (!authorizeUrl) {
				setErrorMessage(MEZON_LOGIN_INIT_ERROR);
				return;
			}

			window.location.href = authorizeUrl;
		} catch {
			setErrorMessage(MEZON_LOGIN_INIT_ERROR);
		}
	};

	return {
		isSubmitting,
		errorMessage,
		onSubmit,
		onLoginWithMezon,
	};
};

export default useLoginPage;
