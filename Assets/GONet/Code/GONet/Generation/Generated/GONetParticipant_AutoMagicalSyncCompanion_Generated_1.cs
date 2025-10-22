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
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using GONet;

namespace GONet.Generation
{
	internal sealed class GONetParticipant_AutoMagicalSyncCompanion_Generated_1 : GONetParticipant_AutoMagicalSyncCompanion_Generated
    {
		private GONet.GONetParticipant _GONetParticipant;
		internal GONet.GONetParticipant GONetParticipant
		{
			get
			{
				if ((object)_GONetParticipant == null)
				{
					_GONetParticipant = gonetParticipant.GetComponent<GONet.GONetParticipant>();
				}
				return _GONetParticipant;
			}
		}

		private UnityEngine.Transform _Transform;
		internal UnityEngine.Transform Transform
		{
			get
			{
				if ((object)_Transform == null)
				{
					_Transform = gonetParticipant.GetComponent<UnityEngine.Transform>();
				}
				return _Transform;
			}
		}

        internal override byte CodeGenerationId => 1;

        internal GONetParticipant_AutoMagicalSyncCompanion_Generated_1(GONetParticipant gonetParticipant) : base(gonetParticipant)
		{
			valuesCount = 7;
			
			cachedCustomSerializers = cachedCustomSerializersArrayPool.Borrow((int)valuesCount);
			cachedCustomValueBlendings = cachedCustomValueBlendingsArrayPool.Borrow((int)valuesCount);
			cachedCustomVelocityBlendings = cachedCustomVelocityBlendingsArrayPool.Borrow((int)valuesCount);
		    
			lastKnownValueChangesSinceLastCheck = lastKnownValuesChangedArrayPool.Borrow((int)valuesCount);
			Array.Clear(lastKnownValueChangesSinceLastCheck, 0, lastKnownValueChangesSinceLastCheck.Length);

			lastKnownValueAtRestBits = lastKnownValueAtRestBitsArrayPool.Borrow((int)valuesCount);
			lastKnownValueChangedAtElapsedTicks = lastKnownValueChangedAtElapsedTicksArrayPool.Borrow((int)valuesCount);
			for (int i = 0; i < (int)valuesCount; ++i)
			{
				lastKnownValueAtRestBits[i] = LAST_KNOWN_VALUE_IS_AT_REST_ALREADY_BROADCASTED; // when things start consider things at rest and alreayd broadcast as to avoid trying to broadcast at rest too early in the beginning
				lastKnownValueChangedAtElapsedTicks[i] = long.MaxValue; // want to start high so the subtraction from actual game time later on does not yield a high value on first times before set with a proper/real value of last change...which would cause an unwanted false positive
			}
			
            doesBaselineValueNeedAdjusting = doesBaselineValueNeedAdjustingArrayPool.Borrow((int)valuesCount);
            Array.Clear(doesBaselineValueNeedAdjusting, 0, doesBaselineValueNeedAdjusting.Length);

			valuesChangesSupport = valuesChangesSupportArrayPool.Borrow((int)valuesCount);
			
			var support0 = valuesChangesSupport[0] = valueChangeSupportArrayPool.Borrow();
            support0.baselineValue_current.System_UInt32 = GONetParticipant.GONetId; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support0.lastKnownValue.System_UInt32 = GONetParticipant.GONetId; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support0.lastKnownValue_previous.System_UInt32 = GONetParticipant.GONetId; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support0.valueLimitEncountered_min.System_UInt32 = GONetParticipant.GONetId; 
			support0.valueLimitEncountered_max.System_UInt32 = GONetParticipant.GONetId; 
			support0.syncCompanion = this;
			support0.memberName = "GONetId";
			support0.index = 0;
			support0.syncAttribute_MustRunOnUnityMainThread = true;
			support0.syncAttribute_ProcessingPriority = 0;
			support0.syncAttribute_ProcessingPriority_GONetInternalOverride = 2147483647;
			support0.syncAttribute_SyncChangesEverySeconds = 0f;
			support0.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support0.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			support0.syncAttribute_PhysicsUpdateInterval = 1;
			// TODO have to revisit this at some point to get animator parameters working: GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue((1, 0), out support0.syncAttribute_ShouldSkipSync);
			support0.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			cachedCustomSerializers[0] = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.GONetParticipant.GONetId_InitialAssignment_CustomSerializer>(0, -1.701412E+38f, 1.701412E+38f);


			var support1 = valuesChangesSupport[1] = valueChangeSupportArrayPool.Borrow();
            support1.baselineValue_current.System_Boolean = GONetParticipant.IsPositionSyncd; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support1.lastKnownValue.System_Boolean = GONetParticipant.IsPositionSyncd; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support1.lastKnownValue_previous.System_Boolean = GONetParticipant.IsPositionSyncd; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support1.valueLimitEncountered_min.System_Boolean = GONetParticipant.IsPositionSyncd; 
			support1.valueLimitEncountered_max.System_Boolean = GONetParticipant.IsPositionSyncd; 
			support1.syncCompanion = this;
			support1.memberName = "IsPositionSyncd";
			support1.index = 1;
			support1.syncAttribute_MustRunOnUnityMainThread = true;
			support1.syncAttribute_ProcessingPriority = 0;
			support1.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support1.syncAttribute_SyncChangesEverySeconds = 0.04166667f;
			support1.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
			support1.syncAttribute_ShouldBlendBetweenValuesReceived = true;
			support1.syncAttribute_PhysicsUpdateInterval = 1;
			// TODO have to revisit this at some point to get animator parameters working: GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue((1, 1), out support1.syncAttribute_ShouldSkipSync);
			support1.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

            int support1_mostRecentChanges_calcdSize = support1.syncAttribute_SyncChangesEverySeconds != 0 ? (int)((GONetMain.valueBlendingBufferLeadSeconds / support1.syncAttribute_SyncChangesEverySeconds) * 2.5f) : 0;
            support1.mostRecentChanges_capacitySize = Math.Max(support1_mostRecentChanges_calcdSize, GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.MOST_RECENT_CHANGEs_SIZE_MINIMUM);
			support1.mostRecentChanges = GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.mostRecentChangesPool.Borrow(support1.mostRecentChanges_capacitySize);

			var support2 = valuesChangesSupport[2] = valueChangeSupportArrayPool.Borrow();
            support2.baselineValue_current.System_Boolean = GONetParticipant.IsRotationSyncd; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support2.lastKnownValue.System_Boolean = GONetParticipant.IsRotationSyncd; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support2.lastKnownValue_previous.System_Boolean = GONetParticipant.IsRotationSyncd; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support2.valueLimitEncountered_min.System_Boolean = GONetParticipant.IsRotationSyncd; 
			support2.valueLimitEncountered_max.System_Boolean = GONetParticipant.IsRotationSyncd; 
			support2.syncCompanion = this;
			support2.memberName = "IsRotationSyncd";
			support2.index = 2;
			support2.syncAttribute_MustRunOnUnityMainThread = true;
			support2.syncAttribute_ProcessingPriority = 0;
			support2.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support2.syncAttribute_SyncChangesEverySeconds = 0.04166667f;
			support2.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
			support2.syncAttribute_ShouldBlendBetweenValuesReceived = true;
			support2.syncAttribute_PhysicsUpdateInterval = 1;
			// TODO have to revisit this at some point to get animator parameters working: GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue((1, 2), out support2.syncAttribute_ShouldSkipSync);
			support2.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

            int support2_mostRecentChanges_calcdSize = support2.syncAttribute_SyncChangesEverySeconds != 0 ? (int)((GONetMain.valueBlendingBufferLeadSeconds / support2.syncAttribute_SyncChangesEverySeconds) * 2.5f) : 0;
            support2.mostRecentChanges_capacitySize = Math.Max(support2_mostRecentChanges_calcdSize, GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.MOST_RECENT_CHANGEs_SIZE_MINIMUM);
			support2.mostRecentChanges = GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.mostRecentChangesPool.Borrow(support2.mostRecentChanges_capacitySize);

			var support3 = valuesChangesSupport[3] = valueChangeSupportArrayPool.Borrow();
            support3.baselineValue_current.System_UInt16 = GONetParticipant.OwnerAuthorityId; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support3.lastKnownValue.System_UInt16 = GONetParticipant.OwnerAuthorityId; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support3.lastKnownValue_previous.System_UInt16 = GONetParticipant.OwnerAuthorityId; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support3.valueLimitEncountered_min.System_UInt16 = GONetParticipant.OwnerAuthorityId; 
			support3.valueLimitEncountered_max.System_UInt16 = GONetParticipant.OwnerAuthorityId; 
			support3.syncCompanion = this;
			support3.memberName = "OwnerAuthorityId";
			support3.index = 3;
			support3.syncAttribute_MustRunOnUnityMainThread = true;
			support3.syncAttribute_ProcessingPriority = 0;
			support3.syncAttribute_ProcessingPriority_GONetInternalOverride = 2147483646;
			support3.syncAttribute_SyncChangesEverySeconds = 0f;
			support3.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support3.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			support3.syncAttribute_PhysicsUpdateInterval = 1;
			// TODO have to revisit this at some point to get animator parameters working: GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue((1, 3), out support3.syncAttribute_ShouldSkipSync);
			support3.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);


			var support4 = valuesChangesSupport[4] = valueChangeSupportArrayPool.Borrow();
            support4.baselineValue_current.System_UInt16 = GONetParticipant.RemotelyControlledByAuthorityId; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support4.lastKnownValue.System_UInt16 = GONetParticipant.RemotelyControlledByAuthorityId; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support4.lastKnownValue_previous.System_UInt16 = GONetParticipant.RemotelyControlledByAuthorityId; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support4.valueLimitEncountered_min.System_UInt16 = GONetParticipant.RemotelyControlledByAuthorityId; 
			support4.valueLimitEncountered_max.System_UInt16 = GONetParticipant.RemotelyControlledByAuthorityId; 
			support4.syncCompanion = this;
			support4.memberName = "RemotelyControlledByAuthorityId";
			support4.index = 4;
			support4.syncAttribute_MustRunOnUnityMainThread = true;
			support4.syncAttribute_ProcessingPriority = 0;
			support4.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support4.syncAttribute_SyncChangesEverySeconds = 0f;
			support4.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support4.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			support4.syncAttribute_PhysicsUpdateInterval = 1;
			// TODO have to revisit this at some point to get animator parameters working: GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue((1, 4), out support4.syncAttribute_ShouldSkipSync);
			support4.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);


			var support5 = valuesChangesSupport[5] = valueChangeSupportArrayPool.Borrow();
            support5.baselineValue_current.UnityEngine_Quaternion = Transform.rotation; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support5.lastKnownValue.UnityEngine_Quaternion = Transform.rotation; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support5.lastKnownValue_previous.UnityEngine_Quaternion = Transform.rotation; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support5.valueLimitEncountered_min.UnityEngine_Quaternion = Transform.rotation; 
			support5.valueLimitEncountered_max.UnityEngine_Quaternion = Transform.rotation; 
			support5.syncCompanion = this;
			support5.memberName = "rotation";
			support5.index = 5;
			support5.syncAttribute_MustRunOnUnityMainThread = true;
			support5.syncAttribute_ProcessingPriority = 0;
			support5.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support5.syncAttribute_SyncChangesEverySeconds = 0.04166667f;
			support5.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
			support5.syncAttribute_ShouldBlendBetweenValuesReceived = true;
			support5.syncAttribute_PhysicsUpdateInterval = 1;
			support5.syncAttribute_ShouldSkipSync = GONetMain.IsRotationNotSyncd;
			// TODO have to revisit this at some point to get animator parameters working: GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue((1, 5), out support5.syncAttribute_ShouldSkipSync);
			support5.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			cachedCustomSerializers[5] = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.QuaternionSerializer>(0, -1.701412E+38f, 1.701412E+38f);

			cachedCustomValueBlendings[5] = GONetAutoMagicalSyncAttribute.GetCustomValueBlending<GONet.PluginAPI.GONetValueBlending_Quaternion_ExtrapolateWithLowPassSmoothingFilter>();
			cachedCustomVelocityBlendings[5] = GONet.Utils.ValueBlendUtils.GetDefaultVelocityBlending(support5.lastKnownValue.GONetSyncType);
            int support5_mostRecentChanges_calcdSize = support5.syncAttribute_SyncChangesEverySeconds != 0 ? (int)((GONetMain.valueBlendingBufferLeadSeconds / support5.syncAttribute_SyncChangesEverySeconds) * 2.5f) : 0;
            support5.mostRecentChanges_capacitySize = Math.Max(support5_mostRecentChanges_calcdSize, GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.MOST_RECENT_CHANGEs_SIZE_MINIMUM);
			support5.mostRecentChanges = GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.mostRecentChangesPool.Borrow(support5.mostRecentChanges_capacitySize);

			var support6 = valuesChangesSupport[6] = valueChangeSupportArrayPool.Borrow();
            support6.baselineValue_current.UnityEngine_Vector3 = Transform.position; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support6.lastKnownValue.UnityEngine_Vector3 = Transform.position; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support6.lastKnownValue_previous.UnityEngine_Vector3 = Transform.position; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support6.valueLimitEncountered_min.UnityEngine_Vector3 = Transform.position; 
			support6.valueLimitEncountered_max.UnityEngine_Vector3 = Transform.position; 
			support6.syncCompanion = this;
			support6.memberName = "position";
			support6.index = 6;
			support6.syncAttribute_MustRunOnUnityMainThread = true;
			support6.syncAttribute_ProcessingPriority = 0;
			support6.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support6.syncAttribute_SyncChangesEverySeconds = 0.04166667f;
			support6.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
			support6.syncAttribute_ShouldBlendBetweenValuesReceived = true;
			support6.syncAttribute_PhysicsUpdateInterval = 3;
			support6.syncAttribute_ShouldSkipSync = GONetMain.IsPositionNotSyncd;
			// TODO have to revisit this at some point to get animator parameters working: GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue((1, 6), out support6.syncAttribute_ShouldSkipSync);
			support6.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-125f, 125f, 18, true);

			cachedCustomSerializers[6] = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.Vector3Serializer>(18, -125f, 125f);

			cachedCustomValueBlendings[6] = GONetAutoMagicalSyncAttribute.GetCustomValueBlending<GONet.PluginAPI.GONetValueBlending_Vector3_HermiteSpline>();
			cachedCustomVelocityBlendings[6] = GONet.Utils.ValueBlendUtils.GetDefaultVelocityBlending(support6.lastKnownValue.GONetSyncType);
            int support6_mostRecentChanges_calcdSize = support6.syncAttribute_SyncChangesEverySeconds != 0 ? (int)((GONetMain.valueBlendingBufferLeadSeconds / support6.syncAttribute_SyncChangesEverySeconds) * 2.5f) : 0;
            support6.mostRecentChanges_capacitySize = Math.Max(support6_mostRecentChanges_calcdSize, GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.MOST_RECENT_CHANGEs_SIZE_MINIMUM);
			support6.mostRecentChanges = GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.mostRecentChangesPool.Borrow(support6.mostRecentChanges_capacitySize);

		}

        internal override void SetAutoMagicalSyncValue(byte index, GONetSyncableValue value)
		{
			switch (index)
			{
				case 0:
					GONetParticipant.GONetId = value.System_UInt32;
					return;
				case 1:
					GONetParticipant.IsPositionSyncd = value.System_Boolean;
					return;
				case 2:
					GONetParticipant.IsRotationSyncd = value.System_Boolean;
					return;
				case 3:
					GONetParticipant.OwnerAuthorityId = value.System_UInt16;
					return;
				case 4:
					GONetParticipant.RemotelyControlledByAuthorityId = value.System_UInt16;
					return;
				case 5:
					Transform.rotation = value.UnityEngine_Quaternion;
					return;
				case 6:
					Transform.position = value.UnityEngine_Vector3;
					return;
			}
		}

        internal override GONetSyncableValue GetAutoMagicalSyncValue(byte index)
		{
			switch (index)
			{
				case 0:
					return GONetParticipant.GONetId;
				case 1:
					return GONetParticipant.IsPositionSyncd;
				case 2:
					return GONetParticipant.IsRotationSyncd;
				case 3:
					return GONetParticipant.OwnerAuthorityId;
				case 4:
					return GONetParticipant.RemotelyControlledByAuthorityId;
				case 5:
					return Transform.rotation;
				case 6:
					return Transform.position;
			}

			return default;
		}

        /// <summary>
        /// Serializes all values of appropriaate member variables internally to <paramref name="bitStream_appendTo"/>.
        /// Oops.  Just kidding....it's ALMOST all values.  The exception being <see cref="GONetParticipant.GONetId"/> because that has to be processed first separately in order
        /// to know which <see cref="GONetParticipant"/> we are working with in order to call this method.
        /// </summary>
        internal override void SerializeAll(Utils.BitByBitByteArrayBuilder bitStream_appendTo)
        {
            // Velocity-augmented sync: Reset flag for this bundle
            didSerializeAnyVelocitySyncedValuesThisBundle = false;

            // Velocity-augmented sync: Write bundle type bit (0 = VALUE, 1 = VELOCITY)
            bitStream_appendTo.WriteBit(nextBundleIsVelocity);

#if GONET_VELOCITY_SYNC_DEBUG
            GONetLog.Debug($"[VelocitySync][{gonetParticipant.GONetId}] SerializeAll: bundleType={(nextBundleIsVelocity ? "VELOCITY" : "VALUE")}");
#endif

			{ // GONetParticipant.IsPositionSyncd
				bitStream_appendTo.WriteBit(GONetParticipant.IsPositionSyncd);
			}
			{ // GONetParticipant.IsRotationSyncd
				bitStream_appendTo.WriteBit(GONetParticipant.IsRotationSyncd);
			}
			{ // GONetParticipant.OwnerAuthorityId
				bitStream_appendTo.WriteUShort(GONetParticipant.OwnerAuthorityId);
			}
			{ // GONetParticipant.RemotelyControlledByAuthorityId
				bitStream_appendTo.WriteUShort(GONetParticipant.RemotelyControlledByAuthorityId);
			}
			{ // Transform.rotation
				    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[5];
				customSerializer.Serialize(bitStream_appendTo, gonetParticipant, Transform.rotation);
			}
			{ // Transform.position
				    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[6];
				{ // SUB-QUANTIZATION DIAGNOSTIC for position
					var currentValue = Transform.position;
					var baselineValue = valuesChangesSupport[6].baselineValue_current.UnityEngine_Vector3;
					var deltaFromBaseline = currentValue - baselineValue;
					GONet.Utils.SubQuantizationDiagnostics.CheckAndLogIfSubQuantization(gonetParticipant.GONetId, "position", deltaFromBaseline, valuesChangesSupport[6].syncAttribute_QuantizerSettingsGroup, customSerializer);
					customSerializer.Serialize(bitStream_appendTo, gonetParticipant, deltaFromBaseline);
				}
			}

            // Velocity-augmented sync: Toggle ONLY if velocity-synced values were actually serialized
            // CRITICAL: Prevents empty VELOCITY packets when PhysicsUpdateInterval > 1 gates physics values
            if (didSerializeAnyVelocitySyncedValuesThisBundle)
            {
#if GONET_VELOCITY_SYNC_DEBUG
                GONetLog.Debug($"[VelocitySync][{gonetParticipant.GONetId}] SerializeAll: Toggling (had velocity-synced values) {(nextBundleIsVelocity ? "VELOCITY" : "VALUE")} → {(!nextBundleIsVelocity ? "VELOCITY" : "VALUE")}");
#endif
                ToggleBundleType();
            }
#if GONET_VELOCITY_SYNC_DEBUG
            else
            {
                GONetLog.Debug($"[VelocitySync][{gonetParticipant.GONetId}] SerializeAll: NOT toggling (no velocity-synced values serialized), keeping {(nextBundleIsVelocity ? "VELOCITY" : "VALUE")}");
            }
#endif
        }

        internal override void SerializeSingle(Utils.BitByBitByteArrayBuilder bitStream_appendTo, byte singleIndex)
        {
			switch (singleIndex)
			{
				case 0:
				{ // GONetParticipant.GONetId
					    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[0];
					customSerializer.Serialize(bitStream_appendTo, gonetParticipant, GONetParticipant.GONetId);
				}
				break;

				case 1:
				{ // GONetParticipant.IsPositionSyncd
					bitStream_appendTo.WriteBit(GONetParticipant.IsPositionSyncd);
				}
				break;

				case 2:
				{ // GONetParticipant.IsRotationSyncd
					bitStream_appendTo.WriteBit(GONetParticipant.IsRotationSyncd);
				}
				break;

				case 3:
				{ // GONetParticipant.OwnerAuthorityId
					bitStream_appendTo.WriteUShort(GONetParticipant.OwnerAuthorityId);
				}
				break;

				case 4:
				{ // GONetParticipant.RemotelyControlledByAuthorityId
					bitStream_appendTo.WriteUShort(GONetParticipant.RemotelyControlledByAuthorityId);
				}
				break;

				case 5:
				{ // Transform.rotation
					    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[5];
					customSerializer.Serialize(bitStream_appendTo, gonetParticipant, Transform.rotation);
				}
				break;

				case 6:
				{ // Transform.position
					    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[6];
					{ // SUB-QUANTIZATION DIAGNOSTIC for position
						var currentValue = Transform.position;
						var baselineValue = valuesChangesSupport[6].baselineValue_current.UnityEngine_Vector3;
						var deltaFromBaseline = currentValue - baselineValue;
						GONet.Utils.SubQuantizationDiagnostics.CheckAndLogIfSubQuantization(gonetParticipant.GONetId, "position", deltaFromBaseline, valuesChangesSupport[6].syncAttribute_QuantizerSettingsGroup, customSerializer);
						customSerializer.Serialize(bitStream_appendTo, gonetParticipant, deltaFromBaseline);
					}
				}
				break;

			}
        }

        /// <summary>
        /// PRE: value at <paramref name="singleIndex"/> is known to be configured to be quantized
        /// NOTE: This is only virtual to avoid upgrading customers prior to this being added having compilation issues when upgrading from a previous version of GONet
        /// </summary>
        protected override bool AreEqualQuantized(byte singleIndex, GONetSyncableValue valueA, GONetSyncableValue valueB)
		{
			switch (singleIndex)
			{
				case 0:
				{ // GONetParticipant.GONetId
				    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[0];
					return customSerializer.AreEqualConsideringQuantization(valueA, valueB);
				}
				break;

				case 1:
				{ // GONetParticipant.IsPositionSyncd
					return valueA.System_Boolean == valueB.System_Boolean;
				}
				break;

				case 2:
				{ // GONetParticipant.IsRotationSyncd
					return valueA.System_Boolean == valueB.System_Boolean;
				}
				break;

				case 3:
				{ // GONetParticipant.OwnerAuthorityId
					// handle quantization of this type eventually?
				}
				break;

				case 4:
				{ // GONetParticipant.RemotelyControlledByAuthorityId
					// handle quantization of this type eventually?
				}
				break;

				case 5:
				{ // Transform.rotation
				    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[5];
					return customSerializer.AreEqualConsideringQuantization(valueA, valueB);
				}
				break;

				case 6:
				{ // Transform.position
				    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[6];
					return customSerializer.AreEqualConsideringQuantization(valueA, valueB);
				}
				break;

			}

			return base.AreEqualQuantized(singleIndex, valueA, valueB);
		}

        /// <summary>
        /// Deserializes all values from <paramref name="bitStream_readFrom"/> and uses them to modify appropriate member variables internally.
        /// Oops.  Just kidding....it's ALMOST all values.  The exception being <see cref="GONetParticipant.GONetId"/> because that has to be processed first separately in order
        /// to know which <see cref="GONetParticipant"/> we are working with in order to call this method.
        /// </summary>
        internal override void DeserializeInitAll(Utils.BitByBitByteArrayBuilder bitStream_readFrom, long assumedElapsedTicksAtChange)
        {
            bool isVelocityBundle;
            bitStream_readFrom.ReadBit(out isVelocityBundle);

#if GONET_VELOCITY_SYNC_DEBUG
            GONetLog.Debug($"[VelocitySync][{gonetParticipant.GONetId}] DeserializeInitAll: Received bundleType={(isVelocityBundle ? "VELOCITY" : "VALUE")}");
#endif

			{ // GONetParticipant.IsPositionSyncd
				bool value;
			                bitStream_readFrom.ReadBit(out value);
				GONetParticipant.IsPositionSyncd = value;
			}
			{ // GONetParticipant.IsRotationSyncd
				bool value;
			                bitStream_readFrom.ReadBit(out value);
				GONetParticipant.IsRotationSyncd = value;
			}
			{ // GONetParticipant.OwnerAuthorityId
				ushort value;
			                bitStream_readFrom.ReadUShort(out value);
				GONetParticipant.OwnerAuthorityId = value;
			}
			{ // GONetParticipant.RemotelyControlledByAuthorityId
				ushort value;
			                bitStream_readFrom.ReadUShort(out value);
				GONetParticipant.RemotelyControlledByAuthorityId = value;
			}
			{ // Transform.rotation
				if (isVelocityBundle)
				{
					// VELOCITY packet: Deserialize velocity and synthesize position
			#if GONET_VELOCITY_SYNC_DEBUG
					GONetLog.Debug($"[VelocitySync][{gonetParticipant.GONetId}] DeserializeInitAll[5]: Deserializing VELOCITY data");
			#endif
					// Angular velocity uses Vector3 serializer (not Quaternion)
					// Create temporary Vector3 serializer for angular velocity
					var vector3Serializer = new PluginAPI.Vector3Serializer();
					UnityEngine.Vector3 angularVelocity = vector3Serializer.Deserialize(bitStream_readFrom).UnityEngine_Vector3;

					// Get previous value to synthesize from
					GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport = valuesChangesSupport[5];
					int mostRecentChangesIndex = valueChangeSupport.mostRecentChanges_usedSize - 1;

					UnityEngine.Quaternion synthesizedValue;
					if (mostRecentChangesIndex >= 0 && valueChangeSupport.mostRecentChanges_usedSize > 0)
					{
						PluginAPI.NumericValueChangeSnapshot previousSnapshot = valueChangeSupport.mostRecentChanges[mostRecentChangesIndex];
						long deltaTimeTicks = assumedElapsedTicksAtChange - previousSnapshot.elapsedTicksAtChange;
						float deltaTimeSeconds = (float)(deltaTimeTicks / (double)System.Diagnostics.Stopwatch.Frequency);

						// Integrate angular velocity: q_new = q_old * exp(omega * dt / 2)
						float angle = angularVelocity.magnitude * deltaTimeSeconds;
						if (angle > 1e-6f)
						{
							UnityEngine.Vector3 axis = angularVelocity.normalized;
							UnityEngine.Quaternion deltaRotation = UnityEngine.Quaternion.AngleAxis(angle * UnityEngine.Mathf.Rad2Deg, axis);
							synthesizedValue = previousSnapshot.numericValue.UnityEngine_Quaternion * deltaRotation;
						}
						else
						{
							synthesizedValue = previousSnapshot.numericValue.UnityEngine_Quaternion;
						}
					}
					else
					{
						// No previous value, use current as-is
						synthesizedValue = Transform.rotation;
					}

			#if GONET_VELOCITY_SYNC_DEBUG
					GONetLog.Debug($"[VelocitySync][{gonetParticipant.GONetId}] DeserializeInitAll[5]: angularVelocity={angularVelocity}, synthesizedRotation={synthesizedValue}");
			#endif

					// Create and manually insert snapshot with velocity data (wasSynthesizedFromVelocity=true)
					GONetSyncableValue synthesizedValueWrapped = synthesizedValue;
					GONetSyncableValue angularVelocityWrapped = angularVelocity;
					var velocitySnapshot = PluginAPI.NumericValueChangeSnapshot.CreateFromVelocityPacket(assumedElapsedTicksAtChange, synthesizedValueWrapped, angularVelocityWrapped);
					// Add velocity snapshot to mostRecentChanges buffer
					if (valueChangeSupport.mostRecentChanges_usedSize < valueChangeSupport.mostRecentChanges_capacitySize)
					{
						int insertIndex = valueChangeSupport.mostRecentChanges_usedSize;
						valueChangeSupport.mostRecentChanges[insertIndex] = velocitySnapshot;
						++valueChangeSupport.mostRecentChanges_usedSize;
					}

					// Apply synthesized value to component
					Transform.rotation = synthesizedValue;
				}
				else
				{
					// VALUE packet: Deserialize position normally
			#if GONET_VELOCITY_SYNC_DEBUG
					GONetLog.Debug($"[VelocitySync][{gonetParticipant.GONetId}] DeserializeInitAll[5]: Deserializing VALUE data (normal position)");
			#endif
					IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[5];
					var value = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Quaternion;
					Transform.rotation = value;
				}
			}
			{ // Transform.position
				if (isVelocityBundle)
				{
					// VELOCITY packet: Deserialize velocity and synthesize position
			#if GONET_VELOCITY_SYNC_DEBUG
					GONetLog.Debug($"[VelocitySync][{gonetParticipant.GONetId}] DeserializeInitAll[6]: Deserializing VELOCITY data");
			#endif
					IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[6];
					UnityEngine.Vector3 velocity = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Vector3;

					// Get previous value to synthesize from
					GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport = valuesChangesSupport[6];
					int mostRecentChangesIndex = valueChangeSupport.mostRecentChanges_usedSize - 1;

					UnityEngine.Vector3 synthesizedValue;
					if (mostRecentChangesIndex >= 0 && valueChangeSupport.mostRecentChanges_usedSize > 0)
					{
						PluginAPI.NumericValueChangeSnapshot previousSnapshot = valueChangeSupport.mostRecentChanges[mostRecentChangesIndex];
						long deltaTimeTicks = assumedElapsedTicksAtChange - previousSnapshot.elapsedTicksAtChange;
						float deltaTimeSeconds = (float)(deltaTimeTicks / (double)System.Diagnostics.Stopwatch.Frequency);

						// Synthesize: position = previousPosition + velocity × deltaTime
						synthesizedValue = previousSnapshot.numericValue.UnityEngine_Vector3 + velocity * deltaTimeSeconds;
					}
					else
					{
						// No previous value, use current as-is (first packet)
						synthesizedValue = Transform.position;
					}

			#if GONET_VELOCITY_SYNC_DEBUG
					GONetLog.Debug($"[VelocitySync][{gonetParticipant.GONetId}] DeserializeInitAll[6]: velocity={velocity}, synthesizedValue={synthesizedValue}");
			#endif

					// Create and manually insert snapshot with velocity data (wasSynthesizedFromVelocity=true)
					GONetSyncableValue synthesizedValueWrapped = synthesizedValue;
					GONetSyncableValue velocityWrapped = velocity;
					var velocitySnapshot = PluginAPI.NumericValueChangeSnapshot.CreateFromVelocityPacket(assumedElapsedTicksAtChange, synthesizedValueWrapped, velocityWrapped);
					// Add velocity snapshot to mostRecentChanges buffer
					if (valueChangeSupport.mostRecentChanges_usedSize < valueChangeSupport.mostRecentChanges_capacitySize)
					{
						int insertIndex = valueChangeSupport.mostRecentChanges_usedSize;
						valueChangeSupport.mostRecentChanges[insertIndex] = velocitySnapshot;
						++valueChangeSupport.mostRecentChanges_usedSize;
					}

					// Apply synthesized value to component
					Transform.position = synthesizedValue;
				}
				else
				{
					// VALUE packet: Deserialize position normally
			#if GONET_VELOCITY_SYNC_DEBUG
					GONetLog.Debug($"[VelocitySync][{gonetParticipant.GONetId}] DeserializeInitAll[6]: Deserializing VALUE data (normal position)");
			#endif
					IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[6];
					var value = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Vector3;
					value += valuesChangesSupport[6].baselineValue_current.UnityEngine_Vector3;
					Transform.position = value;
				}
			}
        }


        /// <summary>
        /// Simply deserializes in order to move along the bit stream counter, but does NOT apply the values (i.e, does NOT init).
        /// </summary>
        internal override GONet.GONetSyncableValue DeserializeInitSingle_ReadOnlyNotApply(Utils.BitByBitByteArrayBuilder bitStream_readFrom, byte singleIndex)
        {
			switch (singleIndex)
			{
				case 0:
				{ // GONetParticipant.GONetId
					IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[0];
					var value = customSerializer.Deserialize(bitStream_readFrom).System_UInt32;
					return value;
				}
				case 1:
				{ // GONetParticipant.IsPositionSyncd
					bool value;
				                bitStream_readFrom.ReadBit(out value);
					return value;
				}
				case 2:
				{ // GONetParticipant.IsRotationSyncd
					bool value;
				                bitStream_readFrom.ReadBit(out value);
					return value;
				}
				case 3:
				{ // GONetParticipant.OwnerAuthorityId
					ushort value;
				                bitStream_readFrom.ReadUShort(out value);
					return value;
				}
				case 4:
				{ // GONetParticipant.RemotelyControlledByAuthorityId
					ushort value;
				                bitStream_readFrom.ReadUShort(out value);
					return value;
				}
				case 5:
				{ // Transform.rotation
					IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[5];
					var value = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Quaternion;
					return value;
				}
				case 6:
				{ // Transform.position
					IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[6];
					var value = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Vector3;
					value += valuesChangesSupport[6].baselineValue_current.UnityEngine_Vector3;
					return value;
				}
			}

			return default;
        }

        internal override void InitSingle(GONetSyncableValue value, byte singleIndex, long assumedElapsedTicksAtChange)
        {
			switch (singleIndex)
			{
				case 0:					GONetParticipant.GONetId = value.System_UInt32; break;
				case 1:					valuesChangesSupport[1].AddToMostRecentChangeQueue_IfAppropriate(assumedElapsedTicksAtChange, value); break; // NOTE: this queue will be used each frame to blend between this value and others added there
				case 2:					valuesChangesSupport[2].AddToMostRecentChangeQueue_IfAppropriate(assumedElapsedTicksAtChange, value); break; // NOTE: this queue will be used each frame to blend between this value and others added there
				case 3:					GONetParticipant.OwnerAuthorityId = value.System_UInt16; break;
				case 4:					GONetParticipant.RemotelyControlledByAuthorityId = value.System_UInt16; break;
				case 5:					valuesChangesSupport[5].AddToMostRecentChangeQueue_IfAppropriate(assumedElapsedTicksAtChange, value); break; // NOTE: this queue will be used each frame to blend between this value and others added there
				case 6:					valuesChangesSupport[6].AddToMostRecentChangeQueue_IfAppropriate(assumedElapsedTicksAtChange, value); break; // NOTE: this queue will be used each frame to blend between this value and others added there
			}
        }

		internal override void UpdateLastKnownValues(GONetMain.SyncBundleUniqueGrouping onlyMatchIfUniqueGroupingMatches)
		{
				var valuesChangesSupport0 = valuesChangesSupport[0];
				if (DoesMatchUniqueGrouping(valuesChangesSupport0, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport0, 0)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport0.lastKnownValue_previous = valuesChangesSupport0.lastKnownValue;
					valuesChangesSupport0.lastKnownValue.System_UInt32 = GONetParticipant.GONetId;
				}

				var valuesChangesSupport1 = valuesChangesSupport[1];
				if (DoesMatchUniqueGrouping(valuesChangesSupport1, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport1, 1)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport1.lastKnownValue_previous = valuesChangesSupport1.lastKnownValue;
					valuesChangesSupport1.lastKnownValue.System_Boolean = GONetParticipant.IsPositionSyncd;
				}

				var valuesChangesSupport2 = valuesChangesSupport[2];
				if (DoesMatchUniqueGrouping(valuesChangesSupport2, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport2, 2)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport2.lastKnownValue_previous = valuesChangesSupport2.lastKnownValue;
					valuesChangesSupport2.lastKnownValue.System_Boolean = GONetParticipant.IsRotationSyncd;
				}

				var valuesChangesSupport3 = valuesChangesSupport[3];
				if (DoesMatchUniqueGrouping(valuesChangesSupport3, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport3, 3)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport3.lastKnownValue_previous = valuesChangesSupport3.lastKnownValue;
					valuesChangesSupport3.lastKnownValue.System_UInt16 = GONetParticipant.OwnerAuthorityId;
				}

				var valuesChangesSupport4 = valuesChangesSupport[4];
				if (DoesMatchUniqueGrouping(valuesChangesSupport4, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport4, 4)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport4.lastKnownValue_previous = valuesChangesSupport4.lastKnownValue;
					valuesChangesSupport4.lastKnownValue.System_UInt16 = GONetParticipant.RemotelyControlledByAuthorityId;
				}

				var valuesChangesSupport5 = valuesChangesSupport[5];
				if (DoesMatchUniqueGrouping(valuesChangesSupport5, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport5, 5)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport5.lastKnownValue_previous = valuesChangesSupport5.lastKnownValue;
					bool shouldSourceFromRigidbody = gonetParticipant.IsRigidBodyOwnerOnlyControlled && gonetParticipant.myRigidBody != null;
					if (shouldSourceFromRigidbody)
					{
						// Source from Rigidbody (physics simulation)
						valuesChangesSupport5.lastKnownValue.UnityEngine_Quaternion = gonetParticipant.myRigidBody.rotation;
					}
					else
					{
						// Source from Transform (regular sync)
						valuesChangesSupport5.lastKnownValue.UnityEngine_Quaternion = Transform.rotation;
					}
				}

				var valuesChangesSupport6 = valuesChangesSupport[6];
				if (DoesMatchUniqueGrouping(valuesChangesSupport6, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport6, 6)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport6.lastKnownValue_previous = valuesChangesSupport6.lastKnownValue;
					bool shouldSourceFromRigidbody = gonetParticipant.IsRigidBodyOwnerOnlyControlled && gonetParticipant.myRigidBody != null;
					if (shouldSourceFromRigidbody)
					{
						// Source from Rigidbody (physics simulation)
						valuesChangesSupport6.lastKnownValue.UnityEngine_Vector3 = gonetParticipant.myRigidBody.position;
					}
					else
					{
						// Source from Transform (regular sync)
						valuesChangesSupport6.lastKnownValue.UnityEngine_Vector3 = Transform.position;
					}
				}

		}

		internal override bool IsLastKnownValue_VeryCloseTo_Or_AlreadyOutsideOf_QuantizationRange(byte singleIndex, GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport)
		{
			switch (singleIndex)
			{
				case 0:
				{ // GONetParticipant.GONetId
					// this type not supported for this functionality
				}
				break;

				case 1:
				{ // GONetParticipant.IsPositionSyncd
					// this type not supported for this functionality
				}
				break;

				case 2:
				{ // GONetParticipant.IsRotationSyncd
					// this type not supported for this functionality
				}
				break;

				case 3:
				{ // GONetParticipant.OwnerAuthorityId
					// this type not supported for this functionality
				}
				break;

				case 4:
				{ // GONetParticipant.RemotelyControlledByAuthorityId
					// this type not supported for this functionality
				}
				break;

				case 5:
				{ // Transform.rotation
					// this type not supported for this functionality
				}
				break;

				case 6:
				{ // Transform.position
                    UnityEngine.Vector3 diff = valueChangeSupport.lastKnownValue.UnityEngine_Vector3 - valueChangeSupport.baselineValue_current.UnityEngine_Vector3;
					System.Single componentLimitLower = valueChangeSupport.syncAttribute_QuantizerSettingsGroup.lowerBound * 0.8f; // TODO cache this value
					System.Single componentLimitUpper = valueChangeSupport.syncAttribute_QuantizerSettingsGroup.upperBound * 0.8f; // TODO cache this value
                    bool isVeryCloseTo_Or_AlreadyOutsideOf_QuantizationRange = 
						diff.x < componentLimitLower || diff.x > componentLimitUpper ||
						diff.y < componentLimitLower || diff.y > componentLimitUpper ||
						diff.z < componentLimitLower || diff.z > componentLimitUpper;
					return isVeryCloseTo_Or_AlreadyOutsideOf_QuantizationRange;
				}
				break;

			}

			return false;
		}

        internal override ValueMonitoringSupport_NewBaselineEvent CreateNewBaselineValueEvent(uint gonetId, byte singleIndex, GONetSyncableValue newBaselineValue)
		{
			switch (singleIndex)
			{
				case 0:
				{ // GONetParticipant.GONetId
					return new ValueMonitoringSupport_NewBaselineEvent_System_UInt32() {
						GONetId = gonetId,
						ValueIndex = singleIndex,
						NewBaselineValue = newBaselineValue.System_UInt32
					};
				}

				case 1:
				{ // GONetParticipant.IsPositionSyncd
					return new ValueMonitoringSupport_NewBaselineEvent_System_Boolean() {
						GONetId = gonetId,
						ValueIndex = singleIndex,
						NewBaselineValue = newBaselineValue.System_Boolean
					};
				}

				case 2:
				{ // GONetParticipant.IsRotationSyncd
					return new ValueMonitoringSupport_NewBaselineEvent_System_Boolean() {
						GONetId = gonetId,
						ValueIndex = singleIndex,
						NewBaselineValue = newBaselineValue.System_Boolean
					};
				}

				case 3:
				{ // GONetParticipant.OwnerAuthorityId
					return new ValueMonitoringSupport_NewBaselineEvent_System_UInt16() {
						GONetId = gonetId,
						ValueIndex = singleIndex,
						NewBaselineValue = newBaselineValue.System_UInt16
					};
				}

				case 4:
				{ // GONetParticipant.RemotelyControlledByAuthorityId
					return new ValueMonitoringSupport_NewBaselineEvent_System_UInt16() {
						GONetId = gonetId,
						ValueIndex = singleIndex,
						NewBaselineValue = newBaselineValue.System_UInt16
					};
				}

				case 5:
				{ // Transform.rotation
					return new ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Quaternion() {
						GONetId = gonetId,
						ValueIndex = singleIndex,
						NewBaselineValue = newBaselineValue.UnityEngine_Quaternion
					};
				}

				case 6:
				{ // Transform.position
					return new ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector3() {
						GONetId = gonetId,
						ValueIndex = singleIndex,
						NewBaselineValue = newBaselineValue.UnityEngine_Vector3
					};
				}

			}

			return null;
		}
    }
}
