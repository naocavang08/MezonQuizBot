import type {
    QuizSessionDto,
    QuizSessionQuestionDto,
    SessionParticipantDto,
} from "../../Interface/session.dto";

export const isSameSession = (
    previous: QuizSessionDto | null,
    next: QuizSessionDto | null
): boolean => {
    if (previous === next) {
        return true;
    }

    if (!previous || !next) {
        return false;
    }

    return (
        previous.id === next.id &&
        previous.status === next.status &&
        previous.currentQuestion === next.currentQuestion &&
        previous.participantCount === next.participantCount &&
        previous.startedAt === next.startedAt &&
        previous.finishedAt === next.finishedAt &&
        previous.createdAt === next.createdAt &&
        previous.code === next.code &&
        previous.deepLink === next.deepLink &&
        previous.qrCodeUrl === next.qrCodeUrl &&
        previous.maxParticipants === next.maxParticipants
    );
};

export const isSameLeaderboard = (
    previous: SessionParticipantDto[],
    next: SessionParticipantDto[]
): boolean => {
    if (previous === next) {
        return true;
    }

    if (previous.length !== next.length) {
        return false;
    }

    for (let index = 0; index < previous.length; index += 1) {
        const left = previous[index];
        const right = next[index];

        if (
            left.userId !== right.userId ||
            left.displayName !== right.displayName ||
            left.totalScore !== right.totalScore ||
            left.answersCount !== right.answersCount ||
            left.correctCount !== right.correctCount ||
            left.rank !== right.rank ||
            left.joinedAt !== right.joinedAt
        ) {
            return false;
        }
    }

    return true;
};

export const isSameQuestion = (
    previous: QuizSessionQuestionDto | null,
    next: QuizSessionQuestionDto | null
): boolean => {
    if (previous === next) {
        return true;
    }

    if (!previous || !next) {
        return false;
    }

    if (
        previous.sessionId !== next.sessionId ||
        previous.questionIndex !== next.questionIndex ||
        previous.content !== next.content ||
        previous.mediaUrl !== next.mediaUrl ||
        previous.timeLimitSeconds !== next.timeLimitSeconds ||
        previous.points !== next.points ||
        previous.questionType !== next.questionType ||
        previous.options.length !== next.options.length
    ) {
        return false;
    }

    for (let index = 0; index < previous.options.length; index += 1) {
        const left = previous.options[index];
        const right = next.options[index];

        if (left.index !== right.index || left.content !== right.content) {
            return false;
        }
    }

    return true;
};
