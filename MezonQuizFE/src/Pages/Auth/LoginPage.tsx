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
import CopyWrite from "../../Components/CopyWrite";
import useLoginPage, { type LoginFormValues } from "../../Hooks/useLoginPage";
import { generateMezonState } from "../../Lib/Utils/auth";

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
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 24px;
    gap: 24px;
`;

const LoginCard = styled(Paper)`
    width: 100%;
    max-width: 420px;
    padding: 32px 24px;
    border-radius: 16px;
    animation: ${fadeIn} 320ms ease-out;
`;

const LoginPage = () => {
    const { isSubmitting, errorMessage, onSubmit } = useLoginPage();

    const {
        register,
        handleSubmit,
        formState: { errors },
    } = useForm<LoginFormValues>();

    const handleMezonLogin = () => {
        const state = generateMezonState();
        sessionStorage.setItem("mezon_oauth_state", state);
        
        const clientId = import.meta.env.VITE_MEZON_CLIENT_ID;
        const redirectUri = encodeURIComponent(import.meta.env.VITE_MEZON_REDIRECT_URI);
        const scope = encodeURIComponent("openid offline");

        const authUrl = `https://oauth2.mezon.ai/oauth2/auth?client_id=${clientId}&redirect_uri=${redirectUri}&response_type=code&scope=${scope}&state=${state}`;
        window.location.href = authUrl;
    }

    return (
        <PageWrapper>
            <Typography variant="h3" fontWeight={700} textAlign="center">
                Welcome Mezon Quiz
            </Typography>

            <LoginCard elevation={4}>
                <Stack spacing={3}>
                    <Box>
                        <Typography variant="h4" fontWeight={700} textAlign="center">
                            Login
                        </Typography>
                        <Typography variant="body2" color="text.secondary" mt={2} textAlign="center">
                            Please enter your credentials to access your account.
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
                        onClick={handleMezonLogin}
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