namespace Mezon_sdk.Models
{
    using Google.Protobuf;
    using Mezon_sdk.Utils;

    public class MezonBaseModel
    {
    }

    public abstract class MezonBaseModel<TSelf> : MezonBaseModel
        where TSelf : class
    {
        public static TSelf? FromProtobuf(IMessage message)
        {
            return ProtoUtils.FromProtobuf<TSelf>(message);
        }
    }
}
