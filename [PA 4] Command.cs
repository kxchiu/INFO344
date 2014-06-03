using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;

namespace Share
{
    public enum Command
    {
        AddDomain,
        Start,
        Stop,
        Reset
    }

    public class CMsg
    {
        public Command command { get; set; }
        public string str { get; set; }
        public int inte { get; set; }

        public int targetWorkerRole { get; set; }

        public CloudQueueMessage cloudQueueMsg { get; set; }

        public CMsg() { }

        public CMsg(Command command) : this(command, null) { }

        public CMsg(Command command, string parameter)
        {
            this.command = command;
            this.str = parameter;
        }

        public CMsg(Command command, int parameter)
        {
            this.command = command;
            this.inte = parameter;
        }
    }
}
