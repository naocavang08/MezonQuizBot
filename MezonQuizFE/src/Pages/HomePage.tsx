import { useState, type MouseEvent } from "react";
import { Box, Button, Typography, Avatar, Menu, MenuItem, Stack, Container } from "@mui/material";
import { MdLogout, MdPerson } from "react-icons/md";
import useAuthStore from "../Stores/login.store";

const HomePage = () => {
    const user = useAuthStore((state) => state.user);
    const clearAuth = useAuthStore((state) => state.clearAuth);
    const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
    const isMenuOpen = Boolean(anchorEl);

    const onAvatarClick = (event: MouseEvent<HTMLElement>) => {
        setAnchorEl(event.currentTarget);
    };

    const onCloseMenu = () => {
        setAnchorEl(null);
    };

    const onLogout = () => {
        onCloseMenu();
        clearAuth();
    };

    const displayName = user?.displayName || user?.username || "User";
    const avatarUrl = user?.avatarUrl;

    return (
        <Box
            sx={{
                minHeight: "100vh",
                background: "linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%)",
                padding: 3,
                position: "relative",
            }}
        >
            {/* Header */}
            <Container maxWidth="lg">
                <Stack direction="row" alignItems="center" justifyContent="space-between" mb={6}>
                    <Typography variant="h4" fontWeight={700} color="primary">
                        <img src="/src/assets/mezon_icon.png" alt="Mezon Logo" width={24} height={24} style={{ marginRight: 8 }} />
                        Mezon Quiz
                    </Typography>

                    {/* Avatar Button */}
                    <Box
                        onClick={onAvatarClick}
                        sx={{
                            cursor: "pointer",
                            display: "flex",
                            alignItems: "center",
                            gap: 1,
                            padding: "8px 12px",
                            borderRadius: "24px",
                            transition: "all 0.2s ease",
                            "&:hover": {
                                backgroundColor: "rgba(0, 0, 0, 0.05)",
                            },
                        }}
                    >
                        <Avatar
                            alt={displayName}
                            src={avatarUrl}
                            sx={{
                                width: 40,
                                height: 40,
                                backgroundColor: "primary.main",
                                fontWeight: 700,
                            }}
                        >
                            {displayName.charAt(0).toUpperCase()}
                        </Avatar>
                        <Box sx={{ display: { xs: "none", sm: "block" } }}>
                            <Typography variant="body2" fontWeight={600}>
                                {displayName}
                            </Typography>
                        </Box>
                    </Box>

                    <Menu
                        anchorEl={anchorEl}
                        open={isMenuOpen}
                        onClose={onCloseMenu}
                        PaperProps={{
                            sx: {
                                width: "200px",
                                borderRadius: "12px",
                                boxShadow: "0 4px 12px rgba(0, 0, 0, 0.1)",
                            },
                        }}
                    >
                        <MenuItem disabled sx={{ color: "text.secondary" }}>
                            <Typography variant="caption">{user?.email}</Typography>
                        </MenuItem>
                        <MenuItem onClick={onCloseMenu} sx={{ gap: 1 }}>
                            <MdPerson size={18} />
                            <Typography variant="body2">My info</Typography>
                        </MenuItem>
                        <MenuItem onClick={onLogout} sx={{ gap: 1, color: "error.main" }}>
                            <MdLogout size={18} />
                            <Typography variant="body2">Logout</Typography>
                        </MenuItem>
                    </Menu>
                </Stack>
            </Container>

            {/* Main Content */}
            <Container maxWidth="md">
                <Box
                    sx={{
                        textAlign: "center",
                        marginTop: "60px",
                    }}
                >
                    <Typography variant="h3" fontWeight={700} mb={2}>
                        Welcome Mezon Quiz!
                    </Typography>
                    <Typography variant="body1" color="text.secondary" mb={6}>
                        Choose an option to get started
                    </Typography>

                    {/* Action Buttons */}
                    <Stack
                        direction={{ xs: "column", sm: "row" }}
                        spacing={3}
                        justifyContent="center"
                        alignItems="center"
                    >
                        <Button
                            variant="contained"
                            size="large"
                            color="primary"
                            sx={{
                                minWidth: "200px",
                                borderRadius: "8px",
                                fontSize: "1rem",
                            }}
                        >
                            ➕ Find Game
                        </Button>

                        <Button
                            variant="outlined"
                            size="large"
                            color="primary"
                            sx={{
                                minWidth: "200px",
                                borderRadius: "8px",
                                fontSize: "1rem",
                            }}
                        >
                            📋 My Game
                        </Button>
                    </Stack>
                </Box>
            </Container>
        </Box>
    );
};

export default HomePage;
