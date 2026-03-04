import { Box, Chip, Paper, Stack, Typography } from "@mui/material";

const roles = [
	{ name: "super_admin", displayName: "Super Admin", isSystem: true },
	{ name: "admin", displayName: "Admin", isSystem: false },
	{ name: "moderator", displayName: "Moderator", isSystem: false }
];

const RolePage = () => {
	return (
		<Box>
			<Typography variant="h5" fontWeight={700} mb={2}>
				Role Management
			</Typography>

			<Stack spacing={1.5}>
				{roles.map((role) => (
					<Paper
						key={role.name}
						sx={{
							p: 2,
							border: "1px solid #e5e7eb",
							boxShadow: "none",
							display: "flex",
							justifyContent: "space-between",
							alignItems: "center"
						}}
					>
						<Box>
							<Typography fontWeight={600}>{role.displayName}</Typography>
							<Typography variant="body2" color="text.secondary">
								{role.name}
							</Typography>
						</Box>

						<Chip
							label={role.isSystem ? "System" : "Custom"}
							color={role.isSystem ? "primary" : "default"}
							size="small"
						/>
					</Paper>
				))}
			</Stack>
		</Box>
	);
};

export default RolePage;
