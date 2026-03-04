import styled from "@emotion/styled";
import { Avatar, Box, Container, Menu, MenuItem, Stack, Typography } from "@mui/material";
import { useNavigate } from "react-router-dom";
import useAuthStore from "../Stores/login.store";
import { MdLogout, MdPerson } from "react-icons/md";
import { useState } from "react";

const Content = styled(Box)`
    padding-bottom: 1rem;
`;

const UserLayout: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const user = useAuthStore((state) => state.user);
    const clearAuth = useAuthStore((state) => state.clearAuth);
    const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
    const isMenuOpen = Boolean(anchorEl);

    const onAvatarClick = (event: React.MouseEvent<HTMLDivElement>) => {
        setAnchorEl(event.currentTarget);
    };

    const onCloseMenu = () => {
        setAnchorEl(null);
    };

    const navigate = useNavigate();
    
    const onLogout = () => {
        onCloseMenu();
        clearAuth();
        navigate("/login", { replace: true });
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
            <Container maxWidth="lg">
                <Stack direction="row" alignItems="center" justifyContent="space-between" mb={6}>
                    <Typography variant="h4" fontWeight={700} color="primary">
                        <img src="/src/assets/mezon_icon.png" alt="Mezon Logo" width={24} height={24} style={{ marginRight: 8 }} />
                        Mezon Quiz
                    </Typography>

                    <Avatar
                        alt={displayName}
                        src={avatarUrl}
                        onClick={onAvatarClick}
                        sx={{
                            width: 44,
                            height: 44,
                            backgroundColor: "primary.main",
                            fontWeight: 700,
                            cursor: "pointer",
                            border: "2px solid rgba(255, 255, 255, 0.5)",
                            boxShadow: "0 2px 8px rgba(0, 0, 0, 0.12)",
                            transition: "all 0.2s ease",
                            "&:hover": {
                                boxShadow: "0 4px 12px rgba(0, 0, 0, 0.18)",
                                transform: "scale(1.05)",
                            },
                        }}
                    >
                        {displayName.charAt(0).toUpperCase()}
                    </Avatar>

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
                        <MenuItem disabled>
                            <Typography variant="body2" fontWeight={600}>
                                {displayName}
                            </Typography>
                        </MenuItem>
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

            <Container maxWidth="lg">
				<Content>{children}</Content>
			</Container>
        </Box>
	);
};

export default UserLayout;
