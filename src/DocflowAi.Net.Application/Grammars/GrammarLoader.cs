using System.IO;
using System.Reflection;

namespace DocflowAi.Net.Application.Grammars
{
    public static class GrammarLoader
    {
        public static string Load(string resourceName)
        {
            // resourceName es.: "DocflowAi.Net.Application.Grammars.json_generic.gbnf"
            var asm = Assembly.GetExecutingAssembly();
            using var s = asm.GetManifestResourceStream(resourceName)
                         ?? throw new FileNotFoundException($"Grammar resource not found: {resourceName}");
            using var r = new StreamReader(s);
            return r.ReadToEnd();
        }
    }
}
