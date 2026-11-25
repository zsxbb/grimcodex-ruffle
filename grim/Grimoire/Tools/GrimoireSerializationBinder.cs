using Newtonsoft.Json.Serialization;
using System;
using System.Text.RegularExpressions;

namespace Grimoire.Tools
{
    public sealed class GrimoireSerializationBinder : DefaultSerializationBinder
    {
        public static readonly GrimoireSerializationBinder Instance = new GrimoireSerializationBinder();
        private static readonly string CurrentAssemblyName = typeof(GrimoireSerializationBinder).Assembly.GetName().Name;
        private static readonly Regex LegacyAssemblyRegex = new Regex(@",\s*Grimoire(?=[\]\s,]|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public override Type BindToType(string assemblyName, string typeName)
        {
            if (string.Equals(assemblyName, "Grimoire", StringComparison.OrdinalIgnoreCase))
            {
                assemblyName = CurrentAssemblyName;
            }
            if (!string.IsNullOrEmpty(typeName) && typeName.IndexOf("Grimoire", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                typeName = LegacyAssemblyRegex.Replace(typeName, ", " + CurrentAssemblyName);
            }
            return base.BindToType(assemblyName, typeName);
        }

        public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            base.BindToName(serializedType, out assemblyName, out typeName);
            if (string.Equals(assemblyName, CurrentAssemblyName, StringComparison.OrdinalIgnoreCase))
            {
                assemblyName = "Grimoire";
            }
            if (!string.IsNullOrEmpty(typeName) && typeName.IndexOf(CurrentAssemblyName, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                typeName = typeName.Replace(", " + CurrentAssemblyName, ", Grimoire");
            }
        }
    }
}

