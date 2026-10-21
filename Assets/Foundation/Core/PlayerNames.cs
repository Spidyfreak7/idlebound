using System;

namespace Idlebound.Foundation
{
    // Display-only normalization. Catalog names, stable IDs and validation remain untouched.
    public static class PlayerNames
    {
        public static string Display(string name)
        {
            if (name == null) return "";
            while (name.StartsWith("[DEV]",StringComparison.Ordinal)) name = name.Substring(5).TrimStart();
            return name;
        }
    }
}
