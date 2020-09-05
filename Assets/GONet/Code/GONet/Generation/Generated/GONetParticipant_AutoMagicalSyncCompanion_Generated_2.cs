


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
	internal sealed class GONetParticipant_AutoMagicalSyncCompanion_Generated_2 : GONetParticipant_AutoMagicalSyncCompanion_Generated
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

		private GONet.Sample.GONetSampleInputSync _GONetSampleInputSync;
		internal GONet.Sample.GONetSampleInputSync GONetSampleInputSync
		{
			get
			{
				if ((object)_GONetSampleInputSync == null)
				{
					_GONetSampleInputSync = gonetParticipant.GetComponent<GONet.Sample.GONetSampleInputSync>();
				}
				return _GONetSampleInputSync;
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


        internal override byte CodeGenerationId => 2;

        internal GONetParticipant_AutoMagicalSyncCompanion_Generated_2(GONetParticipant gonetParticipant) : base(gonetParticipant)
		{
			valuesCount = 14;
			
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
			support0.memberName = "GONetId";
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
			support1.memberName = "IsPositionSyncd";
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
			support2.memberName = "IsRotationSyncd";
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
			support3.memberName = "OwnerAuthorityId";
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
            support4.baselineValue_current.System_Boolean = GONetSampleInputSync.GetKey_A; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support4.lastKnownValue.System_Boolean = GONetSampleInputSync.GetKey_A; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support4.lastKnownValue_previous.System_Boolean = GONetSampleInputSync.GetKey_A; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support4.syncCompanion = this;
			support4.memberName = "GetKey_A";
			support4.index = 4;
			support4.syncAttribute_MustRunOnUnityMainThread = true;
			support4.syncAttribute_ProcessingPriority = 0;
			support4.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support4.syncAttribute_SyncChangesEverySeconds = 0f;
			support4.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support4.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support4.syncAttribute_ShouldSkipSync);
			support4.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);


			var support5 = valuesChangesSupport[5] = valueChangeSupportArrayPool.Borrow();
            support5.baselineValue_current.System_Boolean = GONetSampleInputSync.GetKey_D; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support5.lastKnownValue.System_Boolean = GONetSampleInputSync.GetKey_D; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support5.lastKnownValue_previous.System_Boolean = GONetSampleInputSync.GetKey_D; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support5.syncCompanion = this;
			support5.memberName = "GetKey_D";
			support5.index = 5;
			support5.syncAttribute_MustRunOnUnityMainThread = true;
			support5.syncAttribute_ProcessingPriority = 0;
			support5.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support5.syncAttribute_SyncChangesEverySeconds = 0f;
			support5.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support5.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support5.syncAttribute_ShouldSkipSync);
			support5.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);


			var support6 = valuesChangesSupport[6] = valueChangeSupportArrayPool.Borrow();
            support6.baselineValue_current.System_Boolean = GONetSampleInputSync.GetKey_DownArrow; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support6.lastKnownValue.System_Boolean = GONetSampleInputSync.GetKey_DownArrow; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support6.lastKnownValue_previous.System_Boolean = GONetSampleInputSync.GetKey_DownArrow; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support6.syncCompanion = this;
			support6.memberName = "GetKey_DownArrow";
			support6.index = 6;
			support6.syncAttribute_MustRunOnUnityMainThread = true;
			support6.syncAttribute_ProcessingPriority = 0;
			support6.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support6.syncAttribute_SyncChangesEverySeconds = 0f;
			support6.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support6.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support6.syncAttribute_ShouldSkipSync);
			support6.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);


			var support7 = valuesChangesSupport[7] = valueChangeSupportArrayPool.Borrow();
            support7.baselineValue_current.System_Boolean = GONetSampleInputSync.GetKey_LeftArrow; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support7.lastKnownValue.System_Boolean = GONetSampleInputSync.GetKey_LeftArrow; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support7.lastKnownValue_previous.System_Boolean = GONetSampleInputSync.GetKey_LeftArrow; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support7.syncCompanion = this;
			support7.memberName = "GetKey_LeftArrow";
			support7.index = 7;
			support7.syncAttribute_MustRunOnUnityMainThread = true;
			support7.syncAttribute_ProcessingPriority = 0;
			support7.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support7.syncAttribute_SyncChangesEverySeconds = 0f;
			support7.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support7.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support7.syncAttribute_ShouldSkipSync);
			support7.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);


			var support8 = valuesChangesSupport[8] = valueChangeSupportArrayPool.Borrow();
            support8.baselineValue_current.System_Boolean = GONetSampleInputSync.GetKey_RightArrow; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support8.lastKnownValue.System_Boolean = GONetSampleInputSync.GetKey_RightArrow; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support8.lastKnownValue_previous.System_Boolean = GONetSampleInputSync.GetKey_RightArrow; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support8.syncCompanion = this;
			support8.memberName = "GetKey_RightArrow";
			support8.index = 8;
			support8.syncAttribute_MustRunOnUnityMainThread = true;
			support8.syncAttribute_ProcessingPriority = 0;
			support8.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support8.syncAttribute_SyncChangesEverySeconds = 0f;
			support8.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support8.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support8.syncAttribute_ShouldSkipSync);
			support8.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);


			var support9 = valuesChangesSupport[9] = valueChangeSupportArrayPool.Borrow();
            support9.baselineValue_current.System_Boolean = GONetSampleInputSync.GetKey_S; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support9.lastKnownValue.System_Boolean = GONetSampleInputSync.GetKey_S; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support9.lastKnownValue_previous.System_Boolean = GONetSampleInputSync.GetKey_S; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support9.syncCompanion = this;
			support9.memberName = "GetKey_S";
			support9.index = 9;
			support9.syncAttribute_MustRunOnUnityMainThread = true;
			support9.syncAttribute_ProcessingPriority = 0;
			support9.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support9.syncAttribute_SyncChangesEverySeconds = 0f;
			support9.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support9.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support9.syncAttribute_ShouldSkipSync);
			support9.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);


			var support10 = valuesChangesSupport[10] = valueChangeSupportArrayPool.Borrow();
            support10.baselineValue_current.System_Boolean = GONetSampleInputSync.GetKey_UpArrow; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support10.lastKnownValue.System_Boolean = GONetSampleInputSync.GetKey_UpArrow; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support10.lastKnownValue_previous.System_Boolean = GONetSampleInputSync.GetKey_UpArrow; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support10.syncCompanion = this;
			support10.memberName = "GetKey_UpArrow";
			support10.index = 10;
			support10.syncAttribute_MustRunOnUnityMainThread = true;
			support10.syncAttribute_ProcessingPriority = 0;
			support10.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support10.syncAttribute_SyncChangesEverySeconds = 0f;
			support10.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support10.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support10.syncAttribute_ShouldSkipSync);
			support10.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);


			var support11 = valuesChangesSupport[11] = valueChangeSupportArrayPool.Borrow();
            support11.baselineValue_current.System_Boolean = GONetSampleInputSync.GetKey_W; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support11.lastKnownValue.System_Boolean = GONetSampleInputSync.GetKey_W; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support11.lastKnownValue_previous.System_Boolean = GONetSampleInputSync.GetKey_W; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support11.syncCompanion = this;
			support11.memberName = "GetKey_W";
			support11.index = 11;
			support11.syncAttribute_MustRunOnUnityMainThread = true;
			support11.syncAttribute_ProcessingPriority = 0;
			support11.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support11.syncAttribute_SyncChangesEverySeconds = 0f;
			support11.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support11.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support11.syncAttribute_ShouldSkipSync);
			support11.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);


			var support12 = valuesChangesSupport[12] = valueChangeSupportArrayPool.Borrow();
            support12.baselineValue_current.UnityEngine_Quaternion = Transform.rotation; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support12.lastKnownValue.UnityEngine_Quaternion = Transform.rotation; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support12.lastKnownValue_previous.UnityEngine_Quaternion = Transform.rotation; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support12.syncCompanion = this;
			support12.memberName = "rotation";
			support12.index = 12;
			support12.syncAttribute_MustRunOnUnityMainThread = true;
			support12.syncAttribute_ProcessingPriority = 0;
			support12.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support12.syncAttribute_SyncChangesEverySeconds = 0.03333334f;
			support12.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
			support12.syncAttribute_ShouldBlendBetweenValuesReceived = true;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(1, out support12.syncAttribute_ShouldSkipSync);
			support12.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			cachedCustomSerializers[12] = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.QuaternionSerializer>(0, -1.701412E+38f, 1.701412E+38f);
		
            int support12_mostRecentChanges_calcdSize = support12.syncAttribute_SyncChangesEverySeconds != 0 ? (int)((GONetMain.valueBlendingBufferLeadSeconds / support12.syncAttribute_SyncChangesEverySeconds) * 2.5f) : 0;
            support12.mostRecentChanges_capacitySize = Math.Max(support12_mostRecentChanges_calcdSize, GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.MOST_RECENT_CHANGEs_SIZE_MINIMUM);
			support12.mostRecentChanges = GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.mostRecentChangesPool.Borrow(support12.mostRecentChanges_capacitySize);

			var support13 = valuesChangesSupport[13] = valueChangeSupportArrayPool.Borrow();
            support13.baselineValue_current.UnityEngine_Vector3 = Transform.position; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support13.lastKnownValue.UnityEngine_Vector3 = Transform.position; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support13.lastKnownValue_previous.UnityEngine_Vector3 = Transform.position; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
			support13.syncCompanion = this;
			support13.memberName = "position";
			support13.index = 13;
			support13.syncAttribute_MustRunOnUnityMainThread = true;
			support13.syncAttribute_ProcessingPriority = 0;
			support13.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support13.syncAttribute_SyncChangesEverySeconds = 0.03333334f;
			support13.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
			support13.syncAttribute_ShouldBlendBetweenValuesReceived = true;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(2, out support13.syncAttribute_ShouldSkipSync);
			support13.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-125f, 125f, 18, true);

			cachedCustomSerializers[13] = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.Vector3Serializer>(18, -125f, 125f);
		
            int support13_mostRecentChanges_calcdSize = support13.syncAttribute_SyncChangesEverySeconds != 0 ? (int)((GONetMain.valueBlendingBufferLeadSeconds / support13.syncAttribute_SyncChangesEverySeconds) * 2.5f) : 0;
            support13.mostRecentChanges_capacitySize = Math.Max(support13_mostRecentChanges_calcdSize, GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.MOST_RECENT_CHANGEs_SIZE_MINIMUM);
			support13.mostRecentChanges = GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.mostRecentChangesPool.Borrow(support13.mostRecentChanges_capacitySize);

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
									GONetSampleInputSync.GetKey_A = value.System_Boolean;
									return;
				case 5:
									GONetSampleInputSync.GetKey_D = value.System_Boolean;
									return;
				case 6:
									GONetSampleInputSync.GetKey_DownArrow = value.System_Boolean;
									return;
				case 7:
									GONetSampleInputSync.GetKey_LeftArrow = value.System_Boolean;
									return;
				case 8:
									GONetSampleInputSync.GetKey_RightArrow = value.System_Boolean;
									return;
				case 9:
									GONetSampleInputSync.GetKey_S = value.System_Boolean;
									return;
				case 10:
									GONetSampleInputSync.GetKey_UpArrow = value.System_Boolean;
									return;
				case 11:
									GONetSampleInputSync.GetKey_W = value.System_Boolean;
									return;
				case 12:
									Transform.rotation = value.UnityEngine_Quaternion;
									return;
				case 13:
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
									return GONetSampleInputSync.GetKey_A;
								case 5:
									return GONetSampleInputSync.GetKey_D;
								case 6:
									return GONetSampleInputSync.GetKey_DownArrow;
								case 7:
									return GONetSampleInputSync.GetKey_LeftArrow;
								case 8:
									return GONetSampleInputSync.GetKey_RightArrow;
								case 9:
									return GONetSampleInputSync.GetKey_S;
								case 10:
									return GONetSampleInputSync.GetKey_UpArrow;
								case 11:
									return GONetSampleInputSync.GetKey_W;
								case 12:
									return Transform.rotation;
								case 13:
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
			{ // GONetSampleInputSync.GetKey_A
								bitStream_appendTo.WriteBit(GONetSampleInputSync.GetKey_A);
							}
			{ // GONetSampleInputSync.GetKey_D
								bitStream_appendTo.WriteBit(GONetSampleInputSync.GetKey_D);
							}
			{ // GONetSampleInputSync.GetKey_DownArrow
								bitStream_appendTo.WriteBit(GONetSampleInputSync.GetKey_DownArrow);
							}
			{ // GONetSampleInputSync.GetKey_LeftArrow
								bitStream_appendTo.WriteBit(GONetSampleInputSync.GetKey_LeftArrow);
							}
			{ // GONetSampleInputSync.GetKey_RightArrow
								bitStream_appendTo.WriteBit(GONetSampleInputSync.GetKey_RightArrow);
							}
			{ // GONetSampleInputSync.GetKey_S
								bitStream_appendTo.WriteBit(GONetSampleInputSync.GetKey_S);
							}
			{ // GONetSampleInputSync.GetKey_UpArrow
								bitStream_appendTo.WriteBit(GONetSampleInputSync.GetKey_UpArrow);
							}
			{ // GONetSampleInputSync.GetKey_W
								bitStream_appendTo.WriteBit(GONetSampleInputSync.GetKey_W);
							}
			{ // Transform.rotation
				IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[12];
					customSerializer.Serialize(bitStream_appendTo, gonetParticipant, Transform.rotation);
			}
			{ // Transform.position
				IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[13];
					customSerializer.Serialize(bitStream_appendTo, gonetParticipant, Transform.position - valuesChangesSupport[13].baselineValue_current.UnityEngine_Vector3);
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
				{ // GONetSampleInputSync.GetKey_A
									bitStream_appendTo.WriteBit(GONetSampleInputSync.GetKey_A);
								}
				break;

				case 5:
				{ // GONetSampleInputSync.GetKey_D
									bitStream_appendTo.WriteBit(GONetSampleInputSync.GetKey_D);
								}
				break;

				case 6:
				{ // GONetSampleInputSync.GetKey_DownArrow
									bitStream_appendTo.WriteBit(GONetSampleInputSync.GetKey_DownArrow);
								}
				break;

				case 7:
				{ // GONetSampleInputSync.GetKey_LeftArrow
									bitStream_appendTo.WriteBit(GONetSampleInputSync.GetKey_LeftArrow);
								}
				break;

				case 8:
				{ // GONetSampleInputSync.GetKey_RightArrow
									bitStream_appendTo.WriteBit(GONetSampleInputSync.GetKey_RightArrow);
								}
				break;

				case 9:
				{ // GONetSampleInputSync.GetKey_S
									bitStream_appendTo.WriteBit(GONetSampleInputSync.GetKey_S);
								}
				break;

				case 10:
				{ // GONetSampleInputSync.GetKey_UpArrow
									bitStream_appendTo.WriteBit(GONetSampleInputSync.GetKey_UpArrow);
								}
				break;

				case 11:
				{ // GONetSampleInputSync.GetKey_W
									bitStream_appendTo.WriteBit(GONetSampleInputSync.GetKey_W);
								}
				break;

				case 12:
				{ // Transform.rotation
				    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[12];
					customSerializer.Serialize(bitStream_appendTo, gonetParticipant, Transform.rotation);
				}
				break;

				case 13:
				{ // Transform.position
				    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[13];
					customSerializer.Serialize(bitStream_appendTo, gonetParticipant, Transform.position - valuesChangesSupport[13].baselineValue_current.UnityEngine_Vector3);
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
				{ // GONetSampleInputSync.GetKey_A
					return valueA.System_Boolean == valueB.System_Boolean;
				}
				break;

				case 5:
				{ // GONetSampleInputSync.GetKey_D
					return valueA.System_Boolean == valueB.System_Boolean;
				}
				break;

				case 6:
				{ // GONetSampleInputSync.GetKey_DownArrow
					return valueA.System_Boolean == valueB.System_Boolean;
				}
				break;

				case 7:
				{ // GONetSampleInputSync.GetKey_LeftArrow
					return valueA.System_Boolean == valueB.System_Boolean;
				}
				break;

				case 8:
				{ // GONetSampleInputSync.GetKey_RightArrow
					return valueA.System_Boolean == valueB.System_Boolean;
				}
				break;

				case 9:
				{ // GONetSampleInputSync.GetKey_S
					return valueA.System_Boolean == valueB.System_Boolean;
				}
				break;

				case 10:
				{ // GONetSampleInputSync.GetKey_UpArrow
					return valueA.System_Boolean == valueB.System_Boolean;
				}
				break;

				case 11:
				{ // GONetSampleInputSync.GetKey_W
					return valueA.System_Boolean == valueB.System_Boolean;
				}
				break;

				case 12:
				{ // Transform.rotation
				    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[12];
					return customSerializer.AreEqualConsideringQuantization(valueA, valueB);
				}
				break;

				case 13:
				{ // Transform.position
				    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[13];
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
			{ // GONetSampleInputSync.GetKey_A
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetSampleInputSync.GetKey_A = value;
							}
			{ // GONetSampleInputSync.GetKey_D
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetSampleInputSync.GetKey_D = value;
							}
			{ // GONetSampleInputSync.GetKey_DownArrow
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetSampleInputSync.GetKey_DownArrow = value;
							}
			{ // GONetSampleInputSync.GetKey_LeftArrow
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetSampleInputSync.GetKey_LeftArrow = value;
							}
			{ // GONetSampleInputSync.GetKey_RightArrow
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetSampleInputSync.GetKey_RightArrow = value;
							}
			{ // GONetSampleInputSync.GetKey_S
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetSampleInputSync.GetKey_S = value;
							}
			{ // GONetSampleInputSync.GetKey_UpArrow
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetSampleInputSync.GetKey_UpArrow = value;
							}
			{ // GONetSampleInputSync.GetKey_W
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetSampleInputSync.GetKey_W = value;
							}
			{ // Transform.rotation
				IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[12];
				Transform.rotation = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Quaternion;
			}
			{ // Transform.position
				IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[13];
				Transform.position = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Vector3 + valuesChangesSupport[13].baselineValue_current.UnityEngine_Vector3;
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
				{ // GONetSampleInputSync.GetKey_A
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetSampleInputSync.GetKey_A = value;
								}
				break;

				case 5:
				{ // GONetSampleInputSync.GetKey_D
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetSampleInputSync.GetKey_D = value;
								}
				break;

				case 6:
				{ // GONetSampleInputSync.GetKey_DownArrow
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetSampleInputSync.GetKey_DownArrow = value;
								}
				break;

				case 7:
				{ // GONetSampleInputSync.GetKey_LeftArrow
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetSampleInputSync.GetKey_LeftArrow = value;
								}
				break;

				case 8:
				{ // GONetSampleInputSync.GetKey_RightArrow
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetSampleInputSync.GetKey_RightArrow = value;
								}
				break;

				case 9:
				{ // GONetSampleInputSync.GetKey_S
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetSampleInputSync.GetKey_S = value;
								}
				break;

				case 10:
				{ // GONetSampleInputSync.GetKey_UpArrow
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetSampleInputSync.GetKey_UpArrow = value;
								}
				break;

				case 11:
				{ // GONetSampleInputSync.GetKey_W
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetSampleInputSync.GetKey_W = value;
								}
				break;

				case 12:
				{ // Transform.rotation
				    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[12];
					var value = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Quaternion;

					valuesChangesSupport[12].AddToMostRecentChangeQueue_IfAppropriate(assumedElapsedTicksAtChange, value); // NOTE: this queue will be used each frame to blend between this value and others added there
				}
				break;

				case 13:
				{ // Transform.position
				    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[13];
					var value = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Vector3;
					value += valuesChangesSupport[13].baselineValue_current.UnityEngine_Vector3;

					valuesChangesSupport[13].AddToMostRecentChangeQueue_IfAppropriate(assumedElapsedTicksAtChange, value); // NOTE: this queue will be used each frame to blend between this value and others added there
				}
				break;

			}
        }


        /// <summary>
        /// Simply deserializes in order to move along the bit stream counter, but does NOT apply the values (i.e, does NOT init).
        /// </summary>
        internal override void DeserializeInitSingle_ReadOnlyNotApply(Utils.BitByBitByteArrayBuilder bitStream_readFrom, byte singleIndex)
        {
			switch (singleIndex)
			{
				case 0:
				{ // GONetParticipant.GONetId
				    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[0];
					customSerializer.Deserialize(bitStream_readFrom);
				}
				break;

				case 1:
				{ // GONetParticipant.IsPositionSyncd
					bool value;
					bitStream_readFrom.ReadBit(out value);
				}
				break;

				case 2:
				{ // GONetParticipant.IsRotationSyncd
					bool value;
					bitStream_readFrom.ReadBit(out value);
				}
				break;

				case 3:
				{ // GONetParticipant.OwnerAuthorityId
					ushort value;
					bitStream_readFrom.ReadUShort(out value);
				}
				break;

				case 4:
				{ // GONetSampleInputSync.GetKey_A
					bool value;
					bitStream_readFrom.ReadBit(out value);
				}
				break;

				case 5:
				{ // GONetSampleInputSync.GetKey_D
					bool value;
					bitStream_readFrom.ReadBit(out value);
				}
				break;

				case 6:
				{ // GONetSampleInputSync.GetKey_DownArrow
					bool value;
					bitStream_readFrom.ReadBit(out value);
				}
				break;

				case 7:
				{ // GONetSampleInputSync.GetKey_LeftArrow
					bool value;
					bitStream_readFrom.ReadBit(out value);
				}
				break;

				case 8:
				{ // GONetSampleInputSync.GetKey_RightArrow
					bool value;
					bitStream_readFrom.ReadBit(out value);
				}
				break;

				case 9:
				{ // GONetSampleInputSync.GetKey_S
					bool value;
					bitStream_readFrom.ReadBit(out value);
				}
				break;

				case 10:
				{ // GONetSampleInputSync.GetKey_UpArrow
					bool value;
					bitStream_readFrom.ReadBit(out value);
				}
				break;

				case 11:
				{ // GONetSampleInputSync.GetKey_W
					bool value;
					bitStream_readFrom.ReadBit(out value);
				}
				break;

				case 12:
				{ // Transform.rotation
				    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[12];
					customSerializer.Deserialize(bitStream_readFrom);
				}
				break;

				case 13:
				{ // Transform.position
				    IGONetAutoMagicalSync_CustomSerializer customSerializer = cachedCustomSerializers[13];
					customSerializer.Deserialize(bitStream_readFrom);
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
									valuesChangesSupport4.lastKnownValue.System_Boolean = GONetSampleInputSync.GetKey_A;
								}

				var valuesChangesSupport5 = valuesChangesSupport[5];
				if (DoesMatchUniqueGrouping(valuesChangesSupport5, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport5, 5)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport5.lastKnownValue_previous = valuesChangesSupport5.lastKnownValue;
									valuesChangesSupport5.lastKnownValue.System_Boolean = GONetSampleInputSync.GetKey_D;
								}

				var valuesChangesSupport6 = valuesChangesSupport[6];
				if (DoesMatchUniqueGrouping(valuesChangesSupport6, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport6, 6)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport6.lastKnownValue_previous = valuesChangesSupport6.lastKnownValue;
									valuesChangesSupport6.lastKnownValue.System_Boolean = GONetSampleInputSync.GetKey_DownArrow;
								}

				var valuesChangesSupport7 = valuesChangesSupport[7];
				if (DoesMatchUniqueGrouping(valuesChangesSupport7, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport7, 7)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport7.lastKnownValue_previous = valuesChangesSupport7.lastKnownValue;
									valuesChangesSupport7.lastKnownValue.System_Boolean = GONetSampleInputSync.GetKey_LeftArrow;
								}

				var valuesChangesSupport8 = valuesChangesSupport[8];
				if (DoesMatchUniqueGrouping(valuesChangesSupport8, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport8, 8)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport8.lastKnownValue_previous = valuesChangesSupport8.lastKnownValue;
									valuesChangesSupport8.lastKnownValue.System_Boolean = GONetSampleInputSync.GetKey_RightArrow;
								}

				var valuesChangesSupport9 = valuesChangesSupport[9];
				if (DoesMatchUniqueGrouping(valuesChangesSupport9, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport9, 9)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport9.lastKnownValue_previous = valuesChangesSupport9.lastKnownValue;
									valuesChangesSupport9.lastKnownValue.System_Boolean = GONetSampleInputSync.GetKey_S;
								}

				var valuesChangesSupport10 = valuesChangesSupport[10];
				if (DoesMatchUniqueGrouping(valuesChangesSupport10, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport10, 10)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport10.lastKnownValue_previous = valuesChangesSupport10.lastKnownValue;
									valuesChangesSupport10.lastKnownValue.System_Boolean = GONetSampleInputSync.GetKey_UpArrow;
								}

				var valuesChangesSupport11 = valuesChangesSupport[11];
				if (DoesMatchUniqueGrouping(valuesChangesSupport11, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport11, 11)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport11.lastKnownValue_previous = valuesChangesSupport11.lastKnownValue;
									valuesChangesSupport11.lastKnownValue.System_Boolean = GONetSampleInputSync.GetKey_W;
								}

				var valuesChangesSupport12 = valuesChangesSupport[12];
				if (DoesMatchUniqueGrouping(valuesChangesSupport12, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport12, 12)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport12.lastKnownValue_previous = valuesChangesSupport12.lastKnownValue;
									valuesChangesSupport12.lastKnownValue.UnityEngine_Quaternion = Transform.rotation;
								}

				var valuesChangesSupport13 = valuesChangesSupport[13];
				if (DoesMatchUniqueGrouping(valuesChangesSupport13, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport13, 13)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport13.lastKnownValue_previous = valuesChangesSupport13.lastKnownValue;
									valuesChangesSupport13.lastKnownValue.UnityEngine_Vector3 = Transform.position;
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
				{ // GONetSampleInputSync.GetKey_A
					// this type not supported for this functionality
				}
				break;

				case 5:
				{ // GONetSampleInputSync.GetKey_D
					// this type not supported for this functionality
				}
				break;

				case 6:
				{ // GONetSampleInputSync.GetKey_DownArrow
					// this type not supported for this functionality
				}
				break;

				case 7:
				{ // GONetSampleInputSync.GetKey_LeftArrow
					// this type not supported for this functionality
				}
				break;

				case 8:
				{ // GONetSampleInputSync.GetKey_RightArrow
					// this type not supported for this functionality
				}
				break;

				case 9:
				{ // GONetSampleInputSync.GetKey_S
					// this type not supported for this functionality
				}
				break;

				case 10:
				{ // GONetSampleInputSync.GetKey_UpArrow
					// this type not supported for this functionality
				}
				break;

				case 11:
				{ // GONetSampleInputSync.GetKey_W
					// this type not supported for this functionality
				}
				break;

				case 12:
				{ // Transform.rotation
					// this type not supported for this functionality
				}
				break;

				case 13:
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
				{ // GONetSampleInputSync.GetKey_A
					return new ValueMonitoringSupport_NewBaselineEvent_System_Boolean() {
						GONetId = gonetId,
						ValueIndex = singleIndex,
						NewBaselineValue = newBaselineValue.System_Boolean
					};
				}

				case 5:
				{ // GONetSampleInputSync.GetKey_D
					return new ValueMonitoringSupport_NewBaselineEvent_System_Boolean() {
						GONetId = gonetId,
						ValueIndex = singleIndex,
						NewBaselineValue = newBaselineValue.System_Boolean
					};
				}

				case 6:
				{ // GONetSampleInputSync.GetKey_DownArrow
					return new ValueMonitoringSupport_NewBaselineEvent_System_Boolean() {
						GONetId = gonetId,
						ValueIndex = singleIndex,
						NewBaselineValue = newBaselineValue.System_Boolean
					};
				}

				case 7:
				{ // GONetSampleInputSync.GetKey_LeftArrow
					return new ValueMonitoringSupport_NewBaselineEvent_System_Boolean() {
						GONetId = gonetId,
						ValueIndex = singleIndex,
						NewBaselineValue = newBaselineValue.System_Boolean
					};
				}

				case 8:
				{ // GONetSampleInputSync.GetKey_RightArrow
					return new ValueMonitoringSupport_NewBaselineEvent_System_Boolean() {
						GONetId = gonetId,
						ValueIndex = singleIndex,
						NewBaselineValue = newBaselineValue.System_Boolean
					};
				}

				case 9:
				{ // GONetSampleInputSync.GetKey_S
					return new ValueMonitoringSupport_NewBaselineEvent_System_Boolean() {
						GONetId = gonetId,
						ValueIndex = singleIndex,
						NewBaselineValue = newBaselineValue.System_Boolean
					};
				}

				case 10:
				{ // GONetSampleInputSync.GetKey_UpArrow
					return new ValueMonitoringSupport_NewBaselineEvent_System_Boolean() {
						GONetId = gonetId,
						ValueIndex = singleIndex,
						NewBaselineValue = newBaselineValue.System_Boolean
					};
				}

				case 11:
				{ // GONetSampleInputSync.GetKey_W
					return new ValueMonitoringSupport_NewBaselineEvent_System_Boolean() {
						GONetId = gonetId,
						ValueIndex = singleIndex,
						NewBaselineValue = newBaselineValue.System_Boolean
					};
				}

				case 12:
				{ // Transform.rotation
					return new ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Quaternion() {
						GONetId = gonetId,
						ValueIndex = singleIndex,
						NewBaselineValue = newBaselineValue.UnityEngine_Quaternion
					};
				}

				case 13:
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