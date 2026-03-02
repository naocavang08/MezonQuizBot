import { keyframes } from "@emotion/react";
import styled from "@emotion/styled";
import {
    Box,
    Button,
    Paper,
    Stack,
    TextField,
    Typography,
} from "@mui/material";
import { useForm } from "react-hook-form";
import { MdLogin } from "react-icons/md";
import CopyWrite from "../Components/CopyWrite";
import useLoginPage, { type LoginFormValues } from "../Hooks/useLoginPage";

const fadeIn = keyframes`
    from {
        opacity: 0;
        transform: translateY(8px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
`;

const PageWrapper = styled(Box)`
    min-height: 100vh;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 24px;
`;

const LoginCard = styled(Paper)`
    width: 100%;
    max-width: 420px;
    padding: 32px 24px;
    border-radius: 16px;
    animation: ${fadeIn} 320ms ease-out;
`;

const LoginPage = () => {
    const { isSubmitting, errorMessage, onSubmit, onLoginWithMezon } = useLoginPage();

    const {
        register,
        handleSubmit,
        formState: { errors },
    } = useForm<LoginFormValues>();

    return (
        <PageWrapper>
            <LoginCard elevation={4}>
                <Stack spacing={3}>
                    <Box>
                        <Typography variant="h5" fontWeight={700}>
                            Đăng nhập
                        </Typography>
                        <Typography variant="body2" color="text.secondary" mt={0.5}>
                            Vui lòng nhập thông tin tài khoản để tiếp tục.
                        </Typography>
                    </Box>

                    <Box component="form" noValidate onSubmit={handleSubmit(onSubmit)}>
                        <Stack spacing={2}>
                            <TextField
                                label="Username"
                                fullWidth
                                {...register("username", { required: "Username là bắt buộc" })}
                                error={Boolean(errors.username)}
                                helperText={errors.username?.message}
                            />

                            <TextField
                                label="Password"
                                type="password"
                                fullWidth
                                {...register("password", { required: "Password là bắt buộc" })}
                                error={Boolean(errors.password)}
                                helperText={errors.password?.message}
                            />

                            <Button
                                type="submit"
                                variant="contained"
                                size="large"
                                startIcon={<MdLogin />}
                                loading={isSubmitting}
                                disabled={isSubmitting}
                            >
                                Login
                            </Button>

                            {errorMessage && (
                                <Typography variant="body2" color="error">
                                    {errorMessage}
                                </Typography>
                            )}
                        </Stack>
                    </Box>

                    <Button
                        type="button"
                        variant="outlined"
                        size="large"
                        sx={{ color: "purple" }}
                        startIcon={<img src="/src/assets/mezon_icon.png" alt="Mezon Logo" width={24} height={24} />}
                        onClick={onLoginWithMezon}
                    >
                        Login With Mezon
                    </Button>

                    <CopyWrite />
                </Stack>
            </LoginCard>
        </PageWrapper>
    );
};

export default LoginPage;