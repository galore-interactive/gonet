


/* GONet (TM pending, serial number 88592370), Copyright (c) 2019 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
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
		[MessagePack.Union(2, typeof(GONet.ClientTypeFlagsChangedEvent))]
		[MessagePack.Union(3, typeof(GONet.DestroyGONetParticipantEvent))]
		[MessagePack.Union(4, typeof(GONet.GONetParticipantStartedEvent))]
		[MessagePack.Union(5, typeof(GONet.InstantiateGONetParticipantEvent))]
		[MessagePack.Union(6, typeof(GONet.OwnerAuthorityIdAssignmentEvent))]
		[MessagePack.Union(7, typeof(GONet.PersistentEvents_Bundle))]
		[MessagePack.Union(8, typeof(GONet.RequestMessage))]
		[MessagePack.Union(9, typeof(GONet.ResponseMessage))]
		[MessagePack.Union(10, typeof(GONet.ServerSaysClientInitializationCompletion))]
		[MessagePack.Union(11, typeof(GONet.SyncEvent_GONetParticipant_GONetId))]
		[MessagePack.Union(12, typeof(GONet.SyncEvent_GONetParticipant_IsPositionSyncd))]
		[MessagePack.Union(13, typeof(GONet.SyncEvent_GONetParticipant_IsRotationSyncd))]
		[MessagePack.Union(14, typeof(GONet.SyncEvent_GONetParticipant_OwnerAuthorityId))]
		[MessagePack.Union(15, typeof(GONet.SyncEvent_Time_ElapsedTicks_SetFromAuthority))]
		[MessagePack.Union(16, typeof(GONet.SyncEvent_Transform_position))]
		[MessagePack.Union(17, typeof(GONet.SyncEvent_Transform_rotation))]
		public partial interface IGONetEvent { }


	[MessagePack.Union(0, typeof(GONet.AutoMagicalSync_AllCurrentValues_Message))]
		[MessagePack.Union(1, typeof(GONet.AutoMagicalSync_ValueChanges_Message))]
		[MessagePack.Union(2, typeof(GONet.ClientTypeFlagsChangedEvent))]
		[MessagePack.Union(3, typeof(GONet.GONetParticipantStartedEvent))]
		[MessagePack.Union(4, typeof(GONet.PersistentEvents_Bundle))]
		[MessagePack.Union(5, typeof(GONet.RequestMessage))]
		[MessagePack.Union(6, typeof(GONet.ResponseMessage))]
		[MessagePack.Union(7, typeof(GONet.ServerSaysClientInitializationCompletion))]
		[MessagePack.Union(8, typeof(GONet.SyncEvent_GONetParticipant_GONetId))]
		[MessagePack.Union(9, typeof(GONet.SyncEvent_GONetParticipant_IsPositionSyncd))]
		[MessagePack.Union(10, typeof(GONet.SyncEvent_GONetParticipant_IsRotationSyncd))]
		[MessagePack.Union(11, typeof(GONet.SyncEvent_GONetParticipant_OwnerAuthorityId))]
		[MessagePack.Union(12, typeof(GONet.SyncEvent_Time_ElapsedTicks_SetFromAuthority))]
		[MessagePack.Union(13, typeof(GONet.SyncEvent_Transform_position))]
		[MessagePack.Union(14, typeof(GONet.SyncEvent_Transform_rotation))]
		public partial interface ITransientEvent : IGONetEvent { }


	[MessagePack.Union(0, typeof(GONet.DestroyGONetParticipantEvent))]
		[MessagePack.Union(1, typeof(GONet.InstantiateGONetParticipantEvent))]
		[MessagePack.Union(2, typeof(GONet.OwnerAuthorityIdAssignmentEvent))]
		public partial interface IPersistentEvent : IGONetEvent { }

		[MessagePack.Union(0, typeof(GONet.SyncEvent_GONetParticipant_GONetId))]
		[MessagePack.Union(1, typeof(GONet.SyncEvent_GONetParticipant_IsPositionSyncd))]
		[MessagePack.Union(2, typeof(GONet.SyncEvent_GONetParticipant_IsRotationSyncd))]
		[MessagePack.Union(3, typeof(GONet.SyncEvent_GONetParticipant_OwnerAuthorityId))]
		[MessagePack.Union(4, typeof(GONet.SyncEvent_Transform_position))]
		[MessagePack.Union(5, typeof(GONet.SyncEvent_Transform_rotation))]
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
				typeof(GONet.ClientTypeFlagsChangedEvent),
				typeof(GONet.DestroyGONetParticipantEvent),
				typeof(GONet.GONetParticipantStartedEvent),
				typeof(GONet.InstantiateGONetParticipantEvent),
				typeof(GONet.OwnerAuthorityIdAssignmentEvent),
				typeof(GONet.PersistentEvents_Bundle),
				typeof(GONet.RequestMessage),
				typeof(GONet.ResponseMessage),
				typeof(GONet.ServerSaysClientInitializationCompletion),
				typeof(GONet.SyncEvent_GONetParticipant_GONetId),
				typeof(GONet.SyncEvent_GONetParticipant_IsPositionSyncd),
				typeof(GONet.SyncEvent_GONetParticipant_IsRotationSyncd),
				typeof(GONet.SyncEvent_GONetParticipant_OwnerAuthorityId),
				typeof(GONet.SyncEvent_Time_ElapsedTicks_SetFromAuthority),
				typeof(GONet.SyncEvent_Transform_position),
				typeof(GONet.SyncEvent_Transform_rotation),
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

			var item_codeGenerationId1_single1_singleMember1 = new GONet.Utils.QuantizerSettingsGroup(-5000f, 5000f, 0, true);
			settings.Add(item_codeGenerationId1_single1_singleMember1);

			return settings;
		}

		static internal GONetParticipant_AutoMagicalSyncCompanion_Generated hahaThisIsTrulyTheRealness(GONetParticipant gonetParticipant)
		{
			switch (gonetParticipant.codeGenerationId)
			{
				case 1:
					return new GONetParticipant_AutoMagicalSyncCompanion_Generated_1(gonetParticipant);
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

			}

			return default;
		}
	}
}