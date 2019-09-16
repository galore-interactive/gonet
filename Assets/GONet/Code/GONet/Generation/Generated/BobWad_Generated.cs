


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
		[MessagePack.Union(2, typeof(GONet.ClientTypeFlagsChangedEvent))]
		[MessagePack.Union(3, typeof(GONet.DestroyGONetParticipantEvent))]
		[MessagePack.Union(4, typeof(GONet.GONetParticipantStartedEvent))]
		[MessagePack.Union(5, typeof(GONet.InstantiateGONetParticipantEvent))]
		[MessagePack.Union(6, typeof(GONet.OwnerAuthorityIdAssignmentEvent))]
		[MessagePack.Union(7, typeof(GONet.PersistentEvents_Bundle))]
		[MessagePack.Union(8, typeof(GONet.RequestMessage))]
		[MessagePack.Union(9, typeof(GONet.ResponseMessage))]
		[MessagePack.Union(10, typeof(GONet.ServerSaysClientInitializationCompletion))]
		[MessagePack.Union(11, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Aim))]
		[MessagePack.Union(12, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Fly))]
		[MessagePack.Union(13, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Grounded))]
		[MessagePack.Union(14, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_H))]
		[MessagePack.Union(15, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Jump))]
		[MessagePack.Union(16, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Speed))]
		[MessagePack.Union(17, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_V))]
		[MessagePack.Union(18, typeof(GONet.SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate))]
		[MessagePack.Union(19, typeof(GONet.SyncEvent_FieldChangeTest_color))]
		[MessagePack.Union(20, typeof(GONet.SyncEvent_FieldChangeTest_color_dosientos))]
		[MessagePack.Union(21, typeof(GONet.SyncEvent_FieldChangeTest_nada))]
		[MessagePack.Union(22, typeof(GONet.SyncEvent_FieldChangeTest_shortie))]
		[MessagePack.Union(23, typeof(GONet.SyncEvent_GONetParticipant_GONetId))]
		[MessagePack.Union(24, typeof(GONet.SyncEvent_GONetParticipant_IsPositionSyncd))]
		[MessagePack.Union(25, typeof(GONet.SyncEvent_GONetParticipant_IsRotationSyncd))]
		[MessagePack.Union(26, typeof(GONet.SyncEvent_GONetParticipant_OwnerAuthorityId))]
		[MessagePack.Union(27, typeof(GONet.SyncEvent_Time_ElapsedTicks_SetFromAuthority))]
		[MessagePack.Union(28, typeof(GONet.SyncEvent_Transform_position))]
		[MessagePack.Union(29, typeof(GONet.SyncEvent_Transform_rotation))]
		public partial interface IGONetEvent { }


	[MessagePack.Union(0, typeof(GONet.AutoMagicalSync_AllCurrentValues_Message))]
		[MessagePack.Union(1, typeof(GONet.AutoMagicalSync_ValueChanges_Message))]
		[MessagePack.Union(2, typeof(GONet.ClientTypeFlagsChangedEvent))]
		[MessagePack.Union(3, typeof(GONet.GONetParticipantStartedEvent))]
		[MessagePack.Union(4, typeof(GONet.PersistentEvents_Bundle))]
		[MessagePack.Union(5, typeof(GONet.RequestMessage))]
		[MessagePack.Union(6, typeof(GONet.ResponseMessage))]
		[MessagePack.Union(7, typeof(GONet.ServerSaysClientInitializationCompletion))]
		[MessagePack.Union(8, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Aim))]
		[MessagePack.Union(9, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Fly))]
		[MessagePack.Union(10, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Grounded))]
		[MessagePack.Union(11, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_H))]
		[MessagePack.Union(12, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Jump))]
		[MessagePack.Union(13, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_Speed))]
		[MessagePack.Union(14, typeof(GONet.SyncEvent_AnimatorCharacterController_parameters_V))]
		[MessagePack.Union(15, typeof(GONet.SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate))]
		[MessagePack.Union(16, typeof(GONet.SyncEvent_FieldChangeTest_color))]
		[MessagePack.Union(17, typeof(GONet.SyncEvent_FieldChangeTest_color_dosientos))]
		[MessagePack.Union(18, typeof(GONet.SyncEvent_FieldChangeTest_nada))]
		[MessagePack.Union(19, typeof(GONet.SyncEvent_FieldChangeTest_shortie))]
		[MessagePack.Union(20, typeof(GONet.SyncEvent_GONetParticipant_GONetId))]
		[MessagePack.Union(21, typeof(GONet.SyncEvent_GONetParticipant_IsPositionSyncd))]
		[MessagePack.Union(22, typeof(GONet.SyncEvent_GONetParticipant_IsRotationSyncd))]
		[MessagePack.Union(23, typeof(GONet.SyncEvent_GONetParticipant_OwnerAuthorityId))]
		[MessagePack.Union(24, typeof(GONet.SyncEvent_Time_ElapsedTicks_SetFromAuthority))]
		[MessagePack.Union(25, typeof(GONet.SyncEvent_Transform_position))]
		[MessagePack.Union(26, typeof(GONet.SyncEvent_Transform_rotation))]
		public partial interface ITransientEvent : IGONetEvent { }


	[MessagePack.Union(0, typeof(GONet.DestroyGONetParticipantEvent))]
		[MessagePack.Union(1, typeof(GONet.InstantiateGONetParticipantEvent))]
		[MessagePack.Union(2, typeof(GONet.OwnerAuthorityIdAssignmentEvent))]
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
		[MessagePack.Union(16, typeof(GONet.SyncEvent_Transform_position))]
		[MessagePack.Union(17, typeof(GONet.SyncEvent_Transform_rotation))]
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

        static readonly Utils.ObjectPool<SyncEvent_GONetParticipant_GONetId> pool = new Utils.ObjectPool<SyncEvent_GONetParticipant_GONetId>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 5);
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

        static readonly Utils.ObjectPool<SyncEvent_GONetParticipant_IsPositionSyncd> pool = new Utils.ObjectPool<SyncEvent_GONetParticipant_IsPositionSyncd>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 5);
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

        static readonly Utils.ObjectPool<SyncEvent_GONetParticipant_IsRotationSyncd> pool = new Utils.ObjectPool<SyncEvent_GONetParticipant_IsRotationSyncd>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 5);
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

        static readonly Utils.ObjectPool<SyncEvent_GONetParticipant_OwnerAuthorityId> pool = new Utils.ObjectPool<SyncEvent_GONetParticipant_OwnerAuthorityId>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 5);
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
    public sealed class SyncEvent_Transform_rotation : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public UnityEngine.Quaternion valuePrevious;
		[MessagePack.Key(7)] public UnityEngine.Quaternion valueNew;

        static readonly Utils.ObjectPool<SyncEvent_Transform_rotation> pool = new Utils.ObjectPool<SyncEvent_Transform_rotation>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 5);
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

        static readonly Utils.ObjectPool<SyncEvent_Transform_position> pool = new Utils.ObjectPool<SyncEvent_Transform_position>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 5);
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
    public sealed class SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public System.Single valuePrevious;
		[MessagePack.Key(7)] public System.Single valueNew;

        static readonly Utils.ObjectPool<SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate> pool = new Utils.ObjectPool<SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 5);
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

        static readonly Utils.ObjectPool<SyncEvent_FieldChangeTest_color> pool = new Utils.ObjectPool<SyncEvent_FieldChangeTest_color>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 5);
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
    public sealed class SyncEvent_FieldChangeTest_nada : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public System.Single valuePrevious;
		[MessagePack.Key(7)] public System.Single valueNew;

        static readonly Utils.ObjectPool<SyncEvent_FieldChangeTest_nada> pool = new Utils.ObjectPool<SyncEvent_FieldChangeTest_nada>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 5);
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

        static readonly Utils.ObjectPool<SyncEvent_FieldChangeTest_shortie> pool = new Utils.ObjectPool<SyncEvent_FieldChangeTest_shortie>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 5);
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
    public sealed class SyncEvent_FieldChangeTest_color_dosientos : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public UnityEngine.Vector3 valuePrevious;
		[MessagePack.Key(7)] public UnityEngine.Vector3 valueNew;

        static readonly Utils.ObjectPool<SyncEvent_FieldChangeTest_color_dosientos> pool = new Utils.ObjectPool<SyncEvent_FieldChangeTest_color_dosientos>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 5);
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
    public sealed class SyncEvent_AnimatorCharacterController_parameters_Speed : SyncEvent_ValueChangeProcessed
    {
		[MessagePack.Key(6)] public System.Single valuePrevious;
		[MessagePack.Key(7)] public System.Single valueNew;

        static readonly Utils.ObjectPool<SyncEvent_AnimatorCharacterController_parameters_Speed> pool = new Utils.ObjectPool<SyncEvent_AnimatorCharacterController_parameters_Speed>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 5);
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

        static readonly Utils.ObjectPool<SyncEvent_AnimatorCharacterController_parameters_Jump> pool = new Utils.ObjectPool<SyncEvent_AnimatorCharacterController_parameters_Jump>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 5);
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

        static readonly Utils.ObjectPool<SyncEvent_AnimatorCharacterController_parameters_Fly> pool = new Utils.ObjectPool<SyncEvent_AnimatorCharacterController_parameters_Fly>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 5);
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

        static readonly Utils.ObjectPool<SyncEvent_AnimatorCharacterController_parameters_Aim> pool = new Utils.ObjectPool<SyncEvent_AnimatorCharacterController_parameters_Aim>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 5);
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

        static readonly Utils.ObjectPool<SyncEvent_AnimatorCharacterController_parameters_H> pool = new Utils.ObjectPool<SyncEvent_AnimatorCharacterController_parameters_H>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 5);
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

        static readonly Utils.ObjectPool<SyncEvent_AnimatorCharacterController_parameters_V> pool = new Utils.ObjectPool<SyncEvent_AnimatorCharacterController_parameters_V>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 5);
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

        static readonly Utils.ObjectPool<SyncEvent_AnimatorCharacterController_parameters_Grounded> pool = new Utils.ObjectPool<SyncEvent_AnimatorCharacterController_parameters_Grounded>(GONetMain.SYNC_EVENT_QUEUE_SAVE_WHEN_FULL_SIZE + 50, 5);
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

			var item_codeGenerationId1_single1_singleMember1 = new GONet.Utils.QuantizerSettingsGroup(-5000f, 5000f, 0, true);
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

			var item_codeGenerationId2_single2_singleMember0 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId2_single2_singleMember0);

			var item_codeGenerationId2_single2_singleMember1 = new GONet.Utils.QuantizerSettingsGroup(-10f, 10f, 16, true);
			settings.Add(item_codeGenerationId2_single2_singleMember1);

			var item_codeGenerationId2_single2_singleMember2 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId2_single2_singleMember2);

			var item_codeGenerationId2_single3_singleMember0 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId2_single3_singleMember0);

			var item_codeGenerationId2_single3_singleMember1 = new GONet.Utils.QuantizerSettingsGroup(-5000f, 5000f, 0, true);
			settings.Add(item_codeGenerationId2_single3_singleMember1);

			var item_codeGenerationId3_single0_singleMember0 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId3_single0_singleMember0);

			var item_codeGenerationId3_single0_singleMember1 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId3_single0_singleMember1);

			var item_codeGenerationId3_single0_singleMember2 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId3_single0_singleMember2);

			var item_codeGenerationId3_single0_singleMember3 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId3_single0_singleMember3);

			var item_codeGenerationId3_single1_singleMember0 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId3_single1_singleMember0);

			var item_codeGenerationId3_single2_singleMember0 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId3_single2_singleMember0);

			var item_codeGenerationId3_single2_singleMember1 = new GONet.Utils.QuantizerSettingsGroup(-5000f, 5000f, 0, true);
			settings.Add(item_codeGenerationId3_single2_singleMember1);

			var item_codeGenerationId3_single3_singleMember0 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId3_single3_singleMember0);

			var item_codeGenerationId3_single3_singleMember1 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId3_single3_singleMember1);

			var item_codeGenerationId3_single3_singleMember2 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId3_single3_singleMember2);

			var item_codeGenerationId3_single3_singleMember3 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId3_single3_singleMember3);

			var item_codeGenerationId3_single3_singleMember4 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId3_single3_singleMember4);

			var item_codeGenerationId3_single3_singleMember5 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId3_single3_singleMember5);

			var item_codeGenerationId3_single3_singleMember6 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId3_single3_singleMember6);

			var item_codeGenerationId4_single0_singleMember0 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId4_single0_singleMember0);

			var item_codeGenerationId4_single0_singleMember1 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId4_single0_singleMember1);

			var item_codeGenerationId4_single0_singleMember2 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId4_single0_singleMember2);

			var item_codeGenerationId4_single0_singleMember3 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId4_single0_singleMember3);

			var item_codeGenerationId4_single1_singleMember0 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId4_single1_singleMember0);

			var item_codeGenerationId4_single2_singleMember0 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId4_single2_singleMember0);

			var item_codeGenerationId4_single2_singleMember1 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId4_single2_singleMember1);

			var item_codeGenerationId4_single2_singleMember2 = new GONet.Utils.QuantizerSettingsGroup(-10f, 10f, 16, true);
			settings.Add(item_codeGenerationId4_single2_singleMember2);

			var item_codeGenerationId4_single2_singleMember3 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId4_single2_singleMember3);

			var item_codeGenerationId4_single3_singleMember0 = new GONet.Utils.QuantizerSettingsGroup(-1.701412E+38f, 1.701412E+38f, 0, true);
			settings.Add(item_codeGenerationId4_single3_singleMember0);

			var item_codeGenerationId4_single3_singleMember1 = new GONet.Utils.QuantizerSettingsGroup(-5000f, 5000f, 0, true);
			settings.Add(item_codeGenerationId4_single3_singleMember1);

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
																	var valueNew = companion.GONetParticipant.GONetId;
																	System.UInt32 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_UInt32 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_UInt32; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_GONetId.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 1, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 1:
								{
																	var valueNew = companion.GONetParticipant.IsPositionSyncd;
																	System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_IsPositionSyncd.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 1, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 2:
								{
																	var valueNew = companion.GONetParticipant.IsRotationSyncd;
																	System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_IsRotationSyncd.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 1, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 3:
								{
																	var valueNew = companion.GONetParticipant.OwnerAuthorityId;
																	System.UInt16 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_UInt16 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_UInt16; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_OwnerAuthorityId.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 1, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 4:
								{
																	var valueNew = companion.Transform.rotation;
																	UnityEngine.Quaternion valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.UnityEngine_Quaternion : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.UnityEngine_Quaternion; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_Transform_rotation.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 1, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 5:
								{
																	var valueNew = companion.Transform.position;
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
																	var valueNew = companion.GONetParticipant.GONetId;
																	System.UInt32 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_UInt32 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_UInt32; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_GONetId.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 2, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 1:
								{
																	var valueNew = companion.GONetParticipant.IsPositionSyncd;
																	System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_IsPositionSyncd.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 2, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 2:
								{
																	var valueNew = companion.GONetParticipant.IsRotationSyncd;
																	System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_IsRotationSyncd.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 2, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 3:
								{
																	var valueNew = companion.GONetParticipant.OwnerAuthorityId;
																	System.UInt16 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_UInt16 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_UInt16; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_OwnerAuthorityId.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 2, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 4:
								{
																	var valueNew = companion.DestroyIfMineOnKeyPress.willHeUpdate;
																	System.Single valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Single : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Single; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 2, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 5:
								{
																	var valueNew = companion.FieldChangeTest.color;
																	UnityEngine.Vector3 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.UnityEngine_Vector3 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.UnityEngine_Vector3; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_FieldChangeTest_color.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 2, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 6:
								{
																	var valueNew = companion.FieldChangeTest.nada;
																	System.Single valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Single : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Single; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_FieldChangeTest_nada.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 2, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 7:
								{
																	var valueNew = companion.FieldChangeTest.shortie;
																	System.Int16 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Int16 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Int16; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_FieldChangeTest_shortie.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 2, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 8:
								{
																	var valueNew = companion.Transform.rotation;
																	UnityEngine.Quaternion valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.UnityEngine_Quaternion : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.UnityEngine_Quaternion; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_Transform_rotation.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 2, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 9:
								{
																	var valueNew = companion.Transform.position;
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
																	var valueNew = companion.GONetParticipant.GONetId;
																	System.UInt32 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_UInt32 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_UInt32; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_GONetId.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 3, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 1:
								{
																	var valueNew = companion.GONetParticipant.IsPositionSyncd;
																	System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_IsPositionSyncd.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 3, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 2:
								{
																	var valueNew = companion.GONetParticipant.IsRotationSyncd;
																	System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_IsRotationSyncd.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 3, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 3:
								{
																	var valueNew = companion.GONetParticipant.OwnerAuthorityId;
																	System.UInt16 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_UInt16 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_UInt16; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_OwnerAuthorityId.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 3, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 4:
								{
																	var valueNew = companion.DestroyIfMineOnKeyPress.willHeUpdate;
																	System.Single valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Single : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Single; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 3, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 5:
								{
																	var valueNew = companion.Transform.rotation;
																	UnityEngine.Quaternion valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.UnityEngine_Quaternion : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.UnityEngine_Quaternion; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_Transform_rotation.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 3, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 6:
								{
																	var valueNew = companion.Transform.position;
																	UnityEngine.Vector3 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.UnityEngine_Vector3 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.UnityEngine_Vector3; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_Transform_position.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 3, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 7:
								{
																	System.Single valueNew = companion.Animator.GetFloat(-823668238);
																	System.Single valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Single : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Single; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_AnimatorCharacterController_parameters_Speed.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 3, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 8:
								{
																	System.Boolean valueNew = companion.Animator.GetBool(125937960);
																	System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_AnimatorCharacterController_parameters_Jump.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 3, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 9:
								{
																	System.Boolean valueNew = companion.Animator.GetBool(1808254291);
																	System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_AnimatorCharacterController_parameters_Fly.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 3, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 10:
								{
																	System.Boolean valueNew = companion.Animator.GetBool(153482222);
																	System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_AnimatorCharacterController_parameters_Aim.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 3, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 11:
								{
																	System.Single valueNew = companion.Animator.GetFloat(-1442503121);
																	System.Single valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Single : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Single; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_AnimatorCharacterController_parameters_H.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 3, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 12:
								{
																	System.Single valueNew = companion.Animator.GetFloat(1342839628);
																	System.Single valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Single : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Single; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_AnimatorCharacterController_parameters_V.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 3, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 13:
								{
																	System.Boolean valueNew = companion.Animator.GetBool(862969536);
																	System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_AnimatorCharacterController_parameters_Grounded.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 3, syncMemberIndex, valuePrevious, valueNew);
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
																	var valueNew = companion.GONetParticipant.GONetId;
																	System.UInt32 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_UInt32 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_UInt32; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_GONetId.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 4, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 1:
								{
																	var valueNew = companion.GONetParticipant.IsPositionSyncd;
																	System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_IsPositionSyncd.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 4, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 2:
								{
																	var valueNew = companion.GONetParticipant.IsRotationSyncd;
																	System.Boolean valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Boolean : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Boolean; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_IsRotationSyncd.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 4, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 3:
								{
																	var valueNew = companion.GONetParticipant.OwnerAuthorityId;
																	System.UInt16 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_UInt16 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_UInt16; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_GONetParticipant_OwnerAuthorityId.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 4, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 4:
								{
																	var valueNew = companion.DestroyIfMineOnKeyPress.willHeUpdate;
																	System.Single valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Single : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Single; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 4, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 5:
								{
																	var valueNew = companion.FieldChangeTest.color;
																	UnityEngine.Vector3 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.UnityEngine_Vector3 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.UnityEngine_Vector3; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_FieldChangeTest_color.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 4, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 6:
								{
																	var valueNew = companion.FieldChangeTest.color_dosientos;
																	UnityEngine.Vector3 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.UnityEngine_Vector3 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.UnityEngine_Vector3; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_FieldChangeTest_color_dosientos.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 4, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 7:
								{
																	var valueNew = companion.FieldChangeTest.nada;
																	System.Single valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Single : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Single; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_FieldChangeTest_nada.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 4, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 8:
								{
																	var valueNew = companion.FieldChangeTest.shortie;
																	System.Int16 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.System_Int16 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.System_Int16; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_FieldChangeTest_shortie.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 4, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 9:
								{
																	var valueNew = companion.Transform.rotation;
																	UnityEngine.Quaternion valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.UnityEngine_Quaternion : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.UnityEngine_Quaternion; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_Transform_rotation.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 4, syncMemberIndex, valuePrevious, valueNew);
								}
                            case 10:
								{
																	var valueNew = companion.Transform.position;
																	UnityEngine.Vector3 valuePrevious = explanation == SyncEvent_ValueChangeProcessedExplanation.OutboundToOthers ? companion.valuesChangesSupport[syncMemberIndex].lastKnownValue_previous.UnityEngine_Vector3 : companion.valuesChangesSupport[syncMemberIndex].lastKnownValue.UnityEngine_Vector3; // because of order of operations and state of affairs at the time of publishing this event being different for the in/out direction, there is a different value to pull from that represents the previous value
									return SyncEvent_Transform_position.Borrow(explanation, elapsedTicks, filterUsingOwnerAuthorityId, syncCompanion.gonetParticipant.GONetId, 4, syncMemberIndex, valuePrevious, valueNew);
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
									SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate originalTyped = (SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate)original;
									return SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 5:
								{
									SyncEvent_FieldChangeTest_color originalTyped = (SyncEvent_FieldChangeTest_color)original;
									return SyncEvent_FieldChangeTest_color.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 6:
								{
									SyncEvent_FieldChangeTest_nada originalTyped = (SyncEvent_FieldChangeTest_nada)original;
									return SyncEvent_FieldChangeTest_nada.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 7:
								{
									SyncEvent_FieldChangeTest_shortie originalTyped = (SyncEvent_FieldChangeTest_shortie)original;
									return SyncEvent_FieldChangeTest_shortie.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 8:
								{
									SyncEvent_Transform_rotation originalTyped = (SyncEvent_Transform_rotation)original;
									return SyncEvent_Transform_rotation.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 9:
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
									SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate originalTyped = (SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate)original;
									return SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
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
                            case 7:
								{
									SyncEvent_AnimatorCharacterController_parameters_Speed originalTyped = (SyncEvent_AnimatorCharacterController_parameters_Speed)original;
									return SyncEvent_AnimatorCharacterController_parameters_Speed.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 8:
								{
									SyncEvent_AnimatorCharacterController_parameters_Jump originalTyped = (SyncEvent_AnimatorCharacterController_parameters_Jump)original;
									return SyncEvent_AnimatorCharacterController_parameters_Jump.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 9:
								{
									SyncEvent_AnimatorCharacterController_parameters_Fly originalTyped = (SyncEvent_AnimatorCharacterController_parameters_Fly)original;
									return SyncEvent_AnimatorCharacterController_parameters_Fly.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 10:
								{
									SyncEvent_AnimatorCharacterController_parameters_Aim originalTyped = (SyncEvent_AnimatorCharacterController_parameters_Aim)original;
									return SyncEvent_AnimatorCharacterController_parameters_Aim.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 11:
								{
									SyncEvent_AnimatorCharacterController_parameters_H originalTyped = (SyncEvent_AnimatorCharacterController_parameters_H)original;
									return SyncEvent_AnimatorCharacterController_parameters_H.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 12:
								{
									SyncEvent_AnimatorCharacterController_parameters_V originalTyped = (SyncEvent_AnimatorCharacterController_parameters_V)original;
									return SyncEvent_AnimatorCharacterController_parameters_V.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 13:
								{
									SyncEvent_AnimatorCharacterController_parameters_Grounded originalTyped = (SyncEvent_AnimatorCharacterController_parameters_Grounded)original;
									return SyncEvent_AnimatorCharacterController_parameters_Grounded.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
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
									SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate originalTyped = (SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate)original;
									return SyncEvent_DestroyIfMineOnKeyPress_willHeUpdate.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 5:
								{
									SyncEvent_FieldChangeTest_color originalTyped = (SyncEvent_FieldChangeTest_color)original;
									return SyncEvent_FieldChangeTest_color.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 6:
								{
									SyncEvent_FieldChangeTest_color_dosientos originalTyped = (SyncEvent_FieldChangeTest_color_dosientos)original;
									return SyncEvent_FieldChangeTest_color_dosientos.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 7:
								{
									SyncEvent_FieldChangeTest_nada originalTyped = (SyncEvent_FieldChangeTest_nada)original;
									return SyncEvent_FieldChangeTest_nada.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 8:
								{
									SyncEvent_FieldChangeTest_shortie originalTyped = (SyncEvent_FieldChangeTest_shortie)original;
									return SyncEvent_FieldChangeTest_shortie.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 9:
								{
									SyncEvent_Transform_rotation originalTyped = (SyncEvent_Transform_rotation)original;
									return SyncEvent_Transform_rotation.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
                            case 10:
								{
									SyncEvent_Transform_position originalTyped = (SyncEvent_Transform_position)original;
									return SyncEvent_Transform_position.Borrow(original.Explanation, original.OccurredAtElapsedTicks, original.RelatedOwnerAuthorityId, original.GONetId, original.CodeGenerationId, original.SyncMemberIndex, originalTyped.valuePrevious, originalTyped.valueNew);
								}
						
						}
					}
					break;

			}

			return default;
		}
	}
}