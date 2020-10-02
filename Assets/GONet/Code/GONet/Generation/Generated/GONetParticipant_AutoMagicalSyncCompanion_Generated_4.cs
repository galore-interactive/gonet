


/* GONet (TM pending, serial number 88592370), Copyright (c) 2019 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@unitygo.net
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
	internal sealed class GONetParticipant_AutoMagicalSyncCompanion_Generated_4 : GONetParticipant_AutoMagicalSyncCompanion_Generated
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

		private GONet.DestroyIfMineOnKeyPress _DestroyIfMineOnKeyPress;
		internal GONet.DestroyIfMineOnKeyPress DestroyIfMineOnKeyPress
		{
			get
			{
				if ((object)_DestroyIfMineOnKeyPress == null)
				{
					_DestroyIfMineOnKeyPress = gonetParticipant.GetComponent<GONet.DestroyIfMineOnKeyPress>();
				}
				return _DestroyIfMineOnKeyPress;
			}
		}

		private FieldChangeTest _FieldChangeTest;
		internal FieldChangeTest FieldChangeTest
		{
			get
			{
				if ((object)_FieldChangeTest == null)
				{
					_FieldChangeTest = gonetParticipant.GetComponent<FieldChangeTest>();
				}
				return _FieldChangeTest;
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


        internal override byte CodeGenerationId => 4;

        internal GONetParticipant_AutoMagicalSyncCompanion_Generated_4(GONetParticipant gonetParticipant) : base(gonetParticipant)
		{
			valuesCount = 11;
			
			cachedCustomSerializers = cachedCustomSerializersArrayPool.Borrow((int)valuesCount);
		    
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
			support0.syncCompanion = this;
			support0.index = 0;
			support0.syncAttribute_MustRunOnUnityMainThread = true;
			support0.syncAttribute_ProcessingPriority = 0;
			support0.syncAttribute_ProcessingPriority_GONetInternalOverride = 2147483647;
			support0.syncAttribute_SyncChangesEverySeconds = 0f;
			support0.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support0.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support0.syncAttribute_ShouldSkipSync);
			support0.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			cachedCustomSerializers[0] = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.GONetParticipant.GONetId_InitialAssignment_CustomSerializer>(0, -1.701412E+38f, 1.701412E+38f);

			var support1 = valuesChangesSupport[1] = valueChangeSupportArrayPool.Borrow();
            support1.baselineValue_current.System_Boolean = GONetParticipant.IsPositionSyncd; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support1.lastKnownValue.System_Boolean = GONetParticipant.IsPositionSyncd; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support1.lastKnownValue_previous.System_Boolean = GONetParticipant.IsPositionSyncd; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support1.syncCompanion = this;
			support1.index = 1;
			support1.syncAttribute_MustRunOnUnityMainThread = true;
			support1.syncAttribute_ProcessingPriority = 0;
			support1.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support1.syncAttribute_SyncChangesEverySeconds = 0.04166667f;
			support1.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
			support1.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support1.syncAttribute_ShouldSkipSync);
			support1.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);


			var support2 = valuesChangesSupport[2] = valueChangeSupportArrayPool.Borrow();
            support2.baselineValue_current.System_Boolean = GONetParticipant.IsRotationSyncd; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support2.lastKnownValue.System_Boolean = GONetParticipant.IsRotationSyncd; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support2.lastKnownValue_previous.System_Boolean = GONetParticipant.IsRotationSyncd; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support2.syncCompanion = this;
			support2.index = 2;
			support2.syncAttribute_MustRunOnUnityMainThread = true;
			support2.syncAttribute_ProcessingPriority = 0;
			support2.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support2.syncAttribute_SyncChangesEverySeconds = 0.04166667f;
			support2.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
			support2.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support2.syncAttribute_ShouldSkipSync);
			support2.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);


			var support3 = valuesChangesSupport[3] = valueChangeSupportArrayPool.Borrow();
            support3.baselineValue_current.System_UInt16 = GONetParticipant.OwnerAuthorityId; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support3.lastKnownValue.System_UInt16 = GONetParticipant.OwnerAuthorityId; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support3.lastKnownValue_previous.System_UInt16 = GONetParticipant.OwnerAuthorityId; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support3.syncCompanion = this;
			support3.index = 3;
			support3.syncAttribute_MustRunOnUnityMainThread = true;
			support3.syncAttribute_ProcessingPriority = 0;
			support3.syncAttribute_ProcessingPriority_GONetInternalOverride = 2147483646;
			support3.syncAttribute_SyncChangesEverySeconds = 0f;
			support3.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support3.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support3.syncAttribute_ShouldSkipSync);
			support3.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);


			var support4 = valuesChangesSupport[4] = valueChangeSupportArrayPool.Borrow();
            support4.baselineValue_current.System_Single = DestroyIfMineOnKeyPress.willHeUpdate; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support4.lastKnownValue.System_Single = DestroyIfMineOnKeyPress.willHeUpdate; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support4.lastKnownValue_previous.System_Single = DestroyIfMineOnKeyPress.willHeUpdate; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support4.syncCompanion = this;
			support4.index = 4;
			support4.syncAttribute_MustRunOnUnityMainThread = false;
			support4.syncAttribute_ProcessingPriority = 3;
			support4.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support4.syncAttribute_SyncChangesEverySeconds = 0.04166667f;
			support4.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support4.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support4.syncAttribute_ShouldSkipSync);
			support4.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);


			var support5 = valuesChangesSupport[5] = valueChangeSupportArrayPool.Borrow();
            support5.baselineValue_current.UnityEngine_Vector3 = FieldChangeTest.color; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support5.lastKnownValue.UnityEngine_Vector3 = FieldChangeTest.color; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support5.lastKnownValue_previous.UnityEngine_Vector3 = FieldChangeTest.color; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support5.syncCompanion = this;
			support5.index = 5;
			support5.syncAttribute_MustRunOnUnityMainThread = true;
			support5.syncAttribute_ProcessingPriority = 0;
			support5.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support5.syncAttribute_SyncChangesEverySeconds = 0.04166667f;
			support5.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
			support5.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support5.syncAttribute_ShouldSkipSync);
			support5.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			cachedCustomSerializers[5] = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.Vector3Serializer>(0, -1.701412E+38f, 1.701412E+38f);

			var support6 = valuesChangesSupport[6] = valueChangeSupportArrayPool.Borrow();
            support6.baselineValue_current.UnityEngine_Vector3 = FieldChangeTest.color_dosientos; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support6.lastKnownValue.UnityEngine_Vector3 = FieldChangeTest.color_dosientos; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support6.lastKnownValue_previous.UnityEngine_Vector3 = FieldChangeTest.color_dosientos; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support6.syncCompanion = this;
			support6.index = 6;
			support6.syncAttribute_MustRunOnUnityMainThread = true;
			support6.syncAttribute_ProcessingPriority = 0;
			support6.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support6.syncAttribute_SyncChangesEverySeconds = 0.04166667f;
			support6.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
			support6.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support6.syncAttribute_ShouldSkipSync);
			support6.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			cachedCustomSerializers[6] = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.Vector3Serializer>(0, -1.701412E+38f, 1.701412E+38f);

			var support7 = valuesChangesSupport[7] = valueChangeSupportArrayPool.Borrow();
            support7.baselineValue_current.System_Single = FieldChangeTest.nada; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support7.lastKnownValue.System_Single = FieldChangeTest.nada; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support7.lastKnownValue_previous.System_Single = FieldChangeTest.nada; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support7.syncCompanion = this;
			support7.index = 7;
			support7.syncAttribute_MustRunOnUnityMainThread = true;
			support7.syncAttribute_ProcessingPriority = 0;
			support7.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support7.syncAttribute_SyncChangesEverySeconds = 0.04166667f;
			support7.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
			support7.syncAttribute_ShouldBlendBetweenValuesReceived = true;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support7.syncAttribute_ShouldSkipSync);
			support7.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-10f, 10f, 16, true);

		
            int support7_mostRecentChanges_calcdSize = support7.syncAttribute_SyncChangesEverySeconds != 0 ? (int)((GONetMain.valueBlendingBufferLeadSeconds / support7.syncAttribute_SyncChangesEverySeconds) * 2.5f) : 0;
            support7.mostRecentChanges_capacitySize = Math.Max(support7_mostRecentChanges_calcdSize, GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.MOST_RECENT_CHANGEs_SIZE_MINIMUM);
			support7.mostRecentChanges = GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.mostRecentChangesPool.Borrow(support7.mostRecentChanges_capacitySize);

			var support8 = valuesChangesSupport[8] = valueChangeSupportArrayPool.Borrow();
            support8.baselineValue_current.System_Int16 = FieldChangeTest.shortie; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support8.lastKnownValue.System_Int16 = FieldChangeTest.shortie; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support8.lastKnownValue_previous.System_Int16 = FieldChangeTest.shortie; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support8.syncCompanion = this;
			support8.index = 8;
			support8.syncAttribute_MustRunOnUnityMainThread = true;
			support8.syncAttribute_ProcessingPriority = 0;
			support8.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support8.syncAttribute_SyncChangesEverySeconds = 0.04166667f;
			support8.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
			support8.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support8.syncAttribute_ShouldSkipSync);
			support8.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);


			var support9 = valuesChangesSupport[9] = valueChangeSupportArrayPool.Borrow();
            support9.baselineValue_current.UnityEngine_Quaternion = Transform.rotation; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support9.lastKnownValue.UnityEngine_Quaternion = Transform.rotation; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support9.lastKnownValue_previous.UnityEngine_Quaternion = Transform.rotation; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support9.syncCompanion = this;
			support9.index = 9;
			support9.syncAttribute_MustRunOnUnityMainThread = true;
			support9.syncAttribute_ProcessingPriority = 0;
			support9.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support9.syncAttribute_SyncChangesEverySeconds = 0.03333334f;
			support9.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
			support9.syncAttribute_ShouldBlendBetweenValuesReceived = true;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(1, out support9.syncAttribute_ShouldSkipSync);
			support9.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			cachedCustomSerializers[9] = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.QuaternionSerializer>(0, -1.701412E+38f, 1.701412E+38f);
		
            int support9_mostRecentChanges_calcdSize = support9.syncAttribute_SyncChangesEverySeconds != 0 ? (int)((GONetMain.valueBlendingBufferLeadSeconds / support9.syncAttribute_SyncChangesEverySeconds) * 2.5f) : 0;
            support9.mostRecentChanges_capacitySize = Math.Max(support9_mostRecentChanges_calcdSize, GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.MOST_RECENT_CHANGEs_SIZE_MINIMUM);
			support9.mostRecentChanges = GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.mostRecentChangesPool.Borrow(support9.mostRecentChanges_capacitySize);

			var support10 = valuesChangesSupport[10] = valueChangeSupportArrayPool.Borrow();
            support10.baselineValue_current.UnityEngine_Vector3 = Transform.position; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support10.lastKnownValue.UnityEngine_Vector3 = Transform.position; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support10.lastKnownValue_previous.UnityEngine_Vector3 = Transform.position; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support10.syncCompanion = this;
			support10.index = 10;
			support10.syncAttribute_MustRunOnUnityMainThread = true;
			support10.syncAttribute_ProcessingPriority = 0;
			support10.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support10.syncAttribute_SyncChangesEverySeconds = 0.03333334f;
			support10.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
			support10.syncAttribute_ShouldBlendBetweenValuesReceived = true;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(2, out support10.syncAttribute_ShouldSkipSync);
			support10.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-125f, 125f, 18, true);

			cachedCustomSerializers[10] = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.Vector3Serializer>(18, -125f, 125f);
		
            int support10_mostRecentChanges_calcdSize = support10.syncAttribute_SyncChangesEverySeconds != 0 ? (int)((GONetMain.valueBlendingBufferLeadSeconds / support10.syncAttribute_SyncChangesEverySeconds) * 2.5f) : 0;
            support10.mostRecentChanges_capacitySize = Math.Max(support10_mostRecentChanges_calcdSize, GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.MOST_RECENT_CHANGEs_SIZE_MINIMUM);
			support10.mostRecentChanges = GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.mostRecentChangesPool.Borrow(support10.mostRecentChanges_capacitySize);

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
									DestroyIfMineOnKeyPress.willHeUpdate = value.System_Single;
									return;
				case 5:
									FieldChangeTest.color = value.UnityEngine_Vector3;
									return;
				case 6:
									FieldChangeTest.color_dosientos = value.UnityEngine_Vector3;
									return;
				case 7:
									FieldChangeTest.nada = value.System_Single;
									return;
				case 8:
									FieldChangeTest.shortie = value.System_Int16;
									return;
				case 9:
									Transform.rotation = value.UnityEngine_Quaternion;
									return;
				case 10:
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
									return DestroyIfMineOnKeyPress.willHeUpdate;
								case 5:
									return FieldChangeTest.color;
								case 6:
									return FieldChangeTest.color_dosientos;
								case 7:
									return FieldChangeTest.nada;
								case 8:
									return FieldChangeTest.shortie;
								case 9:
									return Transform.rotation;
								case 10:
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
			{ // GONetParticipant.IsPositionSyncd
								bitStream_appendTo.WriteBit(GONetParticipant.IsPositionSyncd);
							}
			{ // GONetParticipant.IsRotationSyncd
								bitStream_appendTo.WriteBit(GONetParticipant.IsRotationSyncd);
							}
			{ // GONetParticipant.OwnerAuthorityId
				bitStream_appendTo.WriteUShort(GONetParticipant.OwnerAuthorityId);
			}
			{ // DestroyIfMineOnKeyPress.willHeUpdate
								bitStream_appendTo.WriteFloat(DestroyIfMineOnKeyPress.willHeUpdate);
							}
			{ // FieldChangeTest.color
				IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[5];
					customSerializer.Serialize(bitStream_appendTo, gonetParticipant, FieldChangeTest.color);
			}
			{ // FieldChangeTest.color_dosientos
				IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[6];
					customSerializer.Serialize(bitStream_appendTo, gonetParticipant, FieldChangeTest.color_dosientos);
			}
			{ // FieldChangeTest.nada
				SerializeSingleQuantized(bitStream_appendTo, 7, FieldChangeTest.nada);
			}
			{ // FieldChangeTest.shortie
								byte[] bytes = BitConverter.GetBytes(FieldChangeTest.shortie);
								int count = bytes.Length;
				for (int i = 0; i < count; ++i)
				{
					bitStream_appendTo.WriteByte(bytes[i]);
				}
			}
			{ // Transform.rotation
				IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[9];
					customSerializer.Serialize(bitStream_appendTo, gonetParticipant, Transform.rotation);
			}
			{ // Transform.position
				IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[10];
					customSerializer.Serialize(bitStream_appendTo, gonetParticipant, Transform.position - valuesChangesSupport[10].baselineValue_current.UnityEngine_Vector3);
			}
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
				{ // DestroyIfMineOnKeyPress.willHeUpdate
									bitStream_appendTo.WriteFloat(DestroyIfMineOnKeyPress.willHeUpdate);
								}
				break;

				case 5:
				{ // FieldChangeTest.color
				    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[5];
					customSerializer.Serialize(bitStream_appendTo, gonetParticipant, FieldChangeTest.color);
				}
				break;

				case 6:
				{ // FieldChangeTest.color_dosientos
				    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[6];
					customSerializer.Serialize(bitStream_appendTo, gonetParticipant, FieldChangeTest.color_dosientos);
				}
				break;

				case 7:
				{ // FieldChangeTest.nada
					SerializeSingleQuantized(bitStream_appendTo, 7, FieldChangeTest.nada);
				}
				break;

				case 8:
				{ // FieldChangeTest.shortie
									byte[] bytes = BitConverter.GetBytes(FieldChangeTest.shortie);
									int count = bytes.Length;
					for (int i = 0; i < count; ++i)
					{
						bitStream_appendTo.WriteByte(bytes[i]);
					}
				}
				break;

				case 9:
				{ // Transform.rotation
				    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[9];
					customSerializer.Serialize(bitStream_appendTo, gonetParticipant, Transform.rotation);
				}
				break;

				case 10:
				{ // Transform.position
				    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[10];
					customSerializer.Serialize(bitStream_appendTo, gonetParticipant, Transform.position - valuesChangesSupport[10].baselineValue_current.UnityEngine_Vector3);
				}
				break;

			}
        }

        /// <summary>
        /// Deserializes all values from <paramref name="bitStream_readFrom"/> and uses them to modify appropriate member variables internally.
        /// Oops.  Just kidding....it's ALMOST all values.  The exception being <see cref="GONetParticipant.GONetId"/> because that has to be processed first separately in order
        /// to know which <see cref="GONetParticipant"/> we are working with in order to call this method.
        /// </summary>
        internal override void DeserializeInitAll(Utils.BitByBitByteArrayBuilder bitStream_readFrom, long assumedElapsedTicksAtChange)
        {
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
			{ // DestroyIfMineOnKeyPress.willHeUpdate
				float value;
                bitStream_readFrom.ReadFloat(out value);
								DestroyIfMineOnKeyPress.willHeUpdate = value;
							}
			{ // FieldChangeTest.color
				IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[5];
				FieldChangeTest.color = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Vector3;
			}
			{ // FieldChangeTest.color_dosientos
				IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[6];
				FieldChangeTest.color_dosientos = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Vector3;
			}
			{ // FieldChangeTest.nada
				float value;
				value = DeserializeSingleQuantized(bitStream_readFrom, 7).System_Single;
								FieldChangeTest.nada = value;
							}
			{ // FieldChangeTest.shortie
				int count = 2;
				byte[] bytes = GetMyValueDeserializeByteArray();
				for (int i = 0; i < count; ++i)
				{
					byte b = (byte)bitStream_readFrom.ReadByte();
					bytes[i] = b;
				}
				FieldChangeTest.shortie = BitConverter.ToInt16(bytes, 0);
			}
			{ // Transform.rotation
				IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[9];
				Transform.rotation = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Quaternion;
			}
			{ // Transform.position
				IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[10];
				Transform.position = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Vector3 + valuesChangesSupport[10].baselineValue_current.UnityEngine_Vector3;
			}
        }

        /// <summary>
        ///  Deserializes a single value (using <paramref name="singleIndex"/> to know which) from <paramref name="bitStream_readFrom"/>
        ///  and uses them to modify appropriate member variables internally.
        /// </summary>
        internal override void DeserializeInitSingle(Utils.BitByBitByteArrayBuilder bitStream_readFrom, byte singleIndex, long assumedElapsedTicksAtChange)
        {
			switch (singleIndex)
			{
				case 0:
				{ // GONetParticipant.GONetId
				    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[0];
					var value = customSerializer.Deserialize(bitStream_readFrom).System_UInt32;

									GONetParticipant.GONetId = value;
								}
				break;

				case 1:
				{ // GONetParticipant.IsPositionSyncd
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetParticipant.IsPositionSyncd = value;
								}
				break;

				case 2:
				{ // GONetParticipant.IsRotationSyncd
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetParticipant.IsRotationSyncd = value;
								}
				break;

				case 3:
				{ // GONetParticipant.OwnerAuthorityId
					ushort value;
					bitStream_readFrom.ReadUShort(out value);

									GONetParticipant.OwnerAuthorityId = value;
								}
				break;

				case 4:
				{ // DestroyIfMineOnKeyPress.willHeUpdate
					float value;
					bitStream_readFrom.ReadFloat(out value);

									DestroyIfMineOnKeyPress.willHeUpdate = value;
								}
				break;

				case 5:
				{ // FieldChangeTest.color
				    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[5];
					var value = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Vector3;

									FieldChangeTest.color = value;
								}
				break;

				case 6:
				{ // FieldChangeTest.color_dosientos
				    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[6];
					var value = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Vector3;

									FieldChangeTest.color_dosientos = value;
								}
				break;

				case 7:
				{ // FieldChangeTest.nada
					float value;
					value = DeserializeSingleQuantized(bitStream_readFrom, 7).System_Single;

					valuesChangesSupport[7].AddToMostRecentChangeQueue_IfAppropriate(assumedElapsedTicksAtChange, value); // NOTE: this queue will be used each frame to blend between this value and others added there
				}
				break;

				case 8:
				{ // FieldChangeTest.shortie
					int count = 2;
					byte[] bytes = GetMyValueDeserializeByteArray();
					for (int i = 0; i < count; ++i)
					{
						byte b = (byte)bitStream_readFrom.ReadByte();
						bytes[i] = b;
					}
					var value = BitConverter.ToInt16(bytes, 0);

									FieldChangeTest.shortie = value;
								}
				break;

				case 9:
				{ // Transform.rotation
				    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[9];
					var value = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Quaternion;

					valuesChangesSupport[9].AddToMostRecentChangeQueue_IfAppropriate(assumedElapsedTicksAtChange, value); // NOTE: this queue will be used each frame to blend between this value and others added there
				}
				break;

				case 10:
				{ // Transform.position
				    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[10];
					var value = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Vector3;
					value += valuesChangesSupport[10].baselineValue_current.UnityEngine_Vector3;

					valuesChangesSupport[10].AddToMostRecentChangeQueue_IfAppropriate(assumedElapsedTicksAtChange, value); // NOTE: this queue will be used each frame to blend between this value and others added there
				}
				break;

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
									valuesChangesSupport4.lastKnownValue.System_Single = DestroyIfMineOnKeyPress.willHeUpdate;
								}

				var valuesChangesSupport5 = valuesChangesSupport[5];
				if (DoesMatchUniqueGrouping(valuesChangesSupport5, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport5, 5)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport5.lastKnownValue_previous = valuesChangesSupport5.lastKnownValue;
									valuesChangesSupport5.lastKnownValue.UnityEngine_Vector3 = FieldChangeTest.color;
								}

				var valuesChangesSupport6 = valuesChangesSupport[6];
				if (DoesMatchUniqueGrouping(valuesChangesSupport6, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport6, 6)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport6.lastKnownValue_previous = valuesChangesSupport6.lastKnownValue;
									valuesChangesSupport6.lastKnownValue.UnityEngine_Vector3 = FieldChangeTest.color_dosientos;
								}

				var valuesChangesSupport7 = valuesChangesSupport[7];
				if (DoesMatchUniqueGrouping(valuesChangesSupport7, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport7, 7)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport7.lastKnownValue_previous = valuesChangesSupport7.lastKnownValue;
									valuesChangesSupport7.lastKnownValue.System_Single = FieldChangeTest.nada;
								}

				var valuesChangesSupport8 = valuesChangesSupport[8];
				if (DoesMatchUniqueGrouping(valuesChangesSupport8, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport8, 8)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport8.lastKnownValue_previous = valuesChangesSupport8.lastKnownValue;
									valuesChangesSupport8.lastKnownValue.System_Int16 = FieldChangeTest.shortie;
								}

				var valuesChangesSupport9 = valuesChangesSupport[9];
				if (DoesMatchUniqueGrouping(valuesChangesSupport9, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport9, 9)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport9.lastKnownValue_previous = valuesChangesSupport9.lastKnownValue;
									valuesChangesSupport9.lastKnownValue.UnityEngine_Quaternion = Transform.rotation;
								}

				var valuesChangesSupport10 = valuesChangesSupport[10];
				if (DoesMatchUniqueGrouping(valuesChangesSupport10, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport10, 10)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport10.lastKnownValue_previous = valuesChangesSupport10.lastKnownValue;
									valuesChangesSupport10.lastKnownValue.UnityEngine_Vector3 = Transform.position;
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
				{ // DestroyIfMineOnKeyPress.willHeUpdate
                    System.Single diff = valueChangeSupport.lastKnownValue.System_Single - valueChangeSupport.baselineValue_current.System_Single;
					System.Single componentLimitLower = valueChangeSupport.syncAttribute_QuantizerSettingsGroup.lowerBound * 0.8f; // TODO cache this value
					System.Single componentLimitUpper = valueChangeSupport.syncAttribute_QuantizerSettingsGroup.upperBound * 0.8f; // TODO cache this value
                    bool isVeryCloseTo_Or_AlreadyOutsideOf_QuantizationRange = diff < componentLimitLower || diff > componentLimitUpper;
					return isVeryCloseTo_Or_AlreadyOutsideOf_QuantizationRange;
				}
				break;

				case 5:
				{ // FieldChangeTest.color
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

				case 6:
				{ // FieldChangeTest.color_dosientos
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

				case 7:
				{ // FieldChangeTest.nada
                    System.Single diff = valueChangeSupport.lastKnownValue.System_Single - valueChangeSupport.baselineValue_current.System_Single;
					System.Single componentLimitLower = valueChangeSupport.syncAttribute_QuantizerSettingsGroup.lowerBound * 0.8f; // TODO cache this value
					System.Single componentLimitUpper = valueChangeSupport.syncAttribute_QuantizerSettingsGroup.upperBound * 0.8f; // TODO cache this value
                    bool isVeryCloseTo_Or_AlreadyOutsideOf_QuantizationRange = diff < componentLimitLower || diff > componentLimitUpper;
					return isVeryCloseTo_Or_AlreadyOutsideOf_QuantizationRange;
				}
				break;

				case 8:
				{ // FieldChangeTest.shortie
					// this type not supported for this functionality
				}
				break;

				case 9:
				{ // Transform.rotation
					// this type not supported for this functionality
				}
				break;

				case 10:
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
				{ // DestroyIfMineOnKeyPress.willHeUpdate
					return new ValueMonitoringSupport_NewBaselineEvent_System_Single() {
						GONetId = gonetId,
						ValueIndex = singleIndex,
						NewBaselineValue = newBaselineValue.System_Single
					};
				}

				case 5:
				{ // FieldChangeTest.color
					return new ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector3() {
						GONetId = gonetId,
						ValueIndex = singleIndex,
						NewBaselineValue = newBaselineValue.UnityEngine_Vector3
					};
				}

				case 6:
				{ // FieldChangeTest.color_dosientos
					return new ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector3() {
						GONetId = gonetId,
						ValueIndex = singleIndex,
						NewBaselineValue = newBaselineValue.UnityEngine_Vector3
					};
				}

				case 7:
				{ // FieldChangeTest.nada
					return new ValueMonitoringSupport_NewBaselineEvent_System_Single() {
						GONetId = gonetId,
						ValueIndex = singleIndex,
						NewBaselineValue = newBaselineValue.System_Single
					};
				}

				case 8:
				{ // FieldChangeTest.shortie
					return new ValueMonitoringSupport_NewBaselineEvent_System_Int16() {
						GONetId = gonetId,
						ValueIndex = singleIndex,
						NewBaselineValue = newBaselineValue.System_Int16
					};
				}

				case 9:
				{ // Transform.rotation
					return new ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Quaternion() {
						GONetId = gonetId,
						ValueIndex = singleIndex,
						NewBaselineValue = newBaselineValue.UnityEngine_Quaternion
					};
				}

				case 10:
				{ // Transform.position
					return new ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector3() {
						GONetId = gonetId,
						ValueIndex = singleIndex,
						NewBaselineValue = newBaselineValue.UnityEngine_Vector3
					};
				}

			}
			return null;
		}    }}