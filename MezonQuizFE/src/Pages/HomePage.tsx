import { Box, Button, Typography } from "@mui/material";
import useAuthStore from "../Stores/login.store";

const HomePage = () => {
    const clearAuth = useAuthStore((state) => state.clearAuth);

    const onLogout = () => {
        clearAuth();
    };

    return (
        <Box
            sx={{
                display: "flex",
                flexDirection: "column",
                alignItems: "center",
                justifyContent: "center",
                minHeight: "100vh",
                gap: 4,
                padding: 4,
            }}
        >
            <Typography variant="h3" fontWeight={700} textAlign="center">
                Chào mừng đến với Mezon Quiz Bot!
            </Typography>

            <Button variant="outlined" color="error" onClick={onLogout}>
                Logout
            </Button>
        </Box>
    );
};

export default HomePage;