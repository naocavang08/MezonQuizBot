import { Typography } from "@mui/material";

const CopyWrite = () => {
  return (
    <Typography variant="caption" color="text.secondary" textAlign="center">
      Copyright © {new Date().getFullYear()} Mezon Quiz Bot. All rights reserved.
    </Typography>
  );
};

export default CopyWrite;