import { Avatar, Stack, Typography } from "@mui/material";

type UserIdentityCellProps = {
	userId: string;
	displayName?: string;
	avatarUrl?: string;
	hideUserId?: boolean;
	size?: number;
};

const UserIdentityCell = ({
	userId,
	displayName,
	avatarUrl,
	hideUserId = false,
	size = 30,
}: UserIdentityCellProps) => {
	const resolvedName = (displayName || userId || "Unknown").trim();
	const initials = resolvedName.charAt(0).toUpperCase() || "U";

	return (
		<Stack direction="row" spacing={1.25} alignItems="center">
			<Avatar src={avatarUrl} sx={{ width: size, height: size }}>
				{initials}
			</Avatar>
			<Stack spacing={0.1} minWidth={0}>
				<Typography variant="body2" fontWeight={600} noWrap>
					{resolvedName}
				</Typography>
				{hideUserId ? null : (
					<Typography variant="caption" color="text.secondary" noWrap>
						{userId}
					</Typography>
				)}
			</Stack>
		</Stack>
	);
};

export default UserIdentityCell;
