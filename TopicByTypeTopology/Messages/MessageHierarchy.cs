using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
