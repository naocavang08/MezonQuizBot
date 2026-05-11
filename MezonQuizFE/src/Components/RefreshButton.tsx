import { IconButton, Tooltip, type IconButtonProps } from "@mui/material";
import { MdRefresh } from "react-icons/md";
import useRefresh from "../Hooks/useRefresh";
import { useState } from "react";

interface RefreshButtonProps extends IconButtonProps {
    tooltip?: string;
}

const RefreshButton = ({ tooltip = "Refresh data", ...props }: RefreshButtonProps) => {
    const { triggerRefresh } = useRefresh();
    const [rotating, setRotating] = useState(false);

    const handleClick = (e: React.MouseEvent<HTMLButtonElement>) => {
        setRotating(true);
        triggerRefresh();

        // Stop rotating animation after a short delay
        setTimeout(() => {
            setRotating(false);
        }, 500);

        if (props.onClick) {
            props.onClick(e);
        }
    };

    return (
        <Tooltip title={tooltip}>
            <IconButton
                {...props}
                onClick={handleClick}
                sx={{
                    ...props.sx,
                    transition: "transform 0.5s ease-in-out",
                    transform: rotating ? "rotate(360deg)" : "rotate(0deg)",
                }}
            >
                <MdRefresh />
            </IconButton>
        </Tooltip>
    );
};

export default RefreshButton;
