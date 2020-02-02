


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

		private GONet.Sample.GONetInputSync _GONetInputSync;
		internal GONet.Sample.GONetInputSync GONetInputSync
		{
			get
			{
				if ((object)_GONetInputSync == null)
				{
					_GONetInputSync = gonetParticipant.GetComponent<GONet.Sample.GONetInputSync>();
				}
				return _GONetInputSync;
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
			valuesCount = 173;
		    
			lastKnownValueChangesSinceLastCheck = lastKnownValuesChangedArrayPool.Borrow((int)valuesCount);
			Array.Clear(lastKnownValueChangesSinceLastCheck, 0, lastKnownValueChangesSinceLastCheck.Length);

			valuesChangesSupport = valuesChangesSupportArrayPool.Borrow((int)valuesCount);
			
			var support0 = valuesChangesSupport[0] = valueChangeSupportArrayPool.Borrow();
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

			var support1 = valuesChangesSupport[1] = valueChangeSupportArrayPool.Borrow();
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
		            support4.lastKnownValue.System_Boolean = GONetInputSync.GetKey_A; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support4.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_A; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support4.syncCompanion = this;
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
		            support5.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Alpha0; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support5.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Alpha0; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support5.syncCompanion = this;
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
		            support6.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Alpha1; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support6.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Alpha1; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support6.syncCompanion = this;
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
		            support7.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Alpha2; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support7.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Alpha2; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support7.syncCompanion = this;
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
		            support8.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Alpha3; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support8.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Alpha3; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support8.syncCompanion = this;
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
		            support9.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Alpha4; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support9.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Alpha4; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support9.syncCompanion = this;
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
		            support10.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Alpha5; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support10.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Alpha5; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support10.syncCompanion = this;
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
		            support11.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Alpha6; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support11.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Alpha6; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support11.syncCompanion = this;
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
		            support12.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Alpha7; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support12.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Alpha7; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support12.syncCompanion = this;
			support12.index = 12;
			support12.syncAttribute_MustRunOnUnityMainThread = true;
			support12.syncAttribute_ProcessingPriority = 0;
			support12.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support12.syncAttribute_SyncChangesEverySeconds = 0f;
			support12.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support12.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support12.syncAttribute_ShouldSkipSync);
			support12.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support13 = valuesChangesSupport[13] = valueChangeSupportArrayPool.Borrow();
		            support13.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Alpha8; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support13.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Alpha8; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support13.syncCompanion = this;
			support13.index = 13;
			support13.syncAttribute_MustRunOnUnityMainThread = true;
			support13.syncAttribute_ProcessingPriority = 0;
			support13.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support13.syncAttribute_SyncChangesEverySeconds = 0f;
			support13.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support13.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support13.syncAttribute_ShouldSkipSync);
			support13.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support14 = valuesChangesSupport[14] = valueChangeSupportArrayPool.Borrow();
		            support14.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Alpha9; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support14.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Alpha9; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support14.syncCompanion = this;
			support14.index = 14;
			support14.syncAttribute_MustRunOnUnityMainThread = true;
			support14.syncAttribute_ProcessingPriority = 0;
			support14.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support14.syncAttribute_SyncChangesEverySeconds = 0f;
			support14.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support14.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support14.syncAttribute_ShouldSkipSync);
			support14.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support15 = valuesChangesSupport[15] = valueChangeSupportArrayPool.Borrow();
		            support15.lastKnownValue.System_Boolean = GONetInputSync.GetKey_AltGr; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support15.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_AltGr; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support15.syncCompanion = this;
			support15.index = 15;
			support15.syncAttribute_MustRunOnUnityMainThread = true;
			support15.syncAttribute_ProcessingPriority = 0;
			support15.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support15.syncAttribute_SyncChangesEverySeconds = 0f;
			support15.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support15.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support15.syncAttribute_ShouldSkipSync);
			support15.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support16 = valuesChangesSupport[16] = valueChangeSupportArrayPool.Borrow();
		            support16.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Ampersand; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support16.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Ampersand; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support16.syncCompanion = this;
			support16.index = 16;
			support16.syncAttribute_MustRunOnUnityMainThread = true;
			support16.syncAttribute_ProcessingPriority = 0;
			support16.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support16.syncAttribute_SyncChangesEverySeconds = 0f;
			support16.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support16.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support16.syncAttribute_ShouldSkipSync);
			support16.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support17 = valuesChangesSupport[17] = valueChangeSupportArrayPool.Borrow();
		            support17.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Asterisk; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support17.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Asterisk; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support17.syncCompanion = this;
			support17.index = 17;
			support17.syncAttribute_MustRunOnUnityMainThread = true;
			support17.syncAttribute_ProcessingPriority = 0;
			support17.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support17.syncAttribute_SyncChangesEverySeconds = 0f;
			support17.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support17.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support17.syncAttribute_ShouldSkipSync);
			support17.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support18 = valuesChangesSupport[18] = valueChangeSupportArrayPool.Borrow();
		            support18.lastKnownValue.System_Boolean = GONetInputSync.GetKey_At; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support18.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_At; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support18.syncCompanion = this;
			support18.index = 18;
			support18.syncAttribute_MustRunOnUnityMainThread = true;
			support18.syncAttribute_ProcessingPriority = 0;
			support18.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support18.syncAttribute_SyncChangesEverySeconds = 0f;
			support18.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support18.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support18.syncAttribute_ShouldSkipSync);
			support18.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support19 = valuesChangesSupport[19] = valueChangeSupportArrayPool.Borrow();
		            support19.lastKnownValue.System_Boolean = GONetInputSync.GetKey_B; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support19.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_B; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support19.syncCompanion = this;
			support19.index = 19;
			support19.syncAttribute_MustRunOnUnityMainThread = true;
			support19.syncAttribute_ProcessingPriority = 0;
			support19.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support19.syncAttribute_SyncChangesEverySeconds = 0f;
			support19.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support19.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support19.syncAttribute_ShouldSkipSync);
			support19.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support20 = valuesChangesSupport[20] = valueChangeSupportArrayPool.Borrow();
		            support20.lastKnownValue.System_Boolean = GONetInputSync.GetKey_BackQuote; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support20.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_BackQuote; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support20.syncCompanion = this;
			support20.index = 20;
			support20.syncAttribute_MustRunOnUnityMainThread = true;
			support20.syncAttribute_ProcessingPriority = 0;
			support20.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support20.syncAttribute_SyncChangesEverySeconds = 0f;
			support20.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support20.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support20.syncAttribute_ShouldSkipSync);
			support20.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support21 = valuesChangesSupport[21] = valueChangeSupportArrayPool.Borrow();
		            support21.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Backslash; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support21.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Backslash; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support21.syncCompanion = this;
			support21.index = 21;
			support21.syncAttribute_MustRunOnUnityMainThread = true;
			support21.syncAttribute_ProcessingPriority = 0;
			support21.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support21.syncAttribute_SyncChangesEverySeconds = 0f;
			support21.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support21.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support21.syncAttribute_ShouldSkipSync);
			support21.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support22 = valuesChangesSupport[22] = valueChangeSupportArrayPool.Borrow();
		            support22.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Backspace; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support22.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Backspace; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support22.syncCompanion = this;
			support22.index = 22;
			support22.syncAttribute_MustRunOnUnityMainThread = true;
			support22.syncAttribute_ProcessingPriority = 0;
			support22.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support22.syncAttribute_SyncChangesEverySeconds = 0f;
			support22.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support22.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support22.syncAttribute_ShouldSkipSync);
			support22.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support23 = valuesChangesSupport[23] = valueChangeSupportArrayPool.Borrow();
		            support23.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Break; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support23.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Break; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support23.syncCompanion = this;
			support23.index = 23;
			support23.syncAttribute_MustRunOnUnityMainThread = true;
			support23.syncAttribute_ProcessingPriority = 0;
			support23.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support23.syncAttribute_SyncChangesEverySeconds = 0f;
			support23.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support23.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support23.syncAttribute_ShouldSkipSync);
			support23.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support24 = valuesChangesSupport[24] = valueChangeSupportArrayPool.Borrow();
		            support24.lastKnownValue.System_Boolean = GONetInputSync.GetKey_C; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support24.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_C; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support24.syncCompanion = this;
			support24.index = 24;
			support24.syncAttribute_MustRunOnUnityMainThread = true;
			support24.syncAttribute_ProcessingPriority = 0;
			support24.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support24.syncAttribute_SyncChangesEverySeconds = 0f;
			support24.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support24.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support24.syncAttribute_ShouldSkipSync);
			support24.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support25 = valuesChangesSupport[25] = valueChangeSupportArrayPool.Borrow();
		            support25.lastKnownValue.System_Boolean = GONetInputSync.GetKey_CapsLock; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support25.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_CapsLock; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support25.syncCompanion = this;
			support25.index = 25;
			support25.syncAttribute_MustRunOnUnityMainThread = true;
			support25.syncAttribute_ProcessingPriority = 0;
			support25.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support25.syncAttribute_SyncChangesEverySeconds = 0f;
			support25.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support25.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support25.syncAttribute_ShouldSkipSync);
			support25.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support26 = valuesChangesSupport[26] = valueChangeSupportArrayPool.Borrow();
		            support26.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Caret; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support26.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Caret; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support26.syncCompanion = this;
			support26.index = 26;
			support26.syncAttribute_MustRunOnUnityMainThread = true;
			support26.syncAttribute_ProcessingPriority = 0;
			support26.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support26.syncAttribute_SyncChangesEverySeconds = 0f;
			support26.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support26.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support26.syncAttribute_ShouldSkipSync);
			support26.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support27 = valuesChangesSupport[27] = valueChangeSupportArrayPool.Borrow();
		            support27.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Clear; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support27.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Clear; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support27.syncCompanion = this;
			support27.index = 27;
			support27.syncAttribute_MustRunOnUnityMainThread = true;
			support27.syncAttribute_ProcessingPriority = 0;
			support27.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support27.syncAttribute_SyncChangesEverySeconds = 0f;
			support27.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support27.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support27.syncAttribute_ShouldSkipSync);
			support27.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support28 = valuesChangesSupport[28] = valueChangeSupportArrayPool.Borrow();
		            support28.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Colon; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support28.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Colon; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support28.syncCompanion = this;
			support28.index = 28;
			support28.syncAttribute_MustRunOnUnityMainThread = true;
			support28.syncAttribute_ProcessingPriority = 0;
			support28.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support28.syncAttribute_SyncChangesEverySeconds = 0f;
			support28.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support28.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support28.syncAttribute_ShouldSkipSync);
			support28.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support29 = valuesChangesSupport[29] = valueChangeSupportArrayPool.Borrow();
		            support29.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Comma; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support29.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Comma; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support29.syncCompanion = this;
			support29.index = 29;
			support29.syncAttribute_MustRunOnUnityMainThread = true;
			support29.syncAttribute_ProcessingPriority = 0;
			support29.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support29.syncAttribute_SyncChangesEverySeconds = 0f;
			support29.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support29.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support29.syncAttribute_ShouldSkipSync);
			support29.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support30 = valuesChangesSupport[30] = valueChangeSupportArrayPool.Borrow();
		            support30.lastKnownValue.System_Boolean = GONetInputSync.GetKey_D; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support30.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_D; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support30.syncCompanion = this;
			support30.index = 30;
			support30.syncAttribute_MustRunOnUnityMainThread = true;
			support30.syncAttribute_ProcessingPriority = 0;
			support30.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support30.syncAttribute_SyncChangesEverySeconds = 0f;
			support30.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support30.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support30.syncAttribute_ShouldSkipSync);
			support30.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support31 = valuesChangesSupport[31] = valueChangeSupportArrayPool.Borrow();
		            support31.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Delete; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support31.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Delete; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support31.syncCompanion = this;
			support31.index = 31;
			support31.syncAttribute_MustRunOnUnityMainThread = true;
			support31.syncAttribute_ProcessingPriority = 0;
			support31.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support31.syncAttribute_SyncChangesEverySeconds = 0f;
			support31.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support31.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support31.syncAttribute_ShouldSkipSync);
			support31.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support32 = valuesChangesSupport[32] = valueChangeSupportArrayPool.Borrow();
		            support32.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Dollar; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support32.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Dollar; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support32.syncCompanion = this;
			support32.index = 32;
			support32.syncAttribute_MustRunOnUnityMainThread = true;
			support32.syncAttribute_ProcessingPriority = 0;
			support32.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support32.syncAttribute_SyncChangesEverySeconds = 0f;
			support32.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support32.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support32.syncAttribute_ShouldSkipSync);
			support32.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support33 = valuesChangesSupport[33] = valueChangeSupportArrayPool.Borrow();
		            support33.lastKnownValue.System_Boolean = GONetInputSync.GetKey_DoubleQuote; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support33.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_DoubleQuote; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support33.syncCompanion = this;
			support33.index = 33;
			support33.syncAttribute_MustRunOnUnityMainThread = true;
			support33.syncAttribute_ProcessingPriority = 0;
			support33.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support33.syncAttribute_SyncChangesEverySeconds = 0f;
			support33.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support33.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support33.syncAttribute_ShouldSkipSync);
			support33.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support34 = valuesChangesSupport[34] = valueChangeSupportArrayPool.Borrow();
		            support34.lastKnownValue.System_Boolean = GONetInputSync.GetKey_DownArrow; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support34.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_DownArrow; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support34.syncCompanion = this;
			support34.index = 34;
			support34.syncAttribute_MustRunOnUnityMainThread = true;
			support34.syncAttribute_ProcessingPriority = 0;
			support34.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support34.syncAttribute_SyncChangesEverySeconds = 0f;
			support34.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support34.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support34.syncAttribute_ShouldSkipSync);
			support34.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support35 = valuesChangesSupport[35] = valueChangeSupportArrayPool.Borrow();
		            support35.lastKnownValue.System_Boolean = GONetInputSync.GetKey_E; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support35.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_E; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support35.syncCompanion = this;
			support35.index = 35;
			support35.syncAttribute_MustRunOnUnityMainThread = true;
			support35.syncAttribute_ProcessingPriority = 0;
			support35.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support35.syncAttribute_SyncChangesEverySeconds = 0f;
			support35.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support35.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support35.syncAttribute_ShouldSkipSync);
			support35.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support36 = valuesChangesSupport[36] = valueChangeSupportArrayPool.Borrow();
		            support36.lastKnownValue.System_Boolean = GONetInputSync.GetKey_End; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support36.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_End; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support36.syncCompanion = this;
			support36.index = 36;
			support36.syncAttribute_MustRunOnUnityMainThread = true;
			support36.syncAttribute_ProcessingPriority = 0;
			support36.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support36.syncAttribute_SyncChangesEverySeconds = 0f;
			support36.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support36.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support36.syncAttribute_ShouldSkipSync);
			support36.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support37 = valuesChangesSupport[37] = valueChangeSupportArrayPool.Borrow();
		            support37.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Equals; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support37.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Equals; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support37.syncCompanion = this;
			support37.index = 37;
			support37.syncAttribute_MustRunOnUnityMainThread = true;
			support37.syncAttribute_ProcessingPriority = 0;
			support37.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support37.syncAttribute_SyncChangesEverySeconds = 0f;
			support37.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support37.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support37.syncAttribute_ShouldSkipSync);
			support37.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support38 = valuesChangesSupport[38] = valueChangeSupportArrayPool.Borrow();
		            support38.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Escape; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support38.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Escape; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support38.syncCompanion = this;
			support38.index = 38;
			support38.syncAttribute_MustRunOnUnityMainThread = true;
			support38.syncAttribute_ProcessingPriority = 0;
			support38.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support38.syncAttribute_SyncChangesEverySeconds = 0f;
			support38.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support38.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support38.syncAttribute_ShouldSkipSync);
			support38.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support39 = valuesChangesSupport[39] = valueChangeSupportArrayPool.Borrow();
		            support39.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Exclaim; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support39.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Exclaim; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support39.syncCompanion = this;
			support39.index = 39;
			support39.syncAttribute_MustRunOnUnityMainThread = true;
			support39.syncAttribute_ProcessingPriority = 0;
			support39.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support39.syncAttribute_SyncChangesEverySeconds = 0f;
			support39.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support39.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support39.syncAttribute_ShouldSkipSync);
			support39.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support40 = valuesChangesSupport[40] = valueChangeSupportArrayPool.Borrow();
		            support40.lastKnownValue.System_Boolean = GONetInputSync.GetKey_F; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support40.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_F; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support40.syncCompanion = this;
			support40.index = 40;
			support40.syncAttribute_MustRunOnUnityMainThread = true;
			support40.syncAttribute_ProcessingPriority = 0;
			support40.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support40.syncAttribute_SyncChangesEverySeconds = 0f;
			support40.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support40.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support40.syncAttribute_ShouldSkipSync);
			support40.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support41 = valuesChangesSupport[41] = valueChangeSupportArrayPool.Borrow();
		            support41.lastKnownValue.System_Boolean = GONetInputSync.GetKey_F1; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support41.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_F1; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support41.syncCompanion = this;
			support41.index = 41;
			support41.syncAttribute_MustRunOnUnityMainThread = true;
			support41.syncAttribute_ProcessingPriority = 0;
			support41.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support41.syncAttribute_SyncChangesEverySeconds = 0f;
			support41.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support41.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support41.syncAttribute_ShouldSkipSync);
			support41.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support42 = valuesChangesSupport[42] = valueChangeSupportArrayPool.Borrow();
		            support42.lastKnownValue.System_Boolean = GONetInputSync.GetKey_F10; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support42.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_F10; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support42.syncCompanion = this;
			support42.index = 42;
			support42.syncAttribute_MustRunOnUnityMainThread = true;
			support42.syncAttribute_ProcessingPriority = 0;
			support42.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support42.syncAttribute_SyncChangesEverySeconds = 0f;
			support42.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support42.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support42.syncAttribute_ShouldSkipSync);
			support42.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support43 = valuesChangesSupport[43] = valueChangeSupportArrayPool.Borrow();
		            support43.lastKnownValue.System_Boolean = GONetInputSync.GetKey_F11; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support43.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_F11; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support43.syncCompanion = this;
			support43.index = 43;
			support43.syncAttribute_MustRunOnUnityMainThread = true;
			support43.syncAttribute_ProcessingPriority = 0;
			support43.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support43.syncAttribute_SyncChangesEverySeconds = 0f;
			support43.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support43.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support43.syncAttribute_ShouldSkipSync);
			support43.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support44 = valuesChangesSupport[44] = valueChangeSupportArrayPool.Borrow();
		            support44.lastKnownValue.System_Boolean = GONetInputSync.GetKey_F12; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support44.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_F12; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support44.syncCompanion = this;
			support44.index = 44;
			support44.syncAttribute_MustRunOnUnityMainThread = true;
			support44.syncAttribute_ProcessingPriority = 0;
			support44.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support44.syncAttribute_SyncChangesEverySeconds = 0f;
			support44.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support44.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support44.syncAttribute_ShouldSkipSync);
			support44.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support45 = valuesChangesSupport[45] = valueChangeSupportArrayPool.Borrow();
		            support45.lastKnownValue.System_Boolean = GONetInputSync.GetKey_F2; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support45.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_F2; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support45.syncCompanion = this;
			support45.index = 45;
			support45.syncAttribute_MustRunOnUnityMainThread = true;
			support45.syncAttribute_ProcessingPriority = 0;
			support45.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support45.syncAttribute_SyncChangesEverySeconds = 0f;
			support45.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support45.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support45.syncAttribute_ShouldSkipSync);
			support45.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support46 = valuesChangesSupport[46] = valueChangeSupportArrayPool.Borrow();
		            support46.lastKnownValue.System_Boolean = GONetInputSync.GetKey_F3; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support46.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_F3; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support46.syncCompanion = this;
			support46.index = 46;
			support46.syncAttribute_MustRunOnUnityMainThread = true;
			support46.syncAttribute_ProcessingPriority = 0;
			support46.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support46.syncAttribute_SyncChangesEverySeconds = 0f;
			support46.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support46.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support46.syncAttribute_ShouldSkipSync);
			support46.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support47 = valuesChangesSupport[47] = valueChangeSupportArrayPool.Borrow();
		            support47.lastKnownValue.System_Boolean = GONetInputSync.GetKey_F4; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support47.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_F4; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support47.syncCompanion = this;
			support47.index = 47;
			support47.syncAttribute_MustRunOnUnityMainThread = true;
			support47.syncAttribute_ProcessingPriority = 0;
			support47.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support47.syncAttribute_SyncChangesEverySeconds = 0f;
			support47.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support47.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support47.syncAttribute_ShouldSkipSync);
			support47.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support48 = valuesChangesSupport[48] = valueChangeSupportArrayPool.Borrow();
		            support48.lastKnownValue.System_Boolean = GONetInputSync.GetKey_F5; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support48.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_F5; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support48.syncCompanion = this;
			support48.index = 48;
			support48.syncAttribute_MustRunOnUnityMainThread = true;
			support48.syncAttribute_ProcessingPriority = 0;
			support48.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support48.syncAttribute_SyncChangesEverySeconds = 0f;
			support48.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support48.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support48.syncAttribute_ShouldSkipSync);
			support48.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support49 = valuesChangesSupport[49] = valueChangeSupportArrayPool.Borrow();
		            support49.lastKnownValue.System_Boolean = GONetInputSync.GetKey_F6; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support49.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_F6; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support49.syncCompanion = this;
			support49.index = 49;
			support49.syncAttribute_MustRunOnUnityMainThread = true;
			support49.syncAttribute_ProcessingPriority = 0;
			support49.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support49.syncAttribute_SyncChangesEverySeconds = 0f;
			support49.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support49.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support49.syncAttribute_ShouldSkipSync);
			support49.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support50 = valuesChangesSupport[50] = valueChangeSupportArrayPool.Borrow();
		            support50.lastKnownValue.System_Boolean = GONetInputSync.GetKey_F7; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support50.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_F7; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support50.syncCompanion = this;
			support50.index = 50;
			support50.syncAttribute_MustRunOnUnityMainThread = true;
			support50.syncAttribute_ProcessingPriority = 0;
			support50.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support50.syncAttribute_SyncChangesEverySeconds = 0f;
			support50.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support50.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support50.syncAttribute_ShouldSkipSync);
			support50.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support51 = valuesChangesSupport[51] = valueChangeSupportArrayPool.Borrow();
		            support51.lastKnownValue.System_Boolean = GONetInputSync.GetKey_F8; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support51.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_F8; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support51.syncCompanion = this;
			support51.index = 51;
			support51.syncAttribute_MustRunOnUnityMainThread = true;
			support51.syncAttribute_ProcessingPriority = 0;
			support51.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support51.syncAttribute_SyncChangesEverySeconds = 0f;
			support51.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support51.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support51.syncAttribute_ShouldSkipSync);
			support51.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support52 = valuesChangesSupport[52] = valueChangeSupportArrayPool.Borrow();
		            support52.lastKnownValue.System_Boolean = GONetInputSync.GetKey_F9; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support52.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_F9; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support52.syncCompanion = this;
			support52.index = 52;
			support52.syncAttribute_MustRunOnUnityMainThread = true;
			support52.syncAttribute_ProcessingPriority = 0;
			support52.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support52.syncAttribute_SyncChangesEverySeconds = 0f;
			support52.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support52.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support52.syncAttribute_ShouldSkipSync);
			support52.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support53 = valuesChangesSupport[53] = valueChangeSupportArrayPool.Borrow();
		            support53.lastKnownValue.System_Boolean = GONetInputSync.GetKey_G; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support53.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_G; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support53.syncCompanion = this;
			support53.index = 53;
			support53.syncAttribute_MustRunOnUnityMainThread = true;
			support53.syncAttribute_ProcessingPriority = 0;
			support53.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support53.syncAttribute_SyncChangesEverySeconds = 0f;
			support53.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support53.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support53.syncAttribute_ShouldSkipSync);
			support53.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support54 = valuesChangesSupport[54] = valueChangeSupportArrayPool.Borrow();
		            support54.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Greater; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support54.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Greater; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support54.syncCompanion = this;
			support54.index = 54;
			support54.syncAttribute_MustRunOnUnityMainThread = true;
			support54.syncAttribute_ProcessingPriority = 0;
			support54.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support54.syncAttribute_SyncChangesEverySeconds = 0f;
			support54.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support54.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support54.syncAttribute_ShouldSkipSync);
			support54.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support55 = valuesChangesSupport[55] = valueChangeSupportArrayPool.Borrow();
		            support55.lastKnownValue.System_Boolean = GONetInputSync.GetKey_H; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support55.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_H; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support55.syncCompanion = this;
			support55.index = 55;
			support55.syncAttribute_MustRunOnUnityMainThread = true;
			support55.syncAttribute_ProcessingPriority = 0;
			support55.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support55.syncAttribute_SyncChangesEverySeconds = 0f;
			support55.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support55.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support55.syncAttribute_ShouldSkipSync);
			support55.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support56 = valuesChangesSupport[56] = valueChangeSupportArrayPool.Borrow();
		            support56.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Hash; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support56.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Hash; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support56.syncCompanion = this;
			support56.index = 56;
			support56.syncAttribute_MustRunOnUnityMainThread = true;
			support56.syncAttribute_ProcessingPriority = 0;
			support56.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support56.syncAttribute_SyncChangesEverySeconds = 0f;
			support56.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support56.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support56.syncAttribute_ShouldSkipSync);
			support56.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support57 = valuesChangesSupport[57] = valueChangeSupportArrayPool.Borrow();
		            support57.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Help; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support57.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Help; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support57.syncCompanion = this;
			support57.index = 57;
			support57.syncAttribute_MustRunOnUnityMainThread = true;
			support57.syncAttribute_ProcessingPriority = 0;
			support57.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support57.syncAttribute_SyncChangesEverySeconds = 0f;
			support57.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support57.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support57.syncAttribute_ShouldSkipSync);
			support57.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support58 = valuesChangesSupport[58] = valueChangeSupportArrayPool.Borrow();
		            support58.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Home; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support58.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Home; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support58.syncCompanion = this;
			support58.index = 58;
			support58.syncAttribute_MustRunOnUnityMainThread = true;
			support58.syncAttribute_ProcessingPriority = 0;
			support58.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support58.syncAttribute_SyncChangesEverySeconds = 0f;
			support58.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support58.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support58.syncAttribute_ShouldSkipSync);
			support58.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support59 = valuesChangesSupport[59] = valueChangeSupportArrayPool.Borrow();
		            support59.lastKnownValue.System_Boolean = GONetInputSync.GetKey_I; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support59.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_I; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support59.syncCompanion = this;
			support59.index = 59;
			support59.syncAttribute_MustRunOnUnityMainThread = true;
			support59.syncAttribute_ProcessingPriority = 0;
			support59.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support59.syncAttribute_SyncChangesEverySeconds = 0f;
			support59.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support59.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support59.syncAttribute_ShouldSkipSync);
			support59.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support60 = valuesChangesSupport[60] = valueChangeSupportArrayPool.Borrow();
		            support60.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Insert; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support60.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Insert; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support60.syncCompanion = this;
			support60.index = 60;
			support60.syncAttribute_MustRunOnUnityMainThread = true;
			support60.syncAttribute_ProcessingPriority = 0;
			support60.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support60.syncAttribute_SyncChangesEverySeconds = 0f;
			support60.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support60.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support60.syncAttribute_ShouldSkipSync);
			support60.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support61 = valuesChangesSupport[61] = valueChangeSupportArrayPool.Borrow();
		            support61.lastKnownValue.System_Boolean = GONetInputSync.GetKey_J; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support61.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_J; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support61.syncCompanion = this;
			support61.index = 61;
			support61.syncAttribute_MustRunOnUnityMainThread = true;
			support61.syncAttribute_ProcessingPriority = 0;
			support61.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support61.syncAttribute_SyncChangesEverySeconds = 0f;
			support61.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support61.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support61.syncAttribute_ShouldSkipSync);
			support61.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support62 = valuesChangesSupport[62] = valueChangeSupportArrayPool.Borrow();
		            support62.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton0; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support62.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_JoystickButton0; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support62.syncCompanion = this;
			support62.index = 62;
			support62.syncAttribute_MustRunOnUnityMainThread = true;
			support62.syncAttribute_ProcessingPriority = 0;
			support62.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support62.syncAttribute_SyncChangesEverySeconds = 0f;
			support62.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support62.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support62.syncAttribute_ShouldSkipSync);
			support62.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support63 = valuesChangesSupport[63] = valueChangeSupportArrayPool.Borrow();
		            support63.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton1; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support63.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_JoystickButton1; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support63.syncCompanion = this;
			support63.index = 63;
			support63.syncAttribute_MustRunOnUnityMainThread = true;
			support63.syncAttribute_ProcessingPriority = 0;
			support63.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support63.syncAttribute_SyncChangesEverySeconds = 0f;
			support63.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support63.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support63.syncAttribute_ShouldSkipSync);
			support63.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support64 = valuesChangesSupport[64] = valueChangeSupportArrayPool.Borrow();
		            support64.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton10; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support64.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_JoystickButton10; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support64.syncCompanion = this;
			support64.index = 64;
			support64.syncAttribute_MustRunOnUnityMainThread = true;
			support64.syncAttribute_ProcessingPriority = 0;
			support64.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support64.syncAttribute_SyncChangesEverySeconds = 0f;
			support64.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support64.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support64.syncAttribute_ShouldSkipSync);
			support64.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support65 = valuesChangesSupport[65] = valueChangeSupportArrayPool.Borrow();
		            support65.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton11; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support65.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_JoystickButton11; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support65.syncCompanion = this;
			support65.index = 65;
			support65.syncAttribute_MustRunOnUnityMainThread = true;
			support65.syncAttribute_ProcessingPriority = 0;
			support65.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support65.syncAttribute_SyncChangesEverySeconds = 0f;
			support65.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support65.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support65.syncAttribute_ShouldSkipSync);
			support65.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support66 = valuesChangesSupport[66] = valueChangeSupportArrayPool.Borrow();
		            support66.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton12; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support66.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_JoystickButton12; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support66.syncCompanion = this;
			support66.index = 66;
			support66.syncAttribute_MustRunOnUnityMainThread = true;
			support66.syncAttribute_ProcessingPriority = 0;
			support66.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support66.syncAttribute_SyncChangesEverySeconds = 0f;
			support66.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support66.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support66.syncAttribute_ShouldSkipSync);
			support66.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support67 = valuesChangesSupport[67] = valueChangeSupportArrayPool.Borrow();
		            support67.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton13; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support67.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_JoystickButton13; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support67.syncCompanion = this;
			support67.index = 67;
			support67.syncAttribute_MustRunOnUnityMainThread = true;
			support67.syncAttribute_ProcessingPriority = 0;
			support67.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support67.syncAttribute_SyncChangesEverySeconds = 0f;
			support67.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support67.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support67.syncAttribute_ShouldSkipSync);
			support67.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support68 = valuesChangesSupport[68] = valueChangeSupportArrayPool.Borrow();
		            support68.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton14; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support68.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_JoystickButton14; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support68.syncCompanion = this;
			support68.index = 68;
			support68.syncAttribute_MustRunOnUnityMainThread = true;
			support68.syncAttribute_ProcessingPriority = 0;
			support68.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support68.syncAttribute_SyncChangesEverySeconds = 0f;
			support68.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support68.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support68.syncAttribute_ShouldSkipSync);
			support68.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support69 = valuesChangesSupport[69] = valueChangeSupportArrayPool.Borrow();
		            support69.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton15; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support69.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_JoystickButton15; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support69.syncCompanion = this;
			support69.index = 69;
			support69.syncAttribute_MustRunOnUnityMainThread = true;
			support69.syncAttribute_ProcessingPriority = 0;
			support69.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support69.syncAttribute_SyncChangesEverySeconds = 0f;
			support69.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support69.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support69.syncAttribute_ShouldSkipSync);
			support69.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support70 = valuesChangesSupport[70] = valueChangeSupportArrayPool.Borrow();
		            support70.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton16; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support70.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_JoystickButton16; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support70.syncCompanion = this;
			support70.index = 70;
			support70.syncAttribute_MustRunOnUnityMainThread = true;
			support70.syncAttribute_ProcessingPriority = 0;
			support70.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support70.syncAttribute_SyncChangesEverySeconds = 0f;
			support70.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support70.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support70.syncAttribute_ShouldSkipSync);
			support70.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support71 = valuesChangesSupport[71] = valueChangeSupportArrayPool.Borrow();
		            support71.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton17; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support71.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_JoystickButton17; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support71.syncCompanion = this;
			support71.index = 71;
			support71.syncAttribute_MustRunOnUnityMainThread = true;
			support71.syncAttribute_ProcessingPriority = 0;
			support71.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support71.syncAttribute_SyncChangesEverySeconds = 0f;
			support71.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support71.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support71.syncAttribute_ShouldSkipSync);
			support71.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support72 = valuesChangesSupport[72] = valueChangeSupportArrayPool.Borrow();
		            support72.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton18; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support72.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_JoystickButton18; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support72.syncCompanion = this;
			support72.index = 72;
			support72.syncAttribute_MustRunOnUnityMainThread = true;
			support72.syncAttribute_ProcessingPriority = 0;
			support72.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support72.syncAttribute_SyncChangesEverySeconds = 0f;
			support72.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support72.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support72.syncAttribute_ShouldSkipSync);
			support72.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support73 = valuesChangesSupport[73] = valueChangeSupportArrayPool.Borrow();
		            support73.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton19; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support73.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_JoystickButton19; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support73.syncCompanion = this;
			support73.index = 73;
			support73.syncAttribute_MustRunOnUnityMainThread = true;
			support73.syncAttribute_ProcessingPriority = 0;
			support73.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support73.syncAttribute_SyncChangesEverySeconds = 0f;
			support73.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support73.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support73.syncAttribute_ShouldSkipSync);
			support73.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support74 = valuesChangesSupport[74] = valueChangeSupportArrayPool.Borrow();
		            support74.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton2; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support74.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_JoystickButton2; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support74.syncCompanion = this;
			support74.index = 74;
			support74.syncAttribute_MustRunOnUnityMainThread = true;
			support74.syncAttribute_ProcessingPriority = 0;
			support74.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support74.syncAttribute_SyncChangesEverySeconds = 0f;
			support74.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support74.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support74.syncAttribute_ShouldSkipSync);
			support74.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support75 = valuesChangesSupport[75] = valueChangeSupportArrayPool.Borrow();
		            support75.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton3; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support75.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_JoystickButton3; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support75.syncCompanion = this;
			support75.index = 75;
			support75.syncAttribute_MustRunOnUnityMainThread = true;
			support75.syncAttribute_ProcessingPriority = 0;
			support75.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support75.syncAttribute_SyncChangesEverySeconds = 0f;
			support75.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support75.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support75.syncAttribute_ShouldSkipSync);
			support75.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support76 = valuesChangesSupport[76] = valueChangeSupportArrayPool.Borrow();
		            support76.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton4; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support76.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_JoystickButton4; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support76.syncCompanion = this;
			support76.index = 76;
			support76.syncAttribute_MustRunOnUnityMainThread = true;
			support76.syncAttribute_ProcessingPriority = 0;
			support76.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support76.syncAttribute_SyncChangesEverySeconds = 0f;
			support76.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support76.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support76.syncAttribute_ShouldSkipSync);
			support76.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support77 = valuesChangesSupport[77] = valueChangeSupportArrayPool.Borrow();
		            support77.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton5; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support77.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_JoystickButton5; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support77.syncCompanion = this;
			support77.index = 77;
			support77.syncAttribute_MustRunOnUnityMainThread = true;
			support77.syncAttribute_ProcessingPriority = 0;
			support77.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support77.syncAttribute_SyncChangesEverySeconds = 0f;
			support77.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support77.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support77.syncAttribute_ShouldSkipSync);
			support77.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support78 = valuesChangesSupport[78] = valueChangeSupportArrayPool.Borrow();
		            support78.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton6; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support78.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_JoystickButton6; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support78.syncCompanion = this;
			support78.index = 78;
			support78.syncAttribute_MustRunOnUnityMainThread = true;
			support78.syncAttribute_ProcessingPriority = 0;
			support78.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support78.syncAttribute_SyncChangesEverySeconds = 0f;
			support78.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support78.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support78.syncAttribute_ShouldSkipSync);
			support78.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support79 = valuesChangesSupport[79] = valueChangeSupportArrayPool.Borrow();
		            support79.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton7; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support79.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_JoystickButton7; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support79.syncCompanion = this;
			support79.index = 79;
			support79.syncAttribute_MustRunOnUnityMainThread = true;
			support79.syncAttribute_ProcessingPriority = 0;
			support79.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support79.syncAttribute_SyncChangesEverySeconds = 0f;
			support79.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support79.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support79.syncAttribute_ShouldSkipSync);
			support79.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support80 = valuesChangesSupport[80] = valueChangeSupportArrayPool.Borrow();
		            support80.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton8; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support80.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_JoystickButton8; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support80.syncCompanion = this;
			support80.index = 80;
			support80.syncAttribute_MustRunOnUnityMainThread = true;
			support80.syncAttribute_ProcessingPriority = 0;
			support80.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support80.syncAttribute_SyncChangesEverySeconds = 0f;
			support80.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support80.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support80.syncAttribute_ShouldSkipSync);
			support80.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support81 = valuesChangesSupport[81] = valueChangeSupportArrayPool.Borrow();
		            support81.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton9; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support81.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_JoystickButton9; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support81.syncCompanion = this;
			support81.index = 81;
			support81.syncAttribute_MustRunOnUnityMainThread = true;
			support81.syncAttribute_ProcessingPriority = 0;
			support81.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support81.syncAttribute_SyncChangesEverySeconds = 0f;
			support81.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support81.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support81.syncAttribute_ShouldSkipSync);
			support81.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support82 = valuesChangesSupport[82] = valueChangeSupportArrayPool.Borrow();
		            support82.lastKnownValue.System_Boolean = GONetInputSync.GetKey_K; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support82.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_K; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support82.syncCompanion = this;
			support82.index = 82;
			support82.syncAttribute_MustRunOnUnityMainThread = true;
			support82.syncAttribute_ProcessingPriority = 0;
			support82.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support82.syncAttribute_SyncChangesEverySeconds = 0f;
			support82.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support82.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support82.syncAttribute_ShouldSkipSync);
			support82.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support83 = valuesChangesSupport[83] = valueChangeSupportArrayPool.Borrow();
		            support83.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Keypad0; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support83.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Keypad0; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support83.syncCompanion = this;
			support83.index = 83;
			support83.syncAttribute_MustRunOnUnityMainThread = true;
			support83.syncAttribute_ProcessingPriority = 0;
			support83.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support83.syncAttribute_SyncChangesEverySeconds = 0f;
			support83.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support83.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support83.syncAttribute_ShouldSkipSync);
			support83.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support84 = valuesChangesSupport[84] = valueChangeSupportArrayPool.Borrow();
		            support84.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Keypad1; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support84.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Keypad1; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support84.syncCompanion = this;
			support84.index = 84;
			support84.syncAttribute_MustRunOnUnityMainThread = true;
			support84.syncAttribute_ProcessingPriority = 0;
			support84.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support84.syncAttribute_SyncChangesEverySeconds = 0f;
			support84.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support84.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support84.syncAttribute_ShouldSkipSync);
			support84.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support85 = valuesChangesSupport[85] = valueChangeSupportArrayPool.Borrow();
		            support85.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Keypad2; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support85.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Keypad2; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support85.syncCompanion = this;
			support85.index = 85;
			support85.syncAttribute_MustRunOnUnityMainThread = true;
			support85.syncAttribute_ProcessingPriority = 0;
			support85.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support85.syncAttribute_SyncChangesEverySeconds = 0f;
			support85.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support85.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support85.syncAttribute_ShouldSkipSync);
			support85.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support86 = valuesChangesSupport[86] = valueChangeSupportArrayPool.Borrow();
		            support86.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Keypad3; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support86.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Keypad3; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support86.syncCompanion = this;
			support86.index = 86;
			support86.syncAttribute_MustRunOnUnityMainThread = true;
			support86.syncAttribute_ProcessingPriority = 0;
			support86.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support86.syncAttribute_SyncChangesEverySeconds = 0f;
			support86.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support86.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support86.syncAttribute_ShouldSkipSync);
			support86.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support87 = valuesChangesSupport[87] = valueChangeSupportArrayPool.Borrow();
		            support87.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Keypad4; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support87.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Keypad4; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support87.syncCompanion = this;
			support87.index = 87;
			support87.syncAttribute_MustRunOnUnityMainThread = true;
			support87.syncAttribute_ProcessingPriority = 0;
			support87.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support87.syncAttribute_SyncChangesEverySeconds = 0f;
			support87.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support87.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support87.syncAttribute_ShouldSkipSync);
			support87.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support88 = valuesChangesSupport[88] = valueChangeSupportArrayPool.Borrow();
		            support88.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Keypad5; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support88.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Keypad5; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support88.syncCompanion = this;
			support88.index = 88;
			support88.syncAttribute_MustRunOnUnityMainThread = true;
			support88.syncAttribute_ProcessingPriority = 0;
			support88.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support88.syncAttribute_SyncChangesEverySeconds = 0f;
			support88.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support88.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support88.syncAttribute_ShouldSkipSync);
			support88.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support89 = valuesChangesSupport[89] = valueChangeSupportArrayPool.Borrow();
		            support89.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Keypad6; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support89.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Keypad6; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support89.syncCompanion = this;
			support89.index = 89;
			support89.syncAttribute_MustRunOnUnityMainThread = true;
			support89.syncAttribute_ProcessingPriority = 0;
			support89.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support89.syncAttribute_SyncChangesEverySeconds = 0f;
			support89.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support89.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support89.syncAttribute_ShouldSkipSync);
			support89.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support90 = valuesChangesSupport[90] = valueChangeSupportArrayPool.Borrow();
		            support90.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Keypad7; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support90.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Keypad7; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support90.syncCompanion = this;
			support90.index = 90;
			support90.syncAttribute_MustRunOnUnityMainThread = true;
			support90.syncAttribute_ProcessingPriority = 0;
			support90.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support90.syncAttribute_SyncChangesEverySeconds = 0f;
			support90.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support90.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support90.syncAttribute_ShouldSkipSync);
			support90.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support91 = valuesChangesSupport[91] = valueChangeSupportArrayPool.Borrow();
		            support91.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Keypad8; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support91.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Keypad8; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support91.syncCompanion = this;
			support91.index = 91;
			support91.syncAttribute_MustRunOnUnityMainThread = true;
			support91.syncAttribute_ProcessingPriority = 0;
			support91.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support91.syncAttribute_SyncChangesEverySeconds = 0f;
			support91.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support91.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support91.syncAttribute_ShouldSkipSync);
			support91.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support92 = valuesChangesSupport[92] = valueChangeSupportArrayPool.Borrow();
		            support92.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Keypad9; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support92.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Keypad9; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support92.syncCompanion = this;
			support92.index = 92;
			support92.syncAttribute_MustRunOnUnityMainThread = true;
			support92.syncAttribute_ProcessingPriority = 0;
			support92.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support92.syncAttribute_SyncChangesEverySeconds = 0f;
			support92.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support92.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support92.syncAttribute_ShouldSkipSync);
			support92.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support93 = valuesChangesSupport[93] = valueChangeSupportArrayPool.Borrow();
		            support93.lastKnownValue.System_Boolean = GONetInputSync.GetKey_KeypadDivide; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support93.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_KeypadDivide; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support93.syncCompanion = this;
			support93.index = 93;
			support93.syncAttribute_MustRunOnUnityMainThread = true;
			support93.syncAttribute_ProcessingPriority = 0;
			support93.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support93.syncAttribute_SyncChangesEverySeconds = 0f;
			support93.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support93.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support93.syncAttribute_ShouldSkipSync);
			support93.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support94 = valuesChangesSupport[94] = valueChangeSupportArrayPool.Borrow();
		            support94.lastKnownValue.System_Boolean = GONetInputSync.GetKey_KeypadEnter; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support94.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_KeypadEnter; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support94.syncCompanion = this;
			support94.index = 94;
			support94.syncAttribute_MustRunOnUnityMainThread = true;
			support94.syncAttribute_ProcessingPriority = 0;
			support94.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support94.syncAttribute_SyncChangesEverySeconds = 0f;
			support94.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support94.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support94.syncAttribute_ShouldSkipSync);
			support94.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support95 = valuesChangesSupport[95] = valueChangeSupportArrayPool.Borrow();
		            support95.lastKnownValue.System_Boolean = GONetInputSync.GetKey_KeypadEquals; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support95.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_KeypadEquals; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support95.syncCompanion = this;
			support95.index = 95;
			support95.syncAttribute_MustRunOnUnityMainThread = true;
			support95.syncAttribute_ProcessingPriority = 0;
			support95.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support95.syncAttribute_SyncChangesEverySeconds = 0f;
			support95.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support95.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support95.syncAttribute_ShouldSkipSync);
			support95.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support96 = valuesChangesSupport[96] = valueChangeSupportArrayPool.Borrow();
		            support96.lastKnownValue.System_Boolean = GONetInputSync.GetKey_KeypadMinus; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support96.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_KeypadMinus; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support96.syncCompanion = this;
			support96.index = 96;
			support96.syncAttribute_MustRunOnUnityMainThread = true;
			support96.syncAttribute_ProcessingPriority = 0;
			support96.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support96.syncAttribute_SyncChangesEverySeconds = 0f;
			support96.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support96.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support96.syncAttribute_ShouldSkipSync);
			support96.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support97 = valuesChangesSupport[97] = valueChangeSupportArrayPool.Borrow();
		            support97.lastKnownValue.System_Boolean = GONetInputSync.GetKey_KeypadMultiply; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support97.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_KeypadMultiply; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support97.syncCompanion = this;
			support97.index = 97;
			support97.syncAttribute_MustRunOnUnityMainThread = true;
			support97.syncAttribute_ProcessingPriority = 0;
			support97.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support97.syncAttribute_SyncChangesEverySeconds = 0f;
			support97.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support97.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support97.syncAttribute_ShouldSkipSync);
			support97.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support98 = valuesChangesSupport[98] = valueChangeSupportArrayPool.Borrow();
		            support98.lastKnownValue.System_Boolean = GONetInputSync.GetKey_KeypadPeriod; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support98.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_KeypadPeriod; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support98.syncCompanion = this;
			support98.index = 98;
			support98.syncAttribute_MustRunOnUnityMainThread = true;
			support98.syncAttribute_ProcessingPriority = 0;
			support98.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support98.syncAttribute_SyncChangesEverySeconds = 0f;
			support98.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support98.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support98.syncAttribute_ShouldSkipSync);
			support98.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support99 = valuesChangesSupport[99] = valueChangeSupportArrayPool.Borrow();
		            support99.lastKnownValue.System_Boolean = GONetInputSync.GetKey_KeypadPlus; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support99.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_KeypadPlus; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support99.syncCompanion = this;
			support99.index = 99;
			support99.syncAttribute_MustRunOnUnityMainThread = true;
			support99.syncAttribute_ProcessingPriority = 0;
			support99.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support99.syncAttribute_SyncChangesEverySeconds = 0f;
			support99.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support99.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support99.syncAttribute_ShouldSkipSync);
			support99.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support100 = valuesChangesSupport[100] = valueChangeSupportArrayPool.Borrow();
		            support100.lastKnownValue.System_Boolean = GONetInputSync.GetKey_L; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support100.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_L; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support100.syncCompanion = this;
			support100.index = 100;
			support100.syncAttribute_MustRunOnUnityMainThread = true;
			support100.syncAttribute_ProcessingPriority = 0;
			support100.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support100.syncAttribute_SyncChangesEverySeconds = 0f;
			support100.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support100.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support100.syncAttribute_ShouldSkipSync);
			support100.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support101 = valuesChangesSupport[101] = valueChangeSupportArrayPool.Borrow();
		            support101.lastKnownValue.System_Boolean = GONetInputSync.GetKey_LeftAlt; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support101.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_LeftAlt; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support101.syncCompanion = this;
			support101.index = 101;
			support101.syncAttribute_MustRunOnUnityMainThread = true;
			support101.syncAttribute_ProcessingPriority = 0;
			support101.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support101.syncAttribute_SyncChangesEverySeconds = 0f;
			support101.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support101.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support101.syncAttribute_ShouldSkipSync);
			support101.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support102 = valuesChangesSupport[102] = valueChangeSupportArrayPool.Borrow();
		            support102.lastKnownValue.System_Boolean = GONetInputSync.GetKey_LeftApple; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support102.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_LeftApple; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support102.syncCompanion = this;
			support102.index = 102;
			support102.syncAttribute_MustRunOnUnityMainThread = true;
			support102.syncAttribute_ProcessingPriority = 0;
			support102.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support102.syncAttribute_SyncChangesEverySeconds = 0f;
			support102.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support102.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support102.syncAttribute_ShouldSkipSync);
			support102.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support103 = valuesChangesSupport[103] = valueChangeSupportArrayPool.Borrow();
		            support103.lastKnownValue.System_Boolean = GONetInputSync.GetKey_LeftArrow; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support103.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_LeftArrow; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support103.syncCompanion = this;
			support103.index = 103;
			support103.syncAttribute_MustRunOnUnityMainThread = true;
			support103.syncAttribute_ProcessingPriority = 0;
			support103.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support103.syncAttribute_SyncChangesEverySeconds = 0f;
			support103.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support103.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support103.syncAttribute_ShouldSkipSync);
			support103.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support104 = valuesChangesSupport[104] = valueChangeSupportArrayPool.Borrow();
		            support104.lastKnownValue.System_Boolean = GONetInputSync.GetKey_LeftBracket; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support104.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_LeftBracket; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support104.syncCompanion = this;
			support104.index = 104;
			support104.syncAttribute_MustRunOnUnityMainThread = true;
			support104.syncAttribute_ProcessingPriority = 0;
			support104.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support104.syncAttribute_SyncChangesEverySeconds = 0f;
			support104.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support104.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support104.syncAttribute_ShouldSkipSync);
			support104.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support105 = valuesChangesSupport[105] = valueChangeSupportArrayPool.Borrow();
		            support105.lastKnownValue.System_Boolean = GONetInputSync.GetKey_LeftCommand; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support105.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_LeftCommand; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support105.syncCompanion = this;
			support105.index = 105;
			support105.syncAttribute_MustRunOnUnityMainThread = true;
			support105.syncAttribute_ProcessingPriority = 0;
			support105.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support105.syncAttribute_SyncChangesEverySeconds = 0f;
			support105.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support105.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support105.syncAttribute_ShouldSkipSync);
			support105.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support106 = valuesChangesSupport[106] = valueChangeSupportArrayPool.Borrow();
		            support106.lastKnownValue.System_Boolean = GONetInputSync.GetKey_LeftControl; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support106.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_LeftControl; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support106.syncCompanion = this;
			support106.index = 106;
			support106.syncAttribute_MustRunOnUnityMainThread = true;
			support106.syncAttribute_ProcessingPriority = 0;
			support106.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support106.syncAttribute_SyncChangesEverySeconds = 0f;
			support106.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support106.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support106.syncAttribute_ShouldSkipSync);
			support106.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support107 = valuesChangesSupport[107] = valueChangeSupportArrayPool.Borrow();
		            support107.lastKnownValue.System_Boolean = GONetInputSync.GetKey_LeftCurlyBracket; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support107.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_LeftCurlyBracket; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support107.syncCompanion = this;
			support107.index = 107;
			support107.syncAttribute_MustRunOnUnityMainThread = true;
			support107.syncAttribute_ProcessingPriority = 0;
			support107.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support107.syncAttribute_SyncChangesEverySeconds = 0f;
			support107.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support107.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support107.syncAttribute_ShouldSkipSync);
			support107.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support108 = valuesChangesSupport[108] = valueChangeSupportArrayPool.Borrow();
		            support108.lastKnownValue.System_Boolean = GONetInputSync.GetKey_LeftParen; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support108.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_LeftParen; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support108.syncCompanion = this;
			support108.index = 108;
			support108.syncAttribute_MustRunOnUnityMainThread = true;
			support108.syncAttribute_ProcessingPriority = 0;
			support108.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support108.syncAttribute_SyncChangesEverySeconds = 0f;
			support108.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support108.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support108.syncAttribute_ShouldSkipSync);
			support108.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support109 = valuesChangesSupport[109] = valueChangeSupportArrayPool.Borrow();
		            support109.lastKnownValue.System_Boolean = GONetInputSync.GetKey_LeftShift; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support109.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_LeftShift; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support109.syncCompanion = this;
			support109.index = 109;
			support109.syncAttribute_MustRunOnUnityMainThread = true;
			support109.syncAttribute_ProcessingPriority = 0;
			support109.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support109.syncAttribute_SyncChangesEverySeconds = 0f;
			support109.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support109.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support109.syncAttribute_ShouldSkipSync);
			support109.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support110 = valuesChangesSupport[110] = valueChangeSupportArrayPool.Borrow();
		            support110.lastKnownValue.System_Boolean = GONetInputSync.GetKey_LeftWindows; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support110.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_LeftWindows; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support110.syncCompanion = this;
			support110.index = 110;
			support110.syncAttribute_MustRunOnUnityMainThread = true;
			support110.syncAttribute_ProcessingPriority = 0;
			support110.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support110.syncAttribute_SyncChangesEverySeconds = 0f;
			support110.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support110.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support110.syncAttribute_ShouldSkipSync);
			support110.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support111 = valuesChangesSupport[111] = valueChangeSupportArrayPool.Borrow();
		            support111.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Less; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support111.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Less; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support111.syncCompanion = this;
			support111.index = 111;
			support111.syncAttribute_MustRunOnUnityMainThread = true;
			support111.syncAttribute_ProcessingPriority = 0;
			support111.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support111.syncAttribute_SyncChangesEverySeconds = 0f;
			support111.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support111.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support111.syncAttribute_ShouldSkipSync);
			support111.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support112 = valuesChangesSupport[112] = valueChangeSupportArrayPool.Borrow();
		            support112.lastKnownValue.System_Boolean = GONetInputSync.GetKey_M; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support112.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_M; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support112.syncCompanion = this;
			support112.index = 112;
			support112.syncAttribute_MustRunOnUnityMainThread = true;
			support112.syncAttribute_ProcessingPriority = 0;
			support112.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support112.syncAttribute_SyncChangesEverySeconds = 0f;
			support112.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support112.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support112.syncAttribute_ShouldSkipSync);
			support112.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support113 = valuesChangesSupport[113] = valueChangeSupportArrayPool.Borrow();
		            support113.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Menu; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support113.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Menu; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support113.syncCompanion = this;
			support113.index = 113;
			support113.syncAttribute_MustRunOnUnityMainThread = true;
			support113.syncAttribute_ProcessingPriority = 0;
			support113.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support113.syncAttribute_SyncChangesEverySeconds = 0f;
			support113.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support113.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support113.syncAttribute_ShouldSkipSync);
			support113.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support114 = valuesChangesSupport[114] = valueChangeSupportArrayPool.Borrow();
		            support114.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Minus; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support114.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Minus; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support114.syncCompanion = this;
			support114.index = 114;
			support114.syncAttribute_MustRunOnUnityMainThread = true;
			support114.syncAttribute_ProcessingPriority = 0;
			support114.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support114.syncAttribute_SyncChangesEverySeconds = 0f;
			support114.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support114.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support114.syncAttribute_ShouldSkipSync);
			support114.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support115 = valuesChangesSupport[115] = valueChangeSupportArrayPool.Borrow();
		            support115.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Mouse0; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support115.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Mouse0; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support115.syncCompanion = this;
			support115.index = 115;
			support115.syncAttribute_MustRunOnUnityMainThread = true;
			support115.syncAttribute_ProcessingPriority = 0;
			support115.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support115.syncAttribute_SyncChangesEverySeconds = 0f;
			support115.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support115.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support115.syncAttribute_ShouldSkipSync);
			support115.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support116 = valuesChangesSupport[116] = valueChangeSupportArrayPool.Borrow();
		            support116.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Mouse1; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support116.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Mouse1; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support116.syncCompanion = this;
			support116.index = 116;
			support116.syncAttribute_MustRunOnUnityMainThread = true;
			support116.syncAttribute_ProcessingPriority = 0;
			support116.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support116.syncAttribute_SyncChangesEverySeconds = 0f;
			support116.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support116.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support116.syncAttribute_ShouldSkipSync);
			support116.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support117 = valuesChangesSupport[117] = valueChangeSupportArrayPool.Borrow();
		            support117.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Mouse2; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support117.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Mouse2; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support117.syncCompanion = this;
			support117.index = 117;
			support117.syncAttribute_MustRunOnUnityMainThread = true;
			support117.syncAttribute_ProcessingPriority = 0;
			support117.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support117.syncAttribute_SyncChangesEverySeconds = 0f;
			support117.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support117.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support117.syncAttribute_ShouldSkipSync);
			support117.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support118 = valuesChangesSupport[118] = valueChangeSupportArrayPool.Borrow();
		            support118.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Mouse3; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support118.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Mouse3; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support118.syncCompanion = this;
			support118.index = 118;
			support118.syncAttribute_MustRunOnUnityMainThread = true;
			support118.syncAttribute_ProcessingPriority = 0;
			support118.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support118.syncAttribute_SyncChangesEverySeconds = 0f;
			support118.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support118.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support118.syncAttribute_ShouldSkipSync);
			support118.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support119 = valuesChangesSupport[119] = valueChangeSupportArrayPool.Borrow();
		            support119.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Mouse4; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support119.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Mouse4; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support119.syncCompanion = this;
			support119.index = 119;
			support119.syncAttribute_MustRunOnUnityMainThread = true;
			support119.syncAttribute_ProcessingPriority = 0;
			support119.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support119.syncAttribute_SyncChangesEverySeconds = 0f;
			support119.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support119.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support119.syncAttribute_ShouldSkipSync);
			support119.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support120 = valuesChangesSupport[120] = valueChangeSupportArrayPool.Borrow();
		            support120.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Mouse5; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support120.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Mouse5; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support120.syncCompanion = this;
			support120.index = 120;
			support120.syncAttribute_MustRunOnUnityMainThread = true;
			support120.syncAttribute_ProcessingPriority = 0;
			support120.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support120.syncAttribute_SyncChangesEverySeconds = 0f;
			support120.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support120.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support120.syncAttribute_ShouldSkipSync);
			support120.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support121 = valuesChangesSupport[121] = valueChangeSupportArrayPool.Borrow();
		            support121.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Mouse6; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support121.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Mouse6; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support121.syncCompanion = this;
			support121.index = 121;
			support121.syncAttribute_MustRunOnUnityMainThread = true;
			support121.syncAttribute_ProcessingPriority = 0;
			support121.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support121.syncAttribute_SyncChangesEverySeconds = 0f;
			support121.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support121.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support121.syncAttribute_ShouldSkipSync);
			support121.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support122 = valuesChangesSupport[122] = valueChangeSupportArrayPool.Borrow();
		            support122.lastKnownValue.System_Boolean = GONetInputSync.GetKey_N; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support122.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_N; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support122.syncCompanion = this;
			support122.index = 122;
			support122.syncAttribute_MustRunOnUnityMainThread = true;
			support122.syncAttribute_ProcessingPriority = 0;
			support122.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support122.syncAttribute_SyncChangesEverySeconds = 0f;
			support122.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support122.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support122.syncAttribute_ShouldSkipSync);
			support122.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support123 = valuesChangesSupport[123] = valueChangeSupportArrayPool.Borrow();
		            support123.lastKnownValue.System_Boolean = GONetInputSync.GetKey_None; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support123.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_None; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support123.syncCompanion = this;
			support123.index = 123;
			support123.syncAttribute_MustRunOnUnityMainThread = true;
			support123.syncAttribute_ProcessingPriority = 0;
			support123.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support123.syncAttribute_SyncChangesEverySeconds = 0f;
			support123.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support123.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support123.syncAttribute_ShouldSkipSync);
			support123.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support124 = valuesChangesSupport[124] = valueChangeSupportArrayPool.Borrow();
		            support124.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Numlock; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support124.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Numlock; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support124.syncCompanion = this;
			support124.index = 124;
			support124.syncAttribute_MustRunOnUnityMainThread = true;
			support124.syncAttribute_ProcessingPriority = 0;
			support124.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support124.syncAttribute_SyncChangesEverySeconds = 0f;
			support124.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support124.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support124.syncAttribute_ShouldSkipSync);
			support124.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support125 = valuesChangesSupport[125] = valueChangeSupportArrayPool.Borrow();
		            support125.lastKnownValue.System_Boolean = GONetInputSync.GetKey_O; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support125.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_O; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support125.syncCompanion = this;
			support125.index = 125;
			support125.syncAttribute_MustRunOnUnityMainThread = true;
			support125.syncAttribute_ProcessingPriority = 0;
			support125.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support125.syncAttribute_SyncChangesEverySeconds = 0f;
			support125.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support125.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support125.syncAttribute_ShouldSkipSync);
			support125.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support126 = valuesChangesSupport[126] = valueChangeSupportArrayPool.Borrow();
		            support126.lastKnownValue.System_Boolean = GONetInputSync.GetKey_P; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support126.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_P; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support126.syncCompanion = this;
			support126.index = 126;
			support126.syncAttribute_MustRunOnUnityMainThread = true;
			support126.syncAttribute_ProcessingPriority = 0;
			support126.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support126.syncAttribute_SyncChangesEverySeconds = 0f;
			support126.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support126.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support126.syncAttribute_ShouldSkipSync);
			support126.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support127 = valuesChangesSupport[127] = valueChangeSupportArrayPool.Borrow();
		            support127.lastKnownValue.System_Boolean = GONetInputSync.GetKey_PageDown; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support127.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_PageDown; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support127.syncCompanion = this;
			support127.index = 127;
			support127.syncAttribute_MustRunOnUnityMainThread = true;
			support127.syncAttribute_ProcessingPriority = 0;
			support127.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support127.syncAttribute_SyncChangesEverySeconds = 0f;
			support127.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support127.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support127.syncAttribute_ShouldSkipSync);
			support127.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support128 = valuesChangesSupport[128] = valueChangeSupportArrayPool.Borrow();
		            support128.lastKnownValue.System_Boolean = GONetInputSync.GetKey_PageUp; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support128.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_PageUp; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support128.syncCompanion = this;
			support128.index = 128;
			support128.syncAttribute_MustRunOnUnityMainThread = true;
			support128.syncAttribute_ProcessingPriority = 0;
			support128.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support128.syncAttribute_SyncChangesEverySeconds = 0f;
			support128.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support128.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support128.syncAttribute_ShouldSkipSync);
			support128.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support129 = valuesChangesSupport[129] = valueChangeSupportArrayPool.Borrow();
		            support129.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Pause; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support129.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Pause; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support129.syncCompanion = this;
			support129.index = 129;
			support129.syncAttribute_MustRunOnUnityMainThread = true;
			support129.syncAttribute_ProcessingPriority = 0;
			support129.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support129.syncAttribute_SyncChangesEverySeconds = 0f;
			support129.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support129.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support129.syncAttribute_ShouldSkipSync);
			support129.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support130 = valuesChangesSupport[130] = valueChangeSupportArrayPool.Borrow();
		            support130.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Percent; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support130.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Percent; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support130.syncCompanion = this;
			support130.index = 130;
			support130.syncAttribute_MustRunOnUnityMainThread = true;
			support130.syncAttribute_ProcessingPriority = 0;
			support130.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support130.syncAttribute_SyncChangesEverySeconds = 0f;
			support130.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support130.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support130.syncAttribute_ShouldSkipSync);
			support130.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support131 = valuesChangesSupport[131] = valueChangeSupportArrayPool.Borrow();
		            support131.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Period; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support131.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Period; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support131.syncCompanion = this;
			support131.index = 131;
			support131.syncAttribute_MustRunOnUnityMainThread = true;
			support131.syncAttribute_ProcessingPriority = 0;
			support131.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support131.syncAttribute_SyncChangesEverySeconds = 0f;
			support131.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support131.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support131.syncAttribute_ShouldSkipSync);
			support131.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support132 = valuesChangesSupport[132] = valueChangeSupportArrayPool.Borrow();
		            support132.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Pipe; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support132.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Pipe; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support132.syncCompanion = this;
			support132.index = 132;
			support132.syncAttribute_MustRunOnUnityMainThread = true;
			support132.syncAttribute_ProcessingPriority = 0;
			support132.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support132.syncAttribute_SyncChangesEverySeconds = 0f;
			support132.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support132.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support132.syncAttribute_ShouldSkipSync);
			support132.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support133 = valuesChangesSupport[133] = valueChangeSupportArrayPool.Borrow();
		            support133.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Plus; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support133.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Plus; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support133.syncCompanion = this;
			support133.index = 133;
			support133.syncAttribute_MustRunOnUnityMainThread = true;
			support133.syncAttribute_ProcessingPriority = 0;
			support133.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support133.syncAttribute_SyncChangesEverySeconds = 0f;
			support133.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support133.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support133.syncAttribute_ShouldSkipSync);
			support133.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support134 = valuesChangesSupport[134] = valueChangeSupportArrayPool.Borrow();
		            support134.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Print; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support134.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Print; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support134.syncCompanion = this;
			support134.index = 134;
			support134.syncAttribute_MustRunOnUnityMainThread = true;
			support134.syncAttribute_ProcessingPriority = 0;
			support134.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support134.syncAttribute_SyncChangesEverySeconds = 0f;
			support134.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support134.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support134.syncAttribute_ShouldSkipSync);
			support134.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support135 = valuesChangesSupport[135] = valueChangeSupportArrayPool.Borrow();
		            support135.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Q; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support135.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Q; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support135.syncCompanion = this;
			support135.index = 135;
			support135.syncAttribute_MustRunOnUnityMainThread = true;
			support135.syncAttribute_ProcessingPriority = 0;
			support135.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support135.syncAttribute_SyncChangesEverySeconds = 0f;
			support135.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support135.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support135.syncAttribute_ShouldSkipSync);
			support135.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support136 = valuesChangesSupport[136] = valueChangeSupportArrayPool.Borrow();
		            support136.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Question; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support136.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Question; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support136.syncCompanion = this;
			support136.index = 136;
			support136.syncAttribute_MustRunOnUnityMainThread = true;
			support136.syncAttribute_ProcessingPriority = 0;
			support136.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support136.syncAttribute_SyncChangesEverySeconds = 0f;
			support136.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support136.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support136.syncAttribute_ShouldSkipSync);
			support136.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support137 = valuesChangesSupport[137] = valueChangeSupportArrayPool.Borrow();
		            support137.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Quote; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support137.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Quote; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support137.syncCompanion = this;
			support137.index = 137;
			support137.syncAttribute_MustRunOnUnityMainThread = true;
			support137.syncAttribute_ProcessingPriority = 0;
			support137.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support137.syncAttribute_SyncChangesEverySeconds = 0f;
			support137.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support137.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support137.syncAttribute_ShouldSkipSync);
			support137.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support138 = valuesChangesSupport[138] = valueChangeSupportArrayPool.Borrow();
		            support138.lastKnownValue.System_Boolean = GONetInputSync.GetKey_R; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support138.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_R; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support138.syncCompanion = this;
			support138.index = 138;
			support138.syncAttribute_MustRunOnUnityMainThread = true;
			support138.syncAttribute_ProcessingPriority = 0;
			support138.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support138.syncAttribute_SyncChangesEverySeconds = 0f;
			support138.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support138.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support138.syncAttribute_ShouldSkipSync);
			support138.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support139 = valuesChangesSupport[139] = valueChangeSupportArrayPool.Borrow();
		            support139.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Return; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support139.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Return; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support139.syncCompanion = this;
			support139.index = 139;
			support139.syncAttribute_MustRunOnUnityMainThread = true;
			support139.syncAttribute_ProcessingPriority = 0;
			support139.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support139.syncAttribute_SyncChangesEverySeconds = 0f;
			support139.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support139.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support139.syncAttribute_ShouldSkipSync);
			support139.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support140 = valuesChangesSupport[140] = valueChangeSupportArrayPool.Borrow();
		            support140.lastKnownValue.System_Boolean = GONetInputSync.GetKey_RightAlt; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support140.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_RightAlt; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support140.syncCompanion = this;
			support140.index = 140;
			support140.syncAttribute_MustRunOnUnityMainThread = true;
			support140.syncAttribute_ProcessingPriority = 0;
			support140.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support140.syncAttribute_SyncChangesEverySeconds = 0f;
			support140.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support140.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support140.syncAttribute_ShouldSkipSync);
			support140.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support141 = valuesChangesSupport[141] = valueChangeSupportArrayPool.Borrow();
		            support141.lastKnownValue.System_Boolean = GONetInputSync.GetKey_RightApple; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support141.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_RightApple; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support141.syncCompanion = this;
			support141.index = 141;
			support141.syncAttribute_MustRunOnUnityMainThread = true;
			support141.syncAttribute_ProcessingPriority = 0;
			support141.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support141.syncAttribute_SyncChangesEverySeconds = 0f;
			support141.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support141.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support141.syncAttribute_ShouldSkipSync);
			support141.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support142 = valuesChangesSupport[142] = valueChangeSupportArrayPool.Borrow();
		            support142.lastKnownValue.System_Boolean = GONetInputSync.GetKey_RightArrow; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support142.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_RightArrow; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support142.syncCompanion = this;
			support142.index = 142;
			support142.syncAttribute_MustRunOnUnityMainThread = true;
			support142.syncAttribute_ProcessingPriority = 0;
			support142.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support142.syncAttribute_SyncChangesEverySeconds = 0f;
			support142.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support142.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support142.syncAttribute_ShouldSkipSync);
			support142.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support143 = valuesChangesSupport[143] = valueChangeSupportArrayPool.Borrow();
		            support143.lastKnownValue.System_Boolean = GONetInputSync.GetKey_RightBracket; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support143.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_RightBracket; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support143.syncCompanion = this;
			support143.index = 143;
			support143.syncAttribute_MustRunOnUnityMainThread = true;
			support143.syncAttribute_ProcessingPriority = 0;
			support143.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support143.syncAttribute_SyncChangesEverySeconds = 0f;
			support143.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support143.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support143.syncAttribute_ShouldSkipSync);
			support143.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support144 = valuesChangesSupport[144] = valueChangeSupportArrayPool.Borrow();
		            support144.lastKnownValue.System_Boolean = GONetInputSync.GetKey_RightCommand; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support144.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_RightCommand; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support144.syncCompanion = this;
			support144.index = 144;
			support144.syncAttribute_MustRunOnUnityMainThread = true;
			support144.syncAttribute_ProcessingPriority = 0;
			support144.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support144.syncAttribute_SyncChangesEverySeconds = 0f;
			support144.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support144.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support144.syncAttribute_ShouldSkipSync);
			support144.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support145 = valuesChangesSupport[145] = valueChangeSupportArrayPool.Borrow();
		            support145.lastKnownValue.System_Boolean = GONetInputSync.GetKey_RightControl; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support145.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_RightControl; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support145.syncCompanion = this;
			support145.index = 145;
			support145.syncAttribute_MustRunOnUnityMainThread = true;
			support145.syncAttribute_ProcessingPriority = 0;
			support145.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support145.syncAttribute_SyncChangesEverySeconds = 0f;
			support145.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support145.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support145.syncAttribute_ShouldSkipSync);
			support145.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support146 = valuesChangesSupport[146] = valueChangeSupportArrayPool.Borrow();
		            support146.lastKnownValue.System_Boolean = GONetInputSync.GetKey_RightCurlyBracket; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support146.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_RightCurlyBracket; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support146.syncCompanion = this;
			support146.index = 146;
			support146.syncAttribute_MustRunOnUnityMainThread = true;
			support146.syncAttribute_ProcessingPriority = 0;
			support146.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support146.syncAttribute_SyncChangesEverySeconds = 0f;
			support146.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support146.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support146.syncAttribute_ShouldSkipSync);
			support146.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support147 = valuesChangesSupport[147] = valueChangeSupportArrayPool.Borrow();
		            support147.lastKnownValue.System_Boolean = GONetInputSync.GetKey_RightParen; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support147.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_RightParen; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support147.syncCompanion = this;
			support147.index = 147;
			support147.syncAttribute_MustRunOnUnityMainThread = true;
			support147.syncAttribute_ProcessingPriority = 0;
			support147.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support147.syncAttribute_SyncChangesEverySeconds = 0f;
			support147.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support147.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support147.syncAttribute_ShouldSkipSync);
			support147.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support148 = valuesChangesSupport[148] = valueChangeSupportArrayPool.Borrow();
		            support148.lastKnownValue.System_Boolean = GONetInputSync.GetKey_RightShift; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support148.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_RightShift; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support148.syncCompanion = this;
			support148.index = 148;
			support148.syncAttribute_MustRunOnUnityMainThread = true;
			support148.syncAttribute_ProcessingPriority = 0;
			support148.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support148.syncAttribute_SyncChangesEverySeconds = 0f;
			support148.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support148.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support148.syncAttribute_ShouldSkipSync);
			support148.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support149 = valuesChangesSupport[149] = valueChangeSupportArrayPool.Borrow();
		            support149.lastKnownValue.System_Boolean = GONetInputSync.GetKey_RightWindows; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support149.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_RightWindows; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support149.syncCompanion = this;
			support149.index = 149;
			support149.syncAttribute_MustRunOnUnityMainThread = true;
			support149.syncAttribute_ProcessingPriority = 0;
			support149.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support149.syncAttribute_SyncChangesEverySeconds = 0f;
			support149.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support149.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support149.syncAttribute_ShouldSkipSync);
			support149.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support150 = valuesChangesSupport[150] = valueChangeSupportArrayPool.Borrow();
		            support150.lastKnownValue.System_Boolean = GONetInputSync.GetKey_S; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support150.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_S; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support150.syncCompanion = this;
			support150.index = 150;
			support150.syncAttribute_MustRunOnUnityMainThread = true;
			support150.syncAttribute_ProcessingPriority = 0;
			support150.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support150.syncAttribute_SyncChangesEverySeconds = 0f;
			support150.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support150.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support150.syncAttribute_ShouldSkipSync);
			support150.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support151 = valuesChangesSupport[151] = valueChangeSupportArrayPool.Borrow();
		            support151.lastKnownValue.System_Boolean = GONetInputSync.GetKey_ScrollLock; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support151.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_ScrollLock; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support151.syncCompanion = this;
			support151.index = 151;
			support151.syncAttribute_MustRunOnUnityMainThread = true;
			support151.syncAttribute_ProcessingPriority = 0;
			support151.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support151.syncAttribute_SyncChangesEverySeconds = 0f;
			support151.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support151.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support151.syncAttribute_ShouldSkipSync);
			support151.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support152 = valuesChangesSupport[152] = valueChangeSupportArrayPool.Borrow();
		            support152.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Semicolon; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support152.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Semicolon; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support152.syncCompanion = this;
			support152.index = 152;
			support152.syncAttribute_MustRunOnUnityMainThread = true;
			support152.syncAttribute_ProcessingPriority = 0;
			support152.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support152.syncAttribute_SyncChangesEverySeconds = 0f;
			support152.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support152.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support152.syncAttribute_ShouldSkipSync);
			support152.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support153 = valuesChangesSupport[153] = valueChangeSupportArrayPool.Borrow();
		            support153.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Slash; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support153.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Slash; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support153.syncCompanion = this;
			support153.index = 153;
			support153.syncAttribute_MustRunOnUnityMainThread = true;
			support153.syncAttribute_ProcessingPriority = 0;
			support153.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support153.syncAttribute_SyncChangesEverySeconds = 0f;
			support153.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support153.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support153.syncAttribute_ShouldSkipSync);
			support153.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support154 = valuesChangesSupport[154] = valueChangeSupportArrayPool.Borrow();
		            support154.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Space; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support154.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Space; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support154.syncCompanion = this;
			support154.index = 154;
			support154.syncAttribute_MustRunOnUnityMainThread = true;
			support154.syncAttribute_ProcessingPriority = 0;
			support154.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support154.syncAttribute_SyncChangesEverySeconds = 0f;
			support154.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support154.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support154.syncAttribute_ShouldSkipSync);
			support154.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support155 = valuesChangesSupport[155] = valueChangeSupportArrayPool.Borrow();
		            support155.lastKnownValue.System_Boolean = GONetInputSync.GetKey_SysReq; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support155.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_SysReq; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support155.syncCompanion = this;
			support155.index = 155;
			support155.syncAttribute_MustRunOnUnityMainThread = true;
			support155.syncAttribute_ProcessingPriority = 0;
			support155.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support155.syncAttribute_SyncChangesEverySeconds = 0f;
			support155.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support155.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support155.syncAttribute_ShouldSkipSync);
			support155.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support156 = valuesChangesSupport[156] = valueChangeSupportArrayPool.Borrow();
		            support156.lastKnownValue.System_Boolean = GONetInputSync.GetKey_T; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support156.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_T; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support156.syncCompanion = this;
			support156.index = 156;
			support156.syncAttribute_MustRunOnUnityMainThread = true;
			support156.syncAttribute_ProcessingPriority = 0;
			support156.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support156.syncAttribute_SyncChangesEverySeconds = 0f;
			support156.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support156.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support156.syncAttribute_ShouldSkipSync);
			support156.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support157 = valuesChangesSupport[157] = valueChangeSupportArrayPool.Borrow();
		            support157.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Tab; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support157.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Tab; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support157.syncCompanion = this;
			support157.index = 157;
			support157.syncAttribute_MustRunOnUnityMainThread = true;
			support157.syncAttribute_ProcessingPriority = 0;
			support157.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support157.syncAttribute_SyncChangesEverySeconds = 0f;
			support157.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support157.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support157.syncAttribute_ShouldSkipSync);
			support157.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support158 = valuesChangesSupport[158] = valueChangeSupportArrayPool.Borrow();
		            support158.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Tilde; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support158.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Tilde; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support158.syncCompanion = this;
			support158.index = 158;
			support158.syncAttribute_MustRunOnUnityMainThread = true;
			support158.syncAttribute_ProcessingPriority = 0;
			support158.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support158.syncAttribute_SyncChangesEverySeconds = 0f;
			support158.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support158.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support158.syncAttribute_ShouldSkipSync);
			support158.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support159 = valuesChangesSupport[159] = valueChangeSupportArrayPool.Borrow();
		            support159.lastKnownValue.System_Boolean = GONetInputSync.GetKey_U; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support159.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_U; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support159.syncCompanion = this;
			support159.index = 159;
			support159.syncAttribute_MustRunOnUnityMainThread = true;
			support159.syncAttribute_ProcessingPriority = 0;
			support159.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support159.syncAttribute_SyncChangesEverySeconds = 0f;
			support159.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support159.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support159.syncAttribute_ShouldSkipSync);
			support159.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support160 = valuesChangesSupport[160] = valueChangeSupportArrayPool.Borrow();
		            support160.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Underscore; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support160.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Underscore; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support160.syncCompanion = this;
			support160.index = 160;
			support160.syncAttribute_MustRunOnUnityMainThread = true;
			support160.syncAttribute_ProcessingPriority = 0;
			support160.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support160.syncAttribute_SyncChangesEverySeconds = 0f;
			support160.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support160.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support160.syncAttribute_ShouldSkipSync);
			support160.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support161 = valuesChangesSupport[161] = valueChangeSupportArrayPool.Borrow();
		            support161.lastKnownValue.System_Boolean = GONetInputSync.GetKey_UpArrow; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support161.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_UpArrow; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support161.syncCompanion = this;
			support161.index = 161;
			support161.syncAttribute_MustRunOnUnityMainThread = true;
			support161.syncAttribute_ProcessingPriority = 0;
			support161.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support161.syncAttribute_SyncChangesEverySeconds = 0f;
			support161.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support161.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support161.syncAttribute_ShouldSkipSync);
			support161.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support162 = valuesChangesSupport[162] = valueChangeSupportArrayPool.Borrow();
		            support162.lastKnownValue.System_Boolean = GONetInputSync.GetKey_V; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support162.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_V; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support162.syncCompanion = this;
			support162.index = 162;
			support162.syncAttribute_MustRunOnUnityMainThread = true;
			support162.syncAttribute_ProcessingPriority = 0;
			support162.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support162.syncAttribute_SyncChangesEverySeconds = 0f;
			support162.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support162.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support162.syncAttribute_ShouldSkipSync);
			support162.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support163 = valuesChangesSupport[163] = valueChangeSupportArrayPool.Borrow();
		            support163.lastKnownValue.System_Boolean = GONetInputSync.GetKey_W; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support163.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_W; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support163.syncCompanion = this;
			support163.index = 163;
			support163.syncAttribute_MustRunOnUnityMainThread = true;
			support163.syncAttribute_ProcessingPriority = 0;
			support163.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support163.syncAttribute_SyncChangesEverySeconds = 0f;
			support163.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support163.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support163.syncAttribute_ShouldSkipSync);
			support163.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support164 = valuesChangesSupport[164] = valueChangeSupportArrayPool.Borrow();
		            support164.lastKnownValue.System_Boolean = GONetInputSync.GetKey_X; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support164.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_X; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support164.syncCompanion = this;
			support164.index = 164;
			support164.syncAttribute_MustRunOnUnityMainThread = true;
			support164.syncAttribute_ProcessingPriority = 0;
			support164.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support164.syncAttribute_SyncChangesEverySeconds = 0f;
			support164.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support164.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support164.syncAttribute_ShouldSkipSync);
			support164.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support165 = valuesChangesSupport[165] = valueChangeSupportArrayPool.Borrow();
		            support165.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Y; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support165.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Y; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support165.syncCompanion = this;
			support165.index = 165;
			support165.syncAttribute_MustRunOnUnityMainThread = true;
			support165.syncAttribute_ProcessingPriority = 0;
			support165.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support165.syncAttribute_SyncChangesEverySeconds = 0f;
			support165.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support165.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support165.syncAttribute_ShouldSkipSync);
			support165.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support166 = valuesChangesSupport[166] = valueChangeSupportArrayPool.Borrow();
		            support166.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Z; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support166.lastKnownValue_previous.System_Boolean = GONetInputSync.GetKey_Z; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support166.syncCompanion = this;
			support166.index = 166;
			support166.syncAttribute_MustRunOnUnityMainThread = true;
			support166.syncAttribute_ProcessingPriority = 0;
			support166.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support166.syncAttribute_SyncChangesEverySeconds = 0f;
			support166.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support166.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support166.syncAttribute_ShouldSkipSync);
			support166.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support167 = valuesChangesSupport[167] = valueChangeSupportArrayPool.Borrow();
		            support167.lastKnownValue.System_Boolean = GONetInputSync.GetMouseButton_0; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support167.lastKnownValue_previous.System_Boolean = GONetInputSync.GetMouseButton_0; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support167.syncCompanion = this;
			support167.index = 167;
			support167.syncAttribute_MustRunOnUnityMainThread = true;
			support167.syncAttribute_ProcessingPriority = 0;
			support167.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support167.syncAttribute_SyncChangesEverySeconds = 0f;
			support167.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support167.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support167.syncAttribute_ShouldSkipSync);
			support167.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support168 = valuesChangesSupport[168] = valueChangeSupportArrayPool.Borrow();
		            support168.lastKnownValue.System_Boolean = GONetInputSync.GetMouseButton_1; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support168.lastKnownValue_previous.System_Boolean = GONetInputSync.GetMouseButton_1; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support168.syncCompanion = this;
			support168.index = 168;
			support168.syncAttribute_MustRunOnUnityMainThread = true;
			support168.syncAttribute_ProcessingPriority = 0;
			support168.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support168.syncAttribute_SyncChangesEverySeconds = 0f;
			support168.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support168.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support168.syncAttribute_ShouldSkipSync);
			support168.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support169 = valuesChangesSupport[169] = valueChangeSupportArrayPool.Borrow();
		            support169.lastKnownValue.System_Boolean = GONetInputSync.GetMouseButton_2; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support169.lastKnownValue_previous.System_Boolean = GONetInputSync.GetMouseButton_2; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support169.syncCompanion = this;
			support169.index = 169;
			support169.syncAttribute_MustRunOnUnityMainThread = true;
			support169.syncAttribute_ProcessingPriority = 0;
			support169.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support169.syncAttribute_SyncChangesEverySeconds = 0f;
			support169.syncAttribute_Reliability = AutoMagicalSyncReliability.Reliable;
			support169.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support169.syncAttribute_ShouldSkipSync);
			support169.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support170 = valuesChangesSupport[170] = valueChangeSupportArrayPool.Borrow();
		            support170.lastKnownValue.UnityEngine_Vector2 = GONetInputSync.mousePosition; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support170.lastKnownValue_previous.UnityEngine_Vector2 = GONetInputSync.mousePosition; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support170.syncCompanion = this;
			support170.index = 170;
			support170.syncAttribute_MustRunOnUnityMainThread = true;
			support170.syncAttribute_ProcessingPriority = 0;
			support170.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support170.syncAttribute_SyncChangesEverySeconds = 0.04166667f;
			support170.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
			support170.syncAttribute_ShouldBlendBetweenValuesReceived = false;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support170.syncAttribute_ShouldSkipSync);
			support170.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);

			var support171 = valuesChangesSupport[171] = valueChangeSupportArrayPool.Borrow();
		            support171.lastKnownValue.UnityEngine_Quaternion = Transform.rotation; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support171.lastKnownValue_previous.UnityEngine_Quaternion = Transform.rotation; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support171.syncCompanion = this;
			support171.index = 171;
			support171.syncAttribute_MustRunOnUnityMainThread = true;
			support171.syncAttribute_ProcessingPriority = 0;
			support171.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support171.syncAttribute_SyncChangesEverySeconds = 0.03333334f;
			support171.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
			support171.syncAttribute_ShouldBlendBetweenValuesReceived = true;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(1, out support171.syncAttribute_ShouldSkipSync);
			support171.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
		
            int support171_mostRecentChanges_calcdSize = support171.syncAttribute_SyncChangesEverySeconds != 0 ? (int)((GONetMain.valueBlendingBufferLeadSeconds / support171.syncAttribute_SyncChangesEverySeconds) * 2.5f) : 0;
            support171.mostRecentChanges_capacitySize = Math.Max(support171_mostRecentChanges_calcdSize, GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.MOST_RECENT_CHANGEs_SIZE_MINIMUM);
			support171.mostRecentChanges = GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.mostRecentChangesPool.Borrow(support171.mostRecentChanges_capacitySize);

			var support172 = valuesChangesSupport[172] = valueChangeSupportArrayPool.Borrow();
		            support172.lastKnownValue.UnityEngine_Vector3 = Transform.position; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support172.lastKnownValue_previous.UnityEngine_Vector3 = Transform.position; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support172.syncCompanion = this;
			support172.index = 172;
			support172.syncAttribute_MustRunOnUnityMainThread = true;
			support172.syncAttribute_ProcessingPriority = 0;
			support172.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support172.syncAttribute_SyncChangesEverySeconds = 0.03333334f;
			support172.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
			support172.syncAttribute_ShouldBlendBetweenValuesReceived = true;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(2, out support172.syncAttribute_ShouldSkipSync);
			support172.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-5000f, 5000f, 0, true);
		
            int support172_mostRecentChanges_calcdSize = support172.syncAttribute_SyncChangesEverySeconds != 0 ? (int)((GONetMain.valueBlendingBufferLeadSeconds / support172.syncAttribute_SyncChangesEverySeconds) * 2.5f) : 0;
            support172.mostRecentChanges_capacitySize = Math.Max(support172_mostRecentChanges_calcdSize, GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.MOST_RECENT_CHANGEs_SIZE_MINIMUM);
			support172.mostRecentChanges = GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.mostRecentChangesPool.Borrow(support172.mostRecentChanges_capacitySize);

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
									GONetInputSync.GetKey_A = value.System_Boolean;
									return;
				case 5:
									GONetInputSync.GetKey_Alpha0 = value.System_Boolean;
									return;
				case 6:
									GONetInputSync.GetKey_Alpha1 = value.System_Boolean;
									return;
				case 7:
									GONetInputSync.GetKey_Alpha2 = value.System_Boolean;
									return;
				case 8:
									GONetInputSync.GetKey_Alpha3 = value.System_Boolean;
									return;
				case 9:
									GONetInputSync.GetKey_Alpha4 = value.System_Boolean;
									return;
				case 10:
									GONetInputSync.GetKey_Alpha5 = value.System_Boolean;
									return;
				case 11:
									GONetInputSync.GetKey_Alpha6 = value.System_Boolean;
									return;
				case 12:
									GONetInputSync.GetKey_Alpha7 = value.System_Boolean;
									return;
				case 13:
									GONetInputSync.GetKey_Alpha8 = value.System_Boolean;
									return;
				case 14:
									GONetInputSync.GetKey_Alpha9 = value.System_Boolean;
									return;
				case 15:
									GONetInputSync.GetKey_AltGr = value.System_Boolean;
									return;
				case 16:
									GONetInputSync.GetKey_Ampersand = value.System_Boolean;
									return;
				case 17:
									GONetInputSync.GetKey_Asterisk = value.System_Boolean;
									return;
				case 18:
									GONetInputSync.GetKey_At = value.System_Boolean;
									return;
				case 19:
									GONetInputSync.GetKey_B = value.System_Boolean;
									return;
				case 20:
									GONetInputSync.GetKey_BackQuote = value.System_Boolean;
									return;
				case 21:
									GONetInputSync.GetKey_Backslash = value.System_Boolean;
									return;
				case 22:
									GONetInputSync.GetKey_Backspace = value.System_Boolean;
									return;
				case 23:
									GONetInputSync.GetKey_Break = value.System_Boolean;
									return;
				case 24:
									GONetInputSync.GetKey_C = value.System_Boolean;
									return;
				case 25:
									GONetInputSync.GetKey_CapsLock = value.System_Boolean;
									return;
				case 26:
									GONetInputSync.GetKey_Caret = value.System_Boolean;
									return;
				case 27:
									GONetInputSync.GetKey_Clear = value.System_Boolean;
									return;
				case 28:
									GONetInputSync.GetKey_Colon = value.System_Boolean;
									return;
				case 29:
									GONetInputSync.GetKey_Comma = value.System_Boolean;
									return;
				case 30:
									GONetInputSync.GetKey_D = value.System_Boolean;
									return;
				case 31:
									GONetInputSync.GetKey_Delete = value.System_Boolean;
									return;
				case 32:
									GONetInputSync.GetKey_Dollar = value.System_Boolean;
									return;
				case 33:
									GONetInputSync.GetKey_DoubleQuote = value.System_Boolean;
									return;
				case 34:
									GONetInputSync.GetKey_DownArrow = value.System_Boolean;
									return;
				case 35:
									GONetInputSync.GetKey_E = value.System_Boolean;
									return;
				case 36:
									GONetInputSync.GetKey_End = value.System_Boolean;
									return;
				case 37:
									GONetInputSync.GetKey_Equals = value.System_Boolean;
									return;
				case 38:
									GONetInputSync.GetKey_Escape = value.System_Boolean;
									return;
				case 39:
									GONetInputSync.GetKey_Exclaim = value.System_Boolean;
									return;
				case 40:
									GONetInputSync.GetKey_F = value.System_Boolean;
									return;
				case 41:
									GONetInputSync.GetKey_F1 = value.System_Boolean;
									return;
				case 42:
									GONetInputSync.GetKey_F10 = value.System_Boolean;
									return;
				case 43:
									GONetInputSync.GetKey_F11 = value.System_Boolean;
									return;
				case 44:
									GONetInputSync.GetKey_F12 = value.System_Boolean;
									return;
				case 45:
									GONetInputSync.GetKey_F2 = value.System_Boolean;
									return;
				case 46:
									GONetInputSync.GetKey_F3 = value.System_Boolean;
									return;
				case 47:
									GONetInputSync.GetKey_F4 = value.System_Boolean;
									return;
				case 48:
									GONetInputSync.GetKey_F5 = value.System_Boolean;
									return;
				case 49:
									GONetInputSync.GetKey_F6 = value.System_Boolean;
									return;
				case 50:
									GONetInputSync.GetKey_F7 = value.System_Boolean;
									return;
				case 51:
									GONetInputSync.GetKey_F8 = value.System_Boolean;
									return;
				case 52:
									GONetInputSync.GetKey_F9 = value.System_Boolean;
									return;
				case 53:
									GONetInputSync.GetKey_G = value.System_Boolean;
									return;
				case 54:
									GONetInputSync.GetKey_Greater = value.System_Boolean;
									return;
				case 55:
									GONetInputSync.GetKey_H = value.System_Boolean;
									return;
				case 56:
									GONetInputSync.GetKey_Hash = value.System_Boolean;
									return;
				case 57:
									GONetInputSync.GetKey_Help = value.System_Boolean;
									return;
				case 58:
									GONetInputSync.GetKey_Home = value.System_Boolean;
									return;
				case 59:
									GONetInputSync.GetKey_I = value.System_Boolean;
									return;
				case 60:
									GONetInputSync.GetKey_Insert = value.System_Boolean;
									return;
				case 61:
									GONetInputSync.GetKey_J = value.System_Boolean;
									return;
				case 62:
									GONetInputSync.GetKey_JoystickButton0 = value.System_Boolean;
									return;
				case 63:
									GONetInputSync.GetKey_JoystickButton1 = value.System_Boolean;
									return;
				case 64:
									GONetInputSync.GetKey_JoystickButton10 = value.System_Boolean;
									return;
				case 65:
									GONetInputSync.GetKey_JoystickButton11 = value.System_Boolean;
									return;
				case 66:
									GONetInputSync.GetKey_JoystickButton12 = value.System_Boolean;
									return;
				case 67:
									GONetInputSync.GetKey_JoystickButton13 = value.System_Boolean;
									return;
				case 68:
									GONetInputSync.GetKey_JoystickButton14 = value.System_Boolean;
									return;
				case 69:
									GONetInputSync.GetKey_JoystickButton15 = value.System_Boolean;
									return;
				case 70:
									GONetInputSync.GetKey_JoystickButton16 = value.System_Boolean;
									return;
				case 71:
									GONetInputSync.GetKey_JoystickButton17 = value.System_Boolean;
									return;
				case 72:
									GONetInputSync.GetKey_JoystickButton18 = value.System_Boolean;
									return;
				case 73:
									GONetInputSync.GetKey_JoystickButton19 = value.System_Boolean;
									return;
				case 74:
									GONetInputSync.GetKey_JoystickButton2 = value.System_Boolean;
									return;
				case 75:
									GONetInputSync.GetKey_JoystickButton3 = value.System_Boolean;
									return;
				case 76:
									GONetInputSync.GetKey_JoystickButton4 = value.System_Boolean;
									return;
				case 77:
									GONetInputSync.GetKey_JoystickButton5 = value.System_Boolean;
									return;
				case 78:
									GONetInputSync.GetKey_JoystickButton6 = value.System_Boolean;
									return;
				case 79:
									GONetInputSync.GetKey_JoystickButton7 = value.System_Boolean;
									return;
				case 80:
									GONetInputSync.GetKey_JoystickButton8 = value.System_Boolean;
									return;
				case 81:
									GONetInputSync.GetKey_JoystickButton9 = value.System_Boolean;
									return;
				case 82:
									GONetInputSync.GetKey_K = value.System_Boolean;
									return;
				case 83:
									GONetInputSync.GetKey_Keypad0 = value.System_Boolean;
									return;
				case 84:
									GONetInputSync.GetKey_Keypad1 = value.System_Boolean;
									return;
				case 85:
									GONetInputSync.GetKey_Keypad2 = value.System_Boolean;
									return;
				case 86:
									GONetInputSync.GetKey_Keypad3 = value.System_Boolean;
									return;
				case 87:
									GONetInputSync.GetKey_Keypad4 = value.System_Boolean;
									return;
				case 88:
									GONetInputSync.GetKey_Keypad5 = value.System_Boolean;
									return;
				case 89:
									GONetInputSync.GetKey_Keypad6 = value.System_Boolean;
									return;
				case 90:
									GONetInputSync.GetKey_Keypad7 = value.System_Boolean;
									return;
				case 91:
									GONetInputSync.GetKey_Keypad8 = value.System_Boolean;
									return;
				case 92:
									GONetInputSync.GetKey_Keypad9 = value.System_Boolean;
									return;
				case 93:
									GONetInputSync.GetKey_KeypadDivide = value.System_Boolean;
									return;
				case 94:
									GONetInputSync.GetKey_KeypadEnter = value.System_Boolean;
									return;
				case 95:
									GONetInputSync.GetKey_KeypadEquals = value.System_Boolean;
									return;
				case 96:
									GONetInputSync.GetKey_KeypadMinus = value.System_Boolean;
									return;
				case 97:
									GONetInputSync.GetKey_KeypadMultiply = value.System_Boolean;
									return;
				case 98:
									GONetInputSync.GetKey_KeypadPeriod = value.System_Boolean;
									return;
				case 99:
									GONetInputSync.GetKey_KeypadPlus = value.System_Boolean;
									return;
				case 100:
									GONetInputSync.GetKey_L = value.System_Boolean;
									return;
				case 101:
									GONetInputSync.GetKey_LeftAlt = value.System_Boolean;
									return;
				case 102:
									GONetInputSync.GetKey_LeftApple = value.System_Boolean;
									return;
				case 103:
									GONetInputSync.GetKey_LeftArrow = value.System_Boolean;
									return;
				case 104:
									GONetInputSync.GetKey_LeftBracket = value.System_Boolean;
									return;
				case 105:
									GONetInputSync.GetKey_LeftCommand = value.System_Boolean;
									return;
				case 106:
									GONetInputSync.GetKey_LeftControl = value.System_Boolean;
									return;
				case 107:
									GONetInputSync.GetKey_LeftCurlyBracket = value.System_Boolean;
									return;
				case 108:
									GONetInputSync.GetKey_LeftParen = value.System_Boolean;
									return;
				case 109:
									GONetInputSync.GetKey_LeftShift = value.System_Boolean;
									return;
				case 110:
									GONetInputSync.GetKey_LeftWindows = value.System_Boolean;
									return;
				case 111:
									GONetInputSync.GetKey_Less = value.System_Boolean;
									return;
				case 112:
									GONetInputSync.GetKey_M = value.System_Boolean;
									return;
				case 113:
									GONetInputSync.GetKey_Menu = value.System_Boolean;
									return;
				case 114:
									GONetInputSync.GetKey_Minus = value.System_Boolean;
									return;
				case 115:
									GONetInputSync.GetKey_Mouse0 = value.System_Boolean;
									return;
				case 116:
									GONetInputSync.GetKey_Mouse1 = value.System_Boolean;
									return;
				case 117:
									GONetInputSync.GetKey_Mouse2 = value.System_Boolean;
									return;
				case 118:
									GONetInputSync.GetKey_Mouse3 = value.System_Boolean;
									return;
				case 119:
									GONetInputSync.GetKey_Mouse4 = value.System_Boolean;
									return;
				case 120:
									GONetInputSync.GetKey_Mouse5 = value.System_Boolean;
									return;
				case 121:
									GONetInputSync.GetKey_Mouse6 = value.System_Boolean;
									return;
				case 122:
									GONetInputSync.GetKey_N = value.System_Boolean;
									return;
				case 123:
									GONetInputSync.GetKey_None = value.System_Boolean;
									return;
				case 124:
									GONetInputSync.GetKey_Numlock = value.System_Boolean;
									return;
				case 125:
									GONetInputSync.GetKey_O = value.System_Boolean;
									return;
				case 126:
									GONetInputSync.GetKey_P = value.System_Boolean;
									return;
				case 127:
									GONetInputSync.GetKey_PageDown = value.System_Boolean;
									return;
				case 128:
									GONetInputSync.GetKey_PageUp = value.System_Boolean;
									return;
				case 129:
									GONetInputSync.GetKey_Pause = value.System_Boolean;
									return;
				case 130:
									GONetInputSync.GetKey_Percent = value.System_Boolean;
									return;
				case 131:
									GONetInputSync.GetKey_Period = value.System_Boolean;
									return;
				case 132:
									GONetInputSync.GetKey_Pipe = value.System_Boolean;
									return;
				case 133:
									GONetInputSync.GetKey_Plus = value.System_Boolean;
									return;
				case 134:
									GONetInputSync.GetKey_Print = value.System_Boolean;
									return;
				case 135:
									GONetInputSync.GetKey_Q = value.System_Boolean;
									return;
				case 136:
									GONetInputSync.GetKey_Question = value.System_Boolean;
									return;
				case 137:
									GONetInputSync.GetKey_Quote = value.System_Boolean;
									return;
				case 138:
									GONetInputSync.GetKey_R = value.System_Boolean;
									return;
				case 139:
									GONetInputSync.GetKey_Return = value.System_Boolean;
									return;
				case 140:
									GONetInputSync.GetKey_RightAlt = value.System_Boolean;
									return;
				case 141:
									GONetInputSync.GetKey_RightApple = value.System_Boolean;
									return;
				case 142:
									GONetInputSync.GetKey_RightArrow = value.System_Boolean;
									return;
				case 143:
									GONetInputSync.GetKey_RightBracket = value.System_Boolean;
									return;
				case 144:
									GONetInputSync.GetKey_RightCommand = value.System_Boolean;
									return;
				case 145:
									GONetInputSync.GetKey_RightControl = value.System_Boolean;
									return;
				case 146:
									GONetInputSync.GetKey_RightCurlyBracket = value.System_Boolean;
									return;
				case 147:
									GONetInputSync.GetKey_RightParen = value.System_Boolean;
									return;
				case 148:
									GONetInputSync.GetKey_RightShift = value.System_Boolean;
									return;
				case 149:
									GONetInputSync.GetKey_RightWindows = value.System_Boolean;
									return;
				case 150:
									GONetInputSync.GetKey_S = value.System_Boolean;
									return;
				case 151:
									GONetInputSync.GetKey_ScrollLock = value.System_Boolean;
									return;
				case 152:
									GONetInputSync.GetKey_Semicolon = value.System_Boolean;
									return;
				case 153:
									GONetInputSync.GetKey_Slash = value.System_Boolean;
									return;
				case 154:
									GONetInputSync.GetKey_Space = value.System_Boolean;
									return;
				case 155:
									GONetInputSync.GetKey_SysReq = value.System_Boolean;
									return;
				case 156:
									GONetInputSync.GetKey_T = value.System_Boolean;
									return;
				case 157:
									GONetInputSync.GetKey_Tab = value.System_Boolean;
									return;
				case 158:
									GONetInputSync.GetKey_Tilde = value.System_Boolean;
									return;
				case 159:
									GONetInputSync.GetKey_U = value.System_Boolean;
									return;
				case 160:
									GONetInputSync.GetKey_Underscore = value.System_Boolean;
									return;
				case 161:
									GONetInputSync.GetKey_UpArrow = value.System_Boolean;
									return;
				case 162:
									GONetInputSync.GetKey_V = value.System_Boolean;
									return;
				case 163:
									GONetInputSync.GetKey_W = value.System_Boolean;
									return;
				case 164:
									GONetInputSync.GetKey_X = value.System_Boolean;
									return;
				case 165:
									GONetInputSync.GetKey_Y = value.System_Boolean;
									return;
				case 166:
									GONetInputSync.GetKey_Z = value.System_Boolean;
									return;
				case 167:
									GONetInputSync.GetMouseButton_0 = value.System_Boolean;
									return;
				case 168:
									GONetInputSync.GetMouseButton_1 = value.System_Boolean;
									return;
				case 169:
									GONetInputSync.GetMouseButton_2 = value.System_Boolean;
									return;
				case 170:
									GONetInputSync.mousePosition = value.UnityEngine_Vector2;
									return;
				case 171:
									Transform.rotation = value.UnityEngine_Quaternion;
									return;
				case 172:
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
									return GONetInputSync.GetKey_A;
								case 5:
									return GONetInputSync.GetKey_Alpha0;
								case 6:
									return GONetInputSync.GetKey_Alpha1;
								case 7:
									return GONetInputSync.GetKey_Alpha2;
								case 8:
									return GONetInputSync.GetKey_Alpha3;
								case 9:
									return GONetInputSync.GetKey_Alpha4;
								case 10:
									return GONetInputSync.GetKey_Alpha5;
								case 11:
									return GONetInputSync.GetKey_Alpha6;
								case 12:
									return GONetInputSync.GetKey_Alpha7;
								case 13:
									return GONetInputSync.GetKey_Alpha8;
								case 14:
									return GONetInputSync.GetKey_Alpha9;
								case 15:
									return GONetInputSync.GetKey_AltGr;
								case 16:
									return GONetInputSync.GetKey_Ampersand;
								case 17:
									return GONetInputSync.GetKey_Asterisk;
								case 18:
									return GONetInputSync.GetKey_At;
								case 19:
									return GONetInputSync.GetKey_B;
								case 20:
									return GONetInputSync.GetKey_BackQuote;
								case 21:
									return GONetInputSync.GetKey_Backslash;
								case 22:
									return GONetInputSync.GetKey_Backspace;
								case 23:
									return GONetInputSync.GetKey_Break;
								case 24:
									return GONetInputSync.GetKey_C;
								case 25:
									return GONetInputSync.GetKey_CapsLock;
								case 26:
									return GONetInputSync.GetKey_Caret;
								case 27:
									return GONetInputSync.GetKey_Clear;
								case 28:
									return GONetInputSync.GetKey_Colon;
								case 29:
									return GONetInputSync.GetKey_Comma;
								case 30:
									return GONetInputSync.GetKey_D;
								case 31:
									return GONetInputSync.GetKey_Delete;
								case 32:
									return GONetInputSync.GetKey_Dollar;
								case 33:
									return GONetInputSync.GetKey_DoubleQuote;
								case 34:
									return GONetInputSync.GetKey_DownArrow;
								case 35:
									return GONetInputSync.GetKey_E;
								case 36:
									return GONetInputSync.GetKey_End;
								case 37:
									return GONetInputSync.GetKey_Equals;
								case 38:
									return GONetInputSync.GetKey_Escape;
								case 39:
									return GONetInputSync.GetKey_Exclaim;
								case 40:
									return GONetInputSync.GetKey_F;
								case 41:
									return GONetInputSync.GetKey_F1;
								case 42:
									return GONetInputSync.GetKey_F10;
								case 43:
									return GONetInputSync.GetKey_F11;
								case 44:
									return GONetInputSync.GetKey_F12;
								case 45:
									return GONetInputSync.GetKey_F2;
								case 46:
									return GONetInputSync.GetKey_F3;
								case 47:
									return GONetInputSync.GetKey_F4;
								case 48:
									return GONetInputSync.GetKey_F5;
								case 49:
									return GONetInputSync.GetKey_F6;
								case 50:
									return GONetInputSync.GetKey_F7;
								case 51:
									return GONetInputSync.GetKey_F8;
								case 52:
									return GONetInputSync.GetKey_F9;
								case 53:
									return GONetInputSync.GetKey_G;
								case 54:
									return GONetInputSync.GetKey_Greater;
								case 55:
									return GONetInputSync.GetKey_H;
								case 56:
									return GONetInputSync.GetKey_Hash;
								case 57:
									return GONetInputSync.GetKey_Help;
								case 58:
									return GONetInputSync.GetKey_Home;
								case 59:
									return GONetInputSync.GetKey_I;
								case 60:
									return GONetInputSync.GetKey_Insert;
								case 61:
									return GONetInputSync.GetKey_J;
								case 62:
									return GONetInputSync.GetKey_JoystickButton0;
								case 63:
									return GONetInputSync.GetKey_JoystickButton1;
								case 64:
									return GONetInputSync.GetKey_JoystickButton10;
								case 65:
									return GONetInputSync.GetKey_JoystickButton11;
								case 66:
									return GONetInputSync.GetKey_JoystickButton12;
								case 67:
									return GONetInputSync.GetKey_JoystickButton13;
								case 68:
									return GONetInputSync.GetKey_JoystickButton14;
								case 69:
									return GONetInputSync.GetKey_JoystickButton15;
								case 70:
									return GONetInputSync.GetKey_JoystickButton16;
								case 71:
									return GONetInputSync.GetKey_JoystickButton17;
								case 72:
									return GONetInputSync.GetKey_JoystickButton18;
								case 73:
									return GONetInputSync.GetKey_JoystickButton19;
								case 74:
									return GONetInputSync.GetKey_JoystickButton2;
								case 75:
									return GONetInputSync.GetKey_JoystickButton3;
								case 76:
									return GONetInputSync.GetKey_JoystickButton4;
								case 77:
									return GONetInputSync.GetKey_JoystickButton5;
								case 78:
									return GONetInputSync.GetKey_JoystickButton6;
								case 79:
									return GONetInputSync.GetKey_JoystickButton7;
								case 80:
									return GONetInputSync.GetKey_JoystickButton8;
								case 81:
									return GONetInputSync.GetKey_JoystickButton9;
								case 82:
									return GONetInputSync.GetKey_K;
								case 83:
									return GONetInputSync.GetKey_Keypad0;
								case 84:
									return GONetInputSync.GetKey_Keypad1;
								case 85:
									return GONetInputSync.GetKey_Keypad2;
								case 86:
									return GONetInputSync.GetKey_Keypad3;
								case 87:
									return GONetInputSync.GetKey_Keypad4;
								case 88:
									return GONetInputSync.GetKey_Keypad5;
								case 89:
									return GONetInputSync.GetKey_Keypad6;
								case 90:
									return GONetInputSync.GetKey_Keypad7;
								case 91:
									return GONetInputSync.GetKey_Keypad8;
								case 92:
									return GONetInputSync.GetKey_Keypad9;
								case 93:
									return GONetInputSync.GetKey_KeypadDivide;
								case 94:
									return GONetInputSync.GetKey_KeypadEnter;
								case 95:
									return GONetInputSync.GetKey_KeypadEquals;
								case 96:
									return GONetInputSync.GetKey_KeypadMinus;
								case 97:
									return GONetInputSync.GetKey_KeypadMultiply;
								case 98:
									return GONetInputSync.GetKey_KeypadPeriod;
								case 99:
									return GONetInputSync.GetKey_KeypadPlus;
								case 100:
									return GONetInputSync.GetKey_L;
								case 101:
									return GONetInputSync.GetKey_LeftAlt;
								case 102:
									return GONetInputSync.GetKey_LeftApple;
								case 103:
									return GONetInputSync.GetKey_LeftArrow;
								case 104:
									return GONetInputSync.GetKey_LeftBracket;
								case 105:
									return GONetInputSync.GetKey_LeftCommand;
								case 106:
									return GONetInputSync.GetKey_LeftControl;
								case 107:
									return GONetInputSync.GetKey_LeftCurlyBracket;
								case 108:
									return GONetInputSync.GetKey_LeftParen;
								case 109:
									return GONetInputSync.GetKey_LeftShift;
								case 110:
									return GONetInputSync.GetKey_LeftWindows;
								case 111:
									return GONetInputSync.GetKey_Less;
								case 112:
									return GONetInputSync.GetKey_M;
								case 113:
									return GONetInputSync.GetKey_Menu;
								case 114:
									return GONetInputSync.GetKey_Minus;
								case 115:
									return GONetInputSync.GetKey_Mouse0;
								case 116:
									return GONetInputSync.GetKey_Mouse1;
								case 117:
									return GONetInputSync.GetKey_Mouse2;
								case 118:
									return GONetInputSync.GetKey_Mouse3;
								case 119:
									return GONetInputSync.GetKey_Mouse4;
								case 120:
									return GONetInputSync.GetKey_Mouse5;
								case 121:
									return GONetInputSync.GetKey_Mouse6;
								case 122:
									return GONetInputSync.GetKey_N;
								case 123:
									return GONetInputSync.GetKey_None;
								case 124:
									return GONetInputSync.GetKey_Numlock;
								case 125:
									return GONetInputSync.GetKey_O;
								case 126:
									return GONetInputSync.GetKey_P;
								case 127:
									return GONetInputSync.GetKey_PageDown;
								case 128:
									return GONetInputSync.GetKey_PageUp;
								case 129:
									return GONetInputSync.GetKey_Pause;
								case 130:
									return GONetInputSync.GetKey_Percent;
								case 131:
									return GONetInputSync.GetKey_Period;
								case 132:
									return GONetInputSync.GetKey_Pipe;
								case 133:
									return GONetInputSync.GetKey_Plus;
								case 134:
									return GONetInputSync.GetKey_Print;
								case 135:
									return GONetInputSync.GetKey_Q;
								case 136:
									return GONetInputSync.GetKey_Question;
								case 137:
									return GONetInputSync.GetKey_Quote;
								case 138:
									return GONetInputSync.GetKey_R;
								case 139:
									return GONetInputSync.GetKey_Return;
								case 140:
									return GONetInputSync.GetKey_RightAlt;
								case 141:
									return GONetInputSync.GetKey_RightApple;
								case 142:
									return GONetInputSync.GetKey_RightArrow;
								case 143:
									return GONetInputSync.GetKey_RightBracket;
								case 144:
									return GONetInputSync.GetKey_RightCommand;
								case 145:
									return GONetInputSync.GetKey_RightControl;
								case 146:
									return GONetInputSync.GetKey_RightCurlyBracket;
								case 147:
									return GONetInputSync.GetKey_RightParen;
								case 148:
									return GONetInputSync.GetKey_RightShift;
								case 149:
									return GONetInputSync.GetKey_RightWindows;
								case 150:
									return GONetInputSync.GetKey_S;
								case 151:
									return GONetInputSync.GetKey_ScrollLock;
								case 152:
									return GONetInputSync.GetKey_Semicolon;
								case 153:
									return GONetInputSync.GetKey_Slash;
								case 154:
									return GONetInputSync.GetKey_Space;
								case 155:
									return GONetInputSync.GetKey_SysReq;
								case 156:
									return GONetInputSync.GetKey_T;
								case 157:
									return GONetInputSync.GetKey_Tab;
								case 158:
									return GONetInputSync.GetKey_Tilde;
								case 159:
									return GONetInputSync.GetKey_U;
								case 160:
									return GONetInputSync.GetKey_Underscore;
								case 161:
									return GONetInputSync.GetKey_UpArrow;
								case 162:
									return GONetInputSync.GetKey_V;
								case 163:
									return GONetInputSync.GetKey_W;
								case 164:
									return GONetInputSync.GetKey_X;
								case 165:
									return GONetInputSync.GetKey_Y;
								case 166:
									return GONetInputSync.GetKey_Z;
								case 167:
									return GONetInputSync.GetMouseButton_0;
								case 168:
									return GONetInputSync.GetMouseButton_1;
								case 169:
									return GONetInputSync.GetMouseButton_2;
								case 170:
									return GONetInputSync.mousePosition;
								case 171:
									return Transform.rotation;
								case 172:
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
			{ // GONetInputSync.GetKey_A
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_A);
							}
			{ // GONetInputSync.GetKey_Alpha0
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Alpha0);
							}
			{ // GONetInputSync.GetKey_Alpha1
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Alpha1);
							}
			{ // GONetInputSync.GetKey_Alpha2
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Alpha2);
							}
			{ // GONetInputSync.GetKey_Alpha3
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Alpha3);
							}
			{ // GONetInputSync.GetKey_Alpha4
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Alpha4);
							}
			{ // GONetInputSync.GetKey_Alpha5
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Alpha5);
							}
			{ // GONetInputSync.GetKey_Alpha6
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Alpha6);
							}
			{ // GONetInputSync.GetKey_Alpha7
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Alpha7);
							}
			{ // GONetInputSync.GetKey_Alpha8
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Alpha8);
							}
			{ // GONetInputSync.GetKey_Alpha9
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Alpha9);
							}
			{ // GONetInputSync.GetKey_AltGr
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_AltGr);
							}
			{ // GONetInputSync.GetKey_Ampersand
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Ampersand);
							}
			{ // GONetInputSync.GetKey_Asterisk
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Asterisk);
							}
			{ // GONetInputSync.GetKey_At
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_At);
							}
			{ // GONetInputSync.GetKey_B
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_B);
							}
			{ // GONetInputSync.GetKey_BackQuote
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_BackQuote);
							}
			{ // GONetInputSync.GetKey_Backslash
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Backslash);
							}
			{ // GONetInputSync.GetKey_Backspace
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Backspace);
							}
			{ // GONetInputSync.GetKey_Break
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Break);
							}
			{ // GONetInputSync.GetKey_C
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_C);
							}
			{ // GONetInputSync.GetKey_CapsLock
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_CapsLock);
							}
			{ // GONetInputSync.GetKey_Caret
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Caret);
							}
			{ // GONetInputSync.GetKey_Clear
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Clear);
							}
			{ // GONetInputSync.GetKey_Colon
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Colon);
							}
			{ // GONetInputSync.GetKey_Comma
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Comma);
							}
			{ // GONetInputSync.GetKey_D
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_D);
							}
			{ // GONetInputSync.GetKey_Delete
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Delete);
							}
			{ // GONetInputSync.GetKey_Dollar
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Dollar);
							}
			{ // GONetInputSync.GetKey_DoubleQuote
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_DoubleQuote);
							}
			{ // GONetInputSync.GetKey_DownArrow
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_DownArrow);
							}
			{ // GONetInputSync.GetKey_E
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_E);
							}
			{ // GONetInputSync.GetKey_End
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_End);
							}
			{ // GONetInputSync.GetKey_Equals
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Equals);
							}
			{ // GONetInputSync.GetKey_Escape
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Escape);
							}
			{ // GONetInputSync.GetKey_Exclaim
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Exclaim);
							}
			{ // GONetInputSync.GetKey_F
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_F);
							}
			{ // GONetInputSync.GetKey_F1
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_F1);
							}
			{ // GONetInputSync.GetKey_F10
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_F10);
							}
			{ // GONetInputSync.GetKey_F11
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_F11);
							}
			{ // GONetInputSync.GetKey_F12
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_F12);
							}
			{ // GONetInputSync.GetKey_F2
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_F2);
							}
			{ // GONetInputSync.GetKey_F3
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_F3);
							}
			{ // GONetInputSync.GetKey_F4
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_F4);
							}
			{ // GONetInputSync.GetKey_F5
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_F5);
							}
			{ // GONetInputSync.GetKey_F6
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_F6);
							}
			{ // GONetInputSync.GetKey_F7
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_F7);
							}
			{ // GONetInputSync.GetKey_F8
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_F8);
							}
			{ // GONetInputSync.GetKey_F9
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_F9);
							}
			{ // GONetInputSync.GetKey_G
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_G);
							}
			{ // GONetInputSync.GetKey_Greater
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Greater);
							}
			{ // GONetInputSync.GetKey_H
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_H);
							}
			{ // GONetInputSync.GetKey_Hash
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Hash);
							}
			{ // GONetInputSync.GetKey_Help
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Help);
							}
			{ // GONetInputSync.GetKey_Home
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Home);
							}
			{ // GONetInputSync.GetKey_I
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_I);
							}
			{ // GONetInputSync.GetKey_Insert
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Insert);
							}
			{ // GONetInputSync.GetKey_J
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_J);
							}
			{ // GONetInputSync.GetKey_JoystickButton0
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton0);
							}
			{ // GONetInputSync.GetKey_JoystickButton1
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton1);
							}
			{ // GONetInputSync.GetKey_JoystickButton10
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton10);
							}
			{ // GONetInputSync.GetKey_JoystickButton11
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton11);
							}
			{ // GONetInputSync.GetKey_JoystickButton12
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton12);
							}
			{ // GONetInputSync.GetKey_JoystickButton13
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton13);
							}
			{ // GONetInputSync.GetKey_JoystickButton14
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton14);
							}
			{ // GONetInputSync.GetKey_JoystickButton15
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton15);
							}
			{ // GONetInputSync.GetKey_JoystickButton16
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton16);
							}
			{ // GONetInputSync.GetKey_JoystickButton17
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton17);
							}
			{ // GONetInputSync.GetKey_JoystickButton18
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton18);
							}
			{ // GONetInputSync.GetKey_JoystickButton19
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton19);
							}
			{ // GONetInputSync.GetKey_JoystickButton2
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton2);
							}
			{ // GONetInputSync.GetKey_JoystickButton3
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton3);
							}
			{ // GONetInputSync.GetKey_JoystickButton4
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton4);
							}
			{ // GONetInputSync.GetKey_JoystickButton5
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton5);
							}
			{ // GONetInputSync.GetKey_JoystickButton6
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton6);
							}
			{ // GONetInputSync.GetKey_JoystickButton7
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton7);
							}
			{ // GONetInputSync.GetKey_JoystickButton8
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton8);
							}
			{ // GONetInputSync.GetKey_JoystickButton9
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton9);
							}
			{ // GONetInputSync.GetKey_K
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_K);
							}
			{ // GONetInputSync.GetKey_Keypad0
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Keypad0);
							}
			{ // GONetInputSync.GetKey_Keypad1
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Keypad1);
							}
			{ // GONetInputSync.GetKey_Keypad2
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Keypad2);
							}
			{ // GONetInputSync.GetKey_Keypad3
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Keypad3);
							}
			{ // GONetInputSync.GetKey_Keypad4
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Keypad4);
							}
			{ // GONetInputSync.GetKey_Keypad5
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Keypad5);
							}
			{ // GONetInputSync.GetKey_Keypad6
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Keypad6);
							}
			{ // GONetInputSync.GetKey_Keypad7
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Keypad7);
							}
			{ // GONetInputSync.GetKey_Keypad8
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Keypad8);
							}
			{ // GONetInputSync.GetKey_Keypad9
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Keypad9);
							}
			{ // GONetInputSync.GetKey_KeypadDivide
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_KeypadDivide);
							}
			{ // GONetInputSync.GetKey_KeypadEnter
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_KeypadEnter);
							}
			{ // GONetInputSync.GetKey_KeypadEquals
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_KeypadEquals);
							}
			{ // GONetInputSync.GetKey_KeypadMinus
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_KeypadMinus);
							}
			{ // GONetInputSync.GetKey_KeypadMultiply
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_KeypadMultiply);
							}
			{ // GONetInputSync.GetKey_KeypadPeriod
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_KeypadPeriod);
							}
			{ // GONetInputSync.GetKey_KeypadPlus
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_KeypadPlus);
							}
			{ // GONetInputSync.GetKey_L
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_L);
							}
			{ // GONetInputSync.GetKey_LeftAlt
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_LeftAlt);
							}
			{ // GONetInputSync.GetKey_LeftApple
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_LeftApple);
							}
			{ // GONetInputSync.GetKey_LeftArrow
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_LeftArrow);
							}
			{ // GONetInputSync.GetKey_LeftBracket
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_LeftBracket);
							}
			{ // GONetInputSync.GetKey_LeftCommand
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_LeftCommand);
							}
			{ // GONetInputSync.GetKey_LeftControl
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_LeftControl);
							}
			{ // GONetInputSync.GetKey_LeftCurlyBracket
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_LeftCurlyBracket);
							}
			{ // GONetInputSync.GetKey_LeftParen
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_LeftParen);
							}
			{ // GONetInputSync.GetKey_LeftShift
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_LeftShift);
							}
			{ // GONetInputSync.GetKey_LeftWindows
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_LeftWindows);
							}
			{ // GONetInputSync.GetKey_Less
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Less);
							}
			{ // GONetInputSync.GetKey_M
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_M);
							}
			{ // GONetInputSync.GetKey_Menu
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Menu);
							}
			{ // GONetInputSync.GetKey_Minus
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Minus);
							}
			{ // GONetInputSync.GetKey_Mouse0
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Mouse0);
							}
			{ // GONetInputSync.GetKey_Mouse1
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Mouse1);
							}
			{ // GONetInputSync.GetKey_Mouse2
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Mouse2);
							}
			{ // GONetInputSync.GetKey_Mouse3
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Mouse3);
							}
			{ // GONetInputSync.GetKey_Mouse4
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Mouse4);
							}
			{ // GONetInputSync.GetKey_Mouse5
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Mouse5);
							}
			{ // GONetInputSync.GetKey_Mouse6
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Mouse6);
							}
			{ // GONetInputSync.GetKey_N
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_N);
							}
			{ // GONetInputSync.GetKey_None
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_None);
							}
			{ // GONetInputSync.GetKey_Numlock
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Numlock);
							}
			{ // GONetInputSync.GetKey_O
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_O);
							}
			{ // GONetInputSync.GetKey_P
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_P);
							}
			{ // GONetInputSync.GetKey_PageDown
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_PageDown);
							}
			{ // GONetInputSync.GetKey_PageUp
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_PageUp);
							}
			{ // GONetInputSync.GetKey_Pause
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Pause);
							}
			{ // GONetInputSync.GetKey_Percent
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Percent);
							}
			{ // GONetInputSync.GetKey_Period
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Period);
							}
			{ // GONetInputSync.GetKey_Pipe
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Pipe);
							}
			{ // GONetInputSync.GetKey_Plus
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Plus);
							}
			{ // GONetInputSync.GetKey_Print
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Print);
							}
			{ // GONetInputSync.GetKey_Q
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Q);
							}
			{ // GONetInputSync.GetKey_Question
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Question);
							}
			{ // GONetInputSync.GetKey_Quote
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Quote);
							}
			{ // GONetInputSync.GetKey_R
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_R);
							}
			{ // GONetInputSync.GetKey_Return
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Return);
							}
			{ // GONetInputSync.GetKey_RightAlt
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_RightAlt);
							}
			{ // GONetInputSync.GetKey_RightApple
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_RightApple);
							}
			{ // GONetInputSync.GetKey_RightArrow
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_RightArrow);
							}
			{ // GONetInputSync.GetKey_RightBracket
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_RightBracket);
							}
			{ // GONetInputSync.GetKey_RightCommand
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_RightCommand);
							}
			{ // GONetInputSync.GetKey_RightControl
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_RightControl);
							}
			{ // GONetInputSync.GetKey_RightCurlyBracket
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_RightCurlyBracket);
							}
			{ // GONetInputSync.GetKey_RightParen
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_RightParen);
							}
			{ // GONetInputSync.GetKey_RightShift
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_RightShift);
							}
			{ // GONetInputSync.GetKey_RightWindows
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_RightWindows);
							}
			{ // GONetInputSync.GetKey_S
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_S);
							}
			{ // GONetInputSync.GetKey_ScrollLock
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_ScrollLock);
							}
			{ // GONetInputSync.GetKey_Semicolon
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Semicolon);
							}
			{ // GONetInputSync.GetKey_Slash
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Slash);
							}
			{ // GONetInputSync.GetKey_Space
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Space);
							}
			{ // GONetInputSync.GetKey_SysReq
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_SysReq);
							}
			{ // GONetInputSync.GetKey_T
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_T);
							}
			{ // GONetInputSync.GetKey_Tab
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Tab);
							}
			{ // GONetInputSync.GetKey_Tilde
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Tilde);
							}
			{ // GONetInputSync.GetKey_U
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_U);
							}
			{ // GONetInputSync.GetKey_Underscore
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Underscore);
							}
			{ // GONetInputSync.GetKey_UpArrow
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_UpArrow);
							}
			{ // GONetInputSync.GetKey_V
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_V);
							}
			{ // GONetInputSync.GetKey_W
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_W);
							}
			{ // GONetInputSync.GetKey_X
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_X);
							}
			{ // GONetInputSync.GetKey_Y
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Y);
							}
			{ // GONetInputSync.GetKey_Z
								bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Z);
							}
			{ // GONetInputSync.GetMouseButton_0
								bitStream_appendTo.WriteBit(GONetInputSync.GetMouseButton_0);
							}
			{ // GONetInputSync.GetMouseButton_1
								bitStream_appendTo.WriteBit(GONetInputSync.GetMouseButton_1);
							}
			{ // GONetInputSync.GetMouseButton_2
								bitStream_appendTo.WriteBit(GONetInputSync.GetMouseButton_2);
							}
			{ // GONetInputSync.mousePosition
					IGONetAutoMagicalSync_CustomSerializer customSerializer = GONetAutoMagicalSyncAttribute.GetCustomSerializer<Vector2Serializer>(); // TODO need to cache this locally instead of having to lookup each time
					customSerializer.Serialize(bitStream_appendTo, gonetParticipant, GONetInputSync.mousePosition);
			}
			{ // Transform.rotation
				IGONetAutoMagicalSync_CustomSerializer customSerializer = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.QuaternionSerializer>(); // TODO need to cache this locally instead of having to lookup each time
				customSerializer.Serialize(bitStream_appendTo, gonetParticipant, Transform.rotation);
			}
			{ // Transform.position
				IGONetAutoMagicalSync_CustomSerializer customSerializer = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.Vector3Serializer>(); // TODO need to cache this locally instead of having to lookup each time
				customSerializer.Serialize(bitStream_appendTo, gonetParticipant, Transform.position);
			}
        }

        internal override void SerializeSingle(Utils.BitByBitByteArrayBuilder bitStream_appendTo, byte singleIndex)
        {
			switch (singleIndex)
			{
				case 0:
				{ // GONetParticipant.GONetId
					IGONetAutoMagicalSync_CustomSerializer customSerializer = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.GONetParticipant.GONetId_InitialAssignment_CustomSerializer>(); // TODO need to cache this locally instead of having to lookup each time
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
				{ // GONetInputSync.GetKey_A
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_A);
								}
				break;

				case 5:
				{ // GONetInputSync.GetKey_Alpha0
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Alpha0);
								}
				break;

				case 6:
				{ // GONetInputSync.GetKey_Alpha1
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Alpha1);
								}
				break;

				case 7:
				{ // GONetInputSync.GetKey_Alpha2
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Alpha2);
								}
				break;

				case 8:
				{ // GONetInputSync.GetKey_Alpha3
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Alpha3);
								}
				break;

				case 9:
				{ // GONetInputSync.GetKey_Alpha4
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Alpha4);
								}
				break;

				case 10:
				{ // GONetInputSync.GetKey_Alpha5
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Alpha5);
								}
				break;

				case 11:
				{ // GONetInputSync.GetKey_Alpha6
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Alpha6);
								}
				break;

				case 12:
				{ // GONetInputSync.GetKey_Alpha7
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Alpha7);
								}
				break;

				case 13:
				{ // GONetInputSync.GetKey_Alpha8
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Alpha8);
								}
				break;

				case 14:
				{ // GONetInputSync.GetKey_Alpha9
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Alpha9);
								}
				break;

				case 15:
				{ // GONetInputSync.GetKey_AltGr
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_AltGr);
								}
				break;

				case 16:
				{ // GONetInputSync.GetKey_Ampersand
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Ampersand);
								}
				break;

				case 17:
				{ // GONetInputSync.GetKey_Asterisk
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Asterisk);
								}
				break;

				case 18:
				{ // GONetInputSync.GetKey_At
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_At);
								}
				break;

				case 19:
				{ // GONetInputSync.GetKey_B
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_B);
								}
				break;

				case 20:
				{ // GONetInputSync.GetKey_BackQuote
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_BackQuote);
								}
				break;

				case 21:
				{ // GONetInputSync.GetKey_Backslash
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Backslash);
								}
				break;

				case 22:
				{ // GONetInputSync.GetKey_Backspace
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Backspace);
								}
				break;

				case 23:
				{ // GONetInputSync.GetKey_Break
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Break);
								}
				break;

				case 24:
				{ // GONetInputSync.GetKey_C
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_C);
								}
				break;

				case 25:
				{ // GONetInputSync.GetKey_CapsLock
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_CapsLock);
								}
				break;

				case 26:
				{ // GONetInputSync.GetKey_Caret
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Caret);
								}
				break;

				case 27:
				{ // GONetInputSync.GetKey_Clear
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Clear);
								}
				break;

				case 28:
				{ // GONetInputSync.GetKey_Colon
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Colon);
								}
				break;

				case 29:
				{ // GONetInputSync.GetKey_Comma
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Comma);
								}
				break;

				case 30:
				{ // GONetInputSync.GetKey_D
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_D);
								}
				break;

				case 31:
				{ // GONetInputSync.GetKey_Delete
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Delete);
								}
				break;

				case 32:
				{ // GONetInputSync.GetKey_Dollar
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Dollar);
								}
				break;

				case 33:
				{ // GONetInputSync.GetKey_DoubleQuote
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_DoubleQuote);
								}
				break;

				case 34:
				{ // GONetInputSync.GetKey_DownArrow
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_DownArrow);
								}
				break;

				case 35:
				{ // GONetInputSync.GetKey_E
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_E);
								}
				break;

				case 36:
				{ // GONetInputSync.GetKey_End
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_End);
								}
				break;

				case 37:
				{ // GONetInputSync.GetKey_Equals
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Equals);
								}
				break;

				case 38:
				{ // GONetInputSync.GetKey_Escape
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Escape);
								}
				break;

				case 39:
				{ // GONetInputSync.GetKey_Exclaim
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Exclaim);
								}
				break;

				case 40:
				{ // GONetInputSync.GetKey_F
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_F);
								}
				break;

				case 41:
				{ // GONetInputSync.GetKey_F1
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_F1);
								}
				break;

				case 42:
				{ // GONetInputSync.GetKey_F10
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_F10);
								}
				break;

				case 43:
				{ // GONetInputSync.GetKey_F11
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_F11);
								}
				break;

				case 44:
				{ // GONetInputSync.GetKey_F12
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_F12);
								}
				break;

				case 45:
				{ // GONetInputSync.GetKey_F2
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_F2);
								}
				break;

				case 46:
				{ // GONetInputSync.GetKey_F3
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_F3);
								}
				break;

				case 47:
				{ // GONetInputSync.GetKey_F4
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_F4);
								}
				break;

				case 48:
				{ // GONetInputSync.GetKey_F5
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_F5);
								}
				break;

				case 49:
				{ // GONetInputSync.GetKey_F6
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_F6);
								}
				break;

				case 50:
				{ // GONetInputSync.GetKey_F7
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_F7);
								}
				break;

				case 51:
				{ // GONetInputSync.GetKey_F8
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_F8);
								}
				break;

				case 52:
				{ // GONetInputSync.GetKey_F9
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_F9);
								}
				break;

				case 53:
				{ // GONetInputSync.GetKey_G
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_G);
								}
				break;

				case 54:
				{ // GONetInputSync.GetKey_Greater
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Greater);
								}
				break;

				case 55:
				{ // GONetInputSync.GetKey_H
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_H);
								}
				break;

				case 56:
				{ // GONetInputSync.GetKey_Hash
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Hash);
								}
				break;

				case 57:
				{ // GONetInputSync.GetKey_Help
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Help);
								}
				break;

				case 58:
				{ // GONetInputSync.GetKey_Home
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Home);
								}
				break;

				case 59:
				{ // GONetInputSync.GetKey_I
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_I);
								}
				break;

				case 60:
				{ // GONetInputSync.GetKey_Insert
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Insert);
								}
				break;

				case 61:
				{ // GONetInputSync.GetKey_J
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_J);
								}
				break;

				case 62:
				{ // GONetInputSync.GetKey_JoystickButton0
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton0);
								}
				break;

				case 63:
				{ // GONetInputSync.GetKey_JoystickButton1
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton1);
								}
				break;

				case 64:
				{ // GONetInputSync.GetKey_JoystickButton10
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton10);
								}
				break;

				case 65:
				{ // GONetInputSync.GetKey_JoystickButton11
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton11);
								}
				break;

				case 66:
				{ // GONetInputSync.GetKey_JoystickButton12
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton12);
								}
				break;

				case 67:
				{ // GONetInputSync.GetKey_JoystickButton13
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton13);
								}
				break;

				case 68:
				{ // GONetInputSync.GetKey_JoystickButton14
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton14);
								}
				break;

				case 69:
				{ // GONetInputSync.GetKey_JoystickButton15
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton15);
								}
				break;

				case 70:
				{ // GONetInputSync.GetKey_JoystickButton16
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton16);
								}
				break;

				case 71:
				{ // GONetInputSync.GetKey_JoystickButton17
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton17);
								}
				break;

				case 72:
				{ // GONetInputSync.GetKey_JoystickButton18
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton18);
								}
				break;

				case 73:
				{ // GONetInputSync.GetKey_JoystickButton19
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton19);
								}
				break;

				case 74:
				{ // GONetInputSync.GetKey_JoystickButton2
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton2);
								}
				break;

				case 75:
				{ // GONetInputSync.GetKey_JoystickButton3
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton3);
								}
				break;

				case 76:
				{ // GONetInputSync.GetKey_JoystickButton4
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton4);
								}
				break;

				case 77:
				{ // GONetInputSync.GetKey_JoystickButton5
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton5);
								}
				break;

				case 78:
				{ // GONetInputSync.GetKey_JoystickButton6
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton6);
								}
				break;

				case 79:
				{ // GONetInputSync.GetKey_JoystickButton7
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton7);
								}
				break;

				case 80:
				{ // GONetInputSync.GetKey_JoystickButton8
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton8);
								}
				break;

				case 81:
				{ // GONetInputSync.GetKey_JoystickButton9
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_JoystickButton9);
								}
				break;

				case 82:
				{ // GONetInputSync.GetKey_K
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_K);
								}
				break;

				case 83:
				{ // GONetInputSync.GetKey_Keypad0
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Keypad0);
								}
				break;

				case 84:
				{ // GONetInputSync.GetKey_Keypad1
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Keypad1);
								}
				break;

				case 85:
				{ // GONetInputSync.GetKey_Keypad2
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Keypad2);
								}
				break;

				case 86:
				{ // GONetInputSync.GetKey_Keypad3
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Keypad3);
								}
				break;

				case 87:
				{ // GONetInputSync.GetKey_Keypad4
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Keypad4);
								}
				break;

				case 88:
				{ // GONetInputSync.GetKey_Keypad5
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Keypad5);
								}
				break;

				case 89:
				{ // GONetInputSync.GetKey_Keypad6
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Keypad6);
								}
				break;

				case 90:
				{ // GONetInputSync.GetKey_Keypad7
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Keypad7);
								}
				break;

				case 91:
				{ // GONetInputSync.GetKey_Keypad8
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Keypad8);
								}
				break;

				case 92:
				{ // GONetInputSync.GetKey_Keypad9
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Keypad9);
								}
				break;

				case 93:
				{ // GONetInputSync.GetKey_KeypadDivide
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_KeypadDivide);
								}
				break;

				case 94:
				{ // GONetInputSync.GetKey_KeypadEnter
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_KeypadEnter);
								}
				break;

				case 95:
				{ // GONetInputSync.GetKey_KeypadEquals
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_KeypadEquals);
								}
				break;

				case 96:
				{ // GONetInputSync.GetKey_KeypadMinus
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_KeypadMinus);
								}
				break;

				case 97:
				{ // GONetInputSync.GetKey_KeypadMultiply
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_KeypadMultiply);
								}
				break;

				case 98:
				{ // GONetInputSync.GetKey_KeypadPeriod
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_KeypadPeriod);
								}
				break;

				case 99:
				{ // GONetInputSync.GetKey_KeypadPlus
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_KeypadPlus);
								}
				break;

				case 100:
				{ // GONetInputSync.GetKey_L
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_L);
								}
				break;

				case 101:
				{ // GONetInputSync.GetKey_LeftAlt
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_LeftAlt);
								}
				break;

				case 102:
				{ // GONetInputSync.GetKey_LeftApple
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_LeftApple);
								}
				break;

				case 103:
				{ // GONetInputSync.GetKey_LeftArrow
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_LeftArrow);
								}
				break;

				case 104:
				{ // GONetInputSync.GetKey_LeftBracket
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_LeftBracket);
								}
				break;

				case 105:
				{ // GONetInputSync.GetKey_LeftCommand
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_LeftCommand);
								}
				break;

				case 106:
				{ // GONetInputSync.GetKey_LeftControl
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_LeftControl);
								}
				break;

				case 107:
				{ // GONetInputSync.GetKey_LeftCurlyBracket
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_LeftCurlyBracket);
								}
				break;

				case 108:
				{ // GONetInputSync.GetKey_LeftParen
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_LeftParen);
								}
				break;

				case 109:
				{ // GONetInputSync.GetKey_LeftShift
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_LeftShift);
								}
				break;

				case 110:
				{ // GONetInputSync.GetKey_LeftWindows
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_LeftWindows);
								}
				break;

				case 111:
				{ // GONetInputSync.GetKey_Less
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Less);
								}
				break;

				case 112:
				{ // GONetInputSync.GetKey_M
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_M);
								}
				break;

				case 113:
				{ // GONetInputSync.GetKey_Menu
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Menu);
								}
				break;

				case 114:
				{ // GONetInputSync.GetKey_Minus
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Minus);
								}
				break;

				case 115:
				{ // GONetInputSync.GetKey_Mouse0
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Mouse0);
								}
				break;

				case 116:
				{ // GONetInputSync.GetKey_Mouse1
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Mouse1);
								}
				break;

				case 117:
				{ // GONetInputSync.GetKey_Mouse2
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Mouse2);
								}
				break;

				case 118:
				{ // GONetInputSync.GetKey_Mouse3
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Mouse3);
								}
				break;

				case 119:
				{ // GONetInputSync.GetKey_Mouse4
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Mouse4);
								}
				break;

				case 120:
				{ // GONetInputSync.GetKey_Mouse5
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Mouse5);
								}
				break;

				case 121:
				{ // GONetInputSync.GetKey_Mouse6
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Mouse6);
								}
				break;

				case 122:
				{ // GONetInputSync.GetKey_N
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_N);
								}
				break;

				case 123:
				{ // GONetInputSync.GetKey_None
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_None);
								}
				break;

				case 124:
				{ // GONetInputSync.GetKey_Numlock
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Numlock);
								}
				break;

				case 125:
				{ // GONetInputSync.GetKey_O
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_O);
								}
				break;

				case 126:
				{ // GONetInputSync.GetKey_P
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_P);
								}
				break;

				case 127:
				{ // GONetInputSync.GetKey_PageDown
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_PageDown);
								}
				break;

				case 128:
				{ // GONetInputSync.GetKey_PageUp
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_PageUp);
								}
				break;

				case 129:
				{ // GONetInputSync.GetKey_Pause
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Pause);
								}
				break;

				case 130:
				{ // GONetInputSync.GetKey_Percent
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Percent);
								}
				break;

				case 131:
				{ // GONetInputSync.GetKey_Period
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Period);
								}
				break;

				case 132:
				{ // GONetInputSync.GetKey_Pipe
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Pipe);
								}
				break;

				case 133:
				{ // GONetInputSync.GetKey_Plus
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Plus);
								}
				break;

				case 134:
				{ // GONetInputSync.GetKey_Print
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Print);
								}
				break;

				case 135:
				{ // GONetInputSync.GetKey_Q
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Q);
								}
				break;

				case 136:
				{ // GONetInputSync.GetKey_Question
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Question);
								}
				break;

				case 137:
				{ // GONetInputSync.GetKey_Quote
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Quote);
								}
				break;

				case 138:
				{ // GONetInputSync.GetKey_R
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_R);
								}
				break;

				case 139:
				{ // GONetInputSync.GetKey_Return
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Return);
								}
				break;

				case 140:
				{ // GONetInputSync.GetKey_RightAlt
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_RightAlt);
								}
				break;

				case 141:
				{ // GONetInputSync.GetKey_RightApple
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_RightApple);
								}
				break;

				case 142:
				{ // GONetInputSync.GetKey_RightArrow
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_RightArrow);
								}
				break;

				case 143:
				{ // GONetInputSync.GetKey_RightBracket
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_RightBracket);
								}
				break;

				case 144:
				{ // GONetInputSync.GetKey_RightCommand
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_RightCommand);
								}
				break;

				case 145:
				{ // GONetInputSync.GetKey_RightControl
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_RightControl);
								}
				break;

				case 146:
				{ // GONetInputSync.GetKey_RightCurlyBracket
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_RightCurlyBracket);
								}
				break;

				case 147:
				{ // GONetInputSync.GetKey_RightParen
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_RightParen);
								}
				break;

				case 148:
				{ // GONetInputSync.GetKey_RightShift
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_RightShift);
								}
				break;

				case 149:
				{ // GONetInputSync.GetKey_RightWindows
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_RightWindows);
								}
				break;

				case 150:
				{ // GONetInputSync.GetKey_S
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_S);
								}
				break;

				case 151:
				{ // GONetInputSync.GetKey_ScrollLock
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_ScrollLock);
								}
				break;

				case 152:
				{ // GONetInputSync.GetKey_Semicolon
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Semicolon);
								}
				break;

				case 153:
				{ // GONetInputSync.GetKey_Slash
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Slash);
								}
				break;

				case 154:
				{ // GONetInputSync.GetKey_Space
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Space);
								}
				break;

				case 155:
				{ // GONetInputSync.GetKey_SysReq
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_SysReq);
								}
				break;

				case 156:
				{ // GONetInputSync.GetKey_T
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_T);
								}
				break;

				case 157:
				{ // GONetInputSync.GetKey_Tab
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Tab);
								}
				break;

				case 158:
				{ // GONetInputSync.GetKey_Tilde
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Tilde);
								}
				break;

				case 159:
				{ // GONetInputSync.GetKey_U
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_U);
								}
				break;

				case 160:
				{ // GONetInputSync.GetKey_Underscore
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Underscore);
								}
				break;

				case 161:
				{ // GONetInputSync.GetKey_UpArrow
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_UpArrow);
								}
				break;

				case 162:
				{ // GONetInputSync.GetKey_V
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_V);
								}
				break;

				case 163:
				{ // GONetInputSync.GetKey_W
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_W);
								}
				break;

				case 164:
				{ // GONetInputSync.GetKey_X
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_X);
								}
				break;

				case 165:
				{ // GONetInputSync.GetKey_Y
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Y);
								}
				break;

				case 166:
				{ // GONetInputSync.GetKey_Z
									bitStream_appendTo.WriteBit(GONetInputSync.GetKey_Z);
								}
				break;

				case 167:
				{ // GONetInputSync.GetMouseButton_0
									bitStream_appendTo.WriteBit(GONetInputSync.GetMouseButton_0);
								}
				break;

				case 168:
				{ // GONetInputSync.GetMouseButton_1
									bitStream_appendTo.WriteBit(GONetInputSync.GetMouseButton_1);
								}
				break;

				case 169:
				{ // GONetInputSync.GetMouseButton_2
									bitStream_appendTo.WriteBit(GONetInputSync.GetMouseButton_2);
								}
				break;

				case 170:
				{ // GONetInputSync.mousePosition
					IGONetAutoMagicalSync_CustomSerializer customSerializer = GONetAutoMagicalSyncAttribute.GetCustomSerializer<Vector2Serializer>(); // TODO need to cache this locally instead of having to lookup each time
					customSerializer.Serialize(bitStream_appendTo, gonetParticipant, GONetInputSync.mousePosition);
				}
				break;

				case 171:
				{ // Transform.rotation
					IGONetAutoMagicalSync_CustomSerializer customSerializer = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.QuaternionSerializer>(); // TODO need to cache this locally instead of having to lookup each time
					customSerializer.Serialize(bitStream_appendTo, gonetParticipant, Transform.rotation);
				}
				break;

				case 172:
				{ // Transform.position
					IGONetAutoMagicalSync_CustomSerializer customSerializer = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.Vector3Serializer>(); // TODO need to cache this locally instead of having to lookup each time
					customSerializer.Serialize(bitStream_appendTo, gonetParticipant, Transform.position);
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
			{ // GONetInputSync.GetKey_A
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_A = value;
							}
			{ // GONetInputSync.GetKey_Alpha0
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Alpha0 = value;
							}
			{ // GONetInputSync.GetKey_Alpha1
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Alpha1 = value;
							}
			{ // GONetInputSync.GetKey_Alpha2
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Alpha2 = value;
							}
			{ // GONetInputSync.GetKey_Alpha3
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Alpha3 = value;
							}
			{ // GONetInputSync.GetKey_Alpha4
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Alpha4 = value;
							}
			{ // GONetInputSync.GetKey_Alpha5
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Alpha5 = value;
							}
			{ // GONetInputSync.GetKey_Alpha6
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Alpha6 = value;
							}
			{ // GONetInputSync.GetKey_Alpha7
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Alpha7 = value;
							}
			{ // GONetInputSync.GetKey_Alpha8
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Alpha8 = value;
							}
			{ // GONetInputSync.GetKey_Alpha9
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Alpha9 = value;
							}
			{ // GONetInputSync.GetKey_AltGr
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_AltGr = value;
							}
			{ // GONetInputSync.GetKey_Ampersand
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Ampersand = value;
							}
			{ // GONetInputSync.GetKey_Asterisk
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Asterisk = value;
							}
			{ // GONetInputSync.GetKey_At
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_At = value;
							}
			{ // GONetInputSync.GetKey_B
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_B = value;
							}
			{ // GONetInputSync.GetKey_BackQuote
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_BackQuote = value;
							}
			{ // GONetInputSync.GetKey_Backslash
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Backslash = value;
							}
			{ // GONetInputSync.GetKey_Backspace
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Backspace = value;
							}
			{ // GONetInputSync.GetKey_Break
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Break = value;
							}
			{ // GONetInputSync.GetKey_C
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_C = value;
							}
			{ // GONetInputSync.GetKey_CapsLock
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_CapsLock = value;
							}
			{ // GONetInputSync.GetKey_Caret
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Caret = value;
							}
			{ // GONetInputSync.GetKey_Clear
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Clear = value;
							}
			{ // GONetInputSync.GetKey_Colon
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Colon = value;
							}
			{ // GONetInputSync.GetKey_Comma
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Comma = value;
							}
			{ // GONetInputSync.GetKey_D
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_D = value;
							}
			{ // GONetInputSync.GetKey_Delete
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Delete = value;
							}
			{ // GONetInputSync.GetKey_Dollar
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Dollar = value;
							}
			{ // GONetInputSync.GetKey_DoubleQuote
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_DoubleQuote = value;
							}
			{ // GONetInputSync.GetKey_DownArrow
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_DownArrow = value;
							}
			{ // GONetInputSync.GetKey_E
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_E = value;
							}
			{ // GONetInputSync.GetKey_End
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_End = value;
							}
			{ // GONetInputSync.GetKey_Equals
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Equals = value;
							}
			{ // GONetInputSync.GetKey_Escape
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Escape = value;
							}
			{ // GONetInputSync.GetKey_Exclaim
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Exclaim = value;
							}
			{ // GONetInputSync.GetKey_F
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_F = value;
							}
			{ // GONetInputSync.GetKey_F1
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_F1 = value;
							}
			{ // GONetInputSync.GetKey_F10
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_F10 = value;
							}
			{ // GONetInputSync.GetKey_F11
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_F11 = value;
							}
			{ // GONetInputSync.GetKey_F12
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_F12 = value;
							}
			{ // GONetInputSync.GetKey_F2
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_F2 = value;
							}
			{ // GONetInputSync.GetKey_F3
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_F3 = value;
							}
			{ // GONetInputSync.GetKey_F4
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_F4 = value;
							}
			{ // GONetInputSync.GetKey_F5
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_F5 = value;
							}
			{ // GONetInputSync.GetKey_F6
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_F6 = value;
							}
			{ // GONetInputSync.GetKey_F7
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_F7 = value;
							}
			{ // GONetInputSync.GetKey_F8
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_F8 = value;
							}
			{ // GONetInputSync.GetKey_F9
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_F9 = value;
							}
			{ // GONetInputSync.GetKey_G
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_G = value;
							}
			{ // GONetInputSync.GetKey_Greater
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Greater = value;
							}
			{ // GONetInputSync.GetKey_H
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_H = value;
							}
			{ // GONetInputSync.GetKey_Hash
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Hash = value;
							}
			{ // GONetInputSync.GetKey_Help
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Help = value;
							}
			{ // GONetInputSync.GetKey_Home
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Home = value;
							}
			{ // GONetInputSync.GetKey_I
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_I = value;
							}
			{ // GONetInputSync.GetKey_Insert
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Insert = value;
							}
			{ // GONetInputSync.GetKey_J
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_J = value;
							}
			{ // GONetInputSync.GetKey_JoystickButton0
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_JoystickButton0 = value;
							}
			{ // GONetInputSync.GetKey_JoystickButton1
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_JoystickButton1 = value;
							}
			{ // GONetInputSync.GetKey_JoystickButton10
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_JoystickButton10 = value;
							}
			{ // GONetInputSync.GetKey_JoystickButton11
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_JoystickButton11 = value;
							}
			{ // GONetInputSync.GetKey_JoystickButton12
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_JoystickButton12 = value;
							}
			{ // GONetInputSync.GetKey_JoystickButton13
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_JoystickButton13 = value;
							}
			{ // GONetInputSync.GetKey_JoystickButton14
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_JoystickButton14 = value;
							}
			{ // GONetInputSync.GetKey_JoystickButton15
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_JoystickButton15 = value;
							}
			{ // GONetInputSync.GetKey_JoystickButton16
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_JoystickButton16 = value;
							}
			{ // GONetInputSync.GetKey_JoystickButton17
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_JoystickButton17 = value;
							}
			{ // GONetInputSync.GetKey_JoystickButton18
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_JoystickButton18 = value;
							}
			{ // GONetInputSync.GetKey_JoystickButton19
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_JoystickButton19 = value;
							}
			{ // GONetInputSync.GetKey_JoystickButton2
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_JoystickButton2 = value;
							}
			{ // GONetInputSync.GetKey_JoystickButton3
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_JoystickButton3 = value;
							}
			{ // GONetInputSync.GetKey_JoystickButton4
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_JoystickButton4 = value;
							}
			{ // GONetInputSync.GetKey_JoystickButton5
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_JoystickButton5 = value;
							}
			{ // GONetInputSync.GetKey_JoystickButton6
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_JoystickButton6 = value;
							}
			{ // GONetInputSync.GetKey_JoystickButton7
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_JoystickButton7 = value;
							}
			{ // GONetInputSync.GetKey_JoystickButton8
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_JoystickButton8 = value;
							}
			{ // GONetInputSync.GetKey_JoystickButton9
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_JoystickButton9 = value;
							}
			{ // GONetInputSync.GetKey_K
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_K = value;
							}
			{ // GONetInputSync.GetKey_Keypad0
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Keypad0 = value;
							}
			{ // GONetInputSync.GetKey_Keypad1
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Keypad1 = value;
							}
			{ // GONetInputSync.GetKey_Keypad2
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Keypad2 = value;
							}
			{ // GONetInputSync.GetKey_Keypad3
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Keypad3 = value;
							}
			{ // GONetInputSync.GetKey_Keypad4
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Keypad4 = value;
							}
			{ // GONetInputSync.GetKey_Keypad5
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Keypad5 = value;
							}
			{ // GONetInputSync.GetKey_Keypad6
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Keypad6 = value;
							}
			{ // GONetInputSync.GetKey_Keypad7
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Keypad7 = value;
							}
			{ // GONetInputSync.GetKey_Keypad8
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Keypad8 = value;
							}
			{ // GONetInputSync.GetKey_Keypad9
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Keypad9 = value;
							}
			{ // GONetInputSync.GetKey_KeypadDivide
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_KeypadDivide = value;
							}
			{ // GONetInputSync.GetKey_KeypadEnter
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_KeypadEnter = value;
							}
			{ // GONetInputSync.GetKey_KeypadEquals
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_KeypadEquals = value;
							}
			{ // GONetInputSync.GetKey_KeypadMinus
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_KeypadMinus = value;
							}
			{ // GONetInputSync.GetKey_KeypadMultiply
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_KeypadMultiply = value;
							}
			{ // GONetInputSync.GetKey_KeypadPeriod
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_KeypadPeriod = value;
							}
			{ // GONetInputSync.GetKey_KeypadPlus
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_KeypadPlus = value;
							}
			{ // GONetInputSync.GetKey_L
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_L = value;
							}
			{ // GONetInputSync.GetKey_LeftAlt
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_LeftAlt = value;
							}
			{ // GONetInputSync.GetKey_LeftApple
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_LeftApple = value;
							}
			{ // GONetInputSync.GetKey_LeftArrow
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_LeftArrow = value;
							}
			{ // GONetInputSync.GetKey_LeftBracket
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_LeftBracket = value;
							}
			{ // GONetInputSync.GetKey_LeftCommand
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_LeftCommand = value;
							}
			{ // GONetInputSync.GetKey_LeftControl
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_LeftControl = value;
							}
			{ // GONetInputSync.GetKey_LeftCurlyBracket
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_LeftCurlyBracket = value;
							}
			{ // GONetInputSync.GetKey_LeftParen
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_LeftParen = value;
							}
			{ // GONetInputSync.GetKey_LeftShift
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_LeftShift = value;
							}
			{ // GONetInputSync.GetKey_LeftWindows
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_LeftWindows = value;
							}
			{ // GONetInputSync.GetKey_Less
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Less = value;
							}
			{ // GONetInputSync.GetKey_M
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_M = value;
							}
			{ // GONetInputSync.GetKey_Menu
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Menu = value;
							}
			{ // GONetInputSync.GetKey_Minus
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Minus = value;
							}
			{ // GONetInputSync.GetKey_Mouse0
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Mouse0 = value;
							}
			{ // GONetInputSync.GetKey_Mouse1
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Mouse1 = value;
							}
			{ // GONetInputSync.GetKey_Mouse2
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Mouse2 = value;
							}
			{ // GONetInputSync.GetKey_Mouse3
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Mouse3 = value;
							}
			{ // GONetInputSync.GetKey_Mouse4
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Mouse4 = value;
							}
			{ // GONetInputSync.GetKey_Mouse5
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Mouse5 = value;
							}
			{ // GONetInputSync.GetKey_Mouse6
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Mouse6 = value;
							}
			{ // GONetInputSync.GetKey_N
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_N = value;
							}
			{ // GONetInputSync.GetKey_None
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_None = value;
							}
			{ // GONetInputSync.GetKey_Numlock
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Numlock = value;
							}
			{ // GONetInputSync.GetKey_O
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_O = value;
							}
			{ // GONetInputSync.GetKey_P
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_P = value;
							}
			{ // GONetInputSync.GetKey_PageDown
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_PageDown = value;
							}
			{ // GONetInputSync.GetKey_PageUp
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_PageUp = value;
							}
			{ // GONetInputSync.GetKey_Pause
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Pause = value;
							}
			{ // GONetInputSync.GetKey_Percent
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Percent = value;
							}
			{ // GONetInputSync.GetKey_Period
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Period = value;
							}
			{ // GONetInputSync.GetKey_Pipe
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Pipe = value;
							}
			{ // GONetInputSync.GetKey_Plus
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Plus = value;
							}
			{ // GONetInputSync.GetKey_Print
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Print = value;
							}
			{ // GONetInputSync.GetKey_Q
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Q = value;
							}
			{ // GONetInputSync.GetKey_Question
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Question = value;
							}
			{ // GONetInputSync.GetKey_Quote
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Quote = value;
							}
			{ // GONetInputSync.GetKey_R
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_R = value;
							}
			{ // GONetInputSync.GetKey_Return
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Return = value;
							}
			{ // GONetInputSync.GetKey_RightAlt
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_RightAlt = value;
							}
			{ // GONetInputSync.GetKey_RightApple
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_RightApple = value;
							}
			{ // GONetInputSync.GetKey_RightArrow
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_RightArrow = value;
							}
			{ // GONetInputSync.GetKey_RightBracket
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_RightBracket = value;
							}
			{ // GONetInputSync.GetKey_RightCommand
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_RightCommand = value;
							}
			{ // GONetInputSync.GetKey_RightControl
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_RightControl = value;
							}
			{ // GONetInputSync.GetKey_RightCurlyBracket
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_RightCurlyBracket = value;
							}
			{ // GONetInputSync.GetKey_RightParen
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_RightParen = value;
							}
			{ // GONetInputSync.GetKey_RightShift
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_RightShift = value;
							}
			{ // GONetInputSync.GetKey_RightWindows
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_RightWindows = value;
							}
			{ // GONetInputSync.GetKey_S
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_S = value;
							}
			{ // GONetInputSync.GetKey_ScrollLock
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_ScrollLock = value;
							}
			{ // GONetInputSync.GetKey_Semicolon
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Semicolon = value;
							}
			{ // GONetInputSync.GetKey_Slash
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Slash = value;
							}
			{ // GONetInputSync.GetKey_Space
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Space = value;
							}
			{ // GONetInputSync.GetKey_SysReq
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_SysReq = value;
							}
			{ // GONetInputSync.GetKey_T
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_T = value;
							}
			{ // GONetInputSync.GetKey_Tab
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Tab = value;
							}
			{ // GONetInputSync.GetKey_Tilde
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Tilde = value;
							}
			{ // GONetInputSync.GetKey_U
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_U = value;
							}
			{ // GONetInputSync.GetKey_Underscore
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Underscore = value;
							}
			{ // GONetInputSync.GetKey_UpArrow
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_UpArrow = value;
							}
			{ // GONetInputSync.GetKey_V
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_V = value;
							}
			{ // GONetInputSync.GetKey_W
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_W = value;
							}
			{ // GONetInputSync.GetKey_X
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_X = value;
							}
			{ // GONetInputSync.GetKey_Y
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Y = value;
							}
			{ // GONetInputSync.GetKey_Z
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetKey_Z = value;
							}
			{ // GONetInputSync.GetMouseButton_0
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetMouseButton_0 = value;
							}
			{ // GONetInputSync.GetMouseButton_1
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetMouseButton_1 = value;
							}
			{ // GONetInputSync.GetMouseButton_2
				bool value;
                bitStream_readFrom.ReadBit(out value);
								GONetInputSync.GetMouseButton_2 = value;
							}
			{ // GONetInputSync.mousePosition
				IGONetAutoMagicalSync_CustomSerializer customSerializer = GONetAutoMagicalSyncAttribute.GetCustomSerializer<Vector2Serializer>(); // TODO need to cache this locally instead of having to lookup each time
				GONetInputSync.mousePosition = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Vector2;
			}
			{ // Transform.rotation
				IGONetAutoMagicalSync_CustomSerializer customSerializer = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.QuaternionSerializer>(); // TODO need to cache this locally instead of having to lookup each time
				Transform.rotation = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Quaternion;
			}
			{ // Transform.position
				IGONetAutoMagicalSync_CustomSerializer customSerializer = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.Vector3Serializer>(); // TODO need to cache this locally instead of having to lookup each time
				Transform.position = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Vector3;
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
					IGONetAutoMagicalSync_CustomSerializer customSerializer = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.GONetParticipant.GONetId_InitialAssignment_CustomSerializer>(); // TODO need to cache this locally instead of having to lookup each time
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
				{ // GONetInputSync.GetKey_A
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_A = value;
								}
				break;

				case 5:
				{ // GONetInputSync.GetKey_Alpha0
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Alpha0 = value;
								}
				break;

				case 6:
				{ // GONetInputSync.GetKey_Alpha1
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Alpha1 = value;
								}
				break;

				case 7:
				{ // GONetInputSync.GetKey_Alpha2
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Alpha2 = value;
								}
				break;

				case 8:
				{ // GONetInputSync.GetKey_Alpha3
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Alpha3 = value;
								}
				break;

				case 9:
				{ // GONetInputSync.GetKey_Alpha4
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Alpha4 = value;
								}
				break;

				case 10:
				{ // GONetInputSync.GetKey_Alpha5
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Alpha5 = value;
								}
				break;

				case 11:
				{ // GONetInputSync.GetKey_Alpha6
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Alpha6 = value;
								}
				break;

				case 12:
				{ // GONetInputSync.GetKey_Alpha7
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Alpha7 = value;
								}
				break;

				case 13:
				{ // GONetInputSync.GetKey_Alpha8
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Alpha8 = value;
								}
				break;

				case 14:
				{ // GONetInputSync.GetKey_Alpha9
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Alpha9 = value;
								}
				break;

				case 15:
				{ // GONetInputSync.GetKey_AltGr
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_AltGr = value;
								}
				break;

				case 16:
				{ // GONetInputSync.GetKey_Ampersand
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Ampersand = value;
								}
				break;

				case 17:
				{ // GONetInputSync.GetKey_Asterisk
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Asterisk = value;
								}
				break;

				case 18:
				{ // GONetInputSync.GetKey_At
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_At = value;
								}
				break;

				case 19:
				{ // GONetInputSync.GetKey_B
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_B = value;
								}
				break;

				case 20:
				{ // GONetInputSync.GetKey_BackQuote
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_BackQuote = value;
								}
				break;

				case 21:
				{ // GONetInputSync.GetKey_Backslash
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Backslash = value;
								}
				break;

				case 22:
				{ // GONetInputSync.GetKey_Backspace
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Backspace = value;
								}
				break;

				case 23:
				{ // GONetInputSync.GetKey_Break
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Break = value;
								}
				break;

				case 24:
				{ // GONetInputSync.GetKey_C
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_C = value;
								}
				break;

				case 25:
				{ // GONetInputSync.GetKey_CapsLock
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_CapsLock = value;
								}
				break;

				case 26:
				{ // GONetInputSync.GetKey_Caret
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Caret = value;
								}
				break;

				case 27:
				{ // GONetInputSync.GetKey_Clear
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Clear = value;
								}
				break;

				case 28:
				{ // GONetInputSync.GetKey_Colon
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Colon = value;
								}
				break;

				case 29:
				{ // GONetInputSync.GetKey_Comma
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Comma = value;
								}
				break;

				case 30:
				{ // GONetInputSync.GetKey_D
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_D = value;
								}
				break;

				case 31:
				{ // GONetInputSync.GetKey_Delete
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Delete = value;
								}
				break;

				case 32:
				{ // GONetInputSync.GetKey_Dollar
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Dollar = value;
								}
				break;

				case 33:
				{ // GONetInputSync.GetKey_DoubleQuote
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_DoubleQuote = value;
								}
				break;

				case 34:
				{ // GONetInputSync.GetKey_DownArrow
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_DownArrow = value;
								}
				break;

				case 35:
				{ // GONetInputSync.GetKey_E
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_E = value;
								}
				break;

				case 36:
				{ // GONetInputSync.GetKey_End
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_End = value;
								}
				break;

				case 37:
				{ // GONetInputSync.GetKey_Equals
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Equals = value;
								}
				break;

				case 38:
				{ // GONetInputSync.GetKey_Escape
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Escape = value;
								}
				break;

				case 39:
				{ // GONetInputSync.GetKey_Exclaim
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Exclaim = value;
								}
				break;

				case 40:
				{ // GONetInputSync.GetKey_F
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_F = value;
								}
				break;

				case 41:
				{ // GONetInputSync.GetKey_F1
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_F1 = value;
								}
				break;

				case 42:
				{ // GONetInputSync.GetKey_F10
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_F10 = value;
								}
				break;

				case 43:
				{ // GONetInputSync.GetKey_F11
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_F11 = value;
								}
				break;

				case 44:
				{ // GONetInputSync.GetKey_F12
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_F12 = value;
								}
				break;

				case 45:
				{ // GONetInputSync.GetKey_F2
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_F2 = value;
								}
				break;

				case 46:
				{ // GONetInputSync.GetKey_F3
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_F3 = value;
								}
				break;

				case 47:
				{ // GONetInputSync.GetKey_F4
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_F4 = value;
								}
				break;

				case 48:
				{ // GONetInputSync.GetKey_F5
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_F5 = value;
								}
				break;

				case 49:
				{ // GONetInputSync.GetKey_F6
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_F6 = value;
								}
				break;

				case 50:
				{ // GONetInputSync.GetKey_F7
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_F7 = value;
								}
				break;

				case 51:
				{ // GONetInputSync.GetKey_F8
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_F8 = value;
								}
				break;

				case 52:
				{ // GONetInputSync.GetKey_F9
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_F9 = value;
								}
				break;

				case 53:
				{ // GONetInputSync.GetKey_G
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_G = value;
								}
				break;

				case 54:
				{ // GONetInputSync.GetKey_Greater
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Greater = value;
								}
				break;

				case 55:
				{ // GONetInputSync.GetKey_H
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_H = value;
								}
				break;

				case 56:
				{ // GONetInputSync.GetKey_Hash
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Hash = value;
								}
				break;

				case 57:
				{ // GONetInputSync.GetKey_Help
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Help = value;
								}
				break;

				case 58:
				{ // GONetInputSync.GetKey_Home
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Home = value;
								}
				break;

				case 59:
				{ // GONetInputSync.GetKey_I
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_I = value;
								}
				break;

				case 60:
				{ // GONetInputSync.GetKey_Insert
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Insert = value;
								}
				break;

				case 61:
				{ // GONetInputSync.GetKey_J
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_J = value;
								}
				break;

				case 62:
				{ // GONetInputSync.GetKey_JoystickButton0
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_JoystickButton0 = value;
								}
				break;

				case 63:
				{ // GONetInputSync.GetKey_JoystickButton1
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_JoystickButton1 = value;
								}
				break;

				case 64:
				{ // GONetInputSync.GetKey_JoystickButton10
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_JoystickButton10 = value;
								}
				break;

				case 65:
				{ // GONetInputSync.GetKey_JoystickButton11
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_JoystickButton11 = value;
								}
				break;

				case 66:
				{ // GONetInputSync.GetKey_JoystickButton12
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_JoystickButton12 = value;
								}
				break;

				case 67:
				{ // GONetInputSync.GetKey_JoystickButton13
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_JoystickButton13 = value;
								}
				break;

				case 68:
				{ // GONetInputSync.GetKey_JoystickButton14
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_JoystickButton14 = value;
								}
				break;

				case 69:
				{ // GONetInputSync.GetKey_JoystickButton15
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_JoystickButton15 = value;
								}
				break;

				case 70:
				{ // GONetInputSync.GetKey_JoystickButton16
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_JoystickButton16 = value;
								}
				break;

				case 71:
				{ // GONetInputSync.GetKey_JoystickButton17
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_JoystickButton17 = value;
								}
				break;

				case 72:
				{ // GONetInputSync.GetKey_JoystickButton18
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_JoystickButton18 = value;
								}
				break;

				case 73:
				{ // GONetInputSync.GetKey_JoystickButton19
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_JoystickButton19 = value;
								}
				break;

				case 74:
				{ // GONetInputSync.GetKey_JoystickButton2
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_JoystickButton2 = value;
								}
				break;

				case 75:
				{ // GONetInputSync.GetKey_JoystickButton3
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_JoystickButton3 = value;
								}
				break;

				case 76:
				{ // GONetInputSync.GetKey_JoystickButton4
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_JoystickButton4 = value;
								}
				break;

				case 77:
				{ // GONetInputSync.GetKey_JoystickButton5
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_JoystickButton5 = value;
								}
				break;

				case 78:
				{ // GONetInputSync.GetKey_JoystickButton6
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_JoystickButton6 = value;
								}
				break;

				case 79:
				{ // GONetInputSync.GetKey_JoystickButton7
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_JoystickButton7 = value;
								}
				break;

				case 80:
				{ // GONetInputSync.GetKey_JoystickButton8
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_JoystickButton8 = value;
								}
				break;

				case 81:
				{ // GONetInputSync.GetKey_JoystickButton9
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_JoystickButton9 = value;
								}
				break;

				case 82:
				{ // GONetInputSync.GetKey_K
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_K = value;
								}
				break;

				case 83:
				{ // GONetInputSync.GetKey_Keypad0
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Keypad0 = value;
								}
				break;

				case 84:
				{ // GONetInputSync.GetKey_Keypad1
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Keypad1 = value;
								}
				break;

				case 85:
				{ // GONetInputSync.GetKey_Keypad2
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Keypad2 = value;
								}
				break;

				case 86:
				{ // GONetInputSync.GetKey_Keypad3
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Keypad3 = value;
								}
				break;

				case 87:
				{ // GONetInputSync.GetKey_Keypad4
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Keypad4 = value;
								}
				break;

				case 88:
				{ // GONetInputSync.GetKey_Keypad5
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Keypad5 = value;
								}
				break;

				case 89:
				{ // GONetInputSync.GetKey_Keypad6
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Keypad6 = value;
								}
				break;

				case 90:
				{ // GONetInputSync.GetKey_Keypad7
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Keypad7 = value;
								}
				break;

				case 91:
				{ // GONetInputSync.GetKey_Keypad8
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Keypad8 = value;
								}
				break;

				case 92:
				{ // GONetInputSync.GetKey_Keypad9
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Keypad9 = value;
								}
				break;

				case 93:
				{ // GONetInputSync.GetKey_KeypadDivide
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_KeypadDivide = value;
								}
				break;

				case 94:
				{ // GONetInputSync.GetKey_KeypadEnter
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_KeypadEnter = value;
								}
				break;

				case 95:
				{ // GONetInputSync.GetKey_KeypadEquals
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_KeypadEquals = value;
								}
				break;

				case 96:
				{ // GONetInputSync.GetKey_KeypadMinus
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_KeypadMinus = value;
								}
				break;

				case 97:
				{ // GONetInputSync.GetKey_KeypadMultiply
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_KeypadMultiply = value;
								}
				break;

				case 98:
				{ // GONetInputSync.GetKey_KeypadPeriod
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_KeypadPeriod = value;
								}
				break;

				case 99:
				{ // GONetInputSync.GetKey_KeypadPlus
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_KeypadPlus = value;
								}
				break;

				case 100:
				{ // GONetInputSync.GetKey_L
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_L = value;
								}
				break;

				case 101:
				{ // GONetInputSync.GetKey_LeftAlt
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_LeftAlt = value;
								}
				break;

				case 102:
				{ // GONetInputSync.GetKey_LeftApple
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_LeftApple = value;
								}
				break;

				case 103:
				{ // GONetInputSync.GetKey_LeftArrow
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_LeftArrow = value;
								}
				break;

				case 104:
				{ // GONetInputSync.GetKey_LeftBracket
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_LeftBracket = value;
								}
				break;

				case 105:
				{ // GONetInputSync.GetKey_LeftCommand
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_LeftCommand = value;
								}
				break;

				case 106:
				{ // GONetInputSync.GetKey_LeftControl
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_LeftControl = value;
								}
				break;

				case 107:
				{ // GONetInputSync.GetKey_LeftCurlyBracket
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_LeftCurlyBracket = value;
								}
				break;

				case 108:
				{ // GONetInputSync.GetKey_LeftParen
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_LeftParen = value;
								}
				break;

				case 109:
				{ // GONetInputSync.GetKey_LeftShift
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_LeftShift = value;
								}
				break;

				case 110:
				{ // GONetInputSync.GetKey_LeftWindows
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_LeftWindows = value;
								}
				break;

				case 111:
				{ // GONetInputSync.GetKey_Less
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Less = value;
								}
				break;

				case 112:
				{ // GONetInputSync.GetKey_M
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_M = value;
								}
				break;

				case 113:
				{ // GONetInputSync.GetKey_Menu
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Menu = value;
								}
				break;

				case 114:
				{ // GONetInputSync.GetKey_Minus
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Minus = value;
								}
				break;

				case 115:
				{ // GONetInputSync.GetKey_Mouse0
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Mouse0 = value;
								}
				break;

				case 116:
				{ // GONetInputSync.GetKey_Mouse1
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Mouse1 = value;
								}
				break;

				case 117:
				{ // GONetInputSync.GetKey_Mouse2
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Mouse2 = value;
								}
				break;

				case 118:
				{ // GONetInputSync.GetKey_Mouse3
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Mouse3 = value;
								}
				break;

				case 119:
				{ // GONetInputSync.GetKey_Mouse4
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Mouse4 = value;
								}
				break;

				case 120:
				{ // GONetInputSync.GetKey_Mouse5
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Mouse5 = value;
								}
				break;

				case 121:
				{ // GONetInputSync.GetKey_Mouse6
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Mouse6 = value;
								}
				break;

				case 122:
				{ // GONetInputSync.GetKey_N
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_N = value;
								}
				break;

				case 123:
				{ // GONetInputSync.GetKey_None
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_None = value;
								}
				break;

				case 124:
				{ // GONetInputSync.GetKey_Numlock
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Numlock = value;
								}
				break;

				case 125:
				{ // GONetInputSync.GetKey_O
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_O = value;
								}
				break;

				case 126:
				{ // GONetInputSync.GetKey_P
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_P = value;
								}
				break;

				case 127:
				{ // GONetInputSync.GetKey_PageDown
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_PageDown = value;
								}
				break;

				case 128:
				{ // GONetInputSync.GetKey_PageUp
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_PageUp = value;
								}
				break;

				case 129:
				{ // GONetInputSync.GetKey_Pause
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Pause = value;
								}
				break;

				case 130:
				{ // GONetInputSync.GetKey_Percent
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Percent = value;
								}
				break;

				case 131:
				{ // GONetInputSync.GetKey_Period
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Period = value;
								}
				break;

				case 132:
				{ // GONetInputSync.GetKey_Pipe
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Pipe = value;
								}
				break;

				case 133:
				{ // GONetInputSync.GetKey_Plus
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Plus = value;
								}
				break;

				case 134:
				{ // GONetInputSync.GetKey_Print
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Print = value;
								}
				break;

				case 135:
				{ // GONetInputSync.GetKey_Q
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Q = value;
								}
				break;

				case 136:
				{ // GONetInputSync.GetKey_Question
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Question = value;
								}
				break;

				case 137:
				{ // GONetInputSync.GetKey_Quote
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Quote = value;
								}
				break;

				case 138:
				{ // GONetInputSync.GetKey_R
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_R = value;
								}
				break;

				case 139:
				{ // GONetInputSync.GetKey_Return
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Return = value;
								}
				break;

				case 140:
				{ // GONetInputSync.GetKey_RightAlt
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_RightAlt = value;
								}
				break;

				case 141:
				{ // GONetInputSync.GetKey_RightApple
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_RightApple = value;
								}
				break;

				case 142:
				{ // GONetInputSync.GetKey_RightArrow
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_RightArrow = value;
								}
				break;

				case 143:
				{ // GONetInputSync.GetKey_RightBracket
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_RightBracket = value;
								}
				break;

				case 144:
				{ // GONetInputSync.GetKey_RightCommand
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_RightCommand = value;
								}
				break;

				case 145:
				{ // GONetInputSync.GetKey_RightControl
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_RightControl = value;
								}
				break;

				case 146:
				{ // GONetInputSync.GetKey_RightCurlyBracket
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_RightCurlyBracket = value;
								}
				break;

				case 147:
				{ // GONetInputSync.GetKey_RightParen
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_RightParen = value;
								}
				break;

				case 148:
				{ // GONetInputSync.GetKey_RightShift
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_RightShift = value;
								}
				break;

				case 149:
				{ // GONetInputSync.GetKey_RightWindows
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_RightWindows = value;
								}
				break;

				case 150:
				{ // GONetInputSync.GetKey_S
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_S = value;
								}
				break;

				case 151:
				{ // GONetInputSync.GetKey_ScrollLock
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_ScrollLock = value;
								}
				break;

				case 152:
				{ // GONetInputSync.GetKey_Semicolon
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Semicolon = value;
								}
				break;

				case 153:
				{ // GONetInputSync.GetKey_Slash
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Slash = value;
								}
				break;

				case 154:
				{ // GONetInputSync.GetKey_Space
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Space = value;
								}
				break;

				case 155:
				{ // GONetInputSync.GetKey_SysReq
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_SysReq = value;
								}
				break;

				case 156:
				{ // GONetInputSync.GetKey_T
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_T = value;
								}
				break;

				case 157:
				{ // GONetInputSync.GetKey_Tab
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Tab = value;
								}
				break;

				case 158:
				{ // GONetInputSync.GetKey_Tilde
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Tilde = value;
								}
				break;

				case 159:
				{ // GONetInputSync.GetKey_U
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_U = value;
								}
				break;

				case 160:
				{ // GONetInputSync.GetKey_Underscore
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Underscore = value;
								}
				break;

				case 161:
				{ // GONetInputSync.GetKey_UpArrow
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_UpArrow = value;
								}
				break;

				case 162:
				{ // GONetInputSync.GetKey_V
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_V = value;
								}
				break;

				case 163:
				{ // GONetInputSync.GetKey_W
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_W = value;
								}
				break;

				case 164:
				{ // GONetInputSync.GetKey_X
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_X = value;
								}
				break;

				case 165:
				{ // GONetInputSync.GetKey_Y
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Y = value;
								}
				break;

				case 166:
				{ // GONetInputSync.GetKey_Z
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetKey_Z = value;
								}
				break;

				case 167:
				{ // GONetInputSync.GetMouseButton_0
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetMouseButton_0 = value;
								}
				break;

				case 168:
				{ // GONetInputSync.GetMouseButton_1
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetMouseButton_1 = value;
								}
				break;

				case 169:
				{ // GONetInputSync.GetMouseButton_2
					bool value;
					bitStream_readFrom.ReadBit(out value);

									GONetInputSync.GetMouseButton_2 = value;
								}
				break;

				case 170:
				{ // GONetInputSync.mousePosition
					IGONetAutoMagicalSync_CustomSerializer customSerializer = GONetAutoMagicalSyncAttribute.GetCustomSerializer<Vector2Serializer>(); // TODO need to cache this locally instead of having to lookup each time
					var value = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Vector2;

									GONetInputSync.mousePosition = value;
								}
				break;

				case 171:
				{ // Transform.rotation
					IGONetAutoMagicalSync_CustomSerializer customSerializer = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.QuaternionSerializer>(); // TODO need to cache this locally instead of having to lookup each time
					var value = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Quaternion;

					valuesChangesSupport[171].AddToMostRecentChangeQueue_IfAppropriate(assumedElapsedTicksAtChange, value); // NOTE: this queue will be used each frame to blend between this value and others added there
				}
				break;

				case 172:
				{ // Transform.position
					IGONetAutoMagicalSync_CustomSerializer customSerializer = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.Vector3Serializer>(); // TODO need to cache this locally instead of having to lookup each time
					var value = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Vector3;

					valuesChangesSupport[172].AddToMostRecentChangeQueue_IfAppropriate(assumedElapsedTicksAtChange, value); // NOTE: this queue will be used each frame to blend between this value and others added there
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
									valuesChangesSupport4.lastKnownValue.System_Boolean = GONetInputSync.GetKey_A;
								}

				var valuesChangesSupport5 = valuesChangesSupport[5];
				if (DoesMatchUniqueGrouping(valuesChangesSupport5, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport5, 5)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport5.lastKnownValue_previous = valuesChangesSupport5.lastKnownValue;
									valuesChangesSupport5.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Alpha0;
								}

				var valuesChangesSupport6 = valuesChangesSupport[6];
				if (DoesMatchUniqueGrouping(valuesChangesSupport6, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport6, 6)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport6.lastKnownValue_previous = valuesChangesSupport6.lastKnownValue;
									valuesChangesSupport6.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Alpha1;
								}

				var valuesChangesSupport7 = valuesChangesSupport[7];
				if (DoesMatchUniqueGrouping(valuesChangesSupport7, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport7, 7)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport7.lastKnownValue_previous = valuesChangesSupport7.lastKnownValue;
									valuesChangesSupport7.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Alpha2;
								}

				var valuesChangesSupport8 = valuesChangesSupport[8];
				if (DoesMatchUniqueGrouping(valuesChangesSupport8, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport8, 8)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport8.lastKnownValue_previous = valuesChangesSupport8.lastKnownValue;
									valuesChangesSupport8.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Alpha3;
								}

				var valuesChangesSupport9 = valuesChangesSupport[9];
				if (DoesMatchUniqueGrouping(valuesChangesSupport9, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport9, 9)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport9.lastKnownValue_previous = valuesChangesSupport9.lastKnownValue;
									valuesChangesSupport9.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Alpha4;
								}

				var valuesChangesSupport10 = valuesChangesSupport[10];
				if (DoesMatchUniqueGrouping(valuesChangesSupport10, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport10, 10)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport10.lastKnownValue_previous = valuesChangesSupport10.lastKnownValue;
									valuesChangesSupport10.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Alpha5;
								}

				var valuesChangesSupport11 = valuesChangesSupport[11];
				if (DoesMatchUniqueGrouping(valuesChangesSupport11, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport11, 11)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport11.lastKnownValue_previous = valuesChangesSupport11.lastKnownValue;
									valuesChangesSupport11.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Alpha6;
								}

				var valuesChangesSupport12 = valuesChangesSupport[12];
				if (DoesMatchUniqueGrouping(valuesChangesSupport12, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport12, 12)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport12.lastKnownValue_previous = valuesChangesSupport12.lastKnownValue;
									valuesChangesSupport12.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Alpha7;
								}

				var valuesChangesSupport13 = valuesChangesSupport[13];
				if (DoesMatchUniqueGrouping(valuesChangesSupport13, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport13, 13)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport13.lastKnownValue_previous = valuesChangesSupport13.lastKnownValue;
									valuesChangesSupport13.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Alpha8;
								}

				var valuesChangesSupport14 = valuesChangesSupport[14];
				if (DoesMatchUniqueGrouping(valuesChangesSupport14, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport14, 14)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport14.lastKnownValue_previous = valuesChangesSupport14.lastKnownValue;
									valuesChangesSupport14.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Alpha9;
								}

				var valuesChangesSupport15 = valuesChangesSupport[15];
				if (DoesMatchUniqueGrouping(valuesChangesSupport15, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport15, 15)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport15.lastKnownValue_previous = valuesChangesSupport15.lastKnownValue;
									valuesChangesSupport15.lastKnownValue.System_Boolean = GONetInputSync.GetKey_AltGr;
								}

				var valuesChangesSupport16 = valuesChangesSupport[16];
				if (DoesMatchUniqueGrouping(valuesChangesSupport16, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport16, 16)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport16.lastKnownValue_previous = valuesChangesSupport16.lastKnownValue;
									valuesChangesSupport16.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Ampersand;
								}

				var valuesChangesSupport17 = valuesChangesSupport[17];
				if (DoesMatchUniqueGrouping(valuesChangesSupport17, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport17, 17)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport17.lastKnownValue_previous = valuesChangesSupport17.lastKnownValue;
									valuesChangesSupport17.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Asterisk;
								}

				var valuesChangesSupport18 = valuesChangesSupport[18];
				if (DoesMatchUniqueGrouping(valuesChangesSupport18, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport18, 18)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport18.lastKnownValue_previous = valuesChangesSupport18.lastKnownValue;
									valuesChangesSupport18.lastKnownValue.System_Boolean = GONetInputSync.GetKey_At;
								}

				var valuesChangesSupport19 = valuesChangesSupport[19];
				if (DoesMatchUniqueGrouping(valuesChangesSupport19, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport19, 19)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport19.lastKnownValue_previous = valuesChangesSupport19.lastKnownValue;
									valuesChangesSupport19.lastKnownValue.System_Boolean = GONetInputSync.GetKey_B;
								}

				var valuesChangesSupport20 = valuesChangesSupport[20];
				if (DoesMatchUniqueGrouping(valuesChangesSupport20, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport20, 20)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport20.lastKnownValue_previous = valuesChangesSupport20.lastKnownValue;
									valuesChangesSupport20.lastKnownValue.System_Boolean = GONetInputSync.GetKey_BackQuote;
								}

				var valuesChangesSupport21 = valuesChangesSupport[21];
				if (DoesMatchUniqueGrouping(valuesChangesSupport21, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport21, 21)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport21.lastKnownValue_previous = valuesChangesSupport21.lastKnownValue;
									valuesChangesSupport21.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Backslash;
								}

				var valuesChangesSupport22 = valuesChangesSupport[22];
				if (DoesMatchUniqueGrouping(valuesChangesSupport22, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport22, 22)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport22.lastKnownValue_previous = valuesChangesSupport22.lastKnownValue;
									valuesChangesSupport22.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Backspace;
								}

				var valuesChangesSupport23 = valuesChangesSupport[23];
				if (DoesMatchUniqueGrouping(valuesChangesSupport23, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport23, 23)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport23.lastKnownValue_previous = valuesChangesSupport23.lastKnownValue;
									valuesChangesSupport23.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Break;
								}

				var valuesChangesSupport24 = valuesChangesSupport[24];
				if (DoesMatchUniqueGrouping(valuesChangesSupport24, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport24, 24)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport24.lastKnownValue_previous = valuesChangesSupport24.lastKnownValue;
									valuesChangesSupport24.lastKnownValue.System_Boolean = GONetInputSync.GetKey_C;
								}

				var valuesChangesSupport25 = valuesChangesSupport[25];
				if (DoesMatchUniqueGrouping(valuesChangesSupport25, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport25, 25)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport25.lastKnownValue_previous = valuesChangesSupport25.lastKnownValue;
									valuesChangesSupport25.lastKnownValue.System_Boolean = GONetInputSync.GetKey_CapsLock;
								}

				var valuesChangesSupport26 = valuesChangesSupport[26];
				if (DoesMatchUniqueGrouping(valuesChangesSupport26, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport26, 26)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport26.lastKnownValue_previous = valuesChangesSupport26.lastKnownValue;
									valuesChangesSupport26.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Caret;
								}

				var valuesChangesSupport27 = valuesChangesSupport[27];
				if (DoesMatchUniqueGrouping(valuesChangesSupport27, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport27, 27)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport27.lastKnownValue_previous = valuesChangesSupport27.lastKnownValue;
									valuesChangesSupport27.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Clear;
								}

				var valuesChangesSupport28 = valuesChangesSupport[28];
				if (DoesMatchUniqueGrouping(valuesChangesSupport28, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport28, 28)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport28.lastKnownValue_previous = valuesChangesSupport28.lastKnownValue;
									valuesChangesSupport28.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Colon;
								}

				var valuesChangesSupport29 = valuesChangesSupport[29];
				if (DoesMatchUniqueGrouping(valuesChangesSupport29, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport29, 29)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport29.lastKnownValue_previous = valuesChangesSupport29.lastKnownValue;
									valuesChangesSupport29.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Comma;
								}

				var valuesChangesSupport30 = valuesChangesSupport[30];
				if (DoesMatchUniqueGrouping(valuesChangesSupport30, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport30, 30)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport30.lastKnownValue_previous = valuesChangesSupport30.lastKnownValue;
									valuesChangesSupport30.lastKnownValue.System_Boolean = GONetInputSync.GetKey_D;
								}

				var valuesChangesSupport31 = valuesChangesSupport[31];
				if (DoesMatchUniqueGrouping(valuesChangesSupport31, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport31, 31)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport31.lastKnownValue_previous = valuesChangesSupport31.lastKnownValue;
									valuesChangesSupport31.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Delete;
								}

				var valuesChangesSupport32 = valuesChangesSupport[32];
				if (DoesMatchUniqueGrouping(valuesChangesSupport32, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport32, 32)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport32.lastKnownValue_previous = valuesChangesSupport32.lastKnownValue;
									valuesChangesSupport32.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Dollar;
								}

				var valuesChangesSupport33 = valuesChangesSupport[33];
				if (DoesMatchUniqueGrouping(valuesChangesSupport33, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport33, 33)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport33.lastKnownValue_previous = valuesChangesSupport33.lastKnownValue;
									valuesChangesSupport33.lastKnownValue.System_Boolean = GONetInputSync.GetKey_DoubleQuote;
								}

				var valuesChangesSupport34 = valuesChangesSupport[34];
				if (DoesMatchUniqueGrouping(valuesChangesSupport34, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport34, 34)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport34.lastKnownValue_previous = valuesChangesSupport34.lastKnownValue;
									valuesChangesSupport34.lastKnownValue.System_Boolean = GONetInputSync.GetKey_DownArrow;
								}

				var valuesChangesSupport35 = valuesChangesSupport[35];
				if (DoesMatchUniqueGrouping(valuesChangesSupport35, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport35, 35)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport35.lastKnownValue_previous = valuesChangesSupport35.lastKnownValue;
									valuesChangesSupport35.lastKnownValue.System_Boolean = GONetInputSync.GetKey_E;
								}

				var valuesChangesSupport36 = valuesChangesSupport[36];
				if (DoesMatchUniqueGrouping(valuesChangesSupport36, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport36, 36)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport36.lastKnownValue_previous = valuesChangesSupport36.lastKnownValue;
									valuesChangesSupport36.lastKnownValue.System_Boolean = GONetInputSync.GetKey_End;
								}

				var valuesChangesSupport37 = valuesChangesSupport[37];
				if (DoesMatchUniqueGrouping(valuesChangesSupport37, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport37, 37)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport37.lastKnownValue_previous = valuesChangesSupport37.lastKnownValue;
									valuesChangesSupport37.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Equals;
								}

				var valuesChangesSupport38 = valuesChangesSupport[38];
				if (DoesMatchUniqueGrouping(valuesChangesSupport38, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport38, 38)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport38.lastKnownValue_previous = valuesChangesSupport38.lastKnownValue;
									valuesChangesSupport38.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Escape;
								}

				var valuesChangesSupport39 = valuesChangesSupport[39];
				if (DoesMatchUniqueGrouping(valuesChangesSupport39, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport39, 39)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport39.lastKnownValue_previous = valuesChangesSupport39.lastKnownValue;
									valuesChangesSupport39.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Exclaim;
								}

				var valuesChangesSupport40 = valuesChangesSupport[40];
				if (DoesMatchUniqueGrouping(valuesChangesSupport40, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport40, 40)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport40.lastKnownValue_previous = valuesChangesSupport40.lastKnownValue;
									valuesChangesSupport40.lastKnownValue.System_Boolean = GONetInputSync.GetKey_F;
								}

				var valuesChangesSupport41 = valuesChangesSupport[41];
				if (DoesMatchUniqueGrouping(valuesChangesSupport41, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport41, 41)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport41.lastKnownValue_previous = valuesChangesSupport41.lastKnownValue;
									valuesChangesSupport41.lastKnownValue.System_Boolean = GONetInputSync.GetKey_F1;
								}

				var valuesChangesSupport42 = valuesChangesSupport[42];
				if (DoesMatchUniqueGrouping(valuesChangesSupport42, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport42, 42)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport42.lastKnownValue_previous = valuesChangesSupport42.lastKnownValue;
									valuesChangesSupport42.lastKnownValue.System_Boolean = GONetInputSync.GetKey_F10;
								}

				var valuesChangesSupport43 = valuesChangesSupport[43];
				if (DoesMatchUniqueGrouping(valuesChangesSupport43, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport43, 43)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport43.lastKnownValue_previous = valuesChangesSupport43.lastKnownValue;
									valuesChangesSupport43.lastKnownValue.System_Boolean = GONetInputSync.GetKey_F11;
								}

				var valuesChangesSupport44 = valuesChangesSupport[44];
				if (DoesMatchUniqueGrouping(valuesChangesSupport44, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport44, 44)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport44.lastKnownValue_previous = valuesChangesSupport44.lastKnownValue;
									valuesChangesSupport44.lastKnownValue.System_Boolean = GONetInputSync.GetKey_F12;
								}

				var valuesChangesSupport45 = valuesChangesSupport[45];
				if (DoesMatchUniqueGrouping(valuesChangesSupport45, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport45, 45)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport45.lastKnownValue_previous = valuesChangesSupport45.lastKnownValue;
									valuesChangesSupport45.lastKnownValue.System_Boolean = GONetInputSync.GetKey_F2;
								}

				var valuesChangesSupport46 = valuesChangesSupport[46];
				if (DoesMatchUniqueGrouping(valuesChangesSupport46, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport46, 46)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport46.lastKnownValue_previous = valuesChangesSupport46.lastKnownValue;
									valuesChangesSupport46.lastKnownValue.System_Boolean = GONetInputSync.GetKey_F3;
								}

				var valuesChangesSupport47 = valuesChangesSupport[47];
				if (DoesMatchUniqueGrouping(valuesChangesSupport47, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport47, 47)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport47.lastKnownValue_previous = valuesChangesSupport47.lastKnownValue;
									valuesChangesSupport47.lastKnownValue.System_Boolean = GONetInputSync.GetKey_F4;
								}

				var valuesChangesSupport48 = valuesChangesSupport[48];
				if (DoesMatchUniqueGrouping(valuesChangesSupport48, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport48, 48)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport48.lastKnownValue_previous = valuesChangesSupport48.lastKnownValue;
									valuesChangesSupport48.lastKnownValue.System_Boolean = GONetInputSync.GetKey_F5;
								}

				var valuesChangesSupport49 = valuesChangesSupport[49];
				if (DoesMatchUniqueGrouping(valuesChangesSupport49, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport49, 49)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport49.lastKnownValue_previous = valuesChangesSupport49.lastKnownValue;
									valuesChangesSupport49.lastKnownValue.System_Boolean = GONetInputSync.GetKey_F6;
								}

				var valuesChangesSupport50 = valuesChangesSupport[50];
				if (DoesMatchUniqueGrouping(valuesChangesSupport50, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport50, 50)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport50.lastKnownValue_previous = valuesChangesSupport50.lastKnownValue;
									valuesChangesSupport50.lastKnownValue.System_Boolean = GONetInputSync.GetKey_F7;
								}

				var valuesChangesSupport51 = valuesChangesSupport[51];
				if (DoesMatchUniqueGrouping(valuesChangesSupport51, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport51, 51)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport51.lastKnownValue_previous = valuesChangesSupport51.lastKnownValue;
									valuesChangesSupport51.lastKnownValue.System_Boolean = GONetInputSync.GetKey_F8;
								}

				var valuesChangesSupport52 = valuesChangesSupport[52];
				if (DoesMatchUniqueGrouping(valuesChangesSupport52, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport52, 52)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport52.lastKnownValue_previous = valuesChangesSupport52.lastKnownValue;
									valuesChangesSupport52.lastKnownValue.System_Boolean = GONetInputSync.GetKey_F9;
								}

				var valuesChangesSupport53 = valuesChangesSupport[53];
				if (DoesMatchUniqueGrouping(valuesChangesSupport53, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport53, 53)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport53.lastKnownValue_previous = valuesChangesSupport53.lastKnownValue;
									valuesChangesSupport53.lastKnownValue.System_Boolean = GONetInputSync.GetKey_G;
								}

				var valuesChangesSupport54 = valuesChangesSupport[54];
				if (DoesMatchUniqueGrouping(valuesChangesSupport54, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport54, 54)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport54.lastKnownValue_previous = valuesChangesSupport54.lastKnownValue;
									valuesChangesSupport54.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Greater;
								}

				var valuesChangesSupport55 = valuesChangesSupport[55];
				if (DoesMatchUniqueGrouping(valuesChangesSupport55, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport55, 55)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport55.lastKnownValue_previous = valuesChangesSupport55.lastKnownValue;
									valuesChangesSupport55.lastKnownValue.System_Boolean = GONetInputSync.GetKey_H;
								}

				var valuesChangesSupport56 = valuesChangesSupport[56];
				if (DoesMatchUniqueGrouping(valuesChangesSupport56, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport56, 56)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport56.lastKnownValue_previous = valuesChangesSupport56.lastKnownValue;
									valuesChangesSupport56.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Hash;
								}

				var valuesChangesSupport57 = valuesChangesSupport[57];
				if (DoesMatchUniqueGrouping(valuesChangesSupport57, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport57, 57)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport57.lastKnownValue_previous = valuesChangesSupport57.lastKnownValue;
									valuesChangesSupport57.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Help;
								}

				var valuesChangesSupport58 = valuesChangesSupport[58];
				if (DoesMatchUniqueGrouping(valuesChangesSupport58, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport58, 58)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport58.lastKnownValue_previous = valuesChangesSupport58.lastKnownValue;
									valuesChangesSupport58.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Home;
								}

				var valuesChangesSupport59 = valuesChangesSupport[59];
				if (DoesMatchUniqueGrouping(valuesChangesSupport59, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport59, 59)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport59.lastKnownValue_previous = valuesChangesSupport59.lastKnownValue;
									valuesChangesSupport59.lastKnownValue.System_Boolean = GONetInputSync.GetKey_I;
								}

				var valuesChangesSupport60 = valuesChangesSupport[60];
				if (DoesMatchUniqueGrouping(valuesChangesSupport60, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport60, 60)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport60.lastKnownValue_previous = valuesChangesSupport60.lastKnownValue;
									valuesChangesSupport60.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Insert;
								}

				var valuesChangesSupport61 = valuesChangesSupport[61];
				if (DoesMatchUniqueGrouping(valuesChangesSupport61, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport61, 61)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport61.lastKnownValue_previous = valuesChangesSupport61.lastKnownValue;
									valuesChangesSupport61.lastKnownValue.System_Boolean = GONetInputSync.GetKey_J;
								}

				var valuesChangesSupport62 = valuesChangesSupport[62];
				if (DoesMatchUniqueGrouping(valuesChangesSupport62, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport62, 62)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport62.lastKnownValue_previous = valuesChangesSupport62.lastKnownValue;
									valuesChangesSupport62.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton0;
								}

				var valuesChangesSupport63 = valuesChangesSupport[63];
				if (DoesMatchUniqueGrouping(valuesChangesSupport63, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport63, 63)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport63.lastKnownValue_previous = valuesChangesSupport63.lastKnownValue;
									valuesChangesSupport63.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton1;
								}

				var valuesChangesSupport64 = valuesChangesSupport[64];
				if (DoesMatchUniqueGrouping(valuesChangesSupport64, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport64, 64)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport64.lastKnownValue_previous = valuesChangesSupport64.lastKnownValue;
									valuesChangesSupport64.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton10;
								}

				var valuesChangesSupport65 = valuesChangesSupport[65];
				if (DoesMatchUniqueGrouping(valuesChangesSupport65, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport65, 65)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport65.lastKnownValue_previous = valuesChangesSupport65.lastKnownValue;
									valuesChangesSupport65.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton11;
								}

				var valuesChangesSupport66 = valuesChangesSupport[66];
				if (DoesMatchUniqueGrouping(valuesChangesSupport66, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport66, 66)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport66.lastKnownValue_previous = valuesChangesSupport66.lastKnownValue;
									valuesChangesSupport66.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton12;
								}

				var valuesChangesSupport67 = valuesChangesSupport[67];
				if (DoesMatchUniqueGrouping(valuesChangesSupport67, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport67, 67)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport67.lastKnownValue_previous = valuesChangesSupport67.lastKnownValue;
									valuesChangesSupport67.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton13;
								}

				var valuesChangesSupport68 = valuesChangesSupport[68];
				if (DoesMatchUniqueGrouping(valuesChangesSupport68, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport68, 68)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport68.lastKnownValue_previous = valuesChangesSupport68.lastKnownValue;
									valuesChangesSupport68.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton14;
								}

				var valuesChangesSupport69 = valuesChangesSupport[69];
				if (DoesMatchUniqueGrouping(valuesChangesSupport69, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport69, 69)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport69.lastKnownValue_previous = valuesChangesSupport69.lastKnownValue;
									valuesChangesSupport69.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton15;
								}

				var valuesChangesSupport70 = valuesChangesSupport[70];
				if (DoesMatchUniqueGrouping(valuesChangesSupport70, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport70, 70)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport70.lastKnownValue_previous = valuesChangesSupport70.lastKnownValue;
									valuesChangesSupport70.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton16;
								}

				var valuesChangesSupport71 = valuesChangesSupport[71];
				if (DoesMatchUniqueGrouping(valuesChangesSupport71, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport71, 71)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport71.lastKnownValue_previous = valuesChangesSupport71.lastKnownValue;
									valuesChangesSupport71.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton17;
								}

				var valuesChangesSupport72 = valuesChangesSupport[72];
				if (DoesMatchUniqueGrouping(valuesChangesSupport72, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport72, 72)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport72.lastKnownValue_previous = valuesChangesSupport72.lastKnownValue;
									valuesChangesSupport72.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton18;
								}

				var valuesChangesSupport73 = valuesChangesSupport[73];
				if (DoesMatchUniqueGrouping(valuesChangesSupport73, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport73, 73)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport73.lastKnownValue_previous = valuesChangesSupport73.lastKnownValue;
									valuesChangesSupport73.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton19;
								}

				var valuesChangesSupport74 = valuesChangesSupport[74];
				if (DoesMatchUniqueGrouping(valuesChangesSupport74, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport74, 74)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport74.lastKnownValue_previous = valuesChangesSupport74.lastKnownValue;
									valuesChangesSupport74.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton2;
								}

				var valuesChangesSupport75 = valuesChangesSupport[75];
				if (DoesMatchUniqueGrouping(valuesChangesSupport75, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport75, 75)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport75.lastKnownValue_previous = valuesChangesSupport75.lastKnownValue;
									valuesChangesSupport75.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton3;
								}

				var valuesChangesSupport76 = valuesChangesSupport[76];
				if (DoesMatchUniqueGrouping(valuesChangesSupport76, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport76, 76)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport76.lastKnownValue_previous = valuesChangesSupport76.lastKnownValue;
									valuesChangesSupport76.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton4;
								}

				var valuesChangesSupport77 = valuesChangesSupport[77];
				if (DoesMatchUniqueGrouping(valuesChangesSupport77, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport77, 77)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport77.lastKnownValue_previous = valuesChangesSupport77.lastKnownValue;
									valuesChangesSupport77.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton5;
								}

				var valuesChangesSupport78 = valuesChangesSupport[78];
				if (DoesMatchUniqueGrouping(valuesChangesSupport78, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport78, 78)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport78.lastKnownValue_previous = valuesChangesSupport78.lastKnownValue;
									valuesChangesSupport78.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton6;
								}

				var valuesChangesSupport79 = valuesChangesSupport[79];
				if (DoesMatchUniqueGrouping(valuesChangesSupport79, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport79, 79)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport79.lastKnownValue_previous = valuesChangesSupport79.lastKnownValue;
									valuesChangesSupport79.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton7;
								}

				var valuesChangesSupport80 = valuesChangesSupport[80];
				if (DoesMatchUniqueGrouping(valuesChangesSupport80, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport80, 80)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport80.lastKnownValue_previous = valuesChangesSupport80.lastKnownValue;
									valuesChangesSupport80.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton8;
								}

				var valuesChangesSupport81 = valuesChangesSupport[81];
				if (DoesMatchUniqueGrouping(valuesChangesSupport81, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport81, 81)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport81.lastKnownValue_previous = valuesChangesSupport81.lastKnownValue;
									valuesChangesSupport81.lastKnownValue.System_Boolean = GONetInputSync.GetKey_JoystickButton9;
								}

				var valuesChangesSupport82 = valuesChangesSupport[82];
				if (DoesMatchUniqueGrouping(valuesChangesSupport82, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport82, 82)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport82.lastKnownValue_previous = valuesChangesSupport82.lastKnownValue;
									valuesChangesSupport82.lastKnownValue.System_Boolean = GONetInputSync.GetKey_K;
								}

				var valuesChangesSupport83 = valuesChangesSupport[83];
				if (DoesMatchUniqueGrouping(valuesChangesSupport83, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport83, 83)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport83.lastKnownValue_previous = valuesChangesSupport83.lastKnownValue;
									valuesChangesSupport83.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Keypad0;
								}

				var valuesChangesSupport84 = valuesChangesSupport[84];
				if (DoesMatchUniqueGrouping(valuesChangesSupport84, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport84, 84)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport84.lastKnownValue_previous = valuesChangesSupport84.lastKnownValue;
									valuesChangesSupport84.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Keypad1;
								}

				var valuesChangesSupport85 = valuesChangesSupport[85];
				if (DoesMatchUniqueGrouping(valuesChangesSupport85, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport85, 85)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport85.lastKnownValue_previous = valuesChangesSupport85.lastKnownValue;
									valuesChangesSupport85.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Keypad2;
								}

				var valuesChangesSupport86 = valuesChangesSupport[86];
				if (DoesMatchUniqueGrouping(valuesChangesSupport86, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport86, 86)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport86.lastKnownValue_previous = valuesChangesSupport86.lastKnownValue;
									valuesChangesSupport86.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Keypad3;
								}

				var valuesChangesSupport87 = valuesChangesSupport[87];
				if (DoesMatchUniqueGrouping(valuesChangesSupport87, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport87, 87)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport87.lastKnownValue_previous = valuesChangesSupport87.lastKnownValue;
									valuesChangesSupport87.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Keypad4;
								}

				var valuesChangesSupport88 = valuesChangesSupport[88];
				if (DoesMatchUniqueGrouping(valuesChangesSupport88, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport88, 88)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport88.lastKnownValue_previous = valuesChangesSupport88.lastKnownValue;
									valuesChangesSupport88.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Keypad5;
								}

				var valuesChangesSupport89 = valuesChangesSupport[89];
				if (DoesMatchUniqueGrouping(valuesChangesSupport89, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport89, 89)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport89.lastKnownValue_previous = valuesChangesSupport89.lastKnownValue;
									valuesChangesSupport89.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Keypad6;
								}

				var valuesChangesSupport90 = valuesChangesSupport[90];
				if (DoesMatchUniqueGrouping(valuesChangesSupport90, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport90, 90)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport90.lastKnownValue_previous = valuesChangesSupport90.lastKnownValue;
									valuesChangesSupport90.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Keypad7;
								}

				var valuesChangesSupport91 = valuesChangesSupport[91];
				if (DoesMatchUniqueGrouping(valuesChangesSupport91, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport91, 91)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport91.lastKnownValue_previous = valuesChangesSupport91.lastKnownValue;
									valuesChangesSupport91.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Keypad8;
								}

				var valuesChangesSupport92 = valuesChangesSupport[92];
				if (DoesMatchUniqueGrouping(valuesChangesSupport92, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport92, 92)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport92.lastKnownValue_previous = valuesChangesSupport92.lastKnownValue;
									valuesChangesSupport92.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Keypad9;
								}

				var valuesChangesSupport93 = valuesChangesSupport[93];
				if (DoesMatchUniqueGrouping(valuesChangesSupport93, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport93, 93)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport93.lastKnownValue_previous = valuesChangesSupport93.lastKnownValue;
									valuesChangesSupport93.lastKnownValue.System_Boolean = GONetInputSync.GetKey_KeypadDivide;
								}

				var valuesChangesSupport94 = valuesChangesSupport[94];
				if (DoesMatchUniqueGrouping(valuesChangesSupport94, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport94, 94)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport94.lastKnownValue_previous = valuesChangesSupport94.lastKnownValue;
									valuesChangesSupport94.lastKnownValue.System_Boolean = GONetInputSync.GetKey_KeypadEnter;
								}

				var valuesChangesSupport95 = valuesChangesSupport[95];
				if (DoesMatchUniqueGrouping(valuesChangesSupport95, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport95, 95)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport95.lastKnownValue_previous = valuesChangesSupport95.lastKnownValue;
									valuesChangesSupport95.lastKnownValue.System_Boolean = GONetInputSync.GetKey_KeypadEquals;
								}

				var valuesChangesSupport96 = valuesChangesSupport[96];
				if (DoesMatchUniqueGrouping(valuesChangesSupport96, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport96, 96)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport96.lastKnownValue_previous = valuesChangesSupport96.lastKnownValue;
									valuesChangesSupport96.lastKnownValue.System_Boolean = GONetInputSync.GetKey_KeypadMinus;
								}

				var valuesChangesSupport97 = valuesChangesSupport[97];
				if (DoesMatchUniqueGrouping(valuesChangesSupport97, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport97, 97)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport97.lastKnownValue_previous = valuesChangesSupport97.lastKnownValue;
									valuesChangesSupport97.lastKnownValue.System_Boolean = GONetInputSync.GetKey_KeypadMultiply;
								}

				var valuesChangesSupport98 = valuesChangesSupport[98];
				if (DoesMatchUniqueGrouping(valuesChangesSupport98, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport98, 98)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport98.lastKnownValue_previous = valuesChangesSupport98.lastKnownValue;
									valuesChangesSupport98.lastKnownValue.System_Boolean = GONetInputSync.GetKey_KeypadPeriod;
								}

				var valuesChangesSupport99 = valuesChangesSupport[99];
				if (DoesMatchUniqueGrouping(valuesChangesSupport99, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport99, 99)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport99.lastKnownValue_previous = valuesChangesSupport99.lastKnownValue;
									valuesChangesSupport99.lastKnownValue.System_Boolean = GONetInputSync.GetKey_KeypadPlus;
								}

				var valuesChangesSupport100 = valuesChangesSupport[100];
				if (DoesMatchUniqueGrouping(valuesChangesSupport100, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport100, 100)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport100.lastKnownValue_previous = valuesChangesSupport100.lastKnownValue;
									valuesChangesSupport100.lastKnownValue.System_Boolean = GONetInputSync.GetKey_L;
								}

				var valuesChangesSupport101 = valuesChangesSupport[101];
				if (DoesMatchUniqueGrouping(valuesChangesSupport101, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport101, 101)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport101.lastKnownValue_previous = valuesChangesSupport101.lastKnownValue;
									valuesChangesSupport101.lastKnownValue.System_Boolean = GONetInputSync.GetKey_LeftAlt;
								}

				var valuesChangesSupport102 = valuesChangesSupport[102];
				if (DoesMatchUniqueGrouping(valuesChangesSupport102, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport102, 102)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport102.lastKnownValue_previous = valuesChangesSupport102.lastKnownValue;
									valuesChangesSupport102.lastKnownValue.System_Boolean = GONetInputSync.GetKey_LeftApple;
								}

				var valuesChangesSupport103 = valuesChangesSupport[103];
				if (DoesMatchUniqueGrouping(valuesChangesSupport103, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport103, 103)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport103.lastKnownValue_previous = valuesChangesSupport103.lastKnownValue;
									valuesChangesSupport103.lastKnownValue.System_Boolean = GONetInputSync.GetKey_LeftArrow;
								}

				var valuesChangesSupport104 = valuesChangesSupport[104];
				if (DoesMatchUniqueGrouping(valuesChangesSupport104, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport104, 104)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport104.lastKnownValue_previous = valuesChangesSupport104.lastKnownValue;
									valuesChangesSupport104.lastKnownValue.System_Boolean = GONetInputSync.GetKey_LeftBracket;
								}

				var valuesChangesSupport105 = valuesChangesSupport[105];
				if (DoesMatchUniqueGrouping(valuesChangesSupport105, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport105, 105)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport105.lastKnownValue_previous = valuesChangesSupport105.lastKnownValue;
									valuesChangesSupport105.lastKnownValue.System_Boolean = GONetInputSync.GetKey_LeftCommand;
								}

				var valuesChangesSupport106 = valuesChangesSupport[106];
				if (DoesMatchUniqueGrouping(valuesChangesSupport106, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport106, 106)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport106.lastKnownValue_previous = valuesChangesSupport106.lastKnownValue;
									valuesChangesSupport106.lastKnownValue.System_Boolean = GONetInputSync.GetKey_LeftControl;
								}

				var valuesChangesSupport107 = valuesChangesSupport[107];
				if (DoesMatchUniqueGrouping(valuesChangesSupport107, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport107, 107)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport107.lastKnownValue_previous = valuesChangesSupport107.lastKnownValue;
									valuesChangesSupport107.lastKnownValue.System_Boolean = GONetInputSync.GetKey_LeftCurlyBracket;
								}

				var valuesChangesSupport108 = valuesChangesSupport[108];
				if (DoesMatchUniqueGrouping(valuesChangesSupport108, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport108, 108)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport108.lastKnownValue_previous = valuesChangesSupport108.lastKnownValue;
									valuesChangesSupport108.lastKnownValue.System_Boolean = GONetInputSync.GetKey_LeftParen;
								}

				var valuesChangesSupport109 = valuesChangesSupport[109];
				if (DoesMatchUniqueGrouping(valuesChangesSupport109, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport109, 109)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport109.lastKnownValue_previous = valuesChangesSupport109.lastKnownValue;
									valuesChangesSupport109.lastKnownValue.System_Boolean = GONetInputSync.GetKey_LeftShift;
								}

				var valuesChangesSupport110 = valuesChangesSupport[110];
				if (DoesMatchUniqueGrouping(valuesChangesSupport110, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport110, 110)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport110.lastKnownValue_previous = valuesChangesSupport110.lastKnownValue;
									valuesChangesSupport110.lastKnownValue.System_Boolean = GONetInputSync.GetKey_LeftWindows;
								}

				var valuesChangesSupport111 = valuesChangesSupport[111];
				if (DoesMatchUniqueGrouping(valuesChangesSupport111, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport111, 111)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport111.lastKnownValue_previous = valuesChangesSupport111.lastKnownValue;
									valuesChangesSupport111.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Less;
								}

				var valuesChangesSupport112 = valuesChangesSupport[112];
				if (DoesMatchUniqueGrouping(valuesChangesSupport112, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport112, 112)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport112.lastKnownValue_previous = valuesChangesSupport112.lastKnownValue;
									valuesChangesSupport112.lastKnownValue.System_Boolean = GONetInputSync.GetKey_M;
								}

				var valuesChangesSupport113 = valuesChangesSupport[113];
				if (DoesMatchUniqueGrouping(valuesChangesSupport113, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport113, 113)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport113.lastKnownValue_previous = valuesChangesSupport113.lastKnownValue;
									valuesChangesSupport113.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Menu;
								}

				var valuesChangesSupport114 = valuesChangesSupport[114];
				if (DoesMatchUniqueGrouping(valuesChangesSupport114, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport114, 114)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport114.lastKnownValue_previous = valuesChangesSupport114.lastKnownValue;
									valuesChangesSupport114.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Minus;
								}

				var valuesChangesSupport115 = valuesChangesSupport[115];
				if (DoesMatchUniqueGrouping(valuesChangesSupport115, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport115, 115)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport115.lastKnownValue_previous = valuesChangesSupport115.lastKnownValue;
									valuesChangesSupport115.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Mouse0;
								}

				var valuesChangesSupport116 = valuesChangesSupport[116];
				if (DoesMatchUniqueGrouping(valuesChangesSupport116, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport116, 116)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport116.lastKnownValue_previous = valuesChangesSupport116.lastKnownValue;
									valuesChangesSupport116.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Mouse1;
								}

				var valuesChangesSupport117 = valuesChangesSupport[117];
				if (DoesMatchUniqueGrouping(valuesChangesSupport117, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport117, 117)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport117.lastKnownValue_previous = valuesChangesSupport117.lastKnownValue;
									valuesChangesSupport117.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Mouse2;
								}

				var valuesChangesSupport118 = valuesChangesSupport[118];
				if (DoesMatchUniqueGrouping(valuesChangesSupport118, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport118, 118)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport118.lastKnownValue_previous = valuesChangesSupport118.lastKnownValue;
									valuesChangesSupport118.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Mouse3;
								}

				var valuesChangesSupport119 = valuesChangesSupport[119];
				if (DoesMatchUniqueGrouping(valuesChangesSupport119, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport119, 119)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport119.lastKnownValue_previous = valuesChangesSupport119.lastKnownValue;
									valuesChangesSupport119.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Mouse4;
								}

				var valuesChangesSupport120 = valuesChangesSupport[120];
				if (DoesMatchUniqueGrouping(valuesChangesSupport120, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport120, 120)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport120.lastKnownValue_previous = valuesChangesSupport120.lastKnownValue;
									valuesChangesSupport120.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Mouse5;
								}

				var valuesChangesSupport121 = valuesChangesSupport[121];
				if (DoesMatchUniqueGrouping(valuesChangesSupport121, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport121, 121)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport121.lastKnownValue_previous = valuesChangesSupport121.lastKnownValue;
									valuesChangesSupport121.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Mouse6;
								}

				var valuesChangesSupport122 = valuesChangesSupport[122];
				if (DoesMatchUniqueGrouping(valuesChangesSupport122, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport122, 122)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport122.lastKnownValue_previous = valuesChangesSupport122.lastKnownValue;
									valuesChangesSupport122.lastKnownValue.System_Boolean = GONetInputSync.GetKey_N;
								}

				var valuesChangesSupport123 = valuesChangesSupport[123];
				if (DoesMatchUniqueGrouping(valuesChangesSupport123, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport123, 123)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport123.lastKnownValue_previous = valuesChangesSupport123.lastKnownValue;
									valuesChangesSupport123.lastKnownValue.System_Boolean = GONetInputSync.GetKey_None;
								}

				var valuesChangesSupport124 = valuesChangesSupport[124];
				if (DoesMatchUniqueGrouping(valuesChangesSupport124, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport124, 124)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport124.lastKnownValue_previous = valuesChangesSupport124.lastKnownValue;
									valuesChangesSupport124.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Numlock;
								}

				var valuesChangesSupport125 = valuesChangesSupport[125];
				if (DoesMatchUniqueGrouping(valuesChangesSupport125, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport125, 125)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport125.lastKnownValue_previous = valuesChangesSupport125.lastKnownValue;
									valuesChangesSupport125.lastKnownValue.System_Boolean = GONetInputSync.GetKey_O;
								}

				var valuesChangesSupport126 = valuesChangesSupport[126];
				if (DoesMatchUniqueGrouping(valuesChangesSupport126, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport126, 126)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport126.lastKnownValue_previous = valuesChangesSupport126.lastKnownValue;
									valuesChangesSupport126.lastKnownValue.System_Boolean = GONetInputSync.GetKey_P;
								}

				var valuesChangesSupport127 = valuesChangesSupport[127];
				if (DoesMatchUniqueGrouping(valuesChangesSupport127, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport127, 127)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport127.lastKnownValue_previous = valuesChangesSupport127.lastKnownValue;
									valuesChangesSupport127.lastKnownValue.System_Boolean = GONetInputSync.GetKey_PageDown;
								}

				var valuesChangesSupport128 = valuesChangesSupport[128];
				if (DoesMatchUniqueGrouping(valuesChangesSupport128, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport128, 128)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport128.lastKnownValue_previous = valuesChangesSupport128.lastKnownValue;
									valuesChangesSupport128.lastKnownValue.System_Boolean = GONetInputSync.GetKey_PageUp;
								}

				var valuesChangesSupport129 = valuesChangesSupport[129];
				if (DoesMatchUniqueGrouping(valuesChangesSupport129, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport129, 129)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport129.lastKnownValue_previous = valuesChangesSupport129.lastKnownValue;
									valuesChangesSupport129.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Pause;
								}

				var valuesChangesSupport130 = valuesChangesSupport[130];
				if (DoesMatchUniqueGrouping(valuesChangesSupport130, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport130, 130)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport130.lastKnownValue_previous = valuesChangesSupport130.lastKnownValue;
									valuesChangesSupport130.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Percent;
								}

				var valuesChangesSupport131 = valuesChangesSupport[131];
				if (DoesMatchUniqueGrouping(valuesChangesSupport131, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport131, 131)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport131.lastKnownValue_previous = valuesChangesSupport131.lastKnownValue;
									valuesChangesSupport131.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Period;
								}

				var valuesChangesSupport132 = valuesChangesSupport[132];
				if (DoesMatchUniqueGrouping(valuesChangesSupport132, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport132, 132)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport132.lastKnownValue_previous = valuesChangesSupport132.lastKnownValue;
									valuesChangesSupport132.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Pipe;
								}

				var valuesChangesSupport133 = valuesChangesSupport[133];
				if (DoesMatchUniqueGrouping(valuesChangesSupport133, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport133, 133)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport133.lastKnownValue_previous = valuesChangesSupport133.lastKnownValue;
									valuesChangesSupport133.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Plus;
								}

				var valuesChangesSupport134 = valuesChangesSupport[134];
				if (DoesMatchUniqueGrouping(valuesChangesSupport134, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport134, 134)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport134.lastKnownValue_previous = valuesChangesSupport134.lastKnownValue;
									valuesChangesSupport134.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Print;
								}

				var valuesChangesSupport135 = valuesChangesSupport[135];
				if (DoesMatchUniqueGrouping(valuesChangesSupport135, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport135, 135)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport135.lastKnownValue_previous = valuesChangesSupport135.lastKnownValue;
									valuesChangesSupport135.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Q;
								}

				var valuesChangesSupport136 = valuesChangesSupport[136];
				if (DoesMatchUniqueGrouping(valuesChangesSupport136, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport136, 136)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport136.lastKnownValue_previous = valuesChangesSupport136.lastKnownValue;
									valuesChangesSupport136.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Question;
								}

				var valuesChangesSupport137 = valuesChangesSupport[137];
				if (DoesMatchUniqueGrouping(valuesChangesSupport137, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport137, 137)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport137.lastKnownValue_previous = valuesChangesSupport137.lastKnownValue;
									valuesChangesSupport137.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Quote;
								}

				var valuesChangesSupport138 = valuesChangesSupport[138];
				if (DoesMatchUniqueGrouping(valuesChangesSupport138, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport138, 138)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport138.lastKnownValue_previous = valuesChangesSupport138.lastKnownValue;
									valuesChangesSupport138.lastKnownValue.System_Boolean = GONetInputSync.GetKey_R;
								}

				var valuesChangesSupport139 = valuesChangesSupport[139];
				if (DoesMatchUniqueGrouping(valuesChangesSupport139, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport139, 139)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport139.lastKnownValue_previous = valuesChangesSupport139.lastKnownValue;
									valuesChangesSupport139.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Return;
								}

				var valuesChangesSupport140 = valuesChangesSupport[140];
				if (DoesMatchUniqueGrouping(valuesChangesSupport140, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport140, 140)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport140.lastKnownValue_previous = valuesChangesSupport140.lastKnownValue;
									valuesChangesSupport140.lastKnownValue.System_Boolean = GONetInputSync.GetKey_RightAlt;
								}

				var valuesChangesSupport141 = valuesChangesSupport[141];
				if (DoesMatchUniqueGrouping(valuesChangesSupport141, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport141, 141)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport141.lastKnownValue_previous = valuesChangesSupport141.lastKnownValue;
									valuesChangesSupport141.lastKnownValue.System_Boolean = GONetInputSync.GetKey_RightApple;
								}

				var valuesChangesSupport142 = valuesChangesSupport[142];
				if (DoesMatchUniqueGrouping(valuesChangesSupport142, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport142, 142)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport142.lastKnownValue_previous = valuesChangesSupport142.lastKnownValue;
									valuesChangesSupport142.lastKnownValue.System_Boolean = GONetInputSync.GetKey_RightArrow;
								}

				var valuesChangesSupport143 = valuesChangesSupport[143];
				if (DoesMatchUniqueGrouping(valuesChangesSupport143, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport143, 143)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport143.lastKnownValue_previous = valuesChangesSupport143.lastKnownValue;
									valuesChangesSupport143.lastKnownValue.System_Boolean = GONetInputSync.GetKey_RightBracket;
								}

				var valuesChangesSupport144 = valuesChangesSupport[144];
				if (DoesMatchUniqueGrouping(valuesChangesSupport144, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport144, 144)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport144.lastKnownValue_previous = valuesChangesSupport144.lastKnownValue;
									valuesChangesSupport144.lastKnownValue.System_Boolean = GONetInputSync.GetKey_RightCommand;
								}

				var valuesChangesSupport145 = valuesChangesSupport[145];
				if (DoesMatchUniqueGrouping(valuesChangesSupport145, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport145, 145)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport145.lastKnownValue_previous = valuesChangesSupport145.lastKnownValue;
									valuesChangesSupport145.lastKnownValue.System_Boolean = GONetInputSync.GetKey_RightControl;
								}

				var valuesChangesSupport146 = valuesChangesSupport[146];
				if (DoesMatchUniqueGrouping(valuesChangesSupport146, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport146, 146)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport146.lastKnownValue_previous = valuesChangesSupport146.lastKnownValue;
									valuesChangesSupport146.lastKnownValue.System_Boolean = GONetInputSync.GetKey_RightCurlyBracket;
								}

				var valuesChangesSupport147 = valuesChangesSupport[147];
				if (DoesMatchUniqueGrouping(valuesChangesSupport147, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport147, 147)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport147.lastKnownValue_previous = valuesChangesSupport147.lastKnownValue;
									valuesChangesSupport147.lastKnownValue.System_Boolean = GONetInputSync.GetKey_RightParen;
								}

				var valuesChangesSupport148 = valuesChangesSupport[148];
				if (DoesMatchUniqueGrouping(valuesChangesSupport148, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport148, 148)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport148.lastKnownValue_previous = valuesChangesSupport148.lastKnownValue;
									valuesChangesSupport148.lastKnownValue.System_Boolean = GONetInputSync.GetKey_RightShift;
								}

				var valuesChangesSupport149 = valuesChangesSupport[149];
				if (DoesMatchUniqueGrouping(valuesChangesSupport149, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport149, 149)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport149.lastKnownValue_previous = valuesChangesSupport149.lastKnownValue;
									valuesChangesSupport149.lastKnownValue.System_Boolean = GONetInputSync.GetKey_RightWindows;
								}

				var valuesChangesSupport150 = valuesChangesSupport[150];
				if (DoesMatchUniqueGrouping(valuesChangesSupport150, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport150, 150)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport150.lastKnownValue_previous = valuesChangesSupport150.lastKnownValue;
									valuesChangesSupport150.lastKnownValue.System_Boolean = GONetInputSync.GetKey_S;
								}

				var valuesChangesSupport151 = valuesChangesSupport[151];
				if (DoesMatchUniqueGrouping(valuesChangesSupport151, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport151, 151)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport151.lastKnownValue_previous = valuesChangesSupport151.lastKnownValue;
									valuesChangesSupport151.lastKnownValue.System_Boolean = GONetInputSync.GetKey_ScrollLock;
								}

				var valuesChangesSupport152 = valuesChangesSupport[152];
				if (DoesMatchUniqueGrouping(valuesChangesSupport152, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport152, 152)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport152.lastKnownValue_previous = valuesChangesSupport152.lastKnownValue;
									valuesChangesSupport152.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Semicolon;
								}

				var valuesChangesSupport153 = valuesChangesSupport[153];
				if (DoesMatchUniqueGrouping(valuesChangesSupport153, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport153, 153)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport153.lastKnownValue_previous = valuesChangesSupport153.lastKnownValue;
									valuesChangesSupport153.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Slash;
								}

				var valuesChangesSupport154 = valuesChangesSupport[154];
				if (DoesMatchUniqueGrouping(valuesChangesSupport154, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport154, 154)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport154.lastKnownValue_previous = valuesChangesSupport154.lastKnownValue;
									valuesChangesSupport154.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Space;
								}

				var valuesChangesSupport155 = valuesChangesSupport[155];
				if (DoesMatchUniqueGrouping(valuesChangesSupport155, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport155, 155)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport155.lastKnownValue_previous = valuesChangesSupport155.lastKnownValue;
									valuesChangesSupport155.lastKnownValue.System_Boolean = GONetInputSync.GetKey_SysReq;
								}

				var valuesChangesSupport156 = valuesChangesSupport[156];
				if (DoesMatchUniqueGrouping(valuesChangesSupport156, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport156, 156)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport156.lastKnownValue_previous = valuesChangesSupport156.lastKnownValue;
									valuesChangesSupport156.lastKnownValue.System_Boolean = GONetInputSync.GetKey_T;
								}

				var valuesChangesSupport157 = valuesChangesSupport[157];
				if (DoesMatchUniqueGrouping(valuesChangesSupport157, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport157, 157)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport157.lastKnownValue_previous = valuesChangesSupport157.lastKnownValue;
									valuesChangesSupport157.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Tab;
								}

				var valuesChangesSupport158 = valuesChangesSupport[158];
				if (DoesMatchUniqueGrouping(valuesChangesSupport158, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport158, 158)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport158.lastKnownValue_previous = valuesChangesSupport158.lastKnownValue;
									valuesChangesSupport158.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Tilde;
								}

				var valuesChangesSupport159 = valuesChangesSupport[159];
				if (DoesMatchUniqueGrouping(valuesChangesSupport159, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport159, 159)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport159.lastKnownValue_previous = valuesChangesSupport159.lastKnownValue;
									valuesChangesSupport159.lastKnownValue.System_Boolean = GONetInputSync.GetKey_U;
								}

				var valuesChangesSupport160 = valuesChangesSupport[160];
				if (DoesMatchUniqueGrouping(valuesChangesSupport160, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport160, 160)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport160.lastKnownValue_previous = valuesChangesSupport160.lastKnownValue;
									valuesChangesSupport160.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Underscore;
								}

				var valuesChangesSupport161 = valuesChangesSupport[161];
				if (DoesMatchUniqueGrouping(valuesChangesSupport161, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport161, 161)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport161.lastKnownValue_previous = valuesChangesSupport161.lastKnownValue;
									valuesChangesSupport161.lastKnownValue.System_Boolean = GONetInputSync.GetKey_UpArrow;
								}

				var valuesChangesSupport162 = valuesChangesSupport[162];
				if (DoesMatchUniqueGrouping(valuesChangesSupport162, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport162, 162)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport162.lastKnownValue_previous = valuesChangesSupport162.lastKnownValue;
									valuesChangesSupport162.lastKnownValue.System_Boolean = GONetInputSync.GetKey_V;
								}

				var valuesChangesSupport163 = valuesChangesSupport[163];
				if (DoesMatchUniqueGrouping(valuesChangesSupport163, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport163, 163)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport163.lastKnownValue_previous = valuesChangesSupport163.lastKnownValue;
									valuesChangesSupport163.lastKnownValue.System_Boolean = GONetInputSync.GetKey_W;
								}

				var valuesChangesSupport164 = valuesChangesSupport[164];
				if (DoesMatchUniqueGrouping(valuesChangesSupport164, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport164, 164)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport164.lastKnownValue_previous = valuesChangesSupport164.lastKnownValue;
									valuesChangesSupport164.lastKnownValue.System_Boolean = GONetInputSync.GetKey_X;
								}

				var valuesChangesSupport165 = valuesChangesSupport[165];
				if (DoesMatchUniqueGrouping(valuesChangesSupport165, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport165, 165)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport165.lastKnownValue_previous = valuesChangesSupport165.lastKnownValue;
									valuesChangesSupport165.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Y;
								}

				var valuesChangesSupport166 = valuesChangesSupport[166];
				if (DoesMatchUniqueGrouping(valuesChangesSupport166, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport166, 166)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport166.lastKnownValue_previous = valuesChangesSupport166.lastKnownValue;
									valuesChangesSupport166.lastKnownValue.System_Boolean = GONetInputSync.GetKey_Z;
								}

				var valuesChangesSupport167 = valuesChangesSupport[167];
				if (DoesMatchUniqueGrouping(valuesChangesSupport167, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport167, 167)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport167.lastKnownValue_previous = valuesChangesSupport167.lastKnownValue;
									valuesChangesSupport167.lastKnownValue.System_Boolean = GONetInputSync.GetMouseButton_0;
								}

				var valuesChangesSupport168 = valuesChangesSupport[168];
				if (DoesMatchUniqueGrouping(valuesChangesSupport168, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport168, 168)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport168.lastKnownValue_previous = valuesChangesSupport168.lastKnownValue;
									valuesChangesSupport168.lastKnownValue.System_Boolean = GONetInputSync.GetMouseButton_1;
								}

				var valuesChangesSupport169 = valuesChangesSupport[169];
				if (DoesMatchUniqueGrouping(valuesChangesSupport169, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport169, 169)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport169.lastKnownValue_previous = valuesChangesSupport169.lastKnownValue;
									valuesChangesSupport169.lastKnownValue.System_Boolean = GONetInputSync.GetMouseButton_2;
								}

				var valuesChangesSupport170 = valuesChangesSupport[170];
				if (DoesMatchUniqueGrouping(valuesChangesSupport170, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport170, 170)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport170.lastKnownValue_previous = valuesChangesSupport170.lastKnownValue;
									valuesChangesSupport170.lastKnownValue.UnityEngine_Vector2 = GONetInputSync.mousePosition;
								}

				var valuesChangesSupport171 = valuesChangesSupport[171];
				if (DoesMatchUniqueGrouping(valuesChangesSupport171, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport171, 171)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport171.lastKnownValue_previous = valuesChangesSupport171.lastKnownValue;
									valuesChangesSupport171.lastKnownValue.UnityEngine_Quaternion = Transform.rotation;
								}

				var valuesChangesSupport172 = valuesChangesSupport[172];
				if (DoesMatchUniqueGrouping(valuesChangesSupport172, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport172, 172)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport172.lastKnownValue_previous = valuesChangesSupport172.lastKnownValue;
									valuesChangesSupport172.lastKnownValue.UnityEngine_Vector3 = Transform.position;
								}

		}
    }
}
