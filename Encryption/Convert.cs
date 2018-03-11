using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackRunner
{
    class Convert
    {
        public static bool str_to_bool(string s)
        {
            s = s.ToLower();
            if (s == "true")
            {
                return true;
            }
            return false;
        }
    }
}
