import { Navigate, Route, Routes } from "react-router-dom";
import OAuthCallback from "../Components/OAuth/OAuthCallback";
import LoginPage from "../Pages/Auth/LoginPage";
import DashboardPage from "../Pages/Admin/DashboardPage";
import QuizPage from "../Pages/Admin/QuizPage";
import RolePage from "../Pages/Admin/RolePage";
import UserPage from "../Pages/Admin/UserPage";
import CategoryPage from "../Pages/Admin/CategoryPage";
import useAuthStore from "../Stores/login.store";
import ProtectedRoute from "./ProtectedRoute";
import MyQuizPage from "../Pages/MyQuizPage";
import CreateQuizPage from "../Pages/CreateQuizPage";
import QuizSettingPage from "../Pages/QuizSettingPage";
import FindQuizPage from "../Pages/FindQuizPage";
import SessionRoomPage from "../Pages/SessionRoomPage";
import Layout from "../Layouts/Layout";


const AppRoutes = () => {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);

  return (
    <Routes>
      <Route
        path="/login"
        element={isAuthenticated ? <Navigate to="/app" replace /> : <LoginPage />}
      />

      <Route path="/oauth/mezon/callback" element={<OAuthCallback />} />

      <Route element={<ProtectedRoute />}>
        <Route path="/" element={<Navigate to="/app" replace />} />

        <Route path="/app" element={<Layout />}>
          <Route index element={<Navigate to="dashboard" replace />} />
          <Route path="dashboard" element={<DashboardPage />} />
          <Route path="users" element={<UserPage />} />
          <Route path="roles" element={<RolePage />} />
          <Route path="quizzes" element={<QuizPage />} />
          <Route path="categories" element={<CategoryPage />} />
          <Route path="find-quizzes" element={<FindQuizPage />} />
          <Route path="my-quizzes" element={<MyQuizPage />} />
          <Route path="my-quizzes/:quizId/settings" element={<QuizSettingPage />} />
          <Route path="sessions/:sessionId" element={<SessionRoomPage />} />
          <Route path="create-quiz" element={<CreateQuizPage />} />
        </Route>
      </Route>

      <Route path="*" element={<Navigate to={isAuthenticated ? "/app" : "/login"} replace />} />
    </Routes>
  )
};

export default AppRoutes;