using System;
using UnityEditor.AssetImporters;

namespace WaaS.Unity.Editor
{
    internal static class ComponentWitDecoder
    {
        /// <summary>
        ///     Decodes the type of the component as WIT. Returns an empty string and logs a warning on failure.
        /// </summary>
        public static string Decode(AssetImportContext ctx, byte[] component)
        {
            try
            {
                return NativeMethods.DecodeWit(component);
            }
            catch (Exception ex)
            {
                ctx.LogImportWarning($"Failed to decode the type of the component: {ex.Message}");
                return "";
            }
        }
    }
}
