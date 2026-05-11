import { useEffect, useRef } from "react";
import { HubConnectionBuilder, LogLevel, type HubConnection, HubConnectionState } from "@microsoft/signalr";
import type { SessionStateChangedDto } from "../Interface/session.dto";

type UseSessionRealtimeOptions = {
    sessionId?: string;
    quizId?: string;
    onSessionStateChanged: () => void | Promise<void>;
    pollingMs?: number;
    joinGroup?: boolean;
    enabled?: boolean;
};

const resolveHubUrl = () => "/hubs/quiz-session";

const useSessionRealtime = ({
    sessionId,
    quizId,
    onSessionStateChanged,
    pollingMs = 5000,
    joinGroup = true,
    enabled = true,
}: UseSessionRealtimeOptions) => {
    const callbackRef = useRef(onSessionStateChanged);
    callbackRef.current = onSessionStateChanged;

    useEffect(() => {
        if (!enabled || (joinGroup && !sessionId && !quizId)) {
            return;
        }

        let connection: HubConnection | null = null;
        let isDisposed = false;
        let timer: number | undefined;

        const connectHub = async () => {
            try {
                const hub = new HubConnectionBuilder()
                    .withUrl(resolveHubUrl())
                    .configureLogging(LogLevel.Information)
                    .withAutomaticReconnect()
                    .build();

                hub.on("SessionStateChanged", (payload: SessionStateChangedDto) => {
                    if (isDisposed) {
                        return;
                    }

                    if (sessionId && payload.sessionId !== sessionId) {
                        return;
                    }

                    void callbackRef.current();
                });

                await hub.start();

                if (isDisposed) {
                    void hub.stop();
                    return;
                }

                if (joinGroup && sessionId) {
                    await hub.invoke("JoinSessionGroup", sessionId);
                }

                if (joinGroup && quizId) {
                    await hub.invoke("JoinQuizGroup", quizId);
                }

                connection = hub;

                if (timer !== undefined) {
                    window.clearInterval(timer);
                    timer = undefined;
                }
            } catch (err) {
                console.warn("SignalR connection failed, falling back to polling:", err);
            }
        };

        void connectHub();

        timer = window.setInterval(() => {
            void callbackRef.current();
        }, pollingMs);

        return () => {
            isDisposed = true;
            if (timer !== undefined) {
                window.clearInterval(timer);
            }

            if (connection) {
                const conn = connection;
                connection = null;
                if (conn.state === HubConnectionState.Connected) {
                    void conn.stop().catch(err => console.warn("Error stopping SignalR connection:", err));
                }
            }
        };
    }, [enabled, joinGroup, pollingMs, sessionId, quizId]);
};

export default useSessionRealtime;

