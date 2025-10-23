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
            WriteDeserializeInitSingle(); // Synthesis wrapper
            WriteIsVelocityEligible();
            WriteSynthesizeValueFromVelocity();
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
            sb.AppendLine("\t\t\tcachedValueSerializers = cachedCustomSerializersArrayPool.Borrow((int)valuesCount);");
            sb.AppendLine("\t\t\tcachedVelocitySerializers = cachedCustomSerializersArrayPool.Borrow((int)valuesCount);");
            sb.AppendLine("\t\t\tcachedCustomValueBlendings = cachedCustomValueBlendingsArrayPool.Borrow((int)valuesCount);");
            sb.AppendLine("\t\t\tcachedCustomVelocityBlendings = cachedCustomVelocityBlendingsArrayPool.Borrow((int)valuesCount);");
            sb.AppendLine();
            sb.AppendLine("\t\t\t// Velocity-augmented sync: Allocate syncCounter for per-value velocity frequency tracking");
            sb.AppendLine("\t\t\tsyncCounter = syncCounterArrayPool.Borrow((int)valuesCount);");
            sb.AppendLine("\t\t\tArray.Clear(syncCounter, 0, syncCounter.Length); // Start at 0 for all values");
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

                    // VELOCITY-AUGMENTED SYNC: Initialize velocity tracking fields
                    sb.Append("\t\t\tsupport").Append(iOverall).Append(".isVelocityEligible = ").Append(singleMember.attribute.IsVelocityEligible ? "true" : "false").AppendLine(";");

                    // VELOCITY-AUGMENTED SYNC: Determine which velocity quantization bounds to use
                    // Way 1 (Dynamic): Defaults [-20, 20, 10] → Calculate from VALUE precision
                    // Way 2 (Manual): Custom values → Use directly from sync profile
                    string velocityLowerBoundForRuntimeFields;
                    string velocityUpperBoundForRuntimeFields;

                    bool hasManualVelocitySettingsForRuntimeFields = singleMember.attribute.VelocityQuantizeLowerBound != -20f ||
                                                     singleMember.attribute.VelocityQuantizeUpperBound != 20f ||
                                                     singleMember.attribute.VelocityQuantizeDownToBitCount != 10;

                    if (hasManualVelocitySettingsForRuntimeFields)
                    {
                        // Way 2: Use manual settings from sync profile
                        velocityLowerBoundForRuntimeFields = singleMember.attribute.VelocityQuantizeLowerBound.ToString(CultureInfo.InvariantCulture) + "f";
                        velocityUpperBoundForRuntimeFields = singleMember.attribute.VelocityQuantizeUpperBound.ToString(CultureInfo.InvariantCulture) + "f";
                    }
                    else
                    {
                        // Way 1: Calculate dynamic bounds from VALUE quantization precision
                        // This must match the calculation done below for the velocity serializer (lines 277-283)
                        var (velLowerBoundCalc, velUpperBoundCalc, velBitCountCalc) = CalculateVelocityQuantizationSettings(
                            singleMember.attribute.QuantizeLowerBound,
                            singleMember.attribute.QuantizeUpperBound,
                            singleMember.attribute.QuantizeDownToBitCount,
                            singleMember.attribute.SyncChangesEverySeconds,
                            singleMember.attribute.PhysicsUpdateInterval,
                            singleMember.memberTypeFullName == typeof(UnityEngine.Quaternion).FullName);

                        velocityLowerBoundForRuntimeFields = velLowerBoundCalc == float.MinValue ? "float.MinValue" : velLowerBoundCalc.ToString("F6", CultureInfo.InvariantCulture) + "f";
                        velocityUpperBoundForRuntimeFields = velUpperBoundCalc == float.MaxValue ? "float.MaxValue" : velUpperBoundCalc.ToString("F6", CultureInfo.InvariantCulture) + "f";
                    }

                    sb.Append("\t\t\tsupport").Append(iOverall).Append(".syncAttribute_VelocityQuantizeLowerBound = ").Append(velocityLowerBoundForRuntimeFields).AppendLine(";");
                    sb.Append("\t\t\tsupport").Append(iOverall).Append(".syncAttribute_VelocityQuantizeUpperBound = ").Append(velocityUpperBoundForRuntimeFields).AppendLine(";");

                    // OPTIMIZATION: Pre-calculate per-sync-interval bounds for efficient runtime checks
                    // Convert from value-units/second to value-units-per-sync-interval
                    // This eliminates division in hot path (range checking happens every VELOCITY frame)
                    string deltaTimeCalc;
                    if (singleMember.attribute.PhysicsUpdateInterval > 0)
                    {
                        deltaTimeCalc = $"UnityEngine.Time.fixedDeltaTime * {singleMember.attribute.PhysicsUpdateInterval}f";
                    }
                    else
                    {
                        deltaTimeCalc = $"{singleMember.attribute.SyncChangesEverySeconds.ToString("F6", CultureInfo.InvariantCulture)}f";
                    }
                    sb.Append("\t\t\tsupport").Append(iOverall).Append(".velocityQuantizeLowerBound_PerSyncInterval = support").Append(iOverall).Append(".syncAttribute_VelocityQuantizeLowerBound * ").Append(deltaTimeCalc).AppendLine(";");
                    sb.Append("\t\t\tsupport").Append(iOverall).Append(".velocityQuantizeUpperBound_PerSyncInterval = support").Append(iOverall).Append(".syncAttribute_VelocityQuantizeUpperBound * ").Append(deltaTimeCalc).AppendLine(";");

                    // Initialize lastVelocityTimestamp to current time to prevent false "EXPIRED" on first VALUE bundle
                    // (uninitialized = 0 → age = currentTime - 0 = HUGE → immediate expiration)
                    sb.Append("\t\t\tsupport").Append(iOverall).AppendLine(".lastVelocityTimestamp = GONet.GONetMain.Time.ElapsedTicks;");
                    // Store member type as enum for velocity calculations
                    string memberTypeEnum = singleMember.memberTypeFullName.Replace(".", "_");
                    sb.Append("\t\t\tsupport").Append(iOverall).Append(".codeGenerationMemberType = GONetSyncableValueTypes.").Append(memberTypeEnum).AppendLine(";");

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
                        // Initialize legacy cachedCustomSerializers for backward compatibility
                        sb.Append("\t\t\tcachedCustomSerializers[").Append(iOverall).Append("] = GONetAutoMagicalSyncAttribute.GetCustomSerializer<").Append(singleMember.attribute.CustomSerialize_Instance.GetType().FullName.Replace("+", ".")).Append(">(").Append(singleMember.attribute.QuantizeDownToBitCount).Append(", ").Append(singleMember.attribute.QuantizeLowerBound.ToString(CultureInfo.InvariantCulture)).Append("f, ").Append(singleMember.attribute.QuantizeUpperBound.ToString(CultureInfo.InvariantCulture)).AppendLine("f);");

                        // Velocity-augmented sync: Initialize VALUE serializer (same quantization as legacy for now)
                        sb.Append("\t\t\tcachedValueSerializers[").Append(iOverall).Append("] = GONetAutoMagicalSyncAttribute.GetCustomSerializer<").Append(singleMember.attribute.CustomSerialize_Instance.GetType().FullName.Replace("+", ".")).Append(">(").Append(singleMember.attribute.QuantizeDownToBitCount).Append(", ").Append(singleMember.attribute.QuantizeLowerBound.ToString(CultureInfo.InvariantCulture)).Append("f, ").Append(singleMember.attribute.QuantizeUpperBound.ToString(CultureInfo.InvariantCulture)).AppendLine("f);");

                        // Velocity-augmented sync: Initialize VELOCITY serializer
                        // Use manual velocity settings if configured, otherwise calculate dynamically
                        string velBitCount;
                        string velLowerBound;
                        string velUpperBound;

                        // Check if user has manually configured velocity quantization (non-default values)
                        bool hasManualVelocitySettings = singleMember.attribute.VelocityQuantizeLowerBound != -20f ||
                                                         singleMember.attribute.VelocityQuantizeUpperBound != 20f ||
                                                         singleMember.attribute.VelocityQuantizeDownToBitCount != 10;

                        if (hasManualVelocitySettings)
                        {
                            // Use manually configured velocity quantization settings
                            velBitCount = singleMember.attribute.VelocityQuantizeDownToBitCount.ToString();
                            velLowerBound = singleMember.attribute.VelocityQuantizeLowerBound == float.MinValue ? "float.MinValue" : singleMember.attribute.VelocityQuantizeLowerBound.ToString(CultureInfo.InvariantCulture) + "f";
                            velUpperBound = singleMember.attribute.VelocityQuantizeUpperBound == float.MaxValue ? "float.MaxValue" : singleMember.attribute.VelocityQuantizeUpperBound.ToString(CultureInfo.InvariantCulture) + "f";
                        }
                        else
                        {
                            // Calculate velocity range dynamically from VALUE quantization precision (for sub-quantization motion)
                            var (velLowerBoundCalc, velUpperBoundCalc, velBitCountCalc) = CalculateVelocityQuantizationSettings(
                                singleMember.attribute.QuantizeLowerBound,
                                singleMember.attribute.QuantizeUpperBound,
                                singleMember.attribute.QuantizeDownToBitCount,
                                singleMember.attribute.SyncChangesEverySeconds,
                                singleMember.attribute.PhysicsUpdateInterval,
                                singleMember.memberTypeFullName == typeof(UnityEngine.Quaternion).FullName);

                            velBitCount = velBitCountCalc.ToString();
                            velLowerBound = velLowerBoundCalc == float.MinValue ? "float.MinValue" : velLowerBoundCalc.ToString("F6", CultureInfo.InvariantCulture) + "f";
                            velUpperBound = velUpperBoundCalc == float.MaxValue ? "float.MaxValue" : velUpperBoundCalc.ToString("F6", CultureInfo.InvariantCulture) + "f";
                        }

                        // CRITICAL: Quaternion angular velocity uses Vector3Serializer (not QuaternionSerializer!)
                        string velocitySerializerType = singleMember.memberTypeFullName == typeof(UnityEngine.Quaternion).FullName
                            ? "GONet.Vector3Serializer"
                            : singleMember.attribute.CustomSerialize_Instance.GetType().FullName.Replace("+", ".");

                        sb.Append("\t\t\tcachedVelocitySerializers[").Append(iOverall).Append("] = GONetAutoMagicalSyncAttribute.GetCustomSerializer<").Append(velocitySerializerType).Append(">(").Append(velBitCount).Append(", ").Append(velLowerBound).Append(", ").Append(velUpperBound).AppendLine(");");
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
            sb.AppendLine("            // SerializeAll is INIT ONLY - always send VALUES (no velocity data during initial sync)");
            sb.AppendLine("#if GONET_VELOCITY_SYNC_DEBUG");
            sb.AppendLine("            GONetLog.Debug($\"[VelocitySync][{gonetParticipant.GONetId}] SerializeAll: INIT sync (always VALUE packets)\");");
            sb.AppendLine("#endif");
            sb.AppendLine();

            WriteSerializationBody(true);

            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private void WriteSerializeSingle()
        {
            sb.AppendLine("        internal override void SerializeSingle(Utils.BitByBitByteArrayBuilder bitStream_appendTo, byte singleIndex, bool isVelocityBundle = false)");
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
                    WriteSingleSerialization(iOverall, single, singleMember, isSerializeAll ? "\t\t\t" : "\t\t\t\t", isSerializeAll);
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
        /// Generates velocity calculation code for a given type.
        /// Returns the velocity calculation as a GONetSyncableValue.
        /// </summary>
        private void WriteVelocityCalculation(int iOverall, string memberTypeFullName, GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember singleMember, string indent)
        {
            sb.Append(indent).AppendLine("// VELOCITY BUNDLE: Send raw delta (no division - optimization!)");
            sb.Append(indent).AppendLine($"var changesSupport = valuesChangesSupport[{iOverall}];");
            sb.Append(indent).AppendLine("GONetSyncableValue velocityValue;");
            sb.Append(indent).AppendLine();
            sb.Append(indent).AppendLine("// CRITICAL: Use lastKnownValue (authority's transform) NOT mostRecentChanges (client blending queue)");
            sb.Append(indent).AppendLine("var current = changesSupport.lastKnownValue;");
            sb.Append(indent).AppendLine("var previous = changesSupport.lastKnownValue_previous;");
            sb.Append(indent).AppendLine();
            sb.Append(indent).AppendLine("// Check if we have valid previous value");
            sb.Append(indent).AppendLine("if (current.GONetSyncType == previous.GONetSyncType && current.GONetSyncType != GONet.GONetSyncableValueTypes.System_Boolean)");
            sb.Append(indent).AppendLine("{");

            // Type-specific raw delta calculation (NO DIVISION - OPTIMIZATION!)
            if (memberTypeFullName == typeof(float).FullName)
            {
                sb.Append(indent).AppendLine("\t// OPTIMIZATION: Send raw delta (meters-per-sync-interval), not velocity (m/s)");
                sb.Append(indent).AppendLine("\tfloat currentValue = current.System_Single;");
                sb.Append(indent).AppendLine("\tfloat previousValue = previous.System_Single;");
                sb.Append(indent).AppendLine("\tvelocityValue = new GONetSyncableValue();");
                sb.Append(indent).AppendLine("\tvelocityValue.System_Single = currentValue - previousValue;");
            }
            else if (memberTypeFullName == typeof(UnityEngine.Vector2).FullName)
            {
                sb.Append(indent).AppendLine("\t// OPTIMIZATION: Send raw delta (meters-per-sync-interval), not velocity (m/s)");
                sb.Append(indent).AppendLine("\tUnityEngine.Vector2 currentValue = current.UnityEngine_Vector2;");
                sb.Append(indent).AppendLine("\tUnityEngine.Vector2 previousValue = previous.UnityEngine_Vector2;");
                sb.Append(indent).AppendLine("\tvelocityValue = new GONetSyncableValue();");
                sb.Append(indent).AppendLine("\tvelocityValue.UnityEngine_Vector2 = currentValue - previousValue;");
            }
            else if (memberTypeFullName == typeof(UnityEngine.Vector3).FullName)
            {
                sb.Append(indent).AppendLine("\t// OPTIMIZATION: Send raw delta (meters-per-sync-interval), not velocity (m/s)");
                sb.Append(indent).AppendLine("\tUnityEngine.Vector3 currentValue = current.UnityEngine_Vector3;");
                sb.Append(indent).AppendLine("\tUnityEngine.Vector3 previousValue = previous.UnityEngine_Vector3;");
                sb.Append(indent).AppendLine();
                sb.Append(indent).AppendLine($"\t// DIAGNOSTIC: Log snapshot values");
                sb.Append(indent).AppendLine($"\tGONet.GONetLog.Debug($\"[VelocityCalc][{{gonetParticipant.GONetId}}][idx:{iOverall}] current={{currentValue}}, previous={{previousValue}}\");");
                sb.Append(indent).AppendLine();
                sb.Append(indent).AppendLine("\tvelocityValue = new GONetSyncableValue();");
                sb.Append(indent).AppendLine("\tvelocityValue.UnityEngine_Vector3 = currentValue - previousValue;");
                sb.Append(indent).AppendLine();
                sb.Append(indent).AppendLine($"\tGONet.GONetLog.Debug($\"[VelocityCalc][{{gonetParticipant.GONetId}}][idx:{iOverall}] calculated raw delta={{velocityValue.UnityEngine_Vector3}}\");");
            }
            else if (memberTypeFullName == typeof(UnityEngine.Vector4).FullName)
            {
                sb.Append(indent).AppendLine("\t// OPTIMIZATION: Send raw delta (value-units-per-sync-interval), not velocity");
                sb.Append(indent).AppendLine("\tUnityEngine.Vector4 currentValue = current.UnityEngine_Vector4;");
                sb.Append(indent).AppendLine("\tUnityEngine.Vector4 previousValue = previous.UnityEngine_Vector4;");
                sb.Append(indent).AppendLine("\tvelocityValue = new GONetSyncableValue();");
                sb.Append(indent).AppendLine("\tvelocityValue.UnityEngine_Vector4 = currentValue - previousValue;");
            }
            else if (memberTypeFullName == typeof(UnityEngine.Quaternion).FullName)
            {
                sb.Append(indent).AppendLine("\t// Angular velocity for Quaternion (stored as Vector3 axis-angle)");
                sb.Append(indent).AppendLine("\tUnityEngine.Quaternion currentValue = current.UnityEngine_Quaternion;");
                sb.Append(indent).AppendLine("\tUnityEngine.Quaternion previousValue = previous.UnityEngine_Quaternion;");
                sb.Append(indent).AppendLine();
                sb.Append(indent).AppendLine($"\t// DIAGNOSTIC: Log rotation values");
                sb.Append(indent).AppendLine($"\tGONet.GONetLog.Debug($\"[AngularVelCalc][{{gonetParticipant.GONetId}}][idx:{iOverall}] current={{currentValue.eulerAngles}}, previous={{previousValue.eulerAngles}}\");");
                sb.Append(indent).AppendLine();
                sb.Append(indent).AppendLine("\tUnityEngine.Quaternion deltaRotation = currentValue * UnityEngine.Quaternion.Inverse(previousValue);");
                sb.Append(indent).AppendLine("\tdeltaRotation.ToAngleAxis(out float angle, out UnityEngine.Vector3 axis);");
                sb.Append(indent).AppendLine("\t// NOTE: Quaternion still uses radians/sec for now (requires more complex optimization)");
                sb.Append(indent).AppendLine("\t// Get deltaTime for quaternion angular velocity calculation");
                if (singleMember.attribute.PhysicsUpdateInterval > 0)
                {
                    int physicsInterval = singleMember.attribute.PhysicsUpdateInterval;
                    sb.Append(indent).AppendLine($"\tfloat deltaTime = UnityEngine.Time.fixedDeltaTime * {physicsInterval}f;");
                }
                else
                {
                    float deltaTime = singleMember.attribute.SyncChangesEverySeconds;
                    sb.Append(indent).AppendLine($"\tfloat deltaTime = {deltaTime.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}f;");
                }
                sb.Append(indent).AppendLine("\tUnityEngine.Vector3 angularVelocity = axis * (angle * UnityEngine.Mathf.Deg2Rad) / deltaTime;");
                sb.Append(indent).AppendLine("\tvelocityValue = new GONetSyncableValue();");
                sb.Append(indent).AppendLine("\tvelocityValue.UnityEngine_Vector3 = angularVelocity;");
                sb.Append(indent).AppendLine();
                sb.Append(indent).AppendLine($"\tGONet.GONetLog.Debug($\"[AngularVelCalc][{{gonetParticipant.GONetId}}][idx:{iOverall}] calculated angularVelocity={{angularVelocity}} rad/s, degrees/s={{angularVelocity * UnityEngine.Mathf.Rad2Deg}}\");");
            }

            sb.Append(indent).AppendLine("}");
            sb.Append(indent).AppendLine("else");
            sb.Append(indent).AppendLine("{");
            sb.Append(indent).AppendLine("\t// Type mismatch, use zero delta");
            sb.Append(indent).AppendLine($"\tGONet.GONetLog.Debug($\"[VelocityCalc][{{gonetParticipant.GONetId}}][idx:{iOverall}] Type mismatch or not initialized\");");
            sb.Append(indent).AppendLine("\tvelocityValue = new GONetSyncableValue();");

            // CRITICAL FIX: Initialize velocity type properly to avoid System_Boolean default
            if (memberTypeFullName == typeof(float).FullName)
            {
                sb.Append(indent).AppendLine("\tvelocityValue.System_Single = 0f;");
            }
            else if (memberTypeFullName == typeof(UnityEngine.Vector2).FullName)
            {
                sb.Append(indent).AppendLine("\tvelocityValue.UnityEngine_Vector2 = UnityEngine.Vector2.zero;");
            }
            else if (memberTypeFullName == typeof(UnityEngine.Vector3).FullName)
            {
                sb.Append(indent).AppendLine("\tvelocityValue.UnityEngine_Vector3 = UnityEngine.Vector3.zero;");
            }
            else if (memberTypeFullName == typeof(UnityEngine.Vector4).FullName)
            {
                sb.Append(indent).AppendLine("\tvelocityValue.UnityEngine_Vector4 = UnityEngine.Vector4.zero;");
            }
            else if (memberTypeFullName == typeof(UnityEngine.Quaternion).FullName)
            {
                // Quaternion velocity is stored as Vector3 angular velocity
                sb.Append(indent).AppendLine("\tvelocityValue.UnityEngine_Vector3 = UnityEngine.Vector3.zero;");
            }

            sb.Append(indent).AppendLine("}");
            sb.Append(indent).AppendLine();
        }

        private void WriteSingleSerialization(int iOverall, GONetParticipant_ComponentsWithAutoSyncMembers_Single single, GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember singleMember, string indent, bool isSerializeAll)
        {
            string memberTypeFullName = singleMember.memberTypeFullName;
            string valueExpression = singleMember.animatorControllerParameterId == 0
                ? $"{single.componentTypeName}.{singleMember.memberName}"
                : $"{single.componentTypeName}.Get{singleMember.animatorControllerParameterMethodSuffix}({singleMember.animatorControllerParameterId})";

            // Check if velocity-augmented sync applies to this member
            bool usesVelocitySync = IsVelocityCapableType(memberTypeFullName) && singleMember.attribute.ShouldBlendBetweenValuesReceived;

            // Check CustomSerialize, but EXCLUDE Vector types (they're handled separately below)
            if (singleMember.attribute.CustomSerialize_Instance != null &&
                memberTypeFullName != typeof(UnityEngine.Vector2).FullName &&
                memberTypeFullName != typeof(UnityEngine.Vector3).FullName &&
                memberTypeFullName != typeof(UnityEngine.Vector4).FullName &&
                memberTypeFullName != typeof(UnityEngine.Quaternion).FullName)
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
                if (usesVelocitySync && !isSerializeAll)
                {
                    // VELOCITY-CAPABLE: Branch on bundle type
                    sb.Append(indent).AppendLine("\tif (isVelocityBundle)");
                    sb.Append(indent).AppendLine("\t{");

                    WriteVelocityCalculation(iOverall, memberTypeFullName, singleMember, indent + "\t\t");

                    sb.Append(indent).AppendLine($"\t\t// Serialize velocity using velocity serializer");
                    sb.Append(indent).AppendLine($"\t\tcachedVelocitySerializers[{iOverall}].Serialize(bitStream_appendTo, gonetParticipant, velocityValue);");
                    sb.Append(indent).AppendLine("\t}");
                    sb.Append(indent).AppendLine("\telse");
                    sb.Append(indent).AppendLine("\t{");
                    sb.Append(indent).AppendLine("\t\t// VALUE BUNDLE: Serialize position normally");

                    if (singleMember.attribute.QuantizeDownToBitCount > 0)
                    {
                        sb.Append(indent).Append("\t\tSerializeSingleQuantized(bitStream_appendTo, ").Append(iOverall).Append(", ").Append(valueExpression).AppendLine(");");
                    }
                    else
                    {
                        sb.Append(indent).Append("\t\tbitStream_appendTo.WriteFloat(").Append(valueExpression).AppendLine(");");
                    }

                    sb.Append(indent).AppendLine("\t}");
                }
                else
                {
                    // NO VELOCITY: Serialize normally
                    if (singleMember.attribute.QuantizeDownToBitCount > 0)
                    {
                        sb.Append(indent).Append("\tSerializeSingleQuantized(bitStream_appendTo, ").Append(iOverall).Append(", ").Append(valueExpression).AppendLine(");");
                    }
                    else
                    {
                        sb.Append(indent).Append("\tbitStream_appendTo.WriteFloat(").Append(valueExpression).AppendLine(");");
                    }
                }
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
            if (memberTypeFullName == typeof(UnityEngine.Vector2).FullName || memberTypeFullName == typeof(UnityEngine.Vector3).FullName || memberTypeFullName == typeof(UnityEngine.Vector4).FullName || memberTypeFullName == typeof(UnityEngine.Quaternion).FullName)
            {
                sb.Append(indent).AppendLine("\t    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[" + iOverall + "];");

                if (usesVelocitySync && !isSerializeAll)
                {
                    // VELOCITY-CAPABLE: Branch on bundle type
                    sb.Append(indent).AppendLine("\tif (isVelocityBundle)");
                    sb.Append(indent).AppendLine("\t{");

                    WriteVelocityCalculation(iOverall, memberTypeFullName, singleMember, indent + "\t\t");

                    sb.Append(indent).AppendLine($"\t\t// Serialize velocity using velocity serializer");
                    sb.Append(indent).AppendLine($"\t\tcachedVelocitySerializers[{iOverall}].Serialize(bitStream_appendTo, gonetParticipant, velocityValue);");
                    sb.Append(indent).AppendLine("\t}");
                    sb.Append(indent).AppendLine("\telse");
                    sb.Append(indent).AppendLine("\t{");
                    sb.Append(indent).AppendLine("\t\t// VALUE BUNDLE: Serialize position normally");

                    if (singleMember.attribute.QuantizeDownToBitCount > 0)
                    {
                        // SUB-QUANTIZATION DIAGNOSTIC: Log delta-from-baseline that's actually being serialized
                        string deltaFieldName = memberTypeFullName.Replace(".", "_");
                        sb.Append(indent).AppendLine($"\t\t{{ // SUB-QUANTIZATION DIAGNOSTIC for {singleMember.memberName}");
                        sb.Append(indent).AppendLine($"\t\t\tvar currentValue = {valueExpression};");
                        sb.Append(indent).AppendLine($"\t\t\tvar baselineValue = valuesChangesSupport[{iOverall}].baselineValue_current.{deltaFieldName};");
                        sb.Append(indent).AppendLine($"\t\t\tvar deltaFromBaseline = currentValue - baselineValue;");
                        sb.Append(indent).AppendLine($"\t\t\tGONet.Utils.SubQuantizationDiagnostics.CheckAndLogIfSubQuantization(gonetParticipant.GONetId, \"{singleMember.memberName}\", deltaFromBaseline, valuesChangesSupport[{iOverall}].syncAttribute_QuantizerSettingsGroup, customSerializer);");
                        sb.Append(indent).Append("\t\t\tcustomSerializer.Serialize(bitStream_appendTo, gonetParticipant, deltaFromBaseline").AppendLine(");");
                        sb.Append(indent).AppendLine("\t\t}");
                    }
                    else
                    {
                        // NOTE: No sub-quantization diagnostic here because this path is for values WITHOUT baseline subtraction
                        // (like Quaternion rotation which uses Smallest3 encoding with quantization handled inside the serializer)
                        sb.Append(indent).Append("\t\tcustomSerializer.Serialize(bitStream_appendTo, gonetParticipant, ").Append(valueExpression).AppendLine(");");
                    }

                    // CRITICAL: Store this VALUE as a snapshot for future velocity calculations
                    sb.Append(indent).AppendLine();
                    sb.Append(indent).AppendLine($"\t\t// Store snapshot for velocity calculation on next VELOCITY bundle");
                    sb.Append(indent).AppendLine($"\t\tvar snapshotValue = new GONetSyncableValue();");
                    string snapshotFieldName = memberTypeFullName.Replace(".", "_");
                    sb.Append(indent).AppendLine($"\t\tsnapshotValue.{snapshotFieldName} = {valueExpression};");
                    sb.Append(indent).AppendLine($"\t\tvaluesChangesSupport[{iOverall}].AddToMostRecentChangeQueue_IfAppropriate(GONet.GONetMain.Time.ElapsedTicks, snapshotValue);");

                    sb.Append(indent).AppendLine("\t}");
                }
                else
                {
                    // NO VELOCITY: Serialize normally
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
                        // NOTE: No sub-quantization diagnostic here because this path is for values WITHOUT baseline subtraction
                        // (like Quaternion rotation which uses Smallest3 encoding with quantization handled inside the serializer)
                        sb.Append(indent).Append("\tcustomSerializer.Serialize(bitStream_appendTo, gonetParticipant, ").Append(valueExpression).AppendLine(");");
                    }
                }
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
            // DeserializeInitAll: INIT sync - always receives VALUE packets (no velocity data)
            sb.AppendLine("#if GONET_VELOCITY_SYNC_DEBUG");
            sb.AppendLine("            GONetLog.Debug($\"[VelocitySync][{gonetParticipant.GONetId}] DeserializeInitAll: INIT sync (always VALUE packets)\");");
            sb.AppendLine("#endif");
            sb.AppendLine();

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

                    // DeserializeInitAll: Always deserialize VALUES (no velocity during INIT sync)
                    WriteDeserializeSingle(iOverall, single, singleMember, "\t\t\t", true);

                    sb.AppendLine("\t\t\t}");

                    ++iOverall;
                }
            }
        }

        /// <summary>
        /// Generates velocity-aware deserialization logic for a single value.
        /// Branches on isVelocityBundle:
        /// - VELOCITY: Deserialize velocity, synthesize position from velocity
        /// - VALUE: Deserialize position normally
        /// </summary>
        private void WriteVelocityDeserializationLogic(int iOverall, GONetParticipant_ComponentsWithAutoSyncMembers_Single single, GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember singleMember, string indent)
        {
            string memberTypeFullName = singleMember.memberTypeFullName;
            string memberTypeReplaced = memberTypeFullName.Replace(".", "_");

            sb.Append(indent).AppendLine("\tif (isVelocityBundle)");
            sb.Append(indent).AppendLine("\t{");
            sb.Append(indent).AppendLine("\t\t// VELOCITY packet: Deserialize velocity and synthesize position");
            sb.Append(indent).AppendLine("#if GONET_VELOCITY_SYNC_DEBUG");
            sb.Append(indent).AppendLine($"\t\tGONetLog.Debug($\"[VelocitySync][{{gonetParticipant.GONetId}}] DeserializeInitAll[{iOverall}]: Deserializing VELOCITY data\");");
            sb.Append(indent).AppendLine("#endif");

            // Deserialize velocity
            if (memberTypeFullName == typeof(float).FullName)
            {
                sb.Append(indent).AppendLine("\t\tfloat velocity;");
                sb.Append(indent).AppendLine("\t\tbitStream_readFrom.ReadFloat(out velocity);");
            }
            else if (memberTypeFullName == typeof(UnityEngine.Vector2).FullName)
            {
                // Vector2 uses custom serializer
                sb.Append(indent).AppendLine($"\t\tIGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[{iOverall}];");
                sb.Append(indent).AppendLine("\t\tUnityEngine.Vector2 velocity = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Vector2;");
            }
            else if (memberTypeFullName == typeof(UnityEngine.Vector3).FullName)
            {
                // Vector3 uses custom serializer
                sb.Append(indent).AppendLine($"\t\tIGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[{iOverall}];");
                sb.Append(indent).AppendLine("\t\tUnityEngine.Vector3 velocity = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Vector3;");
            }
            else if (memberTypeFullName == typeof(UnityEngine.Quaternion).FullName)
            {
                // Angular velocity stored as Vector3 (uses cached Vector3Serializer with proper quantization)
                // For Quaternion rotation, angular velocity is Vector3, so we use cachedVelocitySerializers
                // which was initialized with Vector3Serializer (not QuaternionSerializer!) in the constructor
                sb.Append(indent).AppendLine($"\t\t// Angular velocity uses cachedVelocitySerializers[{iOverall}] (Vector3Serializer with dynamic quantization)");
                sb.Append(indent).AppendLine($"\t\tIGONetAutoMagicalSync_CustomSerializer angularVelocitySerializer = cachedVelocitySerializers[{iOverall}];");
                sb.Append(indent).AppendLine("\t\tUnityEngine.Vector3 angularVelocity = angularVelocitySerializer.Deserialize(bitStream_readFrom).UnityEngine_Vector3;");
                sb.Append(indent).AppendLine();
                sb.Append(indent).AppendLine($"\t\t// DIAGNOSTIC: Log received angular velocity");
                sb.Append(indent).AppendLine($"\t\tGONet.GONetLog.Debug($\"[CLIENT-AngularVel][{{gonetParticipant.GONetId}}][idx:{iOverall}] receivedAngularVelocity={{angularVelocity}} rad/s, degrees/s={{angularVelocity * UnityEngine.Mathf.Rad2Deg}}\");");
            }

            sb.AppendLine();

            // Get previous value and synthesize new position from velocity
            sb.Append(indent).AppendLine($"\t\t// Get previous value to synthesize from");
            sb.Append(indent).AppendLine($"\t\tGONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport = valuesChangesSupport[{iOverall}];");
            sb.Append(indent).AppendLine($"\t\tint mostRecentChangesIndex = valueChangeSupport.mostRecentChanges_usedSize - 1;");
            sb.AppendLine();

            if (memberTypeFullName == typeof(UnityEngine.Quaternion).FullName)
            {
                // Quaternion: Synthesize using angular velocity integration
                sb.Append(indent).AppendLine("\t\tUnityEngine.Quaternion synthesizedValue;");
                sb.Append(indent).AppendLine("\t\tif (mostRecentChangesIndex >= 0 && valueChangeSupport.mostRecentChanges_usedSize > 0)");
                sb.Append(indent).AppendLine("\t\t{");
                sb.Append(indent).AppendLine("\t\t\tPluginAPI.NumericValueChangeSnapshot previousSnapshot = valueChangeSupport.mostRecentChanges[mostRecentChangesIndex];");
                sb.Append(indent).AppendLine("\t\t\tlong deltaTimeTicks = assumedElapsedTicksAtChange - previousSnapshot.elapsedTicksAtChange;");
                sb.Append(indent).AppendLine("\t\t\tfloat deltaTimeSeconds = (float)(deltaTimeTicks / (double)System.Diagnostics.Stopwatch.Frequency);");
                sb.AppendLine();
                sb.Append(indent).AppendLine("\t\t\t// Integrate angular velocity: q_new = q_old * exp(omega * dt / 2)");
                sb.Append(indent).AppendLine("\t\t\tfloat angle = angularVelocity.magnitude * deltaTimeSeconds;");
                sb.Append(indent).AppendLine("\t\t\tif (angle > 1e-6f)");
                sb.Append(indent).AppendLine("\t\t\t{");
                sb.Append(indent).AppendLine("\t\t\t\tUnityEngine.Vector3 axis = angularVelocity.normalized;");
                sb.Append(indent).AppendLine("\t\t\t\tUnityEngine.Quaternion deltaRotation = UnityEngine.Quaternion.AngleAxis(angle * UnityEngine.Mathf.Rad2Deg, axis);");
                sb.Append(indent).AppendLine("\t\t\t\tsynthesizedValue = previousSnapshot.numericValue.UnityEngine_Quaternion * deltaRotation;");
                sb.Append(indent).AppendLine();
                sb.Append(indent).AppendLine($"\t\t\t\t// DIAGNOSTIC: Log synthesis");
                sb.Append(indent).AppendLine($"\t\t\t\tGONet.GONetLog.Debug($\"[CLIENT-AngularVel][{{gonetParticipant.GONetId}}][idx:{iOverall}] previousRot={{previousSnapshot.numericValue.UnityEngine_Quaternion.eulerAngles}}, synthesized={{synthesizedValue.eulerAngles}}, deltaTime={{deltaTimeSeconds:F4}}s\");");
                sb.Append(indent).AppendLine("\t\t\t}");
                sb.Append(indent).AppendLine("\t\t\telse");
                sb.Append(indent).AppendLine("\t\t\t{");
                sb.Append(indent).AppendLine("\t\t\t\tsynthesizedValue = previousSnapshot.numericValue.UnityEngine_Quaternion;");
                sb.Append(indent).AppendLine("\t\t\t}");
                sb.Append(indent).AppendLine("\t\t}");
                sb.Append(indent).AppendLine("\t\telse");
                sb.Append(indent).AppendLine("\t\t{");
                sb.Append(indent).AppendLine("\t\t\t// No previous value, use current as-is");
                sb.Append(indent).AppendLine($"\t\t\tsynthesizedValue = {single.componentTypeName}.{singleMember.memberName};");
                sb.Append(indent).AppendLine("\t\t}");
            }
            else
            {
                // Scalar/Vector types: synthesizedValue = previousValue + velocity × deltaTime
                sb.Append(indent).AppendLine($"\t\t{memberTypeFullName} synthesizedValue;");
                sb.Append(indent).AppendLine("\t\tif (mostRecentChangesIndex >= 0 && valueChangeSupport.mostRecentChanges_usedSize > 0)");
                sb.Append(indent).AppendLine("\t\t{");
                sb.Append(indent).AppendLine("\t\t\tPluginAPI.NumericValueChangeSnapshot previousSnapshot = valueChangeSupport.mostRecentChanges[mostRecentChangesIndex];");
                sb.Append(indent).AppendLine("\t\t\tlong deltaTimeTicks = assumedElapsedTicksAtChange - previousSnapshot.elapsedTicksAtChange;");
                sb.Append(indent).AppendLine("\t\t\tfloat deltaTimeSeconds = (float)(deltaTimeTicks / (double)System.Diagnostics.Stopwatch.Frequency);");
                sb.AppendLine();
                sb.Append(indent).AppendLine($"\t\t\t// Synthesize: position = previousPosition + velocity × deltaTime");
                sb.Append(indent).AppendLine($"\t\t\tsynthesizedValue = previousSnapshot.numericValue.{memberTypeReplaced} + velocity * deltaTimeSeconds;");
                sb.Append(indent).AppendLine("\t\t}");
                sb.Append(indent).AppendLine("\t\telse");
                sb.Append(indent).AppendLine("\t\t{");
                sb.Append(indent).AppendLine("\t\t\t// No previous value, use current as-is (first packet)");
                sb.Append(indent).AppendLine($"\t\t\tsynthesizedValue = {single.componentTypeName}.{singleMember.memberName};");
                sb.Append(indent).AppendLine("\t\t}");
            }

            sb.AppendLine();
            sb.Append(indent).AppendLine("#if GONET_VELOCITY_SYNC_DEBUG");
            if (memberTypeFullName == typeof(UnityEngine.Quaternion).FullName)
            {
                sb.Append(indent).AppendLine($"\t\tGONetLog.Debug($\"[VelocitySync][{{gonetParticipant.GONetId}}] DeserializeInitAll[{iOverall}]: angularVelocity={{angularVelocity}}, synthesizedRotation={{synthesizedValue}}\");");
            }
            else
            {
                sb.Append(indent).AppendLine($"\t\tGONetLog.Debug($\"[VelocitySync][{{gonetParticipant.GONetId}}] DeserializeInitAll[{iOverall}]: velocity={{velocity}}, synthesizedValue={{synthesizedValue}}\");");
            }
            sb.Append(indent).AppendLine("#endif");
            sb.AppendLine();

            // For velocity packets, we need to manually add the snapshot with velocity data
            // (AddToMostRecentChangeQueue_IfAppropriate would create a snapshot without velocity)
            sb.Append(indent).AppendLine("\t\t// Create and manually insert snapshot with velocity data (wasSynthesizedFromVelocity=true)");
            sb.Append(indent).AppendLine($"\t\tGONetSyncableValue synthesizedValueWrapped = synthesizedValue;");
            if (memberTypeFullName == typeof(UnityEngine.Quaternion).FullName)
            {
                sb.Append(indent).AppendLine($"\t\tGONetSyncableValue angularVelocityWrapped = angularVelocity;");
                sb.Append(indent).AppendLine($"\t\tvar velocitySnapshot = PluginAPI.NumericValueChangeSnapshot.CreateFromVelocityPacket(assumedElapsedTicksAtChange, synthesizedValueWrapped, angularVelocityWrapped);");
            }
            else
            {
                sb.Append(indent).AppendLine($"\t\tGONetSyncableValue velocityWrapped = velocity;");
                sb.Append(indent).AppendLine($"\t\tvar velocitySnapshot = PluginAPI.NumericValueChangeSnapshot.CreateFromVelocityPacket(assumedElapsedTicksAtChange, synthesizedValueWrapped, velocityWrapped);");
            }

            // Manually add to mostRecentChanges array (similar to AddToMostRecentChangeQueue_IfAppropriate but keeps velocity snapshot)
            sb.Append(indent).AppendLine("\t\t// Add velocity snapshot to mostRecentChanges buffer");
            sb.Append(indent).AppendLine($"\t\tif (valueChangeSupport.mostRecentChanges_usedSize < valueChangeSupport.mostRecentChanges_capacitySize)");
            sb.Append(indent).AppendLine("\t\t{");
            sb.Append(indent).AppendLine($"\t\t\tint insertIndex = valueChangeSupport.mostRecentChanges_usedSize;");
            sb.Append(indent).AppendLine($"\t\t\tvalueChangeSupport.mostRecentChanges[insertIndex] = velocitySnapshot;");
            sb.Append(indent).AppendLine($"\t\t\t++valueChangeSupport.mostRecentChanges_usedSize;");
            sb.Append(indent).AppendLine("\t\t}");
            sb.AppendLine();

            // Apply synthesized value to component
            sb.Append(indent).AppendLine("\t\t// Apply synthesized value to component");
            if (singleMember.animatorControllerParameterId == 0)
            {
                sb.Append(indent).Append("\t\t").Append(single.componentTypeName).Append(".").Append(singleMember.memberName).AppendLine(" = synthesizedValue;");
            }
            else
            {
                sb.Append(indent).Append("\t\t").Append(single.componentTypeName).Append(".Set").Append(singleMember.animatorControllerParameterMethodSuffix).Append("(").Append(singleMember.animatorControllerParameterId).AppendLine(", synthesizedValue);");
            }

            sb.Append(indent).AppendLine("\t}");
            sb.Append(indent).AppendLine("\telse");
            sb.Append(indent).AppendLine("\t{");
            sb.Append(indent).AppendLine("\t\t// VALUE packet: Deserialize position normally");
            sb.Append(indent).AppendLine("#if GONET_VELOCITY_SYNC_DEBUG");
            sb.Append(indent).AppendLine($"\t\tGONetLog.Debug($\"[VelocitySync][{{gonetParticipant.GONetId}}] DeserializeInitAll[{iOverall}]: Deserializing VALUE data (normal position)\");");
            sb.Append(indent).AppendLine("#endif");

            // VALUE packet: Normal deserialization
            WriteDeserializeSingle(iOverall, single, singleMember, indent + "\t", true);

            sb.Append(indent).AppendLine("\t}");
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
            sb.AppendLine("        /// Velocity-augmented sync: Supports deserializing either VALUE or VELOCITY data based on useVelocitySerializer parameter.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        /// <param name=\"bitStream_readFrom\">The bit stream to deserialize from</param>");
            sb.AppendLine("        /// <param name=\"singleIndex\">The index of the value to deserialize</param>");
            sb.AppendLine("        /// <param name=\"useVelocitySerializer\">If true, uses cachedVelocitySerializers; if false, uses cachedValueSerializers. Default false.</param>");
            sb.AppendLine("        /// <returns>The deserialized value (either VALUE or VELOCITY depending on useVelocitySerializer)</returns>");
            sb.AppendLine("        internal override GONet.GONetSyncableValue DeserializeInitSingle_ReadOnlyNotApply(Utils.BitByBitByteArrayBuilder bitStream_readFrom, byte singleIndex, bool useVelocitySerializer = false)");
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

        private void WriteDeserializeInitSingle()
        {
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Deserializes and initializes a single value.");
            sb.AppendLine("        /// Velocity-augmented sync: For VELOCITY bundles, synthesizes position from velocity before applying.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        internal override void DeserializeInitSingle(Utils.BitByBitByteArrayBuilder bitStream_readFrom, byte singleIndex, long assumedElapsedTicksAtChange, bool useVelocitySerializer = false)");
            sb.AppendLine("        {");
            sb.AppendLine("            GONetSyncableValue value = DeserializeInitSingle_ReadOnlyNotApply(bitStream_readFrom, singleIndex, useVelocitySerializer);");
            sb.AppendLine();
            sb.AppendLine("            // Velocity-augmented sync: Synthesize position from velocity for eligible fields");
            sb.AppendLine("            if (useVelocitySerializer && IsVelocityEligible(singleIndex))");
            sb.AppendLine("            {");
            sb.AppendLine("                value = SynthesizeValueFromVelocity(value, singleIndex, assumedElapsedTicksAtChange);");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            InitSingle(value, singleIndex, assumedElapsedTicksAtChange);");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private void WriteIsVelocityEligible()
        {
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Checks if a field is eligible for velocity-augmented sync.");
            sb.AppendLine("        /// A field is eligible if it has PhysicsUpdateInterval > 0.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        private bool IsVelocityEligible(byte singleIndex)");
            sb.AppendLine("        {");
            sb.AppendLine("            switch (singleIndex)");
            sb.AppendLine("            {");

            // Generate cases for fields with PhysicsUpdateInterval > 0
            int iOverall = 0;
            int singleCount = uniqueEntry.ComponentMemberNames_By_ComponentTypeFullName.Length;
            for (int iSingle = 0; iSingle < singleCount; ++iSingle)
            {
                GONetParticipant_ComponentsWithAutoSyncMembers_Single single = uniqueEntry.ComponentMemberNames_By_ComponentTypeFullName[iSingle];
                int singleMemberCount = single.autoSyncMembers.Length;
                for (int iSingleMember = 0; iSingleMember < singleMemberCount; ++iSingleMember)
                {
                    GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember singleMember = single.autoSyncMembers[iSingleMember];

                    if (singleMember.attribute.PhysicsUpdateInterval > 0)
                    {
                        sb.Append("                case ").Append(iOverall).Append(": return true; // ").Append(single.componentTypeName).Append(".").Append(singleMember.memberName).Append(" (PhysicsUpdateInterval=").Append(singleMember.attribute.PhysicsUpdateInterval).AppendLine(")");
                    }

                    ++iOverall;
                }
            }

            sb.AppendLine("                default: return false;");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private void WriteSynthesizeValueFromVelocity()
        {
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Synthesizes a new value from velocity data.");
            sb.AppendLine("        /// For Vector types: synthesizedValue = previousValue + velocity × deltaTime");
            sb.AppendLine("        /// For Quaternion: synthesizedValue = previousValue * exp(angularVelocity × deltaTime)");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        private GONetSyncableValue SynthesizeValueFromVelocity(GONetSyncableValue velocityValue, byte singleIndex, long assumedElapsedTicksAtChange)");
            sb.AppendLine("        {");
            sb.AppendLine("            switch (singleIndex)");
            sb.AppendLine("            {");

            // Generate synthesis cases for fields with PhysicsUpdateInterval > 0
            int iOverall = 0;
            int singleCount = uniqueEntry.ComponentMemberNames_By_ComponentTypeFullName.Length;
            for (int iSingle = 0; iSingle < singleCount; ++iSingle)
            {
                GONetParticipant_ComponentsWithAutoSyncMembers_Single single = uniqueEntry.ComponentMemberNames_By_ComponentTypeFullName[iSingle];
                int singleMemberCount = single.autoSyncMembers.Length;
                for (int iSingleMember = 0; iSingleMember < singleMemberCount; ++iSingleMember)
                {
                    GONetParticipant_ComponentsWithAutoSyncMembers_SingleMember singleMember = single.autoSyncMembers[iSingleMember];

                    if (singleMember.attribute.PhysicsUpdateInterval > 0)
                    {
                        string memberTypeFullName = singleMember.memberTypeFullName;

                        sb.Append("                case ").Append(iOverall).AppendLine(":");
                        sb.Append("                { // ").Append(single.componentTypeName).Append(".").AppendLine(singleMember.memberName);

                        // Get value change support
                        sb.AppendLine("                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport = valuesChangesSupport[" + iOverall + "];");
                        sb.AppendLine("                    int mostRecentChangesIndex = valueChangeSupport.mostRecentChanges_usedSize - 1;");
                        sb.AppendLine();
                        sb.AppendLine("                    if (mostRecentChangesIndex >= 0 && valueChangeSupport.mostRecentChanges_usedSize > 0)");
                        sb.AppendLine("                    {");
                        sb.AppendLine("                        PluginAPI.NumericValueChangeSnapshot previousSnapshot = valueChangeSupport.mostRecentChanges[mostRecentChangesIndex];");
                        sb.AppendLine("                        long deltaTimeTicks = assumedElapsedTicksAtChange - previousSnapshot.elapsedTicksAtChange;");
                        sb.AppendLine("                        float deltaTimeSeconds = (float)(deltaTimeTicks / (double)System.Diagnostics.Stopwatch.Frequency);");
                        sb.AppendLine();

                        if (memberTypeFullName == typeof(UnityEngine.Quaternion).FullName)
                        {
                            // Quaternion: Angular velocity integration
                            sb.AppendLine("                        // Angular velocity stored as Vector3 (axis × radians/sec)");
                            sb.AppendLine("                        UnityEngine.Vector3 angularVelocity = velocityValue.UnityEngine_Vector3;");
                            sb.AppendLine("                        float angle = angularVelocity.magnitude * deltaTimeSeconds;");
                            sb.AppendLine();
                            sb.AppendLine("                        if (angle > 1e-6f)");
                            sb.AppendLine("                        {");
                            sb.AppendLine("                            UnityEngine.Vector3 axis = angularVelocity.normalized;");
                            sb.AppendLine("                            UnityEngine.Quaternion deltaRotation = UnityEngine.Quaternion.AngleAxis(angle * UnityEngine.Mathf.Rad2Deg, axis);");
                            sb.AppendLine("                            UnityEngine.Quaternion synthesized = previousSnapshot.numericValue.UnityEngine_Quaternion * deltaRotation;");
                            sb.AppendLine();
                            sb.AppendLine("                            // DIAGNOSTIC: Log synthesis");
                            sb.Append("                            GONet.GONetLog.Debug($\"[CLIENT-AngularVel][{gonetParticipant.GONetId}][idx:").Append(iOverall).AppendLine("] previousRot={previousSnapshot.numericValue.UnityEngine_Quaternion.eulerAngles}, synthesized={synthesized.eulerAngles}, deltaTime={deltaTimeSeconds:F4}s\");");
                            sb.AppendLine();
                            sb.AppendLine("                            return new GONetSyncableValue { UnityEngine_Quaternion = synthesized };");
                            sb.AppendLine("                        }");
                            sb.AppendLine("                        else");
                            sb.AppendLine("                        {");
                            sb.AppendLine("                            // Angle too small, return previous value");
                            sb.AppendLine("                            return new GONetSyncableValue { UnityEngine_Quaternion = previousSnapshot.numericValue.UnityEngine_Quaternion };");
                            sb.AppendLine("                        }");
                        }
                        else if (memberTypeFullName == typeof(UnityEngine.Vector3).FullName)
                        {
                            // Vector3: Linear synthesis
                            sb.AppendLine("                        UnityEngine.Vector3 velocity = velocityValue.UnityEngine_Vector3;");
                            sb.AppendLine("                        UnityEngine.Vector3 synthesized = previousSnapshot.numericValue.UnityEngine_Vector3 + velocity * deltaTimeSeconds;");
                            sb.AppendLine();
                            sb.AppendLine("                        // DIAGNOSTIC: Log synthesis");
                            sb.Append("                        GONet.GONetLog.Debug($\"[CLIENT-Vel][{gonetParticipant.GONetId}][idx:").Append(iOverall).AppendLine("] previousPos={previousSnapshot.numericValue.UnityEngine_Vector3}, velocity={velocity}, synthesized={synthesized}, deltaTime={deltaTimeSeconds:F4}s\");");
                            sb.AppendLine();
                            sb.AppendLine("                        return new GONetSyncableValue { UnityEngine_Vector3 = synthesized };");
                        }
                        else if (memberTypeFullName == typeof(UnityEngine.Vector2).FullName)
                        {
                            // Vector2: Linear synthesis
                            sb.AppendLine("                        UnityEngine.Vector2 velocity = velocityValue.UnityEngine_Vector2;");
                            sb.AppendLine("                        UnityEngine.Vector2 synthesized = previousSnapshot.numericValue.UnityEngine_Vector2 + velocity * deltaTimeSeconds;");
                            sb.AppendLine();
                            sb.AppendLine("                        return new GONetSyncableValue { UnityEngine_Vector2 = synthesized };");
                        }
                        else if (memberTypeFullName == typeof(float).FullName)
                        {
                            // Float: Linear synthesis
                            sb.AppendLine("                        float velocity = velocityValue.System_Single;");
                            sb.AppendLine("                        float synthesized = previousSnapshot.numericValue.System_Single + velocity * deltaTimeSeconds;");
                            sb.AppendLine();
                            sb.AppendLine("                        return new GONetSyncableValue { System_Single = synthesized };");
                        }

                        sb.AppendLine("                    }");
                        sb.AppendLine();
                        sb.AppendLine("                    // No previous value, return velocity as-is (fallback)");
                        sb.AppendLine("                    return velocityValue;");
                        sb.AppendLine("                }");
                    }

                    ++iOverall;
                }
            }

            sb.AppendLine("                default:");
            sb.AppendLine("                    // Not velocity-eligible, return as-is");
            sb.AppendLine("                    return velocityValue;");
            sb.AppendLine("            }");
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

                    // NOTE: Velocity bit reading/processing happens in GONet.cs event processing layer, not here
                    // This method only deserializes the VALUE data (not velocity)
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
                // Velocity-augmented sync: Use appropriate serializer array based on useVelocitySerializer parameter
                // For backward compatibility, cachedCustomSerializers is used when not in ReadOnlyNotApply mode
                if (readOnly)
                {
                    sb.Append(indent).AppendLine("\t// Velocity-augmented sync: Choose serializer based on packet type (VALUE vs VELOCITY)");
                    sb.Append(indent).AppendLine("\tIGONetAutoMagicalSync_CustomSerializer customSerializer = useVelocitySerializer ? cachedVelocitySerializers[" + iOverall + "] : cachedValueSerializers[" + iOverall + "];");
                }
                else
                {
                    sb.Append(indent).AppendLine("\tIGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[" + iOverall + "];");
                }
                if (isDeserializeAll)
                {
                    sb.Append(indent).Append("\tvar value = customSerializer.Deserialize(bitStream_readFrom).").Append(memberTypeFullName.Replace(".", "_")).AppendLine(";");
                    // NOTE: DeserializeAll is ONLY used for INIT sync (always VALUE bundles, never VELOCITY bundles)
                    // So baseline addition is always correct here (no useVelocitySerializer check needed)
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
                    sb.Append(indent).AppendLine("\tGONetSyncableValue value = customSerializer.Deserialize(bitStream_readFrom);");

                    // CRITICAL FIX: Quaternion VELOCITY bundles contain Vector3 angular velocity, NOT Quaternion!
                    if (memberTypeFullName == typeof(UnityEngine.Quaternion).FullName)
                    {
                        sb.Append(indent).AppendLine("\tif (useVelocitySerializer)");
                        sb.Append(indent).AppendLine("\t{");
                        sb.Append(indent).AppendLine("\t\t// VELOCITY bundle: Contains Vector3 angular velocity (rad/s)");
                        sb.Append(indent).AppendLine("\t\tvar typedValue = value.UnityEngine_Vector3;");
                        sb.Append(indent).AppendLine("\t\tvalue.UnityEngine_Vector3 = typedValue;");
                        sb.Append(indent).AppendLine("\t}");
                        sb.Append(indent).AppendLine("\telse");
                        sb.Append(indent).AppendLine("\t{");
                        sb.Append(indent).AppendLine("\t\t// VALUE bundle: Contains Quaternion rotation");
                        sb.Append(indent).AppendLine("\t\tvar typedValue = value.UnityEngine_Quaternion;");
                        sb.Append(indent).AppendLine("\t\tvalue.UnityEngine_Quaternion = typedValue;");
                        sb.Append(indent).AppendLine("\t}");
                    }
                    else
                    {
                        sb.Append(indent).Append("\tvar typedValue = value.").Append(memberTypeFullName.Replace(".", "_")).AppendLine(";");
                        // VELOCITY-AUGMENTED SYNC FIX: Only add baseline for VALUE bundles, NOT VELOCITY bundles
                        // When useVelocitySerializer=true, we're deserializing velocity (not delta-from-baseline)
                        if (singleMember.attribute.QuantizeDownToBitCount > 0)
                        {
                            sb.Append(indent).AppendLine("\tif (!useVelocitySerializer)");
                            sb.Append(indent).AppendLine("\t{");
                            sb.Append(indent).Append("\t\ttypedValue += valuesChangesSupport[").Append(iOverall).Append("].baselineValue_current.").Append(memberTypeFullName.Replace(".", "_")).AppendLine(";");
                            sb.Append(indent).AppendLine("\t}");
                        }
                        // Update the GONetSyncableValue with the adjusted typed value before returning
                        sb.Append(indent).Append("\tvalue.").Append(memberTypeFullName.Replace(".", "_")).AppendLine(" = typedValue;");
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

        /// <summary>
        /// Dynamically calculates velocity quantization settings from VALUE quantization settings.
        /// Velocity range should match quantization step size (sub-quantization motion detection).
        /// </summary>
        private (float lowerBound, float upperBound, int bitCount) CalculateVelocityQuantizationSettings(
            float valueLowerBound,
            float valueUpperBound,
            int valueBitCount,
            float syncChangesEverySeconds,
            int physicsUpdateInterval,
            bool isQuaternion)
        {
            // Determine sync interval (used by both Quaternion and standard calculation)
            // NOTE: At code generation time (editor), UnityEngine.Time.fixedDeltaTime is 0!
            // Read the actual fixedDeltaTime from Project Settings → Time.
            float fixedDeltaTime = 0.02f; // Default Unity value (50Hz)
            try
            {
                var timeManager = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TimeManager.asset");
                if (timeManager != null && timeManager.Length > 0)
                {
                    var serializedObject = new UnityEditor.SerializedObject(timeManager[0]);
                    var fixedTimestepProperty = serializedObject.FindProperty("Fixed Timestep");
                    if (fixedTimestepProperty != null)
                    {
                        fixedDeltaTime = fixedTimestepProperty.floatValue;
                    }
                }
            }
            catch
            {
                // Fallback to default if we can't read project settings
                fixedDeltaTime = 0.02f;
            }

            float syncInterval = physicsUpdateInterval > 0
                ? fixedDeltaTime * physicsUpdateInterval
                : syncChangesEverySeconds;

            if (syncInterval <= 0.0001f)
            {
                syncInterval = 0.033f; // Fallback to 30 Hz
            }
            // DEBUG: Log calculation details
            UnityEngine.Debug.Log($"[VelocityCalc] physicsUpdateInterval={physicsUpdateInterval}, fixedDeltaTime={fixedDeltaTime}, syncChangesEverySeconds={syncChangesEverySeconds}, syncInterval={syncInterval}, isQuaternion={isQuaternion}, valueLowerBound={valueLowerBound}, valueUpperBound={valueUpperBound}, valueBitCount={valueBitCount}");


            // Special handling for Quaternion (angular velocity)
            if (isQuaternion)
            {
                // Quaternion uses smallest-three compression with fixed range: ±(1/√2) ≈ ±0.707 per component
                // With 9-bit default, this gives ~0.16° rotation precision
                float rotationPrecisionDegrees = 0.16f;

                // Calculate maximum sub-quantization angular velocity (degrees/sec)
                // This is the quantization step per sync interval, converted to velocity (per second)
                float maxAngularVelocityDegrees = rotationPrecisionDegrees / syncInterval;

                // Convert to radians/sec (angular velocity stored as Vector3 in radians)
                float maxAngularVelocityRad = maxAngularVelocityDegrees * UnityEngine.Mathf.Deg2Rad;

                // Use 9 bits for good angular velocity precision (~0.056°/s)
                int angularVelocityBitCount = 9;

                return (-maxAngularVelocityRad, maxAngularVelocityRad, angularVelocityBitCount);
            }

            // Standard position/scalar velocity calculation
            if (valueBitCount == 0 || valueLowerBound >= valueUpperBound)
            {
                // No quantization or invalid range - fallback to defaults
                return (-20f, 20f, 18);
            }

            // Calculate VALUE precision (quantization step size in value units per sync interval)
            float valueRange = valueUpperBound - valueLowerBound;
            float valuePrecision = valueRange / ((1 << valueBitCount) - 1);

            // Convert quantization step to equivalent velocity (value-units per SECOND)
            // This value will be multiplied by deltaTime at line 265-266 to get per-interval bounds
            // for runtime range checking.
            //
            // CRITICAL: No safety margin! We want to detect movements smaller than ONE quantization step.
            // Example: Transform.position with 18-bit quantization over [-125, 125], syncInterval=0.075s
            // → valuePrecision = 250 / (2^18 - 1) ≈ 0.000954 meters (~0.95mm per step)
            // → maxSubQuantizationVelocity = 0.000954 / 0.075 ≈ 0.0127 m/s
            // → Runtime bounds (after * deltaTime): 0.0127 * 0.075 = 0.000954 meters (one quantization step!)
            float maxSubQuantizationVelocity = valuePrecision / syncInterval;

            // For dynamic mode (sub-quantization detection), use FULL VALUE precision
            // Velocity deltas must have same resolution as position quantization to accurately
            // detect movements smaller than one quantization step
            //
            // Example: Transform.position with 18-bit quantization (1mm precision)
            // → Velocity should also use 18 bits to accurately represent 1mm deltas
            // → This ensures sub-millimeter velocities can be transmitted without loss
            //
            // For manual mode, this method isn't called (user specifies bit count directly)
            int bitCount = valueBitCount;

            return (-maxSubQuantizationVelocity, maxSubQuantizationVelocity, bitCount);
        }
    }
}
 
