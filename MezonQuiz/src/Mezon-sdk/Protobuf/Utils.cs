using System;
using Google.Protobuf;
using Mezon.Protobuf.Realtime;

public static class ProtobufHelper
{
    /// <summary>
    /// Parse bytes message to Envelope
    /// </summary>
    public static Envelope ParseProtobuf(byte[] message)
    {
        var envelope = new Envelope();
        envelope.MergeFrom(message);
        return envelope;
    }

    /// <summary>
    /// Encode protobuf message to bytes
    /// </summary>
    public static byte[] EncodeProtobuf(IMessage message)
    {
        return message.ToByteArray();
    }

    /// <summary>
    /// Parse binary API response to specific protobuf message type
    /// </summary>
    public static T ParseApiProtobuf<T>(byte[] data) where T : IMessage<T>, new()
    {
        var message = new T();
        message.MergeFrom(data);
        return message;
    }

    /// <summary>
    /// Equivalent to Python constant
    /// </summary>
    public const string NEOF_NAME = "message"; 
}