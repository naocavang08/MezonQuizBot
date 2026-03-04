/* eslint-disable react-hooks/set-state-in-effect */
import React, { useEffect, useState } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { mezonCallbackLogin } from '../../Api/login.api';
import useAuthStore from '../../Stores/login.store';

const OAuthCallback: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const setAuth = useAuthStore((state) => state.setAuth);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const code = searchParams.get('code');
    const returnedState = searchParams.get('state');
    const savedState = sessionStorage.getItem('mezon_oauth_state');

    if (!code || !returnedState || returnedState !== savedState) {
      setError('Xác thực thất bại: Trạng thái (State) không hợp lệ hoặc thiếu mã xác quyền.');
      return;
    }

    sessionStorage.removeItem('mezon_oauth_state');

    const exchangeCodeForToken = async () => {
      try {
        const response = await mezonCallbackLogin({
          code,
          state: returnedState,
          redirectUri: import.meta.env.VITE_MEZON_REDIRECT_URI,
        });

        if (!response?.token) {
          setError('Lỗi từ server khi xử lý đăng nhập Mezon.');
          return;
        }

        setAuth(response);
        navigate(response.hasSystemRole ? '/admin/dashboard' : '/user/home', { replace: true });
      } catch {
        setError('Không thể kết nối tới server hoặc xác thực thất bại.');
      }
    };

    exchangeCodeForToken();
  }, [searchParams, navigate, setAuth]);

  if (error) return <div>Lỗi: {error}</div>;
  return <div>Đang xử lý đăng nhập, vui lòng chờ...</div>;
};

export default OAuthCallback;