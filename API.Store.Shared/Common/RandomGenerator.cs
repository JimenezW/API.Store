using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API.Store.Shared.Common
{
    public static class RandomGenerator
    {
        public static string GenerateRandomString(int length)
        {
            var random  = new Random();
            var charst = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz$#-_.";

            return new string(Enumerable.Repeat(charst, length).Select(s=> s[random.Next(s.Length)]).ToArray());

        }

    }
}
