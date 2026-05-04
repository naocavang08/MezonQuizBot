import { Navigate, Route, Routes } from "react-router-dom";
import OAuthCallback from "../Components/OAuth/OAuthCallback";
import LoginPage from "../Pages/Auth/LoginPage";
import DashboardPage from "../Pages/Admin/DashboardPage";
import AuditLogPage from "../Pages/Admin/AuditLogPage";
import QuizPage from "../Pages/Admin/QuizPage";
import RolePage from "../Pages/Admin/RolePage";
import UserPage from "../Pages/Admin/UserPage";
import CategoryPage from "../Pages/Admin/CategoryPage";
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
import { ACCESS_PERMISSIONS, PERMISSIONS, PUBLIC_HOME_PATH, resolveDefaultAppPath } from "../Lib/Utils/permissions";
import StartQuizPage from "../Pages/StartQuizPage";
import QuizLeaderboardPage from "../Pages/QuizLeaderboardPage";
import GlobalSearchPage from "../Pages/Search/GlobalSearchPage";
import LandingLayout from "../Layouts/LandingLayout";
import Explore from "../Pages/PublicPage/ExplorePage";
import PublicCategoryPage from "../Pages/PublicPage/CategoryPage";
import PublicQuizPage from "../Pages/PublicPage/QuizPage";
import PublicQuizDetailPage from "../Pages/PublicPage/QuizDetailPage";
import PublicLeaderboardPage from "../Pages/PublicPage/LeaderboardPage";


const AppRoutes = () => {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const roleName = useAuthStore((state) => state.roleName);
  const permissionName = useAuthStore((state) => state.permissionName);
  const hasSystemRole = useAuthStore((state) => state.hasSystemRole);

  const defaultAppPath = resolveDefaultAppPath(permissionName, hasSystemRole, roleName);

  return (
    <Routes>
      <Route
        path="/login"
        element={isAuthenticated ? <Navigate to={defaultAppPath} replace /> : <LoginPage />}
      />

      <Route path="/oauth/mezon/callback" element={<OAuthCallback />} />

      {/* Public landing */}
      <Route element={<LandingLayout />}>
        <Route path="/" element={<Navigate to={PUBLIC_HOME_PATH} replace />} />
        <Route path={PUBLIC_HOME_PATH} element={<Explore />} />
        <Route path="/categories" element={<PublicCategoryPage />} />
        <Route path="/quizzes" element={<PublicQuizPage />} />
        <Route path="/quizzes/:quizId" element={<PublicQuizDetailPage />} />
        <Route path="/quizzes/:quizId/sessions/:sessionId/leaderboard" element={<PublicLeaderboardPage />} />
      </Route>

      <Route element={<ProtectedRoute />}>

        <Route path="/app" element={<Layout />}>
          <Route index element={<Navigate to={defaultAppPath} replace />} />
          <Route path="search" element={<GlobalSearchPage />} />

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
            <Route path="find-quizzes" element={<FindQuizPage />} />
            <Route path="find-quizzes/:quizId" element={<QuizDetailPage />} />
            <Route path="find-quizzes/:quizId/leaderboard" element={<QuizLeaderboardPage />} />
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
            <Route path="my-quizzes/:quizId/sessions/:sessionId/start-quiz" element={<StartQuizPage />} />
          </Route>

          <Route element={<ProtectedRoute requiredPermissions={[PERMISSIONS.QUIZZES_CREATE]} />}>
            <Route path="create-quiz" element={<CreateQuizPage />} />
          </Route>
        </Route>
      </Route>

      <Route path="*" element={<Navigate to={isAuthenticated ? defaultAppPath : PUBLIC_HOME_PATH} replace />} />
    </Routes>
  )
};

export default AppRoutes;
