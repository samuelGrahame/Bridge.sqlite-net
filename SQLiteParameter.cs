using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLite
{
    public class SQLiteParameter
    {
        public string Name;
        public object Parameter;

        public SQLiteParameter(string name, object parameter)
        {
            Name = name;
            Parameter = parameter;
        }
    }
}
