using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PathFinder
{
    class MyBot
    {

        public static string LocationToKey(Location location)
        {
            return location.Row + "," + location.Col;
        }

    }
}
