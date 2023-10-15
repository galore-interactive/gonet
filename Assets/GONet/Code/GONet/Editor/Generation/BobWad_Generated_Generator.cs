/* GONet (TM, serial number 88592370), Copyright (c) 2019-2023 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@galoreinteractive.com
 * 
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 */

using Assets.GONet.Code.GONet.Editor.Generation;
using System;
using System.Collections.Generic;
using System.IO;

namespace GONet.Generation
{
    internal static class BobWad_Generated_Generator
    {
        internal static void GenerateClass(byte[] usedCodegenIds, List<GONetParticipant_ComponentsWithAutoSyncMembers> allUniqueSnapsForPersistence, List<Type> allUniqueIGONetEventStructTypes)
        {
            var t4Template = new BobWad_GeneratedTemplate(usedCodegenIds, allUniqueSnapsForPersistence, allUniqueIGONetEventStructTypes);
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

    internal static class SyncEvent_GeneratedTypes_Generator
    {
        internal static void GenerateEnum(List<GONetParticipant_ComponentsWithAutoSyncMembers> allUniqueSnapsForPersistence)
        {
            var t4TemplateEnum = new SyncEvent_GeneratedTypes_GeneratedTemplate(allUniqueSnapsForPersistence);
            string generatedClassText = t4TemplateEnum.TransformText();

            if (!Directory.Exists(GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.GENERATED_FILE_PATH))
            {
                Directory.CreateDirectory(GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.GENERATED_FILE_PATH);
            }

            string writeToPath = string.Concat(GONetParticipant_AutoMagicalSyncCompanion_Generated_Generator.GENERATED_FILE_PATH, nameof(SyncEvent_GeneratedTypes), ".cs");
            File.WriteAllText(writeToPath, generatedClassText);
        }
    }
}