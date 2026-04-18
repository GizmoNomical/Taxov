using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Taxov.Data
{
    public class Utility
    {
        public static double RoundToNearest(double number, double factor)
        {
            if (number > 0)
            {
                return ((int)Math.Ceiling((double)number / factor) * factor);
            }
            else
            {
                return ((int)Math.Floor((double)number / factor) * factor);
            }
        }
    }
}
