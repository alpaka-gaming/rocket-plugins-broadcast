using Rocket.API;
using Rocket.Unturned.Chat;
using System.Collections.Generic;

namespace Rocket.Plugins.Broadcast.Commands
{
    public class TextCommand : IRocketCommand
    {
        private List<string> _text;
        private string _name;
        private string _help;

        public TextCommand(string commandName, string commandHelp, List<string> text)
        {
            _name = commandName;
            _help = commandHelp;
            _text = text;
        }

        public List<string> Aliases { get { return new List<string>(); } }

        public void Execute(IRocketPlayer caller, string[] command)
        {
            foreach (var item in _text)
                UnturnedChat.Say(caller, item);
        }

        public string Help { get { return _help; } }

        public string Name { get { return _name; } }

        public List<string> Permissions { get { return new List<string>(); } }

        public string Syntax { get { return ""; } }

        public AllowedCaller AllowedCaller { get { return AllowedCaller.Both; } }
    }
}