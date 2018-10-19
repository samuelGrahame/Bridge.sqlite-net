using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLite
{
    public class SQLiteCommand
    {
        public List<SQLiteParameter> Parameters = null;
        public string CommandText;
        public SQLiteConnection Connection;
        internal static SQLiteParser parser;

        public SQLiteCommand(string cmdText)
        {
            CommandText = cmdText;
        }
        public SQLiteCommand(string cmdText, SQLiteConnection connection) : this(cmdText)
        {
            Connection = connection;
        }

        public List<T> ExecuteReader<T>()
        {
            if(string.IsNullOrWhiteSpace(CommandText))
            {
                throw new NullReferenceException(nameof(CommandText));
            }
            if (Connection == null)
            {
                throw new NullReferenceException(nameof(Connection));
            }

            var nodes = parser.Parse(CommandText);

            // execute opcodes...

            return null;
        }
    }
}
