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

        public List<string> Aliases => new List<string>();

        public void Execute(IRocketPlayer caller, string[] command)
        {
            foreach (var item in _text)
                UnturnedChat.Say(caller, item);
        }

        public string Help => _help;

        public string Name => _name;

        public List<string> Permissions => new List<string>();

        public string Syntax => "";

        public AllowedCaller AllowedCaller => AllowedCaller.Both;
    }
}