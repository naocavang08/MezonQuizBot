import { useEffect, useMemo, useState } from "react";
import {
  Alert,
  Box,
  Button,
  Card,
  CardActionArea,
  CardContent,
  CircularProgress,
  Container,
  Stack,
  Typography,
} from "@mui/material";
import { useNavigate } from "react-router-dom";
import { getAllCategories } from "../../Api/category.api";
import type { CategoryDto } from "../../Interface/category.dto";
import CategoryIconBadge from "../../Lib/Utils/categoryIconBadge";
import { getCategoryIconOption } from "../../Lib/Utils/categoryIconOptions";
import { dt } from "../../Lib/designTokens";

const CategoryPage = () => {
  const navigate = useNavigate();
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const accentPresets = [
    {
      chipBg: `${dt.colors.primaryFixed}4D`,
      chipColor: dt.colors.onPrimaryFixedVariant,
    },
    {
      chipBg: `${dt.colors.secondaryFixed}4D`,
      chipColor: dt.colors.onSecondaryFixedVariant,
    },
    {
      chipBg: `${dt.colors.tertiaryFixed}4D`,
      chipColor: dt.colors.onTertiaryFixedVariant,
    },
  ];

  const sortedCategories = useMemo(
    () =>
      [...categories].sort((a, b) => {
        const aOrder = a.sortOrder ?? Number.MAX_SAFE_INTEGER;
        const bOrder = b.sortOrder ?? Number.MAX_SAFE_INTEGER;
        if (aOrder !== bOrder) {
          return aOrder - bOrder;
        }
        return a.name.localeCompare(b.name);
      }),
    [categories],
  );

  const fetchCategories = async () => {
    setIsLoading(true);
    setErrorMessage(null);
    try {
      const data = await getAllCategories();
      setCategories(data);
    } catch {
      setErrorMessage("Không thể tải danh sách category. Vui lòng thử lại.");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchCategories();
  }, []);

  return (
    <Box
      component="section"
      id="categories"
      sx={{
        py: { xs: "48px", md: dt.spacing.sectionPadding },
        px: dt.spacing.gutter,
      }}
    >
      <Container sx={{ maxWidth: `${dt.spacing.containerMax} !important` }}>
        <Stack
          direction={{ xs: "column", md: "row" }}
          justifyContent="space-between"
          alignItems={{ xs: "flex-start", md: "flex-end" }}
          mb={dt.spacing.stackLg}
          spacing={2}
        >
          <Box>
            <Typography
              sx={{
                fontFamily: dt.typography.fontFamily,
                ...dt.typography.h2,
                color: dt.colors.onSurface,
              }}
            >
              Curated Categories
            </Typography>
            <Typography
              sx={{
                fontFamily: dt.typography.fontFamily,
                ...dt.typography.bodyMd,
                color: dt.colors.onSurfaceVariant,
                mt: 0.5,
              }}
            >
              Explore professionally authored question banks.
            </Typography>
          </Box>
        </Stack>

        {isLoading ? (
          <Box py={8} display="flex" justifyContent="center">
            <CircularProgress />
          </Box>
        ) : null}

        {!isLoading && errorMessage ? (
          <Alert
            severity="error"
            action={
              <Button color="inherit" size="small" onClick={fetchCategories}>
                Thu lai
              </Button>
            }
          >
            {errorMessage}
          </Alert>
        ) : null}

        {!isLoading && !errorMessage && sortedCategories.length === 0 ? (
          <Box
            sx={{
              borderRadius: dt.radius.lg,
              border: `1px solid ${dt.colors.outlineVariant}`,
              p: 4,
              bgcolor: dt.colors.surfaceContainerLowest,
              textAlign: "center",
            }}
          >
            <Typography
              sx={{
                fontFamily: dt.typography.fontFamily,
                ...dt.typography.bodyMd,
                color: dt.colors.onSurfaceVariant,
              }}
            >
              Chua co category nao duoc tao.
            </Typography>
          </Box>
        ) : null}

        {!isLoading && !errorMessage && sortedCategories.length > 0 ? (
          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: {
                xs: "1fr",
                sm: "repeat(2, 1fr)",
                lg: "repeat(4, 1fr)",
              },
              gap: dt.spacing.stackMd,
            }}
          >
            {sortedCategories.map((category) => (
              <Card key={category.id} variant="outlined" sx={{ borderRadius: dt.radius.md, overflow: "hidden" }}>
                <CardActionArea
                  onClick={() => navigate(`/quizzes?category=${encodeURIComponent(category.id)}`)}
                  sx={{
                    p: 3,
                    height: "100%",
                    position: "relative",
                    display: "flex",
                    flexDirection: "column",
                    alignItems: "flex-start",
                    border: `1px solid ${dt.colors.outlineVariant}`,
                    borderRadius: dt.radius.md,
                    bgcolor: dt.colors.surfaceContainerLowest,
                    transition: "all 0.3s ease",
                    overflow: "hidden",
                    "&:hover": {
                      borderColor: dt.colors.primary,
                      boxShadow: "0 20px 25px -5px rgba(15,23,42,0.18)",
                    },
                  }}
                >
                  <Box
                    sx={{
                      position: "absolute",
                      right: -14,
                      bottom: -14,
                      opacity: 0.03,
                      color: dt.colors.onSurface,
                      transition: "transform 0.5s ease",
                      ".MuiCardActionArea-root:hover &": {
                        transform: "scale(1.1)",
                      },
                    }}
                  >
                    {(() => {
                      const iconOption = getCategoryIconOption(category.icon);
                      const WatermarkIcon = iconOption?.icon;
                      if (!WatermarkIcon) {
                        return null;
                      }
                      return <WatermarkIcon size={96} />;
                    })()}
                  </Box>

                  <CardContent sx={{ p: 0, width: "100%" }}>
                    <Stack direction="row" spacing={1} alignItems="center" mb={2}>
                      <Box
                        sx={{
                          px: 1,
                          py: 0.5,
                          borderRadius: dt.radius.sm,
                          bgcolor: accentPresets[sortedCategories.indexOf(category) % accentPresets.length].chipBg,
                          color: accentPresets[sortedCategories.indexOf(category) % accentPresets.length].chipColor,
                          fontFamily: dt.typography.fontFamily,
                          ...dt.typography.labelSm,
                          fontSize: "10px",
                          lineHeight: 1,
                          letterSpacing: "0.08em",
                          textTransform: "uppercase",
                        }}
                      >
                        {category.slug}
                      </Box>
                      <CategoryIconBadge iconKey={category.icon} size={24} />
                    </Stack>

                    <Typography
                      sx={{
                        fontFamily: dt.typography.fontFamily,
                        ...dt.typography.h3,
                        color: dt.colors.onSurface,
                        mb: 0.75,
                      }}
                    >
                      {category.name}
                    </Typography>

                    <Typography
                      sx={{
                        fontFamily: dt.typography.fontFamily,
                        ...dt.typography.bodyMd,
                        fontSize: "0.9rem",
                        color: dt.colors.onSurfaceVariant,
                      }}
                    >
                      Tập câu hỏi theo chủ đề {category.name}.
                    </Typography>

                    <Stack direction="row" alignItems="center" spacing={0.75} mt={3}>
                      <Typography
                        sx={{
                          fontFamily: dt.typography.fontFamily,
                          ...dt.typography.bodyMd,
                          fontSize: "0.875rem",
                          color: dt.colors.onSurfaceVariant,
                        }}
                      >
                        📝
                      </Typography>
                      <Typography
                        sx={{
                          fontFamily: dt.typography.fontFamily,
                          ...dt.typography.bodyMd,
                          fontSize: "0.875rem",
                          color: dt.colors.onSurfaceVariant,
                        }}
                      >
                        Quizzes by category
                      </Typography>
                    </Stack>
                  </CardContent>
                </CardActionArea>
              </Card>
            ))}
          </Box>
        ) : null}
      </Container>
    </Box>
  );
};

export default CategoryPage;
