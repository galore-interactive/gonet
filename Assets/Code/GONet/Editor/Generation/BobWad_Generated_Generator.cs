using Assets.Code.GONet.Editor.Generation;
using System.IO;

namespace GONet.Generation
{
    internal static class BobWad_Generated_Generator
    {
        internal static void GenerateClass(byte maxCodeGenerationId)
        {
            var t4Template = new BobWad_GeneratedTemplate(maxCodeGenerationId);
            string generatedClassText = t4Template.TransformText();

            if (!Directory.Exists(GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.GENERATED_FILE_PATH))
            {
                Directory.CreateDirectory(GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.GENERATED_FILE_PATH);
            }
            const string GEN_D = "_Generated.cs";
            string writeToPath = string.Concat(GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.GENERATED_FILE_PATH, nameof(BobWad), GEN_D);
            File.WriteAllText(writeToPath, generatedClassText);
        }
    }
}
