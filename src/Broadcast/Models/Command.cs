using System.Collections.Generic;
using System.Xml.Serialization;

namespace Rocket.Plugins.Broadcast.Models
{
    public sealed class Command
    {
        public string Name;
        public string Help;

        [XmlArrayItem("Line")]
        public List<string> Text;
    }
}