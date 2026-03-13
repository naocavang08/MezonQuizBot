import { Alert, Snackbar, Slide, type AlertColor, type SlideProps } from "@mui/material";

type AppSnackbarProps = {
	open: boolean;
	message: string;
	severity: AlertColor;
	onClose: () => void;
	autoHideDuration?: number;
};

const SlideFromRight = (props: SlideProps) => {
	return <Slide {...props} direction="left" />;
};

const AppSnackbar = ({
	open,
	message,
	severity,
	onClose,
	autoHideDuration = 3200,
}: AppSnackbarProps) => {
	return (
		<Snackbar
			open={open}
			autoHideDuration={autoHideDuration}
			onClose={onClose}
			anchorOrigin={{ vertical: "top", horizontal: "right" }}
			TransitionComponent={SlideFromRight}
		>
			<Alert onClose={onClose} severity={severity} variant="filled" sx={{ width: "100%" }}>
				{message}
			</Alert>
		</Snackbar>
	);
};

export default AppSnackbar;
