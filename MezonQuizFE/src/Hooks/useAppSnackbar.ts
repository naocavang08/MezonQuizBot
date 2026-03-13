import { useCallback, useState } from "react";
import type { AlertColor } from "@mui/material";

type SnackbarState = {
	open: boolean;
	message: string;
	severity: AlertColor;
};

const defaultState: SnackbarState = {
	open: false,
	message: "",
	severity: "info",
};

const useAppSnackbar = () => {
	const [snackbar, setSnackbar] = useState<SnackbarState>(defaultState);

	const showSnackbar = useCallback((message: string, severity: AlertColor = "info") => {
		setSnackbar({ open: true, message, severity });
	}, []);

	const showError = useCallback((message: string) => {
		showSnackbar(message, "error");
	}, [showSnackbar]);

	const showSuccess = useCallback((message: string) => {
		showSnackbar(message, "success");
	}, [showSnackbar]);

	const showInfo = useCallback((message: string) => {
		showSnackbar(message, "info");
	}, [showSnackbar]);

	const closeSnackbar = useCallback(() => {
		setSnackbar((prev) => ({ ...prev, open: false }));
	}, []);

	return {
		snackbar,
		showSnackbar,
		showError,
		showSuccess,
		showInfo,
		closeSnackbar,
	};
};

export default useAppSnackbar;
