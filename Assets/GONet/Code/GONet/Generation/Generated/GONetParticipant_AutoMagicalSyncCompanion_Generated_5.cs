


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

using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using GONet;

namespace GONet.Generation
{
	internal sealed class GONetParticipant_AutoMagicalSyncCompanion_Generated_5 : GONetParticipant_AutoMagicalSyncCompanion_Generated
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

		private UnityEngine.Animator _Animator;
		internal UnityEngine.Animator Animator
		{
			get
			{
				if ((object)_Animator == null)
				{
					_Animator = gonetParticipant.GetComponent<UnityEngine.Animator>();
				}
				return _Animator;
			}
		}


        internal override byte CodeGenerationId => 5;

        internal GONetParticipant_AutoMagicalSyncCompanion_Generated_5(GONetParticipant gonetParticipant) : base(gonetParticipant)
		{
			valuesCount = 13;
		    
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
		            support5.lastKnownValue.UnityEngine_Quaternion = Transform.rotation; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support5.lastKnownValue_previous.UnityEngine_Quaternion = Transform.rotation; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support5.syncCompanion = this;
			support5.index = 5;
			support5.syncAttribute_MustRunOnUnityMainThread = true;
			support5.syncAttribute_ProcessingPriority = 0;
			support5.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support5.syncAttribute_SyncChangesEverySeconds = 0.03333334f;
			support5.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
			support5.syncAttribute_ShouldBlendBetweenValuesReceived = true;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(1, out support5.syncAttribute_ShouldSkipSync);
			support5.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
		
            int support5_mostRecentChanges_calcdSize = support5.syncAttribute_SyncChangesEverySeconds != 0 ? (int)((GONetMain.valueBlendingBufferLeadSeconds / support5.syncAttribute_SyncChangesEverySeconds) * 2.5f) : 0;
            support5.mostRecentChanges_capacitySize = Math.Max(support5_mostRecentChanges_calcdSize, GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.MOST_RECENT_CHANGEs_SIZE_MINIMUM);
			support5.mostRecentChanges = GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.mostRecentChangesPool.Borrow(support5.mostRecentChanges_capacitySize);

			var support6 = valuesChangesSupport[6] = valueChangeSupportArrayPool.Borrow();
		            support6.lastKnownValue.UnityEngine_Vector3 = Transform.position; // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support6.lastKnownValue_previous.UnityEngine_Vector3 = Transform.position; // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support6.syncCompanion = this;
			support6.index = 6;
			support6.syncAttribute_MustRunOnUnityMainThread = true;
			support6.syncAttribute_ProcessingPriority = 0;
			support6.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support6.syncAttribute_SyncChangesEverySeconds = 0.03333334f;
			support6.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
			support6.syncAttribute_ShouldBlendBetweenValuesReceived = true;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(2, out support6.syncAttribute_ShouldSkipSync);
			support6.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-5000f, 5000f, 0, true);
		
            int support6_mostRecentChanges_calcdSize = support6.syncAttribute_SyncChangesEverySeconds != 0 ? (int)((GONetMain.valueBlendingBufferLeadSeconds / support6.syncAttribute_SyncChangesEverySeconds) * 2.5f) : 0;
            support6.mostRecentChanges_capacitySize = Math.Max(support6_mostRecentChanges_calcdSize, GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.MOST_RECENT_CHANGEs_SIZE_MINIMUM);
			support6.mostRecentChanges = GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.mostRecentChangesPool.Borrow(support6.mostRecentChanges_capacitySize);

			var support7 = valuesChangesSupport[7] = valueChangeSupportArrayPool.Borrow();
		            support7.lastKnownValue.System_Single = Animator.GetFloat(-823668238); // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support7.lastKnownValue_previous.System_Single = Animator.GetFloat(-823668238); // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support7.syncCompanion = this;
			support7.index = 7;
			support7.syncAttribute_MustRunOnUnityMainThread = true;
			support7.syncAttribute_ProcessingPriority = 0;
			support7.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support7.syncAttribute_SyncChangesEverySeconds = 0.05f;
			support7.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
			support7.syncAttribute_ShouldBlendBetweenValuesReceived = true;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support7.syncAttribute_ShouldSkipSync);
			support7.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
		
            int support7_mostRecentChanges_calcdSize = support7.syncAttribute_SyncChangesEverySeconds != 0 ? (int)((GONetMain.valueBlendingBufferLeadSeconds / support7.syncAttribute_SyncChangesEverySeconds) * 2.5f) : 0;
            support7.mostRecentChanges_capacitySize = Math.Max(support7_mostRecentChanges_calcdSize, GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.MOST_RECENT_CHANGEs_SIZE_MINIMUM);
			support7.mostRecentChanges = GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.mostRecentChangesPool.Borrow(support7.mostRecentChanges_capacitySize);

			var support8 = valuesChangesSupport[8] = valueChangeSupportArrayPool.Borrow();
		            support8.lastKnownValue.System_Boolean = Animator.GetBool(125937960); // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support8.lastKnownValue_previous.System_Boolean = Animator.GetBool(125937960); // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support8.syncCompanion = this;
			support8.index = 8;
			support8.syncAttribute_MustRunOnUnityMainThread = true;
			support8.syncAttribute_ProcessingPriority = 0;
			support8.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support8.syncAttribute_SyncChangesEverySeconds = 0.05f;
			support8.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
			support8.syncAttribute_ShouldBlendBetweenValuesReceived = true;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support8.syncAttribute_ShouldSkipSync);
			support8.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
		
            int support8_mostRecentChanges_calcdSize = support8.syncAttribute_SyncChangesEverySeconds != 0 ? (int)((GONetMain.valueBlendingBufferLeadSeconds / support8.syncAttribute_SyncChangesEverySeconds) * 2.5f) : 0;
            support8.mostRecentChanges_capacitySize = Math.Max(support8_mostRecentChanges_calcdSize, GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.MOST_RECENT_CHANGEs_SIZE_MINIMUM);
			support8.mostRecentChanges = GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.mostRecentChangesPool.Borrow(support8.mostRecentChanges_capacitySize);

			var support9 = valuesChangesSupport[9] = valueChangeSupportArrayPool.Borrow();
		            support9.lastKnownValue.System_Boolean = Animator.GetBool(153482222); // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support9.lastKnownValue_previous.System_Boolean = Animator.GetBool(153482222); // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support9.syncCompanion = this;
			support9.index = 9;
			support9.syncAttribute_MustRunOnUnityMainThread = true;
			support9.syncAttribute_ProcessingPriority = 0;
			support9.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support9.syncAttribute_SyncChangesEverySeconds = 0.05f;
			support9.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
			support9.syncAttribute_ShouldBlendBetweenValuesReceived = true;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support9.syncAttribute_ShouldSkipSync);
			support9.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
		
            int support9_mostRecentChanges_calcdSize = support9.syncAttribute_SyncChangesEverySeconds != 0 ? (int)((GONetMain.valueBlendingBufferLeadSeconds / support9.syncAttribute_SyncChangesEverySeconds) * 2.5f) : 0;
            support9.mostRecentChanges_capacitySize = Math.Max(support9_mostRecentChanges_calcdSize, GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.MOST_RECENT_CHANGEs_SIZE_MINIMUM);
			support9.mostRecentChanges = GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.mostRecentChangesPool.Borrow(support9.mostRecentChanges_capacitySize);

			var support10 = valuesChangesSupport[10] = valueChangeSupportArrayPool.Borrow();
		            support10.lastKnownValue.System_Single = Animator.GetFloat(-1442503121); // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support10.lastKnownValue_previous.System_Single = Animator.GetFloat(-1442503121); // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support10.syncCompanion = this;
			support10.index = 10;
			support10.syncAttribute_MustRunOnUnityMainThread = true;
			support10.syncAttribute_ProcessingPriority = 0;
			support10.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support10.syncAttribute_SyncChangesEverySeconds = 0.05f;
			support10.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
			support10.syncAttribute_ShouldBlendBetweenValuesReceived = true;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support10.syncAttribute_ShouldSkipSync);
			support10.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
		
            int support10_mostRecentChanges_calcdSize = support10.syncAttribute_SyncChangesEverySeconds != 0 ? (int)((GONetMain.valueBlendingBufferLeadSeconds / support10.syncAttribute_SyncChangesEverySeconds) * 2.5f) : 0;
            support10.mostRecentChanges_capacitySize = Math.Max(support10_mostRecentChanges_calcdSize, GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.MOST_RECENT_CHANGEs_SIZE_MINIMUM);
			support10.mostRecentChanges = GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.mostRecentChangesPool.Borrow(support10.mostRecentChanges_capacitySize);

			var support11 = valuesChangesSupport[11] = valueChangeSupportArrayPool.Borrow();
		            support11.lastKnownValue.System_Single = Animator.GetFloat(1342839628); // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support11.lastKnownValue_previous.System_Single = Animator.GetFloat(1342839628); // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support11.syncCompanion = this;
			support11.index = 11;
			support11.syncAttribute_MustRunOnUnityMainThread = true;
			support11.syncAttribute_ProcessingPriority = 0;
			support11.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support11.syncAttribute_SyncChangesEverySeconds = 0.05f;
			support11.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
			support11.syncAttribute_ShouldBlendBetweenValuesReceived = true;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support11.syncAttribute_ShouldSkipSync);
			support11.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
		
            int support11_mostRecentChanges_calcdSize = support11.syncAttribute_SyncChangesEverySeconds != 0 ? (int)((GONetMain.valueBlendingBufferLeadSeconds / support11.syncAttribute_SyncChangesEverySeconds) * 2.5f) : 0;
            support11.mostRecentChanges_capacitySize = Math.Max(support11_mostRecentChanges_calcdSize, GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.MOST_RECENT_CHANGEs_SIZE_MINIMUM);
			support11.mostRecentChanges = GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.mostRecentChangesPool.Borrow(support11.mostRecentChanges_capacitySize);

			var support12 = valuesChangesSupport[12] = valueChangeSupportArrayPool.Borrow();
		            support12.lastKnownValue.System_Boolean = Animator.GetBool(862969536); // IMPORTANT: The use of the property here (i.e., prior to use anywhere herein after) ensures GetComponnet<T>() called up front and that component is cached and available subsequently as needed/referenced/used
            support12.lastKnownValue_previous.System_Boolean = Animator.GetBool(862969536); // IMPORTANT: same as above PLUS capturing the initial value now as the previous will ensure we do not accumulate changes during first pass "has anything changed" checks, which caused some problems before putting this in because things run in different threads and this is appropriate!
					support12.syncCompanion = this;
			support12.index = 12;
			support12.syncAttribute_MustRunOnUnityMainThread = true;
			support12.syncAttribute_ProcessingPriority = 0;
			support12.syncAttribute_ProcessingPriority_GONetInternalOverride = 0;
			support12.syncAttribute_SyncChangesEverySeconds = 0.05f;
			support12.syncAttribute_Reliability = AutoMagicalSyncReliability.Unreliable;
			support12.syncAttribute_ShouldBlendBetweenValuesReceived = true;
			GONet.GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap.TryGetValue(0, out support12.syncAttribute_ShouldSkipSync);
			support12.syncAttribute_QuantizerSettingsGroup = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
		
            int support12_mostRecentChanges_calcdSize = support12.syncAttribute_SyncChangesEverySeconds != 0 ? (int)((GONetMain.valueBlendingBufferLeadSeconds / support12.syncAttribute_SyncChangesEverySeconds) * 2.5f) : 0;
            support12.mostRecentChanges_capacitySize = Math.Max(support12_mostRecentChanges_calcdSize, GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.MOST_RECENT_CHANGEs_SIZE_MINIMUM);
			support12.mostRecentChanges = GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue.mostRecentChangesPool.Borrow(support12.mostRecentChanges_capacitySize);

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
									Transform.rotation = value.UnityEngine_Quaternion;
									return;
				case 6:
									Transform.position = value.UnityEngine_Vector3;
									return;
				case 7:
									Animator.SetFloat(-823668238, value.System_Single);
									return;
				case 8:
									Animator.SetBool(125937960, value.System_Boolean);
									return;
				case 9:
									Animator.SetBool(153482222, value.System_Boolean);
									return;
				case 10:
									Animator.SetFloat(-1442503121, value.System_Single);
									return;
				case 11:
									Animator.SetFloat(1342839628, value.System_Single);
									return;
				case 12:
									Animator.SetBool(862969536, value.System_Boolean);
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
									return Transform.rotation;
								case 6:
									return Transform.position;
								case 7:
									return Animator.GetFloat(-823668238);
								case 8:
									return Animator.GetBool(125937960);
								case 9:
									return Animator.GetBool(153482222);
								case 10:
									return Animator.GetFloat(-1442503121);
								case 11:
									return Animator.GetFloat(1342839628);
								case 12:
									return Animator.GetBool(862969536);
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
			{ // Transform.rotation
				IGONetAutoMagicalSync_CustomSerializer customSerializer = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.QuaternionSerializer>(); // TODO need to cache this locally instead of having to lookup each time
				customSerializer.Serialize(bitStream_appendTo, gonetParticipant, Transform.rotation);
			}
			{ // Transform.position
				IGONetAutoMagicalSync_CustomSerializer customSerializer = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.Vector3Serializer>(); // TODO need to cache this locally instead of having to lookup each time
				customSerializer.Serialize(bitStream_appendTo, gonetParticipant, Transform.position);
			}
			{ // Animator.parameters
								bitStream_appendTo.WriteFloat(Animator.GetFloat(-823668238));
							}
			{ // Animator.parameters
								bitStream_appendTo.WriteBit(Animator.GetBool(125937960));
							}
			{ // Animator.parameters
								bitStream_appendTo.WriteBit(Animator.GetBool(153482222));
							}
			{ // Animator.parameters
								bitStream_appendTo.WriteFloat(Animator.GetFloat(-1442503121));
							}
			{ // Animator.parameters
								bitStream_appendTo.WriteFloat(Animator.GetFloat(1342839628));
							}
			{ // Animator.parameters
								bitStream_appendTo.WriteBit(Animator.GetBool(862969536));
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
				{ // DestroyIfMineOnKeyPress.willHeUpdate
									bitStream_appendTo.WriteFloat(DestroyIfMineOnKeyPress.willHeUpdate);
								}
				break;

				case 5:
				{ // Transform.rotation
					IGONetAutoMagicalSync_CustomSerializer customSerializer = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.QuaternionSerializer>(); // TODO need to cache this locally instead of having to lookup each time
					customSerializer.Serialize(bitStream_appendTo, gonetParticipant, Transform.rotation);
				}
				break;

				case 6:
				{ // Transform.position
					IGONetAutoMagicalSync_CustomSerializer customSerializer = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.Vector3Serializer>(); // TODO need to cache this locally instead of having to lookup each time
					customSerializer.Serialize(bitStream_appendTo, gonetParticipant, Transform.position);
				}
				break;

				case 7:
				{ // Animator.parameters
									bitStream_appendTo.WriteFloat(Animator.GetFloat(-823668238));
								}
				break;

				case 8:
				{ // Animator.parameters
									bitStream_appendTo.WriteBit(Animator.GetBool(125937960));
								}
				break;

				case 9:
				{ // Animator.parameters
									bitStream_appendTo.WriteBit(Animator.GetBool(153482222));
								}
				break;

				case 10:
				{ // Animator.parameters
									bitStream_appendTo.WriteFloat(Animator.GetFloat(-1442503121));
								}
				break;

				case 11:
				{ // Animator.parameters
									bitStream_appendTo.WriteFloat(Animator.GetFloat(1342839628));
								}
				break;

				case 12:
				{ // Animator.parameters
									bitStream_appendTo.WriteBit(Animator.GetBool(862969536));
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
			{ // Transform.rotation
				IGONetAutoMagicalSync_CustomSerializer customSerializer = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.QuaternionSerializer>(); // TODO need to cache this locally instead of having to lookup each time
				Transform.rotation = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Quaternion;
			}
			{ // Transform.position
				IGONetAutoMagicalSync_CustomSerializer customSerializer = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.Vector3Serializer>(); // TODO need to cache this locally instead of having to lookup each time
				Transform.position = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Vector3;
			}
			{ // Animator.parameters
				float value;
                bitStream_readFrom.ReadFloat(out value);
								Animator.SetFloat(-823668238, value);
							}
			{ // Animator.parameters
				bool value;
                bitStream_readFrom.ReadBit(out value);
								Animator.SetBool(125937960, value);
							}
			{ // Animator.parameters
				bool value;
                bitStream_readFrom.ReadBit(out value);
								Animator.SetBool(153482222, value);
							}
			{ // Animator.parameters
				float value;
                bitStream_readFrom.ReadFloat(out value);
								Animator.SetFloat(-1442503121, value);
							}
			{ // Animator.parameters
				float value;
                bitStream_readFrom.ReadFloat(out value);
								Animator.SetFloat(1342839628, value);
							}
			{ // Animator.parameters
				bool value;
                bitStream_readFrom.ReadBit(out value);
								Animator.SetBool(862969536, value);
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
				{ // DestroyIfMineOnKeyPress.willHeUpdate
					float value;
					bitStream_readFrom.ReadFloat(out value);

									DestroyIfMineOnKeyPress.willHeUpdate = value;
								}
				break;

				case 5:
				{ // Transform.rotation
					IGONetAutoMagicalSync_CustomSerializer customSerializer = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.QuaternionSerializer>(); // TODO need to cache this locally instead of having to lookup each time
					var value = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Quaternion;

					valuesChangesSupport[5].AddToMostRecentChangeQueue_IfAppropriate(assumedElapsedTicksAtChange, value); // NOTE: this queue will be used each frame to blend between this value and others added there
				}
				break;

				case 6:
				{ // Transform.position
					IGONetAutoMagicalSync_CustomSerializer customSerializer = GONetAutoMagicalSyncAttribute.GetCustomSerializer<GONet.Vector3Serializer>(); // TODO need to cache this locally instead of having to lookup each time
					var value = customSerializer.Deserialize(bitStream_readFrom).UnityEngine_Vector3;

					valuesChangesSupport[6].AddToMostRecentChangeQueue_IfAppropriate(assumedElapsedTicksAtChange, value); // NOTE: this queue will be used each frame to blend between this value and others added there
				}
				break;

				case 7:
				{ // Animator.parameters
					float value;
					bitStream_readFrom.ReadFloat(out value);

					valuesChangesSupport[7].AddToMostRecentChangeQueue_IfAppropriate(assumedElapsedTicksAtChange, value); // NOTE: this queue will be used each frame to blend between this value and others added there
				}
				break;

				case 8:
				{ // Animator.parameters
					bool value;
					bitStream_readFrom.ReadBit(out value);

									Animator.SetBool(125937960, (System.Boolean)value);
								}
				break;

				case 9:
				{ // Animator.parameters
					bool value;
					bitStream_readFrom.ReadBit(out value);

									Animator.SetBool(153482222, (System.Boolean)value);
								}
				break;

				case 10:
				{ // Animator.parameters
					float value;
					bitStream_readFrom.ReadFloat(out value);

					valuesChangesSupport[10].AddToMostRecentChangeQueue_IfAppropriate(assumedElapsedTicksAtChange, value); // NOTE: this queue will be used each frame to blend between this value and others added there
				}
				break;

				case 11:
				{ // Animator.parameters
					float value;
					bitStream_readFrom.ReadFloat(out value);

					valuesChangesSupport[11].AddToMostRecentChangeQueue_IfAppropriate(assumedElapsedTicksAtChange, value); // NOTE: this queue will be used each frame to blend between this value and others added there
				}
				break;

				case 12:
				{ // Animator.parameters
					bool value;
					bitStream_readFrom.ReadBit(out value);

									Animator.SetBool(862969536, (System.Boolean)value);
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
									valuesChangesSupport5.lastKnownValue.UnityEngine_Quaternion = Transform.rotation;
								}

				var valuesChangesSupport6 = valuesChangesSupport[6];
				if (DoesMatchUniqueGrouping(valuesChangesSupport6, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport6, 6)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport6.lastKnownValue_previous = valuesChangesSupport6.lastKnownValue;
									valuesChangesSupport6.lastKnownValue.UnityEngine_Vector3 = Transform.position;
								}

				var valuesChangesSupport7 = valuesChangesSupport[7];
				if (DoesMatchUniqueGrouping(valuesChangesSupport7, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport7, 7)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport7.lastKnownValue_previous = valuesChangesSupport7.lastKnownValue;
									valuesChangesSupport7.lastKnownValue.System_Single = Animator.GetFloat(-823668238);
								}

				var valuesChangesSupport8 = valuesChangesSupport[8];
				if (DoesMatchUniqueGrouping(valuesChangesSupport8, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport8, 8)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport8.lastKnownValue_previous = valuesChangesSupport8.lastKnownValue;
									valuesChangesSupport8.lastKnownValue.System_Boolean = Animator.GetBool(125937960);
								}

				var valuesChangesSupport9 = valuesChangesSupport[9];
				if (DoesMatchUniqueGrouping(valuesChangesSupport9, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport9, 9)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport9.lastKnownValue_previous = valuesChangesSupport9.lastKnownValue;
									valuesChangesSupport9.lastKnownValue.System_Boolean = Animator.GetBool(153482222);
								}

				var valuesChangesSupport10 = valuesChangesSupport[10];
				if (DoesMatchUniqueGrouping(valuesChangesSupport10, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport10, 10)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport10.lastKnownValue_previous = valuesChangesSupport10.lastKnownValue;
									valuesChangesSupport10.lastKnownValue.System_Single = Animator.GetFloat(-1442503121);
								}

				var valuesChangesSupport11 = valuesChangesSupport[11];
				if (DoesMatchUniqueGrouping(valuesChangesSupport11, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport11, 11)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport11.lastKnownValue_previous = valuesChangesSupport11.lastKnownValue;
									valuesChangesSupport11.lastKnownValue.System_Single = Animator.GetFloat(1342839628);
								}

				var valuesChangesSupport12 = valuesChangesSupport[12];
				if (DoesMatchUniqueGrouping(valuesChangesSupport12, onlyMatchIfUniqueGroupingMatches) &&
					!ShouldSkipSync(valuesChangesSupport12, 12)) // TODO examine eval order and performance...should this be first or last?, TODO also consider taking this check out of this condition alltogether, because it is perhaps more expensive to do this check than it is to just execute the body AND the body execution will not actually affect whether or not this value change will get sync'd or not..hmm...
				{
					valuesChangesSupport12.lastKnownValue_previous = valuesChangesSupport12.lastKnownValue;
									valuesChangesSupport12.lastKnownValue.System_Boolean = Animator.GetBool(862969536);
								}

		}
    }
}
