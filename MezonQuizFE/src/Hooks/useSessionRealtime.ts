import { useEffect } from "react";
import { HubConnectionBuilder, LogLevel, type HubConnection } from "@microsoft/signalr";
import type { SessionStateChangedDto } from "../Interface/session.dto";

type UseSessionRealtimeOptions = {
    sessionId?: string;
    onSessionStateChanged: () => void | Promise<void>;
    pollingMs?: number;
    joinGroup?: boolean;
    enabled?: boolean;
};

const resolveHubUrl = () => "/hubs/quiz-session";

const useSessionRealtime = ({
    sessionId,
    onSessionStateChanged,
    pollingMs = 5000,
    joinGroup = true,
    enabled = true,
}: UseSessionRealtimeOptions) => {
    useEffect(() => {
        if (!enabled || (joinGroup && !sessionId)) {
            return;
        }

        let connection: HubConnection | null = null;
        let isDisposed = false;

        const connectHub = async () => {
            try {
                const hub = new HubConnectionBuilder()
                    .withUrl(resolveHubUrl())
                    .configureLogging(LogLevel.Warning)
                    .withAutomaticReconnect()
                    .build();

                hub.on("SessionStateChanged", (payload: SessionStateChangedDto) => {
                    if (isDisposed) {
                        return;
                    }

                    if (sessionId && payload.sessionId !== sessionId) {
                        return;
                    }

                    void onSessionStateChanged();
                });

                await hub.start();

                if (joinGroup && sessionId) {
                    await hub.invoke("JoinSessionGroup", sessionId);
                }

                connection = hub;
            } catch {
                // Keep fallback refresh active if realtime connection fails.
            }
        };

        void connectHub();

        const timer = window.setInterval(() => {
            void onSessionStateChanged();
        }, pollingMs);

        return () => {
            isDisposed = true;
            window.clearInterval(timer);

            if (!connection) {
                return;
            }

            if (joinGroup && sessionId) {
                void connection.invoke("LeaveSessionGroup", sessionId).catch(() => undefined);
            }

            void connection.stop().catch(() => undefined);
        };
    }, [enabled, joinGroup, onSessionStateChanged, pollingMs, sessionId]);
};

export default useSessionRealtime;
