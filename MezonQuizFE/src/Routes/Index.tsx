import { Navigate, Route, Routes } from "react-router-dom";
import OAuthCallback from "../Components/OAuth/OAuthCallback";
import HomePage from "../Pages/HomePage";
import LoginPage from "../Pages/LoginPage";
import useAuthStore from "../Stores/login.store";
import ProtectedRoute from "./ProtectedRoute";

const AppRoutes = () => {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);

  return (
    <Routes>
      <Route
        path="/login"
        element={isAuthenticated ? <Navigate to="/" replace /> : <LoginPage />}
      />

      <Route path="/oauth/mezon/callback" element={<OAuthCallback />} />

      <Route element={<ProtectedRoute />}>
        <Route path="/" element={<HomePage />} />
      </Route>

      <Route path="*" element={<Navigate to={isAuthenticated ? "/" : "/login"} replace />} />
    </Routes>
  )
};

export default AppRoutes;