using System.Collections.Generic;

namespace Portal.Models
{
    public class MessageOrder
    {
        public Message Message { get; set; }
        public List<RKNet_Model.TT.TT> TTs { get; set; }
    }
}
