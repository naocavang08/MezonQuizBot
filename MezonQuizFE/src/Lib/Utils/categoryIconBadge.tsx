import { Box } from "@mui/material";
import type { ReactNode } from "react";
import { getCategoryIconOption } from "./categoryIconOptions";

interface CategoryIconBadgeProps {
  iconKey?: string | null;
  size?: number;
  fallback?: ReactNode;
}

const CategoryIconBadge = ({ iconKey, size = 22, fallback = "-" }: CategoryIconBadgeProps) => {
  const option = getCategoryIconOption(iconKey);

  if (!option) {
    return <>{fallback}</>;
  }

  const Icon = option.icon;

  return (
    <Box
      component="span"
      sx={{
        width: size,
        height: size,
        borderRadius: "999px",
        display: "inline-flex",
        alignItems: "center",
        justifyContent: "center",
        color: option.color,
        backgroundColor: option.background,
        border: "1px solid rgba(15, 23, 42, 0.08)",
      }}
    >
      <Icon size={Math.max(size - 8, 12)} />
    </Box>
  );
};

export default CategoryIconBadge;
