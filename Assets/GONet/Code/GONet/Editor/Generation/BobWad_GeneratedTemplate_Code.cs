/* GONet (TM pending, serial number 88592370), Copyright (c) 2019 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 * 
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 */

using GONet.Generation;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.GONet.Code.GONet.Editor.Generation
{
    public partial class BobWad_GeneratedTemplate
    {
        private byte maxCodeGenerationId;
        private List<GONetParticipant_ComponentsWithAutoSyncMembers> allUniqueSnapsForPersistence;

        /// <summary>
        /// memberOwnerTypeName => [memberName => memberTypeName]
        /// </summary>
        private readonly Dictionary<string, Dictionary<string, ValueTuple<string, AnimatorControllerParameterGenerationInfo>>> uniqueCombos = new Dictionary<string, Dictionary<string, ValueTuple<string, AnimatorControllerParameterGenerationInfo>>>();

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
                        Dictionary<string, ValueTuple<string, AnimatorControllerParameterGenerationInfo>> componentTypeNamesMap;
                        string componentTypeName = ll.componentTypeFullName;
                        if (ll.componentTypeFullName == typeof(Animator).FullName)
                        {
                            componentTypeName += ll.autoSyncMembers[0].animatorControllerName; // NOTE: just "randomly" grabbling the first one and ASSuming they are all the same, which is a good assumption at the time of writing!
                        }
                        if (!uniqueCombos.TryGetValue(componentTypeName, out componentTypeNamesMap))
                        {
                            uniqueCombos[componentTypeName] = componentTypeNamesMap = new Dictionary<string, ValueTuple<string, AnimatorControllerParameterGenerationInfo>>();
                        }

                        foreach (var lll in ll.autoSyncMembers)
                        {
                            string memberTypeFullName = lll.animatorControllerParameterId == 0 ? lll.memberTypeFullName : lll.animatorControllerParameterTypeFullName;

                            ValueTuple<string, AnimatorControllerParameterGenerationInfo> memberTuple = 
                                ValueTuple.Create(
                                    memberTypeFullName,
                                    lll.animatorControllerParameterId == 0 ? null : new AnimatorControllerParameterGenerationInfo(lll.animatorControllerName, lll.animatorControllerParameterName));

                            string memberName = lll.memberName;
                            if (memberTuple.Item2 != null)
                            {
                                const string UNDIE = "_";
                                memberName += string.Concat(UNDIE, memberTuple.Item2.ControllerParameterName);
                            }
                            componentTypeNamesMap[memberName] = memberTuple;
                        }
                    }
                }
            }
        }
    }

    public class AnimatorControllerParameterGenerationInfo
    {
        public string ControllerName;
        public string ControllerParameterName;

        public AnimatorControllerParameterGenerationInfo(string controllerName, string controllerParameterName)
        {
            ControllerName = controllerName;
            ControllerParameterName = controllerParameterName;
        }
    }
}
