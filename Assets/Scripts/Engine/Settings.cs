using System;
using System.Collections.Generic;
using System.Linq;
using Engine.Network.Server;

namespace Engine
{
    public class Settings
    {
        public static string SanitizedServerName(string dirty)
        {
            var clean = SanitizedName(dirty);
            if (IsNullOrWhiteSpace(clean))
                return new ServerSettings().Name;
            else
                return clean;
        }


        static string SanitizedName(string dirty)
        {
            if (string.IsNullOrEmpty(dirty))
                return null;

            var clean = dirty;

            // reserved characters for MiniYAML and JSON
            var disallowedChars = new char[] { '#', '@', ':', '\n', '\t', '[', ']', '{', '}', '"', '`' };
            foreach (var disallowedChar in disallowedChars)
                clean = clean.Replace(disallowedChar.ToString(), string.Empty);

            return clean;
        }

        public static bool IsNullOrWhiteSpace(String value)
        {
            if (value == null) return true;

            for (int i = 0; i < value.Length; i++)
            {
                if (!Char.IsWhiteSpace(value[i])) return false;
            }

            return true;
        }
    }
}
