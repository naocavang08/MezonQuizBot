namespace Mezon_sdk.Constants
{
    public static class InternalEventsSocket
    {
        public const string VoiceStartedEvent = "voice_started_event";
        public const string VoiceEndedEvent = "voice_ended_event";
        public const string VoiceJoinedEvent = "voice_joined_event";
        public const string VoiceLeavedEvent = "voice_leaved_event";
        public const string ChannelCreatedEvent = "channel_created_event";
        public const string ChannelDeletedEvent = "channel_deleted_event";
        public const string ChannelUpdatedEvent = "channel_updated_event";
        public const string ClanProfileUpdatedEvent = "clan_profile_updated_event";
        public const string ClanUpdatedEvent = "clan_updated_event";
        public const string StatusPresenceEvent = "status_presence_event";
        public const string StreamPresenceEvent = "stream_presence_event";
        public const string StreamData = "stream_data";
        public const string ChannelMessage = "channel_message";
        public const string MessageTypingEvent = "message_typing_event";
        public const string MessageReactionEvent = "message_reaction_event";
        public const string ChannelPresenceEvent = "channel_presence_event";
        public const string LastPinMessageEvent = "last_pin_message_event";
        public const string CustomStatusEvent = "custom_status_event";
        public const string UserChannelAddedEvent = "user_channel_added_event";
        public const string AddClanUserEvent = "add_clan_user_event";
        public const string UserProfileUpdatedEvent = "user_profile_updated_event";
        public const string UserChannelRemovedEvent = "user_channel_removed_event";
        public const string UserClanRemovedEvent = "user_clan_removed_event";
        public const string RoleEvent = "role_event";
        public const string GiveCoffeeEvent = "give_coffee_event";
        public const string RoleAssignEvent = "role_assign_event";
        public const string TokenSend = "token_sent_event";
        public const string ClanEventCreated = "clan_event_created";
        public const string MessageButtonClicked = "message_button_clicked";
        public const string StreamingJoinedEvent = "streaming_joined_event";
        public const string StreamingLeavedEvent = "streaming_leaved_event";
        public const string DropdownBoxSelected = "dropdown_box_selected";
        public const string WebrtcSignalingFwd = "webrtc_signaling_fwd";
        public const string Notifications = "notifications";
        public const string QuickMenu = "quick_menu_event";
        public const string AiAgentEnable = "ai_agent_enabled_event";
    }

    public static class Events
    {
        public const string ChannelMessage = InternalEventsSocket.ChannelMessage;
        public const string MessageReaction = InternalEventsSocket.MessageReactionEvent;
        public const string UserChannelRemoved = InternalEventsSocket.UserChannelRemovedEvent;
        public const string UserClanRemoved = InternalEventsSocket.UserClanRemovedEvent;
        public const string UserChannelAdded = InternalEventsSocket.UserChannelAddedEvent;
        public const string ChannelCreated = InternalEventsSocket.ChannelCreatedEvent;
        public const string ChannelDeleted = InternalEventsSocket.ChannelDeletedEvent;
        public const string ChannelUpdated = InternalEventsSocket.ChannelUpdatedEvent;
        public const string RoleEvent = InternalEventsSocket.RoleEvent;
        public const string GiveCoffee = InternalEventsSocket.GiveCoffeeEvent;
        public const string RoleAssign = InternalEventsSocket.RoleAssignEvent;
        public const string AddClanUser = InternalEventsSocket.AddClanUserEvent;
        public const string TokenSend = InternalEventsSocket.TokenSend;
        public const string ClanEventCreated = InternalEventsSocket.ClanEventCreated;
        public const string MessageButtonClicked = InternalEventsSocket.MessageButtonClicked;
        public const string StreamingJoinedEvent = InternalEventsSocket.StreamingJoinedEvent;
        public const string StreamingLeavedEvent = InternalEventsSocket.StreamingLeavedEvent;
        public const string DropdownBoxSelected = InternalEventsSocket.DropdownBoxSelected;
        public const string WebrtcSignalingFwd = InternalEventsSocket.WebrtcSignalingFwd;
        public const string VoiceStartedEvent = InternalEventsSocket.VoiceStartedEvent;
        public const string VoiceEndedEvent = InternalEventsSocket.VoiceEndedEvent;
        public const string VoiceJoinedEvent = InternalEventsSocket.VoiceJoinedEvent;
        public const string VoiceLeavedEvent = InternalEventsSocket.VoiceLeavedEvent;
        public const string Notifications = InternalEventsSocket.Notifications;
        public const string QuickMenu = InternalEventsSocket.QuickMenu;
        public const string AiAgentEnable = InternalEventsSocket.AiAgentEnable;
    }

    public enum ChannelType
    {
        ChannelTypeChannel = 1,
        ChannelTypeGroup = 2,
        ChannelTypeDm = 3,
        ChannelTypeGmeetVoice = 4,
        ChannelTypeForum = 5,
        ChannelTypeStreaming = 6,
        ChannelTypeThread = 7,
        ChannelTypeApp = 8,
        ChannelTypeAnnouncement = 9,
        ChannelTypeMezonVoice = 10
    }

    public enum ChannelStreamMode
    {
        StreamModeChannel = 2,
        StreamModeGroup = 3,
        StreamModeDm = 4,
        StreamModeClan = 5,
        StreamModeThread = 6
    }

    public enum TypeMessage
    {
        Chat = 0,
        ChatUpdate = 1,
        ChatRemove = 2,
        Typing = 3,
        Indicator = 4,
        Welcome = 5,
        CreateThread = 6,
        CreatePin = 7,
        MessageBuzz = 8,
        Topic = 9,
        AuditLog = 10,
        SendToken = 11,
        Ephemeral = 12,
        UpcomingEvent = 13,
        UpdateEphemeralMsg = 14,
        DeleteEphemeralMsg = 15
    }
}