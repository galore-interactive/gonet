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

using System;
using System.Globalization;
using System.Text;
using GONet.Generation;

namespace GONet.Editor.Generation
{
    /// <summary>
    /// Pure C# code generator that replaces the T4 template system for GONetParticipant AutoMagicalSync companion class generation.
    /// This eliminates the dependency on Visual Studio's T4 engine and makes the code generation more maintainable.
    /// </summary>
    internal sealed class GONetParticipant_AutoMagicalSyncCompanion_CodeGenerator
    {
        private readonly GONetParticipant_ComponentsWithAutoSyncMembers uniqueEntry;
        private readonly StringBuilder sb;
        private readonly string className;

        public string ClassName => className;

        public GONetParticipant_AutoMagicalSyncCompanion_CodeGenerator(GONetParticipant_ComponentsWithAutoSyncMembers uniqueEntry)
        {
            this.uniqueEntry = uniqueEntry ?? throw new ArgumentNullException(nameof(uniqueEntry));
            this.sb = new StringBuilder(50000); // Pre-allocate for large output
            this.className = $"GONetParticipant_AutoMagicalSyncCompanion_Generated_{uniqueEntry.codeGenerationId}";
        }

        public string Generate()
        {
            sb.Clear();

            WriteHeader();
            WriteUsings();
            WriteNamespaceAndClassDeclaration();
            WriteComponentCachedProperties();
            WriteCodeGenerationIdProperty();
            WriteConstructor();
            WriteSetAutoMagicalSyncValue();
            WriteGetAutoMagicalSyncValue();
            WriteSerializeAll();
            WriteSerializeSingle();
            WriteAreEqualQuantized();
            WriteDeserializeInitAll();
            WriteDeserializeInitSingle_ReadOnlyNotApply();
            WriteInitSingle();
            WriteUpdateLastKnownValues();
            WriteIsLastKnownValue_VeryCloseTo_Or_AlreadyOutsideOf_QuantizationRange();
            WriteCreateNewBaselineValueEvent();
            WriteClassClose();

            return sb.ToString();
        }

        private void WriteHeader()
        {
            sb.AppendLine("/* GONet (TM, serial number 88592370), Copyright (c) 2019-2023 Galore Interactive LLC - All Rights Reserved");
            sb.AppendLine(" * Unauthorized copying of this file, via any medium is strictly prohibited");
            sb.AppendLine(" * Proprietary and confidential, email: contactus@galoreinteractive.com");
            sb.AppendLine(" * ");
            sb.AppendLine(" *");
            sb.AppendLine(" * Authorized use is explicitly limited to the following:\t");
            sb.AppendLine(" * -The ability to view and reference source code without changing it");
            sb.AppendLine(" * -The ability to enhance debugging with source code access");
            sb.AppendLine(" * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products");
            sb.AppendLine(" * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet");
            sb.AppendLine(" * -The ability to modify source code for local use only");
            sb.AppendLine(" * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products");
            sb.AppendLine(" * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet");
            sb.AppendLine(" */");
            sb.AppendLine();
        }

        private void WriteUsings()
        {
            sb.AppendLine("using System;");
            sb.AppendLine("using System.IO;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using System.Text;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using GONet;");
            sb.AppendLine();
        }

        private void WriteNamespaceAndClassDeclaration()
        {
            sb.AppendLine("namespace GONet.Generation");
            sb.AppendLine("{");
            sb.Append("\tinternal sealed class ").Append(className).AppendLine(" : GONetParticipant_AutoMagicalSyncCompanion_Generated");
            sb.AppendLine("    {");
        }

        private void WriteComponentCachedProperties()
        {
            int singleCount = uniqueEntry.ComponentMemberNames_By_ComponentTypeFullName.Length;
            for (int iSingle = 0; iSingle < singleCount; ++iSingle)
            {
                GONetParticipant_ComponentsWithAutoSyncMembers_Single single = uniqueEntry.ComponentMemberNames_By_ComponentTypeFullName[iSingle];

                sb.Append("\t\tprivate ").Append(single.componentTypeFullName).Append(" _").Append(single.componentTypeName).AppendLine(";");
                sb.Append("\t\tinternal ").Append(single.componentTypeFullName).Append(" ").AppendLine(single.componentTypeName);
                sb.AppendLine("\t\t{");
                sb.AppendLine("\t\t\tget");
                sb.AppendLine("\t\t\t{");
                sb.Append("\t\t\t\tif ((object)_").Append(single.componentTypeName).AppendLine(" == null)");
                sb.AppendLine("\t\t\t\t{");
                sb.Append("\t\t\t\t\t_").Append(single.componentTypeName).Append(" = gonetParticipant.GetComponent<").Append(single.componentTypeFullName).AppendLine(">();");
                sb.AppendLine("\t\t\t\t}");
                sb.Append("\t\t\t\treturn _").Append(single.componentTypeName).AppendLine(";");
                sb.AppendLine("\t\t\t}");
                sb.AppendLine("\t\t}");
                sb.AppendLine();
            }
        }

        private void WriteCodeGenerationIdProperty()
        {
            sb.Append("        internal override byte CodeGenerationId => ").Append(uniqueEntry.codeGenerationId).AppendLine(";");
            sb.AppendLine();
        }

        private void WriteConstructor()
        {
            int overallCount = 0;
            Array.ForEach(uniqueEntry.ComponentMemberNames_By_ComponentTypeFullName, uE => overallCount += uE.autoSyncMembers.Length);

            sb.Append("        internal ").Append(className).AppendLine("(GONetParticipant gonetParticipant) : base(gonetParticipant)");
            sb.AppendLine("\t\t{");
            sb.Append("\t\t\tvaluesCount = ").Append(overallCount).AppendLine(";");
            sb.AppendLine("\t\t\t");
            sb.AppendLine("\t\t\tcachedCustomSerializers = cachedCustomSerializersArrayPool.Borrow((int)valuesCount);");
            sb.AppendLine("\t\t\tcachedCustomValueBlendings = cachedCustomValueBlendingsArrayPool.Borrow((int)valuesCount);");
            sb.AppendLine("\t\t\tcachedCustomVelocityBlendings = cachedCustomVelocityBlendingsArrayPool.Borrow((int)valuesCount);");
            sb.AppendLine("\t\t    ");
            sb.AppendLine("\t\t\tlastKnownValueChangesSinceLastCheck = lastKnownValuesChangedArrayPool.Borrow((int)valuesCount);");
            sb.AppendLine("\t\t\tArray.Clear(lastKnownValueChangesSinceLastCheck, 0, lastKnownValueChangesSinceLastCheck.Length);");
            sb.AppendLine();
            sb.AppendLine("\t\t\tlastKnownValueAtRestBits = lastKnownValueAtRestBitsArrayPool.Borrow((int)valuesCount);");
            sb.AppendLine("\t\t\tlastKnownValueChangedAtElapsedTicks = lastKnownValueChangedAtElapsedTicksArrayPool.Borrow((int)valuesCount);");
            sb.AppendLine("\t\t\tfor (int i = 0; i < (int)valuesCount; ++i)");
            sb.AppendLine("\t\t\t{");
            sb.AppendLine("\t\t\t\tlastKnownValueAtRestBits[i] = LAST_KNOWN_VALUE_IS_AT_REST_ALREADY_BROADCASTED; // when things start consider things at rest and alreayd broadcast as to avoid trying to broadcast at rest too early in the beginning");
            sb.AppendLine("\t\t\t\tlastKnownValueChangedAtElapsedTicks[i] = long.MaxValue; // want to start high so the subtraction from actual game time later on does not yield a high value on first times before set with a proper/real value of last change...which would cause an unwanted false positive");
            sb.AppendLine("\t\t\t}");
            sb.AppendLine("\t\t\t");
            sb.AppendLine("            doesBaselineValueNeedAdjusting = doesBaselineValueNeedAdjustingArrayPool.Borrow((int)valuesCount);");
            sb.AppendLine("            Array.Clear(doesBaselineValueNeedAdjusting, 0, doesBaselineValueNeedAdjusting.Length);");
            sb.AppendLine();
            sb.AppendLine("\t\t\tvaluesChangesSupport = valuesChangesSupportArrayPool.Borrow((int)valuesCount);");
            sb.AppendLine("\t\t\t");

            WriteConstructorBody();

            sb.AppendLine("\t\t}");
            sb.AppendLine();
        }

        private void WriteConstructorBody()
        {
            int iOverall = 0;
            int singleCount = uniqueEntry.ComponentMemberNames_By_ComponentTypeFullName.Length;
            for (int iSingle = 0; iSingle < singleCount; ++iSingle)
            {
                GONetParticipant_ComponentsWithAutoSyncMembers_Single single = uniqueEntry.ComponentMemberNames_By_ComponentTypeFullName[iSingle];
                int singleMemberCount = single.autoSyncMembers.Length;
                for (int iSingleMember = 0; iSingleMember < singleMemberCount; ++iSingleMember)
                {
                    GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember singleMember = single.autoSyncMembers[iSingleMember];

                    sb.Append("\t\t\tvar support").Append(iOverall).Append(" = valuesChangesSupport[").Append(iOverall).AppendLine("] = valueChangeSupportArrayPool.Borrow();");

                    if (singleMember.animatorControllerParameterId == 0)
                    {
                        WriteConstructorMemberInit(iOverall, single, singleMember, singleMember.memberTypeFullName, $"{single.componentTypeName}.{singleMember.memberName}");
                    }
                    else
                    {
                        string animatorGetter = $"{single.componentTypeName}.Get{singleMember.animatorControllerParameterMethodSuffix}({singleMember.animatorControllerParameterId})";
                        WriteConstructorMemberInit(iOverall, single, singleMember, singleMember.animatorControllerParameterTypeFullName, animatorGetter);
                    }

                    sb.Append("\t\t\tsupport").Append(iOverall).AppendLine(".syncCompanion = this;");
                    sb.Append("\t\t\tsupport").Append(iOverall).Append(".memberName = \"").Append(singleMember.memberName).AppendLine("\";");
                    sb.Append("\t\t\tsupport").Append(iOverall).Append(".index = ").Append(iOverall).AppendLine(";");
                    sb.Append("\t\t\tsupport").Append(iOverall).Append(".syncAttribute_MustRunOnUnityMainThread = ").Append(singleMember.attribute.MustRunOnUnityMainThread ? "true" : "false").AppendLine(";");
                    sb.Append("\t\t\tsupport").Append(iOverall).Append(".syncAttribute_ProcessingPriority = ").Append(singleMember.attribute.ProcessingPriority).AppendLine(";");
                    sb.Append("\t\t\tsupport").Append(iOverall).Append(".syncAttribute_ProcessingPriority_GONetInternalOverride = ").Append(singleMember.attribute.ProcessingPriority_GONetInternalOverride).AppendLine(";");
                    sb.Append("\t\t\tsupport").Append(iOverall).Append(".syncAttribute_SyncChangesEverySeconds = ").Append(singleMember.attribute.SyncChangesEverySeconds).AppendLine("f;");
                    sb.Append("\t\t\tsupport").Append(iOverall).Append(".syncAttribute_Reliability = AutoMagicalSyncReliability.").Append(singleMember.attribute.Reliability).AppendLine(";");
                    sb.Append("\t\t\tsupport").Append(iOverall).Append(".syncAttribute_ShouldBlendBetweenValuesReceived = ").Append(singleMember.attribute.ShouldBlendBetweenValuesReceived ? "true" : "false").AppendLine(";");
                    sb.Append("\t\t\tsupport").Append(iOverall).Append(".syncAttribute_PhysicsUpdateInterval = ").Append(singleMember.attribute.PhysicsUpdateInterval).AppendLine(";");

                    // Check if this is Transform.position or Transform.rotation - needs special ShouldSkipSync handling
                    bool isTransformPosition = single.componentTypeFullName == "UnityEngine.Transform" && singleMember.memberName == "position";
                    bool isTransformRotation = single.componentTypeFullName == "UnityEngine.Transform" && singleMember.memberName == "rotation";

                    if (isTransformRotation)
                    {
                        sb.Append("\t\t\tsupport").Append(iOverall).AppendLine(".syncAttribute_ShouldSkipSync = GONetMain.IsRotationNotSyncd;");
                    }
                    else if (isTransformPosition)
                    {
                        sb.Append("\t\t\tsupport").Append(iOverall).AppendLine(".syncAttribute_ShouldSkipSync = GONetMain.IsPositionNotSyncd;");
                    }

                    sb.Append("\t\t\t// TODO have to revisit this at some point to get animator parameters working: GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue((").Append(uniqueEntry.codeGenerationId).Append(", ").Append(iOverall).Append("), out support").Append(iOverall).AppendLine(".syncAttribute_ShouldSkipSync);");

                    string lowerBound = singleMember.attribute.QuantizeLowerBound == float.MinValue ? "float.MinValue" : singleMember.attribute.QuantizeLowerBound.ToString(CultureInfo.InvariantCulture) + "f";
                    string upperBound = singleMember.attribute.QuantizeUpperBound == float.MaxValue ? "float.MaxValue" : singleMember.attribute.QuantizeUpperBound.ToString(CultureInfo.InvariantCulture) + "f";
                    sb.Append("\t\t\tsupport").Append(iOverall).Append(".syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(").Append(lowerBound).Append(", ").Append(upperBound).Append(", ").Append(singleMember.attribute.QuantizeDownToBitCount).AppendLine(", true);");
                    sb.AppendLine();

                    if (singleMember.attribute.CustomSerialize_Instance != null)
                    {
                        sb.Append("\t\t\tcachedCustomSerializers[").Append(iOverall).Append("] = GONetAutoMagicalSyncAttribute.GetCustomSerializer<").Append(singleMember.attribute.CustomSerialize_Instance.GetType().FullName.Replace("+", ".")).Append(">(").Append(singleMember.attribute.QuantizeDownToBitCount).Append(", ").Append(singleMember.attribute.QuantizeLowerBound.ToString(CultureInfo.InvariantCulture)).Append("f, ").Append(singleMember.attribute.QuantizeUpperBound.ToString(CultureInfo.InvariantCulture)).AppendLine("f);");
                        sb.AppendLine();
                    }
                    if (singleMember.attribute.CustomValueBlending_Instance != null)
                    {
                        sb.Append("\t\t\tcachedCustomValueBlendings[").Append(iOverall).Append("] = GONetAutoMagicalSyncAttribute.GetCustomValueBlending<").Append(singleMember.attribute.CustomValueBlending_Instance.GetType().FullName.Replace("+", ".")).AppendLine(">();");
                    }

                    // Velocity blending: Populate with default implementations from ValueBlendUtils
                    // Only for velocity-capable types (float, Vector2/3/4, Quaternion) that use value blending
                    if (singleMember.attribute.ShouldBlendBetweenValuesReceived && IsVelocityCapableType(singleMember.memberTypeFullName))
                    {
                        sb.Append("\t\t\tcachedCustomVelocityBlendings[").Append(iOverall).AppendLine("] = GONet.Utils.ValueBlendUtils.GetDefaultVelocityBlending(support" + iOverall + ".lastKnownValue.GONetSyncType);");
                    }

                    if (singleMember.attribute.ShouldBlendBetweenValuesReceived)
                    {
                        sb.Append("            int support").Append(iOverall).Append("_mostRecentChanges_calcdSize = support").Append(iOverall).Append(".syncAttribute_SyncChangesEverySeconds != 0 ? (int)((GONetMain.valueBlendingBufferLeadSeconds / support").Append(iOverall).AppendLine(".syncAttribute_SyncChangesEverySeconds) * 2.5f) : 0;");
                        sb.Append("            support").Append(iOverall).Append(".mostRecentChanges_capacitySize = Math.Max(support").Append(iOverall).AppendLine("_mostRecentChanges_calcdSize, GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.MOST_RECENT_CHANGEs_SIZE_MINIMUM);");
                        sb.Append("\t\t\tsupport").Append(iOverall).Append(".mostRecentChanges = GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.mostRecentChangesPool.Borrow(support").Append(iOverall).AppendLine(".mostRecentChanges_capacitySize);");
                    }
                    sb.AppendLine();

                    iOverall++;
                }
            }
        }

        private void WriteConstructorMemberInit(int iOverall, GONetParticipant_ComponentsWithAutoSyncMembers_Single single, GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember singleMember, string typeFullName, string valueExpression)
        {
            string fieldName = typeFullName.Replace(".", "_");
            sb.Append("            support").Append(iOverall).Append(".baselineValue_current.").Append(fieldName).Append(" = ").Append(valueExpression).AppendLine("; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used");
            sb.Append("            support").Append(iOverall).Append(".lastKnownValue.").Append(fieldName).Append(" = ").Append(valueExpression).AppendLine("; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used");
            sb.Append("            support").Append(iOverall).Append(".lastKnownValue_previous.").Append(fieldName).Append(" = ").Append(valueExpression).AppendLine("; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass \"has anything changed\" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!");
            sb.Append("\t\t\tsupport").Append(iOverall).Append(".valueLimitEncountered_min.").Append(fieldName).Append(" = ").Append(valueExpression).AppendLine("; ");
            sb.Append("\t\t\tsupport").Append(iOverall).Append(".valueLimitEncountered_max.").Append(fieldName).Append(" = ").Append(valueExpression).AppendLine("; ");
        }

        private void WriteSetAutoMagicalSyncValue()
        {
            sb.AppendLine("        internal override void SetAutoMagicalSyncValue(byte index, GONetSyncableValue value)");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\tswitch (index)");
            sb.AppendLine("\t\t\t{");

            int iOverall = 0;
            int singleCount = uniqueEntry.ComponentMemberNames_By_ComponentTypeFullName.Length;
            for (int iSingle = 0; iSingle < singleCount; ++iSingle)
            {
                GONetParticipant_ComponentsWithAutoSyncMembers_Single single = uniqueEntry.ComponentMemberNames_By_ComponentTypeFullName[iSingle];
                int singleMemberCount = single.autoSyncMembers.Length;
                for (int iSingleMember = 0; iSingleMember < singleMemberCount; ++iSingleMember)
                {
                    GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember singleMember = single.autoSyncMembers[iSingleMember];

                    sb.Append("\t\t\t\tcase ").Append(iOverall++).AppendLine(":");
                    if (singleMember.animatorControllerParameterId == 0)
                    {
                        sb.Append("\t\t\t\t\t").Append(single.componentTypeName).Append(".").Append(singleMember.memberName).Append(" = value.").Append(singleMember.memberTypeFullName.Replace(".", "_")).AppendLine(";");
                    }
                    else
                    {
                        sb.Append("\t\t\t\t\t").Append(single.componentTypeName).Append(".Set").Append(singleMember.animatorControllerParameterMethodSuffix).Append("(").Append(singleMember.animatorControllerParameterId).Append(", value.").Append(singleMember.animatorControllerParameterTypeFullName.Replace(".", "_")).AppendLine(");");
                    }
                    sb.AppendLine("\t\t\t\t\treturn;");
                }
            }

            sb.AppendLine("\t\t\t}");
            sb.AppendLine("\t\t}");
            sb.AppendLine();
        }

        private void WriteGetAutoMagicalSyncValue()
        {
            sb.AppendLine("        internal override GONetSyncableValue GetAutoMagicalSyncValue(byte index)");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\tswitch (index)");
            sb.AppendLine("\t\t\t{");

            int iOverall = 0;
            int singleCount = uniqueEntry.ComponentMemberNames_By_ComponentTypeFullName.Length;
            for (int iSingle = 0; iSingle < singleCount; ++iSingle)
            {
                GONetParticipant_ComponentsWithAutoSyncMembers_Single single = uniqueEntry.ComponentMemberNames_By_ComponentTypeFullName[iSingle];
                int singleMemberCount = single.autoSyncMembers.Length;
                for (int iSingleMember = 0; iSingleMember < singleMemberCount; ++iSingleMember)
                {
                    GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember singleMember = single.autoSyncMembers[iSingleMember];

                    sb.Append("\t\t\t\tcase ").Append(iOverall++).AppendLine(":");
                    if (singleMember.animatorControllerParameterId == 0)
                    {
                        sb.Append("\t\t\t\t\treturn ").Append(single.componentTypeName).Append(".").Append(singleMember.memberName).AppendLine(";");
                    }
                    else
                    {
                        sb.Append("\t\t\t\t\treturn ").Append(single.componentTypeName).Append(".Get").Append(singleMember.animatorControllerParameterMethodSuffix).Append("(").Append(singleMember.animatorControllerParameterId).AppendLine(");");
                    }
                }
            }

            sb.AppendLine("\t\t\t}");
            sb.AppendLine();
            sb.AppendLine("\t\t\treturn default;");
            sb.AppendLine("\t\t}");
            sb.AppendLine();
        }

        private void WriteSerializeAll()
        {
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Serializes all values of appropriaate member variables internally to <paramref name=\"bitStream_appendTo\"/>.");
            sb.AppendLine("        /// Oops.  Just kidding....it's ALMOST all values.  The exception being <see cref=\"GONetParticipant.GONetId\"/> because that has to be processed first separately in order");
            sb.AppendLine("        /// to know which <see cref=\"GONetParticipant\"/> we are working with in order to call this method.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        internal override void SerializeAll(Utils.BitByBitByteArrayBuilder bitStream_appendTo)");
            sb.AppendLine("        {");
            sb.AppendLine("            // Velocity-augmented sync: Reset flag for this bundle");
            sb.AppendLine("            didSerializeAnyVelocitySyncedValuesThisBundle = false;");
            sb.AppendLine();
            sb.AppendLine("            // Velocity-augmented sync: Write bundle type bit (0 = VALUE, 1 = VELOCITY)");
            sb.AppendLine("            bitStream_appendTo.WriteBit(nextBundleIsVelocity);");
            sb.AppendLine();

            WriteSerializationBody(true);

            sb.AppendLine();
            sb.AppendLine("            // Velocity-augmented sync: Toggle ONLY if velocity-synced values were actually serialized");
            sb.AppendLine("            // CRITICAL: Prevents empty VELOCITY packets when PhysicsUpdateInterval > 1 gates physics values");
            sb.AppendLine("            if (didSerializeAnyVelocitySyncedValuesThisBundle)");
            sb.AppendLine("            {");
            sb.AppendLine("                ToggleBundleType();");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private void WriteSerializeSingle()
        {
            sb.AppendLine("        internal override void SerializeSingle(Utils.BitByBitByteArrayBuilder bitStream_appendTo, byte singleIndex)");
            sb.AppendLine("        {");
            sb.AppendLine("\t\t\tswitch (singleIndex)");
            sb.AppendLine("\t\t\t{");

            WriteSerializationBody(false);

            sb.AppendLine("\t\t\t}");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private void WriteSerializationBody(bool isSerializeAll)
        {
            int iOverall = 0;
            int singleCount = uniqueEntry.ComponentMemberNames_By_ComponentTypeFullName.Length;
            for (int iSingle = 0; iSingle < singleCount; ++iSingle)
            {
                GONetParticipant_ComponentsWithAutoSyncMembers_Single single = uniqueEntry.ComponentMemberNames_By_ComponentTypeFullName[iSingle];
                int singleMemberCount = single.autoSyncMembers.Length;
                int iSingleMemberStart = 0;

                if (isSerializeAll && single.componentTypeFullName == typeof(GONetParticipant).FullName)
                {
                    iSingleMemberStart = 1; // Skip GONetId
                    ++iOverall;
                }

                for (int iSingleMember = iSingleMemberStart; iSingleMember < singleMemberCount; ++iSingleMember)
                {
                    GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember singleMember = single.autoSyncMembers[iSingleMember];

                    if (!isSerializeAll)
                    {
                        sb.Append("\t\t\t\tcase ").Append(iOverall).AppendLine(":");
                    }

                    sb.Append(isSerializeAll ? "\t\t\t" : "\t\t\t\t").Append("{ // ").Append(single.componentTypeName).Append(".").AppendLine(singleMember.memberName);
                    WriteSingleSerialization(iOverall, single, singleMember, isSerializeAll ? "\t\t\t" : "\t\t\t\t");
                    sb.Append(isSerializeAll ? "\t\t\t" : "\t\t\t\t").AppendLine("}");

                    if (!isSerializeAll)
                    {
                        sb.AppendLine("\t\t\t\tbreak;");
                        sb.AppendLine();
                    }

                    ++iOverall;
                }
            }
        }

        /// <summary>
        /// Checks if a type supports velocity-augmented sync (float, Vector2/3/4, Quaternion).
        /// </summary>
        private bool IsVelocityCapableType(string memberTypeFullName)
        {
            return memberTypeFullName == typeof(float).FullName ||
                   memberTypeFullName == typeof(UnityEngine.Vector2).FullName ||
                   memberTypeFullName == typeof(UnityEngine.Vector3).FullName ||
                   memberTypeFullName == typeof(UnityEngine.Vector4).FullName ||
                   memberTypeFullName == typeof(UnityEngine.Quaternion).FullName;
        }

        /// <summary>
        /// Writes beginning of velocity-aware serialization wrapper (if needed).
        /// Returns true if wrapper was written (velocity sync enabled), false otherwise.
        /// </summary>
        private bool WriteVelocitySerializationHeader(int iOverall, string indent, bool usesVelocitySync, string valueExpression)
        {
            if (usesVelocitySync)
            {
                sb.Append(indent).AppendLine("\tif (nextBundleIsVelocity)");
                sb.Append(indent).AppendLine("\t{");
                sb.Append(indent).AppendLine("\t\t// VELOCITY packet: Calculate and serialize velocity");
                sb.Append(indent).AppendLine("\t\tdidSerializeAnyVelocitySyncedValuesThisBundle = true; // Track that we serialized velocity data");
                sb.Append(indent).AppendLine($"\t\tvar currentValue = {valueExpression};");
                sb.Append(indent).AppendLine($"\t\tvar velocity = CalculateVelocity({iOverall}, currentValue, GONetMain.Time.ElapsedTicks);");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Writes middle section of velocity-aware serialization (between velocity and value blocks).
        /// </summary>
        private void WriteVelocitySerializationMiddle(string indent)
        {
            sb.Append(indent).AppendLine("\t}");
            sb.Append(indent).AppendLine("\telse");
            sb.Append(indent).AppendLine("\t{");
            sb.Append(indent).AppendLine("\t\t// VALUE packet: Serialize position normally");
            sb.Append(indent).AppendLine("\t\tdidSerializeAnyVelocitySyncedValuesThisBundle = true; // Track that we serialized position data");
        }

        /// <summary>
        /// Writes end of velocity-aware serialization wrapper (if needed).
        /// </summary>
        private void WriteVelocitySerializationFooter(string indent, bool usesVelocitySync)
        {
            if (usesVelocitySync)
            {
                sb.Append(indent).AppendLine("\t}");
            }
        }

        private void WriteSingleSerialization(int iOverall, GONetParticipant_ComponentsWithAutoSyncMembers_Single single, GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember singleMember, string indent)
        {
            string memberTypeFullName = singleMember.memberTypeFullName;
            string valueExpression = singleMember.animatorControllerParameterId == 0
                ? $"{single.componentTypeName}.{singleMember.memberName}"
                : $"{single.componentTypeName}.Get{singleMember.animatorControllerParameterMethodSuffix}({singleMember.animatorControllerParameterId})";

            // Check if velocity-augmented sync applies to this member
            bool usesVelocitySync = IsVelocityCapableType(memberTypeFullName) && singleMember.attribute.ShouldBlendBetweenValuesReceived;

            if (singleMember.attribute.CustomSerialize_Instance != null)
            {
                sb.Append(indent).AppendLine("\t    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[" + iOverall + "];");
                if (singleMember.attribute.QuantizeDownToBitCount > 0)
                {
                    // SUB-QUANTIZATION DIAGNOSTIC: Log delta-from-baseline that's actually being serialized
                    string deltaFieldName = memberTypeFullName.Replace(".", "_");
                    sb.Append(indent).AppendLine($"\t{{ // SUB-QUANTIZATION DIAGNOSTIC for {singleMember.memberName}");
                    sb.Append(indent).AppendLine($"\t\tvar currentValue = {valueExpression};");
                    sb.Append(indent).AppendLine($"\t\tvar baselineValue = valuesChangesSupport[{iOverall}].baselineValue_current.{deltaFieldName};");
                    sb.Append(indent).AppendLine($"\t\tvar deltaFromBaseline = currentValue - baselineValue;");
                    sb.Append(indent).AppendLine($"\t\tGONet.Utils.SubQuantizationDiagnostics.CheckAndLogIfSubQuantization(gonetParticipant.GONetId, \"{singleMember.memberName}\", deltaFromBaseline, valuesChangesSupport[{iOverall}].syncAttribute_QuantizerSettingsGroup, customSerializer);");
                    sb.Append(indent).Append("\t\tcustomSerializer.Serialize(bitStream_appendTo, gonetParticipant, deltaFromBaseline").AppendLine(");");
                    sb.Append(indent).AppendLine("\t}");
                }
                else
                {
                    sb.Append(indent).Append("\tcustomSerializer.Serialize(bitStream_appendTo, gonetParticipant, ").Append(valueExpression).AppendLine(");");
                }
            }
            else if (memberTypeFullName == typeof(bool).FullName || singleMember.animatorControllerParameterTypeFullName == typeof(bool).FullName)
            {
                sb.Append(indent).Append("\tbitStream_appendTo.WriteBit(").Append(valueExpression).AppendLine(");");
            }
            else if (memberTypeFullName == typeof(float).FullName || singleMember.animatorControllerParameterTypeFullName == typeof(float).FullName)
            {
                bool wroteVelocityHeader = WriteVelocitySerializationHeader(iOverall, indent, usesVelocitySync, valueExpression);

                if (wroteVelocityHeader)
                {
                    // VELOCITY branch: Serialize velocity
                    if (singleMember.attribute.QuantizeDownToBitCount > 0)
                    {
                        sb.Append(indent).AppendLine("\t\t// Serialize velocity (quantized using velocity quantization settings)");
                        sb.Append(indent).AppendLine("\t\t// TODO: Use velocity quantization settings from profile");
                        sb.Append(indent).Append("\t\tSerializeSingleQuantized(bitStream_appendTo, ").Append(iOverall).Append(", velocity").AppendLine(");");
                    }
                    else
                    {
                        sb.Append(indent).AppendLine("\t\tbitStream_appendTo.WriteFloat(velocity.System_Single);");
                    }

                    WriteVelocitySerializationMiddle(indent);
                }

                // VALUE branch (or non-velocity path)
                string valueIndent = wroteVelocityHeader ? indent + "\t\t" : indent + "\t";
                if (singleMember.attribute.QuantizeDownToBitCount > 0)
                {
                    sb.Append(valueIndent).Append("SerializeSingleQuantized(bitStream_appendTo, ").Append(iOverall).Append(", ").Append(valueExpression).AppendLine(");");
                }
                else
                {
                    sb.Append(valueIndent).Append("bitStream_appendTo.WriteFloat(").Append(valueExpression).AppendLine(");");
                }

                WriteVelocitySerializationFooter(indent, usesVelocitySync);
            }
            else if (memberTypeFullName == typeof(long).FullName)
            {
                sb.Append(indent).Append("\tbitStream_appendTo.WriteLong(").Append(valueExpression).AppendLine(");");
            }
            else if (memberTypeFullName == typeof(uint).FullName)
            {
                sb.Append(indent).Append("\tbitStream_appendTo.WriteUInt(").Append(valueExpression).AppendLine(");");
            }
            else if (memberTypeFullName == typeof(string).FullName)
            {
                sb.Append(indent).Append("                bitStream_appendTo.WriteString(").Append(valueExpression).AppendLine(");");
            }
            else if (memberTypeFullName == typeof(byte).FullName)
            {
                sb.Append(indent).Append("\tbitStream_appendTo.WriteByte(").Append(valueExpression).AppendLine(");");
            }
            else if (memberTypeFullName == typeof(ushort).FullName)
            {
                sb.Append(indent).Append("\tbitStream_appendTo.WriteUShort(").Append(valueExpression).AppendLine(");");
            }
            else if (memberTypeFullName == typeof(short).FullName || memberTypeFullName == typeof(int).FullName || singleMember.animatorControllerParameterTypeFullName == typeof(int).FullName || memberTypeFullName == typeof(sbyte).FullName || memberTypeFullName == typeof(double).FullName)
            {
                sb.Append(indent).Append("\tbyte[] bytes = BitConverter.GetBytes(").Append(valueExpression).AppendLine(");");
                sb.Append(indent).AppendLine("\tint count = bytes.Length;");
                sb.Append(indent).AppendLine("\tfor (int i = 0; i < count; ++i)");
                sb.Append(indent).AppendLine("\t{");
                sb.Append(indent).AppendLine("\t\tbitStream_appendTo.WriteByte(bytes[i]);");
                sb.Append(indent).AppendLine("\t}");
            }
            else if (memberTypeFullName == typeof(UnityEngine.Vector2).FullName || memberTypeFullName == typeof(UnityEngine.Vector3).FullName || memberTypeFullName == typeof(UnityEngine.Vector4).FullName || memberTypeFullName == typeof(UnityEngine.Quaternion).FullName)
            {
                sb.Append(indent).AppendLine("\t    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[" + iOverall + "];");

                bool wroteVelocityHeader = WriteVelocitySerializationHeader(iOverall, indent, usesVelocitySync, valueExpression);

                if (wroteVelocityHeader)
                {
                    // VELOCITY branch: Serialize velocity (linear or angular)
                    if (memberTypeFullName == typeof(UnityEngine.Quaternion).FullName)
                    {
                        sb.Append(indent).AppendLine("\t\t// Angular velocity (omega) serialization for Quaternion");
                        sb.Append(indent).AppendLine("\t\t// velocity is Vector3 (axis * radians/sec), serialize using Vector3Serializer");
                        sb.Append(indent).AppendLine("\t\tvar vector3Serializer = new GONet.Vector3Serializer();");
                        sb.Append(indent).AppendLine("\t\t// TODO: Use velocity quantization settings instead of position settings");
                        sb.Append(indent).AppendLine("\t\tvector3Serializer.Serialize(bitStream_appendTo, gonetParticipant, velocity.UnityEngine_Vector3);");
                    }
                    else
                    {
                        // Vector2/3/4: Serialize velocity
                        sb.Append(indent).AppendLine("\t\t// Serialize velocity (TODO: use velocity quantization settings)");
                        sb.Append(indent).AppendLine("\t\tcustomSerializer.Serialize(bitStream_appendTo, gonetParticipant, velocity);");
                    }

                    WriteVelocitySerializationMiddle(indent);
                }

                // VALUE branch (or non-velocity path)
                string valueIndent = wroteVelocityHeader ? indent + "\t\t" : indent + "\t";
                if (singleMember.attribute.QuantizeDownToBitCount > 0)
                {
                    // SUB-QUANTIZATION DIAGNOSTIC: Log delta-from-baseline that's actually being serialized
                    string deltaFieldName = memberTypeFullName.Replace(".", "_");
                    sb.Append(valueIndent).AppendLine($"{{ // SUB-QUANTIZATION DIAGNOSTIC for {singleMember.memberName}");
                    sb.Append(valueIndent).AppendLine($"\tvar currentValue = {valueExpression};");
                    sb.Append(valueIndent).AppendLine($"\tvar baselineValue = valuesChangesSupport[{iOverall}].baselineValue_current.{deltaFieldName};");
                    sb.Append(valueIndent).AppendLine($"\tvar deltaFromBaseline = currentValue - baselineValue;");
                    sb.Append(valueIndent).AppendLine($"\tGONet.Utils.SubQuantizationDiagnostics.CheckAndLogIfSubQuantization(gonetParticipant.GONetId, \"{singleMember.memberName}\", deltaFromBaseline, valuesChangesSupport[{iOverall}].syncAttribute_QuantizerSettingsGroup, customSerializer);");
                    sb.Append(valueIndent).Append("\tcustomSerializer.Serialize(bitStream_appendTo, gonetParticipant, deltaFromBaseline").AppendLine(");");
                    sb.Append(valueIndent).AppendLine("}");
                }
                else
                {
                    // NOTE: No sub-quantization diagnostic here because this path is for values WITHOUT baseline subtraction
                    // (like Quaternion rotation which uses Smallest3 encoding with quantization handled inside the serializer)
                    sb.Append(valueIndent).Append("customSerializer.Serialize(bitStream_appendTo, gonetParticipant, ").Append(valueExpression).AppendLine(");");
                }

                WriteVelocitySerializationFooter(indent, usesVelocitySync);
            }
        }

        private void WriteAreEqualQuantized()
        {
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// PRE: value at <paramref name=\"singleIndex\"/> is known to be configured to be quantized");
            sb.AppendLine("        /// NOTE: This is only virtual to avoid upgrading customers prior to this being added having compilation issues when upgrading from a previous version of GONet");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        protected override bool AreEqualQuantized(byte singleIndex, GONetSyncableValue valueA, GONetSyncableValue valueB)");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\tswitch (singleIndex)");
            sb.AppendLine("\t\t\t{");

            int iOverall = 0;
            int singleCount = uniqueEntry.ComponentMemberNames_By_ComponentTypeFullName.Length;
            for (int iSingle = 0; iSingle < singleCount; ++iSingle)
            {
                GONetParticipant_ComponentsWithAutoSyncMembers_Single single = uniqueEntry.ComponentMemberNames_By_ComponentTypeFullName[iSingle];
                int singleMemberCount = single.autoSyncMembers.Length;
                for (int iSingleMember = 0; iSingleMember < singleMemberCount; ++iSingleMember)
                {
                    GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember singleMember = single.autoSyncMembers[iSingleMember];

                    sb.Append("\t\t\t\tcase ").Append(iOverall).AppendLine(":");
                    sb.Append("\t\t\t\t{ // ").Append(single.componentTypeName).Append(".").AppendLine(singleMember.memberName);

                    WriteAreEqualQuantizedBody(iOverall, singleMember);

                    sb.AppendLine("\t\t\t\t}");
                    sb.AppendLine("\t\t\t\tbreak;");
                    sb.AppendLine();

                    ++iOverall;
                }
            }

            sb.AppendLine("\t\t\t}");
            sb.AppendLine();
            sb.AppendLine("\t\t\treturn base.AreEqualQuantized(singleIndex, valueA, valueB);");
            sb.AppendLine("\t\t}");
            sb.AppendLine();
        }

        private void WriteAreEqualQuantizedBody(int iOverall, GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember singleMember)
        {
            string memberTypeFullName = singleMember.memberTypeFullName;

            if (singleMember.attribute.CustomSerialize_Instance != null)
            {
                sb.AppendLine("\t\t\t\t    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[" + iOverall + "];");
                sb.AppendLine("\t\t\t\t\treturn customSerializer.AreEqualConsideringQuantization(valueA, valueB);");
            }
            else if (memberTypeFullName == typeof(bool).FullName)
            {
                sb.Append("\t\t\t\t\treturn valueA.").Append(memberTypeFullName.Replace(".", "_")).Append(" == valueB.").Append(memberTypeFullName.Replace(".", "_")).AppendLine(";");
            }
            else if (singleMember.animatorControllerParameterTypeFullName == typeof(bool).FullName)
            {
                sb.Append("\t\t\t\t\treturn valueA.").Append(singleMember.animatorControllerParameterTypeFullName.Replace(".", "_")).Append(" == valueB.").Append(singleMember.animatorControllerParameterTypeFullName.Replace(".", "_")).AppendLine(";");
            }
            else if (memberTypeFullName == typeof(float).FullName || singleMember.animatorControllerParameterTypeFullName == typeof(float).FullName)
            {
                if (singleMember.attribute.QuantizeDownToBitCount > 0)
                {
                    sb.Append("\t\t\t\t\treturn QuantizeSingle(").Append(iOverall).Append(", valueA) == QuantizeSingle(").Append(iOverall).AppendLine(", valueB);");
                }
            }
            else if (memberTypeFullName == typeof(UnityEngine.Vector2).FullName || memberTypeFullName == typeof(UnityEngine.Vector3).FullName || memberTypeFullName == typeof(UnityEngine.Vector4).FullName || memberTypeFullName == typeof(UnityEngine.Quaternion).FullName)
            {
                sb.AppendLine("\t\t\t\t    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[" + iOverall + "];");
                sb.AppendLine("\t\t\t\t\treturn customSerializer.AreEqualConsideringQuantization(valueA, valueB);");
            }
            else
            {
                sb.AppendLine("\t\t\t\t\t// handle quantization of this type eventually?");
            }
        }

        private void WriteDeserializeInitAll()
        {
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Deserializes all values from <paramref name=\"bitStream_readFrom\"/> and uses them to modify appropriate member variables internally.");
            sb.AppendLine("        /// Oops.  Just kidding....it's ALMOST all values.  The exception being <see cref=\"GONetParticipant.GONetId\"/> because that has to be processed first separately in order");
            sb.AppendLine("        /// to know which <see cref=\"GONetParticipant\"/> we are working with in order to call this method.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        internal override void DeserializeInitAll(Utils.BitByBitByteArrayBuilder bitStream_readFrom, long assumedElapsedTicksAtChange)");
            sb.AppendLine("        {");

            WriteDeserializeAllBody();

            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private void WriteDeserializeAllBody()
        {
            int iOverall = 0;
            int singleCount = uniqueEntry.ComponentMemberNames_By_ComponentTypeFullName.Length;
            for (int iSingle = 0; iSingle < singleCount; ++iSingle)
            {
                GONetParticipant_ComponentsWithAutoSyncMembers_Single single = uniqueEntry.ComponentMemberNames_By_ComponentTypeFullName[iSingle];
                int singleMemberCount = single.autoSyncMembers.Length;
                int iSingleMemberStart = 0;

                if (single.componentTypeFullName == typeof(GONetParticipant).FullName)
                {
                    iSingleMemberStart = 1;
                    ++iOverall;
                }

                for (int iSingleMember = iSingleMemberStart; iSingleMember < singleMemberCount; ++iSingleMember)
                {
                    GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember singleMember = single.autoSyncMembers[iSingleMember];

                    sb.Append("\t\t\t{ // ").Append(single.componentTypeName).Append(".").AppendLine(singleMember.memberName);
                    WriteDeserializeSingle(iOverall, single, singleMember, "\t\t\t", true);
                    sb.AppendLine("\t\t\t}");

                    ++iOverall;
                }
            }
        }

        private void WriteInitSingle()
        {
            sb.AppendLine("        internal override void InitSingle(GONetSyncableValue value, byte singleIndex, long assumedElapsedTicksAtChange)");
            sb.AppendLine("        {");
            sb.AppendLine("\t\t\tswitch (singleIndex)");
            sb.AppendLine("\t\t\t{");

            WriteInitSingleBody();

            sb.AppendLine("\t\t\t}");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private void WriteInitSingleBody()
        {
            int iOverall = 0;
            int singleCount = uniqueEntry.ComponentMemberNames_By_ComponentTypeFullName.Length;
            for (int iSingle = 0; iSingle < singleCount; ++iSingle)
            {
                GONetParticipant_ComponentsWithAutoSyncMembers_Single single = uniqueEntry.ComponentMemberNames_By_ComponentTypeFullName[iSingle];
                int singleMemberCount = single.autoSyncMembers.Length;
                for (int iSingleMember = 0; iSingleMember < singleMemberCount; ++iSingleMember)
                {
                    GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember singleMember = single.autoSyncMembers[iSingleMember];

                    // Compact single-line format to match T4 template
                    sb.Append("\t\t\t\tcase ").Append(iOverall).Append(":\t\t\t\t\t");

                    // Apply value based on blend settings
                    if (singleMember.attribute.ShouldBlendBetweenValuesReceived && (singleMember.animatorControllerParameterId == 0 || singleMember.animatorControllerParameterTypeFullName == typeof(float).FullName))
                    {
                        sb.Append("valuesChangesSupport[").Append(iOverall).Append("].AddToMostRecentChangeQueue_IfAppropriate(assumedElapsedTicksAtChange, value); break; // NOTE: this queue will be used each frame to blend between this value and others added there");
                    }
                    else
                    {
                        if (singleMember.animatorControllerParameterId == 0)
                        {
                            sb.Append(single.componentTypeName).Append(".").Append(singleMember.memberName).Append(" = value.").Append(singleMember.memberTypeFullName.Replace(".", "_")).Append("; break;");
                        }
                        else
                        {
                            sb.Append(single.componentTypeName).Append(".Set").Append(singleMember.animatorControllerParameterMethodSuffix).Append("(").Append(singleMember.animatorControllerParameterId).Append(", (").Append(singleMember.animatorControllerParameterTypeFullName).Append(")value.").Append(singleMember.animatorControllerParameterTypeFullName.Replace(".", "_")).Append("); break;");
                        }
                    }
                    sb.AppendLine();

                    ++iOverall;
                }
            }
        }

        private void WriteDeserializeInitSingle_ReadOnlyNotApply()
        {
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Simply deserializes in order to move along the bit stream counter, but does NOT apply the values (i.e, does NOT init).");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        internal override GONet.GONetSyncableValue DeserializeInitSingle_ReadOnlyNotApply(Utils.BitByBitByteArrayBuilder bitStream_readFrom, byte singleIndex)");
            sb.AppendLine("        {");
            sb.AppendLine("\t\t\tswitch (singleIndex)");
            sb.AppendLine("\t\t\t{");

            WriteDeserializeSingleBody(true);

            sb.AppendLine("\t\t\t}");
            sb.AppendLine();
            sb.AppendLine("\t\t\treturn default;");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private void WriteDeserializeSingleBody(bool readOnly)
        {
            int iOverall = 0;
            int singleCount = uniqueEntry.ComponentMemberNames_By_ComponentTypeFullName.Length;
            for (int iSingle = 0; iSingle < singleCount; ++iSingle)
            {
                GONetParticipant_ComponentsWithAutoSyncMembers_Single single = uniqueEntry.ComponentMemberNames_By_ComponentTypeFullName[iSingle];
                int singleMemberCount = single.autoSyncMembers.Length;
                for (int iSingleMember = 0; iSingleMember < singleMemberCount; ++iSingleMember)
                {
                    GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember singleMember = single.autoSyncMembers[iSingleMember];

                    sb.Append("\t\t\t\tcase ").Append(iOverall).AppendLine(":");
                    sb.Append("\t\t\t\t{ // ").Append(single.componentTypeName).Append(".").AppendLine(singleMember.memberName);
                    WriteDeserializeSingle(iOverall, single, singleMember, "\t\t\t\t", false, readOnly);
                    sb.AppendLine("\t\t\t\t\treturn value;");
                    sb.AppendLine("\t\t\t\t}");

                    ++iOverall;
                }
            }
        }

        private void WriteDeserializeSingle(int iOverall, GONetParticipant_ComponentsWithAutoSyncMembers_Single single, GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember singleMember, string indent, bool isDeserializeAll, bool readOnly = false)
        {
            string memberTypeFullName = singleMember.memberTypeFullName;

            if (singleMember.attribute.CustomSerialize_Instance != null)
            {
                sb.Append(indent).AppendLine("\tIGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[" + iOverall + "];");
                if (isDeserializeAll)
                {
                    sb.Append(indent).Append("\tvar value = customSerializer.Deserialize(bitStream_readFrom).").Append(memberTypeFullName.Replace(".", "_")).AppendLine(";");
                    if (singleMember.attribute.QuantizeDownToBitCount > 0)
                    {
                        sb.Append(indent).Append("\tvalue += valuesChangesSupport[").Append(iOverall).Append("].baselineValue_current.").Append(memberTypeFullName.Replace(".", "_")).AppendLine(";");
                    }
                    // For DeserializeAll, apply the value directly
                    if (singleMember.animatorControllerParameterId == 0)
                    {
                        sb.Append(indent).Append("\t").Append(single.componentTypeName).Append(".").Append(singleMember.memberName).AppendLine(" = value;");
                    }
                    else
                    {
                        sb.Append(indent).Append("\t").Append(single.componentTypeName).Append(".Set").Append(singleMember.animatorControllerParameterMethodSuffix).Append("(").Append(singleMember.animatorControllerParameterId).AppendLine(", value);");
                    }
                }
                else if (readOnly) // ReadOnlyNotApply - extract specific type from GONetSyncableValue
                {
                    sb.Append(indent).Append("\tvar value = customSerializer.Deserialize(bitStream_readFrom).").Append(memberTypeFullName.Replace(".", "_")).AppendLine(";");
                    if (singleMember.attribute.QuantizeDownToBitCount > 0)
                    {
                        sb.Append(indent).Append("\tvalue += valuesChangesSupport[").Append(iOverall).Append("].baselineValue_current.").Append(memberTypeFullName.Replace(".", "_")).AppendLine(";");
                    }
                }
            }
            else if (memberTypeFullName == typeof(bool).FullName || singleMember.animatorControllerParameterTypeFullName == typeof(bool).FullName)
            {
                sb.Append(indent).AppendLine("\tbool value;");
                sb.Append(indent).AppendLine("                bitStream_readFrom.ReadBit(out value);");
                if (isDeserializeAll)
                {
                    if (singleMember.animatorControllerParameterId == 0)
                    {
                        sb.Append(indent).Append("\t").Append(single.componentTypeName).Append(".").Append(singleMember.memberName).AppendLine(" = value;");
                    }
                    else
                    {
                        sb.Append(indent).Append("\t").Append(single.componentTypeName).Append(".Set").Append(singleMember.animatorControllerParameterMethodSuffix).Append("(").Append(singleMember.animatorControllerParameterId).AppendLine(", value);");
                    }
                }
            }
            else if (memberTypeFullName == typeof(float).FullName || singleMember.animatorControllerParameterTypeFullName == typeof(float).FullName)
            {
                sb.Append(indent).AppendLine("\tfloat value;");
                if (singleMember.attribute.QuantizeDownToBitCount > 0)
                {
                    sb.Append(indent).Append("\tvalue = DeserializeSingleQuantized(bitStream_readFrom, ").Append(iOverall).AppendLine(").System_Single;");
                }
                else
                {
                    sb.Append(indent).AppendLine("                bitStream_readFrom.ReadFloat(out value);");
                }

                if (isDeserializeAll)
                {
                    if (singleMember.animatorControllerParameterId == 0)
                    {
                        sb.Append(indent).Append("\t").Append(single.componentTypeName).Append(".").Append(singleMember.memberName).AppendLine(" = value;");
                    }
                    else
                    {
                        sb.Append(indent).Append("\t").Append(single.componentTypeName).Append(".Set").Append(singleMember.animatorControllerParameterMethodSuffix).Append("(").Append(singleMember.animatorControllerParameterId).AppendLine(", value);");
                    }
                }
            }
            else if (memberTypeFullName == typeof(long).FullName)
            {
                sb.Append(indent).AppendLine("\tlong value;");
                sb.Append(indent).AppendLine("                bitStream_readFrom.ReadLong(out value);");
                if (isDeserializeAll)
                {
                    sb.Append(indent).Append("\t").Append(single.componentTypeName).Append(".").Append(singleMember.memberName).AppendLine(" = value;");
                }
            }
            else if (memberTypeFullName == typeof(uint).FullName)
            {
                sb.Append(indent).AppendLine("\tuint value;");
                sb.Append(indent).AppendLine("                bitStream_readFrom.ReadUInt(out value);");
                if (isDeserializeAll)
                {
                    sb.Append(indent).Append("\t").Append(single.componentTypeName).Append(".").Append(singleMember.memberName).AppendLine(" = value;");
                }
            }
            else if (memberTypeFullName == typeof(string).FullName)
            {
                sb.Append(indent).AppendLine("\tstring value;");
                sb.Append(indent).AppendLine("                bitStream_readFrom.ReadString(out value);");
                if (isDeserializeAll)
                {
                    sb.Append(indent).Append("\t").Append(single.componentTypeName).Append(".").Append(singleMember.memberName).AppendLine(" = value;");
                }
            }
            else if (memberTypeFullName == typeof(byte).FullName)
            {
                if (isDeserializeAll)
                {
                    sb.Append(indent).Append("\t").Append(single.componentTypeName).Append(".").Append(singleMember.memberName).AppendLine(" = (byte)bitStream_readFrom.ReadByte();");
                }
                else
                {
                    sb.Append(indent).AppendLine("\tvar value = (byte)bitStream_readFrom.ReadByte();");
                }
            }
            else if (memberTypeFullName == typeof(ushort).FullName)
            {
                sb.Append(indent).AppendLine("\tushort value;");
                sb.Append(indent).AppendLine("                bitStream_readFrom.ReadUShort(out value);");
                if (isDeserializeAll)
                {
                    sb.Append(indent).Append("\t").Append(single.componentTypeName).Append(".").Append(singleMember.memberName).AppendLine(" = value;");
                }
            }
            else if (memberTypeFullName == typeof(short).FullName)
            {
                sb.Append(indent).AppendLine("\tint count = 2;");
                sb.Append(indent).AppendLine("\tbyte[] bytes = GetMyValueDeserializeByteArray();");
                sb.Append(indent).AppendLine("\tfor (int i = 0; i < count; ++i)");
                sb.Append(indent).AppendLine("\t{");
                sb.Append(indent).AppendLine("\t\tbyte b = (byte)bitStream_readFrom.ReadByte();");
                sb.Append(indent).AppendLine("\t\tbytes[i] = b;");
                sb.Append(indent).AppendLine("\t}");
                if (isDeserializeAll)
                {
                    sb.Append(indent).Append("\t").Append(single.componentTypeName).Append(".").Append(singleMember.memberName).AppendLine(" = BitConverter.ToInt16(bytes, 0);");
                }
                else // for ReadOnlyNotApply variant
                {
                    sb.Append(indent).AppendLine("\tvar value = BitConverter.ToInt16(bytes, 0);");
                }
            }
            else if (memberTypeFullName == typeof(int).FullName || singleMember.animatorControllerParameterTypeFullName == typeof(int).FullName)
            {
                sb.Append(indent).AppendLine("\tint count = 4;");
                sb.Append(indent).AppendLine("\tbyte[] bytes = GetMyValueDeserializeByteArray();");
                sb.Append(indent).AppendLine("\tfor (int i = 0; i < count; ++i)");
                sb.Append(indent).AppendLine("\t{");
                sb.Append(indent).AppendLine("\t\tbyte b = (byte)bitStream_readFrom.ReadByte();");
                sb.Append(indent).AppendLine("\t\tbytes[i] = b;");
                sb.Append(indent).AppendLine("\t}");
                if (isDeserializeAll)
                {
                    if (singleMember.animatorControllerParameterId == 0)
                    {
                        sb.Append(indent).Append("\t").Append(single.componentTypeName).Append(".").Append(singleMember.memberName).AppendLine(" = BitConverter.ToInt32(bytes, 0);");
                    }
                    else
                    {
                        sb.Append(indent).Append("\t").Append(single.componentTypeName).Append(".Set").Append(singleMember.animatorControllerParameterMethodSuffix).Append("(").Append(singleMember.animatorControllerParameterId).AppendLine(", BitConverter.ToInt32(bytes, 0));");
                    }
                }
                else // for ReadOnlyNotApply variant
                {
                    sb.Append(indent).AppendLine("\tvar value = BitConverter.ToInt32(bytes, 0);");
                }
            }
            else if (memberTypeFullName == typeof(sbyte).FullName)
            {
                if (isDeserializeAll)
                {
                    sb.Append(indent).Append("\t").Append(single.componentTypeName).Append(".").Append(singleMember.memberName).AppendLine(" = (sbyte)bitStream_readFrom.ReadByte();");
                }
                else
                {
                    sb.Append(indent).AppendLine("\tvar value = (sbyte)bitStream_readFrom.ReadByte();");
                }
            }
            else if (memberTypeFullName == typeof(double).FullName)
            {
                sb.Append(indent).AppendLine("\tint count = 8;");
                sb.Append(indent).AppendLine("\tbyte[] bytes = GetMyValueDeserializeByteArray();");
                sb.Append(indent).AppendLine("\tfor (int i = 0; i < count; ++i)");
                sb.Append(indent).AppendLine("\t{");
                sb.Append(indent).AppendLine("\t\tbyte b = (byte)bitStream_readFrom.ReadByte();");
                sb.Append(indent).AppendLine("\t\tbytes[i] = b;");
                sb.Append(indent).AppendLine("\t}");
                if (isDeserializeAll)
                {
                    sb.Append(indent).Append("\t").Append(single.componentTypeName).Append(".").Append(singleMember.memberName).AppendLine(" = BitConverter.ToDouble(bytes, 0);");
                }
                else // for ReadOnlyNotApply variant
                {
                    sb.Append(indent).AppendLine("\tvar value = BitConverter.ToDouble(bytes, 0);");
                }
            }
            else if (memberTypeFullName == typeof(UnityEngine.Vector2).FullName || memberTypeFullName == typeof(UnityEngine.Vector3).FullName || memberTypeFullName == typeof(UnityEngine.Vector4).FullName || memberTypeFullName == typeof(UnityEngine.Quaternion).FullName)
            {
                sb.Append(indent).AppendLine("\tIGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[" + iOverall + "];");
                if (isDeserializeAll)
                {
                    sb.Append(indent).Append("\tvar value = customSerializer.Deserialize(bitStream_readFrom).").Append(memberTypeFullName.Replace(".", "_")).AppendLine(";");
                    if (singleMember.attribute.QuantizeDownToBitCount > 0)
                    {
                        sb.Append(indent).Append("\tvalue += valuesChangesSupport[").Append(iOverall).Append("].baselineValue_current.").Append(memberTypeFullName.Replace(".", "_")).AppendLine(";");
                    }
                    // For DeserializeAll, apply the value directly
                    if (singleMember.animatorControllerParameterId == 0)
                    {
                        sb.Append(indent).Append("\t").Append(single.componentTypeName).Append(".").Append(singleMember.memberName).AppendLine(" = value;");
                    }
                    else
                    {
                        sb.Append(indent).Append("\t").Append(single.componentTypeName).Append(".Set").Append(singleMember.animatorControllerParameterMethodSuffix).Append("(").Append(singleMember.animatorControllerParameterId).Append(", value);");
                    }
                }
                else if (readOnly) // ReadOnlyNotApply - extract specific type from GONetSyncableValue
                {
                    sb.Append(indent).Append("\tvar value = customSerializer.Deserialize(bitStream_readFrom).").Append(memberTypeFullName.Replace(".", "_")).AppendLine(";");
                    if (singleMember.attribute.QuantizeDownToBitCount > 0)
                    {
                        sb.Append(indent).Append("\tvalue += valuesChangesSupport[").Append(iOverall).Append("].baselineValue_current.").Append(memberTypeFullName.Replace(".", "_")).AppendLine(";");
                    }
                }
            }
        }

        private void WriteUpdateLastKnownValues()
        {
            sb.AppendLine("\t\tinternal override void UpdateLastKnownValues(GONetMain.SyncBundleUniqueGrouping onlyMatchIfUniqueGroupingMatches)");
            sb.AppendLine("\t\t{");

            int iOverall = 0;
            int singleCount = uniqueEntry.ComponentMemberNames_By_ComponentTypeFullName.Length;
            for (int iSingle = 0; iSingle < singleCount; ++iSingle)
            {
                GONetParticipant_ComponentsWithAutoSyncMembers_Single single = uniqueEntry.ComponentMemberNames_By_ComponentTypeFullName[iSingle];
                int singleMemberCount = single.autoSyncMembers.Length;
                for (int iSingleMember = 0; iSingleMember < singleMemberCount; ++iSingleMember)
                {
                    GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember singleMember = single.autoSyncMembers[iSingleMember];

                    // Check if this is a Rigidbody-aware member (position or rotation from Transform component)
                    bool isTransformComponent = single.componentTypeFullName == "UnityEngine.Transform";
                    bool isPositionMember = singleMember.memberName == "position";
                    bool isRotationMember = singleMember.memberName == "rotation";
                    bool isRigidbodyAwareMember = isTransformComponent && (isPositionMember || isRotationMember);

                    sb.Append("\t\t\t\tvar valuesChangesSupport").Append(iOverall).Append(" = valuesChangesSupport[").Append(iOverall).AppendLine("];");
                    sb.Append("\t\t\t\tif (DoesMatchUniqueGrouping(valuesChangesSupport").Append(iOverall).Append(", onlyMatchIfUniqueGroupingMatches) &&");
                    sb.AppendLine();
                    sb.Append("\t\t\t\t\t!ShouldSkipSync(valuesChangesSupport").Append(iOverall).Append(", ").Append(iOverall).AppendLine(")) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...");
                    sb.AppendLine("\t\t\t\t{");

                    // Standard value update - always update tracking values first
                    sb.Append("\t\t\t\t\tvaluesChangesSupport").Append(iOverall).Append(".lastKnownValue_previous = valuesChangesSupport").Append(iOverall).AppendLine(".lastKnownValue;");

                    if (isRigidbodyAwareMember && singleMember.animatorControllerParameterId == 0)
                    {
                        // PHYSICS SYNC: For Transform.position/rotation, source from Rigidbody when appropriate (IsRigidBodyOwnerOnlyControlled).
                        // This is filtered at call site (AutoMagicalSyncProcessing.Process()) - physics pipeline only calls this for physics objects.
                        // So we check here to determine SOURCE (Rigidbody vs Transform), not to skip processing (that's done at call site).
                        sb.AppendLine("\t\t\t\t\tbool shouldSourceFromRigidbody = gonetParticipant.IsRigidBodyOwnerOnlyControlled && gonetParticipant.myRigidBody != null;");
                        sb.AppendLine("\t\t\t\t\tif (shouldSourceFromRigidbody)");
                        sb.AppendLine("\t\t\t\t\t{");
                        sb.AppendLine("\t\t\t\t\t\t// Source from Rigidbody (physics simulation)");
                        sb.Append("\t\t\t\t\t\tvaluesChangesSupport").Append(iOverall).Append(".lastKnownValue.").Append(singleMember.memberTypeFullName.Replace(".", "_")).Append(" = gonetParticipant.myRigidBody.").Append(singleMember.memberName).AppendLine(";");
                        sb.AppendLine("\t\t\t\t\t}");
                        sb.AppendLine("\t\t\t\t\telse");
                        sb.AppendLine("\t\t\t\t\t{");
                        sb.AppendLine("\t\t\t\t\t\t// Source from Transform (regular sync)");
                        sb.Append("\t\t\t\t\t\tvaluesChangesSupport").Append(iOverall).Append(".lastKnownValue.").Append(singleMember.memberTypeFullName.Replace(".", "_")).Append(" = ").Append(single.componentTypeName).Append(".").Append(singleMember.memberName).AppendLine(";");
                        sb.AppendLine("\t\t\t\t\t}");
                    }
                    else if (singleMember.animatorControllerParameterId == 0)
                    {
                        sb.Append("\t\t\t\t\tvaluesChangesSupport").Append(iOverall).Append(".lastKnownValue.").Append(singleMember.memberTypeFullName.Replace(".", "_")).Append(" = ").Append(single.componentTypeName).Append(".").Append(singleMember.memberName).AppendLine(";");
                    }
                    else
                    {
                        sb.Append("\t\t\t\t\tvaluesChangesSupport").Append(iOverall).Append(".lastKnownValue.").Append(singleMember.animatorControllerParameterTypeFullName.Replace(".", "_")).Append(" = ").Append(single.componentTypeName).Append(".Get").Append(singleMember.animatorControllerParameterMethodSuffix).Append("(").Append(singleMember.animatorControllerParameterId).AppendLine(");");
                    }

                    sb.AppendLine("\t\t\t\t}");
                    sb.AppendLine();

                    iOverall++;
                }
            }

            sb.AppendLine("\t\t}");
            sb.AppendLine();
        }

        private void WriteIsLastKnownValue_VeryCloseTo_Or_AlreadyOutsideOf_QuantizationRange()
        {
            sb.AppendLine("\t\tinternal override bool IsLastKnownValue_VeryCloseTo_Or_AlreadyOutsideOf_QuantizationRange(byte singleIndex, GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport)");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\tswitch (singleIndex)");
            sb.AppendLine("\t\t\t{");

            int iOverall = 0;
            int singleCount = uniqueEntry.ComponentMemberNames_By_ComponentTypeFullName.Length;
            for (int iSingle = 0; iSingle < singleCount; ++iSingle)
            {
                GONetParticipant_ComponentsWithAutoSyncMembers_Single single = uniqueEntry.ComponentMemberNames_By_ComponentTypeFullName[iSingle];
                int singleMemberCount = single.autoSyncMembers.Length;
                for (int iSingleMember = 0; iSingleMember < singleMemberCount; ++iSingleMember)
                {
                    GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember singleMember = single.autoSyncMembers[iSingleMember];

                    sb.Append("\t\t\t\tcase ").Append(iOverall).AppendLine(":");
                    sb.Append("\t\t\t\t{ // ").Append(single.componentTypeName).Append(".").AppendLine(singleMember.memberName);

                    string memberTypeFullName = singleMember.memberTypeFullName;
                    if (memberTypeFullName == typeof(float).FullName || singleMember.animatorControllerParameterTypeFullName == typeof(float).FullName)
                    {
                        string typeField = singleMember.animatorControllerParameterId == 0 ? singleMember.memberTypeFullName.Replace(".", "_") : singleMember.animatorControllerParameterTypeFullName.Replace(".", "_");
                        sb.Append("                    System.Single diff = valueChangeSupport.lastKnownValue.").Append(typeField).Append(" - valueChangeSupport.baselineValue_current.").Append(typeField).AppendLine(";");
                        sb.AppendLine("\t\t\t\t\tSystem.Single componentLimitLower = valueChangeSupport.syncAttribute_QuantizerSettingsGroup.lowerBound * 0.8f; // TODO cache this value");
                        sb.AppendLine("\t\t\t\t\tSystem.Single componentLimitUpper = valueChangeSupport.syncAttribute_QuantizerSettingsGroup.upperBound * 0.8f; // TODO cache this value");
                        sb.AppendLine("                    bool isVeryCloseTo_Or_AlreadyOutsideOf_QuantizationRange = diff < componentLimitLower || diff > componentLimitUpper;");
                        sb.AppendLine("\t\t\t\t\treturn isVeryCloseTo_Or_AlreadyOutsideOf_QuantizationRange;");
                    }
                    else if (memberTypeFullName == typeof(UnityEngine.Vector3).FullName)
                    {
                        sb.Append("                    UnityEngine.Vector3 diff = valueChangeSupport.lastKnownValue.").Append(memberTypeFullName.Replace(".", "_")).Append(" - valueChangeSupport.baselineValue_current.").Append(memberTypeFullName.Replace(".", "_")).AppendLine(";");
                        sb.AppendLine("\t\t\t\t\tSystem.Single componentLimitLower = valueChangeSupport.syncAttribute_QuantizerSettingsGroup.lowerBound * 0.8f; // TODO cache this value");
                        sb.AppendLine("\t\t\t\t\tSystem.Single componentLimitUpper = valueChangeSupport.syncAttribute_QuantizerSettingsGroup.upperBound * 0.8f; // TODO cache this value");
                        sb.AppendLine("                    bool isVeryCloseTo_Or_AlreadyOutsideOf_QuantizationRange = ");
                        sb.AppendLine("\t\t\t\t\t\tdiff.x < componentLimitLower || diff.x > componentLimitUpper ||");
                        sb.AppendLine("\t\t\t\t\t\tdiff.y < componentLimitLower || diff.y > componentLimitUpper ||");
                        sb.AppendLine("\t\t\t\t\t\tdiff.z < componentLimitLower || diff.z > componentLimitUpper;");
                        sb.AppendLine("\t\t\t\t\treturn isVeryCloseTo_Or_AlreadyOutsideOf_QuantizationRange;");
                    }
                    else
                    {
                        sb.AppendLine("\t\t\t\t\t// this type not supported for this functionality");
                    }

                    sb.AppendLine("\t\t\t\t}");
                    sb.AppendLine("\t\t\t\tbreak;");
                    sb.AppendLine();

                    ++iOverall;
                }
            }

            sb.AppendLine("\t\t\t}");
            sb.AppendLine();
            sb.AppendLine("\t\t\treturn false;");
            sb.AppendLine("\t\t}");
            sb.AppendLine();
        }

        private void WriteCreateNewBaselineValueEvent()
        {
            sb.AppendLine("        internal override ValueMonitoringSupport_NewBaselineEvent CreateNewBaselineValueEvent(uint gonetId, byte singleIndex, GONetSyncableValue newBaselineValue)");
            sb.AppendLine("\t\t{");
            sb.AppendLine("\t\t\tswitch (singleIndex)");
            sb.AppendLine("\t\t\t{");

            int iOverall = 0;
            int singleCount = uniqueEntry.ComponentMemberNames_By_ComponentTypeFullName.Length;
            for (int iSingle = 0; iSingle < singleCount; ++iSingle)
            {
                GONetParticipant_ComponentsWithAutoSyncMembers_Single single = uniqueEntry.ComponentMemberNames_By_ComponentTypeFullName[iSingle];
                int singleMemberCount = single.autoSyncMembers.Length;
                for (int iSingleMember = 0; iSingleMember < singleMemberCount; ++iSingleMember)
                {
                    GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember singleMember = single.autoSyncMembers[iSingleMember];

                    sb.Append("\t\t\t\tcase ").Append(iOverall).AppendLine(":");
                    sb.Append("\t\t\t\t{ // ").Append(single.componentTypeName).Append(".").AppendLine(singleMember.memberName);

                    if (singleMember.animatorControllerParameterId == 0)
                    {
                        sb.Append("\t\t\t\t\treturn new ValueMonitoringSupport_NewBaselineEvent_").Append(singleMember.memberTypeFullName.Replace(".", "_")).AppendLine("() {");
                        sb.AppendLine("\t\t\t\t\t\tGONetId = gonetId,");
                        sb.AppendLine("\t\t\t\t\t\tValueIndex = singleIndex,");
                        sb.Append("\t\t\t\t\t\tNewBaselineValue = newBaselineValue.").Append(singleMember.memberTypeFullName.Replace(".", "_")).AppendLine();
                        sb.AppendLine("\t\t\t\t\t};");
                    }
                    else
                    {
                        sb.Append("\t\t\t\t\treturn new ValueMonitoringSupport_NewBaselineEvent_").Append(singleMember.animatorControllerParameterTypeFullName.Replace(".", "_")).AppendLine("() {");
                        sb.AppendLine("\t\t\t\t\t\tGONetId = gonetId,");
                        sb.AppendLine("\t\t\t\t\t\tValueIndex = singleIndex,");
                        sb.Append("\t\t\t\t\t\tNewBaselineValue = newBaselineValue.").Append(singleMember.animatorControllerParameterTypeFullName.Replace(".", "_")).AppendLine();
                        sb.AppendLine("\t\t\t\t\t};");
                    }

                    sb.AppendLine("\t\t\t\t}");
                    sb.AppendLine();

                    ++iOverall;
                }
            }

            sb.AppendLine("\t\t\t}");
            sb.AppendLine();
            sb.AppendLine("\t\t\treturn null;");
            sb.AppendLine("\t\t}");
        }

        private void WriteClassClose()
        {
            sb.AppendLine("    }");
            sb.AppendLine("}");
        }
    }
}
