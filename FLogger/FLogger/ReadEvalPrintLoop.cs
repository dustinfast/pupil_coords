using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace FLogger
{
    class ReadEvalPrintLoop
    {

        // REPL command container - Format is { CommandString: Function }
        private Dictionary<String, Func<string, Task>> _cmdList;
        private ListView _outputWindow;

        // Constructor - Creates an empty _cmdlist
        //  outputlist = the output "window", used to communicate with user
        public ReadEvalPrintLoop(ListView outputlist)
        {
            _cmdList = new Dictionary<string, Func<string, Task>>();
            _outputWindow = outputlist;
        }

        // Adds a cmd to _cmdList
        //  cmd   = the command string. Ex: 'run build 2'
        //  func  = the function to call on receiving cmd - must accept one
        //          string parameter (assumed to be args) and return one Task
        public void AddCmd(String cmd, Func<string, Task> func)
        {
            _cmdList.Add(cmd, func);
        }

        // Attempts to evaluate the given input string as a cmd and calls the
        // corresponding function(args)
        // Returns true iff cmd was valid (i.e. found in _cmdList)
        public bool DoCmd(String input)
        {
            // Explode input on whitespace, if any. 
            // Head is then cmd to try, tail is args.
            String trycmd = input;
            String args = null; 
            int w_index = input.IndexOf(' ');

            if (w_index >= 0)
            {
                String[] words = input.Split(' ');
                trycmd = words[0];
                args = input.Substring(w_index + 1, input.Length - w_index - 1);
            }

            bool cmdOK = false;
            foreach (var c in _cmdList)
                if (c.Key.Equals(trycmd))
                {
                    cmdOK = true;
                    Task retval = c.Value.Invoke(args);

                    if (retval.Status == TaskStatus.Faulted)
                        _outputWindow.Items.Add(retval.Exception.InnerException.Message);
                }

            if (!cmdOK)
                _outputWindow.Items.Add("ERROR: Invalid command");
            return cmdOK;
        }
    }
}
