/* eslint-disable react-hooks/set-state-in-effect */
import React, { useEffect, useState } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { mezonCallbackLogin } from '../../Api/login.api';
import { getMezonCallbackParams } from '../../Lib/Utils/auth';
import { resolveDefaultAppPath } from '../../Lib/Utils/permissions';
import useAuthStore from '../../Stores/login.store';

const OAuthCallback: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const setAuth = useAuthStore((state) => state.setAuth);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const { code, state: returnedState } = getMezonCallbackParams(searchParams);

    if (!code || !returnedState) {
      setError('Xác thực thất bại: Thiếu mã xác quyền hoặc trạng thái (state).');
      return;
    }

    const exchangeCodeForToken = async () => {
      try {
        const response = await mezonCallbackLogin({
          code,
          state: returnedState,
        });

        if (!response?.token || !response?.refreshToken) {
          setError('Lỗi từ server khi xử lý đăng nhập Mezon.');
          return;
        }

        setAuth(response);
        navigate(resolveDefaultAppPath(response.permissionName ?? [], response.hasSystemRole ?? false), { replace: true });
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
