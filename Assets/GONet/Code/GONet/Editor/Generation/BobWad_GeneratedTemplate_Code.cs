/* Copyright (C) Shaun Curtis Sheppard - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 * Written by Shaun Sheppard <shasheppard@gmail.com>, June 2019
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products
 */

using GONet.Generation;
using System.Collections.Generic;

namespace Assets.GONet.Code.GONet.Editor.Generation
{
    public partial class BobWad_GeneratedTemplate
    {
        private byte maxCodeGenerationId;
        private List<GONetParticipant_ComponentsWithAutoSyncMembers> allUniqueSnapsForPersistence;

        /// <summary>
        /// memberOwnerTypeName => [memberName => memberTypeName]
        /// </summary>
        private readonly Dictionary<string, Dictionary<string, string>> uniqueCombos = new Dictionary<string, Dictionary<string, string>>();

        private readonly Dictionary<byte, GONetParticipant_ComponentsWithAutoSyncMembers_Single[]> allUniqueSnapsByCodeGenerationId = new Dictionary<byte, GONetParticipant_ComponentsWithAutoSyncMembers_Single[]>();

        internal BobWad_GeneratedTemplate(byte maxCodeGenerationId, List<GONetParticipant_ComponentsWithAutoSyncMembers> allUniqueSnapsForPersistence)
        {
            this.maxCodeGenerationId = maxCodeGenerationId;
            this.allUniqueSnapsForPersistence = allUniqueSnapsForPersistence;

            { // populate uniqueCombos
                foreach (var l in allUniqueSnapsForPersistence)
                {
                    if (!allUniqueSnapsByCodeGenerationId.ContainsKey(l.codeGenerationId))
                    {
                        allUniqueSnapsByCodeGenerationId[l.codeGenerationId] = l.ComponentMemberNames_By_ComponentTypeFullName;
                    }

                    foreach (var ll in l.ComponentMemberNames_By_ComponentTypeFullName)
                    {
                        Dictionary<string, string> componentTypeNamesMap;
                        if (!uniqueCombos.TryGetValue(ll.componentTypeFullName, out componentTypeNamesMap))
                        {
                            uniqueCombos[ll.componentTypeFullName] = componentTypeNamesMap = new Dictionary<string, string>();
                        }

                        foreach (var lll in ll.autoSyncMembers)
                        {
                            componentTypeNamesMap[lll.memberName] = lll.memberTypeFullName;
                        }
                    }
                }
            }
        }
    }
}
