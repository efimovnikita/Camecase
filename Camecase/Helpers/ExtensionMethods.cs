using System;

namespace Camecase.Helpers
{
    public static class ExtensionMethods
    {
        public static string? FirstCharToLowerCase(this string? str)
        {
            if ( !String.IsNullOrEmpty(str) && Char.IsUpper(str[0]))
            {
                return str.Length == 1 ? Char.ToLower(str[0]).ToString() : Char.ToLower(str[0]) + str[1..];
            }

            return str;
        }
    }
}