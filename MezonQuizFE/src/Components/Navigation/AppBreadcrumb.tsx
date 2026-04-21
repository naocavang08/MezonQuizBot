import { Breadcrumbs, Link, Stack, Typography } from "@mui/material";
import { useMemo } from "react";
import { matchPath, useLocation, useNavigate } from "react-router-dom";

type BreadcrumbRoute = {
    path: string;
    label: string;
    root?: boolean;
    parentPath?: string;
};

const breadcrumbRoutes: BreadcrumbRoute[] = [
    { path: "/app/dashboard", label: "Dashboard", root: true },
    { path: "/app/users", label: "User Management", root: true },
    { path: "/app/roles", label: "Role Management", root: true },
    { path: "/app/quizzes", label: "Quiz Management", root: true },
    { path: "/app/categories", label: "Category Management", root: true },
    { path: "/app/audit-logs", label: "Audit Logs", root: true },
    { path: "/app/find-quizzes", label: "Find Quizzes", root: true },
    { path: "/app/find-quizzes/:quizId", label: "Quiz Detail", parentPath: "/app/find-quizzes" },
    { path: "/app/find-quizzes/:quizId/sessions/:sessionId/leaderboard", label: "Session Leaderboard", parentPath: "/app/find-quizzes/:quizId" },
    { path: "/app/my-quizzes", label: "My Quizzes", root: true },
    { path: "/app/my-quizzes/:quizId/settings", label: "Quiz Setting", parentPath: "/app/my-quizzes" },
    { path: "/app/my-quizzes/:quizId/sessions", label: "Quiz Sessions", parentPath: "/app/my-quizzes" },
    { path: "/app/create-quiz", label: "Create Quiz", root: true },
    { path: "/app/my-quizzes/:quizId/sessions/:sessionId", label: "Session Room", parentPath: "/app/my-quizzes/:quizId/sessions" },
    { path: "/app/my-quizzes/:quizId/sessions/:sessionId/play", label: "Player View", parentPath: "/app/my-quizzes/:quizId/sessions/:sessionId" },
    { path: "/app/my-quizzes/:quizId/sessions/:sessionId/start-quiz", label: "Start Quiz", parentPath: "/app/my-quizzes/:quizId/sessions/:sessionId" },
];

const findRouteMatch = (pathname: string) => {
    for (const route of breadcrumbRoutes) {
        const match = matchPath({ path: route.path, end: true }, pathname);
        if (match) {
            return { route, params: match.params };
        }
    }

    return null;
};

const resolvePathParams = (
    templatePath: string,
    params: Record<string, string | undefined>
) => {
    return templatePath.replace(/:([A-Za-z0-9_]+)/g, (_, key: string) => params[key] ?? "");
};

const findRoute = (pathname: string) => {
    return findRouteMatch(pathname)?.route;
};

const resolveBreadcrumbLabel = (pathname: string) => {
    const route = findRoute(pathname);

    if (route) {
        return route.label;
    }

    const lastSegment = pathname.split("/").filter(Boolean).at(-1);
    if (!lastSegment) {
        return "Workspace";
    }

    return lastSegment
        .replace(/-/g, " ")
        .replace(/\b\w/g, (char) => char.toUpperCase());
};

const AppBreadcrumb = () => {
    const location = useLocation();
    const navigate = useNavigate();

    const currentRouteMatch = useMemo(
        () => findRouteMatch(location.pathname),
        [location.pathname]
    );

    const currentRoute = currentRouteMatch?.route;

    const logicalParentPath = useMemo(() => {
        if (!currentRouteMatch?.route.parentPath) {
            return null;
        }

        const resolvedPath = resolvePathParams(
            currentRouteMatch.route.parentPath,
            currentRouteMatch.params
        );

        return resolvedPath && resolvedPath !== location.pathname ? resolvedPath : null;
    }, [currentRouteMatch, location.pathname]);

    const breadcrumbParentPath = logicalParentPath;

    const currentLabel = useMemo(
        () => currentRoute?.label ?? resolveBreadcrumbLabel(location.pathname),
        [currentRoute?.label, location.pathname]
    );

    const previousLabel = useMemo(
        () => (breadcrumbParentPath ? resolveBreadcrumbLabel(breadcrumbParentPath) : ""),
        [breadcrumbParentPath]
    );

    if (currentRoute?.root) {
        return (
            <Typography variant="body2" color="text.secondary" fontWeight={600}>
                {currentLabel}
            </Typography>
        );
    }

    if (!breadcrumbParentPath) {
        return (
            <Typography variant="body2" color="text.secondary" fontWeight={600}>
                {currentLabel}
            </Typography>
        );
    }

    return (
        <Stack direction="row" alignItems="center" sx={{ minHeight: 28 }}>
            <Breadcrumbs aria-label="breadcrumb" separator="/">
                <Link
                    component="button"
                    type="button"
                    variant="body2"
                    underline="hover"
                    color="inherit"
                    onClick={() => {
                        navigate(breadcrumbParentPath);
                    }}
                    sx={{ fontWeight: 600 }}
                >
                    {previousLabel}
                </Link>
                <Typography variant="body2" color="text.secondary" fontWeight={700}>
                    {currentLabel}
                </Typography>
            </Breadcrumbs>
        </Stack>
    );
};

export default AppBreadcrumb;