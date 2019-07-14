using UnityEditor;

namespace Crosstales.BWF.EditorTask
{
    /// <summary>Adds the given define symbols to PlayerSettings define symbols.</summary>
    [InitializeOnLoad]
    public class CompileDefines : Common.EditorTask.BaseCompileDefines
    {

        private static readonly string[] symbols = new string[] {
            "CT_BWF",
        };

        static CompileDefines()
        {
            setCompileDefines(symbols);
        }
    }
}
// © 2017-2019 crosstales LLC (https://www.crosstales.com)