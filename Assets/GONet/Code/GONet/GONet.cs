/* Copyright (C) Shaun Curtis Sheppard - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 * Written by Shaun Sheppard <shasheppard@gmail.com>, June 2019
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products
 */

using GONet.Generation;
using GONet.Utils;
using ReliableNetcode;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

using GONetCodeGenerationId = System.Byte;
using GONetChannelId = System.Byte;
using GONet.Database.Sqlite;
using GONet.Database;
using MessagePack;

namespace GONet
{
    public static class GONetMain
    {
        public const ulong noIdeaWhatThisShouldBe_CopiedFromTheirUnitTest = 0x1122334455667788L;

        public static readonly byte[] _privateKey = new byte[] // TODO generate this!?
        {
            0x60, 0x6a, 0xbe, 0x6e, 0xc9, 0x19, 0x10, 0xea,
            0x9a, 0x65, 0x62, 0xf6, 0x6f, 0x2b, 0x30, 0xe4,
            0x43, 0x71, 0xd6, 0x2c, 0xd1, 0x99, 0x27, 0x26,
            0x6b, 0x3c, 0x60, 0xf4, 0xb7, 0x15, 0xab, 0xa1,
        };

        private static GONetSessionContext globalSessionContext;

        public static GONetSessionContext GlobalSessionContext
        {
            get { return globalSessionContext; }
            internal set
            {
                globalSessionContext = value;
                GlobalSessionContext_Participant = (object)globalSessionContext == null ? null : globalSessionContext.gameObject.GetComponent<GONetParticipant>();
            }
        }

        public static GONetParticipant GlobalSessionContext_Participant { get; private set; }

        private static GONetSessionContext mySessionContext;

        public static GONetSessionContext MySessionContext
        {
            get { return mySessionContext; }
            internal set
            {
                mySessionContext = value;
                MySessionContext_Participant = (object)mySessionContext == null ? null : mySessionContext.gameObject.GetComponent<GONetParticipant>();

                // TODO FIXME need to update the connection associated with OwnerAuthorityId
            }
        }

        public static GONetParticipant MySessionContext_Participant { get; private set; } // TODO FIXME need to spawn this for everyone and set it here!
        public static uint MyAuthorityId { get; private set; }

        internal static bool isServerOverride = NetworkUtils.IsIPAddressOnLocalMachine(GONetSampleClientOrServer.serverIP) && !NetworkUtils.IsLocalPortListening(GONetSampleClientOrServer.serverPort); // TODO FIXME gotta iron out good startup process..this is quite temporary
        /// <summary>
        /// IMPORTANT: This can be true even when <see cref="IsClient"/> is also true.
        ///            At time of writing, the case for that would be when <see cref="clientTypeFlags"/> has <see cref="ClientTypeFlags.ServerHost"/> set.
        /// </summary>
        public static bool IsServer => isServerOverride || MyAuthorityId == OwnerAuthorityId_Server; // TODO cache this since it will not change and too much processing to get now

        /// <summary>
        /// IMPORTANT: This can return true even when <see cref="IsServer"/> is also true.
        ///            At time of writing, the case for that would be when <see cref="clientTypeFlags"/> has <see cref="ClientTypeFlags.ServerHost"/> set.
        /// </summary>
        public static bool IsClientType(ClientTypeFlags requiredFlags)
        {
            return (MyClientTypeFlags & requiredFlags) == requiredFlags;
        }

        public static ClientTypeFlags MyClientTypeFlags => _gonetClient == null ? ClientTypeFlags.None : _gonetClient.ClientTypeFlags;

        /// <summary>
        /// IMPORTANT: This can be true even when <see cref="IsServer"/> is also true.
        ///            At time of writing, the case for that would be when <see cref="clientTypeFlags"/> has <see cref="ClientTypeFlags.ServerHost"/> set.
        /// </summary>
        public static bool IsClient => _gonetClient == null ? false : _gonetClient.ClientTypeFlags != ClientTypeFlags.None;

        private static GONetServer _gonetServer; // TODO remove this once we make gonetServer private again!
        /// <summary>
        /// TODO FIXME make this private.....its internal temporary for testing
        /// </summary>
        internal static GONetServer gonetServer
        {
            get { return _gonetServer; }
            set
            {
                MyAuthorityId = OwnerAuthorityId_Server;
                _gonetServer = value;
                _gonetServer.ClientConnected += Server_OnClientConnected_SendClientCurrentState;
            }
        }

        public static GONetEventBus EventBus => GONetEventBus.Instance;

        public static bool IsUnityApplicationEditor { get; internal set; }  = false;

        static readonly Queue<IPersistentEvent> persistentEventsThisSession = new Queue<IPersistentEvent>();

        internal static GONetClient _gonetClient;
        /// <summary>
        /// TODO FIXME make this private.....its internal temporary for testing
        /// </summary>
        internal static GONetClient GONetClient
        {
            get => _gonetClient;

            set
            {
                ClientTypeFlags flagsPrevious = MyClientTypeFlags;

                _gonetClient = value;

                ClientTypeFlags flagsNow = MyClientTypeFlags;

                if (flagsNow != flagsPrevious)
                {
                    EventBus.Publish(new ClientTypeFlagsChangedEvent(Time.ElapsedTicks, MyAuthorityId, flagsPrevious, flagsNow));
                }
            }
        }

        internal static readonly Dictionary<uint, GONetParticipant> gonetParticipantByGONetIdMap = new Dictionary<uint, GONetParticipant>(1000);

        public const uint OwnerAuthorityId_Unset = 0;
        public const uint OwnerAuthorityId_Server = uint.MaxValue;

        /// <summary>
        /// Only used/applicable if <see cref="IsServer"/> is true.
        /// </summary>
        private static uint server_lastAssignedAuthorityId = OwnerAuthorityId_Unset;

        /// <summary>
        /// <para>Use this to write code that does one thing if you are the owner and another thing if not.</para>
        /// <para>From a GONet perspective, this checks if the <paramref name="gameObject"/> has a <see cref="GONetParticipant"/> and if so, whether or not you own it.</para>
        /// <para>If you already have access to the <see cref="GONetParticipant"/> associated with this <paramref name="gameObject"/>, then use the sister method instead: <see cref="IsMine(GONetParticipant)"/></para>
        /// </summary>
        public static bool IsMine(GameObject gameObject)
        {
            return gameObject.GetComponent<GONetParticipant>()?.OwnerAuthorityId == MyAuthorityId; // TODO cache instead of lookup/get each time!
        }

        /// <summary>
        /// <para>Use this to write code that does one thing if you are the owner and another thing if not.</para>
        /// <para>From a GONet perspective, this checks if the <paramref name="gameObject"/> has a <see cref="GONetParticipant"/> and if so, whether or not you own it.</para>
        /// </summary>
        public static bool IsMine(GONetParticipant gonetParticipant)
        {
            return gonetParticipant.OwnerAuthorityId == MyAuthorityId;
        }

        /// <summary>
        /// NOTE: The time maintained within is only updated once per main thread frame tick (i.e., call to <see cref="Update"/>).
        /// </summary>
        internal static readonly SecretaryOfTemporalAffairs Time = new SecretaryOfTemporalAffairs();

        /// <summary>
        /// This is used to know which instances were instantiated due to a remote spawn message being received/processed.
        /// See <see cref="Instantiate_Remote(InstantiateGONetParticipantEvent)"/> and <see cref="Start_AutoPropogateInstantiation_IfAppropriate(GONetParticipant)"/>.
        /// </summary>
        static readonly List<GONetParticipant> remoteSpawns_avoidAutoPropogateSupport = new List<GONetParticipant>(1000);

        static readonly AutoMagicalSqliteDatabase sqlite = new AutoMagicalSqliteDatabase();

        static GONetMain()
        {
            //Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            InitMessageTypeToMessageIDMap();
            InitShouldSkipSyncSupport();
            InitEventSubscriptions();

            const string DATABASE_PATH_RELATIVE = AutoMagicalSqliteDatabase.CONNECTION_STRING_FILE_URI_PREFIX + "database/";
            const string DATA_SOURCE_SOLO = "Data Source=";
            const string DATA_SOURCE = DATA_SOURCE_SOLO + DATABASE_PATH_RELATIVE;
            const string DATE_FORMAT = "yyyy_MM_dd___HH-mm-ss";
            const string DB_EXT = ".db";
            sqlite.ConnectionString = string.Concat(DATA_SOURCE, DateTime.Now.ToString(DATE_FORMAT), DB_EXT); // setup for new database to save into
        }

        private static void InitEventSubscriptions()
        {
            EventBus.Subscribe<IGONetEvent>(OnAnyEvent_RelayToRemoteConnections_IfAppropriate);
            EventBus.Subscribe<IPersistentEvent>(OnPersistentEvent_KeepTrack);
            EventBus.Subscribe<PersistentEvents_Bundle>(OnPersistentEventsBundle_ProcessAll_Remote, envelope => envelope.IsSourceRemote);
            EventBus.Subscribe<InstantiateGONetParticipantEvent>(OnInstantiationEvent_Remote, envelope => envelope.IsSourceRemote);
        }

        private static void OnPersistentEventsBundle_ProcessAll_Remote(IGONetEventEnvelope<PersistentEvents_Bundle> eventEnvelope)
        {
            foreach (var item in eventEnvelope.Event.PersistentEvents)
            {
                persistentEventsThisSession.Enqueue(item);

                if (item is InstantiateGONetParticipantEvent)
                {
                    Instantiate_Remote((InstantiateGONetParticipantEvent)item);
                }
            }
        }

        /// <summary>
        /// Definition of "if appropriate":
        ///     -The server will always send to remote connections....clients only send to remote connections (i.e., just to server) when locally sourced!
        /// </summary>
        private static void OnAnyEvent_RelayToRemoteConnections_IfAppropriate(IGONetEventEnvelope<IGONetEvent> eventEnvelope)
        {
            if (IsServer && eventEnvelope.IsSourceRemote) // in this case we have to be more selective and avoid sending to the remote originator!
            {
                byte[] bytes = SerializationUtils.SerializeToBytes(eventEnvelope.Event); // TODO FIXME if the envelope is processed from a remote source, then we SHOULD attach the bytes to it and reuse them!

                uint count = _gonetServer.numConnections;// remoteClients.Length;
                for (uint i = 0; i < count; ++i)
                {
                    var remoteClient = _gonetServer.remoteClients[i];
                    if (remoteClient.OwnerAuthorityId != eventEnvelope.SourceAuthorityId)
                    {
                        GONetChannelId channelId = GONetChannel.EventSingles_Reliable; // TODO FIXME the envelope should have this on it as well if remote source
                        SendBytesToRemoteConnection(remoteClient, bytes, bytes.Length, channelId);
                    }
                }
            }
            else if (IsServer || !eventEnvelope.IsSourceRemote)
            {
                byte[] bytes = SerializationUtils.SerializeToBytes(eventEnvelope.Event);
                bool shouldSendRelilably = true; // TODO support unreliable events?
                SendBytesToRemoteConnections(bytes, bytes.Length, shouldSendRelilably ? GONetChannel.EventSingles_Reliable : GONetChannel.EventSingles_Unreliable);
            }
        }

        private static void OnPersistentEvent_KeepTrack(IGONetEventEnvelope<IPersistentEvent> eventEnvelope)
        {
            GONetLog.Debug("pub/sub Persistent");

            persistentEventsThisSession.Enqueue(eventEnvelope.Event);
        }

        private static void OnInstantiationEvent_Remote(IGONetEventEnvelope<InstantiateGONetParticipantEvent> eventEnvelope)
        {
            GONetLog.Debug("pub/sub Instantiate REMOTE");

            Instantiate_Remote(eventEnvelope.Event);
        }

        private static void InitShouldSkipSyncSupport()
        {
            GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap[(int)GONetAutoMagicalSyncAttribute.ShouldSkipSyncRegistrationId.GONetParticipant_IsRotationSyncd] =
                (monitoringSupport, index) => !monitoringSupport.syncCompanion.gonetParticipant.IsRotationSyncd;

            GONetAutoMagicalSyncAttribute.ShouldSkipSyncByRegistrationIdMap[(int)GONetAutoMagicalSyncAttribute.ShouldSkipSyncRegistrationId.GONetParticipant_IsPositionSyncd] =
                (monitoringSupport, index) => !monitoringSupport.syncCompanion.gonetParticipant.IsPositionSyncd;
        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            GONetLog.Error(string.Concat("Error Message: ", e.Exception.Message, "\nError Stacktrace:\n", e.Exception.StackTrace));
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception exception = (e.ExceptionObject as Exception);
            GONetLog.Error(string.Concat("Error Message: ", exception.Message, "\nError Stacktrace:\n", exception.StackTrace));
        }

        #region public methods

        #region instantiate special support

        /// <summary>
        /// <para>This is the option to instantiate/spawn something that uses one original/prefab/template for the authority/owner/originator and a different one for everyone else (i.e., non-authorities).</para>
        /// <para>This is useful in some cases for instantiating/spawning things like players where the authority (i.e., the player) has certain scripts attached and only a model/mesh with arms and legs and non-authorities get less scripts and the full model/mesh.</para>
        /// <para>Only the authority/owner/originator can call this method (i.e., the resulting instance's <see cref="GONetParticipant.OwnerAuthorityId"/> will be set to <see cref="MyAuthorityId"/>).</para>
        /// <para>It operates within GONet just like <see cref="UnityEngine.Object.Instantiate{T}(T)"/>, where there is automatic spawn propogation support to all other machines in this game/session on the network.</para>
        /// <para>However, the difference is using this method ensures the other non-owner (networked) parties automatically instantiate <paramref name="nonAuthorityAlternateOriginal"/> instead of <paramref name="authorityOriginal"/>, which will be instantiated here for the authority/owner.</para>
        /// <para>Therefore, if you simply want to instantiate something across the network and it should be the same original <see cref="UnityEngine.Object"/> template, then use <see cref=""/></para>
        /// </summary>
        /// <param name="authorityOriginal"></param>
        /// <param name="nonAuthorityAlternateOriginal"></param>
        /// <returns></returns>
        public static GONetParticipant Instantiate_WithNonAuthorityAlternate(GONetParticipant authorityOriginal, GONetParticipant nonAuthorityAlternateOriginal)
        {
            return GONetSpawnSupport_Runtime.Instantiate_WithNonAuthorityAlternate(authorityOriginal, nonAuthorityAlternateOriginal);
        }

        /// <summary>
        /// <para>This is the option to instantiate/spawn something that uses one original/prefab/template for the authority/owner/originator and a different one for everyone else (i.e., non-authorities).</para>
        /// <para>This is useful in some cases for instantiating/spawning things like players where the authority (i.e., the player) has certain scripts attached and only a model/mesh with arms and legs and non-authorities get less scripts and the full model/mesh.</para>
        /// <para>Only the authority/owner/originator can call this method (i.e., the resulting instance's <see cref="GONetParticipant.OwnerAuthorityId"/> will be set to <see cref="MyAuthorityId"/>).</para>
        /// <para>It operates within GONet just like <see cref="UnityEngine.Object.Instantiate{T}(T)"/>, where there is automatic spawn propogation support to all other machines in this game/session on the network.</para>
        /// <para>However, the difference is using this method ensures the other non-owner (networked) parties automatically instantiate <paramref name="nonAuthorityAlternateOriginal"/> instead of <paramref name="authorityOriginal"/>, which will be instantiated here for the authority/owner.</para>
        /// <para>Therefore, if you simply want to instantiate something across the network and it should be the same original <see cref="UnityEngine.Object"/> template, then use <see cref=""/></para>
        /// </summary>
        /// <param name="authorityOriginal"></param>
        /// <param name="nonAuthorityAlternateOriginal"></param>
        /// <returns></returns>
        public static GONetParticipant Instantiate_WithNonAuthorityAlternate(GONetParticipant authorityOriginal, GONetParticipant nonAuthorityAlternateOriginal, Vector3 position, Quaternion rotation)
        {
            return GONetSpawnSupport_Runtime.Instantiate_WithNonAuthorityAlternate(authorityOriginal, nonAuthorityAlternateOriginal, position, rotation);
        }

        #endregion

        /// <summary>
        /// This can be called from multiple threads....the final send will be done on yet another thread - <see cref="SendBytes_EndOfTheLine_AllSendsMUSTComeHere_SeparateThread"/>
        /// </summary>
        public static void SendBytesToRemoteConnections(byte[] bytes, int bytesUsedCount, GONetChannelId channelId)
        {
            SendBytesToRemoteConnection(null, bytes, bytesUsedCount, channelId); // passing null will result in sending to all remote connections
        }

        /// <summary>
        /// This can be called from multiple threads....the final send will be done on yet another thread - <see cref="SendBytes_EndOfTheLine_AllSendsMUSTComeHere_SeparateThread"/>
        /// </summary>
        private static void SendBytesToRemoteConnection(GONetConnection sendToConnection, byte[] bytes, int bytesUsedCount, GONetChannelId channelId)
        {
            ConcurrentQueue<NetworkData> readyToReturnQueue;
            if (readyToReturnQueue_ThreadMap.TryGetValue(Thread.CurrentThread, out readyToReturnQueue))
            {
                int processedCount = 0;
                int readyCount = readyToReturnQueue.Count;
                NetworkData readyToReturn;
                while (processedCount < readyCount && readyToReturnQueue.TryDequeue(out readyToReturn))
                {
                    readyToReturn.messageBytesBorrowedFromPool.Return(readyToReturn.messageBytes); // since we now know we are on the correct thread (i.e.., same as borrowed on) we can return it to pool
                    ++processedCount;
                }
            }
            else
            {
                readyToReturnQueue_ThreadMap[Thread.CurrentThread] = new ConcurrentQueue<NetworkData>();
            }

            ArrayPool<byte> endOfTheLineSendArrayPool;
            if (!netThread_outgoingNetworkDataArrayPool_ThreadMap.TryGetValue(Thread.CurrentThread, out endOfTheLineSendArrayPool))
            {
                endOfTheLineSendArrayPool = new ArrayPool<byte>(100, 10, 1024, 2048);
                netThread_outgoingNetworkDataArrayPool_ThreadMap[Thread.CurrentThread] = endOfTheLineSendArrayPool;
            }
                
            byte[] bytesCopy = endOfTheLineSendArrayPool.Borrow(bytesUsedCount);
            Buffer.BlockCopy(bytes, 0, bytesCopy, 0, bytesUsedCount);

            NetworkData networkData = new NetworkData()
            {
                messageBytesBorrowedFromPool = endOfTheLineSendArrayPool,
                messageBytesBorrowedOnThread = Thread.CurrentThread,
                messageBytes = bytesCopy,
                bytesUsedCount = bytesUsedCount,
                relatedConnection = sendToConnection,
                channelId = channelId
            };

            endOfTheLineSendQueue.Enqueue(networkData);
        }

        static readonly ConcurrentQueue<NetworkData> endOfTheLineSendQueue = new ConcurrentQueue<NetworkData>();
        static readonly ConcurrentDictionary<Thread, ArrayPool<byte>> netThread_outgoingNetworkDataArrayPool_ThreadMap = new ConcurrentDictionary<Thread, ArrayPool<byte>>();

        private static volatile bool isRunning_endOfTheLineSend_Thread;
        private static void SendBytes_EndOfTheLine_AllSendsMUSTComeHere_SeparateThread()
        {
            while (isRunning_endOfTheLineSend_Thread)
            {
                int processedCount = 0;
                int count = endOfTheLineSendQueue.Count;
                NetworkData networkData;
                while (processedCount < count && endOfTheLineSendQueue.TryDequeue(out networkData))
                {
                    if (networkData.relatedConnection == null)
                    {
                        if (IsServer)
                        {
                            if (gonetServer != null)
                            {
                                //GONetLog.Debug("sending something....my seconds: " + Time.ElapsedSeconds);
                                gonetServer.SendBytesToAllClients(networkData.messageBytes, networkData.bytesUsedCount, networkData.channelId);

                                //sqlite.Save(new SavedNetworkData(networkData, Time.ElapsedSeconds));
                            }
                        }
                        else
                        {
                            if (GONetClient != null)
                            {
                                while (!GONetClient.IsConnectedToServer)
                                {
                                    const string SLEEP = "SLEEP!  So I can send this stuff....not yet connected...that's why.";
                                    GONetLog.Info(SLEEP);

                                    Thread.Sleep(33); // TODO FIXME I am sure things will eventually get into strange states out in the wild where clients spotty network puts them here too often and I wonder if this is problematic...certainly quick/dirty and nieve!
                                }

                                //GONetLog.Debug("sending something....my seconds: " + Time.ElapsedSeconds);
                                GONetClient.SendBytesToServer(networkData.messageBytes, networkData.bytesUsedCount, networkData.channelId);
                            }
                        }
                    }
                    else
                    {
                        networkData.relatedConnection.SendMessageOverChannel(networkData.messageBytes, networkData.bytesUsedCount, networkData.channelId);

                        //sqlite.Save(new SavedNetworkData(networkData, Time.ElapsedSeconds, networkData.relatedConnection.OwnerAuthorityId));
                    }

                    { // set things up so the byte[] on networkData can be returned to the proper pool AND on the proper thread on which is was initially borrowed!
                        ConcurrentQueue<NetworkData> readyToReturnQueue = readyToReturnQueue_ThreadMap[networkData.messageBytesBorrowedOnThread];
                        readyToReturnQueue.Enqueue(networkData);
                    }
                }
            }
        }

        #endregion

        #region internal methods

        static readonly SyncBundleUniqueGrouping grouping_endOfFrame_reliable = new SyncBundleUniqueGrouping(AutoMagicalSyncFrequencies.END_OF_FRAME_IN_WHICH_CHANGE_OCCURS_SECONDS, AutoMagicalSyncReliability.Reliable, false);
        static readonly SyncBundleUniqueGrouping grouping_endOfFrame_unreliable = new SyncBundleUniqueGrouping(AutoMagicalSyncFrequencies.END_OF_FRAME_IN_WHICH_CHANGE_OCCURS_SECONDS, AutoMagicalSyncReliability.Unreliable, false);

        static Thread endOfLineSendThread;

        /// <summary>
        /// Should only be called from <see cref="GONetGlobal"/>
        /// </summary>
        internal static void Update()
        {
            Time.Update();

            ProcessIncomingBytes_QueuedNetworkData_MainThread();

            AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable itemsToProcessEveryFrame;
            if (autoSyncProcessingSupportByFrequencyMap.TryGetValue(grouping_endOfFrame_reliable, out itemsToProcessEveryFrame))
            {
                itemsToProcessEveryFrame.ProcessASAP(); // this one requires manual initiation of processing
            }
            if (autoSyncProcessingSupportByFrequencyMap.TryGetValue(grouping_endOfFrame_unreliable, out itemsToProcessEveryFrame))
            {
                itemsToProcessEveryFrame.ProcessASAP(); // this one requires manual initiation of processing
            }

            int mainThreadSupportCount = autoSyncProcessingSupports_UnityMainThread.Count;
            for (int i = 0; i < mainThreadSupportCount; ++i)
            {
                AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable autoSyncProcessingSupport_mainThread = autoSyncProcessingSupports_UnityMainThread[i];
                autoSyncProcessingSupport_mainThread.ProcessASAP();
            }

            var aEnumerator = activeAutoSyncCompanionsByCodeGenerationIdMap.GetEnumerator(); // TODO use better name!
            while (aEnumerator.MoveNext())
            {
                var a = aEnumerator.Current; // TODO use better name!

                var bEnumerator = a.Value.GetEnumerator(); // TODO use better name!
                while (bEnumerator.MoveNext())
                {
                    var b = bEnumerator.Current; // TODO use better name!

                    int xLength = b.Value.valuesChangesSupport.Length; // TODO use better name!
                    for (int i = 0; i < xLength; ++i)
                    {
                        var x = b.Value.valuesChangesSupport[i]; // TODO use better name!
                        if (x != null)
                        {
                            x.ApplyValueBlending_IfAppropriate();
                        }
                    }
                }
            }

            if (endOfLineSendThread == null)
            {
                isRunning_endOfTheLineSend_Thread = true;
                endOfLineSendThread = new Thread(SendBytes_EndOfTheLine_AllSendsMUSTComeHere_SeparateThread);
                endOfLineSendThread.Start();
            }

            if (IsServer)
            {
                gonetServer?.Update();
            }

            if (IsClient)
            {
                Client_SyncTimeWithServer_Initiate_IfAppropriate();
                GONetClient?.Update();
            }
        }

        #region time sync client-server-client

        /// <summary>
        /// How close the clients time must be to the server before the gap is considered closed and time can go
        /// from being sync'd every <see cref="CLIENT_SYNC_TIME_EVERY_TICKS__UNTIL_GAP_CLOSED"/> ticks 
        /// to every <see cref="CLIENT_SYNC_TIME_EVERY_TICKS__POST_GAP_CLOSED"/> ticks for maintenance.
        /// </summary>
        static readonly long CLIENT_SYNC_TIME_GAP_TICKS = TimeSpan.FromSeconds(1f / 60f).Ticks;
        static bool client_hasClosedTimeSyncGapWithServer;
        static readonly long CLIENT_SYNC_TIME_EVERY_TICKS__UNTIL_GAP_CLOSED = TimeSpan.FromSeconds(1f / 4f).Ticks;
        static readonly long CLIENT_SYNC_TIME_EVERY_TICKS__POST_GAP_CLOSED = TimeSpan.FromSeconds(30f).Ticks;
        static readonly float CLIENT_SYNC_TIME_EVERY_TICKS_FLOAT__UNTIL_GAP_CLOSED = (float)CLIENT_SYNC_TIME_EVERY_TICKS__UNTIL_GAP_CLOSED;
        static readonly float CLIENT_SYNC_TIME_EVERY_TICKS_FLOAT__POST_GAP_CLOSED = (float)CLIENT_SYNC_TIME_EVERY_TICKS__POST_GAP_CLOSED;
        static readonly long DIFF_TICKS_TOO_BIG_FOR_EASING = TimeSpan.FromSeconds(1f).Ticks; // if you are over a second out of sync...do not ease as that will take forever
        static bool client_hasSentSyncTimeRequest;
        static DateTime client_lastSyncTimeRequestSent;
        const int CLIENT_TIME_SYNCS_SENT_HISTORY_SIZE = 60;
        static readonly Dictionary<long, RequestMessage> client_lastFewTimeSyncsSentByUID = new Dictionary<long, RequestMessage>(CLIENT_TIME_SYNCS_SENT_HISTORY_SIZE);
        static long client_mostRecentTimeSyncResponseSentTicks;

        internal static readonly float BLENDING_BUFFER_LEAD_SECONDS_DEFAULT = 0.25f; // 0 is to always extrapolate pretty much.....here is a decent delay to get good interpolation: 0.25f
        internal static float valueBlendingBufferLeadSeconds = BLENDING_BUFFER_LEAD_SECONDS_DEFAULT;
        internal static long valueBlendingBufferLeadTicks = TimeSpan.FromSeconds(BLENDING_BUFFER_LEAD_SECONDS_DEFAULT).Ticks;

        /// <summary>
        /// 0 is to always extrapolate pretty much.....here is a decent delay to get good interpolation: TimeSpan.FromMilliseconds(250).Ticks;
        /// </summary>
        internal static void SetValueBlendingBufferLeadTimeFromMilliseconds(int valueBlendingBufferLeadTimeMilliseconds)
        {
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(valueBlendingBufferLeadTimeMilliseconds);
            valueBlendingBufferLeadSeconds = (float)timeSpan.TotalSeconds;
            valueBlendingBufferLeadTicks = timeSpan.Ticks;
        }

        /// <summary>
        /// "IfAppropriate" is to indicate this runs on a schedule....if it is not the right time, this will do nothing.
        /// </summary>
        private static void Client_SyncTimeWithServer_Initiate_IfAppropriate()
        {
            DateTime now = DateTime.UtcNow;
            long syncEveryTicks = client_hasClosedTimeSyncGapWithServer ? CLIENT_SYNC_TIME_EVERY_TICKS__POST_GAP_CLOSED : CLIENT_SYNC_TIME_EVERY_TICKS__UNTIL_GAP_CLOSED;
            bool isAppropriate = !client_hasSentSyncTimeRequest || (now - client_lastSyncTimeRequestSent).Duration().Ticks > syncEveryTicks;
            if (isAppropriate)
            {
                client_hasSentSyncTimeRequest = true;
                client_lastSyncTimeRequestSent = now;

                { // the actual sync request:
                    RequestMessage timeSync = new RequestMessage(Time.ElapsedTicks);

                    client_lastFewTimeSyncsSentByUID[timeSync.UID] = timeSync;
                    if (client_lastFewTimeSyncsSentByUID.Count > CLIENT_TIME_SYNCS_SENT_HISTORY_SIZE)
                    {
                        // TODO do not let client_lastFewTimeSyncsSentByUID get larger than TIME_SYNCS_SENT_QUEUE_SIZE.....delete oldest
                    }

                    using (BitByBitByteArrayBuilder bitStream = BitByBitByteArrayBuilder.GetBuilder())
                    {
                        { // header...just message type/id...well, and now time 
                            uint messageID = messageTypeToMessageIDMap[typeof(RequestMessage)];
                            bitStream.WriteUInt(messageID);

                            bitStream.WriteLong(timeSync.OccurredAtElapsedTicks);
                        }

                        // body
                        bitStream.WriteLong(timeSync.UID);

                        bitStream.WriteCurrentPartialByte();

                        int bytesUsedCount = bitStream.Length_WrittenBytes;
                        byte[] bytes = mainThread_miscSerializationArrayPool.Borrow(bytesUsedCount);
                        Array.Copy(bitStream.GetBuffer(), 0, bytes, 0, bytesUsedCount);

                        SendBytesToRemoteConnections(bytes, bytesUsedCount, GONetChannel.TimeSync_Unreliable);

                        //GONetLog.Debug("just sent time sync to server....my time (seconds): " + TimeSpan.FromTicks(timeSync.OccurredAtElapsedTicks).TotalSeconds);

                        mainThread_miscSerializationArrayPool.Return(bytes);
                    }
                }
            }
        }

        private static void Server_SyncTimeWithClient_Respond(long requestUID, GONetConnection connectionToClient)
        {
            using (BitByBitByteArrayBuilder bitStream = BitByBitByteArrayBuilder.GetBuilder())
            {
                { // header...just message type/id...well, and now time 
                    uint messageID = messageTypeToMessageIDMap[typeof(ResponseMessage)];
                    bitStream.WriteUInt(messageID);

                    bitStream.WriteLong(Time.ElapsedTicks);
                }

                // body
                bitStream.WriteLong(requestUID);

                bitStream.WriteCurrentPartialByte();

                int bytesUsedCount = bitStream.Length_WrittenBytes;
                byte[] bytes = mainThread_miscSerializationArrayPool.Borrow(bytesUsedCount);
                Array.Copy(bitStream.GetBuffer(), 0, bytes, 0, bytesUsedCount);

                //GONetLog.Debug("about to send time sync to client....");

                SendBytesToRemoteConnection(connectionToClient, bytes, bytesUsedCount, GONetChannel.TimeSync_Unreliable);

                mainThread_miscSerializationArrayPool.Return(bytes);
            }
        }

        private static void Client_SyncTimeWithServer_ProcessResponse(long requestUID, long server_elapsedTicksAtSendResponse)
        {
            RequestMessage requestMessage;
            if (client_lastFewTimeSyncsSentByUID.TryGetValue(requestUID, out requestMessage)) // if we cannot find it, we cannot really do anything....now can we?
            {
                if (server_elapsedTicksAtSendResponse > client_mostRecentTimeSyncResponseSentTicks) // only process the latest send from server, ignore older stuff
                {
                    client_mostRecentTimeSyncResponseSentTicks = server_elapsedTicksAtSendResponse;

                    long responseReceivedTicks_Client = Time.ElapsedTicks;
                    long requestSentTicks_Client = requestMessage.OccurredAtElapsedTicks;
                    long rtt_ticks = responseReceivedTicks_Client - requestSentTicks_Client;

                    GONetClient.connectionToServer.RTT_Latest = (float)TimeSpan.FromTicks(rtt_ticks).TotalSeconds;
                    //GONetLog.Debug("RTT_Latest: " + gonetClient.connectionToServer.RTT_Latest + " RTT_RecentAverage: " + gonetClient.connectionToServer.RTT_RecentAverage + " their.rtt: " + gonetClient.connectionToServer.RTT);

                    long assumedNetworkDelayTicks = rtt_ticks >> 1; // divide by 2
                    long newClientTimeTicks = server_elapsedTicksAtSendResponse + assumedNetworkDelayTicks;

                    Time.SetFromAuthority(newClientTimeTicks);

                    if (!client_hasClosedTimeSyncGapWithServer)
                    {
                        long diffTicksABS = Math.Abs(responseReceivedTicks_Client - newClientTimeTicks);
                        if (diffTicksABS < CLIENT_SYNC_TIME_GAP_TICKS)
                        {
                            client_hasClosedTimeSyncGapWithServer = true;
                        }
                    }

                }
                else { GONetLog.Warning("throwing away older time from server"); }
            }
        }

        #endregion

        /// <summary>
        /// Should only be called from <see cref="GONetGlobal"/>
        /// </summary>
        internal static void Shutdown()
        {
            isRunning_endOfTheLineSend_Thread = false;

            if (IsServer)
            {
                gonetServer?.Stop();
            }
            else
            {
                GONetClient?.Disconnect();
            }

            var enumeratorThread = autoSyncProcessingSupportByFrequencyMap.GetEnumerator();
            while (enumeratorThread.MoveNext())
            {
                enumeratorThread.Current.Value.Dispose();
            }
        }

        internal struct NetworkData
        {
            public GONetConnection relatedConnection;
            public ArrayPool<byte> messageBytesBorrowedFromPool;
            public Thread messageBytesBorrowedOnThread;
            public byte[] messageBytes;
            public int bytesUsedCount;
            public GONetChannelId channelId;
        }

        [MessagePackObject]
        public class SavedNetworkData : IDatabaseRow
        {
            [Key(0)]
            public long UID { get; set; }

            [Key(1)]
            public byte[] Bytes { get; set; }

            [Key(2)]
            [FilterableBy]
            public int BytesUsedCount { get; set; }

            [Key(3)]
            [FilterableBy(true)]
            public GONetChannelId ChannelId { get; set; }

            [Key(4)]
            [FilterableBy(true)]
            public double ElapasedSeconds { get; set; }

            [Key(5)]
            [FilterableBy(true)]
            public uint SentToConnection_OwnerAuthorityId { get; set; }

            public SavedNetworkData() { }

            internal SavedNetworkData(NetworkData source, double elapsedSeconds, uint sentToConnection_OwnerAuthorityId = GONetMain.OwnerAuthorityId_Unset)
            {
                Bytes = source.messageBytes;
                BytesUsedCount = source.bytesUsedCount;
                ChannelId = source.channelId;
                ElapasedSeconds = elapsedSeconds;
                SentToConnection_OwnerAuthorityId = sentToConnection_OwnerAuthorityId;
            }
        }

        static readonly ConcurrentDictionary<Thread, ArrayPool<byte>> netThread_incomingNetworkDataArrayPool_ThreadMap = new ConcurrentDictionary<Thread, ArrayPool<byte>>();
        static readonly ConcurrentQueue<NetworkData> incomingNetworkData = new ConcurrentQueue<NetworkData>();
        static readonly ConcurrentDictionary<Thread, ConcurrentQueue<NetworkData>> readyToReturnQueue_ThreadMap = new ConcurrentDictionary<Thread, ConcurrentQueue<NetworkData>>();

        /// <summary>
        /// Lengthy and nerdy way of naming this class "Time" just like <see cref="UnityEngine.Time"/> to avoid conflicts in a memorable way!
        /// </summary>
        public sealed class SecretaryOfTemporalAffairs
        {
            public delegate void TimeChangeArgs(double fromElapsedSeconds, double toElapsedSeconds, long fromElapsedTicks, long toElapsedTicks);
            public event TimeChangeArgs TimeSetFromAuthority;

            long baselineTicks;
            long lastSetFromAuthorityDiffTicks;
            long lastSetFromAuthorityAtTicks;

            public const double ElapsedSecondsUnset = -1;

            public long ElapsedTicks { get; private set; }
            double elapsedSeconds = ElapsedSecondsUnset;
            public double ElapsedSeconds => elapsedSeconds;

            internal volatile int updateCount = 0;
            public int UpdateCount
            {
                get { return updateCount; }
                private set { updateCount = value; }
            }

            internal void SetFromAuthority(long elapsedTicksFromAuthority)
            {
                lastSetFromAuthorityDiffTicks = elapsedTicksFromAuthority - ElapsedTicks;

                long elapsedTicksBefore = ElapsedTicks;
                double elapsedSecondsBefore = elapsedSeconds;

                lastSetFromAuthorityAtTicks = HighResolutionTimeUtils.Now.Ticks;
                baselineTicks = lastSetFromAuthorityAtTicks - elapsedTicksFromAuthority;
                ElapsedTicks = lastSetFromAuthorityAtTicks - baselineTicks;
                elapsedSeconds = TimeSpan.FromTicks(ElapsedTicks).TotalSeconds;

                /* if you want debugging in log
                const string STR_ElapsedTimeClient = "ElapsedTime client of: ";
                const string STR_BeingOverwritten = " is being overwritten from authority source to: ";
                const string STR_DoubleCheck = " seconds and here is new client value to double check it worked correctly: ";
                const string STR_Diff = " lastSetFromAuthorityDiffTicks (well, as ms): ";
                GONetLog.Info(string.Concat(STR_ElapsedTimeClient, elapsedSecondsBefore, STR_BeingOverwritten, TimeSpan.FromTicks(elapsedTicksFromAuthority).TotalSeconds, STR_DoubleCheck, elapsedSeconds, STR_Diff, TimeSpan.FromTicks(lastSetFromAuthorityDiffTicks).TotalMilliseconds));
                */

                TimeSetFromAuthority?.Invoke(elapsedSecondsBefore, elapsedSeconds, elapsedTicksBefore, ElapsedTicks);
            }

            /// <summary>
            /// IMPORTANT: Call this every engine tick.
            /// See <see cref="UpdateCount"/> to know how many times this has been called this session.
            /// </summary>
            internal void Update()
            {
                ++UpdateCount;

                if (elapsedSeconds == ElapsedSecondsUnset)
                {
                    baselineTicks = HighResolutionTimeUtils.Now.Ticks;
                }

                long elapsedTicks_withoutEasement = HighResolutionTimeUtils.Now.Ticks - baselineTicks;
                ElapsedTicks = elapsedTicks_withoutEasement - GetTicksToSubtractForSetFromAuthorityEasing();

                elapsedSeconds = TimeSpan.FromTicks(ElapsedTicks).TotalSeconds;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private long GetTicksToSubtractForSetFromAuthorityEasing()
            {
                bool shouldNotEase = lastSetFromAuthorityDiffTicks > DIFF_TICKS_TOO_BIG_FOR_EASING;
                if (!shouldNotEase && lastSetFromAuthorityDiffTicks != 0)
                { // IMPORTANT: This code eases the adjustment (i.e., diff) back to resync time over the entire period between resyncs to avoid a possibly dramatic jump in time just after a resync!
                    long ticksSinceLastSetFromAuthority = HighResolutionTimeUtils.Now.Ticks - lastSetFromAuthorityAtTicks;
                    float syncEveryTicks = client_hasClosedTimeSyncGapWithServer ? CLIENT_SYNC_TIME_EVERY_TICKS_FLOAT__POST_GAP_CLOSED : CLIENT_SYNC_TIME_EVERY_TICKS_FLOAT__UNTIL_GAP_CLOSED;
                    float inverseLerpBetweenSyncs = ticksSinceLastSetFromAuthority / syncEveryTicks;
                    if (inverseLerpBetweenSyncs < 1f) // if 1 or greater there will be nothing to add based on calculations
                    {
                        return (long)(lastSetFromAuthorityDiffTicks * (1f - inverseLerpBetweenSyncs));
                    }
                }

                return 0;
            }
        }

        /// <summary>
        /// All incoming network bytes need to come here.
        /// IMPORTANT: the thread on which this processes may likely NOT be the main Unity thread.
        /// </summary>
        internal static void ProcessIncomingBytes(GONetConnection sourceConnection, byte[] messageBytes, int bytesUsedCount, GONetChannelId channelId)
        {
            //GONetLog.Debug("received something....");

            ArrayPool<byte> pool;
            if (!netThread_incomingNetworkDataArrayPool_ThreadMap.TryGetValue(Thread.CurrentThread, out pool))
            {
                pool = new ArrayPool<byte>(1000, 10, 1024, 2048);
                netThread_incomingNetworkDataArrayPool_ThreadMap[Thread.CurrentThread] = pool;
            }

            ConcurrentQueue<NetworkData> readyToReturnQueue;
            if (readyToReturnQueue_ThreadMap.TryGetValue(Thread.CurrentThread, out readyToReturnQueue))
            {
                int processedCount = 0;
                int readyCount = readyToReturnQueue.Count;
                NetworkData readyToReturn;
                while (processedCount < readyCount && readyToReturnQueue.TryDequeue(out readyToReturn))
                {
                    readyToReturn.messageBytesBorrowedFromPool.Return(readyToReturn.messageBytes); // since we now know we are on the correct thread (i.e.., same as borrowed on) we can return it to pool
                    ++processedCount;
                }
            }
            else
            {
                readyToReturnQueue_ThreadMap[Thread.CurrentThread] = new ConcurrentQueue<NetworkData>();
            }

            NetworkData networkData = new NetworkData()
            {
                relatedConnection = sourceConnection,
                messageBytes = pool.Borrow(bytesUsedCount),
                messageBytesBorrowedFromPool = pool,
                messageBytesBorrowedOnThread = Thread.CurrentThread,
                bytesUsedCount = bytesUsedCount,
                channelId = channelId
            };

            Buffer.BlockCopy(messageBytes, 0, networkData.messageBytes, 0, bytesUsedCount);

            incomingNetworkData.Enqueue(networkData);
        }

        #endregion

        #region private methods

        /// <summary>
        /// Call this from the main Unity thread!
        /// </summary>
        private static void ProcessIncomingBytes_QueuedNetworkData_MainThread()
        {
            NetworkData networkData;
            int count = incomingNetworkData.Count;
            for (int i = 0; i < count && !incomingNetworkData.IsEmpty; ++i)
            {
                if (incomingNetworkData.TryDequeue(out networkData))
                {
                    try
                    {
                        if (networkData.channelId == GONetChannel.EventSingles_Reliable || networkData.channelId == GONetChannel.EventSingles_Unreliable)
                        {
                            DeserializeBody_EventSingle(networkData.messageBytes, networkData.relatedConnection);
                        }
                        else
                        {
                            using (var bitStream = BitByBitByteArrayBuilder.GetBuilder_WithNewData(networkData.messageBytes, networkData.bytesUsedCount))
                            {
                                Type messageType;
                                ////////////////////////////////////////////////////////////////////////////
                                // header...just message type/id...well, now it is send time too
                                uint messageID;
                                bitStream.ReadUInt(out messageID);
                                messageType = messageTypeByMessageIDMap[messageID];

                                long elapsedTicksAtSend;
                                bitStream.ReadLong(out elapsedTicksAtSend);
                                ////////////////////////////////////////////////////////////////////////////

                                //GONetLog.Debug("received something....my seconds: " + Time.ElapsedSeconds);

                                // TODO need to get in generic event/message serialization in .... AND add persistent events into persistentEventsThisSession

                                {  // body:
                                    if (messageType == typeof(AutoMagicalSync_ValueChanges_Message))
                                    {
                                        DeserializeBody_ChangesBundle(bitStream, networkData.relatedConnection, elapsedTicksAtSend);
                                    }
                                    else if (messageType == typeof(RequestMessage))
                                    {
                                        long requestUID;
                                        bitStream.ReadLong(out requestUID);

                                        Server_SyncTimeWithClient_Respond(requestUID, networkData.relatedConnection);
                                    }
                                    else if (messageType == typeof(ResponseMessage))
                                    {
                                        long requestUID;
                                        bitStream.ReadLong(out requestUID);

                                        Client_SyncTimeWithServer_ProcessResponse(requestUID, elapsedTicksAtSend);
                                    }
                                    else if (messageType == typeof(OwnerAuthorityIdAssignmentEvent)) // this should be the first message ever received....but since only sent once per client, do not put it first in the if statements list of message type check
                                    {
                                        uint ownerAuthorityId;
                                        bitStream.ReadUInt(out ownerAuthorityId);

                                        if (!IsServer) // this only applied to clients....should NEVER happen on server
                                        {
                                            MyAuthorityId = ownerAuthorityId;
                                        } // else log warning?
                                    }
                                    else if (messageType == typeof(AutoMagicalSync_AllCurrentValues_Message))
                                    {
                                        DeserializeBody_AllValuesBundle(bitStream, networkData.bytesUsedCount, networkData.relatedConnection, elapsedTicksAtSend);
                                    } // else?  TODO lookup proper deserialize method instead of if-else-if statement(s)
                                }
                            }
                        }

                        // TODO this should only deserialize the message....and then send over to an EventBus where subscribers to that event/message from the bus can process accordingly
                    }
                    catch (Exception e)
                    {
                        GONetLog.Error(string.Concat("Error Message: ", e.Message, "\nError Stacktrace:\n", e.StackTrace));
                    }
                    finally
                    {
                        { // set things up so the byte[] on networkData can be returned to the proper pool AND on the proper thread on which is was initially borrowed!
                            ConcurrentQueue<NetworkData> readyToReturnQueue = readyToReturnQueue_ThreadMap[networkData.messageBytesBorrowedOnThread];
                            readyToReturnQueue.Enqueue(networkData);
                        }
                    }
                }
                else
                {
                    GONetLog.Warning("Trying to dequeue from queued up incoming network data elements and cannot....WHY?");
                }
            }
        }

        private static void DeserializeBody_EventSingle(byte[] messageBytes, GONetConnection relatedConnection)
        {
            IGONetEvent @event = SerializationUtils.DeserializeFromBytes<IGONetEvent>(messageBytes);
            EventBus.Publish(@event, relatedConnection.OwnerAuthorityId);
        }

        /// <summary>
        /// Process instantiation event from remote source.
        /// </summary>
        /// <param name="instantiateEvent"></param>
        private static void Instantiate_Remote(InstantiateGONetParticipantEvent instantiateEvent)
        {
            GONetParticipant template = GONetSpawnSupport_Runtime.LookupTemplateFromDesignTimeLocation(instantiateEvent.DesignTimeLocation);
            GONetParticipant instance = UnityEngine.Object.Instantiate(template);

            if (!string.IsNullOrWhiteSpace(instantiateEvent.InstanceName))
            {
                instance.gameObject.name = instantiateEvent.InstanceName;
            }

            GONetLog.Debug("Instantiate_Remote, Instantiate complete....instanceID: " + instance.GetInstanceID());
            instance.OwnerAuthorityId = instantiateEvent.OwnerAuthorityId;
            if (!IsServer || instantiateEvent.GONetId != GONetParticipant.GONetId_Unset)
            {
                instance.GONetId = instantiateEvent.GONetId; // TODO when/if replay support is added, this might overwrite what will automatically be done in OnEnable_AssignGONetId_IfAppropriate...maybe that one should be prevented..going to comment there now too
            }
            remoteSpawns_avoidAutoPropogateSupport.Add(instance);
        }

        private static void Server_OnClientConnected_SendClientCurrentState(GONetConnection_ServerToClient gonetConnection_ServerToClient)
        {
            Server_AssignNewClientAuthorityId(gonetConnection_ServerToClient);
            Server_SendClientPersistentEventsSinceStart(gonetConnection_ServerToClient);
            Server_SendClientCurrentState_AllAutoMagicalSync(gonetConnection_ServerToClient);
        }

        private static void Server_SendClientPersistentEventsSinceStart(GONetConnection_ServerToClient gonetConnection_ServerToClient)
        {
            if (persistentEventsThisSession.Count > 0)
            {
                PersistentEvents_Bundle bundle = new PersistentEvents_Bundle(Time.ElapsedTicks, persistentEventsThisSession);
                byte[] bytes = SerializationUtils.SerializeToBytes<IGONetEvent>(bundle); // EXTREMELY important to include the <IGONetEvent> because there are multiple options for MessagePack to serialize this thing based on BobWad_Generated.cs' usage of [MessagePack.Union] for relevant interfaces this concrete class implements and the other end's call to deserialize will be to DeserializeBody_EventSingle and <IGONetEvent> will be used there too!!!
                SendBytesToRemoteConnection(gonetConnection_ServerToClient, bytes, bytes.Length, GONetChannel.EventSingles_Reliable);
            }
        }

        private static void Server_AssignNewClientAuthorityId(GONetConnection_ServerToClient gonetConnection_ServerToClient)
        {
            // first assign locally
            gonetConnection_ServerToClient.OwnerAuthorityId = ++server_lastAssignedAuthorityId;

            // then send the assignment to the client
            using (BitByBitByteArrayBuilder bitStream = BitByBitByteArrayBuilder.GetBuilder())
            {
                { // header...just message type/id...well, and now time 
                    uint messageID = messageTypeToMessageIDMap[typeof(OwnerAuthorityIdAssignmentEvent)];
                    bitStream.WriteUInt(messageID);

                    bitStream.WriteLong(Time.ElapsedTicks);
                }

                { // body
                    bitStream.WriteUInt(gonetConnection_ServerToClient.OwnerAuthorityId);
                }

                bitStream.WriteCurrentPartialByte();

                SendBytesToRemoteConnection(gonetConnection_ServerToClient, bitStream.GetBuffer(), bitStream.Length_WrittenBytes, GONetChannel.CustomSerialization_Reliable);
            }
        }

        #endregion

        #region what once was GONetAutoMagicalSyncManager

        static uint lastAssignedGONetId = GONetParticipant.GONetId_Unset;
        /// <summary>
        /// For every runtime instance of <see cref="GONetParticipant"/>, there will be one and only one item in one and only one of the <see cref="activeAutoSyncCompanionsByCodeGenerationIdMap"/>'s <see cref="Dictionary{TKey, TValue}.Values"/>.
        /// The key into this is the <see cref="GONetParticipant.codeGenerationId"/>.
        /// </summary>
        static readonly Dictionary<GONetCodeGenerationId, Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated>> activeAutoSyncCompanionsByCodeGenerationIdMap = 
            new Dictionary<GONetCodeGenerationId, Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated>>(byte.MaxValue);

        static readonly Dictionary<SyncBundleUniqueGrouping, AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable> autoSyncProcessingSupportByFrequencyMap = 
            new Dictionary<SyncBundleUniqueGrouping, AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable>(5);

        static readonly List<AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable> autoSyncProcessingSupports_UnityMainThread =
            new List<AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable>(5);

        internal class AutoMagicalSync_ValueMonitoringSupport_ChangedValue
        {
            internal byte index;
            internal GONetParticipant_AutoMagicalSyncCompanion_Generated syncCompanion;

            #region properties copied off of GONetAutoMagicalSyncAttribute
            /// <summary>
            /// Matches with <see cref="GONetAutoMagicalSyncAttribute.MustRunOnUnityMainThread"/>
            /// </summary>
            internal bool syncAttribute_MustRunOnUnityMainThread;
            /// <summary>
            /// Matches with <see cref="GONetAutoMagicalSyncAttribute.ProcessingPriority"/>
            /// </summary>
            internal int syncAttribute_ProcessingPriority;
            /// <summary>
            /// Matches with <see cref="GONetAutoMagicalSyncAttribute.ProcessingPriority_GONetInternalOverride"/>
            /// </summary>
            internal int syncAttribute_ProcessingPriority_GONetInternalOverride;
            /// <summary>
            /// Matches with <see cref="GONetAutoMagicalSyncAttribute.SyncChangesEverySeconds"/>
            /// </summary>
            internal float syncAttribute_SyncChangesEverySeconds;
            /// <summary>
            /// Matches with <see cref="GONetAutoMagicalSyncAttribute.Reliability"/>
            /// </summary>
            internal AutoMagicalSyncReliability syncAttribute_Reliability;
            /// <summary>
            /// Matches with <see cref="GONetAutoMagicalSyncAttribute.ShouldBlendBetweenValuesReceived"/>
            /// </summary>
            internal bool syncAttribute_ShouldBlendBetweenValuesReceived;
            /// <summary>
            /// Matches with <see cref="GONetAutoMagicalSyncAttribute.ShouldSkipSync"/>
            /// </summary>
            internal Func<GONetMain.AutoMagicalSync_ValueMonitoringSupport_ChangedValue, int, bool> syncAttribute_ShouldSkipSync;
            /// <summary>
            /// Matches/corresponds with/to each of the following members:
            ///     <see cref="GONetAutoMagicalSyncAttribute.QuantizeDownToBitCount"/>
            ///     <see cref="GONetAutoMagicalSyncAttribute.QuantizeLowerBound"/>
            ///     <see cref="GONetAutoMagicalSyncAttribute.QuantizeUpperBound"/>
            /// </summary>
            internal QuantizerSettingsGroup syncAttribute_QuantizerSettingsGroup;
            #endregion

            internal object lastKnownValue;
            internal object lastKnownValue_previous;

            [StructLayout(LayoutKind.Explicit)]
            internal struct NumericValueChangeSnapshot
            {
                [FieldOffset(0)]
                internal long elapsedTicksAtChange;

                [FieldOffset(8)]
                internal GONetBlendyValueType valueType;

                [FieldOffset(9)]
                internal float value_float;
                [FieldOffset(9)]
                internal Quaternion value_Quaternion;
                [FieldOffset(9)]
                internal Vector3 value_Vector3;

                internal static NumericValueChangeSnapshot Create(long elapsedTicksAtChange, object value)
                {
                    Type type = value.GetType();
                    if (type == typeof(float))
                    {
                        return new NumericValueChangeSnapshot(elapsedTicksAtChange, (float)value);
                    }

                    if (type == typeof(Quaternion))
                    {
                        return new NumericValueChangeSnapshot(elapsedTicksAtChange, (Quaternion)value);
                    }

                    if (type == typeof(Vector3))
                    {
                        return new NumericValueChangeSnapshot(elapsedTicksAtChange, (Vector3)value);
                    }

                    throw new ArgumentException("Type not supported.", nameof(value));
                }

                internal NumericValueChangeSnapshot(long elapsedTicksAtChange, float value) : this()
                {
                    this.elapsedTicksAtChange = elapsedTicksAtChange;
                    valueType = GONetBlendyValueType.Float;
                    value_float = value;
                }

                internal NumericValueChangeSnapshot(long elapsedTicksAtChange, Quaternion value) : this()
                {
                    this.elapsedTicksAtChange = elapsedTicksAtChange;
                    valueType = GONetBlendyValueType.Quaternion;
                    value_Quaternion = value;
                }

                internal NumericValueChangeSnapshot(long elapsedTicksAtChange, Vector3 value) : this()
                {
                    this.elapsedTicksAtChange = elapsedTicksAtChange;
                    valueType = GONetBlendyValueType.Vector3;
                    value_Vector3 = value;
                }

                public override bool Equals(object obj)
                {
                    if (!(obj is NumericValueChangeSnapshot))
                    {
                        return false;
                    }

                    var snapshot = (NumericValueChangeSnapshot)obj;
                    return elapsedTicksAtChange == snapshot.elapsedTicksAtChange &&
                           valueType == snapshot.valueType &&
                           value_float == snapshot.value_float &&
                           value_Quaternion.Equals(snapshot.value_Quaternion) &&
                           value_Vector3.Equals(snapshot.value_Vector3);
                }

                public override int GetHashCode()
                {
                    var hashCode = -1592683669;
                    hashCode = hashCode * -1521134295 + elapsedTicksAtChange.GetHashCode();
                    hashCode = hashCode * -1521134295 + valueType.GetHashCode();
                    hashCode = hashCode * -1521134295 + value_float.GetHashCode();
                    hashCode = hashCode * -1521134295 + EqualityComparer<Quaternion>.Default.GetHashCode(value_Quaternion);
                    hashCode = hashCode * -1521134295 + EqualityComparer<Vector3>.Default.GetHashCode(value_Vector3);
                    return hashCode;
                }
            }

            [Flags]
            internal enum GONetBlendyValueType : byte
            {
                Float =         1 << 0,
                Quaternion =    1 << 1,
                Vector3 =       1 << 2,
            }

            internal const int MOST_RECENT_CHANGEs_SIZE_MINIMUM = 10;
            internal const int MOST_RECENT_CHANGEs_SIZE_MAX_EXPECTED = 100;
            internal static readonly ArrayPool<NumericValueChangeSnapshot> mostRecentChangesPool = new ArrayPool<NumericValueChangeSnapshot>(1000, 50, MOST_RECENT_CHANGEs_SIZE_MINIMUM, MOST_RECENT_CHANGEs_SIZE_MAX_EXPECTED);
            internal static readonly long AUTO_STOP_PROCESSING_BLENDING_IF_INACTIVE_FOR_TICKS = TimeSpan.FromSeconds(2).Ticks;

            /// <summary>
            /// This will be null when <see cref="syncAttribute_ShouldBlendBetweenValuesReceived"/> is false AND/OR if the value type is NOT numeric (although, the latter will be identified early on in either generation or runtime and cause an exception to essentially disallow that!).
            /// IMPORTANT: This is always sorted in most recent with lowest index to oldest with highest index order.
            /// </summary>
            internal NumericValueChangeSnapshot[] mostRecentChanges;
            internal int mostRecentChanges_capacitySize;
            internal int mostRecentChanges_usedSize = 0;
            private uint mostRecentChanges_UpdatedByAuthorityId;

            /// <summary>
            /// DO NOT USE THIS.
            /// Public default constructor is required for object pool instantiation under current impl of <see cref="ObjectPool{T}"/>;
            /// </summary>
            public AutoMagicalSync_ValueMonitoringSupport_ChangedValue() { }

            /// <summary>
            /// This is called in generated code (i.e., sub-classes of <see cref="GONetParticipant_AutoMagicalSyncCompanion_Generated"/>) for any
            /// member decorated with <see cref="GONetAutoMagicalSyncAttribute.ShouldBlendBetweenValuesReceived"/> set to true.
            /// </summary>
            internal void AddToMostRecentChangeQueue_IfAppropriate(long elapsedTicksAtChange, object value)
            {
                for (int i = 0; i < mostRecentChanges_usedSize; ++i)
                {
                    var item = mostRecentChanges[i];
                    if (item.elapsedTicksAtChange < elapsedTicksAtChange)
                    {
                        // insert new guy, who is more recent than current, here at i; but first, move all the ones down a notch as they are all older than the new guy:
                        for (int j = mostRecentChanges_usedSize; j >= i; --j)
                        {
                            if (j < (mostRecentChanges_capacitySize - 1))
                            {
                                mostRecentChanges[j + 1] = mostRecentChanges[j];
                            }
                        }

                        mostRecentChanges[i] = NumericValueChangeSnapshot.Create(elapsedTicksAtChange, value);
                        if (mostRecentChanges_usedSize < mostRecentChanges_capacitySize)
                        {
                            ++mostRecentChanges_usedSize;
                        }
                        //LogBufferContentsIfAppropriate();
                        return;
                    }
                }

                if (mostRecentChanges_usedSize < mostRecentChanges_capacitySize)
                {
                    mostRecentChanges[mostRecentChanges_usedSize] = NumericValueChangeSnapshot.Create(elapsedTicksAtChange, value);
                    ++mostRecentChanges_usedSize;
                }

                //LogBufferContentsIfAppropriate();
            }

            long lastLogBufferContentsTicks;

            private void LogBufferContentsIfAppropriate()
            {
                if (mostRecentChanges_usedSize == mostRecentChanges_capacitySize && (TimeSpan.FromTicks(DateTime.UtcNow.Ticks - lastLogBufferContentsTicks).TotalSeconds > 20))
                {
                    lastLogBufferContentsTicks = DateTime.UtcNow.Ticks;
                    GONetLog.Debug("==============================================================================================");
                    for (int k = 0; k < mostRecentChanges_usedSize; ++k)
                    {
                        GONetLog.Debug(string.Concat("item: ", k, " value: ", mostRecentChanges[k].value_float, " changed @ time (seconds): ", TimeSpan.FromTicks(mostRecentChanges[k].elapsedTicksAtChange).TotalSeconds));
                    }
                }
            }

            /// <summary>
            /// Expected that this is called each frame.
            /// Loop through the recent changes to interpolate or extrapolate is possible.
            /// POST: The related/associated value is updated to what is believed to be the current value based on recent changes accumulated from owner/source.
            /// </summary>
            internal void ApplyValueBlending_IfAppropriate()
            {
                object blendedValue;
                if (ValueBlendUtils.TryGetBlendedValue(this, Time.ElapsedTicks, out blendedValue))
                {
                    syncCompanion.SetAutoMagicalSyncValue(index, blendedValue);
                }
            }
        }

        /// <summary>
        /// Only (re)used in <see cref="OnEnable_StartMonitoringForAutoMagicalNetworking"/>.
        /// </summary>
        static readonly HashSet<SyncBundleUniqueGrouping> uniqueSyncGroupings = new HashSet<SyncBundleUniqueGrouping>();

        internal struct SyncBundleUniqueGrouping
        {
            internal readonly float scheduleFrequency;
            internal readonly AutoMagicalSyncReliability reliability;
            internal readonly bool mustRunOnUnityMainThread;

            internal SyncBundleUniqueGrouping(float scheduleFrequency, AutoMagicalSyncReliability reliability, bool mustRunOnUnityMainThread)
            {
                this.scheduleFrequency = scheduleFrequency;
                this.reliability = reliability;
                this.mustRunOnUnityMainThread = mustRunOnUnityMainThread;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is SyncBundleUniqueGrouping))
                {
                    return false;
                }

                var grouping = (SyncBundleUniqueGrouping)obj;
                return scheduleFrequency == grouping.scheduleFrequency &&
                       reliability == grouping.reliability &&
                       mustRunOnUnityMainThread == grouping.mustRunOnUnityMainThread;
            }

            public override int GetHashCode()
            {
                var hashCode = -1343937139;
                hashCode = hashCode * -1521134295 + scheduleFrequency.GetHashCode();
                hashCode = hashCode * -1521134295 + reliability.GetHashCode();
                hashCode = hashCode * -1521134295 + mustRunOnUnityMainThread.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Call me in the <paramref name="gonetParticipant"/>'s OnEnable method.
        /// </summary>
        internal static void OnEnable_StartMonitoringForAutoMagicalNetworking(GONetParticipant gonetParticipant)
        {
            if (Application.isPlaying) // now that [ExecuteInEditMode] was added to GONetParticipant for OnDestroy, we have to guard this to only run in play
            {
                gonetParticipant.OwnerAuthorityIdChanged += OnOwnerAuthorityIdChanged_InitValueBlendSupport_IfAppropriate;

                { // auto-magical sync related housekeeping
                    Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated> autoSyncCompanions;
                    if (!activeAutoSyncCompanionsByCodeGenerationIdMap.TryGetValue(gonetParticipant.codeGenerationId, out autoSyncCompanions))
                    {
                        autoSyncCompanions = new Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated>(1000);
                        activeAutoSyncCompanionsByCodeGenerationIdMap[gonetParticipant.codeGenerationId] = autoSyncCompanions; // NOTE: This is the only place we add to the outer dictionary and this is always run in the main unity thread, THEREFORE no need for Concurrent....just on the inner ones
                    }
                    GONetParticipant_AutoMagicalSyncCompanion_Generated companion = GONetParticipant_AutoMagicalSyncCompanion_Generated_Factory.CreateInstance(gonetParticipant);
                    autoSyncCompanions[gonetParticipant] = companion; // NOTE: This is the only place where the inner dictionary is added to and is ensured to run on unity main thread since OnEnable, so no need for concurrency as long as we can say the same about removes

                    uniqueSyncGroupings.Clear();
                    for (int i = 0; i < companion.valuesCount; ++i)
                    {
                        AutoMagicalSync_ValueMonitoringSupport_ChangedValue monitoringSupport = companion.valuesChangesSupport[i];
                        SyncBundleUniqueGrouping grouping = 
                            new SyncBundleUniqueGrouping(
                                monitoringSupport.syncAttribute_SyncChangesEverySeconds, 
                                monitoringSupport.syncAttribute_Reliability, 
                                monitoringSupport.syncAttribute_MustRunOnUnityMainThread);

                        uniqueSyncGroupings.Add(grouping); // since it is a set, duplicates will be discarded

                        if (monitoringSupport.syncAttribute_QuantizerSettingsGroup.CanBeUsedForQuantization)
                        {
                            var quantizer_ensureCachedUpFront = Quantizer.LookupQuantizer(monitoringSupport.syncAttribute_QuantizerSettingsGroup);
                        }
                    }

                    if (gonetParticipant.animatorSyncSupport != null)
                    { // auto-sync stuffs, but this time for animation controller parameters
                        var animatorSyncSupportEnum = gonetParticipant.animatorSyncSupport.GetEnumerator();
                        while (animatorSyncSupportEnum.MoveNext())
                        {
                            string parameterName = animatorSyncSupportEnum.Current.Key;
                            GONetParticipant.AnimatorControllerParameter parameter = animatorSyncSupportEnum.Current.Value;

                            GONetLog.Debug(string.Concat("animator parameter....name: ", parameterName, " type: ", parameter.valueType, " isSyncd: ", parameter.isSyncd));
                        }
                    }

                    foreach (SyncBundleUniqueGrouping uniqueSyncGrouping in uniqueSyncGroupings)
                    {
                        if (!autoSyncProcessingSupportByFrequencyMap.ContainsKey(uniqueSyncGrouping))
                        {
                            var autoSyncProcessingSupport = 
                                new AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable(uniqueSyncGrouping, activeAutoSyncCompanionsByCodeGenerationIdMap); // IMPORTANT: this starts the thread!

                            autoSyncProcessingSupportByFrequencyMap[uniqueSyncGrouping] = autoSyncProcessingSupport;

                            if (uniqueSyncGrouping.mustRunOnUnityMainThread)
                            {
                                autoSyncProcessingSupports_UnityMainThread.Add(autoSyncProcessingSupport);
                            }
                        }
                    }
                }

                OnEnable_AssignGONetId_IfAppropriate(gonetParticipant);
            }
        }

        public static bool WasDefinedInScene(GONetParticipant gonetParticipant)
        {
            return definedInSceneParticipantInstanceIDs.Contains(gonetParticipant.GetInstanceID());
        }

        internal static void Start_AutoPropogateInstantiation_IfAppropriate(GONetParticipant gonetParticipant)
        {
            if (Application.isPlaying && !WasDefinedInScene(gonetParticipant))
            {
                GONetLog.Debug("Start...NOT defined in scene...name: " + gonetParticipant.gameObject.name);

                bool isThisCondisideredTheMomentOfInitialInstantiation = !remoteSpawns_avoidAutoPropogateSupport.Contains(gonetParticipant);
                if (isThisCondisideredTheMomentOfInitialInstantiation)
                {
                    gonetParticipant.OwnerAuthorityId = MyAuthorityId; // With the flow of methods and such, this looks like the first point in time we know to set this to my authority id

                    AutoPropogateInitialInstantiation(gonetParticipant);
                }
                else
                {
                    // this data item has now served its purpose (i.e., avoid auto propogate since it already came from remote source!), so remove it
                    remoteSpawns_avoidAutoPropogateSupport.Remove(gonetParticipant);
                }
            }
        }

        private static void AutoPropogateInitialInstantiation(GONetParticipant gonetParticipant)
        {
            InstantiateGONetParticipantEvent @event;

            string nonAuthorityDesignTimeLocation;
            if (GONetSpawnSupport_Runtime.TryGetNonAuthorityDesignTimeLocation(gonetParticipant, out nonAuthorityDesignTimeLocation))
            {
                @event = InstantiateGONetParticipantEvent.Create_WithNonAuthorityInfo(gonetParticipant, nonAuthorityDesignTimeLocation);
            }
            else
            {
                @event = InstantiateGONetParticipantEvent.Create(gonetParticipant);
            }

            EventBus.Publish(@event); // this causes the auto propogation via local handler to send to all remotes (i.e., all clients if server, server if client)
        }

        private static void OnOwnerAuthorityIdChanged_InitValueBlendSupport_IfAppropriate(GONetParticipant gonetParticipant, uint valueOld, uint valueNew)
        {
            bool shouldConsiderBlendingBetweenChangedValues = valueNew != MyAuthorityId && valueNew != OwnerAuthorityId_Unset; // if I do not own it, I might need to keep track of some value changes over time in order to blend between them
            if (shouldConsiderBlendingBetweenChangedValues)
            {
                Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated> autoSyncCompanions = activeAutoSyncCompanionsByCodeGenerationIdMap[gonetParticipant.codeGenerationId];
                GONetParticipant_AutoMagicalSyncCompanion_Generated autoSyncCompanion = autoSyncCompanions[gonetParticipant];
                for (int i = 0; i < autoSyncCompanion.valuesCount; ++i)
                {
                    AutoMagicalSync_ValueMonitoringSupport_ChangedValue valueChangeSupport = autoSyncCompanion.valuesChangesSupport[i];
                    if (valueChangeSupport.syncAttribute_ShouldBlendBetweenValuesReceived)
                    {
                        // TODO FIXME impl
                    }
                }
            }

            gonetParticipant.OwnerAuthorityIdChanged -= OnOwnerAuthorityIdChanged_InitValueBlendSupport_IfAppropriate; // we did what we needed to...done.
        }

        private static void OnEnable_AssignGONetId_IfAppropriate(GONetParticipant gonetParticipant)
        {
            if (IsServer && gonetParticipant.GONetId == GONetParticipant.GONetId_Unset) // TODO need to avoid this when this guy is coming from replay too! gonetParticipant.WasInstantiated true is all we have now...will have WasFromReplay later
            {
                gonetParticipant.GONetId = ++lastAssignedGONetId;
            }
        }

        static readonly HashSet<int> definedInSceneParticipantInstanceIDs = new HashSet<int>();

        internal static void RecordParticipantsAsDefinedInScene(List<GONetParticipant> gonetParticipantsInLevel)
        {
            gonetParticipantsInLevel.ForEach(gonetParticipant => definedInSceneParticipantInstanceIDs.Add(gonetParticipant.GetInstanceID()));
        }

        internal static void AssignOwnerAuthorityIds_IfAppropriate(List<GONetParticipant> gonetParticipantsInConsideration)
        {
            if (IsServer)
            {
                MyAuthorityId = OwnerAuthorityId_Server; // NOTE: at time of writing, MyAuthorityId is not set quite yet, which is why we go ahead and manually set here

                int count = gonetParticipantsInConsideration.Count;
                for (int i = 0; i < count; ++i)
                {
                    GONetParticipant item = gonetParticipantsInConsideration[i];
                    item.OwnerAuthorityId = MyAuthorityId;
                }
            }
        }

        /// <summary>
        /// PRE: <paramref name="connectionToClient"/> already has been assigned a good value to <see cref="GONetConnection.OwnerAuthorityId"/>.
        /// </summary>
        static void Server_SendClientCurrentState_AllAutoMagicalSync(GONetConnection connectionToClient)
        {
            using (BitByBitByteArrayBuilder bitStream = BitByBitByteArrayBuilder.GetBuilder())
            {
                { // header...just message type/id...well, and now time 
                    uint messageID = messageTypeToMessageIDMap[typeof(AutoMagicalSync_AllCurrentValues_Message)];
                    bitStream.WriteUInt(messageID);

                    bitStream.WriteLong(Time.ElapsedTicks);
                }

                SerializeBody_AllCurrentValuesBundle(bitStream); // body

                bitStream.WriteCurrentPartialByte();

                int bytesUsedCount = bitStream.Length_WrittenBytes;
                byte[] allValuesSerialized = mainThread_valueChangeSerializationArrayPool.Borrow(bytesUsedCount);
                Array.Copy(bitStream.GetBuffer(), 0, allValuesSerialized, 0, bytesUsedCount);
                    
                SendBytesToRemoteConnection(connectionToClient, allValuesSerialized, bytesUsedCount, GONetChannel.CustomSerialization_Reliable); // NOT using GONetChannel.AutoMagicalSync_Reliable because that one is reserved for things as they are happening and not this one time blast to a new client for all things
                mainThread_valueChangeSerializationArrayPool.Return(allValuesSerialized);
            }
        }

        /// <summary>
        /// For every unique value encountered for <see cref="GONetAutoMagicalSyncAttribute.SyncChangesEverySeconds"/>, an instance of this 
        /// class will be created and used to process only those fields/properties set to be sync'd on that frequency.
        /// </summary>
        internal sealed class AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable : IDisposable
        {
            private readonly int timeUpdateCountUponConstruction;

            bool isSetupToRunInSeparateThread;
            /// <summary>
            /// Only non-null when <see cref="isSetupToRunInSeparateThread"/> is true
            /// </summary>
            Thread thread;
            /// <summary>
            /// Can only be true when <see cref="isSetupToRunInSeparateThread"/> is true
            /// </summary>
            volatile bool isThreadRunning;

            volatile bool shouldProcessInSeparateThreadASAP = false;
            long lastProcessCompleteTicks;

            static readonly long END_OF_FRAME_IN_WHICH_CHANGE_OCCURS_TICKS = TimeSpan.FromSeconds(AutoMagicalSyncFrequencies.END_OF_FRAME_IN_WHICH_CHANGE_OCCURS_SECONDS).Ticks;

            SyncBundleUniqueGrouping uniqueGrouping;
            long scheduleFrequencyTicks;
            Dictionary<byte, Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated>> everythingMap_evenStuffNotOnThisScheduleFrequency;
            QosType uniqueGrouping_qualityOfService;
            GONetChannelId uniqueGrouping_channelId;

            /// <summary>
            /// Indicates whether or not <see cref="ProcessASAP"/> must be called (manually) from an outside part in order for sync processing to occur.
            /// </summary>
            internal bool DoesRequireManualProcessInitiation => scheduleFrequencyTicks == END_OF_FRAME_IN_WHICH_CHANGE_OCCURS_TICKS || !isSetupToRunInSeparateThread;

            /// <summary>
            /// Just a helper data structure just for use in <see cref="ProcessAutoMagicalSyncStuffs(bool, ReliableEndpoint)"/>
            /// </summary>
            readonly List<AutoMagicalSync_ValueMonitoringSupport_ChangedValue> syncValuesToSend = new List<AutoMagicalSync_ValueMonitoringSupport_ChangedValue>(1000);

            readonly ArrayPool<byte> myThread_valueChangeSerializationArrayPool;

            static readonly SecretaryOfTemporalAffairs myThread_Time = new SecretaryOfTemporalAffairs();

            /// <summary>
            /// IMPORTANT: If a value of <see cref="AutoMagicalSyncFrequencies.END_OF_FRAME_IN_WHICH_CHANGE_OCCURS"/> is passed in here for <paramref name="scheduleFrequency"/>,
            ///            then nothing will happen in here automatically....<see cref="GONetMain"/> or some other party will have to manually call <see cref="ProcessASAP"/>.
            /// </summary>
            internal AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable(SyncBundleUniqueGrouping uniqueGrouping, Dictionary<byte, Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated>> everythingMap_evenStuffNotOnThisScheduleFrequency)
            {
                autoSyncProcessThread_valueChangeSerializationArrayPool_ThreadMap[this] = myThread_valueChangeSerializationArrayPool = new ArrayPool<byte>(100, 10, 1024, 2048);

                this.uniqueGrouping = uniqueGrouping;
                scheduleFrequencyTicks = TimeSpan.FromSeconds(uniqueGrouping.scheduleFrequency).Ticks;
                uniqueGrouping_qualityOfService = uniqueGrouping.reliability == AutoMagicalSyncReliability.Reliable ? QosType.Reliable : QosType.Unreliable;
                uniqueGrouping_channelId = uniqueGrouping.reliability == AutoMagicalSyncReliability.Reliable ? GONetChannel.AutoMagicalSync_Reliable : GONetChannel.AutoMagicalSync_Unreliable;

                this.everythingMap_evenStuffNotOnThisScheduleFrequency = everythingMap_evenStuffNotOnThisScheduleFrequency;

                timeUpdateCountUponConstruction = Time.updateCount;
                Time.TimeSetFromAuthority += Time_TimeSetFromAuthority;

                isSetupToRunInSeparateThread = !uniqueGrouping.mustRunOnUnityMainThread;
                if (isSetupToRunInSeparateThread)
                {
                    thread = new Thread(ContinuallyProcess_NotMainThread);
                    isThreadRunning = true;
                    thread.Start();
                }
            }

            private void Time_TimeSetFromAuthority(double fromElapsedSeconds, double toElapsedSeconds, long fromElapsedTicks, long toElapsedTicks)
            {
                myThread_Time.SetFromAuthority(toElapsedTicks);
            }

            ~AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable()
            {
                Dispose();
            }

            private void ContinuallyProcess_NotMainThread()
            {
                bool doesRequireManualProcessInitiation = DoesRequireManualProcessInitiation;
                while (isThreadRunning)
                {
                    bool isNotSafeToContinue = Time.UpdateCount == timeUpdateCountUponConstruction;
                    if (isNotSafeToContinue || (doesRequireManualProcessInitiation && !shouldProcessInSeparateThreadASAP))
                    {
                        Thread.Sleep(1); // TODO come up with appropriate sleep time/value 
                    }
                    else
                    {
                        Process();
                        shouldProcessInSeparateThreadASAP = false; // reset this

                        if (!doesRequireManualProcessInitiation)
                        { // (auto sync) frequency control:
                            long nextProcessStartTicks = lastProcessCompleteTicks + scheduleFrequencyTicks;
                            long nowTicks = DateTime.UtcNow.Ticks;// NOTE: avoiding using high resolution as follows because that class is not thread-safe (yet): HighResolutionTimeUtils.Now.Ticks;
                            lastProcessCompleteTicks = nowTicks;
                            long ticksToSleep = nextProcessStartTicks - nowTicks;
                            if (ticksToSleep > 0)
                            {
                                Thread.Sleep(TimeSpan.FromTicks(ticksToSleep));
                            }
                        }
                    }
                }
            }

            private void Process()
            {
                myThread_Time.Update();
                long myTicks = myThread_Time.ElapsedTicks;
                // loop over everythingMap_evenStuffNotOnThisScheduleFrequency only processing the items inside that match scheduleFrequency
                syncValuesToSend.Clear();

                var enumeratorOuter = everythingMap_evenStuffNotOnThisScheduleFrequency.GetEnumerator();
                while (enumeratorOuter.MoveNext())
                {
                    Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated> currentMap = enumeratorOuter.Current.Value;
                    var enumeratorInner = currentMap.GetEnumerator();
                    while (enumeratorInner.MoveNext())
                    {
                        GONetParticipant_AutoMagicalSyncCompanion_Generated monitoringSupport = enumeratorInner.Current.Value;

                        // need to call this for every single one to keep track of changes, BUT we only want to consider/process ones that match the current frequency:
                        monitoringSupport.UpdateLastKnownValues(uniqueGrouping); // IMPORTANT: passing in the frequency here narrows down what gets appended to only ones with frequency match
                        if (monitoringSupport.HaveAnyValuesChangedSinceLastCheck(uniqueGrouping)) // IMPORTANT: passing in the frequency here narrows down what gets appended to only ones with frequency match
                        {
                            monitoringSupport.AppendListWithChangesSinceLastCheck(syncValuesToSend, uniqueGrouping); // IMPORTANT: passing in the frequency here narrows down what gets appended to only ones with frequency match
                            monitoringSupport.OnValueChangeCheck_Reset(uniqueGrouping); // IMPORTANT: passing in the frequency here narrows down what gets appended to only ones with frequency match
                        }
                    }
                }

                if (syncValuesToSend.Count > 0)
                {
                    int bytesUsedCount;
                    //GONetLog.Debug("sending changed auto-magical sync values to all connections");
                    if (IsServer)
                    {
                        // if its the server, we have to consider who we are sending to and ensure we do not send then changes that initially came from them!
                        gonetServer?.ForEachClient((clientConnection) =>
                        {
                            byte[] changesSerialized_clientSpecific = SerializeWhole_ChangesBundle(syncValuesToSend, myThread_valueChangeSerializationArrayPool, out bytesUsedCount, clientConnection.OwnerAuthorityId, myTicks);
                            SendBytesToRemoteConnection(clientConnection, changesSerialized_clientSpecific, bytesUsedCount, uniqueGrouping_channelId);
                            myThread_valueChangeSerializationArrayPool.Return(changesSerialized_clientSpecific);
                        });
                    }
                    else
                    {
                        if (MyAuthorityId == OwnerAuthorityId_Unset)
                        {
                            throw new Exception("Magoo.....we need this set before doing the following:");
                        }
                        byte[] changesSerialized = SerializeWhole_ChangesBundle(syncValuesToSend, myThread_valueChangeSerializationArrayPool, out bytesUsedCount, MyAuthorityId, myTicks);
                        SendBytesToRemoteConnections(changesSerialized, bytesUsedCount, uniqueGrouping_channelId);
                        myThread_valueChangeSerializationArrayPool.Return(changesSerialized);
                    }
                }
            }

            /// <summary>
            /// IMPORTANT: When <see cref="isSetupToRunInSeparateThread"/> is false, calling this will NOT yield a call to <see cref="Process"/> and caller must keep calling this method each frame until proper schedule cycle permits the call to <see cref="Process"/> to go through.
            /// </summary>
            internal void ProcessASAP()
            {
                if (isSetupToRunInSeparateThread)
                {
                    shouldProcessInSeparateThreadASAP = true;
                }
                else
                {
                    if (scheduleFrequencyTicks == END_OF_FRAME_IN_WHICH_CHANGE_OCCURS_TICKS)
                    {
                        Process();
                    }
                    else
                    {
                        long nowTicks = DateTime.UtcNow.Ticks;// NOTE: avoiding using high resolution as follows because that class is not thread-safe (yet): HighResolutionTimeUtils.Now.Ticks;
                        bool isASAPNow = (nowTicks - lastProcessCompleteTicks) > scheduleFrequencyTicks;
                        if (isASAPNow)
                        {
                            Process();
                            lastProcessCompleteTicks = nowTicks;
                        }
                    }
                }
            }

            public void Dispose()
            {
                if (isSetupToRunInSeparateThread)
                {
                    isThreadRunning = false;
                    thread.Abort();
                }
            }
        }

        /// <summary>
        /// this is used as changes are taking place over time....unlike <see cref="mainThread_valueChangeSerializationArrayPool"/>
        /// </summary>
        static readonly ConcurrentDictionary<AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable, ArrayPool<byte>> autoSyncProcessThread_valueChangeSerializationArrayPool_ThreadMap =
            new ConcurrentDictionary<AutoMagicalSyncProcessing_SingleGrouping_SeparateThreadCapable, ArrayPool<byte>>();
        /// <summary>
        /// This is used when sending currente state to newly connecting clients unlike <see cref="autoSyncProcessThread_valueChangeSerializationArrayPool_ThreadMap"/>
        /// </summary>
        static readonly ArrayPool<byte> mainThread_valueChangeSerializationArrayPool = new ArrayPool<byte>(100, 10, 1024, 2048);

        static readonly ArrayPool<byte> mainThread_miscSerializationArrayPool = new ArrayPool<byte>(100, 10, 1024, 2048);
        

        static readonly Dictionary<Type, uint> messageTypeToMessageIDMap = new Dictionary<Type, uint>(4096);
        static readonly Dictionary<uint, Type> messageTypeByMessageIDMap = new Dictionary<uint, Type>(4096);
        static uint nextMessageID;

        private static void InitMessageTypeToMessageIDMap()
        {
            try
            {
                foreach (var types in AppDomain.CurrentDomain.GetAssemblies().OrderBy(a => a.FullName)
                        .Select(a => a.GetTypes().Where(t => TypeUtils.IsTypeAInstanceOfTypeB(t, typeof(IGONetEvent)) && !t.IsAbstract).OrderBy(t2 => t2.FullName)))
                {
                    foreach (var type in types)
                    {
                        uint messageID = nextMessageID++;
                        messageTypeToMessageIDMap[type] = messageID;
                        messageTypeByMessageIDMap[messageID] = type;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex); // since our log stuffs does not work in static context within unity editor, use unity logging for this one
            }
        }

        /// <summary>
        /// PRE: <paramref name="changes"/> size is greater than 0
        /// PRE: <paramref name="filterUsingOwnerAuthorityId"/> is not <see cref="OwnerAuthorityId_Unset"/> otherwise an exception is thrown
        /// IMPORTANT: The caller is responsible for returning the returned byte[] to <paramref name="byteArrayPool"/>
        /// </summary>
        private static byte[] SerializeWhole_ChangesBundle(List<AutoMagicalSync_ValueMonitoringSupport_ChangedValue> changes, ArrayPool<byte> byteArrayPool, out int bytesUsedCount, uint filterUsingOwnerAuthorityId, long elapsedTicksAtCapture)
        {
            if (filterUsingOwnerAuthorityId == OwnerAuthorityId_Unset)
            {
                throw new ArgumentOutOfRangeException(nameof(filterUsingOwnerAuthorityId));
            }

            using (BitByBitByteArrayBuilder bitStream = BitByBitByteArrayBuilder.GetBuilder())
            {
                { // header...just message type/id...well, and now time 
                    uint messageID = messageTypeToMessageIDMap[typeof(AutoMagicalSync_ValueChanges_Message)];
                    bitStream.WriteUInt(messageID);

                    bitStream.WriteLong(elapsedTicksAtCapture);
                }

                SerializeBody_ChangesBundle(changes, bitStream, filterUsingOwnerAuthorityId); // body

                bitStream.WriteCurrentPartialByte();

                bytesUsedCount = bitStream.Length_WrittenBytes;
                byte[] bytes = byteArrayPool.Borrow(bytesUsedCount);
                Array.Copy(bitStream.GetBuffer(), 0, bytes, 0, bytesUsedCount);

                return bytes;
            }
        }

        class AutoMagicalSyncChangePriorityComparer : IComparer<AutoMagicalSync_ValueMonitoringSupport_ChangedValue>
        {
            internal static readonly AutoMagicalSyncChangePriorityComparer Instance = new AutoMagicalSyncChangePriorityComparer();

            private AutoMagicalSyncChangePriorityComparer() { }

            public int Compare(AutoMagicalSync_ValueMonitoringSupport_ChangedValue x, AutoMagicalSync_ValueMonitoringSupport_ChangedValue y)
            {
                int xPriority = x.syncAttribute_ProcessingPriority_GONetInternalOverride != 0 ? x.syncAttribute_ProcessingPriority_GONetInternalOverride : x.syncAttribute_ProcessingPriority;
                int yPriority = y.syncAttribute_ProcessingPriority_GONetInternalOverride != 0 ? y.syncAttribute_ProcessingPriority_GONetInternalOverride : y.syncAttribute_ProcessingPriority;

                return yPriority.CompareTo(xPriority); // descending...highest priority first!
            }
        }

        private static void SerializeBody_AllCurrentValuesBundle(Utils.BitByBitByteArrayBuilder bitStream_headerAlreadyWritten)
        {
            var enumeratorOuter = activeAutoSyncCompanionsByCodeGenerationIdMap.GetEnumerator();
            while (enumeratorOuter.MoveNext())
            {
                Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated> currentMap = enumeratorOuter.Current.Value;
                var enumeratorInner = currentMap.GetEnumerator();
                while (enumeratorInner.MoveNext())
                {
                    var current = enumeratorInner.Current;

                    GONetParticipant gonetParticipant = current.Key;
                    GONetParticipant.GONetId_InitialAssignment_CustomSerializer.Instance.Serialize(bitStream_headerAlreadyWritten, gonetParticipant, gonetParticipant.GONetId);

                    GONetParticipant_AutoMagicalSyncCompanion_Generated monitoringSupport = current.Value;
                    monitoringSupport.SerializeAll(bitStream_headerAlreadyWritten);
                }
            }
        }

        private static void SerializeBody_ChangesBundle(List<AutoMagicalSync_ValueMonitoringSupport_ChangedValue> changes, Utils.BitByBitByteArrayBuilder bitStream_headerAlreadyWritten, uint filterUsingOwnerAuthorityId)
        {
            int countTotal = changes.Count;
            int countMinus1 = countTotal - 1;

            int countFiltered = 0;
            for (int iSort = 0; iSort < countMinus1; ++iSort) // manual sort to avoid GC
            {
                var changeA = changes[iSort];
                var changeB = changes[iSort + 1];
                if (AutoMagicalSyncChangePriorityComparer.Instance.Compare(changeA, changeB) > 0)
                {
                    changes[iSort + 1] = changeA;
                    changes[iSort] = changeB;
                }

                if (ShouldSendChange(changes[iSort], filterUsingOwnerAuthorityId)) // use this manual check to avoid Linq.Count(....) GC/perf hit
                {
                    ++countFiltered;
                }
            }
            if (ShouldSendChange(changes[countMinus1], filterUsingOwnerAuthorityId)) // use this manual check to avoid Linq.Count(....) GC/perf hit
            {
                ++countFiltered;
            }

            bitStream_headerAlreadyWritten.WriteUShort((ushort)countFiltered);
            //GONetLog.Debug(string.Concat("about to send changes bundle...countFiltered: " + countFiltered));

            for (int i = 0; i < countTotal; ++i)
            {
                AutoMagicalSync_ValueMonitoringSupport_ChangedValue change = changes[i];
                if (!ShouldSendChange(change, filterUsingOwnerAuthorityId))
                {
                    continue; // skip this guy (i.e., apply the "filter")
                }

                bool canASSumeNetId = change.index == GONetParticipant.ASSumed_GONetId_INDEX;
                bitStream_headerAlreadyWritten.WriteBit(canASSumeNetId);
                if (canASSumeNetId)
                {
                    // this will use GONetId_InitialAssignment_CustomSerializer and write the full unique path and the gonetId:
                    change.syncCompanion.SerializeSingle(bitStream_headerAlreadyWritten, GONetParticipant.ASSumed_GONetId_INDEX);
                }
                else
                {
                    if (change.syncCompanion.gonetParticipant.GONetId == GONetParticipant.GONetId_Unset)
                    {
                        GONetLog.Error("Snafoo....gonetid 0.....why are we about to send change? ...makes no sense! ShouldSendChange(change, filterUsingOwnerAuthorityId): " + ShouldSendChange(change, filterUsingOwnerAuthorityId) + " filterUsingOwnerAuthorityId: " + filterUsingOwnerAuthorityId);
                    }
                    bitStream_headerAlreadyWritten.WriteUInt(change.syncCompanion.gonetParticipant.GONetId); // have to write the gonetid first before each changed value
                    bitStream_headerAlreadyWritten.WriteByte(change.index); // then have to write the index, otherwise other end does not know which index to deserialize
                    change.syncCompanion.SerializeSingle(bitStream_headerAlreadyWritten, change.index);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ShouldSendChange(AutoMagicalSync_ValueMonitoringSupport_ChangedValue change, uint filterUsingOwnerAuthorityId)
        {
            return
                change.syncCompanion.gonetParticipant.GONetId != GONetParticipant.GONetId_Unset &&
                (IsServer
                    ? (change.syncCompanion.gonetParticipant.OwnerAuthorityId != filterUsingOwnerAuthorityId // In most circumstances, the server should send every change exception for changes back to the owner itself
                       || change.index == GONetParticipant.ASSumed_GONetId_INDEX) // this is the one exception, if the server is assigning the instantiator/owner its GONetId for the first time, it DOES need to get sent back to itself
                    : change.syncCompanion.gonetParticipant.OwnerAuthorityId == filterUsingOwnerAuthorityId); // clients should only send out changes it owns
        }

        private static void DeserializeBody_AllValuesBundle(Utils.BitByBitByteArrayBuilder bitStream_headerAlreadyRead, int bytesUsedCount, GONetConnection sourceOfChangeConnection, long elapsedTicksAtSend)
        {
            while (bitStream_headerAlreadyRead.Position_Bytes < bytesUsedCount) // while more data to read/process
            {
                uint gonetId = (uint)GONetParticipant.GONetId_InitialAssignment_CustomSerializer.Instance.Deserialize(bitStream_headerAlreadyRead);

                GONetParticipant gonetParticipant = gonetParticipantByGONetIdMap[gonetId];
                GONetParticipant_AutoMagicalSyncCompanion_Generated syncCompanion = activeAutoSyncCompanionsByCodeGenerationIdMap[gonetParticipant.codeGenerationId][gonetParticipant];

                syncCompanion.DeserializeInitAll(bitStream_headerAlreadyRead, elapsedTicksAtSend);
            }
        }

        private static void DeserializeBody_ChangesBundle(Utils.BitByBitByteArrayBuilder bitStream_headerAlreadyRead, GONetConnection sourceOfChangeConnection, long elapsedTicksAtSend)
        {
            ushort count;
            bitStream_headerAlreadyRead.ReadUShort(out count);
            //GONetLog.Debug(string.Concat("about to read changes bundle...count: " + count));
            for (int i = 0; i < count; ++i)
            {
                bool canASSumeNetId;
                bitStream_headerAlreadyRead.ReadBit(out canASSumeNetId);
                if (canASSumeNetId)
                {
                    GONetParticipant.GONetId_InitialAssignment_CustomSerializer.Instance.Deserialize(bitStream_headerAlreadyRead);
                }
                else
                {
                    uint gonetId;
                    bitStream_headerAlreadyRead.ReadUInt(out gonetId);

                    if (!gonetParticipantByGONetIdMap.ContainsKey(gonetId))
                    {
                        GONetLog.Error("gladousche...NOT FOUND....expect an exception/ERROR to follow.....gonetId: " + gonetId);
                    }

                    GONetParticipant gonetParticipant = gonetParticipantByGONetIdMap[gonetId];
                    Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated> companionMap = activeAutoSyncCompanionsByCodeGenerationIdMap[gonetParticipant.codeGenerationId];
                    GONetParticipant_AutoMagicalSyncCompanion_Generated syncCompanion = companionMap[gonetParticipant];

                    byte index = (byte)bitStream_headerAlreadyRead.ReadByte();
                    syncCompanion.DeserializeInitSingle(bitStream_headerAlreadyRead, index, elapsedTicksAtSend);
                }
            }
            //GONetLog.Debug(string.Concat("************done reading changes bundle"));
        }

        /// <summary>
        /// Call me in the <paramref name="gonetParticipant"/>'s OnDisable method.
        /// </summary>
        internal static void OnDisable_StopMonitoringForAutoMagicalNetworking(GONetParticipant gonetParticipant)
        {
            if (Application.isPlaying) // now that [ExecuteInEditMode] was added to GONetParticipant for OnDestroy, we have to guard this to only run in play
            {
                { // auto-magical sync related housekeeping
                    Dictionary<GONetParticipant, GONetParticipant_AutoMagicalSyncCompanion_Generated> autoSyncCompanions = activeAutoSyncCompanionsByCodeGenerationIdMap[gonetParticipant.codeGenerationId];
                    if (!autoSyncCompanions.Remove(gonetParticipant)) // NOTE: This is the only place where the inner dictionary is removed from and is ensured to run on unity main thread since OnDisable, so no need for concurrency as long as we can say the same about adds
                    {
                        const string PORK = "Expecting to find active auto-sync companion in order to de-active/remove it upon gonetParticipant.OnDisable, but did not. gonetParticipant.GONetId: ";
                        const string NAME = " gonetParticipant.gameObject.name: ";
                        GONetLog.Warning(string.Concat(PORK, gonetParticipant.GONetId, NAME, gonetParticipant.gameObject.name));
                    }
                }

                gonetParticipantByGONetIdMap.Remove(gonetParticipant.GONetId);

                // do we need to send event to disable this thing?
            }
        }

        #endregion
    }

    public class GONetChannel
    {
        private static readonly Dictionary<GONetChannelId, GONetChannel> byIdMap = new Dictionary<GONetChannelId, GONetChannel>(byte.MaxValue);
        private static GONetChannelId nextAvailableId = 0;

        public static readonly GONetChannel TimeSync_Unreliable;
        public static readonly GONetChannel AutoMagicalSync_Reliable;
        public static readonly GONetChannel AutoMagicalSync_Unreliable;
        public static readonly GONetChannel CustomSerialization_Reliable;
        public static readonly GONetChannel CustomSerialization_Unreliable;
        public static readonly GONetChannel EventSingles_Reliable;
        /// <summary>
        /// <para>Using this probably only makes sense when the event implements <see cref="ITransientEvent"/>.</para>
        /// <para>If it implements <see cref="IPersistentEvent"/>, then it likely makes more sense to use <see cref="EventSingles_Reliable"/> instead.</para>
        /// </summary>
        public static readonly GONetChannel EventSingles_Unreliable;

        public GONetChannelId Id { get; private set; }

        public QosType QualityOfService { get; private set; }

        static GONetChannel()
        {
            TimeSync_Unreliable = new GONetChannel(QosType.Unreliable);
            AutoMagicalSync_Reliable = new GONetChannel(QosType.Reliable);
            AutoMagicalSync_Unreliable = new GONetChannel(QosType.Unreliable);
            CustomSerialization_Reliable = new GONetChannel(QosType.Reliable);
            CustomSerialization_Unreliable = new GONetChannel(QosType.Unreliable);
            EventSingles_Reliable = new GONetChannel(QosType.Reliable);
            EventSingles_Unreliable = new GONetChannel(QosType.Unreliable);
        }

        internal GONetChannel(QosType qualityOfService)
        {
            Id = nextAvailableId++;
            QualityOfService = qualityOfService;

            byIdMap[Id] = this;
        }

        public static GONetChannel ById(GONetChannelId id)
        {
            return byIdMap[id];
        }

        public static implicit operator GONetChannelId(GONetChannel channel)
        {
            return channel.Id;
        }
    }
}
