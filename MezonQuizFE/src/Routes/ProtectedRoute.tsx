import { Navigate, Outlet } from "react-router-dom";
import { hasAnyPermission } from "../Lib/Utils/permissions";
import useAuthStore from "../Stores/login.store";

type ProtectedRouteProps = {
	requiredPermissions?: string[];
};

const ProtectedRoute = ({ requiredPermissions }: ProtectedRouteProps) => {
	const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
	const permissionName = useAuthStore((state) => state.permissionName);
	const hasSystemRole = useAuthStore((state) => state.hasSystemRole);

	if (!isAuthenticated) {
		return <Navigate to="/login" replace />;
	}

	if (!hasAnyPermission(permissionName, requiredPermissions, hasSystemRole)) {
		return <Navigate to="/app" replace />;
	}

	return <Outlet />;
};

export default ProtectedRoute;
