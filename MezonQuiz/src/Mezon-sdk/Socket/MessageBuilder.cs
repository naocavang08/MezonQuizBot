using System.Text.Json;
using Mezon.Protobuf;
using Mezon_sdk.Models;
using Pb = Mezon.Protobuf;
using Rt = Mezon.Protobuf.Realtime;

namespace Mezon_sdk.Socket
{
	/// <summary>
	/// Builder class for constructing ChannelMessageSend protobuf messages.
	/// </summary>
	public static class ChannelMessageBuilder
	{
		private static string PrepareContent(object content)
		{
			return JsonSerializer.Serialize(content);
		}

		private static void AddMentions(Rt.ChannelMessageSend message, IEnumerable<ApiMessageMention> mentions)
		{
			foreach (var mention in mentions)
			{
				var msgMention = new MessageMention();
				if (mention.UserId.HasValue)
				{
					msgMention.UserId = mention.UserId.Value;
				}

				if (!string.IsNullOrWhiteSpace(mention.Username))
				{
					msgMention.Username = mention.Username;
				}

				if (mention.RoleId.HasValue)
				{
					msgMention.RoleId = mention.RoleId.Value;
				}

				if (mention.S.HasValue)
				{
					msgMention.S = mention.S.Value;
				}

				if (mention.E.HasValue)
				{
					msgMention.E = mention.E.Value;
				}

				message.Mentions.Add(msgMention);
			}
		}

		private static void AddAttachments(Rt.ChannelMessageSend message, IEnumerable<ApiMessageAttachment> attachments)
		{
			foreach (var attachment in attachments)
			{
				var msgAttachment = new MessageAttachment();
				if (!string.IsNullOrWhiteSpace(attachment.Filename))
				{
					msgAttachment.Filename = attachment.Filename;
				}

				if (!string.IsNullOrWhiteSpace(attachment.Url))
				{
					msgAttachment.Url = attachment.Url;
				}

				if (!string.IsNullOrWhiteSpace(attachment.Filetype))
				{
					msgAttachment.Filetype = attachment.Filetype;
				}

				if (attachment.Size.HasValue)
				{
					msgAttachment.Size = attachment.Size.Value;
				}

				if (attachment.Width.HasValue)
				{
					msgAttachment.Width = attachment.Width.Value;
				}

				if (attachment.Height.HasValue)
				{
					msgAttachment.Height = attachment.Height.Value;
				}

				message.Attachments.Add(msgAttachment);
			}
		}

		private static void AddReferences(Rt.ChannelMessageSend message, IEnumerable<ApiMessageRef> references)
		{
			foreach (var reference in references)
			{
				var msgRef = new MessageRef
				{
					MessageRefId = reference.MessageRefId,
					MessageSenderId = reference.MessageSenderId,
				};

				if (!string.IsNullOrWhiteSpace(reference.MessageSenderUsername))
				{
					msgRef.MessageSenderUsername = reference.MessageSenderUsername;
				}

				if (!string.IsNullOrWhiteSpace(reference.Content))
				{
					msgRef.Content = reference.Content;
				}

				if (reference.HasAttachment.HasValue)
				{
					msgRef.HasAttachment = reference.HasAttachment.Value;
				}

				message.References.Add(msgRef);
			}
		}

		private static void SetOptionalFields(
			Rt.ChannelMessageSend message,
			bool? anonymousMessage = null,
			bool? mentionEveryone = null,
			string? avatar = null,
			int? code = null,
			long? topicId = null)
		{
			if (anonymousMessage.HasValue)
			{
				message.AnonymousMessage = anonymousMessage.Value;
			}

			if (mentionEveryone.HasValue)
			{
				message.MentionEveryone = mentionEveryone.Value;
			}

			if (!string.IsNullOrWhiteSpace(avatar))
			{
				message.Avatar = avatar;
			}

			if (code.HasValue)
			{
				message.Code = code.Value;
			}

			if (topicId.HasValue && topicId.Value != 0)
			{
				message.TopicId = topicId.Value;
			}
		}

		public static Rt.ChannelMessageSend Build(
			long clanId,
			long channelId,
			int mode,
			bool isPublic,
			object content,
			List<ApiMessageMention>? mentions = null,
			List<ApiMessageAttachment>? attachments = null,
			List<ApiMessageRef>? references = null,
			bool? anonymousMessage = null,
			bool? mentionEveryone = null,
			string? avatar = null,
			int? code = null,
			long? topicId = null)
		{
			var contentStr = PrepareContent(content);
			var message = new Rt.ChannelMessageSend
			{
				ClanId = clanId,
				ChannelId = channelId,
				Mode = mode,
				IsPublic = isPublic,
				Content = contentStr,
			};

			if (mentions != null && mentions.Count > 0)
			{
				AddMentions(message, mentions);
			}

			if (attachments != null && attachments.Count > 0)
			{
				AddAttachments(message, attachments);
			}

			if (references != null && references.Count > 0)
			{
				AddReferences(message, references);
			}

			SetOptionalFields(
				message,
				anonymousMessage: anonymousMessage,
				mentionEveryone: mentionEveryone,
				avatar: avatar,
				code: code,
				topicId: topicId);

			return message;
		}
	}

	/// <summary>
	/// Builder class for constructing EphemeralMessageSend protobuf messages.
	/// </summary>
	public static class EphemeralMessageBuilder
	{
		public static Rt.EphemeralMessageSend Build(
			List<long> receiverIds,
			long clanId,
			long channelId,
			int mode,
			bool isPublic,
			object content,
			List<ApiMessageMention>? mentions = null,
			List<ApiMessageAttachment>? attachments = null,
			List<ApiMessageRef>? references = null,
			bool? anonymousMessage = null,
			bool? mentionEveryone = null,
			string? avatar = null,
			int? code = null,
			long? topicId = null)
		{
			var channelMessageSend = ChannelMessageBuilder.Build(
				clanId: clanId,
				channelId: channelId,
				mode: mode,
				isPublic: isPublic,
				content: content,
				mentions: mentions,
				attachments: attachments,
				references: references,
				anonymousMessage: anonymousMessage,
				mentionEveryone: mentionEveryone,
				avatar: avatar,
				code: code,
				topicId: topicId);

			var ephemeralMessage = new Rt.EphemeralMessageSend
			{
				Message = channelMessageSend,
			};

			if (receiverIds != null && receiverIds.Count > 0)
			{
				ephemeralMessage.ReceiverIds.Add(receiverIds);
			}

			return ephemeralMessage;
		}
	}

	/// <summary>
	/// Builder class for constructing ChannelMessageUpdate protobuf messages.
	/// </summary>
	public static class ChannelMessageUpdateBuilder
	{
		private static void SetOptionalFields(
			Rt.ChannelMessageUpdate message,
			bool? hideEditted = null,
			long? topicId = null,
			bool? isUpdateMsgTopic = null)
		{
			if (hideEditted.HasValue)
			{
				message.HideEditted = hideEditted.Value;
			}

			if (topicId.HasValue && topicId.Value != 0)
			{
				message.TopicId = topicId.Value;
			}

			if (isUpdateMsgTopic.HasValue)
			{
				message.IsUpdateMsgTopic = isUpdateMsgTopic.Value;
			}
		}

		public static Rt.ChannelMessageUpdate Build(
			long clanId,
			long channelId,
			int mode,
			bool isPublic,
			long messageId,
			object content,
			List<ApiMessageMention>? mentions = null,
			List<ApiMessageAttachment>? attachments = null,
			bool? hideEditted = null,
			long? topicId = null,
			bool? isUpdateMsgTopic = null)
		{
			var contentStr = JsonSerializer.Serialize(content);
			var message = new Rt.ChannelMessageUpdate
			{
				ClanId = clanId,
				ChannelId = channelId,
				Mode = mode,
				IsPublic = isPublic,
				MessageId = messageId,
				Content = contentStr,
			};

			if (mentions != null && mentions.Count > 0)
			{
				foreach (var mention in mentions)
				{
					var msgMention = new MessageMention();
					if (mention.UserId.HasValue)
					{
						msgMention.UserId = mention.UserId.Value;
					}

					if (!string.IsNullOrWhiteSpace(mention.Username))
					{
						msgMention.Username = mention.Username;
					}

					if (mention.RoleId.HasValue)
					{
						msgMention.RoleId = mention.RoleId.Value;
					}

					if (mention.S.HasValue)
					{
						msgMention.S = mention.S.Value;
					}

					if (mention.E.HasValue)
					{
						msgMention.E = mention.E.Value;
					}

					message.Mentions.Add(msgMention);
				}
			}

			if (attachments != null && attachments.Count > 0)
			{
				foreach (var attachment in attachments)
				{
					var msgAttachment = new MessageAttachment();
					if (!string.IsNullOrWhiteSpace(attachment.Filename))
					{
						msgAttachment.Filename = attachment.Filename;
					}

					if (!string.IsNullOrWhiteSpace(attachment.Url))
					{
						msgAttachment.Url = attachment.Url;
					}

					if (!string.IsNullOrWhiteSpace(attachment.Filetype))
					{
						msgAttachment.Filetype = attachment.Filetype;
					}

					if (attachment.Size.HasValue)
					{
						msgAttachment.Size = attachment.Size.Value;
					}

					if (attachment.Width.HasValue)
					{
						msgAttachment.Width = attachment.Width.Value;
					}

					if (attachment.Height.HasValue)
					{
						msgAttachment.Height = attachment.Height.Value;
					}

					message.Attachments.Add(msgAttachment);
				}
			}

			SetOptionalFields(
				message,
				hideEditted: hideEditted,
				topicId: topicId,
				isUpdateMsgTopic: isUpdateMsgTopic);

			return message;
		}
	}

	/// <summary>
	/// Builder class for constructing MessageReaction protobuf messages.
	/// </summary>
	public static class MessageReactionBuilder
	{
		public static Pb.MessageReaction Build(
			long id,
			long clanId,
			long channelId,
			int mode,
			bool isPublic,
			long messageId,
			long emojiId,
			string emoji,
			int count,
			long messageSenderId,
			bool actionDelete)
		{
			return new Pb.MessageReaction
			{
				Id = id,
				ClanId = clanId,
				ChannelId = channelId,
				Mode = mode,
				IsPublic = isPublic,
				MessageId = messageId,
				EmojiId = emojiId,
				Emoji = emoji,
				Count = count,
				MessageSenderId = messageSenderId,
				Action = actionDelete,
			};
		}
	}
}
