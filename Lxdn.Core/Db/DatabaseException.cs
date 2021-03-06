using System;
using Lxdn.Core.Extensions;

namespace Lxdn.Core.Db
{
    public class DatabaseException : ApplicationException
    {
        // we don't depend on Newtonsoft.Json here, so use a bit trickier method:
        private static readonly Func<object, string> StringifyParameters = parameters =>
            parameters.ToDictionary().Agglutinate(parameter => $"{parameter.Key}={parameter.Value}", ", ");

        public DatabaseException(Schema schema, object parameters, Exception inner) 
            : base($"Error executing an sql on connection '{schema.ConnectionString}', parameters: ({StringifyParameters(parameters)})", inner)
        {
        }
    }
}