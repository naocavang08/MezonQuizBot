import { Navigate, Outlet, Route, Routes } from "react-router-dom";
import OAuthCallback from "../Components/OAuth/OAuthCallback";
import AdminLayout from "../Layouts/AdminLayout";
import UserLayout from "../Layouts/UserLayout";
import HomePage from "../Pages/User/HomePage";
import LoginPage from "../Pages/LoginPage";
import DashboardPage from "../Pages/Admin/DashboardPage";
import QuizPage from "../Pages/Admin/QuizPage";
import RolePage from "../Pages/Admin/RolePage";
import UserPage from "../Pages/Admin/UserPage";
import useAuthStore from "../Stores/login.store";
import ProtectedRoute from "./ProtectedRoute";

const AdminShell = () => {
  return (
    <AdminLayout>
      <Outlet />
    </AdminLayout>
  );
};

const UserShell = () => {
  return (
    <UserLayout>
      <Outlet />
    </UserLayout>
  );
};

const AppRoutes = () => {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const hasSystemRole = useAuthStore((state) => state.hasSystemRole);
  const defaultPath = hasSystemRole ? "/admin/dashboard" : "/user/home";

  return (
    <Routes>
      <Route
        path="/login"
        element={isAuthenticated ? <Navigate to={defaultPath} replace /> : <LoginPage />}
      />

      <Route path="/oauth/mezon/callback" element={<OAuthCallback />} />

      <Route element={<ProtectedRoute />}>
        <Route path="/" element={<Navigate to={defaultPath} replace />} />

        <Route path="/admin" element={hasSystemRole ? <AdminShell /> : <Navigate to="/user/home" replace />}>
          <Route index element={<Navigate to="dashboard" replace />} />
          <Route path="dashboard" element={<DashboardPage />} />
          <Route path="users" element={<UserPage />} />
          <Route path="roles" element={<RolePage />} />
          <Route path="quizzes" element={<QuizPage />} />
        </Route>

        <Route path="/user" element={!hasSystemRole ? <UserShell /> : <Navigate to="/admin/dashboard" replace />}>
          <Route index element={<Navigate to="home" replace />} />
          <Route path="home" element={<HomePage />} />
        </Route>
      </Route>

      <Route path="*" element={<Navigate to={isAuthenticated ? defaultPath : "/login"} replace />} />
    </Routes>
  )
};

export default AppRoutes;