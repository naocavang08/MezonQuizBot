import { Box, Button, Typography, Stack, Container } from "@mui/material";
import { useNavigate } from "react-router-dom";

const HomePage = () => {
    const navigate = useNavigate();

    return (
        <Container maxWidth="md">
            <Box
                sx={{
                    textAlign: "center",
                    marginTop: "60px",
                }}
            >
                <Typography variant="h3" fontWeight={700} mb={2}>
                    Welcome Mezon Quiz!
                </Typography>
                <Typography variant="body1" color="text.secondary" mb={6}>
                    Choose an option to get started
                </Typography>

                <Stack
                    direction={{ xs: "column", sm: "row" }}
                    spacing={3}
                    justifyContent="center"
                    alignItems="center"
                >
                    <Button
                        variant="contained"
                        size="large"
                        color="primary"
                        sx={{
                            minWidth: "200px",
                            borderRadius: "8px",
                            fontSize: "1rem",
                        }}
                    >
                        ➕ Find Quiz
                    </Button>

                    <Button
                        variant="outlined"
                        size="large"
                        color="primary"
                        sx={{
                            minWidth: "200px",
                            borderRadius: "8px",
                            fontSize: "1rem",
                        }}
                        onClick={() => {
                            navigate("/user/my-quizzes");
                        }}
                    >
                        📋 My Quiz
                    </Button>
                </Stack>
            </Box>
        </Container>
    );
};

export default HomePage;
