


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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using GONet;

namespace GONet
{

	[MessagePack.Union(0, typeof(GONet.AutoMagicalSync_AllCurrentValues_Message))]
		[MessagePack.Union(1, typeof(GONet.AutoMagicalSync_ValueChanges_Message))]
		[MessagePack.Union(2, typeof(GONet.AutoMagicalSync_ValuesNowAtRest_Message))]
		[MessagePack.Union(3, typeof(GONet.ClientStateChangedEvent))]
		[MessagePack.Union(4, typeof(GONet.ClientTypeFlagsChangedEvent))]
		[MessagePack.Union(5, typeof(GONet.DestroyGONetParticipantEvent))]
		[MessagePack.Union(6, typeof(GONet.GONetParticipantDisabledEvent))]
		[MessagePack.Union(7, typeof(GONet.GONetParticipantEnabledEvent))]
		[MessagePack.Union(8, typeof(GONet.GONetParticipantStartedEvent))]
		[MessagePack.Union(9, typeof(GONet.InstantiateGONetParticipantEvent))]
		[MessagePack.Union(10, typeof(GONet.OwnerAuthorityIdAssignmentEvent))]
		[MessagePack.Union(11, typeof(GONet.PersistentEvents_Bundle))]
		[MessagePack.Union(12, typeof(GONet.RemoteClientStateChangedEvent))]
		[MessagePack.Union(13, typeof(GONet.RequestMessage))]
		[MessagePack.Union(14, typeof(GONet.ResponseMessage))]
		[MessagePack.Union(15, typeof(GONet.ServerSaysClientInitializationCompletion))]
		[MessagePack.Union(16, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Aim))]
		[MessagePack.Union(17, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Fly))]
		[MessagePack.Union(18, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Grounded))]
		[MessagePack.Union(19, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_H))]
		[MessagePack.Union(20, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Jump))]
		[MessagePack.Union(21, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Speed))]
		[MessagePack.Union(22, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_V))]
		[MessagePack.Union(23, typeof(GONet.SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate))]
		[MessagePack.Union(24, typeof(GONet.SyncEvent_FieldChangeTest_color))]
		[MessagePack.Union(25, typeof(GONet.SyncEvent_FieldChangeTest_color_dosientos))]
		[MessagePack.Union(26, typeof(GONet.SyncEvent_FieldChangeTest_nada))]
		[MessagePack.Union(27, typeof(GONet.SyncEvent_FieldChangeTest_shortie))]
		[MessagePack.Union(28, typeof(GONet.SyncEvent_GONetParticipant_GONetId))]
		[MessagePack.Union(29, typeof(GONet.SyncEvent_GONetParticipant_IsPositionSyncd))]
		[MessagePack.Union(30, typeof(GONet.SyncEvent_GONetParticipant_IsRotationSyncd))]
		[MessagePack.Union(31, typeof(GONet.SyncEvent_GONetParticipant_OwnerAuthorityId))]
		[MessagePack.Union(32, typeof(GONet.SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId))]
		[MessagePack.Union(33, typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_A))]
		[MessagePack.Union(34, typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_D))]
		[MessagePack.Union(35, typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_DownArrow))]
		[MessagePack.Union(36, typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_LeftArrow))]
		[MessagePack.Union(37, typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_RightArrow))]
		[MessagePack.Union(38, typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_S))]
		[MessagePack.Union(39, typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_UpArrow))]
		[MessagePack.Union(40, typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_W))]
		[MessagePack.Union(41, typeof(GONet.SyncEvent_Time_ElapsedTicks_SetFromAuthority))]
		[MessagePack.Union(42, typeof(GONet.SyncEvent_Transform_position))]
		[MessagePack.Union(43, typeof(GONet.SyncEvent_Transform_rotation))]
		[MessagePack.Union(44, typeof(GONet.ValueMonitoringSupport_BaselineExpiredEvent))]
		[MessagePack.Union(45, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Boolean))]
		[MessagePack.Union(46, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Byte))]
		[MessagePack.Union(47, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Double))]
		[MessagePack.Union(48, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Int16))]
		[MessagePack.Union(49, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Int32))]
		[MessagePack.Union(50, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Int64))]
		[MessagePack.Union(51, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_SByte))]
		[MessagePack.Union(52, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Single))]
		[MessagePack.Union(53, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_UInt16))]
		[MessagePack.Union(54, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_UInt32))]
		[MessagePack.Union(55, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_UInt64))]
		[MessagePack.Union(56, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Quaternion))]
		[MessagePack.Union(57, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector2))]
		[MessagePack.Union(58, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector3))]
		[MessagePack.Union(59, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector4))]
		public partial interface IGONetEvent { }


	[MessagePack.Union(0, typeof(GONet.AutoMagicalSync_AllCurrentValues_Message))]
		[MessagePack.Union(1, typeof(GONet.AutoMagicalSync_ValueChanges_Message))]
		[MessagePack.Union(2, typeof(GONet.AutoMagicalSync_ValuesNowAtRest_Message))]
		[MessagePack.Union(3, typeof(GONet.ClientStateChangedEvent))]
		[MessagePack.Union(4, typeof(GONet.ClientTypeFlagsChangedEvent))]
		[MessagePack.Union(5, typeof(GONet.GONetParticipantDisabledEvent))]
		[MessagePack.Union(6, typeof(GONet.GONetParticipantEnabledEvent))]
		[MessagePack.Union(7, typeof(GONet.GONetParticipantStartedEvent))]
		[MessagePack.Union(8, typeof(GONet.PersistentEvents_Bundle))]
		[MessagePack.Union(9, typeof(GONet.RemoteClientStateChangedEvent))]
		[MessagePack.Union(10, typeof(GONet.RequestMessage))]
		[MessagePack.Union(11, typeof(GONet.ResponseMessage))]
		[MessagePack.Union(12, typeof(GONet.ServerSaysClientInitializationCompletion))]
		[MessagePack.Union(13, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Aim))]
		[MessagePack.Union(14, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Fly))]
		[MessagePack.Union(15, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Grounded))]
		[MessagePack.Union(16, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_H))]
		[MessagePack.Union(17, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Jump))]
		[MessagePack.Union(18, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Speed))]
		[MessagePack.Union(19, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_V))]
		[MessagePack.Union(20, typeof(GONet.SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate))]
		[MessagePack.Union(21, typeof(GONet.SyncEvent_FieldChangeTest_color))]
		[MessagePack.Union(22, typeof(GONet.SyncEvent_FieldChangeTest_color_dosientos))]
		[MessagePack.Union(23, typeof(GONet.SyncEvent_FieldChangeTest_nada))]
		[MessagePack.Union(24, typeof(GONet.SyncEvent_FieldChangeTest_shortie))]
		[MessagePack.Union(25, typeof(GONet.SyncEvent_GONetParticipant_GONetId))]
		[MessagePack.Union(26, typeof(GONet.SyncEvent_GONetParticipant_IsPositionSyncd))]
		[MessagePack.Union(27, typeof(GONet.SyncEvent_GONetParticipant_IsRotationSyncd))]
		[MessagePack.Union(28, typeof(GONet.SyncEvent_GONetParticipant_OwnerAuthorityId))]
		[MessagePack.Union(29, typeof(GONet.SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId))]
		[MessagePack.Union(30, typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_A))]
		[MessagePack.Union(31, typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_D))]
		[MessagePack.Union(32, typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_DownArrow))]
		[MessagePack.Union(33, typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_LeftArrow))]
		[MessagePack.Union(34, typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_RightArrow))]
		[MessagePack.Union(35, typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_S))]
		[MessagePack.Union(36, typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_UpArrow))]
		[MessagePack.Union(37, typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_W))]
		[MessagePack.Union(38, typeof(GONet.SyncEvent_Time_ElapsedTicks_SetFromAuthority))]
		[MessagePack.Union(39, typeof(GONet.SyncEvent_Transform_position))]
		[MessagePack.Union(40, typeof(GONet.SyncEvent_Transform_rotation))]
		public partial interface ITransientEvent : IGONetEvent { }


	[MessagePack.Union(0, typeof(GONet.DestroyGONetParticipantEvent))]
		[MessagePack.Union(1, typeof(GONet.InstantiateGONetParticipantEvent))]
		[MessagePack.Union(2, typeof(GONet.OwnerAuthorityIdAssignmentEvent))]
		[MessagePack.Union(3, typeof(GONet.ValueMonitoringSupport_BaselineExpiredEvent))]
		[MessagePack.Union(4, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Boolean))]
		[MessagePack.Union(5, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Byte))]
		[MessagePack.Union(6, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Double))]
		[MessagePack.Union(7, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Int16))]
		[MessagePack.Union(8, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Int32))]
		[MessagePack.Union(9, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Int64))]
		[MessagePack.Union(10, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_SByte))]
		[MessagePack.Union(11, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Single))]
		[MessagePack.Union(12, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_UInt16))]
		[MessagePack.Union(13, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_UInt32))]
		[MessagePack.Union(14, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_UInt64))]
		[MessagePack.Union(15, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Quaternion))]
		[MessagePack.Union(16, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector2))]
		[MessagePack.Union(17, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector3))]
		[MessagePack.Union(18, typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector4))]
		public partial interface IPersistentEvent : IGONetEvent { }

		[MessagePack.Union(0, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Aim))]
		[MessagePack.Union(1, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Fly))]
		[MessagePack.Union(2, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Grounded))]
		[MessagePack.Union(3, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_H))]
		[MessagePack.Union(4, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Jump))]
		[MessagePack.Union(5, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Speed))]
		[MessagePack.Union(6, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_V))]
		[MessagePack.Union(7, typeof(GONet.SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate))]
		[MessagePack.Union(8, typeof(GONet.SyncEvent_FieldChangeTest_color))]
		[MessagePack.Union(9, typeof(GONet.SyncEvent_FieldChangeTest_color_dosientos))]
		[MessagePack.Union(10, typeof(GONet.SyncEvent_FieldChangeTest_nada))]
		[MessagePack.Union(11, typeof(GONet.SyncEvent_FieldChangeTest_shortie))]
		[MessagePack.Union(12, typeof(GONet.SyncEvent_GONetParticipant_GONetId))]
		[MessagePack.Union(13, typeof(GONet.SyncEvent_GONetParticipant_IsPositionSyncd))]
		[MessagePack.Union(14, typeof(GONet.SyncEvent_GONetParticipant_IsRotationSyncd))]
		[MessagePack.Union(15, typeof(GONet.SyncEvent_GONetParticipant_OwnerAuthorityId))]
		[MessagePack.Union(16, typeof(GONet.SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId))]
		[MessagePack.Union(17, typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_A))]
		[MessagePack.Union(18, typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_D))]
		[MessagePack.Union(19, typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_DownArrow))]
		[MessagePack.Union(20, typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_LeftArrow))]
		[MessagePack.Union(21, typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_RightArrow))]
		[MessagePack.Union(22, typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_S))]
		[MessagePack.Union(23, typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_UpArrow))]
		[MessagePack.Union(24, typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_W))]
		[MessagePack.Union(25, typeof(GONet.SyncEvent_Transform_position))]
		[MessagePack.Union(26, typeof(GONet.SyncEvent_Transform_rotation))]
		public abstract partial class SyncEvent_ValueChangeProcessed { }

    
	
    /// <summary>
    /// <para>This represents that a sync value change has been processed.  Use the class name to determine the related value/type/context.</para>
	/// <para>Two major occassions this event will occur (use this.<see cref="SyncEvent_ValueChangeProcessed.Explanation"/> to know which of the two occasions this represents):</para>
    /// <para>1) For an outbound change being sent to remote recipients (in which case, this event is published just AFTER the change has been sent to remote sources; however, the remote recipients likely have NOT received/processed it yet.)</para>
    /// <para>2) For an inbound change received from a remote source (in which case, this event is published just AFTER the change has been applied)</para>
    /// </summary>
    [MessagePack.MessagePackObject]
    public sealed class SyncEvent_GONetParticipant_GONetId : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public System.UInt32 valuePrevious;
		[MessagePack.Key(7)] public System.UInt32 valueNew;

        static readonly Utils.ObjectPool<SyncEvent_GONetParticipant_GONetId> pool = new Utils.ObjectPool<SyncEvent_GONetParticipant_GONetId>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 1);
        static readonly System.Collections.Concurrent.ConcurrentQueue<SyncEvent_GONetParticipant_GONetId> returnQueue_onceOnBorrowThread = new System.Collections.Concurrent.ConcurrentQueue<SyncEvent_GONetParticipant_GONetId>();
        static System.Threading.Thread borrowThread;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// Instead, call <see cref="Borrow(SyncEvent_ValueChangeProcessedExplanation, long, uint, uint, byte, System.UInt32, System.UInt32)"/>.
        /// </summary>
        public SyncEvent_GONetParticipant_GONetId() { }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to ensure the instance returned from this method is also returned back
        ///            here (i.e., to private object pool) via <see cref="Return(SyncEvent_GONetParticipant_GONetId)"/> when no longer needed!
        /// </summary>
        public static SyncEvent_GONetParticipant_GONetId Borrow(SyncEvent_ValueChangeProcessedExplanation explanation, long occurredAtElapsedTicks, ushort relatedOwnerAuthorityId, uint gonetId, byte codeGenerationId, byte syncMemberIndex, System.UInt32 valuePrevious, System.UInt32 valueNew)
		{
            if (borrowThread == null)
            {
                borrowThread = System.Threading.Thread.CurrentThread;
            }
            else if (borrowThread != System.Threading.Thread.CurrentThread)
            {
                const string REQUIRED_CALL_SAME_BORROW_THREAD = "Not allowed to call this from more than one thread.  So, ensure Borrow() is called from the same exact thread for this specific event type.  NOTE: Each event type can have its' Borrow() called from a different thread from one another.";
                throw new InvalidOperationException(REQUIRED_CALL_SAME_BORROW_THREAD);
            }

            int autoReturnCount = returnQueue_onceOnBorrowThread.Count;
            SyncEvent_GONetParticipant_GONetId autoReturn;
            while (returnQueue_onceOnBorrowThread.TryDequeue(out autoReturn) && autoReturnCount > 0)
            {
                Return(autoReturn);
                ++autoReturnCount;
            }

            var @event = pool.Borrow();
            
            @event.Explanation = explanation;
            @event.OccurredAtElapsedTicks = occurredAtElapsedTicks;
            @event.RelatedOwnerAuthorityId = relatedOwnerAuthorityId;
            @event.GONetId = gonetId;
			@event.CodeGenerationId = codeGenerationId;
            @event.SyncMemberIndex = syncMemberIndex;
			@event.valuePrevious = valuePrevious;
            @event.valueNew = valueNew;

            return @event;
		}

        public override void Return()
		{
			Return(this);
		}

        public static void Return(SyncEvent_GONetParticipant_GONetId borrowed)
        {
            if (borrowThread == System.Threading.Thread.CurrentThread)
            {
                pool.Return(borrowed);
            }
            else
            {
                returnQueue_onceOnBorrowThread.Enqueue(borrowed);
            }
        }
    }

    
	
    /// <summary>
    /// <para>This represents that a sync value change has been processed.  Use the class name to determine the related value/type/context.</para>
	/// <para>Two major occassions this event will occur (use this.<see cref="SyncEvent_ValueChangeProcessed.Explanation"/> to know which of the two occasions this represents):</para>
    /// <para>1) For an outbound change being sent to remote recipients (in which case, this event is published just AFTER the change has been sent to remote sources; however, the remote recipients likely have NOT received/processed it yet.)</para>
    /// <para>2) For an inbound change received from a remote source (in which case, this event is published just AFTER the change has been applied)</para>
    /// </summary>
    [MessagePack.MessagePackObject]
    public sealed class SyncEvent_GONetParticipant_IsPositionSyncd : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public System.Boolean valuePrevious;
		[MessagePack.Key(7)] public System.Boolean valueNew;

        static readonly Utils.ObjectPool<SyncEvent_GONetParticipant_IsPositionSyncd> pool = new Utils.ObjectPool<SyncEvent_GONetParticipant_IsPositionSyncd>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 1);
        static readonly System.Collections.Concurrent.ConcurrentQueue<SyncEvent_GONetParticipant_IsPositionSyncd> returnQueue_onceOnBorrowThread = new System.Collections.Concurrent.ConcurrentQueue<SyncEvent_GONetParticipant_IsPositionSyncd>();
        static System.Threading.Thread borrowThread;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// Instead, call <see cref="Borrow(SyncEvent_ValueChangeProcessedExplanation, long, uint, uint, byte, System.Boolean, System.Boolean)"/>.
        /// </summary>
        public SyncEvent_GONetParticipant_IsPositionSyncd() { }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to ensure the instance returned from this method is also returned back
        ///            here (i.e., to private object pool) via <see cref="Return(SyncEvent_GONetParticipant_IsPositionSyncd)"/> when no longer needed!
        /// </summary>
        public static SyncEvent_GONetParticipant_IsPositionSyncd Borrow(SyncEvent_ValueChangeProcessedExplanation explanation, long occurredAtElapsedTicks, ushort relatedOwnerAuthorityId, uint gonetId, byte codeGenerationId, byte syncMemberIndex, System.Boolean valuePrevious, System.Boolean valueNew)
		{
            if (borrowThread == null)
            {
                borrowThread = System.Threading.Thread.CurrentThread;
            }
            else if (borrowThread != System.Threading.Thread.CurrentThread)
            {
                const string REQUIRED_CALL_SAME_BORROW_THREAD = "Not allowed to call this from more than one thread.  So, ensure Borrow() is called from the same exact thread for this specific event type.  NOTE: Each event type can have its' Borrow() called from a different thread from one another.";
                throw new InvalidOperationException(REQUIRED_CALL_SAME_BORROW_THREAD);
            }

            int autoReturnCount = returnQueue_onceOnBorrowThread.Count;
            SyncEvent_GONetParticipant_IsPositionSyncd autoReturn;
            while (returnQueue_onceOnBorrowThread.TryDequeue(out autoReturn) && autoReturnCount > 0)
            {
                Return(autoReturn);
                ++autoReturnCount;
            }

            var @event = pool.Borrow();
            
            @event.Explanation = explanation;
            @event.OccurredAtElapsedTicks = occurredAtElapsedTicks;
            @event.RelatedOwnerAuthorityId = relatedOwnerAuthorityId;
            @event.GONetId = gonetId;
			@event.CodeGenerationId = codeGenerationId;
            @event.SyncMemberIndex = syncMemberIndex;
			@event.valuePrevious = valuePrevious;
            @event.valueNew = valueNew;

            return @event;
		}

        public override void Return()
		{
			Return(this);
		}

        public static void Return(SyncEvent_GONetParticipant_IsPositionSyncd borrowed)
        {
            if (borrowThread == System.Threading.Thread.CurrentThread)
            {
                pool.Return(borrowed);
            }
            else
            {
                returnQueue_onceOnBorrowThread.Enqueue(borrowed);
            }
        }
    }

    
	
    /// <summary>
    /// <para>This represents that a sync value change has been processed.  Use the class name to determine the related value/type/context.</para>
	/// <para>Two major occassions this event will occur (use this.<see cref="SyncEvent_ValueChangeProcessed.Explanation"/> to know which of the two occasions this represents):</para>
    /// <para>1) For an outbound change being sent to remote recipients (in which case, this event is published just AFTER the change has been sent to remote sources; however, the remote recipients likely have NOT received/processed it yet.)</para>
    /// <para>2) For an inbound change received from a remote source (in which case, this event is published just AFTER the change has been applied)</para>
    /// </summary>
    [MessagePack.MessagePackObject]
    public sealed class SyncEvent_GONetParticipant_IsRotationSyncd : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public System.Boolean valuePrevious;
		[MessagePack.Key(7)] public System.Boolean valueNew;

        static readonly Utils.ObjectPool<SyncEvent_GONetParticipant_IsRotationSyncd> pool = new Utils.ObjectPool<SyncEvent_GONetParticipant_IsRotationSyncd>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 1);
        static readonly System.Collections.Concurrent.ConcurrentQueue<SyncEvent_GONetParticipant_IsRotationSyncd> returnQueue_onceOnBorrowThread = new System.Collections.Concurrent.ConcurrentQueue<SyncEvent_GONetParticipant_IsRotationSyncd>();
        static System.Threading.Thread borrowThread;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// Instead, call <see cref="Borrow(SyncEvent_ValueChangeProcessedExplanation, long, uint, uint, byte, System.Boolean, System.Boolean)"/>.
        /// </summary>
        public SyncEvent_GONetParticipant_IsRotationSyncd() { }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to ensure the instance returned from this method is also returned back
        ///            here (i.e., to private object pool) via <see cref="Return(SyncEvent_GONetParticipant_IsRotationSyncd)"/> when no longer needed!
        /// </summary>
        public static SyncEvent_GONetParticipant_IsRotationSyncd Borrow(SyncEvent_ValueChangeProcessedExplanation explanation, long occurredAtElapsedTicks, ushort relatedOwnerAuthorityId, uint gonetId, byte codeGenerationId, byte syncMemberIndex, System.Boolean valuePrevious, System.Boolean valueNew)
		{
            if (borrowThread == null)
            {
                borrowThread = System.Threading.Thread.CurrentThread;
            }
            else if (borrowThread != System.Threading.Thread.CurrentThread)
            {
                const string REQUIRED_CALL_SAME_BORROW_THREAD = "Not allowed to call this from more than one thread.  So, ensure Borrow() is called from the same exact thread for this specific event type.  NOTE: Each event type can have its' Borrow() called from a different thread from one another.";
                throw new InvalidOperationException(REQUIRED_CALL_SAME_BORROW_THREAD);
            }

            int autoReturnCount = returnQueue_onceOnBorrowThread.Count;
            SyncEvent_GONetParticipant_IsRotationSyncd autoReturn;
            while (returnQueue_onceOnBorrowThread.TryDequeue(out autoReturn) && autoReturnCount > 0)
            {
                Return(autoReturn);
                ++autoReturnCount;
            }

            var @event = pool.Borrow();
            
            @event.Explanation = explanation;
            @event.OccurredAtElapsedTicks = occurredAtElapsedTicks;
            @event.RelatedOwnerAuthorityId = relatedOwnerAuthorityId;
            @event.GONetId = gonetId;
			@event.CodeGenerationId = codeGenerationId;
            @event.SyncMemberIndex = syncMemberIndex;
			@event.valuePrevious = valuePrevious;
            @event.valueNew = valueNew;

            return @event;
		}

        public override void Return()
		{
			Return(this);
		}

        public static void Return(SyncEvent_GONetParticipant_IsRotationSyncd borrowed)
        {
            if (borrowThread == System.Threading.Thread.CurrentThread)
            {
                pool.Return(borrowed);
            }
            else
            {
                returnQueue_onceOnBorrowThread.Enqueue(borrowed);
            }
        }
    }

    
	
    /// <summary>
    /// <para>This represents that a sync value change has been processed.  Use the class name to determine the related value/type/context.</para>
	/// <para>Two major occassions this event will occur (use this.<see cref="SyncEvent_ValueChangeProcessed.Explanation"/> to know which of the two occasions this represents):</para>
    /// <para>1) For an outbound change being sent to remote recipients (in which case, this event is published just AFTER the change has been sent to remote sources; however, the remote recipients likely have NOT received/processed it yet.)</para>
    /// <para>2) For an inbound change received from a remote source (in which case, this event is published just AFTER the change has been applied)</para>
    /// </summary>
    [MessagePack.MessagePackObject]
    public sealed class SyncEvent_GONetParticipant_OwnerAuthorityId : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public System.UInt16 valuePrevious;
		[MessagePack.Key(7)] public System.UInt16 valueNew;

        static readonly Utils.ObjectPool<SyncEvent_GONetParticipant_OwnerAuthorityId> pool = new Utils.ObjectPool<SyncEvent_GONetParticipant_OwnerAuthorityId>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 1);
        static readonly System.Collections.Concurrent.ConcurrentQueue<SyncEvent_GONetParticipant_OwnerAuthorityId> returnQueue_onceOnBorrowThread = new System.Collections.Concurrent.ConcurrentQueue<SyncEvent_GONetParticipant_OwnerAuthorityId>();
        static System.Threading.Thread borrowThread;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// Instead, call <see cref="Borrow(SyncEvent_ValueChangeProcessedExplanation, long, uint, uint, byte, System.UInt16, System.UInt16)"/>.
        /// </summary>
        public SyncEvent_GONetParticipant_OwnerAuthorityId() { }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to ensure the instance returned from this method is also returned back
        ///            here (i.e., to private object pool) via <see cref="Return(SyncEvent_GONetParticipant_OwnerAuthorityId)"/> when no longer needed!
        /// </summary>
        public static SyncEvent_GONetParticipant_OwnerAuthorityId Borrow(SyncEvent_ValueChangeProcessedExplanation explanation, long occurredAtElapsedTicks, ushort relatedOwnerAuthorityId, uint gonetId, byte codeGenerationId, byte syncMemberIndex, System.UInt16 valuePrevious, System.UInt16 valueNew)
		{
            if (borrowThread == null)
            {
                borrowThread = System.Threading.Thread.CurrentThread;
            }
            else if (borrowThread != System.Threading.Thread.CurrentThread)
            {
                const string REQUIRED_CALL_SAME_BORROW_THREAD = "Not allowed to call this from more than one thread.  So, ensure Borrow() is called from the same exact thread for this specific event type.  NOTE: Each event type can have its' Borrow() called from a different thread from one another.";
                throw new InvalidOperationException(REQUIRED_CALL_SAME_BORROW_THREAD);
            }

            int autoReturnCount = returnQueue_onceOnBorrowThread.Count;
            SyncEvent_GONetParticipant_OwnerAuthorityId autoReturn;
            while (returnQueue_onceOnBorrowThread.TryDequeue(out autoReturn) && autoReturnCount > 0)
            {
                Return(autoReturn);
                ++autoReturnCount;
            }

            var @event = pool.Borrow();
            
            @event.Explanation = explanation;
            @event.OccurredAtElapsedTicks = occurredAtElapsedTicks;
            @event.RelatedOwnerAuthorityId = relatedOwnerAuthorityId;
            @event.GONetId = gonetId;
			@event.CodeGenerationId = codeGenerationId;
            @event.SyncMemberIndex = syncMemberIndex;
			@event.valuePrevious = valuePrevious;
            @event.valueNew = valueNew;

            return @event;
		}

        public override void Return()
		{
			Return(this);
		}

        public static void Return(SyncEvent_GONetParticipant_OwnerAuthorityId borrowed)
        {
            if (borrowThread == System.Threading.Thread.CurrentThread)
            {
                pool.Return(borrowed);
            }
            else
            {
                returnQueue_onceOnBorrowThread.Enqueue(borrowed);
            }
        }
    }

    
	
    /// <summary>
    /// <para>This represents that a sync value change has been processed.  Use the class name to determine the related value/type/context.</para>
	/// <para>Two major occassions this event will occur (use this.<see cref="SyncEvent_ValueChangeProcessed.Explanation"/> to know which of the two occasions this represents):</para>
    /// <para>1) For an outbound change being sent to remote recipients (in which case, this event is published just AFTER the change has been sent to remote sources; however, the remote recipients likely have NOT received/processed it yet.)</para>
    /// <para>2) For an inbound change received from a remote source (in which case, this event is published just AFTER the change has been applied)</para>
    /// </summary>
    [MessagePack.MessagePackObject]
    public sealed class SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public System.UInt16 valuePrevious;
		[MessagePack.Key(7)] public System.UInt16 valueNew;

        static readonly Utils.ObjectPool<SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId> pool = new Utils.ObjectPool<SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 1);
        static readonly System.Collections.Concurrent.ConcurrentQueue<SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId> returnQueue_onceOnBorrowThread = new System.Collections.Concurrent.ConcurrentQueue<SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId>();
        static System.Threading.Thread borrowThread;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// Instead, call <see cref="Borrow(SyncEvent_ValueChangeProcessedExplanation, long, uint, uint, byte, System.UInt16, System.UInt16)"/>.
        /// </summary>
        public SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId() { }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to ensure the instance returned from this method is also returned back
        ///            here (i.e., to private object pool) via <see cref="Return(SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId)"/> when no longer needed!
        /// </summary>
        public static SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId Borrow(SyncEvent_ValueChangeProcessedExplanation explanation, long occurredAtElapsedTicks, ushort relatedOwnerAuthorityId, uint gonetId, byte codeGenerationId, byte syncMemberIndex, System.UInt16 valuePrevious, System.UInt16 valueNew)
		{
            if (borrowThread == null)
            {
                borrowThread = System.Threading.Thread.CurrentThread;
            }
            else if (borrowThread != System.Threading.Thread.CurrentThread)
            {
                const string REQUIRED_CALL_SAME_BORROW_THREAD = "Not allowed to call this from more than one thread.  So, ensure Borrow() is called from the same exact thread for this specific event type.  NOTE: Each event type can have its' Borrow() called from a different thread from one another.";
                throw new InvalidOperationException(REQUIRED_CALL_SAME_BORROW_THREAD);
            }

            int autoReturnCount = returnQueue_onceOnBorrowThread.Count;
            SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId autoReturn;
            while (returnQueue_onceOnBorrowThread.TryDequeue(out autoReturn) && autoReturnCount > 0)
            {
                Return(autoReturn);
                ++autoReturnCount;
            }

            var @event = pool.Borrow();
            
            @event.Explanation = explanation;
            @event.OccurredAtElapsedTicks = occurredAtElapsedTicks;
            @event.RelatedOwnerAuthorityId = relatedOwnerAuthorityId;
            @event.GONetId = gonetId;
			@event.CodeGenerationId = codeGenerationId;
            @event.SyncMemberIndex = syncMemberIndex;
			@event.valuePrevious = valuePrevious;
            @event.valueNew = valueNew;

            return @event;
		}

        public override void Return()
		{
			Return(this);
		}

        public static void Return(SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId borrowed)
        {
            if (borrowThread == System.Threading.Thread.CurrentThread)
            {
                pool.Return(borrowed);
            }
            else
            {
                returnQueue_onceOnBorrowThread.Enqueue(borrowed);
            }
        }
    }

    
	
    /// <summary>
    /// <para>This represents that a sync value change has been processed.  Use the class name to determine the related value/type/context.</para>
	/// <para>Two major occassions this event will occur (use this.<see cref="SyncEvent_ValueChangeProcessed.Explanation"/> to know which of the two occasions this represents):</para>
    /// <para>1) For an outbound change being sent to remote recipients (in which case, this event is published just AFTER the change has been sent to remote sources; however, the remote recipients likely have NOT received/processed it yet.)</para>
    /// <para>2) For an inbound change received from a remote source (in which case, this event is published just AFTER the change has been applied)</para>
    /// </summary>
    [MessagePack.MessagePackObject]
    public sealed class SyncEvent_Transform_rotation : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public UnityEngine.Quaternion valuePrevious;
		[MessagePack.Key(7)] public UnityEngine.Quaternion valueNew;

        static readonly Utils.ObjectPool<SyncEvent_Transform_rotation> pool = new Utils.ObjectPool<SyncEvent_Transform_rotation>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 1);
        static readonly System.Collections.Concurrent.ConcurrentQueue<SyncEvent_Transform_rotation> returnQueue_onceOnBorrowThread = new System.Collections.Concurrent.ConcurrentQueue<SyncEvent_Transform_rotation>();
        static System.Threading.Thread borrowThread;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// Instead, call <see cref="Borrow(SyncEvent_ValueChangeProcessedExplanation, long, uint, uint, byte, UnityEngine.Quaternion, UnityEngine.Quaternion)"/>.
        /// </summary>
        public SyncEvent_Transform_rotation() { }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to ensure the instance returned from this method is also returned back
        ///            here (i.e., to private object pool) via <see cref="Return(SyncEvent_Transform_rotation)"/> when no longer needed!
        /// </summary>
        public static SyncEvent_Transform_rotation Borrow(SyncEvent_ValueChangeProcessedExplanation explanation, long occurredAtElapsedTicks, ushort relatedOwnerAuthorityId, uint gonetId, byte codeGenerationId, byte syncMemberIndex, UnityEngine.Quaternion valuePrevious, UnityEngine.Quaternion valueNew)
		{
            if (borrowThread == null)
            {
                borrowThread = System.Threading.Thread.CurrentThread;
            }
            else if (borrowThread != System.Threading.Thread.CurrentThread)
            {
                const string REQUIRED_CALL_SAME_BORROW_THREAD = "Not allowed to call this from more than one thread.  So, ensure Borrow() is called from the same exact thread for this specific event type.  NOTE: Each event type can have its' Borrow() called from a different thread from one another.";
                throw new InvalidOperationException(REQUIRED_CALL_SAME_BORROW_THREAD);
            }

            int autoReturnCount = returnQueue_onceOnBorrowThread.Count;
            SyncEvent_Transform_rotation autoReturn;
            while (returnQueue_onceOnBorrowThread.TryDequeue(out autoReturn) && autoReturnCount > 0)
            {
                Return(autoReturn);
                ++autoReturnCount;
            }

            var @event = pool.Borrow();
            
            @event.Explanation = explanation;
            @event.OccurredAtElapsedTicks = occurredAtElapsedTicks;
            @event.RelatedOwnerAuthorityId = relatedOwnerAuthorityId;
            @event.GONetId = gonetId;
			@event.CodeGenerationId = codeGenerationId;
            @event.SyncMemberIndex = syncMemberIndex;
			@event.valuePrevious = valuePrevious;
            @event.valueNew = valueNew;

            return @event;
		}

        public override void Return()
		{
			Return(this);
		}

        public static void Return(SyncEvent_Transform_rotation borrowed)
        {
            if (borrowThread == System.Threading.Thread.CurrentThread)
            {
                pool.Return(borrowed);
            }
            else
            {
                returnQueue_onceOnBorrowThread.Enqueue(borrowed);
            }
        }
    }

    
	
    /// <summary>
    /// <para>This represents that a sync value change has been processed.  Use the class name to determine the related value/type/context.</para>
	/// <para>Two major occassions this event will occur (use this.<see cref="SyncEvent_ValueChangeProcessed.Explanation"/> to know which of the two occasions this represents):</para>
    /// <para>1) For an outbound change being sent to remote recipients (in which case, this event is published just AFTER the change has been sent to remote sources; however, the remote recipients likely have NOT received/processed it yet.)</para>
    /// <para>2) For an inbound change received from a remote source (in which case, this event is published just AFTER the change has been applied)</para>
    /// </summary>
    [MessagePack.MessagePackObject]
    public sealed class SyncEvent_Transform_position : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public UnityEngine.Vector3 valuePrevious;
		[MessagePack.Key(7)] public UnityEngine.Vector3 valueNew;

        static readonly Utils.ObjectPool<SyncEvent_Transform_position> pool = new Utils.ObjectPool<SyncEvent_Transform_position>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 1);
        static readonly System.Collections.Concurrent.ConcurrentQueue<SyncEvent_Transform_position> returnQueue_onceOnBorrowThread = new System.Collections.Concurrent.ConcurrentQueue<SyncEvent_Transform_position>();
        static System.Threading.Thread borrowThread;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// Instead, call <see cref="Borrow(SyncEvent_ValueChangeProcessedExplanation, long, uint, uint, byte, UnityEngine.Vector3, UnityEngine.Vector3)"/>.
        /// </summary>
        public SyncEvent_Transform_position() { }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to ensure the instance returned from this method is also returned back
        ///            here (i.e., to private object pool) via <see cref="Return(SyncEvent_Transform_position)"/> when no longer needed!
        /// </summary>
        public static SyncEvent_Transform_position Borrow(SyncEvent_ValueChangeProcessedExplanation explanation, long occurredAtElapsedTicks, ushort relatedOwnerAuthorityId, uint gonetId, byte codeGenerationId, byte syncMemberIndex, UnityEngine.Vector3 valuePrevious, UnityEngine.Vector3 valueNew)
		{
            if (borrowThread == null)
            {
                borrowThread = System.Threading.Thread.CurrentThread;
            }
            else if (borrowThread != System.Threading.Thread.CurrentThread)
            {
                const string REQUIRED_CALL_SAME_BORROW_THREAD = "Not allowed to call this from more than one thread.  So, ensure Borrow() is called from the same exact thread for this specific event type.  NOTE: Each event type can have its' Borrow() called from a different thread from one another.";
                throw new InvalidOperationException(REQUIRED_CALL_SAME_BORROW_THREAD);
            }

            int autoReturnCount = returnQueue_onceOnBorrowThread.Count;
            SyncEvent_Transform_position autoReturn;
            while (returnQueue_onceOnBorrowThread.TryDequeue(out autoReturn) && autoReturnCount > 0)
            {
                Return(autoReturn);
                ++autoReturnCount;
            }

            var @event = pool.Borrow();
            
            @event.Explanation = explanation;
            @event.OccurredAtElapsedTicks = occurredAtElapsedTicks;
            @event.RelatedOwnerAuthorityId = relatedOwnerAuthorityId;
            @event.GONetId = gonetId;
			@event.CodeGenerationId = codeGenerationId;
            @event.SyncMemberIndex = syncMemberIndex;
			@event.valuePrevious = valuePrevious;
            @event.valueNew = valueNew;

            return @event;
		}

        public override void Return()
		{
			Return(this);
		}

        public static void Return(SyncEvent_Transform_position borrowed)
        {
            if (borrowThread == System.Threading.Thread.CurrentThread)
            {
                pool.Return(borrowed);
            }
            else
            {
                returnQueue_onceOnBorrowThread.Enqueue(borrowed);
            }
        }
    }

    
	
    /// <summary>
    /// <para>This represents that a sync value change has been processed.  Use the class name to determine the related value/type/context.</para>
	/// <para>Two major occassions this event will occur (use this.<see cref="SyncEvent_ValueChangeProcessed.Explanation"/> to know which of the two occasions this represents):</para>
    /// <para>1) For an outbound change being sent to remote recipients (in which case, this event is published just AFTER the change has been sent to remote sources; however, the remote recipients likely have NOT received/processed it yet.)</para>
    /// <para>2) For an inbound change received from a remote source (in which case, this event is published just AFTER the change has been applied)</para>
    /// </summary>
    [MessagePack.MessagePackObject]
    public sealed class SyncEvent_GONetSampleInputSync_GetKey_A : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public System.Boolean valuePrevious;
		[MessagePack.Key(7)] public System.Boolean valueNew;

        static readonly Utils.ObjectPool<SyncEvent_GONetSampleInputSync_GetKey_A> pool = new Utils.ObjectPool<SyncEvent_GONetSampleInputSync_GetKey_A>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 1);
        static readonly System.Collections.Concurrent.ConcurrentQueue<SyncEvent_GONetSampleInputSync_GetKey_A> returnQueue_onceOnBorrowThread = new System.Collections.Concurrent.ConcurrentQueue<SyncEvent_GONetSampleInputSync_GetKey_A>();
        static System.Threading.Thread borrowThread;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// Instead, call <see cref="Borrow(SyncEvent_ValueChangeProcessedExplanation, long, uint, uint, byte, System.Boolean, System.Boolean)"/>.
        /// </summary>
        public SyncEvent_GONetSampleInputSync_GetKey_A() { }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to ensure the instance returned from this method is also returned back
        ///            here (i.e., to private object pool) via <see cref="Return(SyncEvent_GONetSampleInputSync_GetKey_A)"/> when no longer needed!
        /// </summary>
        public static SyncEvent_GONetSampleInputSync_GetKey_A Borrow(SyncEvent_ValueChangeProcessedExplanation explanation, long occurredAtElapsedTicks, ushort relatedOwnerAuthorityId, uint gonetId, byte codeGenerationId, byte syncMemberIndex, System.Boolean valuePrevious, System.Boolean valueNew)
		{
            if (borrowThread == null)
            {
                borrowThread = System.Threading.Thread.CurrentThread;
            }
            else if (borrowThread != System.Threading.Thread.CurrentThread)
            {
                const string REQUIRED_CALL_SAME_BORROW_THREAD = "Not allowed to call this from more than one thread.  So, ensure Borrow() is called from the same exact thread for this specific event type.  NOTE: Each event type can have its' Borrow() called from a different thread from one another.";
                throw new InvalidOperationException(REQUIRED_CALL_SAME_BORROW_THREAD);
            }

            int autoReturnCount = returnQueue_onceOnBorrowThread.Count;
            SyncEvent_GONetSampleInputSync_GetKey_A autoReturn;
            while (returnQueue_onceOnBorrowThread.TryDequeue(out autoReturn) && autoReturnCount > 0)
            {
                Return(autoReturn);
                ++autoReturnCount;
            }

            var @event = pool.Borrow();
            
            @event.Explanation = explanation;
            @event.OccurredAtElapsedTicks = occurredAtElapsedTicks;
            @event.RelatedOwnerAuthorityId = relatedOwnerAuthorityId;
            @event.GONetId = gonetId;
			@event.CodeGenerationId = codeGenerationId;
            @event.SyncMemberIndex = syncMemberIndex;
			@event.valuePrevious = valuePrevious;
            @event.valueNew = valueNew;

            return @event;
		}

        public override void Return()
		{
			Return(this);
		}

        public static void Return(SyncEvent_GONetSampleInputSync_GetKey_A borrowed)
        {
            if (borrowThread == System.Threading.Thread.CurrentThread)
            {
                pool.Return(borrowed);
            }
            else
            {
                returnQueue_onceOnBorrowThread.Enqueue(borrowed);
            }
        }
    }

    
	
    /// <summary>
    /// <para>This represents that a sync value change has been processed.  Use the class name to determine the related value/type/context.</para>
	/// <para>Two major occassions this event will occur (use this.<see cref="SyncEvent_ValueChangeProcessed.Explanation"/> to know which of the two occasions this represents):</para>
    /// <para>1) For an outbound change being sent to remote recipients (in which case, this event is published just AFTER the change has been sent to remote sources; however, the remote recipients likely have NOT received/processed it yet.)</para>
    /// <para>2) For an inbound change received from a remote source (in which case, this event is published just AFTER the change has been applied)</para>
    /// </summary>
    [MessagePack.MessagePackObject]
    public sealed class SyncEvent_GONetSampleInputSync_GetKey_D : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public System.Boolean valuePrevious;
		[MessagePack.Key(7)] public System.Boolean valueNew;

        static readonly Utils.ObjectPool<SyncEvent_GONetSampleInputSync_GetKey_D> pool = new Utils.ObjectPool<SyncEvent_GONetSampleInputSync_GetKey_D>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 1);
        static readonly System.Collections.Concurrent.ConcurrentQueue<SyncEvent_GONetSampleInputSync_GetKey_D> returnQueue_onceOnBorrowThread = new System.Collections.Concurrent.ConcurrentQueue<SyncEvent_GONetSampleInputSync_GetKey_D>();
        static System.Threading.Thread borrowThread;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// Instead, call <see cref="Borrow(SyncEvent_ValueChangeProcessedExplanation, long, uint, uint, byte, System.Boolean, System.Boolean)"/>.
        /// </summary>
        public SyncEvent_GONetSampleInputSync_GetKey_D() { }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to ensure the instance returned from this method is also returned back
        ///            here (i.e., to private object pool) via <see cref="Return(SyncEvent_GONetSampleInputSync_GetKey_D)"/> when no longer needed!
        /// </summary>
        public static SyncEvent_GONetSampleInputSync_GetKey_D Borrow(SyncEvent_ValueChangeProcessedExplanation explanation, long occurredAtElapsedTicks, ushort relatedOwnerAuthorityId, uint gonetId, byte codeGenerationId, byte syncMemberIndex, System.Boolean valuePrevious, System.Boolean valueNew)
		{
            if (borrowThread == null)
            {
                borrowThread = System.Threading.Thread.CurrentThread;
            }
            else if (borrowThread != System.Threading.Thread.CurrentThread)
            {
                const string REQUIRED_CALL_SAME_BORROW_THREAD = "Not allowed to call this from more than one thread.  So, ensure Borrow() is called from the same exact thread for this specific event type.  NOTE: Each event type can have its' Borrow() called from a different thread from one another.";
                throw new InvalidOperationException(REQUIRED_CALL_SAME_BORROW_THREAD);
            }

            int autoReturnCount = returnQueue_onceOnBorrowThread.Count;
            SyncEvent_GONetSampleInputSync_GetKey_D autoReturn;
            while (returnQueue_onceOnBorrowThread.TryDequeue(out autoReturn) && autoReturnCount > 0)
            {
                Return(autoReturn);
                ++autoReturnCount;
            }

            var @event = pool.Borrow();
            
            @event.Explanation = explanation;
            @event.OccurredAtElapsedTicks = occurredAtElapsedTicks;
            @event.RelatedOwnerAuthorityId = relatedOwnerAuthorityId;
            @event.GONetId = gonetId;
			@event.CodeGenerationId = codeGenerationId;
            @event.SyncMemberIndex = syncMemberIndex;
			@event.valuePrevious = valuePrevious;
            @event.valueNew = valueNew;

            return @event;
		}

        public override void Return()
		{
			Return(this);
		}

        public static void Return(SyncEvent_GONetSampleInputSync_GetKey_D borrowed)
        {
            if (borrowThread == System.Threading.Thread.CurrentThread)
            {
                pool.Return(borrowed);
            }
            else
            {
                returnQueue_onceOnBorrowThread.Enqueue(borrowed);
            }
        }
    }

    
	
    /// <summary>
    /// <para>This represents that a sync value change has been processed.  Use the class name to determine the related value/type/context.</para>
	/// <para>Two major occassions this event will occur (use this.<see cref="SyncEvent_ValueChangeProcessed.Explanation"/> to know which of the two occasions this represents):</para>
    /// <para>1) For an outbound change being sent to remote recipients (in which case, this event is published just AFTER the change has been sent to remote sources; however, the remote recipients likely have NOT received/processed it yet.)</para>
    /// <para>2) For an inbound change received from a remote source (in which case, this event is published just AFTER the change has been applied)</para>
    /// </summary>
    [MessagePack.MessagePackObject]
    public sealed class SyncEvent_GONetSampleInputSync_GetKey_DownArrow : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public System.Boolean valuePrevious;
		[MessagePack.Key(7)] public System.Boolean valueNew;

        static readonly Utils.ObjectPool<SyncEvent_GONetSampleInputSync_GetKey_DownArrow> pool = new Utils.ObjectPool<SyncEvent_GONetSampleInputSync_GetKey_DownArrow>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 1);
        static readonly System.Collections.Concurrent.ConcurrentQueue<SyncEvent_GONetSampleInputSync_GetKey_DownArrow> returnQueue_onceOnBorrowThread = new System.Collections.Concurrent.ConcurrentQueue<SyncEvent_GONetSampleInputSync_GetKey_DownArrow>();
        static System.Threading.Thread borrowThread;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// Instead, call <see cref="Borrow(SyncEvent_ValueChangeProcessedExplanation, long, uint, uint, byte, System.Boolean, System.Boolean)"/>.
        /// </summary>
        public SyncEvent_GONetSampleInputSync_GetKey_DownArrow() { }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to ensure the instance returned from this method is also returned back
        ///            here (i.e., to private object pool) via <see cref="Return(SyncEvent_GONetSampleInputSync_GetKey_DownArrow)"/> when no longer needed!
        /// </summary>
        public static SyncEvent_GONetSampleInputSync_GetKey_DownArrow Borrow(SyncEvent_ValueChangeProcessedExplanation explanation, long occurredAtElapsedTicks, ushort relatedOwnerAuthorityId, uint gonetId, byte codeGenerationId, byte syncMemberIndex, System.Boolean valuePrevious, System.Boolean valueNew)
		{
            if (borrowThread == null)
            {
                borrowThread = System.Threading.Thread.CurrentThread;
            }
            else if (borrowThread != System.Threading.Thread.CurrentThread)
            {
                const string REQUIRED_CALL_SAME_BORROW_THREAD = "Not allowed to call this from more than one thread.  So, ensure Borrow() is called from the same exact thread for this specific event type.  NOTE: Each event type can have its' Borrow() called from a different thread from one another.";
                throw new InvalidOperationException(REQUIRED_CALL_SAME_BORROW_THREAD);
            }

            int autoReturnCount = returnQueue_onceOnBorrowThread.Count;
            SyncEvent_GONetSampleInputSync_GetKey_DownArrow autoReturn;
            while (returnQueue_onceOnBorrowThread.TryDequeue(out autoReturn) && autoReturnCount > 0)
            {
                Return(autoReturn);
                ++autoReturnCount;
            }

            var @event = pool.Borrow();
            
            @event.Explanation = explanation;
            @event.OccurredAtElapsedTicks = occurredAtElapsedTicks;
            @event.RelatedOwnerAuthorityId = relatedOwnerAuthorityId;
            @event.GONetId = gonetId;
			@event.CodeGenerationId = codeGenerationId;
            @event.SyncMemberIndex = syncMemberIndex;
			@event.valuePrevious = valuePrevious;
            @event.valueNew = valueNew;

            return @event;
		}

        public override void Return()
		{
			Return(this);
		}

        public static void Return(SyncEvent_GONetSampleInputSync_GetKey_DownArrow borrowed)
        {
            if (borrowThread == System.Threading.Thread.CurrentThread)
            {
                pool.Return(borrowed);
            }
            else
            {
                returnQueue_onceOnBorrowThread.Enqueue(borrowed);
            }
        }
    }

    
	
    /// <summary>
    /// <para>This represents that a sync value change has been processed.  Use the class name to determine the related value/type/context.</para>
	/// <para>Two major occassions this event will occur (use this.<see cref="SyncEvent_ValueChangeProcessed.Explanation"/> to know which of the two occasions this represents):</para>
    /// <para>1) For an outbound change being sent to remote recipients (in which case, this event is published just AFTER the change has been sent to remote sources; however, the remote recipients likely have NOT received/processed it yet.)</para>
    /// <para>2) For an inbound change received from a remote source (in which case, this event is published just AFTER the change has been applied)</para>
    /// </summary>
    [MessagePack.MessagePackObject]
    public sealed class SyncEvent_GONetSampleInputSync_GetKey_LeftArrow : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public System.Boolean valuePrevious;
		[MessagePack.Key(7)] public System.Boolean valueNew;

        static readonly Utils.ObjectPool<SyncEvent_GONetSampleInputSync_GetKey_LeftArrow> pool = new Utils.ObjectPool<SyncEvent_GONetSampleInputSync_GetKey_LeftArrow>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 1);
        static readonly System.Collections.Concurrent.ConcurrentQueue<SyncEvent_GONetSampleInputSync_GetKey_LeftArrow> returnQueue_onceOnBorrowThread = new System.Collections.Concurrent.ConcurrentQueue<SyncEvent_GONetSampleInputSync_GetKey_LeftArrow>();
        static System.Threading.Thread borrowThread;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// Instead, call <see cref="Borrow(SyncEvent_ValueChangeProcessedExplanation, long, uint, uint, byte, System.Boolean, System.Boolean)"/>.
        /// </summary>
        public SyncEvent_GONetSampleInputSync_GetKey_LeftArrow() { }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to ensure the instance returned from this method is also returned back
        ///            here (i.e., to private object pool) via <see cref="Return(SyncEvent_GONetSampleInputSync_GetKey_LeftArrow)"/> when no longer needed!
        /// </summary>
        public static SyncEvent_GONetSampleInputSync_GetKey_LeftArrow Borrow(SyncEvent_ValueChangeProcessedExplanation explanation, long occurredAtElapsedTicks, ushort relatedOwnerAuthorityId, uint gonetId, byte codeGenerationId, byte syncMemberIndex, System.Boolean valuePrevious, System.Boolean valueNew)
		{
            if (borrowThread == null)
            {
                borrowThread = System.Threading.Thread.CurrentThread;
            }
            else if (borrowThread != System.Threading.Thread.CurrentThread)
            {
                const string REQUIRED_CALL_SAME_BORROW_THREAD = "Not allowed to call this from more than one thread.  So, ensure Borrow() is called from the same exact thread for this specific event type.  NOTE: Each event type can have its' Borrow() called from a different thread from one another.";
                throw new InvalidOperationException(REQUIRED_CALL_SAME_BORROW_THREAD);
            }

            int autoReturnCount = returnQueue_onceOnBorrowThread.Count;
            SyncEvent_GONetSampleInputSync_GetKey_LeftArrow autoReturn;
            while (returnQueue_onceOnBorrowThread.TryDequeue(out autoReturn) && autoReturnCount > 0)
            {
                Return(autoReturn);
                ++autoReturnCount;
            }

            var @event = pool.Borrow();
            
            @event.Explanation = explanation;
            @event.OccurredAtElapsedTicks = occurredAtElapsedTicks;
            @event.RelatedOwnerAuthorityId = relatedOwnerAuthorityId;
            @event.GONetId = gonetId;
			@event.CodeGenerationId = codeGenerationId;
            @event.SyncMemberIndex = syncMemberIndex;
			@event.valuePrevious = valuePrevious;
            @event.valueNew = valueNew;

            return @event;
		}

        public override void Return()
		{
			Return(this);
		}

        public static void Return(SyncEvent_GONetSampleInputSync_GetKey_LeftArrow borrowed)
        {
            if (borrowThread == System.Threading.Thread.CurrentThread)
            {
                pool.Return(borrowed);
            }
            else
            {
                returnQueue_onceOnBorrowThread.Enqueue(borrowed);
            }
        }
    }

    
	
    /// <summary>
    /// <para>This represents that a sync value change has been processed.  Use the class name to determine the related value/type/context.</para>
	/// <para>Two major occassions this event will occur (use this.<see cref="SyncEvent_ValueChangeProcessed.Explanation"/> to know which of the two occasions this represents):</para>
    /// <para>1) For an outbound change being sent to remote recipients (in which case, this event is published just AFTER the change has been sent to remote sources; however, the remote recipients likely have NOT received/processed it yet.)</para>
    /// <para>2) For an inbound change received from a remote source (in which case, this event is published just AFTER the change has been applied)</para>
    /// </summary>
    [MessagePack.MessagePackObject]
    public sealed class SyncEvent_GONetSampleInputSync_GetKey_RightArrow : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public System.Boolean valuePrevious;
		[MessagePack.Key(7)] public System.Boolean valueNew;

        static readonly Utils.ObjectPool<SyncEvent_GONetSampleInputSync_GetKey_RightArrow> pool = new Utils.ObjectPool<SyncEvent_GONetSampleInputSync_GetKey_RightArrow>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 1);
        static readonly System.Collections.Concurrent.ConcurrentQueue<SyncEvent_GONetSampleInputSync_GetKey_RightArrow> returnQueue_onceOnBorrowThread = new System.Collections.Concurrent.ConcurrentQueue<SyncEvent_GONetSampleInputSync_GetKey_RightArrow>();
        static System.Threading.Thread borrowThread;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// Instead, call <see cref="Borrow(SyncEvent_ValueChangeProcessedExplanation, long, uint, uint, byte, System.Boolean, System.Boolean)"/>.
        /// </summary>
        public SyncEvent_GONetSampleInputSync_GetKey_RightArrow() { }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to ensure the instance returned from this method is also returned back
        ///            here (i.e., to private object pool) via <see cref="Return(SyncEvent_GONetSampleInputSync_GetKey_RightArrow)"/> when no longer needed!
        /// </summary>
        public static SyncEvent_GONetSampleInputSync_GetKey_RightArrow Borrow(SyncEvent_ValueChangeProcessedExplanation explanation, long occurredAtElapsedTicks, ushort relatedOwnerAuthorityId, uint gonetId, byte codeGenerationId, byte syncMemberIndex, System.Boolean valuePrevious, System.Boolean valueNew)
		{
            if (borrowThread == null)
            {
                borrowThread = System.Threading.Thread.CurrentThread;
            }
            else if (borrowThread != System.Threading.Thread.CurrentThread)
            {
                const string REQUIRED_CALL_SAME_BORROW_THREAD = "Not allowed to call this from more than one thread.  So, ensure Borrow() is called from the same exact thread for this specific event type.  NOTE: Each event type can have its' Borrow() called from a different thread from one another.";
                throw new InvalidOperationException(REQUIRED_CALL_SAME_BORROW_THREAD);
            }

            int autoReturnCount = returnQueue_onceOnBorrowThread.Count;
            SyncEvent_GONetSampleInputSync_GetKey_RightArrow autoReturn;
            while (returnQueue_onceOnBorrowThread.TryDequeue(out autoReturn) && autoReturnCount > 0)
            {
                Return(autoReturn);
                ++autoReturnCount;
            }

            var @event = pool.Borrow();
            
            @event.Explanation = explanation;
            @event.OccurredAtElapsedTicks = occurredAtElapsedTicks;
            @event.RelatedOwnerAuthorityId = relatedOwnerAuthorityId;
            @event.GONetId = gonetId;
			@event.CodeGenerationId = codeGenerationId;
            @event.SyncMemberIndex = syncMemberIndex;
			@event.valuePrevious = valuePrevious;
            @event.valueNew = valueNew;

            return @event;
		}

        public override void Return()
		{
			Return(this);
		}

        public static void Return(SyncEvent_GONetSampleInputSync_GetKey_RightArrow borrowed)
        {
            if (borrowThread == System.Threading.Thread.CurrentThread)
            {
                pool.Return(borrowed);
            }
            else
            {
                returnQueue_onceOnBorrowThread.Enqueue(borrowed);
            }
        }
    }

    
	
    /// <summary>
    /// <para>This represents that a sync value change has been processed.  Use the class name to determine the related value/type/context.</para>
	/// <para>Two major occassions this event will occur (use this.<see cref="SyncEvent_ValueChangeProcessed.Explanation"/> to know which of the two occasions this represents):</para>
    /// <para>1) For an outbound change being sent to remote recipients (in which case, this event is published just AFTER the change has been sent to remote sources; however, the remote recipients likely have NOT received/processed it yet.)</para>
    /// <para>2) For an inbound change received from a remote source (in which case, this event is published just AFTER the change has been applied)</para>
    /// </summary>
    [MessagePack.MessagePackObject]
    public sealed class SyncEvent_GONetSampleInputSync_GetKey_S : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public System.Boolean valuePrevious;
		[MessagePack.Key(7)] public System.Boolean valueNew;

        static readonly Utils.ObjectPool<SyncEvent_GONetSampleInputSync_GetKey_S> pool = new Utils.ObjectPool<SyncEvent_GONetSampleInputSync_GetKey_S>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 1);
        static readonly System.Collections.Concurrent.ConcurrentQueue<SyncEvent_GONetSampleInputSync_GetKey_S> returnQueue_onceOnBorrowThread = new System.Collections.Concurrent.ConcurrentQueue<SyncEvent_GONetSampleInputSync_GetKey_S>();
        static System.Threading.Thread borrowThread;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// Instead, call <see cref="Borrow(SyncEvent_ValueChangeProcessedExplanation, long, uint, uint, byte, System.Boolean, System.Boolean)"/>.
        /// </summary>
        public SyncEvent_GONetSampleInputSync_GetKey_S() { }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to ensure the instance returned from this method is also returned back
        ///            here (i.e., to private object pool) via <see cref="Return(SyncEvent_GONetSampleInputSync_GetKey_S)"/> when no longer needed!
        /// </summary>
        public static SyncEvent_GONetSampleInputSync_GetKey_S Borrow(SyncEvent_ValueChangeProcessedExplanation explanation, long occurredAtElapsedTicks, ushort relatedOwnerAuthorityId, uint gonetId, byte codeGenerationId, byte syncMemberIndex, System.Boolean valuePrevious, System.Boolean valueNew)
		{
            if (borrowThread == null)
            {
                borrowThread = System.Threading.Thread.CurrentThread;
            }
            else if (borrowThread != System.Threading.Thread.CurrentThread)
            {
                const string REQUIRED_CALL_SAME_BORROW_THREAD = "Not allowed to call this from more than one thread.  So, ensure Borrow() is called from the same exact thread for this specific event type.  NOTE: Each event type can have its' Borrow() called from a different thread from one another.";
                throw new InvalidOperationException(REQUIRED_CALL_SAME_BORROW_THREAD);
            }

            int autoReturnCount = returnQueue_onceOnBorrowThread.Count;
            SyncEvent_GONetSampleInputSync_GetKey_S autoReturn;
            while (returnQueue_onceOnBorrowThread.TryDequeue(out autoReturn) && autoReturnCount > 0)
            {
                Return(autoReturn);
                ++autoReturnCount;
            }

            var @event = pool.Borrow();
            
            @event.Explanation = explanation;
            @event.OccurredAtElapsedTicks = occurredAtElapsedTicks;
            @event.RelatedOwnerAuthorityId = relatedOwnerAuthorityId;
            @event.GONetId = gonetId;
			@event.CodeGenerationId = codeGenerationId;
            @event.SyncMemberIndex = syncMemberIndex;
			@event.valuePrevious = valuePrevious;
            @event.valueNew = valueNew;

            return @event;
		}

        public override void Return()
		{
			Return(this);
		}

        public static void Return(SyncEvent_GONetSampleInputSync_GetKey_S borrowed)
        {
            if (borrowThread == System.Threading.Thread.CurrentThread)
            {
                pool.Return(borrowed);
            }
            else
            {
                returnQueue_onceOnBorrowThread.Enqueue(borrowed);
            }
        }
    }

    
	
    /// <summary>
    /// <para>This represents that a sync value change has been processed.  Use the class name to determine the related value/type/context.</para>
	/// <para>Two major occassions this event will occur (use this.<see cref="SyncEvent_ValueChangeProcessed.Explanation"/> to know which of the two occasions this represents):</para>
    /// <para>1) For an outbound change being sent to remote recipients (in which case, this event is published just AFTER the change has been sent to remote sources; however, the remote recipients likely have NOT received/processed it yet.)</para>
    /// <para>2) For an inbound change received from a remote source (in which case, this event is published just AFTER the change has been applied)</para>
    /// </summary>
    [MessagePack.MessagePackObject]
    public sealed class SyncEvent_GONetSampleInputSync_GetKey_UpArrow : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public System.Boolean valuePrevious;
		[MessagePack.Key(7)] public System.Boolean valueNew;

        static readonly Utils.ObjectPool<SyncEvent_GONetSampleInputSync_GetKey_UpArrow> pool = new Utils.ObjectPool<SyncEvent_GONetSampleInputSync_GetKey_UpArrow>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 1);
        static readonly System.Collections.Concurrent.ConcurrentQueue<SyncEvent_GONetSampleInputSync_GetKey_UpArrow> returnQueue_onceOnBorrowThread = new System.Collections.Concurrent.ConcurrentQueue<SyncEvent_GONetSampleInputSync_GetKey_UpArrow>();
        static System.Threading.Thread borrowThread;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// Instead, call <see cref="Borrow(SyncEvent_ValueChangeProcessedExplanation, long, uint, uint, byte, System.Boolean, System.Boolean)"/>.
        /// </summary>
        public SyncEvent_GONetSampleInputSync_GetKey_UpArrow() { }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to ensure the instance returned from this method is also returned back
        ///            here (i.e., to private object pool) via <see cref="Return(SyncEvent_GONetSampleInputSync_GetKey_UpArrow)"/> when no longer needed!
        /// </summary>
        public static SyncEvent_GONetSampleInputSync_GetKey_UpArrow Borrow(SyncEvent_ValueChangeProcessedExplanation explanation, long occurredAtElapsedTicks, ushort relatedOwnerAuthorityId, uint gonetId, byte codeGenerationId, byte syncMemberIndex, System.Boolean valuePrevious, System.Boolean valueNew)
		{
            if (borrowThread == null)
            {
                borrowThread = System.Threading.Thread.CurrentThread;
            }
            else if (borrowThread != System.Threading.Thread.CurrentThread)
            {
                const string REQUIRED_CALL_SAME_BORROW_THREAD = "Not allowed to call this from more than one thread.  So, ensure Borrow() is called from the same exact thread for this specific event type.  NOTE: Each event type can have its' Borrow() called from a different thread from one another.";
                throw new InvalidOperationException(REQUIRED_CALL_SAME_BORROW_THREAD);
            }

            int autoReturnCount = returnQueue_onceOnBorrowThread.Count;
            SyncEvent_GONetSampleInputSync_GetKey_UpArrow autoReturn;
            while (returnQueue_onceOnBorrowThread.TryDequeue(out autoReturn) && autoReturnCount > 0)
            {
                Return(autoReturn);
                ++autoReturnCount;
            }

            var @event = pool.Borrow();
            
            @event.Explanation = explanation;
            @event.OccurredAtElapsedTicks = occurredAtElapsedTicks;
            @event.RelatedOwnerAuthorityId = relatedOwnerAuthorityId;
            @event.GONetId = gonetId;
			@event.CodeGenerationId = codeGenerationId;
            @event.SyncMemberIndex = syncMemberIndex;
			@event.valuePrevious = valuePrevious;
            @event.valueNew = valueNew;

            return @event;
		}

        public override void Return()
		{
			Return(this);
		}

        public static void Return(SyncEvent_GONetSampleInputSync_GetKey_UpArrow borrowed)
        {
            if (borrowThread == System.Threading.Thread.CurrentThread)
            {
                pool.Return(borrowed);
            }
            else
            {
                returnQueue_onceOnBorrowThread.Enqueue(borrowed);
            }
        }
    }

    
	
    /// <summary>
    /// <para>This represents that a sync value change has been processed.  Use the class name to determine the related value/type/context.</para>
	/// <para>Two major occassions this event will occur (use this.<see cref="SyncEvent_ValueChangeProcessed.Explanation"/> to know which of the two occasions this represents):</para>
    /// <para>1) For an outbound change being sent to remote recipients (in which case, this event is published just AFTER the change has been sent to remote sources; however, the remote recipients likely have NOT received/processed it yet.)</para>
    /// <para>2) For an inbound change received from a remote source (in which case, this event is published just AFTER the change has been applied)</para>
    /// </summary>
    [MessagePack.MessagePackObject]
    public sealed class SyncEvent_GONetSampleInputSync_GetKey_W : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public System.Boolean valuePrevious;
		[MessagePack.Key(7)] public System.Boolean valueNew;

        static readonly Utils.ObjectPool<SyncEvent_GONetSampleInputSync_GetKey_W> pool = new Utils.ObjectPool<SyncEvent_GONetSampleInputSync_GetKey_W>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 1);
        static readonly System.Collections.Concurrent.ConcurrentQueue<SyncEvent_GONetSampleInputSync_GetKey_W> returnQueue_onceOnBorrowThread = new System.Collections.Concurrent.ConcurrentQueue<SyncEvent_GONetSampleInputSync_GetKey_W>();
        static System.Threading.Thread borrowThread;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// Instead, call <see cref="Borrow(SyncEvent_ValueChangeProcessedExplanation, long, uint, uint, byte, System.Boolean, System.Boolean)"/>.
        /// </summary>
        public SyncEvent_GONetSampleInputSync_GetKey_W() { }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to ensure the instance returned from this method is also returned back
        ///            here (i.e., to private object pool) via <see cref="Return(SyncEvent_GONetSampleInputSync_GetKey_W)"/> when no longer needed!
        /// </summary>
        public static SyncEvent_GONetSampleInputSync_GetKey_W Borrow(SyncEvent_ValueChangeProcessedExplanation explanation, long occurredAtElapsedTicks, ushort relatedOwnerAuthorityId, uint gonetId, byte codeGenerationId, byte syncMemberIndex, System.Boolean valuePrevious, System.Boolean valueNew)
		{
            if (borrowThread == null)
            {
                borrowThread = System.Threading.Thread.CurrentThread;
            }
            else if (borrowThread != System.Threading.Thread.CurrentThread)
            {
                const string REQUIRED_CALL_SAME_BORROW_THREAD = "Not allowed to call this from more than one thread.  So, ensure Borrow() is called from the same exact thread for this specific event type.  NOTE: Each event type can have its' Borrow() called from a different thread from one another.";
                throw new InvalidOperationException(REQUIRED_CALL_SAME_BORROW_THREAD);
            }

            int autoReturnCount = returnQueue_onceOnBorrowThread.Count;
            SyncEvent_GONetSampleInputSync_GetKey_W autoReturn;
            while (returnQueue_onceOnBorrowThread.TryDequeue(out autoReturn) && autoReturnCount > 0)
            {
                Return(autoReturn);
                ++autoReturnCount;
            }

            var @event = pool.Borrow();
            
            @event.Explanation = explanation;
            @event.OccurredAtElapsedTicks = occurredAtElapsedTicks;
            @event.RelatedOwnerAuthorityId = relatedOwnerAuthorityId;
            @event.GONetId = gonetId;
			@event.CodeGenerationId = codeGenerationId;
            @event.SyncMemberIndex = syncMemberIndex;
			@event.valuePrevious = valuePrevious;
            @event.valueNew = valueNew;

            return @event;
		}

        public override void Return()
		{
			Return(this);
		}

        public static void Return(SyncEvent_GONetSampleInputSync_GetKey_W borrowed)
        {
            if (borrowThread == System.Threading.Thread.CurrentThread)
            {
                pool.Return(borrowed);
            }
            else
            {
                returnQueue_onceOnBorrowThread.Enqueue(borrowed);
            }
        }
    }

    
	
    /// <summary>
    /// <para>This represents that a sync value change has been processed.  Use the class name to determine the related value/type/context.</para>
	/// <para>Two major occassions this event will occur (use this.<see cref="SyncEvent_ValueChangeProcessed.Explanation"/> to know which of the two occasions this represents):</para>
    /// <para>1) For an outbound change being sent to remote recipients (in which case, this event is published just AFTER the change has been sent to remote sources; however, the remote recipients likely have NOT received/processed it yet.)</para>
    /// <para>2) For an inbound change received from a remote source (in which case, this event is published just AFTER the change has been applied)</para>
    /// </summary>
    [MessagePack.MessagePackObject]
    public sealed class SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public System.Single valuePrevious;
		[MessagePack.Key(7)] public System.Single valueNew;

        static readonly Utils.ObjectPool<SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate> pool = new Utils.ObjectPool<SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 1);
        static readonly System.Collections.Concurrent.ConcurrentQueue<SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate> returnQueue_onceOnBorrowThread = new System.Collections.Concurrent.ConcurrentQueue<SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate>();
        static System.Threading.Thread borrowThread;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// Instead, call <see cref="Borrow(SyncEvent_ValueChangeProcessedExplanation, long, uint, uint, byte, System.Single, System.Single)"/>.
        /// </summary>
        public SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate() { }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to ensure the instance returned from this method is also returned back
        ///            here (i.e., to private object pool) via <see cref="Return(SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate)"/> when no longer needed!
        /// </summary>
        public static SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate Borrow(SyncEvent_ValueChangeProcessedExplanation explanation, long occurredAtElapsedTicks, ushort relatedOwnerAuthorityId, uint gonetId, byte codeGenerationId, byte syncMemberIndex, System.Single valuePrevious, System.Single valueNew)
		{
            if (borrowThread == null)
            {
                borrowThread = System.Threading.Thread.CurrentThread;
            }
            else if (borrowThread != System.Threading.Thread.CurrentThread)
            {
                const string REQUIRED_CALL_SAME_BORROW_THREAD = "Not allowed to call this from more than one thread.  So, ensure Borrow() is called from the same exact thread for this specific event type.  NOTE: Each event type can have its' Borrow() called from a different thread from one another.";
                throw new InvalidOperationException(REQUIRED_CALL_SAME_BORROW_THREAD);
            }

            int autoReturnCount = returnQueue_onceOnBorrowThread.Count;
            SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate autoReturn;
            while (returnQueue_onceOnBorrowThread.TryDequeue(out autoReturn) && autoReturnCount > 0)
            {
                Return(autoReturn);
                ++autoReturnCount;
            }

            var @event = pool.Borrow();
            
            @event.Explanation = explanation;
            @event.OccurredAtElapsedTicks = occurredAtElapsedTicks;
            @event.RelatedOwnerAuthorityId = relatedOwnerAuthorityId;
            @event.GONetId = gonetId;
			@event.CodeGenerationId = codeGenerationId;
            @event.SyncMemberIndex = syncMemberIndex;
			@event.valuePrevious = valuePrevious;
            @event.valueNew = valueNew;

            return @event;
		}

        public override void Return()
		{
			Return(this);
		}

        public static void Return(SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate borrowed)
        {
            if (borrowThread == System.Threading.Thread.CurrentThread)
            {
                pool.Return(borrowed);
            }
            else
            {
                returnQueue_onceOnBorrowThread.Enqueue(borrowed);
            }
        }
    }

    
	
    /// <summary>
    /// <para>This represents that a sync value change has been processed.  Use the class name to determine the related value/type/context.</para>
	/// <para>Two major occassions this event will occur (use this.<see cref="SyncEvent_ValueChangeProcessed.Explanation"/> to know which of the two occasions this represents):</para>
    /// <para>1) For an outbound change being sent to remote recipients (in which case, this event is published just AFTER the change has been sent to remote sources; however, the remote recipients likely have NOT received/processed it yet.)</para>
    /// <para>2) For an inbound change received from a remote source (in which case, this event is published just AFTER the change has been applied)</para>
    /// </summary>
    [MessagePack.MessagePackObject]
    public sealed class SyncEvent_FieldChangeTest_color : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public UnityEngine.Vector3 valuePrevious;
		[MessagePack.Key(7)] public UnityEngine.Vector3 valueNew;

        static readonly Utils.ObjectPool<SyncEvent_FieldChangeTest_color> pool = new Utils.ObjectPool<SyncEvent_FieldChangeTest_color>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 1);
        static readonly System.Collections.Concurrent.ConcurrentQueue<SyncEvent_FieldChangeTest_color> returnQueue_onceOnBorrowThread = new System.Collections.Concurrent.ConcurrentQueue<SyncEvent_FieldChangeTest_color>();
        static System.Threading.Thread borrowThread;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// Instead, call <see cref="Borrow(SyncEvent_ValueChangeProcessedExplanation, long, uint, uint, byte, UnityEngine.Vector3, UnityEngine.Vector3)"/>.
        /// </summary>
        public SyncEvent_FieldChangeTest_color() { }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to ensure the instance returned from this method is also returned back
        ///            here (i.e., to private object pool) via <see cref="Return(SyncEvent_FieldChangeTest_color)"/> when no longer needed!
        /// </summary>
        public static SyncEvent_FieldChangeTest_color Borrow(SyncEvent_ValueChangeProcessedExplanation explanation, long occurredAtElapsedTicks, ushort relatedOwnerAuthorityId, uint gonetId, byte codeGenerationId, byte syncMemberIndex, UnityEngine.Vector3 valuePrevious, UnityEngine.Vector3 valueNew)
		{
            if (borrowThread == null)
            {
                borrowThread = System.Threading.Thread.CurrentThread;
            }
            else if (borrowThread != System.Threading.Thread.CurrentThread)
            {
                const string REQUIRED_CALL_SAME_BORROW_THREAD = "Not allowed to call this from more than one thread.  So, ensure Borrow() is called from the same exact thread for this specific event type.  NOTE: Each event type can have its' Borrow() called from a different thread from one another.";
                throw new InvalidOperationException(REQUIRED_CALL_SAME_BORROW_THREAD);
            }

            int autoReturnCount = returnQueue_onceOnBorrowThread.Count;
            SyncEvent_FieldChangeTest_color autoReturn;
            while (returnQueue_onceOnBorrowThread.TryDequeue(out autoReturn) && autoReturnCount > 0)
            {
                Return(autoReturn);
                ++autoReturnCount;
            }

            var @event = pool.Borrow();
            
            @event.Explanation = explanation;
            @event.OccurredAtElapsedTicks = occurredAtElapsedTicks;
            @event.RelatedOwnerAuthorityId = relatedOwnerAuthorityId;
            @event.GONetId = gonetId;
			@event.CodeGenerationId = codeGenerationId;
            @event.SyncMemberIndex = syncMemberIndex;
			@event.valuePrevious = valuePrevious;
            @event.valueNew = valueNew;

            return @event;
		}

        public override void Return()
		{
			Return(this);
		}

        public static void Return(SyncEvent_FieldChangeTest_color borrowed)
        {
            if (borrowThread == System.Threading.Thread.CurrentThread)
            {
                pool.Return(borrowed);
            }
            else
            {
                returnQueue_onceOnBorrowThread.Enqueue(borrowed);
            }
        }
    }

    
	
    /// <summary>
    /// <para>This represents that a sync value change has been processed.  Use the class name to determine the related value/type/context.</para>
	/// <para>Two major occassions this event will occur (use this.<see cref="SyncEvent_ValueChangeProcessed.Explanation"/> to know which of the two occasions this represents):</para>
    /// <para>1) For an outbound change being sent to remote recipients (in which case, this event is published just AFTER the change has been sent to remote sources; however, the remote recipients likely have NOT received/processed it yet.)</para>
    /// <para>2) For an inbound change received from a remote source (in which case, this event is published just AFTER the change has been applied)</para>
    /// </summary>
    [MessagePack.MessagePackObject]
    public sealed class SyncEvent_FieldChangeTest_color_dosientos : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public UnityEngine.Vector3 valuePrevious;
		[MessagePack.Key(7)] public UnityEngine.Vector3 valueNew;

        static readonly Utils.ObjectPool<SyncEvent_FieldChangeTest_color_dosientos> pool = new Utils.ObjectPool<SyncEvent_FieldChangeTest_color_dosientos>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 1);
        static readonly System.Collections.Concurrent.ConcurrentQueue<SyncEvent_FieldChangeTest_color_dosientos> returnQueue_onceOnBorrowThread = new System.Collections.Concurrent.ConcurrentQueue<SyncEvent_FieldChangeTest_color_dosientos>();
        static System.Threading.Thread borrowThread;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// Instead, call <see cref="Borrow(SyncEvent_ValueChangeProcessedExplanation, long, uint, uint, byte, UnityEngine.Vector3, UnityEngine.Vector3)"/>.
        /// </summary>
        public SyncEvent_FieldChangeTest_color_dosientos() { }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to ensure the instance returned from this method is also returned back
        ///            here (i.e., to private object pool) via <see cref="Return(SyncEvent_FieldChangeTest_color_dosientos)"/> when no longer needed!
        /// </summary>
        public static SyncEvent_FieldChangeTest_color_dosientos Borrow(SyncEvent_ValueChangeProcessedExplanation explanation, long occurredAtElapsedTicks, ushort relatedOwnerAuthorityId, uint gonetId, byte codeGenerationId, byte syncMemberIndex, UnityEngine.Vector3 valuePrevious, UnityEngine.Vector3 valueNew)
		{
            if (borrowThread == null)
            {
                borrowThread = System.Threading.Thread.CurrentThread;
            }
            else if (borrowThread != System.Threading.Thread.CurrentThread)
            {
                const string REQUIRED_CALL_SAME_BORROW_THREAD = "Not allowed to call this from more than one thread.  So, ensure Borrow() is called from the same exact thread for this specific event type.  NOTE: Each event type can have its' Borrow() called from a different thread from one another.";
                throw new InvalidOperationException(REQUIRED_CALL_SAME_BORROW_THREAD);
            }

            int autoReturnCount = returnQueue_onceOnBorrowThread.Count;
            SyncEvent_FieldChangeTest_color_dosientos autoReturn;
            while (returnQueue_onceOnBorrowThread.TryDequeue(out autoReturn) && autoReturnCount > 0)
            {
                Return(autoReturn);
                ++autoReturnCount;
            }

            var @event = pool.Borrow();
            
            @event.Explanation = explanation;
            @event.OccurredAtElapsedTicks = occurredAtElapsedTicks;
            @event.RelatedOwnerAuthorityId = relatedOwnerAuthorityId;
            @event.GONetId = gonetId;
			@event.CodeGenerationId = codeGenerationId;
            @event.SyncMemberIndex = syncMemberIndex;
			@event.valuePrevious = valuePrevious;
            @event.valueNew = valueNew;

            return @event;
		}

        public override void Return()
		{
			Return(this);
		}

        public static void Return(SyncEvent_FieldChangeTest_color_dosientos borrowed)
        {
            if (borrowThread == System.Threading.Thread.CurrentThread)
            {
                pool.Return(borrowed);
            }
            else
            {
                returnQueue_onceOnBorrowThread.Enqueue(borrowed);
            }
        }
    }

    
	
    /// <summary>
    /// <para>This represents that a sync value change has been processed.  Use the class name to determine the related value/type/context.</para>
	/// <para>Two major occassions this event will occur (use this.<see cref="SyncEvent_ValueChangeProcessed.Explanation"/> to know which of the two occasions this represents):</para>
    /// <para>1) For an outbound change being sent to remote recipients (in which case, this event is published just AFTER the change has been sent to remote sources; however, the remote recipients likely have NOT received/processed it yet.)</para>
    /// <para>2) For an inbound change received from a remote source (in which case, this event is published just AFTER the change has been applied)</para>
    /// </summary>
    [MessagePack.MessagePackObject]
    public sealed class SyncEvent_FieldChangeTest_nada : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public System.Single valuePrevious;
		[MessagePack.Key(7)] public System.Single valueNew;

        static readonly Utils.ObjectPool<SyncEvent_FieldChangeTest_nada> pool = new Utils.ObjectPool<SyncEvent_FieldChangeTest_nada>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 1);
        static readonly System.Collections.Concurrent.ConcurrentQueue<SyncEvent_FieldChangeTest_nada> returnQueue_onceOnBorrowThread = new System.Collections.Concurrent.ConcurrentQueue<SyncEvent_FieldChangeTest_nada>();
        static System.Threading.Thread borrowThread;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// Instead, call <see cref="Borrow(SyncEvent_ValueChangeProcessedExplanation, long, uint, uint, byte, System.Single, System.Single)"/>.
        /// </summary>
        public SyncEvent_FieldChangeTest_nada() { }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to ensure the instance returned from this method is also returned back
        ///            here (i.e., to private object pool) via <see cref="Return(SyncEvent_FieldChangeTest_nada)"/> when no longer needed!
        /// </summary>
        public static SyncEvent_FieldChangeTest_nada Borrow(SyncEvent_ValueChangeProcessedExplanation explanation, long occurredAtElapsedTicks, ushort relatedOwnerAuthorityId, uint gonetId, byte codeGenerationId, byte syncMemberIndex, System.Single valuePrevious, System.Single valueNew)
		{
            if (borrowThread == null)
            {
                borrowThread = System.Threading.Thread.CurrentThread;
            }
            else if (borrowThread != System.Threading.Thread.CurrentThread)
            {
                const string REQUIRED_CALL_SAME_BORROW_THREAD = "Not allowed to call this from more than one thread.  So, ensure Borrow() is called from the same exact thread for this specific event type.  NOTE: Each event type can have its' Borrow() called from a different thread from one another.";
                throw new InvalidOperationException(REQUIRED_CALL_SAME_BORROW_THREAD);
            }

            int autoReturnCount = returnQueue_onceOnBorrowThread.Count;
            SyncEvent_FieldChangeTest_nada autoReturn;
            while (returnQueue_onceOnBorrowThread.TryDequeue(out autoReturn) && autoReturnCount > 0)
            {
                Return(autoReturn);
                ++autoReturnCount;
            }

            var @event = pool.Borrow();
            
            @event.Explanation = explanation;
            @event.OccurredAtElapsedTicks = occurredAtElapsedTicks;
            @event.RelatedOwnerAuthorityId = relatedOwnerAuthorityId;
            @event.GONetId = gonetId;
			@event.CodeGenerationId = codeGenerationId;
            @event.SyncMemberIndex = syncMemberIndex;
			@event.valuePrevious = valuePrevious;
            @event.valueNew = valueNew;

            return @event;
		}

        public override void Return()
		{
			Return(this);
		}

        public static void Return(SyncEvent_FieldChangeTest_nada borrowed)
        {
            if (borrowThread == System.Threading.Thread.CurrentThread)
            {
                pool.Return(borrowed);
            }
            else
            {
                returnQueue_onceOnBorrowThread.Enqueue(borrowed);
            }
        }
    }

    
	
    /// <summary>
    /// <para>This represents that a sync value change has been processed.  Use the class name to determine the related value/type/context.</para>
	/// <para>Two major occassions this event will occur (use this.<see cref="SyncEvent_ValueChangeProcessed.Explanation"/> to know which of the two occasions this represents):</para>
    /// <para>1) For an outbound change being sent to remote recipients (in which case, this event is published just AFTER the change has been sent to remote sources; however, the remote recipients likely have NOT received/processed it yet.)</para>
    /// <para>2) For an inbound change received from a remote source (in which case, this event is published just AFTER the change has been applied)</para>
    /// </summary>
    [MessagePack.MessagePackObject]
    public sealed class SyncEvent_FieldChangeTest_shortie : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public System.Int16 valuePrevious;
		[MessagePack.Key(7)] public System.Int16 valueNew;

        static readonly Utils.ObjectPool<SyncEvent_FieldChangeTest_shortie> pool = new Utils.ObjectPool<SyncEvent_FieldChangeTest_shortie>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 1);
        static readonly System.Collections.Concurrent.ConcurrentQueue<SyncEvent_FieldChangeTest_shortie> returnQueue_onceOnBorrowThread = new System.Collections.Concurrent.ConcurrentQueue<SyncEvent_FieldChangeTest_shortie>();
        static System.Threading.Thread borrowThread;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// Instead, call <see cref="Borrow(SyncEvent_ValueChangeProcessedExplanation, long, uint, uint, byte, System.Int16, System.Int16)"/>.
        /// </summary>
        public SyncEvent_FieldChangeTest_shortie() { }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to ensure the instance returned from this method is also returned back
        ///            here (i.e., to private object pool) via <see cref="Return(SyncEvent_FieldChangeTest_shortie)"/> when no longer needed!
        /// </summary>
        public static SyncEvent_FieldChangeTest_shortie Borrow(SyncEvent_ValueChangeProcessedExplanation explanation, long occurredAtElapsedTicks, ushort relatedOwnerAuthorityId, uint gonetId, byte codeGenerationId, byte syncMemberIndex, System.Int16 valuePrevious, System.Int16 valueNew)
		{
            if (borrowThread == null)
            {
                borrowThread = System.Threading.Thread.CurrentThread;
            }
            else if (borrowThread != System.Threading.Thread.CurrentThread)
            {
                const string REQUIRED_CALL_SAME_BORROW_THREAD = "Not allowed to call this from more than one thread.  So, ensure Borrow() is called from the same exact thread for this specific event type.  NOTE: Each event type can have its' Borrow() called from a different thread from one another.";
                throw new InvalidOperationException(REQUIRED_CALL_SAME_BORROW_THREAD);
            }

            int autoReturnCount = returnQueue_onceOnBorrowThread.Count;
            SyncEvent_FieldChangeTest_shortie autoReturn;
            while (returnQueue_onceOnBorrowThread.TryDequeue(out autoReturn) && autoReturnCount > 0)
            {
                Return(autoReturn);
                ++autoReturnCount;
            }

            var @event = pool.Borrow();
            
            @event.Explanation = explanation;
            @event.OccurredAtElapsedTicks = occurredAtElapsedTicks;
            @event.RelatedOwnerAuthorityId = relatedOwnerAuthorityId;
            @event.GONetId = gonetId;
			@event.CodeGenerationId = codeGenerationId;
            @event.SyncMemberIndex = syncMemberIndex;
			@event.valuePrevious = valuePrevious;
            @event.valueNew = valueNew;

            return @event;
		}

        public override void Return()
		{
			Return(this);
		}

        public static void Return(SyncEvent_FieldChangeTest_shortie borrowed)
        {
            if (borrowThread == System.Threading.Thread.CurrentThread)
            {
                pool.Return(borrowed);
            }
            else
            {
                returnQueue_onceOnBorrowThread.Enqueue(borrowed);
            }
        }
    }

    
	
    /// <summary>
    /// <para>This represents that a sync value change has been processed.  Use the class name to determine the related value/type/context.</para>
	/// <para>Two major occassions this event will occur (use this.<see cref="SyncEvent_ValueChangeProcessed.Explanation"/> to know which of the two occasions this represents):</para>
    /// <para>1) For an outbound change being sent to remote recipients (in which case, this event is published just AFTER the change has been sent to remote sources; however, the remote recipients likely have NOT received/processed it yet.)</para>
    /// <para>2) For an inbound change received from a remote source (in which case, this event is published just AFTER the change has been applied)</para>
    /// </summary>
    [MessagePack.MessagePackObject]
    public sealed class SyncEvent_AnimatorCharacterController_parameters_Speed : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public System.Single valuePrevious;
		[MessagePack.Key(7)] public System.Single valueNew;

        static readonly Utils.ObjectPool<SyncEvent_AnimatorCharacterController_parameters_Speed> pool = new Utils.ObjectPool<SyncEvent_AnimatorCharacterController_parameters_Speed>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 1);
        static readonly System.Collections.Concurrent.ConcurrentQueue<SyncEvent_AnimatorCharacterController_parameters_Speed> returnQueue_onceOnBorrowThread = new System.Collections.Concurrent.ConcurrentQueue<SyncEvent_AnimatorCharacterController_parameters_Speed>();
        static System.Threading.Thread borrowThread;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// Instead, call <see cref="Borrow(SyncEvent_ValueChangeProcessedExplanation, long, uint, uint, byte, System.Single, System.Single)"/>.
        /// </summary>
        public SyncEvent_AnimatorCharacterController_parameters_Speed() { }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to ensure the instance returned from this method is also returned back
        ///            here (i.e., to private object pool) via <see cref="Return(SyncEvent_AnimatorCharacterController_parameters_Speed)"/> when no longer needed!
        /// </summary>
        public static SyncEvent_AnimatorCharacterController_parameters_Speed Borrow(SyncEvent_ValueChangeProcessedExplanation explanation, long occurredAtElapsedTicks, ushort relatedOwnerAuthorityId, uint gonetId, byte codeGenerationId, byte syncMemberIndex, System.Single valuePrevious, System.Single valueNew)
		{
            if (borrowThread == null)
            {
                borrowThread = System.Threading.Thread.CurrentThread;
            }
            else if (borrowThread != System.Threading.Thread.CurrentThread)
            {
                const string REQUIRED_CALL_SAME_BORROW_THREAD = "Not allowed to call this from more than one thread.  So, ensure Borrow() is called from the same exact thread for this specific event type.  NOTE: Each event type can have its' Borrow() called from a different thread from one another.";
                throw new InvalidOperationException(REQUIRED_CALL_SAME_BORROW_THREAD);
            }

            int autoReturnCount = returnQueue_onceOnBorrowThread.Count;
            SyncEvent_AnimatorCharacterController_parameters_Speed autoReturn;
            while (returnQueue_onceOnBorrowThread.TryDequeue(out autoReturn) && autoReturnCount > 0)
            {
                Return(autoReturn);
                ++autoReturnCount;
            }

            var @event = pool.Borrow();
            
            @event.Explanation = explanation;
            @event.OccurredAtElapsedTicks = occurredAtElapsedTicks;
            @event.RelatedOwnerAuthorityId = relatedOwnerAuthorityId;
            @event.GONetId = gonetId;
			@event.CodeGenerationId = codeGenerationId;
            @event.SyncMemberIndex = syncMemberIndex;
			@event.valuePrevious = valuePrevious;
            @event.valueNew = valueNew;

            return @event;
		}

        public override void Return()
		{
			Return(this);
		}

        public static void Return(SyncEvent_AnimatorCharacterController_parameters_Speed borrowed)
        {
            if (borrowThread == System.Threading.Thread.CurrentThread)
            {
                pool.Return(borrowed);
            }
            else
            {
                returnQueue_onceOnBorrowThread.Enqueue(borrowed);
            }
        }
    }

    
	
    /// <summary>
    /// <para>This represents that a sync value change has been processed.  Use the class name to determine the related value/type/context.</para>
	/// <para>Two major occassions this event will occur (use this.<see cref="SyncEvent_ValueChangeProcessed.Explanation"/> to know which of the two occasions this represents):</para>
    /// <para>1) For an outbound change being sent to remote recipients (in which case, this event is published just AFTER the change has been sent to remote sources; however, the remote recipients likely have NOT received/processed it yet.)</para>
    /// <para>2) For an inbound change received from a remote source (in which case, this event is published just AFTER the change has been applied)</para>
    /// </summary>
    [MessagePack.MessagePackObject]
    public sealed class SyncEvent_AnimatorCharacterController_parameters_Jump : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public System.Boolean valuePrevious;
		[MessagePack.Key(7)] public System.Boolean valueNew;

        static readonly Utils.ObjectPool<SyncEvent_AnimatorCharacterController_parameters_Jump> pool = new Utils.ObjectPool<SyncEvent_AnimatorCharacterController_parameters_Jump>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 1);
        static readonly System.Collections.Concurrent.ConcurrentQueue<SyncEvent_AnimatorCharacterController_parameters_Jump> returnQueue_onceOnBorrowThread = new System.Collections.Concurrent.ConcurrentQueue<SyncEvent_AnimatorCharacterController_parameters_Jump>();
        static System.Threading.Thread borrowThread;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// Instead, call <see cref="Borrow(SyncEvent_ValueChangeProcessedExplanation, long, uint, uint, byte, System.Boolean, System.Boolean)"/>.
        /// </summary>
        public SyncEvent_AnimatorCharacterController_parameters_Jump() { }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to ensure the instance returned from this method is also returned back
        ///            here (i.e., to private object pool) via <see cref="Return(SyncEvent_AnimatorCharacterController_parameters_Jump)"/> when no longer needed!
        /// </summary>
        public static SyncEvent_AnimatorCharacterController_parameters_Jump Borrow(SyncEvent_ValueChangeProcessedExplanation explanation, long occurredAtElapsedTicks, ushort relatedOwnerAuthorityId, uint gonetId, byte codeGenerationId, byte syncMemberIndex, System.Boolean valuePrevious, System.Boolean valueNew)
		{
            if (borrowThread == null)
            {
                borrowThread = System.Threading.Thread.CurrentThread;
            }
            else if (borrowThread != System.Threading.Thread.CurrentThread)
            {
                const string REQUIRED_CALL_SAME_BORROW_THREAD = "Not allowed to call this from more than one thread.  So, ensure Borrow() is called from the same exact thread for this specific event type.  NOTE: Each event type can have its' Borrow() called from a different thread from one another.";
                throw new InvalidOperationException(REQUIRED_CALL_SAME_BORROW_THREAD);
            }

            int autoReturnCount = returnQueue_onceOnBorrowThread.Count;
            SyncEvent_AnimatorCharacterController_parameters_Jump autoReturn;
            while (returnQueue_onceOnBorrowThread.TryDequeue(out autoReturn) && autoReturnCount > 0)
            {
                Return(autoReturn);
                ++autoReturnCount;
            }

            var @event = pool.Borrow();
            
            @event.Explanation = explanation;
            @event.OccurredAtElapsedTicks = occurredAtElapsedTicks;
            @event.RelatedOwnerAuthorityId = relatedOwnerAuthorityId;
            @event.GONetId = gonetId;
			@event.CodeGenerationId = codeGenerationId;
            @event.SyncMemberIndex = syncMemberIndex;
			@event.valuePrevious = valuePrevious;
            @event.valueNew = valueNew;

            return @event;
		}

        public override void Return()
		{
			Return(this);
		}

        public static void Return(SyncEvent_AnimatorCharacterController_parameters_Jump borrowed)
        {
            if (borrowThread == System.Threading.Thread.CurrentThread)
            {
                pool.Return(borrowed);
            }
            else
            {
                returnQueue_onceOnBorrowThread.Enqueue(borrowed);
            }
        }
    }

    
	
    /// <summary>
    /// <para>This represents that a sync value change has been processed.  Use the class name to determine the related value/type/context.</para>
	/// <para>Two major occassions this event will occur (use this.<see cref="SyncEvent_ValueChangeProcessed.Explanation"/> to know which of the two occasions this represents):</para>
    /// <para>1) For an outbound change being sent to remote recipients (in which case, this event is published just AFTER the change has been sent to remote sources; however, the remote recipients likely have NOT received/processed it yet.)</para>
    /// <para>2) For an inbound change received from a remote source (in which case, this event is published just AFTER the change has been applied)</para>
    /// </summary>
    [MessagePack.MessagePackObject]
    public sealed class SyncEvent_AnimatorCharacterController_parameters_Fly : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public System.Boolean valuePrevious;
		[MessagePack.Key(7)] public System.Boolean valueNew;

        static readonly Utils.ObjectPool<SyncEvent_AnimatorCharacterController_parameters_Fly> pool = new Utils.ObjectPool<SyncEvent_AnimatorCharacterController_parameters_Fly>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 1);
        static readonly System.Collections.Concurrent.ConcurrentQueue<SyncEvent_AnimatorCharacterController_parameters_Fly> returnQueue_onceOnBorrowThread = new System.Collections.Concurrent.ConcurrentQueue<SyncEvent_AnimatorCharacterController_parameters_Fly>();
        static System.Threading.Thread borrowThread;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// Instead, call <see cref="Borrow(SyncEvent_ValueChangeProcessedExplanation, long, uint, uint, byte, System.Boolean, System.Boolean)"/>.
        /// </summary>
        public SyncEvent_AnimatorCharacterController_parameters_Fly() { }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to ensure the instance returned from this method is also returned back
        ///            here (i.e., to private object pool) via <see cref="Return(SyncEvent_AnimatorCharacterController_parameters_Fly)"/> when no longer needed!
        /// </summary>
        public static SyncEvent_AnimatorCharacterController_parameters_Fly Borrow(SyncEvent_ValueChangeProcessedExplanation explanation, long occurredAtElapsedTicks, ushort relatedOwnerAuthorityId, uint gonetId, byte codeGenerationId, byte syncMemberIndex, System.Boolean valuePrevious, System.Boolean valueNew)
		{
            if (borrowThread == null)
            {
                borrowThread = System.Threading.Thread.CurrentThread;
            }
            else if (borrowThread != System.Threading.Thread.CurrentThread)
            {
                const string REQUIRED_CALL_SAME_BORROW_THREAD = "Not allowed to call this from more than one thread.  So, ensure Borrow() is called from the same exact thread for this specific event type.  NOTE: Each event type can have its' Borrow() called from a different thread from one another.";
                throw new InvalidOperationException(REQUIRED_CALL_SAME_BORROW_THREAD);
            }

            int autoReturnCount = returnQueue_onceOnBorrowThread.Count;
            SyncEvent_AnimatorCharacterController_parameters_Fly autoReturn;
            while (returnQueue_onceOnBorrowThread.TryDequeue(out autoReturn) && autoReturnCount > 0)
            {
                Return(autoReturn);
                ++autoReturnCount;
            }

            var @event = pool.Borrow();
            
            @event.Explanation = explanation;
            @event.OccurredAtElapsedTicks = occurredAtElapsedTicks;
            @event.RelatedOwnerAuthorityId = relatedOwnerAuthorityId;
            @event.GONetId = gonetId;
			@event.CodeGenerationId = codeGenerationId;
            @event.SyncMemberIndex = syncMemberIndex;
			@event.valuePrevious = valuePrevious;
            @event.valueNew = valueNew;

            return @event;
		}

        public override void Return()
		{
			Return(this);
		}

        public static void Return(SyncEvent_AnimatorCharacterController_parameters_Fly borrowed)
        {
            if (borrowThread == System.Threading.Thread.CurrentThread)
            {
                pool.Return(borrowed);
            }
            else
            {
                returnQueue_onceOnBorrowThread.Enqueue(borrowed);
            }
        }
    }

    
	
    /// <summary>
    /// <para>This represents that a sync value change has been processed.  Use the class name to determine the related value/type/context.</para>
	/// <para>Two major occassions this event will occur (use this.<see cref="SyncEvent_ValueChangeProcessed.Explanation"/> to know which of the two occasions this represents):</para>
    /// <para>1) For an outbound change being sent to remote recipients (in which case, this event is published just AFTER the change has been sent to remote sources; however, the remote recipients likely have NOT received/processed it yet.)</para>
    /// <para>2) For an inbound change received from a remote source (in which case, this event is published just AFTER the change has been applied)</para>
    /// </summary>
    [MessagePack.MessagePackObject]
    public sealed class SyncEvent_AnimatorCharacterController_parameters_Aim : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public System.Boolean valuePrevious;
		[MessagePack.Key(7)] public System.Boolean valueNew;

        static readonly Utils.ObjectPool<SyncEvent_AnimatorCharacterController_parameters_Aim> pool = new Utils.ObjectPool<SyncEvent_AnimatorCharacterController_parameters_Aim>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 1);
        static readonly System.Collections.Concurrent.ConcurrentQueue<SyncEvent_AnimatorCharacterController_parameters_Aim> returnQueue_onceOnBorrowThread = new System.Collections.Concurrent.ConcurrentQueue<SyncEvent_AnimatorCharacterController_parameters_Aim>();
        static System.Threading.Thread borrowThread;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// Instead, call <see cref="Borrow(SyncEvent_ValueChangeProcessedExplanation, long, uint, uint, byte, System.Boolean, System.Boolean)"/>.
        /// </summary>
        public SyncEvent_AnimatorCharacterController_parameters_Aim() { }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to ensure the instance returned from this method is also returned back
        ///            here (i.e., to private object pool) via <see cref="Return(SyncEvent_AnimatorCharacterController_parameters_Aim)"/> when no longer needed!
        /// </summary>
        public static SyncEvent_AnimatorCharacterController_parameters_Aim Borrow(SyncEvent_ValueChangeProcessedExplanation explanation, long occurredAtElapsedTicks, ushort relatedOwnerAuthorityId, uint gonetId, byte codeGenerationId, byte syncMemberIndex, System.Boolean valuePrevious, System.Boolean valueNew)
		{
            if (borrowThread == null)
            {
                borrowThread = System.Threading.Thread.CurrentThread;
            }
            else if (borrowThread != System.Threading.Thread.CurrentThread)
            {
                const string REQUIRED_CALL_SAME_BORROW_THREAD = "Not allowed to call this from more than one thread.  So, ensure Borrow() is called from the same exact thread for this specific event type.  NOTE: Each event type can have its' Borrow() called from a different thread from one another.";
                throw new InvalidOperationException(REQUIRED_CALL_SAME_BORROW_THREAD);
            }

            int autoReturnCount = returnQueue_onceOnBorrowThread.Count;
            SyncEvent_AnimatorCharacterController_parameters_Aim autoReturn;
            while (returnQueue_onceOnBorrowThread.TryDequeue(out autoReturn) && autoReturnCount > 0)
            {
                Return(autoReturn);
                ++autoReturnCount;
            }

            var @event = pool.Borrow();
            
            @event.Explanation = explanation;
            @event.OccurredAtElapsedTicks = occurredAtElapsedTicks;
            @event.RelatedOwnerAuthorityId = relatedOwnerAuthorityId;
            @event.GONetId = gonetId;
			@event.CodeGenerationId = codeGenerationId;
            @event.SyncMemberIndex = syncMemberIndex;
			@event.valuePrevious = valuePrevious;
            @event.valueNew = valueNew;

            return @event;
		}

        public override void Return()
		{
			Return(this);
		}

        public static void Return(SyncEvent_AnimatorCharacterController_parameters_Aim borrowed)
        {
            if (borrowThread == System.Threading.Thread.CurrentThread)
            {
                pool.Return(borrowed);
            }
            else
            {
                returnQueue_onceOnBorrowThread.Enqueue(borrowed);
            }
        }
    }

    
	
    /// <summary>
    /// <para>This represents that a sync value change has been processed.  Use the class name to determine the related value/type/context.</para>
	/// <para>Two major occassions this event will occur (use this.<see cref="SyncEvent_ValueChangeProcessed.Explanation"/> to know which of the two occasions this represents):</para>
    /// <para>1) For an outbound change being sent to remote recipients (in which case, this event is published just AFTER the change has been sent to remote sources; however, the remote recipients likely have NOT received/processed it yet.)</para>
    /// <para>2) For an inbound change received from a remote source (in which case, this event is published just AFTER the change has been applied)</para>
    /// </summary>
    [MessagePack.MessagePackObject]
    public sealed class SyncEvent_AnimatorCharacterController_parameters_H : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public System.Single valuePrevious;
		[MessagePack.Key(7)] public System.Single valueNew;

        static readonly Utils.ObjectPool<SyncEvent_AnimatorCharacterController_parameters_H> pool = new Utils.ObjectPool<SyncEvent_AnimatorCharacterController_parameters_H>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 1);
        static readonly System.Collections.Concurrent.ConcurrentQueue<SyncEvent_AnimatorCharacterController_parameters_H> returnQueue_onceOnBorrowThread = new System.Collections.Concurrent.ConcurrentQueue<SyncEvent_AnimatorCharacterController_parameters_H>();
        static System.Threading.Thread borrowThread;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// Instead, call <see cref="Borrow(SyncEvent_ValueChangeProcessedExplanation, long, uint, uint, byte, System.Single, System.Single)"/>.
        /// </summary>
        public SyncEvent_AnimatorCharacterController_parameters_H() { }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to ensure the instance returned from this method is also returned back
        ///            here (i.e., to private object pool) via <see cref="Return(SyncEvent_AnimatorCharacterController_parameters_H)"/> when no longer needed!
        /// </summary>
        public static SyncEvent_AnimatorCharacterController_parameters_H Borrow(SyncEvent_ValueChangeProcessedExplanation explanation, long occurredAtElapsedTicks, ushort relatedOwnerAuthorityId, uint gonetId, byte codeGenerationId, byte syncMemberIndex, System.Single valuePrevious, System.Single valueNew)
		{
            if (borrowThread == null)
            {
                borrowThread = System.Threading.Thread.CurrentThread;
            }
            else if (borrowThread != System.Threading.Thread.CurrentThread)
            {
                const string REQUIRED_CALL_SAME_BORROW_THREAD = "Not allowed to call this from more than one thread.  So, ensure Borrow() is called from the same exact thread for this specific event type.  NOTE: Each event type can have its' Borrow() called from a different thread from one another.";
                throw new InvalidOperationException(REQUIRED_CALL_SAME_BORROW_THREAD);
            }

            int autoReturnCount = returnQueue_onceOnBorrowThread.Count;
            SyncEvent_AnimatorCharacterController_parameters_H autoReturn;
            while (returnQueue_onceOnBorrowThread.TryDequeue(out autoReturn) && autoReturnCount > 0)
            {
                Return(autoReturn);
                ++autoReturnCount;
            }

            var @event = pool.Borrow();
            
            @event.Explanation = explanation;
            @event.OccurredAtElapsedTicks = occurredAtElapsedTicks;
            @event.RelatedOwnerAuthorityId = relatedOwnerAuthorityId;
            @event.GONetId = gonetId;
			@event.CodeGenerationId = codeGenerationId;
            @event.SyncMemberIndex = syncMemberIndex;
			@event.valuePrevious = valuePrevious;
            @event.valueNew = valueNew;

            return @event;
		}

        public override void Return()
		{
			Return(this);
		}

        public static void Return(SyncEvent_AnimatorCharacterController_parameters_H borrowed)
        {
            if (borrowThread == System.Threading.Thread.CurrentThread)
            {
                pool.Return(borrowed);
            }
            else
            {
                returnQueue_onceOnBorrowThread.Enqueue(borrowed);
            }
        }
    }

    
	
    /// <summary>
    /// <para>This represents that a sync value change has been processed.  Use the class name to determine the related value/type/context.</para>
	/// <para>Two major occassions this event will occur (use this.<see cref="SyncEvent_ValueChangeProcessed.Explanation"/> to know which of the two occasions this represents):</para>
    /// <para>1) For an outbound change being sent to remote recipients (in which case, this event is published just AFTER the change has been sent to remote sources; however, the remote recipients likely have NOT received/processed it yet.)</para>
    /// <para>2) For an inbound change received from a remote source (in which case, this event is published just AFTER the change has been applied)</para>
    /// </summary>
    [MessagePack.MessagePackObject]
    public sealed class SyncEvent_AnimatorCharacterController_parameters_V : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public System.Single valuePrevious;
		[MessagePack.Key(7)] public System.Single valueNew;

        static readonly Utils.ObjectPool<SyncEvent_AnimatorCharacterController_parameters_V> pool = new Utils.ObjectPool<SyncEvent_AnimatorCharacterController_parameters_V>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 1);
        static readonly System.Collections.Concurrent.ConcurrentQueue<SyncEvent_AnimatorCharacterController_parameters_V> returnQueue_onceOnBorrowThread = new System.Collections.Concurrent.ConcurrentQueue<SyncEvent_AnimatorCharacterController_parameters_V>();
        static System.Threading.Thread borrowThread;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// Instead, call <see cref="Borrow(SyncEvent_ValueChangeProcessedExplanation, long, uint, uint, byte, System.Single, System.Single)"/>.
        /// </summary>
        public SyncEvent_AnimatorCharacterController_parameters_V() { }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to ensure the instance returned from this method is also returned back
        ///            here (i.e., to private object pool) via <see cref="Return(SyncEvent_AnimatorCharacterController_parameters_V)"/> when no longer needed!
        /// </summary>
        public static SyncEvent_AnimatorCharacterController_parameters_V Borrow(SyncEvent_ValueChangeProcessedExplanation explanation, long occurredAtElapsedTicks, ushort relatedOwnerAuthorityId, uint gonetId, byte codeGenerationId, byte syncMemberIndex, System.Single valuePrevious, System.Single valueNew)
		{
            if (borrowThread == null)
            {
                borrowThread = System.Threading.Thread.CurrentThread;
            }
            else if (borrowThread != System.Threading.Thread.CurrentThread)
            {
                const string REQUIRED_CALL_SAME_BORROW_THREAD = "Not allowed to call this from more than one thread.  So, ensure Borrow() is called from the same exact thread for this specific event type.  NOTE: Each event type can have its' Borrow() called from a different thread from one another.";
                throw new InvalidOperationException(REQUIRED_CALL_SAME_BORROW_THREAD);
            }

            int autoReturnCount = returnQueue_onceOnBorrowThread.Count;
            SyncEvent_AnimatorCharacterController_parameters_V autoReturn;
            while (returnQueue_onceOnBorrowThread.TryDequeue(out autoReturn) && autoReturnCount > 0)
            {
                Return(autoReturn);
                ++autoReturnCount;
            }

            var @event = pool.Borrow();
            
            @event.Explanation = explanation;
            @event.OccurredAtElapsedTicks = occurredAtElapsedTicks;
            @event.RelatedOwnerAuthorityId = relatedOwnerAuthorityId;
            @event.GONetId = gonetId;
			@event.CodeGenerationId = codeGenerationId;
            @event.SyncMemberIndex = syncMemberIndex;
			@event.valuePrevious = valuePrevious;
            @event.valueNew = valueNew;

            return @event;
		}

        public override void Return()
		{
			Return(this);
		}

        public static void Return(SyncEvent_AnimatorCharacterController_parameters_V borrowed)
        {
            if (borrowThread == System.Threading.Thread.CurrentThread)
            {
                pool.Return(borrowed);
            }
            else
            {
                returnQueue_onceOnBorrowThread.Enqueue(borrowed);
            }
        }
    }

    
	
    /// <summary>
    /// <para>This represents that a sync value change has been processed.  Use the class name to determine the related value/type/context.</para>
	/// <para>Two major occassions this event will occur (use this.<see cref="SyncEvent_ValueChangeProcessed.Explanation"/> to know which of the two occasions this represents):</para>
    /// <para>1) For an outbound change being sent to remote recipients (in which case, this event is published just AFTER the change has been sent to remote sources; however, the remote recipients likely have NOT received/processed it yet.)</para>
    /// <para>2) For an inbound change received from a remote source (in which case, this event is published just AFTER the change has been applied)</para>
    /// </summary>
    [MessagePack.MessagePackObject]
    public sealed class SyncEvent_AnimatorCharacterController_parameters_Grounded : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public System.Boolean valuePrevious;
		[MessagePack.Key(7)] public System.Boolean valueNew;

        static readonly Utils.ObjectPool<SyncEvent_AnimatorCharacterController_parameters_Grounded> pool = new Utils.ObjectPool<SyncEvent_AnimatorCharacterController_parameters_Grounded>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 1);
        static readonly System.Collections.Concurrent.ConcurrentQueue<SyncEvent_AnimatorCharacterController_parameters_Grounded> returnQueue_onceOnBorrowThread = new System.Collections.Concurrent.ConcurrentQueue<SyncEvent_AnimatorCharacterController_parameters_Grounded>();
        static System.Threading.Thread borrowThread;

        /// <summary>
        /// Do NOT use!  This is for object pooling and MessagePack only.
        /// Instead, call <see cref="Borrow(SyncEvent_ValueChangeProcessedExplanation, long, uint, uint, byte, System.Boolean, System.Boolean)"/>.
        /// </summary>
        public SyncEvent_AnimatorCharacterController_parameters_Grounded() { }

        /// <summary>
        /// IMPORTANT: It is the caller's responsibility to ensure the instance returned from this method is also returned back
        ///            here (i.e., to private object pool) via <see cref="Return(SyncEvent_AnimatorCharacterController_parameters_Grounded)"/> when no longer needed!
        /// </summary>
        public static SyncEvent_AnimatorCharacterController_parameters_Grounded Borrow(SyncEvent_ValueChangeProcessedExplanation explanation, long occurredAtElapsedTicks, ushort relatedOwnerAuthorityId, uint gonetId, byte codeGenerationId, byte syncMemberIndex, System.Boolean valuePrevious, System.Boolean valueNew)
		{
            if (borrowThread == null)
            {
                borrowThread = System.Threading.Thread.CurrentThread;
            }
            else if (borrowThread != System.Threading.Thread.CurrentThread)
            {
                const string REQUIRED_CALL_SAME_BORROW_THREAD = "Not allowed to call this from more than one thread.  So, ensure Borrow() is called from the same exact thread for this specific event type.  NOTE: Each event type can have its' Borrow() called from a different thread from one another.";
                throw new InvalidOperationException(REQUIRED_CALL_SAME_BORROW_THREAD);
            }

            int autoReturnCount = returnQueue_onceOnBorrowThread.Count;
            SyncEvent_AnimatorCharacterController_parameters_Grounded autoReturn;
            while (returnQueue_onceOnBorrowThread.TryDequeue(out autoReturn) && autoReturnCount > 0)
            {
                Return(autoReturn);
                ++autoReturnCount;
            }

            var @event = pool.Borrow();
            
            @event.Explanation = explanation;
            @event.OccurredAtElapsedTicks = occurredAtElapsedTicks;
            @event.RelatedOwnerAuthorityId = relatedOwnerAuthorityId;
            @event.GONetId = gonetId;
			@event.CodeGenerationId = codeGenerationId;
            @event.SyncMemberIndex = syncMemberIndex;
			@event.valuePrevious = valuePrevious;
            @event.valueNew = valueNew;

            return @event;
		}

        public override void Return()
		{
			Return(this);
		}

        public static void Return(SyncEvent_AnimatorCharacterController_parameters_Grounded borrowed)
        {
            if (borrowThread == System.Threading.Thread.CurrentThread)
            {
                pool.Return(borrowed);
            }
            else
            {
                returnQueue_onceOnBorrowThread.Enqueue(borrowed);
            }
        }
    }

}

namespace GONet.Generation
{
	public partial class BobWad
	{
		static BobWad()
		{
			GONetParticipant_AutoMagicalSyncCompanion_Generated_Factory.theRealness = hahaThisIsTrulyTheRealness;
			GONetParticipant_AutoMagicalSyncCompanion_Generated_Factory.theRealness_quantizerSettings = hahaThisIsTrulyTheRealness_quantizerSettings;

			GONet_SyncEvent_ValueChangeProcessed_Generated_Factory.theRealness = hahaThisIsTrulyTheRealness_Events;
			GONet_SyncEvent_ValueChangeProcessed_Generated_Factory.theRealness_copy = hahaThisIsTrulyTheRealness_Events_Copy;

			GONet_SyncEvent_ValueChangeProcessed_Generated_Factory.allUniqueSyncEventTypes = new List<Type>()
			{
				typeof(GONet.AutoMagicalSync_AllCurrentValues_Message),
				typeof(GONet.AutoMagicalSync_ValueChanges_Message),
				typeof(GONet.AutoMagicalSync_ValuesNowAtRest_Message),
				typeof(GONet.ClientStateChangedEvent),
				typeof(GONet.ClientTypeFlagsChangedEvent),
				typeof(GONet.DestroyGONetParticipantEvent),
				typeof(GONet.GONetParticipantDisabledEvent),
				typeof(GONet.GONetParticipantEnabledEvent),
				typeof(GONet.GONetParticipantStartedEvent),
				typeof(GONet.InstantiateGONetParticipantEvent),
				typeof(GONet.OwnerAuthorityIdAssignmentEvent),
				typeof(GONet.PersistentEvents_Bundle),
				typeof(GONet.RemoteClientStateChangedEvent),
				typeof(GONet.RequestMessage),
				typeof(GONet.ResponseMessage),
				typeof(GONet.ServerSaysClientInitializationCompletion),
				typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Aim),
				typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Fly),
				typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Grounded),
				typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_H),
				typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Jump),
				typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Speed),
				typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_V),
				typeof(GONet.SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate),
				typeof(GONet.SyncEvent_FieldChangeTest_color),
				typeof(GONet.SyncEvent_FieldChangeTest_color_dosientos),
				typeof(GONet.SyncEvent_FieldChangeTest_nada),
				typeof(GONet.SyncEvent_FieldChangeTest_shortie),
				typeof(GONet.SyncEvent_GONetParticipant_GONetId),
				typeof(GONet.SyncEvent_GONetParticipant_IsPositionSyncd),
				typeof(GONet.SyncEvent_GONetParticipant_IsRotationSyncd),
				typeof(GONet.SyncEvent_GONetParticipant_OwnerAuthorityId),
				typeof(GONet.SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId),
				typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_A),
				typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_D),
				typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_DownArrow),
				typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_LeftArrow),
				typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_RightArrow),
				typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_S),
				typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_UpArrow),
				typeof(GONet.SyncEvent_GONetSampleInputSync_GetKey_W),
				typeof(GONet.SyncEvent_Time_ElapsedTicks_SetFromAuthority),
				typeof(GONet.SyncEvent_Transform_position),
				typeof(GONet.SyncEvent_Transform_rotation),
				typeof(GONet.ValueMonitoringSupport_BaselineExpiredEvent),
				typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Boolean),
				typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Byte),
				typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Double),
				typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Int16),
				typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Int32),
				typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Int64),
				typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_SByte),
				typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_Single),
				typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_UInt16),
				typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_UInt32),
				typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_System_UInt64),
				typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Quaternion),
				typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector2),
				typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector3),
				typeof(GONet.ValueMonitoringSupport_NewBaselineEvent_UnityEngine_Vector4),
			};
		}

		static internal HashSet<GONet.Utils.QuantizerSettingsGroup> hahaThisIsTrulyTheRealness_quantizerSettings()
		{
			HashSet<GONet.Utils.QuantizerSettingsGroup> settings = new HashSet<GONet.Utils.QuantizerSettingsGroup>();

			var item_codeGenerationId1_single0_singleMember0 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId1_single0_singleMember0);

			var item_codeGenerationId1_single0_singleMember1 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId1_single0_singleMember1);

			var item_codeGenerationId1_single0_singleMember2 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId1_single0_singleMember2);

			var item_codeGenerationId1_single0_singleMember3 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId1_single0_singleMember3);

			var item_codeGenerationId1_single1_singleMember0 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId1_single1_singleMember0);

			var item_codeGenerationId1_single1_singleMember1 = new GONet.Utils.QuantizerSettingsGroup(-125f, 125f, 18, true);
			settings.Add(item_codeGenerationId1_single1_singleMember1);

			var item_codeGenerationId2_single0_singleMember0 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId2_single0_singleMember0);

			var item_codeGenerationId2_single0_singleMember1 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId2_single0_singleMember1);

			var item_codeGenerationId2_single0_singleMember2 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId2_single0_singleMember2);

			var item_codeGenerationId2_single0_singleMember3 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId2_single0_singleMember3);

			var item_codeGenerationId2_single1_singleMember0 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId2_single1_singleMember0);

			var item_codeGenerationId2_single1_singleMember1 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId2_single1_singleMember1);

			var item_codeGenerationId2_single1_singleMember2 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId2_single1_singleMember2);

			var item_codeGenerationId2_single1_singleMember3 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId2_single1_singleMember3);

			var item_codeGenerationId2_single1_singleMember4 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId2_single1_singleMember4);

			var item_codeGenerationId2_single1_singleMember5 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId2_single1_singleMember5);

			var item_codeGenerationId2_single1_singleMember6 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId2_single1_singleMember6);

			var item_codeGenerationId2_single1_singleMember7 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId2_single1_singleMember7);

			var item_codeGenerationId2_single2_singleMember0 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId2_single2_singleMember0);

			var item_codeGenerationId2_single2_singleMember1 = new GONet.Utils.QuantizerSettingsGroup(-125f, 125f, 18, true);
			settings.Add(item_codeGenerationId2_single2_singleMember1);

			var item_codeGenerationId3_single0_singleMember0 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId3_single0_singleMember0);

			var item_codeGenerationId3_single0_singleMember1 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId3_single0_singleMember1);

			var item_codeGenerationId3_single0_singleMember2 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId3_single0_singleMember2);

			var item_codeGenerationId3_single0_singleMember3 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId3_single0_singleMember3);

			var item_codeGenerationId3_single0_singleMember4 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId3_single0_singleMember4);

			var item_codeGenerationId3_single1_singleMember0 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId3_single1_singleMember0);

			var item_codeGenerationId3_single1_singleMember1 = new GONet.Utils.QuantizerSettingsGroup(-125f, 125f, 18, true);
			settings.Add(item_codeGenerationId3_single1_singleMember1);

			var item_codeGenerationId4_single0_singleMember0 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId4_single0_singleMember0);

			var item_codeGenerationId4_single0_singleMember1 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId4_single0_singleMember1);

			var item_codeGenerationId4_single0_singleMember2 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId4_single0_singleMember2);

			var item_codeGenerationId4_single0_singleMember3 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId4_single0_singleMember3);

			var item_codeGenerationId4_single0_singleMember4 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId4_single0_singleMember4);

			var item_codeGenerationId4_single1_singleMember0 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId4_single1_singleMember0);

			var item_codeGenerationId4_single1_singleMember1 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId4_single1_singleMember1);

			var item_codeGenerationId4_single1_singleMember2 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId4_single1_singleMember2);

			var item_codeGenerationId4_single1_singleMember3 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId4_single1_singleMember3);

			var item_codeGenerationId4_single1_singleMember4 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId4_single1_singleMember4);

			var item_codeGenerationId4_single1_singleMember5 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId4_single1_singleMember5);

			var item_codeGenerationId4_single1_singleMember6 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId4_single1_singleMember6);

			var item_codeGenerationId4_single1_singleMember7 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId4_single1_singleMember7);

			var item_codeGenerationId4_single2_singleMember0 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId4_single2_singleMember0);

			var item_codeGenerationId4_single2_singleMember1 = new GONet.Utils.QuantizerSettingsGroup(-125f, 125f, 18, true);
			settings.Add(item_codeGenerationId4_single2_singleMember1);

			var item_codeGenerationId5_single0_singleMember0 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId5_single0_singleMember0);

			var item_codeGenerationId5_single0_singleMember1 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId5_single0_singleMember1);

			var item_codeGenerationId5_single0_singleMember2 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId5_single0_singleMember2);

			var item_codeGenerationId5_single0_singleMember3 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId5_single0_singleMember3);

			var item_codeGenerationId5_single0_singleMember4 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId5_single0_singleMember4);

			var item_codeGenerationId5_single1_singleMember0 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId5_single1_singleMember0);

			var item_codeGenerationId5_single2_singleMember0 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId5_single2_singleMember0);

			var item_codeGenerationId5_single2_singleMember1 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId5_single2_singleMember1);

			var item_codeGenerationId5_single2_singleMember2 = new GONet.Utils.QuantizerSettingsGroup(-10f, 10f, 16, true);
			settings.Add(item_codeGenerationId5_single2_singleMember2);

			var item_codeGenerationId5_single2_singleMember3 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId5_single2_singleMember3);

			var item_codeGenerationId5_single3_singleMember0 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId5_single3_singleMember0);

			var item_codeGenerationId5_single3_singleMember1 = new GONet.Utils.QuantizerSettingsGroup(-125f, 125f, 18, true);
			settings.Add(item_codeGenerationId5_single3_singleMember1);

			var item_codeGenerationId6_single0_singleMember0 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId6_single0_singleMember0);

			var item_codeGenerationId6_single0_singleMember1 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId6_single0_singleMember1);

			var item_codeGenerationId6_single0_singleMember2 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId6_single0_singleMember2);

			var item_codeGenerationId6_single0_singleMember3 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId6_single0_singleMember3);

			var item_codeGenerationId6_single0_singleMember4 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId6_single0_singleMember4);

			var item_codeGenerationId6_single1_singleMember0 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId6_single1_singleMember0);

			var item_codeGenerationId6_single2_singleMember0 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId6_single2_singleMember0);

			var item_codeGenerationId6_single2_singleMember1 = new GONet.Utils.QuantizerSettingsGroup(-125f, 125f, 18, true);
			settings.Add(item_codeGenerationId6_single2_singleMember1);

			var item_codeGenerationId6_single3_singleMember0 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId6_single3_singleMember0);

			var item_codeGenerationId6_single3_singleMember1 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId6_single3_singleMember1);

			var item_codeGenerationId6_single3_singleMember2 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId6_single3_singleMember2);

			var item_codeGenerationId6_single3_singleMember3 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId6_single3_singleMember3);

			var item_codeGenerationId6_single3_singleMember4 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId6_single3_singleMember4);

			var item_codeGenerationId6_single3_singleMember5 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId6_single3_singleMember5);

			var item_codeGenerationId6_single3_singleMember6 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId6_single3_singleMember6);

			return settings;
		}

		static internal GONetParticipant_AutoMagicalSyncCompanion_Generated hahaThisIsTrulyTheRealness(GONetParticipant gonetParticipant)
		{
			switch (gonetParticipant.codeGenerationId)
			{
				case 1:
					return new GONetParticipant_AutoMagicalSyncCompanion_Generated_1(gonetParticipant);
				case 2:
					return new GONetParticipant_AutoMagicalSyncCompanion_Generated_2(gonetParticipant);
				case 3:
					return new GONetParticipant_AutoMagicalSyncCompanion_Generated_3(gonetParticipant);
				case 4:
					return new GONetParticipant_AutoMagicalSyncCompanion_Generated_4(gonetParticipant);
				case 5:
					return new GONetParticipant_AutoMagicalSyncCompanion_Generated_5(gonetParticipant);
				case 6:
					return new GONetParticipant_AutoMagicalSyncCompanion_Generated_6(gonetParticipant);
			}

			return null;
		}

		internal static SyncEvent_ValueChangeProcessed hahaThisIsTrulyTheRealness_Events(SyncEvent_ValueChangeProcessedExplanation explanation, long elapsedTicks, ushort filterUsingOwnerAuthorityId, GONetParticipant_AutoMagicalSyncCompanion_Generated syncCompanion, byte syncMemberIndex)
        {
            switch (syncCompanion.gonetParticipant.codeGenerationId)
            {
				case 1:
					{
						GONetParticipant_AutoMagicalSyncCompanion_Generated_1 companion = (GONetParticipant_AutoMagicalSyncCompanion_Generated_1)syncCompanion;
                        switch (syncMemberIndex)
                        {

                            case 0:
								{
									System.UInt32 valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_UInt32;
									}
									else
									{
																			valueNew = companion.GONetParticipant.GONetId;
																		}
									System.UInt32 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_UInt32 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_UInt32; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_GONetId.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 1, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 1:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.GONetParticipant.IsPositionSyncd;
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_IsPositionSyncd.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 1, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 2:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.GONetParticipant.IsRotationSyncd;
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_IsRotationSyncd.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 1, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 3:
								{
									System.UInt16 valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_UInt16;
									}
									else
									{
																			valueNew = companion.GONetParticipant.OwnerAuthorityId;
																		}
									System.UInt16 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_UInt16 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_UInt16; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_OwnerAuthorityId.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 1, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 4:
								{
									UnityEngine.Quaternion valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.UnityEngine_Quaternion;
									}
									else
									{
																			valueNew = companion.Transform.rotation;
																		}
									UnityEngine.Quaternion valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.UnityEngine_Quaternion : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.UnityEngine_Quaternion; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_Transform_rotation.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 1, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 5:
								{
									UnityEngine.Vector3 valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.UnityEngine_Vector3;
									}
									else
									{
																			valueNew = companion.Transform.position;
																		}
									UnityEngine.Vector3 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.UnityEngine_Vector3 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.UnityEngine_Vector3; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_Transform_position.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 1, syncMemberIndex, valuePrevious, valueNew);
								}
						
						}
					}
					break;

				case 2:
					{
						GONetParticipant_AutoMagicalSyncCompanion_Generated_2 companion = (GONetParticipant_AutoMagicalSyncCompanion_Generated_2)syncCompanion;
                        switch (syncMemberIndex)
                        {

                            case 0:
								{
									System.UInt32 valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_UInt32;
									}
									else
									{
																			valueNew = companion.GONetParticipant.GONetId;
																		}
									System.UInt32 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_UInt32 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_UInt32; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_GONetId.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 2, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 1:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.GONetParticipant.IsPositionSyncd;
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_IsPositionSyncd.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 2, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 2:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.GONetParticipant.IsRotationSyncd;
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_IsRotationSyncd.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 2, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 3:
								{
									System.UInt16 valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_UInt16;
									}
									else
									{
																			valueNew = companion.GONetParticipant.OwnerAuthorityId;
																		}
									System.UInt16 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_UInt16 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_UInt16; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_OwnerAuthorityId.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 2, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 4:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.GONetSampleInputSync.GetKey_A;
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetSampleInputSync_GetKey_A.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 2, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 5:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.GONetSampleInputSync.GetKey_D;
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetSampleInputSync_GetKey_D.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 2, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 6:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.GONetSampleInputSync.GetKey_DownArrow;
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetSampleInputSync_GetKey_DownArrow.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 2, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 7:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.GONetSampleInputSync.GetKey_LeftArrow;
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetSampleInputSync_GetKey_LeftArrow.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 2, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 8:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.GONetSampleInputSync.GetKey_RightArrow;
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetSampleInputSync_GetKey_RightArrow.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 2, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 9:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.GONetSampleInputSync.GetKey_S;
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetSampleInputSync_GetKey_S.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 2, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 10:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.GONetSampleInputSync.GetKey_UpArrow;
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetSampleInputSync_GetKey_UpArrow.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 2, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 11:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.GONetSampleInputSync.GetKey_W;
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetSampleInputSync_GetKey_W.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 2, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 12:
								{
									UnityEngine.Quaternion valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.UnityEngine_Quaternion;
									}
									else
									{
																			valueNew = companion.Transform.rotation;
																		}
									UnityEngine.Quaternion valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.UnityEngine_Quaternion : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.UnityEngine_Quaternion; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_Transform_rotation.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 2, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 13:
								{
									UnityEngine.Vector3 valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.UnityEngine_Vector3;
									}
									else
									{
																			valueNew = companion.Transform.position;
																		}
									UnityEngine.Vector3 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.UnityEngine_Vector3 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.UnityEngine_Vector3; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_Transform_position.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 2, syncMemberIndex, valuePrevious, valueNew);
								}
						
						}
					}
					break;

				case 3:
					{
						GONetParticipant_AutoMagicalSyncCompanion_Generated_3 companion = (GONetParticipant_AutoMagicalSyncCompanion_Generated_3)syncCompanion;
                        switch (syncMemberIndex)
                        {

                            case 0:
								{
									System.UInt32 valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_UInt32;
									}
									else
									{
																			valueNew = companion.GONetParticipant.GONetId;
																		}
									System.UInt32 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_UInt32 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_UInt32; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_GONetId.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 3, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 1:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.GONetParticipant.IsPositionSyncd;
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_IsPositionSyncd.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 3, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 2:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.GONetParticipant.IsRotationSyncd;
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_IsRotationSyncd.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 3, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 3:
								{
									System.UInt16 valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_UInt16;
									}
									else
									{
																			valueNew = companion.GONetParticipant.OwnerAuthorityId;
																		}
									System.UInt16 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_UInt16 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_UInt16; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_OwnerAuthorityId.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 3, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 4:
								{
									System.UInt16 valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_UInt16;
									}
									else
									{
																			valueNew = companion.GONetParticipant.RemotelyControlledByAuthorityId;
																		}
									System.UInt16 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_UInt16 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_UInt16; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 3, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 5:
								{
									UnityEngine.Quaternion valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.UnityEngine_Quaternion;
									}
									else
									{
																			valueNew = companion.Transform.rotation;
																		}
									UnityEngine.Quaternion valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.UnityEngine_Quaternion : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.UnityEngine_Quaternion; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_Transform_rotation.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 3, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 6:
								{
									UnityEngine.Vector3 valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.UnityEngine_Vector3;
									}
									else
									{
																			valueNew = companion.Transform.position;
																		}
									UnityEngine.Vector3 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.UnityEngine_Vector3 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.UnityEngine_Vector3; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_Transform_position.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 3, syncMemberIndex, valuePrevious, valueNew);
								}
						
						}
					}
					break;

				case 4:
					{
						GONetParticipant_AutoMagicalSyncCompanion_Generated_4 companion = (GONetParticipant_AutoMagicalSyncCompanion_Generated_4)syncCompanion;
                        switch (syncMemberIndex)
                        {

                            case 0:
								{
									System.UInt32 valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_UInt32;
									}
									else
									{
																			valueNew = companion.GONetParticipant.GONetId;
																		}
									System.UInt32 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_UInt32 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_UInt32; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_GONetId.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 4, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 1:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.GONetParticipant.IsPositionSyncd;
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_IsPositionSyncd.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 4, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 2:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.GONetParticipant.IsRotationSyncd;
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_IsRotationSyncd.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 4, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 3:
								{
									System.UInt16 valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_UInt16;
									}
									else
									{
																			valueNew = companion.GONetParticipant.OwnerAuthorityId;
																		}
									System.UInt16 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_UInt16 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_UInt16; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_OwnerAuthorityId.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 4, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 4:
								{
									System.UInt16 valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_UInt16;
									}
									else
									{
																			valueNew = companion.GONetParticipant.RemotelyControlledByAuthorityId;
																		}
									System.UInt16 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_UInt16 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_UInt16; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 4, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 5:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.GONetSampleInputSync.GetKey_A;
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetSampleInputSync_GetKey_A.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 4, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 6:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.GONetSampleInputSync.GetKey_D;
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetSampleInputSync_GetKey_D.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 4, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 7:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.GONetSampleInputSync.GetKey_DownArrow;
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetSampleInputSync_GetKey_DownArrow.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 4, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 8:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.GONetSampleInputSync.GetKey_LeftArrow;
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetSampleInputSync_GetKey_LeftArrow.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 4, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 9:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.GONetSampleInputSync.GetKey_RightArrow;
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetSampleInputSync_GetKey_RightArrow.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 4, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 10:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.GONetSampleInputSync.GetKey_S;
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetSampleInputSync_GetKey_S.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 4, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 11:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.GONetSampleInputSync.GetKey_UpArrow;
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetSampleInputSync_GetKey_UpArrow.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 4, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 12:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.GONetSampleInputSync.GetKey_W;
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetSampleInputSync_GetKey_W.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 4, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 13:
								{
									UnityEngine.Quaternion valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.UnityEngine_Quaternion;
									}
									else
									{
																			valueNew = companion.Transform.rotation;
																		}
									UnityEngine.Quaternion valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.UnityEngine_Quaternion : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.UnityEngine_Quaternion; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_Transform_rotation.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 4, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 14:
								{
									UnityEngine.Vector3 valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.UnityEngine_Vector3;
									}
									else
									{
																			valueNew = companion.Transform.position;
																		}
									UnityEngine.Vector3 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.UnityEngine_Vector3 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.UnityEngine_Vector3; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_Transform_position.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 4, syncMemberIndex, valuePrevious, valueNew);
								}
						
						}
					}
					break;

				case 5:
					{
						GONetParticipant_AutoMagicalSyncCompanion_Generated_5 companion = (GONetParticipant_AutoMagicalSyncCompanion_Generated_5)syncCompanion;
                        switch (syncMemberIndex)
                        {

                            case 0:
								{
									System.UInt32 valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_UInt32;
									}
									else
									{
																			valueNew = companion.GONetParticipant.GONetId;
																		}
									System.UInt32 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_UInt32 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_UInt32; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_GONetId.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 5, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 1:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.GONetParticipant.IsPositionSyncd;
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_IsPositionSyncd.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 5, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 2:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.GONetParticipant.IsRotationSyncd;
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_IsRotationSyncd.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 5, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 3:
								{
									System.UInt16 valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_UInt16;
									}
									else
									{
																			valueNew = companion.GONetParticipant.OwnerAuthorityId;
																		}
									System.UInt16 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_UInt16 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_UInt16; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_OwnerAuthorityId.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 5, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 4:
								{
									System.UInt16 valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_UInt16;
									}
									else
									{
																			valueNew = companion.GONetParticipant.RemotelyControlledByAuthorityId;
																		}
									System.UInt16 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_UInt16 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_UInt16; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 5, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 5:
								{
									System.Single valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Single;
									}
									else
									{
																			valueNew = companion.DestroyIfMineOnKeyPress.willHeUpdate;
																		}
									System.Single valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Single : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Single; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 5, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 6:
								{
									UnityEngine.Vector3 valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.UnityEngine_Vector3;
									}
									else
									{
																			valueNew = companion.FieldChangeTest.color;
																		}
									UnityEngine.Vector3 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.UnityEngine_Vector3 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.UnityEngine_Vector3; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_FieldChangeTest_color.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 5, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 7:
								{
									UnityEngine.Vector3 valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.UnityEngine_Vector3;
									}
									else
									{
																			valueNew = companion.FieldChangeTest.color_dosientos;
																		}
									UnityEngine.Vector3 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.UnityEngine_Vector3 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.UnityEngine_Vector3; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_FieldChangeTest_color_dosientos.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 5, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 8:
								{
									System.Single valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Single;
									}
									else
									{
																			valueNew = companion.FieldChangeTest.nada;
																		}
									System.Single valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Single : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Single; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_FieldChangeTest_nada.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 5, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 9:
								{
									System.Int16 valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Int16;
									}
									else
									{
																			valueNew = companion.FieldChangeTest.shortie;
																		}
									System.Int16 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Int16 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Int16; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_FieldChangeTest_shortie.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 5, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 10:
								{
									UnityEngine.Quaternion valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.UnityEngine_Quaternion;
									}
									else
									{
																			valueNew = companion.Transform.rotation;
																		}
									UnityEngine.Quaternion valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.UnityEngine_Quaternion : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.UnityEngine_Quaternion; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_Transform_rotation.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 5, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 11:
								{
									UnityEngine.Vector3 valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.UnityEngine_Vector3;
									}
									else
									{
																			valueNew = companion.Transform.position;
																		}
									UnityEngine.Vector3 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.UnityEngine_Vector3 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.UnityEngine_Vector3; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_Transform_position.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 5, syncMemberIndex, valuePrevious, valueNew);
								}
						
						}
					}
					break;

				case 6:
					{
						GONetParticipant_AutoMagicalSyncCompanion_Generated_6 companion = (GONetParticipant_AutoMagicalSyncCompanion_Generated_6)syncCompanion;
                        switch (syncMemberIndex)
                        {

                            case 0:
								{
									System.UInt32 valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_UInt32;
									}
									else
									{
																			valueNew = companion.GONetParticipant.GONetId;
																		}
									System.UInt32 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_UInt32 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_UInt32; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_GONetId.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 6, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 1:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.GONetParticipant.IsPositionSyncd;
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_IsPositionSyncd.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 6, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 2:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.GONetParticipant.IsRotationSyncd;
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_IsRotationSyncd.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 6, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 3:
								{
									System.UInt16 valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_UInt16;
									}
									else
									{
																			valueNew = companion.GONetParticipant.OwnerAuthorityId;
																		}
									System.UInt16 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_UInt16 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_UInt16; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_OwnerAuthorityId.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 6, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 4:
								{
									System.UInt16 valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_UInt16;
									}
									else
									{
																			valueNew = companion.GONetParticipant.RemotelyControlledByAuthorityId;
																		}
									System.UInt16 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_UInt16 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_UInt16; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 6, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 5:
								{
									System.Single valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Single;
									}
									else
									{
																			valueNew = companion.DestroyIfMineOnKeyPress.willHeUpdate;
																		}
									System.Single valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Single : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Single; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 6, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 6:
								{
									UnityEngine.Quaternion valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.UnityEngine_Quaternion;
									}
									else
									{
																			valueNew = companion.Transform.rotation;
																		}
									UnityEngine.Quaternion valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.UnityEngine_Quaternion : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.UnityEngine_Quaternion; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_Transform_rotation.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 6, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 7:
								{
									UnityEngine.Vector3 valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.UnityEngine_Vector3;
									}
									else
									{
																			valueNew = companion.Transform.position;
																		}
									UnityEngine.Vector3 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.UnityEngine_Vector3 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.UnityEngine_Vector3; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_Transform_position.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 6, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 8:
								{
									System.Single valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Single;
									}
									else
									{
																			valueNew = companion.Animator.GetFloat(-823668238);
																		}
									System.Single valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Single : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Single; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_AnimatorCharacterController_parameters_Speed.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 6, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 9:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.Animator.GetBool(125937960);
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_AnimatorCharacterController_parameters_Jump.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 6, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 10:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.Animator.GetBool(1808254291);
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_AnimatorCharacterController_parameters_Fly.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 6, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 11:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.Animator.GetBool(153482222);
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_AnimatorCharacterController_parameters_Aim.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 6, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 12:
								{
									System.Single valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Single;
									}
									else
									{
																			valueNew = companion.Animator.GetFloat(-1442503121);
																		}
									System.Single valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Single : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Single; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_AnimatorCharacterController_parameters_H.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 6, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 13:
								{
									System.Single valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Single;
									}
									else
									{
																			valueNew = companion.Animator.GetFloat(1342839628);
																		}
									System.Single valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Single : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Single; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_AnimatorCharacterController_parameters_V.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 6, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 14:
								{
									System.Boolean valueNew;
									GONetSyncableValue valueNew_mostRecentChangeAtTime;
									if (explanation == SyncEvent_ValueChangeProcessedExplanation.InboundFromOther && companion.valuesChangesSupport[syncMemberIndex].TryGetMostRecentChangeAtTime(elapsedTicks, out valueNew_mostRecentChangeAtTime))
									{
										valueNew = valueNew_mostRecentChangeAtTime.System_Boolean;
									}
									else
									{
																			valueNew = companion.Animator.GetBool(862969536);
																		}
									System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_AnimatorCharacterController_parameters_Grounded.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 6, syncMemberIndex, valuePrevious, valueNew);
								}
						
						}
					}
					break;

            }

            return default;
        }

		internal static SyncEvent_ValueChangeProcessed hahaThisIsTrulyTheRealness_Events_Copy(SyncEvent_ValueChangeProcessed original)
		{
            switch (original.CodeGenerationId)
            {
				case 1:
					{
                        switch (original.SyncMemberIndex)
                        {

                            case 0:
								{
									SyncEvent_GONetParticipant_GONetId originalTyped = (SyncEvent_GONetParticipant_GONetId)original;
									return SyncEvent_GONetParticipant_GONetId.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 1:
								{
									SyncEvent_GONetParticipant_IsPositionSyncd originalTyped = (SyncEvent_GONetParticipant_IsPositionSyncd)original;
									return SyncEvent_GONetParticipant_IsPositionSyncd.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 2:
								{
									SyncEvent_GONetParticipant_IsRotationSyncd originalTyped = (SyncEvent_GONetParticipant_IsRotationSyncd)original;
									return SyncEvent_GONetParticipant_IsRotationSyncd.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 3:
								{
									SyncEvent_GONetParticipant_OwnerAuthorityId originalTyped = (SyncEvent_GONetParticipant_OwnerAuthorityId)original;
									return SyncEvent_GONetParticipant_OwnerAuthorityId.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 4:
								{
									SyncEvent_Transform_rotation originalTyped = (SyncEvent_Transform_rotation)original;
									return SyncEvent_Transform_rotation.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 5:
								{
									SyncEvent_Transform_position originalTyped = (SyncEvent_Transform_position)original;
									return SyncEvent_Transform_position.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
						
						}
					}
					break;

				case 2:
					{
                        switch (original.SyncMemberIndex)
                        {

                            case 0:
								{
									SyncEvent_GONetParticipant_GONetId originalTyped = (SyncEvent_GONetParticipant_GONetId)original;
									return SyncEvent_GONetParticipant_GONetId.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 1:
								{
									SyncEvent_GONetParticipant_IsPositionSyncd originalTyped = (SyncEvent_GONetParticipant_IsPositionSyncd)original;
									return SyncEvent_GONetParticipant_IsPositionSyncd.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 2:
								{
									SyncEvent_GONetParticipant_IsRotationSyncd originalTyped = (SyncEvent_GONetParticipant_IsRotationSyncd)original;
									return SyncEvent_GONetParticipant_IsRotationSyncd.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 3:
								{
									SyncEvent_GONetParticipant_OwnerAuthorityId originalTyped = (SyncEvent_GONetParticipant_OwnerAuthorityId)original;
									return SyncEvent_GONetParticipant_OwnerAuthorityId.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 4:
								{
									SyncEvent_GONetSampleInputSync_GetKey_A originalTyped = (SyncEvent_GONetSampleInputSync_GetKey_A)original;
									return SyncEvent_GONetSampleInputSync_GetKey_A.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 5:
								{
									SyncEvent_GONetSampleInputSync_GetKey_D originalTyped = (SyncEvent_GONetSampleInputSync_GetKey_D)original;
									return SyncEvent_GONetSampleInputSync_GetKey_D.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 6:
								{
									SyncEvent_GONetSampleInputSync_GetKey_DownArrow originalTyped = (SyncEvent_GONetSampleInputSync_GetKey_DownArrow)original;
									return SyncEvent_GONetSampleInputSync_GetKey_DownArrow.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 7:
								{
									SyncEvent_GONetSampleInputSync_GetKey_LeftArrow originalTyped = (SyncEvent_GONetSampleInputSync_GetKey_LeftArrow)original;
									return SyncEvent_GONetSampleInputSync_GetKey_LeftArrow.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 8:
								{
									SyncEvent_GONetSampleInputSync_GetKey_RightArrow originalTyped = (SyncEvent_GONetSampleInputSync_GetKey_RightArrow)original;
									return SyncEvent_GONetSampleInputSync_GetKey_RightArrow.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 9:
								{
									SyncEvent_GONetSampleInputSync_GetKey_S originalTyped = (SyncEvent_GONetSampleInputSync_GetKey_S)original;
									return SyncEvent_GONetSampleInputSync_GetKey_S.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 10:
								{
									SyncEvent_GONetSampleInputSync_GetKey_UpArrow originalTyped = (SyncEvent_GONetSampleInputSync_GetKey_UpArrow)original;
									return SyncEvent_GONetSampleInputSync_GetKey_UpArrow.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 11:
								{
									SyncEvent_GONetSampleInputSync_GetKey_W originalTyped = (SyncEvent_GONetSampleInputSync_GetKey_W)original;
									return SyncEvent_GONetSampleInputSync_GetKey_W.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 12:
								{
									SyncEvent_Transform_rotation originalTyped = (SyncEvent_Transform_rotation)original;
									return SyncEvent_Transform_rotation.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 13:
								{
									SyncEvent_Transform_position originalTyped = (SyncEvent_Transform_position)original;
									return SyncEvent_Transform_position.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
						
						}
					}
					break;

				case 3:
					{
                        switch (original.SyncMemberIndex)
                        {

                            case 0:
								{
									SyncEvent_GONetParticipant_GONetId originalTyped = (SyncEvent_GONetParticipant_GONetId)original;
									return SyncEvent_GONetParticipant_GONetId.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 1:
								{
									SyncEvent_GONetParticipant_IsPositionSyncd originalTyped = (SyncEvent_GONetParticipant_IsPositionSyncd)original;
									return SyncEvent_GONetParticipant_IsPositionSyncd.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 2:
								{
									SyncEvent_GONetParticipant_IsRotationSyncd originalTyped = (SyncEvent_GONetParticipant_IsRotationSyncd)original;
									return SyncEvent_GONetParticipant_IsRotationSyncd.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 3:
								{
									SyncEvent_GONetParticipant_OwnerAuthorityId originalTyped = (SyncEvent_GONetParticipant_OwnerAuthorityId)original;
									return SyncEvent_GONetParticipant_OwnerAuthorityId.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 4:
								{
									SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId originalTyped = (SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId)original;
									return SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 5:
								{
									SyncEvent_Transform_rotation originalTyped = (SyncEvent_Transform_rotation)original;
									return SyncEvent_Transform_rotation.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 6:
								{
									SyncEvent_Transform_position originalTyped = (SyncEvent_Transform_position)original;
									return SyncEvent_Transform_position.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
						
						}
					}
					break;

				case 4:
					{
                        switch (original.SyncMemberIndex)
                        {

                            case 0:
								{
									SyncEvent_GONetParticipant_GONetId originalTyped = (SyncEvent_GONetParticipant_GONetId)original;
									return SyncEvent_GONetParticipant_GONetId.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 1:
								{
									SyncEvent_GONetParticipant_IsPositionSyncd originalTyped = (SyncEvent_GONetParticipant_IsPositionSyncd)original;
									return SyncEvent_GONetParticipant_IsPositionSyncd.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 2:
								{
									SyncEvent_GONetParticipant_IsRotationSyncd originalTyped = (SyncEvent_GONetParticipant_IsRotationSyncd)original;
									return SyncEvent_GONetParticipant_IsRotationSyncd.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 3:
								{
									SyncEvent_GONetParticipant_OwnerAuthorityId originalTyped = (SyncEvent_GONetParticipant_OwnerAuthorityId)original;
									return SyncEvent_GONetParticipant_OwnerAuthorityId.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 4:
								{
									SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId originalTyped = (SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId)original;
									return SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 5:
								{
									SyncEvent_GONetSampleInputSync_GetKey_A originalTyped = (SyncEvent_GONetSampleInputSync_GetKey_A)original;
									return SyncEvent_GONetSampleInputSync_GetKey_A.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 6:
								{
									SyncEvent_GONetSampleInputSync_GetKey_D originalTyped = (SyncEvent_GONetSampleInputSync_GetKey_D)original;
									return SyncEvent_GONetSampleInputSync_GetKey_D.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 7:
								{
									SyncEvent_GONetSampleInputSync_GetKey_DownArrow originalTyped = (SyncEvent_GONetSampleInputSync_GetKey_DownArrow)original;
									return SyncEvent_GONetSampleInputSync_GetKey_DownArrow.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 8:
								{
									SyncEvent_GONetSampleInputSync_GetKey_LeftArrow originalTyped = (SyncEvent_GONetSampleInputSync_GetKey_LeftArrow)original;
									return SyncEvent_GONetSampleInputSync_GetKey_LeftArrow.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 9:
								{
									SyncEvent_GONetSampleInputSync_GetKey_RightArrow originalTyped = (SyncEvent_GONetSampleInputSync_GetKey_RightArrow)original;
									return SyncEvent_GONetSampleInputSync_GetKey_RightArrow.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 10:
								{
									SyncEvent_GONetSampleInputSync_GetKey_S originalTyped = (SyncEvent_GONetSampleInputSync_GetKey_S)original;
									return SyncEvent_GONetSampleInputSync_GetKey_S.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 11:
								{
									SyncEvent_GONetSampleInputSync_GetKey_UpArrow originalTyped = (SyncEvent_GONetSampleInputSync_GetKey_UpArrow)original;
									return SyncEvent_GONetSampleInputSync_GetKey_UpArrow.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 12:
								{
									SyncEvent_GONetSampleInputSync_GetKey_W originalTyped = (SyncEvent_GONetSampleInputSync_GetKey_W)original;
									return SyncEvent_GONetSampleInputSync_GetKey_W.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 13:
								{
									SyncEvent_Transform_rotation originalTyped = (SyncEvent_Transform_rotation)original;
									return SyncEvent_Transform_rotation.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 14:
								{
									SyncEvent_Transform_position originalTyped = (SyncEvent_Transform_position)original;
									return SyncEvent_Transform_position.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
						
						}
					}
					break;

				case 5:
					{
                        switch (original.SyncMemberIndex)
                        {

                            case 0:
								{
									SyncEvent_GONetParticipant_GONetId originalTyped = (SyncEvent_GONetParticipant_GONetId)original;
									return SyncEvent_GONetParticipant_GONetId.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 1:
								{
									SyncEvent_GONetParticipant_IsPositionSyncd originalTyped = (SyncEvent_GONetParticipant_IsPositionSyncd)original;
									return SyncEvent_GONetParticipant_IsPositionSyncd.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 2:
								{
									SyncEvent_GONetParticipant_IsRotationSyncd originalTyped = (SyncEvent_GONetParticipant_IsRotationSyncd)original;
									return SyncEvent_GONetParticipant_IsRotationSyncd.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 3:
								{
									SyncEvent_GONetParticipant_OwnerAuthorityId originalTyped = (SyncEvent_GONetParticipant_OwnerAuthorityId)original;
									return SyncEvent_GONetParticipant_OwnerAuthorityId.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 4:
								{
									SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId originalTyped = (SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId)original;
									return SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 5:
								{
									SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate originalTyped = (SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate)original;
									return SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 6:
								{
									SyncEvent_FieldChangeTest_color originalTyped = (SyncEvent_FieldChangeTest_color)original;
									return SyncEvent_FieldChangeTest_color.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 7:
								{
									SyncEvent_FieldChangeTest_color_dosientos originalTyped = (SyncEvent_FieldChangeTest_color_dosientos)original;
									return SyncEvent_FieldChangeTest_color_dosientos.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 8:
								{
									SyncEvent_FieldChangeTest_nada originalTyped = (SyncEvent_FieldChangeTest_nada)original;
									return SyncEvent_FieldChangeTest_nada.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 9:
								{
									SyncEvent_FieldChangeTest_shortie originalTyped = (SyncEvent_FieldChangeTest_shortie)original;
									return SyncEvent_FieldChangeTest_shortie.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 10:
								{
									SyncEvent_Transform_rotation originalTyped = (SyncEvent_Transform_rotation)original;
									return SyncEvent_Transform_rotation.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 11:
								{
									SyncEvent_Transform_position originalTyped = (SyncEvent_Transform_position)original;
									return SyncEvent_Transform_position.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
						
						}
					}
					break;

				case 6:
					{
                        switch (original.SyncMemberIndex)
                        {

                            case 0:
								{
									SyncEvent_GONetParticipant_GONetId originalTyped = (SyncEvent_GONetParticipant_GONetId)original;
									return SyncEvent_GONetParticipant_GONetId.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 1:
								{
									SyncEvent_GONetParticipant_IsPositionSyncd originalTyped = (SyncEvent_GONetParticipant_IsPositionSyncd)original;
									return SyncEvent_GONetParticipant_IsPositionSyncd.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 2:
								{
									SyncEvent_GONetParticipant_IsRotationSyncd originalTyped = (SyncEvent_GONetParticipant_IsRotationSyncd)original;
									return SyncEvent_GONetParticipant_IsRotationSyncd.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 3:
								{
									SyncEvent_GONetParticipant_OwnerAuthorityId originalTyped = (SyncEvent_GONetParticipant_OwnerAuthorityId)original;
									return SyncEvent_GONetParticipant_OwnerAuthorityId.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 4:
								{
									SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId originalTyped = (SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId)original;
									return SyncEvent_GONetParticipant_RemotelyControlledByAuthorityId.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 5:
								{
									SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate originalTyped = (SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate)original;
									return SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 6:
								{
									SyncEvent_Transform_rotation originalTyped = (SyncEvent_Transform_rotation)original;
									return SyncEvent_Transform_rotation.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 7:
								{
									SyncEvent_Transform_position originalTyped = (SyncEvent_Transform_position)original;
									return SyncEvent_Transform_position.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 8:
								{
									SyncEvent_AnimatorCharacterController_parameters_Speed originalTyped = (SyncEvent_AnimatorCharacterController_parameters_Speed)original;
									return SyncEvent_AnimatorCharacterController_parameters_Speed.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 9:
								{
									SyncEvent_AnimatorCharacterController_parameters_Jump originalTyped = (SyncEvent_AnimatorCharacterController_parameters_Jump)original;
									return SyncEvent_AnimatorCharacterController_parameters_Jump.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 10:
								{
									SyncEvent_AnimatorCharacterController_parameters_Fly originalTyped = (SyncEvent_AnimatorCharacterController_parameters_Fly)original;
									return SyncEvent_AnimatorCharacterController_parameters_Fly.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 11:
								{
									SyncEvent_AnimatorCharacterController_parameters_Aim originalTyped = (SyncEvent_AnimatorCharacterController_parameters_Aim)original;
									return SyncEvent_AnimatorCharacterController_parameters_Aim.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 12:
								{
									SyncEvent_AnimatorCharacterController_parameters_H originalTyped = (SyncEvent_AnimatorCharacterController_parameters_H)original;
									return SyncEvent_AnimatorCharacterController_parameters_H.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 13:
								{
									SyncEvent_AnimatorCharacterController_parameters_V originalTyped = (SyncEvent_AnimatorCharacterController_parameters_V)original;
									return SyncEvent_AnimatorCharacterController_parameters_V.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 14:
								{
									SyncEvent_AnimatorCharacterController_parameters_Grounded originalTyped = (SyncEvent_AnimatorCharacterController_parameters_Grounded)original;
									return SyncEvent_AnimatorCharacterController_parameters_Grounded.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
						
						}
					}
					break;

			}

			return default;
		}
	}
}