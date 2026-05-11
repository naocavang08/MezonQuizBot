import { useLocation } from "react-router-dom";
import useRefreshStore from "../Stores/refresh.store";

const useRefresh = (customKey?: string) => {
  const location = useLocation();
  const key = customKey ?? location.pathname;

  const refreshKey = useRefreshStore((state) => state.refreshKeys[key] ?? 0);
  const triggerRefresh = useRefreshStore((state) => state.triggerRefresh);

  const trigger = () => triggerRefresh(key);

  return { refreshKey, triggerRefresh: trigger };
};

export default useRefresh;
