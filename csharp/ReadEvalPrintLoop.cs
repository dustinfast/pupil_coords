/// .xaml.cs - 
// 
//
//
// Author: Dustin Fast, 2018

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace FLogger
{
    class ReadEvalPrintLoop
    {

        // Availalbe REPL commands - Format is { CommandString: Function }
        // Should be set by user with AddCmd()
        private Dictionary<String, Func<string, Task>> _cmdList;
        private CoreDispatcher _dispatcher;
        private TextBlock _textBlock;
        private string _messageText = string.Empty;
        private readonly object _messageLock = new object();
        private int _messageCount;

        private List<string> _cmdHistory = new List<string>();


        // Constructor - Creates an empty _cmdlist
        //  outputlist = the output "window", used to communicate with user
        public ReadEvalPrintLoop(TextBlock textBlock)
        {
            _cmdList = new Dictionary<string, Func<string, Task>>();
            _textBlock = textBlock;
            if (textBlock != null)
                _dispatcher = _textBlock.Dispatcher;
        }


        // Adds a cmd to _cmdList
        //  cmd   = the command string. Ex: 'run build 2'
        //  func  = the function to call on receiving cmd - must accept one
        //          string parameter (assumed to be args) and return one Task
        public void AddCmd(String cmd, Func<string, Task> func)
        {
            _cmdList.Add(cmd, func);
        }


        // Adds an attempted command to _cmdHistory, so it can be accessed with up/down arrows.
        public void AddCmdHistory(String cmd)
        {
            _cmdHistory.Add(cmd);
        }


        // Attempts to evaluate the given input string as a command and calls the
        // corresponding function(args). Also adds the string to _cmdHistory
        // Returns true iff cmd was valid (i.e. found in _cmdList)
        public bool DoCmd(String input)
        {
            // Add the input to _cmdHistory
            AddCmdHistory(input);

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
                        Log(retval.Exception.InnerException.Message);
                }

            Log(input);
            if (!cmdOK)
                Log("ERROR: Invalid command");
            return cmdOK;
        }


        // Does output to _textBlock
        internal async void Log(string message)
        {
            var newMessage = $"[{_messageCount++}] {DateTime.Now.ToString("hh:MM:ss")} : {message}\n{_messageText}";

            lock (_messageLock)
            {
                _messageText = newMessage;
            }

            await _dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                lock (_messageLock)
                {
                    _textBlock.Text = _messageText;
                }
            });
        }
    }
}
