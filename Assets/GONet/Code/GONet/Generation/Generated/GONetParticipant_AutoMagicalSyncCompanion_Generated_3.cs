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
	internal sealed class GONetParticipant_AutoMagicalSyncCompanion_Generated_3 : GONetParticipant_AutoMagicalSyncCompanion_Generated
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

		private ClaimableObjectViaRpcs _ClaimableObjectViaRpcs;
		internal ClaimableObjectViaRpcs ClaimableObjectViaRpcs
		{
			get
			{
				if ((object)_ClaimableObjectViaRpcs == null)
				{
					_ClaimableObjectViaRpcs = gonetParticipant.GetComponent<ClaimableObjectViaRpcs>();
				}
				return _ClaimableObjectViaRpcs;
			}
		}

		private CircularMotion _CircularMotion;
		internal CircularMotion CircularMotion
		{
			get
			{
				if ((object)_CircularMotion == null)
				{
					_CircularMotion = gonetParticipant.GetComponent<CircularMotion>();
				}
				return _CircularMotion;
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

        internal override byte CodeGenerationId => 3;

        internal GONetParticipant_AutoMagicalSyncCompanion_Generated_3(GONetParticipant gonetParticipant) : base(gonetParticipant)
		{
			valuesCount = 10;
			
			cachedCustomSerializers = cachedCustomSerializersArrayPool.Borrow((int)valuesCount);
			cachedValueSerializers = cachedCustomSerializersArrayPool.Borrow((int)valuesCount);
			cachedVelocitySerializers = cachedCustomSerializersArrayPool.Borrow((int)valuesCount);
			cachedCustomValueBlendings = cachedCustomValueBlendingsArrayPool.Borrow((int)valuesCount);
			cachedCustomVelocityBlendings = cachedCustomVelocityBlendingsArrayPool.Borrow((int)valuesCount);

			// Velocity-augmented sync: Allocate syncCounter for per-value velocity frequency tracking
			syncCounter = syncCounterArrayPool.Borrow((int)valuesCount);
			Array.Clear(syncCounter, 0, syncCounter.Length); // Start at 0 for all values
		    
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
			support0.isVelocityEligible = false;
			support0.syncAttribute_VelocityQuantizeLowerBound = -20f;
			support0.syncAttribute_VelocityQuantizeUpperBound = 20f;
			support0.lastVelocityTimestamp = GONet.GONetMain.Time.ElapsedTicks;
			support0.codeGenerationMemberType = GONetSyncableValueTypes.System_UInt32;
			// TODO have to revisit this at some point to get animator parameters working: GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue((3, 0), out support0.syncAttribute_ShouldSkipSync);
			support0.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			cachedCustomSerializers[0] = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.GONetParticipant.GONetId_InitialAssignment_CustomSerializer>(0, -1.701412E+38f, 1.701412E+38f);
			cachedValueSerializers[0] = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.GONetParticipant.GONetId_InitialAssignment_CustomSerializer>(0, -1.701412E+38f, 1.701412E+38f);
			cachedVelocitySerializers[0] = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.GONetParticipant.GONetId_InitialAssignment_CustomSerializer>(18, -20.000000f, 20.000000f);


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
			support1.isVelocityEligible = false;
			support1.syncAttribute_VelocityQuantizeLowerBound = -20f;
			support1.syncAttribute_VelocityQuantizeUpperBound = 20f;
			support1.lastVelocityTimestamp = GONet.GONetMain.Time.ElapsedTicks;
			support1.codeGenerationMemberType = GONetSyncableValueTypes.System_Boolean;
			// TODO have to revisit this at some point to get animator parameters working: GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue((3, 1), out support1.syncAttribute_ShouldSkipSync);
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
			support2.isVelocityEligible = false;
			support2.syncAttribute_VelocityQuantizeLowerBound = -20f;
			support2.syncAttribute_VelocityQuantizeUpperBound = 20f;
			support2.lastVelocityTimestamp = GONet.GONetMain.Time.ElapsedTicks;
			support2.codeGenerationMemberType = GONetSyncableValueTypes.System_Boolean;
			// TODO have to revisit this at some point to get animator parameters working: GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue((3, 2), out support2.syncAttribute_ShouldSkipSync);
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
			support3.isVelocityEligible = false;
			support3.syncAttribute_VelocityQuantizeLowerBound = -20f;
			support3.syncAttribute_VelocityQuantizeUpperBound = 20f;
			support3.lastVelocityTimestamp = GONet.GONetMain.Time.ElapsedTicks;
			support3.codeGenerationMemberType = GONetSyncableValueTypes.System_UInt16;
			// TODO have to revisit this at some point to get animator parameters working: GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue((3, 3), out support3.syncAttribute_ShouldSkipSync);
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
			support4.isVelocityEligible = false;
			support4.syncAttribute_VelocityQuantizeLowerBound = -20f;
			support4.syncAttribute_VelocityQuantizeUpperBound = 20f;
			support4.lastVelocityTimestamp = GONet.GONetMain.Time.ElapsedTicks;
			support4.codeGenerationMemberType = GONetSyncableValueTypes.System_UInt16;
			// TODO have to revisit this at some point to get animator parameters working: GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue((3, 4), out support4.syncAttribute_ShouldSkipSync);
			support4.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);


			var support5 = valuesChangesSupport[5] = valueChangeSupportArrayPool.Borrow();
            support5.baselineValue_current.System_UInt16 = ClaimableObjectViaRpcs.ClaimedByAuthorityId; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support5.lastKnownValue.System_UInt16 = ClaimableObjectViaRpcs.ClaimedByAuthorityId; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support5.lastKnownValue_previous.System_UInt16 = ClaimableObjectViaRpcs.ClaimedByAuthorityId; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support5.valueLimitEncountered_min.System_UInt16 = ClaimableObjectViaRpcs.ClaimedByAuthorityId; 
			support5.valueLimitEncountered_max.System_UInt16 = ClaimableObjectViaRpcs.ClaimedByAuthorityId; 
			support5.syncCompanion = this;
			support5.memberName = "ClaimedByAuthorityId";
			support5.index = 5;
			support5.syncAttribute_MustRunOnUnityMainThread = false;
			support5.syncAttribute_ProcessingPriority = 0;
			support5.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support5.syncAttribute_SyncChangesEverySeconds = 0.04166667f;
			support5.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support5.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			support5.syncAttribute_PhysicsUpdateInterval = 1;
			support5.isVelocityEligible = false;
			support5.syncAttribute_VelocityQuantizeLowerBound = -20f;
			support5.syncAttribute_VelocityQuantizeUpperBound = 20f;
			support5.lastVelocityTimestamp = GONet.GONetMain.Time.ElapsedTicks;
			support5.codeGenerationMemberType = GONetSyncableValueTypes.System_UInt16;
			// TODO have to revisit this at some point to get animator parameters working: GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue((3, 5), out support5.syncAttribute_ShouldSkipSync);
			support5.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);


			var support6 = valuesChangesSupport[6] = valueChangeSupportArrayPool.Borrow();
            support6.baselineValue_current.System_Int32 = ClaimableObjectViaRpcs.TotalClaimCount; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support6.lastKnownValue.System_Int32 = ClaimableObjectViaRpcs.TotalClaimCount; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support6.lastKnownValue_previous.System_Int32 = ClaimableObjectViaRpcs.TotalClaimCount; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support6.valueLimitEncountered_min.System_Int32 = ClaimableObjectViaRpcs.TotalClaimCount; 
			support6.valueLimitEncountered_max.System_Int32 = ClaimableObjectViaRpcs.TotalClaimCount; 
			support6.syncCompanion = this;
			support6.memberName = "TotalClaimCount";
			support6.index = 6;
			support6.syncAttribute_MustRunOnUnityMainThread = false;
			support6.syncAttribute_ProcessingPriority = 0;
			support6.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support6.syncAttribute_SyncChangesEverySeconds = 0.04166667f;
			support6.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support6.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			support6.syncAttribute_PhysicsUpdateInterval = 1;
			support6.isVelocityEligible = false;
			support6.syncAttribute_VelocityQuantizeLowerBound = -20f;
			support6.syncAttribute_VelocityQuantizeUpperBound = 20f;
			support6.lastVelocityTimestamp = GONet.GONetMain.Time.ElapsedTicks;
			support6.codeGenerationMemberType = GONetSyncableValueTypes.System_Int32;
			// TODO have to revisit this at some point to get animator parameters working: GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue((3, 6), out support6.syncAttribute_ShouldSkipSync);
			support6.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);


			var support7 = valuesChangesSupport[7] = valueChangeSupportArrayPool.Borrow();
            support7.baselineValue_current.System_Single = CircularMotion.NettyWorkedFloat; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support7.lastKnownValue.System_Single = CircularMotion.NettyWorkedFloat; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support7.lastKnownValue_previous.System_Single = CircularMotion.NettyWorkedFloat; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support7.valueLimitEncountered_min.System_Single = CircularMotion.NettyWorkedFloat; 
			support7.valueLimitEncountered_max.System_Single = CircularMotion.NettyWorkedFloat; 
			support7.syncCompanion = this;
			support7.memberName = "NettyWorkedFloat";
			support7.index = 7;
			support7.syncAttribute_MustRunOnUnityMainThread = true;
			support7.syncAttribute_ProcessingPriority = 0;
			support7.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support7.syncAttribute_SyncChangesEverySeconds = 0.04166667f;
			support7.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
			support7.syncAttribute_ShouldBlendBetweenValuesReceived = true;
			support7.syncAttribute_PhysicsUpdateInterval = 1;
			support7.isVelocityEligible = false;
			support7.syncAttribute_VelocityQuantizeLowerBound = -20f;
			support7.syncAttribute_VelocityQuantizeUpperBound = 20f;
			support7.lastVelocityTimestamp = GONet.GONetMain.Time.ElapsedTicks;
			support7.codeGenerationMemberType = GONetSyncableValueTypes.System_Single;
			// TODO have to revisit this at some point to get animator parameters working: GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue((3, 7), out support7.syncAttribute_ShouldSkipSync);
			support7.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			cachedCustomValueBlendings[7] = GONetAutoMagicalSyncAttribute.GetCustomValueBlending<GONet.PluginAPI.GONetValueBlending_Float_ExtrapolateWithLowPassSmoothingFilter>();
			cachedCustomVelocityBlendings[7] = GONet.Utils.ValueBlendUtils.GetDefaultVelocityBlending(support7.lastKnownValue.GONetSyncType);
            int support7_mostRecentChanges_calcdSize = support7.syncAttribute_SyncChangesEverySeconds != 0 ? (int)((GONetMain.valueBlendingBufferLeadSeconds / support7.syncAttribute_SyncChangesEverySeconds) * 2.5f) : 0;
            support7.mostRecentChanges_capacitySize = Math.Max(support7_mostRecentChanges_calcdSize, GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.MOST_RECENT_CHANGEs_SIZE_MINIMUM);
			support7.mostRecentChanges = GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.mostRecentChangesPool.Borrow(support7.mostRecentChanges_capacitySize);

			var support8 = valuesChangesSupport[8] = valueChangeSupportArrayPool.Borrow();
            support8.baselineValue_current.UnityEngine_Quaternion = Transform.rotation; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support8.lastKnownValue.UnityEngine_Quaternion = Transform.rotation; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support8.lastKnownValue_previous.UnityEngine_Quaternion = Transform.rotation; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support8.valueLimitEncountered_min.UnityEngine_Quaternion = Transform.rotation; 
			support8.valueLimitEncountered_max.UnityEngine_Quaternion = Transform.rotation; 
			support8.syncCompanion = this;
			support8.memberName = "rotation";
			support8.index = 8;
			support8.syncAttribute_MustRunOnUnityMainThread = true;
			support8.syncAttribute_ProcessingPriority = 0;
			support8.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support8.syncAttribute_SyncChangesEverySeconds = 0.04166667f;
			support8.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
			support8.syncAttribute_ShouldBlendBetweenValuesReceived = true;
			support8.syncAttribute_PhysicsUpdateInterval = 1;
			support8.isVelocityEligible = true;
			support8.syncAttribute_VelocityQuantizeLowerBound = -20f;
			support8.syncAttribute_VelocityQuantizeUpperBound = 20f;
			support8.lastVelocityTimestamp = GONet.GONetMain.Time.ElapsedTicks;
			support8.codeGenerationMemberType = GONetSyncableValueTypes.UnityEngine_Quaternion;
			support8.syncAttribute_ShouldSkipSync = GONetMain.IsRotationNotSyncd;
			// TODO have to revisit this at some point to get animator parameters working: GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue((3, 8), out support8.syncAttribute_ShouldSkipSync);
			support8.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			cachedCustomSerializers[8] = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.QuaternionSerializer>(0, -1.701412E+38f, 1.701412E+38f);
			cachedValueSerializers[8] = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.QuaternionSerializer>(0, -1.701412E+38f, 1.701412E+38f);
			cachedVelocitySerializers[8] = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.Vector3Serializer>(9, -0.501652f, 0.501652f);

			cachedCustomValueBlendings[8] = GONetAutoMagicalSyncAttribute.GetCustomValueBlending<GONet.PluginAPI.GONetValueBlending_Quaternion_HighPerformance>();
			cachedCustomVelocityBlendings[8] = GONet.Utils.ValueBlendUtils.GetDefaultVelocityBlending(support8.lastKnownValue.GONetSyncType);
            int support8_mostRecentChanges_calcdSize = support8.syncAttribute_SyncChangesEverySeconds != 0 ? (int)((GONetMain.valueBlendingBufferLeadSeconds / support8.syncAttribute_SyncChangesEverySeconds) * 2.5f) : 0;
            support8.mostRecentChanges_capacitySize = Math.Max(support8_mostRecentChanges_calcdSize, GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.MOST_RECENT_CHANGEs_SIZE_MINIMUM);
			support8.mostRecentChanges = GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.mostRecentChangesPool.Borrow(support8.mostRecentChanges_capacitySize);

			var support9 = valuesChangesSupport[9] = valueChangeSupportArrayPool.Borrow();
            support9.baselineValue_current.UnityEngine_Vector3 = Transform.position; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support9.lastKnownValue.UnityEngine_Vector3 = Transform.position; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support9.lastKnownValue_previous.UnityEngine_Vector3 = Transform.position; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support9.valueLimitEncountered_min.UnityEngine_Vector3 = Transform.position; 
			support9.valueLimitEncountered_max.UnityEngine_Vector3 = Transform.position; 
			support9.syncCompanion = this;
			support9.memberName = "position";
			support9.index = 9;
			support9.syncAttribute_MustRunOnUnityMainThread = true;
			support9.syncAttribute_ProcessingPriority = 0;
			support9.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support9.syncAttribute_SyncChangesEverySeconds = 0.04166667f;
			support9.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
			support9.syncAttribute_ShouldBlendBetweenValuesReceived = true;
			support9.syncAttribute_PhysicsUpdateInterval = 3;
			support9.isVelocityEligible = true;
			support9.syncAttribute_VelocityQuantizeLowerBound = -20f;
			support9.syncAttribute_VelocityQuantizeUpperBound = 20f;
			support9.lastVelocityTimestamp = GONet.GONetMain.Time.ElapsedTicks;
			support9.codeGenerationMemberType = GONetSyncableValueTypes.UnityEngine_Vector3;
			support9.syncAttribute_ShouldSkipSync = GONetMain.IsPositionNotSyncd;
			// TODO have to revisit this at some point to get animator parameters working: GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue((3, 9), out support9.syncAttribute_ShouldSkipSync);
			support9.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-125f, 125f, 18, true);

			cachedCustomSerializers[9] = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.Vector3Serializer>(18, -125f, 125f);
			cachedValueSerializers[9] = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.Vector3Serializer>(18, -125f, 125f);
			cachedVelocitySerializers[9] = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.Vector3Serializer>(6, -0.057106f, 0.057106f);

			cachedCustomValueBlendings[9] = GONetAutoMagicalSyncAttribute.GetCustomValueBlending<GONet.PluginAPI.GONetValueBlending_Vector3_HighPerformance>();
			cachedCustomVelocityBlendings[9] = GONet.Utils.ValueBlendUtils.GetDefaultVelocityBlending(support9.lastKnownValue.GONetSyncType);
            int support9_mostRecentChanges_calcdSize = support9.syncAttribute_SyncChangesEverySeconds != 0 ? (int)((GONetMain.valueBlendingBufferLeadSeconds / support9.syncAttribute_SyncChangesEverySeconds) * 2.5f) : 0;
            support9.mostRecentChanges_capacitySize = Math.Max(support9_mostRecentChanges_calcdSize, GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.MOST_RECENT_CHANGEs_SIZE_MINIMUM);
			support9.mostRecentChanges = GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.mostRecentChangesPool.Borrow(support9.mostRecentChanges_capacitySize);

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
					ClaimableObjectViaRpcs.ClaimedByAuthorityId = value.System_UInt16;
					return;
				case 6:
					ClaimableObjectViaRpcs.TotalClaimCount = value.System_Int32;
					return;
				case 7:
					CircularMotion.NettyWorkedFloat = value.System_Single;
					return;
				case 8:
					Transform.rotation = value.UnityEngine_Quaternion;
					return;
				case 9:
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
					return ClaimableObjectViaRpcs.ClaimedByAuthorityId;
				case 6:
					return ClaimableObjectViaRpcs.TotalClaimCount;
				case 7:
					return CircularMotion.NettyWorkedFloat;
				case 8:
					return Transform.rotation;
				case 9:
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
            // SerializeAll is INIT ONLY - always send VALUES (no velocity data during initial sync)
#if GONET_VELOCITY_SYNC_DEBUG
            GONetLog.Debug($"[VelocitySync][{gonetParticipant.GONetId}] SerializeAll: INIT sync (always VALUE packets)");
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
			{ // ClaimableObjectViaRpcs.ClaimedByAuthorityId
				bitStream_appendTo.WriteUShort(ClaimableObjectViaRpcs.ClaimedByAuthorityId);
			}
			{ // ClaimableObjectViaRpcs.TotalClaimCount
				byte[] bytes = BitConverter.GetBytes(ClaimableObjectViaRpcs.TotalClaimCount);
				int count = bytes.Length;
				for (int i = 0; i < count; ++i)
				{
					bitStream_appendTo.WriteByte(bytes[i]);
				}
			}
			{ // CircularMotion.NettyWorkedFloat
				bitStream_appendTo.WriteFloat(CircularMotion.NettyWorkedFloat);
			}
			{ // Transform.rotation
				    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[8];
				customSerializer.Serialize(bitStream_appendTo, gonetParticipant, Transform.rotation);
			}
			{ // Transform.position
				    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[9];
				{ // SUB-QUANTIZATION DIAGNOSTIC for position
					var currentValue = Transform.position;
					var baselineValue = valuesChangesSupport[9].baselineValue_current.UnityEngine_Vector3;
					var deltaFromBaseline = currentValue - baselineValue;
					GONet.Utils.SubQuantizationDiagnostics.CheckAndLogIfSubQuantization(gonetParticipant.GONetId, "position", deltaFromBaseline, valuesChangesSupport[9].syncAttribute_QuantizerSettingsGroup, customSerializer);
					customSerializer.Serialize(bitStream_appendTo, gonetParticipant, deltaFromBaseline);
				}
			}
        }

        internal override void SerializeSingle(Utils.BitByBitByteArrayBuilder bitStream_appendTo, byte singleIndex, bool isVelocityBundle = false)
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
				{ // ClaimableObjectViaRpcs.ClaimedByAuthorityId
					bitStream_appendTo.WriteUShort(ClaimableObjectViaRpcs.ClaimedByAuthorityId);
				}
				break;

				case 6:
				{ // ClaimableObjectViaRpcs.TotalClaimCount
					byte[] bytes = BitConverter.GetBytes(ClaimableObjectViaRpcs.TotalClaimCount);
					int count = bytes.Length;
					for (int i = 0; i < count; ++i)
					{
						bitStream_appendTo.WriteByte(bytes[i]);
					}
				}
				break;

				case 7:
				{ // CircularMotion.NettyWorkedFloat
					if (isVelocityBundle)
					{
						// VELOCITY BUNDLE: Calculate velocity from last two snapshots
						var changesSupport = valuesChangesSupport[7];
						int recentChangesCount = changesSupport.mostRecentChanges_usedSize;
						GONetSyncableValue velocityValue;
						
						if (recentChangesCount >= 2)
						{
							var current = changesSupport.mostRecentChanges[0];
							var previous = changesSupport.mostRecentChanges[1];
							// DETERMINISTIC deltaTime for FixedUpdate path: Time.fixedDeltaTime × 1
							float deltaTime = UnityEngine.Time.fixedDeltaTime * 1f;
						
							if (deltaTime > 0.0001f)
							{
								float currentValue = current.numericValue.System_Single;
								float previousValue = previous.numericValue.System_Single;
								velocityValue = new GONetSyncableValue();
								velocityValue.System_Single = (currentValue - previousValue) / deltaTime;
							}
							else
							{
								// Delta time too small, use zero velocity
								GONet.GONetLog.Debug($"[VelocityCalc][{gonetParticipant.GONetId}][idx:7] DeltaTime too small: {deltaTime:F6}s");
								velocityValue = new GONetSyncableValue();
							}
						}
						else
						{
							// Not enough snapshots, use zero velocity
								GONet.GONetLog.Debug($"[VelocityCalc][{gonetParticipant.GONetId}][idx:7] Not enough snapshots: recentChangesCount={recentChangesCount}");
							velocityValue = new GONetSyncableValue();
						}
						
						// Serialize velocity using velocity serializer
						cachedVelocitySerializers[7].Serialize(bitStream_appendTo, gonetParticipant, velocityValue);
					}
					else
					{
						// VALUE BUNDLE: Serialize position normally
						bitStream_appendTo.WriteFloat(CircularMotion.NettyWorkedFloat);
					}
				}
				break;

				case 8:
				{ // Transform.rotation
					    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[8];
					if (isVelocityBundle)
					{
						// VELOCITY BUNDLE: Calculate velocity from last two snapshots
						var changesSupport = valuesChangesSupport[8];
						int recentChangesCount = changesSupport.mostRecentChanges_usedSize;
						GONetSyncableValue velocityValue;
						
						if (recentChangesCount >= 2)
						{
							var current = changesSupport.mostRecentChanges[0];
							var previous = changesSupport.mostRecentChanges[1];
							// DETERMINISTIC deltaTime for FixedUpdate path: Time.fixedDeltaTime × 1
							float deltaTime = UnityEngine.Time.fixedDeltaTime * 1f;
						
							if (deltaTime > 0.0001f)
							{
								// Angular velocity for Quaternion (stored as Vector3)
								UnityEngine.Quaternion currentValue = current.numericValue.UnityEngine_Quaternion;
								UnityEngine.Quaternion previousValue = previous.numericValue.UnityEngine_Quaternion;
						
								// DIAGNOSTIC: Log rotation values
								GONet.GONetLog.Debug($"[AngularVelCalc][{gonetParticipant.GONetId}][idx:8] current={currentValue.eulerAngles}, previous={previousValue.eulerAngles}, deltaTime={deltaTime:F4}s");
						
								UnityEngine.Quaternion deltaRotation = currentValue * UnityEngine.Quaternion.Inverse(previousValue);
								deltaRotation.ToAngleAxis(out float angle, out UnityEngine.Vector3 axis);
								UnityEngine.Vector3 angularVelocity = axis * (angle * UnityEngine.Mathf.Deg2Rad) / deltaTime;
								velocityValue = new GONetSyncableValue();
								velocityValue.UnityEngine_Vector3 = angularVelocity;
						
								GONet.GONetLog.Debug($"[AngularVelCalc][{gonetParticipant.GONetId}][idx:8] calculated angularVelocity={angularVelocity} rad/s, degrees/s={angularVelocity * UnityEngine.Mathf.Rad2Deg}");
							}
							else
							{
								// Delta time too small, use zero velocity
								GONet.GONetLog.Debug($"[VelocityCalc][{gonetParticipant.GONetId}][idx:8] DeltaTime too small: {deltaTime:F6}s");
								velocityValue = new GONetSyncableValue();
							}
						}
						else
						{
							// Not enough snapshots, use zero velocity
								GONet.GONetLog.Debug($"[VelocityCalc][{gonetParticipant.GONetId}][idx:8] Not enough snapshots: recentChangesCount={recentChangesCount}");
							velocityValue = new GONetSyncableValue();
						}
						
						// Serialize velocity using velocity serializer
						cachedVelocitySerializers[8].Serialize(bitStream_appendTo, gonetParticipant, velocityValue);
					}
					else
					{
						// VALUE BUNDLE: Serialize position normally
						customSerializer.Serialize(bitStream_appendTo, gonetParticipant, Transform.rotation);
				
						// Store snapshot for velocity calculation on next VELOCITY bundle
						var snapshotValue = new GONetSyncableValue();
						snapshotValue.UnityEngine_Quaternion = Transform.rotation;
						valuesChangesSupport[8].AddToMostRecentChangeQueue_IfAppropriate(GONet.GONetMain.Time.ElapsedTicks, snapshotValue);
					}
				}
				break;

				case 9:
				{ // Transform.position
					    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[9];
					if (isVelocityBundle)
					{
						// VELOCITY BUNDLE: Calculate velocity from last two snapshots
						var changesSupport = valuesChangesSupport[9];
						int recentChangesCount = changesSupport.mostRecentChanges_usedSize;
						GONetSyncableValue velocityValue;
						
						if (recentChangesCount >= 2)
						{
							var current = changesSupport.mostRecentChanges[0];
							var previous = changesSupport.mostRecentChanges[1];
							// DETERMINISTIC deltaTime for FixedUpdate path: Time.fixedDeltaTime × 3
							float deltaTime = UnityEngine.Time.fixedDeltaTime * 3f;
						
							if (deltaTime > 0.0001f)
							{
								UnityEngine.Vector3 currentValue = current.numericValue.UnityEngine_Vector3;
								UnityEngine.Vector3 previousValue = previous.numericValue.UnityEngine_Vector3;
						
								// DIAGNOSTIC: Log snapshot values and velocity calculation
								GONet.GONetLog.Debug($"[VelocityCalc][{gonetParticipant.GONetId}][idx:9] current={currentValue}, previous={previousValue}, deltaTime={deltaTime:F4}s");
						
								velocityValue = new GONetSyncableValue();
								velocityValue.UnityEngine_Vector3 = (currentValue - previousValue) / deltaTime;
						
								GONet.GONetLog.Debug($"[VelocityCalc][{gonetParticipant.GONetId}][idx:9] calculated velocity={velocityValue.UnityEngine_Vector3}");
							}
							else
							{
								// Delta time too small, use zero velocity
								GONet.GONetLog.Debug($"[VelocityCalc][{gonetParticipant.GONetId}][idx:9] DeltaTime too small: {deltaTime:F6}s");
								velocityValue = new GONetSyncableValue();
							}
						}
						else
						{
							// Not enough snapshots, use zero velocity
								GONet.GONetLog.Debug($"[VelocityCalc][{gonetParticipant.GONetId}][idx:9] Not enough snapshots: recentChangesCount={recentChangesCount}");
							velocityValue = new GONetSyncableValue();
						}
						
						// Serialize velocity using velocity serializer
						cachedVelocitySerializers[9].Serialize(bitStream_appendTo, gonetParticipant, velocityValue);
					}
					else
					{
						// VALUE BUNDLE: Serialize position normally
						{ // SUB-QUANTIZATION DIAGNOSTIC for position
							var currentValue = Transform.position;
							var baselineValue = valuesChangesSupport[9].baselineValue_current.UnityEngine_Vector3;
							var deltaFromBaseline = currentValue - baselineValue;
							GONet.Utils.SubQuantizationDiagnostics.CheckAndLogIfSubQuantization(gonetParticipant.GONetId, "position", deltaFromBaseline, valuesChangesSupport[9].syncAttribute_QuantizerSettingsGroup, customSerializer);
							customSerializer.Serialize(bitStream_appendTo, gonetParticipant, deltaFromBaseline);
						}
				
						// Store snapshot for velocity calculation on next VELOCITY bundle
						var snapshotValue = new GONetSyncableValue();
						snapshotValue.UnityEngine_Vector3 = Transform.position;
						valuesChangesSupport[9].AddToMostRecentChangeQueue_IfAppropriate(GONet.GONetMain.Time.ElapsedTicks, snapshotValue);
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
				{ // ClaimableObjectViaRpcs.ClaimedByAuthorityId
					// handle quantization of this type eventually?
				}
				break;

				case 6:
				{ // ClaimableObjectViaRpcs.TotalClaimCount
					// handle quantization of this type eventually?
				}
				break;

				case 7:
				{ // CircularMotion.NettyWorkedFloat
				}
				break;

				case 8:
				{ // Transform.rotation
				    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[8];
					return customSerializer.AreEqualConsideringQuantization(valueA, valueB);
				}
				break;

				case 9:
				{ // Transform.position
				    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[9];
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
#if GONET_VELOCITY_SYNC_DEBUG
            GONetLog.Debug($"[VelocitySync][{gonetParticipant.GONetId}] DeserializeInitAll: INIT sync (always VALUE packets)");
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
			{ // ClaimableObjectViaRpcs.ClaimedByAuthorityId
				ushort value;
			                bitStream_readFrom.ReadUShort(out value);
				ClaimableObjectViaRpcs.ClaimedByAuthorityId = value;
			}
			{ // ClaimableObjectViaRpcs.TotalClaimCount
				int count = 4;
				byte[] bytes = GetMyValueDeserializeByteArray();
				for (int i = 0; i < count; ++i)
				{
					byte b = (byte)bitStream_readFrom.ReadByte();
					bytes[i] = b;
				}
				ClaimableObjectViaRpcs.TotalClaimCount = BitConverter.ToInt32(bytes, 0);
			}
			{ // CircularMotion.NettyWorkedFloat
				float value;
			                bitStream_readFrom.ReadFloat(out value);
				CircularMotion.NettyWorkedFloat = value;
			}
			{ // Transform.rotation
				IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[8];
				var value = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Quaternion;
				Transform.rotation = value;
			}
			{ // Transform.position
				IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[9];
				var value = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Vector3;
				value += valuesChangesSupport[9].baselineValue_current.UnityEngine_Vector3;
				Transform.position = value;
			}
        }


        /// <summary>
        /// Simply deserializes in order to move along the bit stream counter, but does NOT apply the values (i.e, does NOT init).
        /// Velocity-augmented sync: Supports deserializing either VALUE or VELOCITY data based on useVelocitySerializer parameter.
        /// </summary>
        /// <param name="bitStream_readFrom">The bit stream to deserialize from</param>
        /// <param name="singleIndex">The index of the value to deserialize</param>
        /// <param name="useVelocitySerializer">If true, uses cachedVelocitySerializers; if false, uses cachedValueSerializers. Default false.</param>
        /// <returns>The deserialized value (either VALUE or VELOCITY depending on useVelocitySerializer)</returns>
        internal override GONet.GONetSyncableValue DeserializeInitSingle_ReadOnlyNotApply(Utils.BitByBitByteArrayBuilder bitStream_readFrom, byte singleIndex, bool useVelocitySerializer = false)
        {
			switch (singleIndex)
			{
				case 0:
				{ // GONetParticipant.GONetId
					// Velocity-augmented sync: Choose serializer based on packet type (VALUE vs VELOCITY)
					IGONetAutoMagicalSync_CustomSerializer customSerializer = useVelocitySerializer ? cachedVelocitySerializers[0] : cachedValueSerializers[0];
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
				{ // ClaimableObjectViaRpcs.ClaimedByAuthorityId
					ushort value;
				                bitStream_readFrom.ReadUShort(out value);
					return value;
				}
				case 6:
				{ // ClaimableObjectViaRpcs.TotalClaimCount
					int count = 4;
					byte[] bytes = GetMyValueDeserializeByteArray();
					for (int i = 0; i < count; ++i)
					{
						byte b = (byte)bitStream_readFrom.ReadByte();
						bytes[i] = b;
					}
					var value = BitConverter.ToInt32(bytes, 0);
					return value;
				}
				case 7:
				{ // CircularMotion.NettyWorkedFloat
					float value;
				                bitStream_readFrom.ReadFloat(out value);
					return value;
				}
				case 8:
				{ // Transform.rotation
					// Velocity-augmented sync: Choose serializer based on packet type (VALUE vs VELOCITY)
					IGONetAutoMagicalSync_CustomSerializer customSerializer = useVelocitySerializer ? cachedVelocitySerializers[8] : cachedValueSerializers[8];
					var value = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Quaternion;
					return value;
				}
				case 9:
				{ // Transform.position
					// Velocity-augmented sync: Choose serializer based on packet type (VALUE vs VELOCITY)
					IGONetAutoMagicalSync_CustomSerializer customSerializer = useVelocitySerializer ? cachedVelocitySerializers[9] : cachedValueSerializers[9];
					var value = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Vector3;
					if (!useVelocitySerializer)
					{
						value += valuesChangesSupport[9].baselineValue_current.UnityEngine_Vector3;
					}
					return value;
				}
			}

			return default;
        }


        /// <summary>
        /// Deserializes and initializes a single value.
        /// Velocity-augmented sync: For VELOCITY bundles, synthesizes position from velocity before applying.
        /// </summary>
        internal override void DeserializeInitSingle(Utils.BitByBitByteArrayBuilder bitStream_readFrom, byte singleIndex, long assumedElapsedTicksAtChange, bool useVelocitySerializer = false)
        {
            GONetSyncableValue value = DeserializeInitSingle_ReadOnlyNotApply(bitStream_readFrom, singleIndex, useVelocitySerializer);

            // Velocity-augmented sync: Synthesize position from velocity for eligible fields
            if (useVelocitySerializer && IsVelocityEligible(singleIndex))
            {
                value = SynthesizeValueFromVelocity(value, singleIndex, assumedElapsedTicksAtChange);
            }

            InitSingle(value, singleIndex, assumedElapsedTicksAtChange);
        }


        /// <summary>
        /// Checks if a field is eligible for velocity-augmented sync.
        /// A field is eligible if it has PhysicsUpdateInterval > 0.
        /// </summary>
        private bool IsVelocityEligible(byte singleIndex)
        {
            switch (singleIndex)
            {
                case 0: return true; // GONetParticipant.GONetId (PhysicsUpdateInterval=1)
                case 1: return true; // GONetParticipant.IsPositionSyncd (PhysicsUpdateInterval=1)
                case 2: return true; // GONetParticipant.IsRotationSyncd (PhysicsUpdateInterval=1)
                case 3: return true; // GONetParticipant.OwnerAuthorityId (PhysicsUpdateInterval=1)
                case 4: return true; // GONetParticipant.RemotelyControlledByAuthorityId (PhysicsUpdateInterval=1)
                case 5: return true; // ClaimableObjectViaRpcs.ClaimedByAuthorityId (PhysicsUpdateInterval=1)
                case 6: return true; // ClaimableObjectViaRpcs.TotalClaimCount (PhysicsUpdateInterval=1)
                case 7: return true; // CircularMotion.NettyWorkedFloat (PhysicsUpdateInterval=1)
                case 8: return true; // Transform.rotation (PhysicsUpdateInterval=1)
                case 9: return true; // Transform.position (PhysicsUpdateInterval=3)
                default: return false;
            }
        }


        /// <summary>
        /// Synthesizes a new value from velocity data.
        /// For Vector types: synthesizedValue = previousValue + velocity × deltaTime
        /// For Quaternion: synthesizedValue = previousValue * exp(angularVelocity × deltaTime)
        /// </summary>
        private GONetSyncableValue SynthesizeValueFromVelocity(GONetSyncableValue velocityValue, byte singleIndex, long assumedElapsedTicksAtChange)
        {
            switch (singleIndex)
            {
                case 0:
                { // GONetParticipant.GONetId
                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport = valuesChangesSupport[0];
                    int mostRecentChangesIndex = valueChangeSupport.mostRecentChanges_usedSize - 1;

                    if (mostRecentChangesIndex >= 0 && valueChangeSupport.mostRecentChanges_usedSize > 0)
                    {
                        PluginAPI.NumericValueChangeSnapshot previousSnapshot = valueChangeSupport.mostRecentChanges[mostRecentChangesIndex];
                        long deltaTimeTicks = assumedElapsedTicksAtChange - previousSnapshot.elapsedTicksAtChange;
                        float deltaTimeSeconds = (float)(deltaTimeTicks / (double)System.Diagnostics.Stopwatch.Frequency);

                    }

                    // No previous value, return velocity as-is (fallback)
                    return velocityValue;
                }
                case 1:
                { // GONetParticipant.IsPositionSyncd
                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport = valuesChangesSupport[1];
                    int mostRecentChangesIndex = valueChangeSupport.mostRecentChanges_usedSize - 1;

                    if (mostRecentChangesIndex >= 0 && valueChangeSupport.mostRecentChanges_usedSize > 0)
                    {
                        PluginAPI.NumericValueChangeSnapshot previousSnapshot = valueChangeSupport.mostRecentChanges[mostRecentChangesIndex];
                        long deltaTimeTicks = assumedElapsedTicksAtChange - previousSnapshot.elapsedTicksAtChange;
                        float deltaTimeSeconds = (float)(deltaTimeTicks / (double)System.Diagnostics.Stopwatch.Frequency);

                    }

                    // No previous value, return velocity as-is (fallback)
                    return velocityValue;
                }
                case 2:
                { // GONetParticipant.IsRotationSyncd
                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport = valuesChangesSupport[2];
                    int mostRecentChangesIndex = valueChangeSupport.mostRecentChanges_usedSize - 1;

                    if (mostRecentChangesIndex >= 0 && valueChangeSupport.mostRecentChanges_usedSize > 0)
                    {
                        PluginAPI.NumericValueChangeSnapshot previousSnapshot = valueChangeSupport.mostRecentChanges[mostRecentChangesIndex];
                        long deltaTimeTicks = assumedElapsedTicksAtChange - previousSnapshot.elapsedTicksAtChange;
                        float deltaTimeSeconds = (float)(deltaTimeTicks / (double)System.Diagnostics.Stopwatch.Frequency);

                    }

                    // No previous value, return velocity as-is (fallback)
                    return velocityValue;
                }
                case 3:
                { // GONetParticipant.OwnerAuthorityId
                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport = valuesChangesSupport[3];
                    int mostRecentChangesIndex = valueChangeSupport.mostRecentChanges_usedSize - 1;

                    if (mostRecentChangesIndex >= 0 && valueChangeSupport.mostRecentChanges_usedSize > 0)
                    {
                        PluginAPI.NumericValueChangeSnapshot previousSnapshot = valueChangeSupport.mostRecentChanges[mostRecentChangesIndex];
                        long deltaTimeTicks = assumedElapsedTicksAtChange - previousSnapshot.elapsedTicksAtChange;
                        float deltaTimeSeconds = (float)(deltaTimeTicks / (double)System.Diagnostics.Stopwatch.Frequency);

                    }

                    // No previous value, return velocity as-is (fallback)
                    return velocityValue;
                }
                case 4:
                { // GONetParticipant.RemotelyControlledByAuthorityId
                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport = valuesChangesSupport[4];
                    int mostRecentChangesIndex = valueChangeSupport.mostRecentChanges_usedSize - 1;

                    if (mostRecentChangesIndex >= 0 && valueChangeSupport.mostRecentChanges_usedSize > 0)
                    {
                        PluginAPI.NumericValueChangeSnapshot previousSnapshot = valueChangeSupport.mostRecentChanges[mostRecentChangesIndex];
                        long deltaTimeTicks = assumedElapsedTicksAtChange - previousSnapshot.elapsedTicksAtChange;
                        float deltaTimeSeconds = (float)(deltaTimeTicks / (double)System.Diagnostics.Stopwatch.Frequency);

                    }

                    // No previous value, return velocity as-is (fallback)
                    return velocityValue;
                }
                case 5:
                { // ClaimableObjectViaRpcs.ClaimedByAuthorityId
                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport = valuesChangesSupport[5];
                    int mostRecentChangesIndex = valueChangeSupport.mostRecentChanges_usedSize - 1;

                    if (mostRecentChangesIndex >= 0 && valueChangeSupport.mostRecentChanges_usedSize > 0)
                    {
                        PluginAPI.NumericValueChangeSnapshot previousSnapshot = valueChangeSupport.mostRecentChanges[mostRecentChangesIndex];
                        long deltaTimeTicks = assumedElapsedTicksAtChange - previousSnapshot.elapsedTicksAtChange;
                        float deltaTimeSeconds = (float)(deltaTimeTicks / (double)System.Diagnostics.Stopwatch.Frequency);

                    }

                    // No previous value, return velocity as-is (fallback)
                    return velocityValue;
                }
                case 6:
                { // ClaimableObjectViaRpcs.TotalClaimCount
                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport = valuesChangesSupport[6];
                    int mostRecentChangesIndex = valueChangeSupport.mostRecentChanges_usedSize - 1;

                    if (mostRecentChangesIndex >= 0 && valueChangeSupport.mostRecentChanges_usedSize > 0)
                    {
                        PluginAPI.NumericValueChangeSnapshot previousSnapshot = valueChangeSupport.mostRecentChanges[mostRecentChangesIndex];
                        long deltaTimeTicks = assumedElapsedTicksAtChange - previousSnapshot.elapsedTicksAtChange;
                        float deltaTimeSeconds = (float)(deltaTimeTicks / (double)System.Diagnostics.Stopwatch.Frequency);

                    }

                    // No previous value, return velocity as-is (fallback)
                    return velocityValue;
                }
                case 7:
                { // CircularMotion.NettyWorkedFloat
                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport = valuesChangesSupport[7];
                    int mostRecentChangesIndex = valueChangeSupport.mostRecentChanges_usedSize - 1;

                    if (mostRecentChangesIndex >= 0 && valueChangeSupport.mostRecentChanges_usedSize > 0)
                    {
                        PluginAPI.NumericValueChangeSnapshot previousSnapshot = valueChangeSupport.mostRecentChanges[mostRecentChangesIndex];
                        long deltaTimeTicks = assumedElapsedTicksAtChange - previousSnapshot.elapsedTicksAtChange;
                        float deltaTimeSeconds = (float)(deltaTimeTicks / (double)System.Diagnostics.Stopwatch.Frequency);

                        float velocity = velocityValue.System_Single;
                        float synthesized = previousSnapshot.numericValue.System_Single + velocity * deltaTimeSeconds;

                        return new GONetSyncableValue { System_Single = synthesized };
                    }

                    // No previous value, return velocity as-is (fallback)
                    return velocityValue;
                }
                case 8:
                { // Transform.rotation
                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport = valuesChangesSupport[8];
                    int mostRecentChangesIndex = valueChangeSupport.mostRecentChanges_usedSize - 1;

                    if (mostRecentChangesIndex >= 0 && valueChangeSupport.mostRecentChanges_usedSize > 0)
                    {
                        PluginAPI.NumericValueChangeSnapshot previousSnapshot = valueChangeSupport.mostRecentChanges[mostRecentChangesIndex];
                        long deltaTimeTicks = assumedElapsedTicksAtChange - previousSnapshot.elapsedTicksAtChange;
                        float deltaTimeSeconds = (float)(deltaTimeTicks / (double)System.Diagnostics.Stopwatch.Frequency);

                        // Angular velocity stored as Vector3 (axis × radians/sec)
                        UnityEngine.Vector3 angularVelocity = velocityValue.UnityEngine_Vector3;
                        float angle = angularVelocity.magnitude * deltaTimeSeconds;

                        if (angle > 1e-6f)
                        {
                            UnityEngine.Vector3 axis = angularVelocity.normalized;
                            UnityEngine.Quaternion deltaRotation = UnityEngine.Quaternion.AngleAxis(angle * UnityEngine.Mathf.Rad2Deg, axis);
                            UnityEngine.Quaternion synthesized = previousSnapshot.numericValue.UnityEngine_Quaternion * deltaRotation;

                            // DIAGNOSTIC: Log synthesis
                            GONet.GONetLog.Debug($"[CLIENT-AngularVel][{gonetParticipant.GONetId}][idx:8] previousRot={previousSnapshot.numericValue.UnityEngine_Quaternion.eulerAngles}, synthesized={synthesized.eulerAngles}, deltaTime={deltaTimeSeconds:F4}s");

                            return new GONetSyncableValue { UnityEngine_Quaternion = synthesized };
                        }
                        else
                        {
                            // Angle too small, return previous value
                            return new GONetSyncableValue { UnityEngine_Quaternion = previousSnapshot.numericValue.UnityEngine_Quaternion };
                        }
                    }

                    // No previous value, return velocity as-is (fallback)
                    return velocityValue;
                }
                case 9:
                { // Transform.position
                    GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport = valuesChangesSupport[9];
                    int mostRecentChangesIndex = valueChangeSupport.mostRecentChanges_usedSize - 1;

                    if (mostRecentChangesIndex >= 0 && valueChangeSupport.mostRecentChanges_usedSize > 0)
                    {
                        PluginAPI.NumericValueChangeSnapshot previousSnapshot = valueChangeSupport.mostRecentChanges[mostRecentChangesIndex];
                        long deltaTimeTicks = assumedElapsedTicksAtChange - previousSnapshot.elapsedTicksAtChange;
                        float deltaTimeSeconds = (float)(deltaTimeTicks / (double)System.Diagnostics.Stopwatch.Frequency);

                        UnityEngine.Vector3 velocity = velocityValue.UnityEngine_Vector3;
                        UnityEngine.Vector3 synthesized = previousSnapshot.numericValue.UnityEngine_Vector3 + velocity * deltaTimeSeconds;

                        // DIAGNOSTIC: Log synthesis
                        GONet.GONetLog.Debug($"[CLIENT-Vel][{gonetParticipant.GONetId}][idx:9] previousPos={previousSnapshot.numericValue.UnityEngine_Vector3}, velocity={velocity}, synthesized={synthesized}, deltaTime={deltaTimeSeconds:F4}s");

                        return new GONetSyncableValue { UnityEngine_Vector3 = synthesized };
                    }

                    // No previous value, return velocity as-is (fallback)
                    return velocityValue;
                }
                default:
                    // Not velocity-eligible, return as-is
                    return velocityValue;
            }
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
				case 5:					ClaimableObjectViaRpcs.ClaimedByAuthorityId = value.System_UInt16; break;
				case 6:					ClaimableObjectViaRpcs.TotalClaimCount = value.System_Int32; break;
				case 7:					valuesChangesSupport[7].AddToMostRecentChangeQueue_IfAppropriate(assumedElapsedTicksAtChange, value); break; // NOTE: this queue will be used each frame to blend between this value and others added there
				case 8:					valuesChangesSupport[8].AddToMostRecentChangeQueue_IfAppropriate(assumedElapsedTicksAtChange, value); break; // NOTE: this queue will be used each frame to blend between this value and others added there
				case 9:					valuesChangesSupport[9].AddToMostRecentChangeQueue_IfAppropriate(assumedElapsedTicksAtChange, value); break; // NOTE: this queue will be used each frame to blend between this value and others added there
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
					valuesChangesSupport5.lastKnownValue.System_UInt16 = ClaimableObjectViaRpcs.ClaimedByAuthorityId;
				}

				var valuesChangesSupport6 = valuesChangesSupport[6];
				if (DoesMatchUniqueGrouping(valuesChangesSupport6, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport6, 6)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport6.lastKnownValue_previous = valuesChangesSupport6.lastKnownValue;
					valuesChangesSupport6.lastKnownValue.System_Int32 = ClaimableObjectViaRpcs.TotalClaimCount;
				}

				var valuesChangesSupport7 = valuesChangesSupport[7];
				if (DoesMatchUniqueGrouping(valuesChangesSupport7, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport7, 7)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport7.lastKnownValue_previous = valuesChangesSupport7.lastKnownValue;
					valuesChangesSupport7.lastKnownValue.System_Single = CircularMotion.NettyWorkedFloat;
				}

				var valuesChangesSupport8 = valuesChangesSupport[8];
				if (DoesMatchUniqueGrouping(valuesChangesSupport8, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport8, 8)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport8.lastKnownValue_previous = valuesChangesSupport8.lastKnownValue;
					bool shouldSourceFromRigidbody = gonetParticipant.IsRigidBodyOwnerOnlyControlled && gonetParticipant.myRigidBody != null;
					if (shouldSourceFromRigidbody)
					{
						// Source from Rigidbody (physics simulation)
						valuesChangesSupport8.lastKnownValue.UnityEngine_Quaternion = gonetParticipant.myRigidBody.rotation;
					}
					else
					{
						// Source from Transform (regular sync)
						valuesChangesSupport8.lastKnownValue.UnityEngine_Quaternion = Transform.rotation;
					}
				}

				var valuesChangesSupport9 = valuesChangesSupport[9];
				if (DoesMatchUniqueGrouping(valuesChangesSupport9, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport9, 9)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport9.lastKnownValue_previous = valuesChangesSupport9.lastKnownValue;
					bool shouldSourceFromRigidbody = gonetParticipant.IsRigidBodyOwnerOnlyControlled && gonetParticipant.myRigidBody != null;
					if (shouldSourceFromRigidbody)
					{
						// Source from Rigidbody (physics simulation)
						valuesChangesSupport9.lastKnownValue.UnityEngine_Vector3 = gonetParticipant.myRigidBody.position;
					}
					else
					{
						// Source from Transform (regular sync)
						valuesChangesSupport9.lastKnownValue.UnityEngine_Vector3 = Transform.position;
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
				{ // ClaimableObjectViaRpcs.ClaimedByAuthorityId
					// this type not supported for this functionality
				}
				break;

				case 6:
				{ // ClaimableObjectViaRpcs.TotalClaimCount
					// this type not supported for this functionality
				}
				break;

				case 7:
				{ // CircularMotion.NettyWorkedFloat
                    System.Single diff = valueChangeSupport.lastKnownValue.System_Single - valueChangeSupport.baselineValue_current.System_Single;
					System.Single componentLimitLower = valueChangeSupport.syncAttribute_QuantizerSettingsGroup.lowerBound * 0.8f; // TODO cache this value
					System.Single componentLimitUpper = valueChangeSupport.syncAttribute_QuantizerSettingsGroup.upperBound * 0.8f; // TODO cache this value
                    bool isVeryCloseTo_Or_AlreadyOutsideOf_QuantizationRange = diff < componentLimitLower || diff > componentLimitUpper;
					return isVeryCloseTo_Or_AlreadyOutsideOf_QuantizationRange;
				}
				break;

				case 8:
				{ // Transform.rotation
					// this type not supported for this functionality
				}
				break;

				case 9:
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
				{ // ClaimableObjectViaRpcs.ClaimedByAuthorityId
					return new ValueMonitoringSupport_NewBaselineEvent_System_UInt16() {
						GONetId = gonetId,
						ValueIndex = singleIndex,
						NewBaselineValue = newBaselineValue.System_UInt16
					};
				}

				case 6:
				{ // ClaimableObjectViaRpcs.TotalClaimCount
					return new ValueMonitoringSupport_NewBaselineEvent_System_Int32() {
						GONetId = gonetId,
						ValueIndex = singleIndex,
						NewBaselineValue = newBaselineValue.System_Int32
					};
				}

				case 7:
				{ // CircularMotion.NettyWorkedFloat
					return new ValueMonitoringSupport_NewBaselineEvent_System_Single() {
						GONetId = gonetId,
						ValueIndex = singleIndex,
						NewBaselineValue = newBaselineValue.System_Single
					};
				}

				case 8:
				{ // Transform.rotation
					return new ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Quaternion() {
						GONetId = gonetId,
						ValueIndex = singleIndex,
						NewBaselineValue = newBaselineValue.UnityEngine_Quaternion
					};
				}

				case 9:
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
