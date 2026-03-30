using Mezon.Protobuf;

namespace xUTest.TestUtils
{
    internal static class TestProtobufFactory
    {
        public static Session CreateSessionProto(
            string token,
            string refreshToken,
            int userId = 123,
            string apiUrl = "https://api.test",
            string idToken = "id-token",
            string wsUrl = "socket.test")
        {
            return new Session
            {
                Token = token,
                RefreshToken = refreshToken,
                UserId = userId,
                ApiUrl = apiUrl,
                IdToken = idToken,
                WsUrl = wsUrl
            };
        }

        public static ClanDescList CreateClanDescListProto()
        {
            var list = new ClanDescList();
            list.Clandesc.Add(new ClanDesc
            {
                ClanId = 99,
                ClanName = "quiz-clan",
                CreatorId = 7,
                Logo = "logo.png",
                Banner = "banner.png",
                Status = 1
            });
            return list;
        }

        public static ChannelDescList CreateChannelDescListProto()
        {
            var list = new ChannelDescList();
            list.Channeldesc.Add(new ChannelDescription
            {
                ChannelId = 11,
                ClanId = 99,
                ChannelLabel = "general",
                Type = 2,
            });
            return list;
        }

        public static ChannelDescription CreateChannelDescriptionProto()
        {
            return new ChannelDescription
            {
                ChannelId = 22,
                ClanId = 99,
                ChannelLabel = "bot-room",
                Type = 3,
            };
        }

        public static QuickMenuAccess CreateQuickMenuAccessProto()
        {
            return new QuickMenuAccess
            {
                Id = 5,
                BotId = 77,
                ClanId = 99,
                ChannelId = 22,
                MenuName = "Quick Quiz",
                Background = "blue",
                ActionMsg = "start"
            };
        }

        public static QuickMenuAccessList CreateQuickMenuAccessListProto()
        {
            var list = new QuickMenuAccessList();
            list.ListMenus.Add(CreateQuickMenuAccessProto());
            return list;
        }
    }
}
