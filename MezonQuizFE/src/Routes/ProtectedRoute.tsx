import { Navigate, Outlet } from "react-router-dom";
import { hasAnyPermission, PUBLIC_HOME_PATH, resolveDefaultAppPath } from "../Lib/Utils/permissions";
import useAuthStore from "../Stores/login.store";

type ProtectedRouteProps = {
	requiredPermissions?: readonly string[];
	requireSystemRole?: boolean;
};

const ProtectedRoute = ({ requiredPermissions, requireSystemRole = false }: ProtectedRouteProps) => {
	const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
	const roleName = useAuthStore((state) => state.roleName);
	const permissionName = useAuthStore((state) => state.permissionName);
	const hasSystemRole = useAuthStore((state) => state.hasSystemRole);
	const defaultAppPath = resolveDefaultAppPath(permissionName, hasSystemRole, roleName);

	if (!isAuthenticated) {
		return <Navigate to={PUBLIC_HOME_PATH} replace />;
	}

	if (requireSystemRole && !hasSystemRole) {
		return <Navigate to={defaultAppPath} replace />;
	}

	if (!hasAnyPermission(permissionName, requiredPermissions, hasSystemRole)) {
		return <Navigate to={defaultAppPath} replace />;
	}

	return <Outlet />;
};

export default ProtectedRoute;
