using System.Xml.Serialization;

namespace Rocket.Plugins.Broadcast.Models
{
    public sealed class Message
    {
        [XmlAttribute("Text")]
        public string Text;

        [XmlAttribute("Color")]
        public string Color;

        public Message(string text, string color)
        {
            Text = text;
            Color = color;
        }

        public Message()
        {
            Text = "";
            Color = "";
        }
    }
}