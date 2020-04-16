using Rocket.API;
using Rocket.Plugins.Broadcast.Models;
using Rocket.Unturned.Chat;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

namespace Rocket.Plugins.Broadcast
{
    public class Config : IRocketPluginConfiguration
    {
        [XmlArrayItem("Message")]
        [XmlArray(ElementName = "Messages")]
        public List<Message> Messages;

        [XmlArrayItem("Command")]
        [XmlArray(ElementName = "Commands")]
        public List<Command> Commands;

        public bool AnnouncementsEnable { get; set; }
        public int AnnouncementsInterval { get; set; }

        public bool JoinMessageEnable { get; set; }
        public bool LeaveMessageEnable { get; set; }
        public bool DeathMessageEnable { get; set; }

        public string JoinMessageColor { get; set; }
        public string LeaveMessageColor { get; set; }
        public string DeathMessageColor { get; set; }

        public bool GroupMessages { get; set; }
        public bool ExtendedMessages { get; set; }
        public bool SuicideMessages { get; set; }

        [XmlIgnore]
        public Color JoinMessage => UnturnedChat.GetColorFromName(JoinMessageColor, Color.green);

        [XmlIgnore]
        public Color LeaveMessage => UnturnedChat.GetColorFromName(LeaveMessageColor, Color.green);

        [XmlIgnore]
        public Color DeathMessage => UnturnedChat.GetColorFromName(DeathMessageColor, Color.red);

        public void LoadDefaults()
        {
            AnnouncementsEnable = true;
            JoinMessageEnable = true;
            LeaveMessageEnable = true;
            DeathMessageEnable = true;

            JoinMessageColor = "green";
            LeaveMessageColor = "green";
            DeathMessageColor = "red";

            GroupMessages = false;
            ExtendedMessages = false;
            SuicideMessages = true;

            AnnouncementsInterval = 180;

            Messages = new List<Message>();
            Messages.Add(new Message()
            {
                Text = "Type [/rules] to read the server rules",
                Color = "green"
            });

            Commands = new List<Command>();
            Commands.Add(new Command()
            {
                Name = "rules",
                Help = "Shows the server rules",
                Text = new List<string>(new[] { "#1 Kill", "#2 Survive", "#3 Build" })
            });
        }
    }
}