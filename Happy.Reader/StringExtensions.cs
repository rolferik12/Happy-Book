﻿namespace Happy.Reader
{
    using System.Text;

    public static class StringExtensions
    {
        public static string RemoveSpecialCharacters(this string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if (c == '-')
                {
                    sb.Append("_");
                }
                else if (c == ' ')
                {
                    sb.Append(" ");
                }
                else if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}
