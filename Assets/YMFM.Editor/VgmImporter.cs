using System.IO;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.AssetImporters;
using Ymfm.Vgm;

namespace Ymfm.Editor
{
    [InitializeOnLoad]
    [ScriptedImporter(1, new[] { "vgm", "vgz" })]
    internal sealed class VgmImporter : ScriptedImporter
    {
        public override void OnImportAsset([NotNull] AssetImportContext ctx)
        {
            var vgm = File.ReadAllBytes(ctx.assetPath);

            var data = VgmFile.CreateInstance(vgm); 

            ctx.AddObjectToAsset("VGM", data);
            ctx.SetMainObject(data);
        }
    }
}
