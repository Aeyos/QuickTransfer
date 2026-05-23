using System;
using Sandbox.ModAPI.Ingame;

namespace SpaceEngineers.CommonLibs
{
    class Logger
    {
        IMyTerminalBlock _logBlock;
        uint _maxLines;
        string nowTime => $"[{DateTime.Now:HH:mm:ss}]";
        public Logger(IMyTerminalBlock logBlock, uint maxLines)
        {
            _logBlock = logBlock;
            _maxLines = maxLines;
        }

        public void Trim()
        {
            var lines = _logBlock.CustomData.Split('\n');
            if (lines.Length > _maxLines)
            {
                _logBlock.CustomData = string.Join("\n", lines, lines.Length - (int)_maxLines, (int)_maxLines);
            }
        }

        public void LogTrace(string message)
        {
            _logBlock.CustomData = $"{nowTime} Trace: {message} \n" + _logBlock.CustomData;
            Trim();
        }
        public void Clear()
        {
            _logBlock.CustomData = "";
        }

        public void LogWarning(string message)
        {
            _logBlock.CustomData = $"{nowTime} Warning: {message} \n" + _logBlock.CustomData;
            Trim();
        }

        public void LogError(string message, string errorMessage)
        {
            _logBlock.CustomData = $"{nowTime} Error: {message}. Details: {errorMessage} \n" + _logBlock.CustomData;
            Trim();
        }
    }
}