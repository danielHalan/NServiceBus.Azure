namespace Messages
{
    public class BaseMessage
    {
        public string Id { get; set; }
    }

    public class MiddleMessage : BaseMessage
    {
    }

    public class LeafMessage : MiddleMessage
    {
    }

    public class SecondLeafMessage : MiddleMessage
    {
    }
}
