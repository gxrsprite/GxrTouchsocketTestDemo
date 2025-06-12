using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TouchSocket.Core;
using TouchLogLevel = TouchSocket.Core.LogLevel;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;



public class TouchLogAdapterILogger : ILog
{
    ILogger logger;

    public TouchLogAdapterILogger(ILogger logger)
    {
        this.logger = logger;
    }

    public TouchLogLevel LogLevel
    {
        get
        {
            if (this.logger.IsEnabled(MsLogLevel.Trace))
                return TouchLogLevel.Trace;
            else if (this.logger.IsEnabled(MsLogLevel.Debug))
                return TouchLogLevel.Debug;
            else if (this.logger.IsEnabled(MsLogLevel.Information))
                return TouchLogLevel.Info;
            else if (this.logger.IsEnabled(MsLogLevel.Warning))
                return TouchLogLevel.Warning;
            else if (this.logger.IsEnabled(MsLogLevel.Error))
                return TouchLogLevel.Error;
            else if (this.logger.IsEnabled(MsLogLevel.Critical))
                return TouchLogLevel.Critical;
            else if (this.logger.IsEnabled(MsLogLevel.None))
                return TouchLogLevel.None;
            return TouchLogLevel.None;
        }

        set => throw new NotImplementedException();
    }

    public string DateTimeFormat { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public void Log(TouchLogLevel logLevel, object source, string message, Exception exception)
    {
        this.logger.Log(GetLevel(logLevel), exception, message);
    }

    MsLogLevel GetLevel(TouchLogLevel level)
    {
        return level switch
        {
            TouchLogLevel.Trace => MsLogLevel.Trace,
            TouchLogLevel.Debug => MsLogLevel.Debug,
            TouchLogLevel.Info => MsLogLevel.Information,
            TouchLogLevel.Warning => MsLogLevel.Warning,
            TouchLogLevel.Error => MsLogLevel.Error,
            TouchLogLevel.Critical => MsLogLevel.Critical,
            TouchLogLevel.None => MsLogLevel.None,
            _ => MsLogLevel.None,
        };
    }
}