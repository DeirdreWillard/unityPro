using Google.Protobuf;


    public static class MessageProtoHelp
{
        public static byte[] Serialize<T>(T obj) where T : IMessage
        {
            return obj.ToByteArray();
        }

        public static T Deserialize<T>(byte[] data) where T : IMessage, new()
        {
            var obj = new T();
            var msg = obj.Descriptor.Parser.ParseFrom(data);
            if (msg == null)
            {
                return default(T);
            }

            return (T)msg;
        }
    }
