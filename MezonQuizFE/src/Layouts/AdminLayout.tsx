import styled from "@emotion/styled";
import { Avatar, Box, Menu, MenuItem, Typography } from "@mui/material";
import { NavLink, useNavigate } from "react-router-dom";
import useAuthStore from "../Stores/login.store";
import { MdLogout, MdPerson } from "react-icons/md";
import { useState } from "react";

const Root = styled(Box)`
    min-height: 100vh;
    display: flex;
    background: #f4f6f9;
    color: #1f2d3d;
    font-family: "Segoe UI", Tahoma, Geneva, Verdana, sans-serif;

    @media (max-width: 768px) {
        flex-direction: column;
    }
`;

const Sidebar = styled(Box)`
    width: 250px;
    background: #1f2d3d;
    color: #c2c7d0;
    display: flex;
    flex-direction: column;
    border-right: 1px solid #17212b;

    @media (max-width: 992px) {
        width: 200px;
    }

    @media (max-width: 768px) {
        width: 100%;
        border-right: 0;
        border-bottom: 1px solid #17212b;
    }
`;

const Brand = styled(Box)`
    height: 57px;
    display: flex;
    align-items: center;
    padding: 0 1rem;
    font-size: 1rem;
    font-weight: 600;
    color: #fff;
    border-bottom: 1px solid #2c3b47;
`;

const SidebarNav = styled(Box)`
    display: flex;
    flex-direction: column;
    padding: 0.5rem;
    gap: 0.25rem;

    @media (max-width: 768px) {
        flex-direction: row;
        overflow-x: auto;
    }
`;

const SidebarLink = styled(NavLink)`
    text-decoration: none;
    color: #c2c7d0;
    padding: 0.625rem 0.875rem;
    border-radius: 0.375rem;
    font-size: 0.93rem;
    font-weight: 500;
    transition: background-color 0.2s ease, color 0.2s ease;

    &:hover {
        background: #2c3b47;
        color: #fff;
    }

    &.active {
        background: #007bff;
        color: #fff;
    }

    @media (max-width: 768px) {
        white-space: nowrap;
    }
`;

const Main = styled(Box)`
    flex: 1;
    min-width: 0;
    display: flex;
    flex-direction: column;
`;

const Topbar = styled(Box)`
    height: 57px;
    background: #ffffff;
    border-bottom: 1px solid #dee2e6;
    display: flex;
    align-items: center;
    padding: 0 1rem;
`;

const Content = styled(Box)`
    padding: 1rem;
`;

const ContentCard = styled(Box)`
    background: #fff;
    border: 1px solid #dee2e6;
    border-radius: 0.375rem;
    box-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);
    padding: 1rem;
`;

const AdminLayout: React.FC<{ children: React.ReactNode }> = ({ children }) => {
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
        <Root>
            <Sidebar>
                <Brand>
                    <img src="/src/assets/mezon_icon.png" alt="Mezon Logo" width={24} height={24} style={{ marginRight: 8 }} />
                    Mezon Quiz
                </Brand>

                <SidebarNav>
                    <SidebarLink to="/admin/dashboard">
                        Dashboard
                    </SidebarLink>
                    <SidebarLink to="/admin/users">
                        Users
                    </SidebarLink>
                    <SidebarLink to="/admin/roles">
                        Roles
                    </SidebarLink>
                    <SidebarLink to="/admin/quizzes">
                        Quizzes
                    </SidebarLink>
                    <SidebarLink to="/admin/categories">
                        Categories
                    </SidebarLink>
                </SidebarNav>
            </Sidebar>

            <Main>
                <Topbar>
                    <Box sx={{ width: "100%", display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                        <Typography
                            variant="h6"
                            sx={{ margin: 0, fontSize: "1rem", fontWeight: 600, color: "#343a40" }}
                        >
                            Dashboard
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
                    </Box>
                </Topbar>

                <Content>
                    <ContentCard>{children}</ContentCard>
                </Content>
            </Main>
        </Root>
    );
};

export default AdminLayout;