using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure.Interception;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace NeuroSpeech.WebAtoms.Entity
{
    public class DbExceptionLogger : IDbCommandInterceptor
    {
        


        public void NonQueryExecuted(System.Data.Common.DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            Write(command, interceptionContext.Exception, interceptionContext.OriginalException);
        }

        public void NonQueryExecuting(System.Data.Common.DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            Write(command, interceptionContext.Exception, interceptionContext.OriginalException);
        }

        public void ReaderExecuted(System.Data.Common.DbCommand command, DbCommandInterceptionContext<System.Data.Common.DbDataReader> interceptionContext)
        {
            Write(command, interceptionContext.Exception, interceptionContext.OriginalException);
        }

        public void ReaderExecuting(System.Data.Common.DbCommand command, DbCommandInterceptionContext<System.Data.Common.DbDataReader> interceptionContext)
        {
            Write(command, interceptionContext.Exception, interceptionContext.OriginalException);
        }

        public void ScalarExecuted(System.Data.Common.DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            Write(command, interceptionContext.Exception, interceptionContext.OriginalException);
        }

        public void ScalarExecuting(System.Data.Common.DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            Write(command, interceptionContext.Exception, interceptionContext.OriginalException);
        }

        private void Write(System.Data.Common.DbCommand command, Exception exception, Exception original)
        {
            if (original == null && exception == null)
                return;
            if (exception != null)
            {
                Log.Add(new CommandException(command.CommandText, exception.Message, exception));
            }
            if (original != null) {
                Log.Add(new CommandException(command.CommandText, original.Message, original));
            }
        }

        public static List<CommandException> Log
        {
            get {
                List<CommandException> log = HttpContext.Current.Items["DbExceptionLogger"] as List<CommandException>;
                if (log == null) {
                    log = new List<CommandException>();
                    HttpContext.Current.Items["DbExceptionLogger"] = log;
                }
                return log;
            }
        }
    }

    public class CommandException: Exception {
        
        public CommandException(string command,string message, Exception exception):base(message + "\r\n" + command ,exception)
        {
        }


    }
}
