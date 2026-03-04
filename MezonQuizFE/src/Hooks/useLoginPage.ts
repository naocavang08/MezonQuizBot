import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { login } from "../Api/login.api";
import useAuthStore from "../Stores/login.store";

export type LoginFormValues = {
	username: string;
	password: string;
};

const DEFAULT_ERROR_MESSAGE = "Đăng nhập thất bại. Vui lòng kiểm tra lại thông tin.";

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

			if (!response?.token) {
				setErrorMessage(DEFAULT_ERROR_MESSAGE);
				return;
			}

			setAuth(response);
			navigate(response.hasSystemRole ? "/admin/dashboard" : "/user/home", { replace: true });
		} catch (error: unknown) {
			if (error && typeof error === "object" && "response" in error) {
				const errorResponse = (error as { response?: { data?: { message?: string } } }).response;
				const backendMessage = errorResponse?.data?.message;

				setErrorMessage(backendMessage ?? DEFAULT_ERROR_MESSAGE);
			} else {
				setErrorMessage(DEFAULT_ERROR_MESSAGE);
			}
		} finally {
			setIsSubmitting(false);
		}
	};

	const onLoginWithMezon = () => {
		setErrorMessage("Tính năng đăng nhập Mezon đang được phát triển.");
	};

	return {
		isSubmitting,
		errorMessage,
		onSubmit,
		onLoginWithMezon,
	};
};

export default useLoginPage;
