import { Navigate, Route, Routes } from "react-router-dom";
import OAuthCallback from "../Components/OAuth/OAuthCallback";
import LoginPage from "../Pages/Auth/LoginPage";
import DashboardPage from "../Pages/Admin/DashboardPage";
import AuditLogPage from "../Pages/Admin/AuditLogPage";
import QuizPage from "../Pages/Admin/QuizPage";
import RolePage from "../Pages/Admin/RolePage";
import UserPage from "../Pages/Admin/UserPage";
import CategoryPage from "../Pages/Admin/CategoryPage";
import LeaderboardPage from "../Pages/LeaderboardPage";
import useAuthStore from "../Stores/login.store";
import ProtectedRoute from "./ProtectedRoute";
import MyQuizPage from "../Pages/MyQuizPage";
import CreateQuizPage from "../Pages/CreateQuizPage";
import QuizSettingPage from "../Pages/QuizSettingPage";
import QuizSessionPage from "../Pages/QuizSessionPage";
import FindQuizPage from "../Pages/FindQuizPage";
import QuizDetailPage from "../Pages/QuizDetailPage";
import SessionRoomPage from "../Pages/SessionRoomPage";
import Layout from "../Layouts/Layout";
// import QuizSessionLayout from "../Layouts/QuizSessionLayout";
import { ACCESS_PERMISSIONS, PERMISSIONS, resolveDefaultAppPath } from "../Lib/Utils/permissions";
import StartQuizPage from "../Pages/StartQuizPage";
import PlayerQuizPage from "../Pages/PlayerQuizPage";


const AppRoutes = () => {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const permissionName = useAuthStore((state) => state.permissionName);
  const hasSystemRole = useAuthStore((state) => state.hasSystemRole);

  const defaultAppPath = resolveDefaultAppPath(permissionName, hasSystemRole);

  return (
    <Routes>
      <Route
        path="/login"
        element={isAuthenticated ? <Navigate to="/app" replace /> : <LoginPage />}
      />

      <Route path="/oauth/mezon/callback" element={<OAuthCallback />} />

      <Route element={<ProtectedRoute />}>
        <Route path="/" element={<Navigate to={defaultAppPath} replace />} />

        <Route path="/app" element={<Layout />}>
          <Route index element={<Navigate to={defaultAppPath} replace />} />

          <Route element={<ProtectedRoute requireSystemRole />}>
            <Route element={<ProtectedRoute requiredPermissions={ACCESS_PERMISSIONS.DASHBOARD} />}>
              <Route path="dashboard" element={<DashboardPage />} />
            </Route>

            <Route element={<ProtectedRoute requiredPermissions={[PERMISSIONS.USERS_LIST]} />}>
              <Route path="users" element={<UserPage />} />
            </Route>

            <Route element={<ProtectedRoute requiredPermissions={[PERMISSIONS.ROLES_LIST]} />}>
              <Route path="roles" element={<RolePage />} />
            </Route>

            <Route element={<ProtectedRoute requiredPermissions={ACCESS_PERMISSIONS.QUIZ_MANAGEMENT_PAGE} />}>
              <Route path="quizzes" element={<QuizPage />} />
            </Route>

            <Route element={<ProtectedRoute requiredPermissions={ACCESS_PERMISSIONS.CATEGORY_PAGE} />}>
              <Route path="categories" element={<CategoryPage />} />
            </Route>

            <Route element={<ProtectedRoute requiredPermissions={[PERMISSIONS.AUDIT_LOGS_LIST]} />}>
              <Route path="audit-logs" element={<AuditLogPage />} />
            </Route>
          </Route>

          <Route element={<ProtectedRoute requiredPermissions={ACCESS_PERMISSIONS.QUIZ_WORKSPACE} />}>
            <Route path="leaderboard" element={<LeaderboardPage />} />
            <Route path="find-quizzes" element={<FindQuizPage />} />
            <Route path="find-quizzes/:quizId" element={<QuizDetailPage />} />
          </Route>

          <Route element={<ProtectedRoute requiredPermissions={[PERMISSIONS.QUIZZES_CREATOR_LIST]} />}>
            <Route path="my-quizzes" element={<MyQuizPage />} />
          </Route>

          <Route element={<ProtectedRoute requiredPermissions={[PERMISSIONS.QUIZZES_CREATOR_VIEW]} />}>
            <Route path="my-quizzes/:quizId/settings" element={<QuizSettingPage />} />
            <Route path="my-quizzes/:quizId/sessions" element={<QuizSessionPage />} />
          </Route>

          <Route element={<ProtectedRoute requiredPermissions={ACCESS_PERMISSIONS.SESSION_ROOM} />}>
            <Route path="my-quizzes/:quizId/sessions/:sessionId" element={<SessionRoomPage />} />
            <Route path="my-quizzes/:quizId/sessions/:sessionId/play" element={<PlayerQuizPage />} />
            <Route path="my-quizzes/:quizId/sessions/:sessionId/start-quiz" element={<StartQuizPage />} />
          </Route>

          <Route element={<ProtectedRoute requiredPermissions={[PERMISSIONS.QUIZZES_CREATE]} />}>
            <Route path="create-quiz" element={<CreateQuizPage />} />
          </Route>
        </Route>
      </Route>

      <Route path="*" element={<Navigate to={isAuthenticated ? "/app" : "/login"} replace />} />
    </Routes>
  )
};

export default AppRoutes;
