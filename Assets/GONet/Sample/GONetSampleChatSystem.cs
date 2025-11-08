using GONet;
using MemoryPack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Runtime.CompilerServices;

/// <summary>
/// A comprehensive chat system for GONet that demonstrates advanced RPC features including
/// TargetRpc with validation, server-side message filtering, and multi-parameter async RPCs.
///
/// This sample showcases the new GONet RPC validation system with:
/// - Custom validation methods that can modify message content before delivery
/// - Server-side profanity filtering with web API integration and local fallback
/// - TargetRpc routing to specific clients or groups with delivery reports
/// - Multi-parameter RPC support (up to 8 parameters) for complex data transfer
/// - Async RPC patterns with proper error handling and timeouts
///
/// Usage:
/// 1. Place this component on a GameObject with a GONetParticipant
/// 2. The system automatically tracks GONetLocal objects as chat participants
/// 3. Use channels for public chat, or select participants for direct/group messages
/// 4. Server automatically validates and filters all messages before delivery
///
/// Key RPC Examples:
/// - [TargetRpc] with validation: SendMessage() shows parameter validation and content filtering
/// - [ServerRpc] for registration: RegisterParticipant() demonstrates server authority
/// - [ClientRpc] for broadcasts: BroadcastParticipantUpdate() shows multi-client updates
///
/// Persistent RPC Strategy (Late-Joining Client Support):
/// ✅ Channel creation: [ClientRpc(IsPersistent = true)] - All clients need channel info
/// ✅ Participant updates: [ClientRpc(IsPersistent = true)] - All clients need participant list
/// ❌ Chat messages: [TargetRpc] (NOT persistent) - Transient content, history handled locally
///
/// This serves as a production-ready reference implementation for GONet's RPC system.
/// </summary>
[RequireComponent(typeof(GONetParticipant))]
public class GONetSampleChatSystem : GONetParticipantCompanionBehaviour
{
    #region Data Structures

    public enum ChatType
    {
        Channel,
        DirectMessage,
        GroupMessage,
        System
    }

    public enum ConnectionStatus
    {
        Connected,
        Away,
        Busy,
        Offline
    }

    #endregion

    #region Properties and Fields

    // UI State
    private bool isExpanded = false;
    private Vector2 scrollPosition;
    private Vector2 participantsScrollPosition;
    private string currentInputText = "";
    private string newChannelName = "";
    private bool showNewChannelDialog = false;

    // Chat State
    private List<ChatParticipant> participants = new List<ChatParticipant>();
    private List<ChatChannel> channels = new List<ChatChannel>();
    private List<ChatMessage> allMessages = new List<ChatMessage>(); // Store ALL messages
    private HashSet<ushort> selectedParticipants = new HashSet<ushort>();
    private string activeChannel = "general";
    private ChatType currentChatMode = ChatType.Channel;

    // UI Configuration
    private Rect windowRect = new Rect(10, Screen.height - 510, 400, 500);
    private Rect collapsedRect = new Rect(10, Screen.height - 70, 100, 60);

    // Profanity Filter (Server-side)
    private static readonly Dictionary<string, string> profanityReplacements = new Dictionary<string, string>
    {
        { "damn", "darn" },
        { "hell", "heck" },
        { "shit", "💩" },
        { "fuck", "🤬" },
        { "ass", "🍑" },
        { "bitch", "🐕" },
        // Add more as needed
    };

    // For TargetRpc routing
    public List<ushort> CurrentMessageTargets { get; set; } = new();
    public ushort CurrentSingleTarget { get; set; } // For single-target RPCs

    // Local user info
    private string localDisplayName = "";
    private ushort localAuthorityId;

    #endregion

    #region Unity & GONet Lifecycle

    protected override void Awake()
    {
        base.Awake();

        // Initialize default channel
        channels.Add(new ChatChannel
        {
            Name = "general",
            Description = "General discussion",
            CreatorId = 0,
            CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        });
    }

    public override void OnGONetReady()
    {
        base.OnGONetReady();

        // This hook is called whether this component was added at design-time or runtime
        // GONetId, OwnerAuthorityId, GONetLocal lookups, etc. are ALL guaranteed to be ready
        localAuthorityId = GONetMain.MyAuthorityId;
        localDisplayName = (GONetMain.IsServer || IsServerAuthorityId(localAuthorityId)) ? "Server" : $"Player_{localAuthorityId}";

        GONetLog.Info($"[CHAT-DEBUG] OnGONetReady - localAuthorityId: {localAuthorityId}, IsServer: {GONetMain.IsServer}, GONetId: {gonetParticipant?.GONetId ?? 0}, WasAddedAtRuntime: {WasAddedAtRuntime}");

        // CRITICAL: Scan for existing GONetLocal participants that may have been enabled before this chat system was added
        // This handles the case where GONetRuntimeComponentInitializer adds us AFTER participants are already active
        ScanForExistingParticipants();

        // CRITICAL: Auto-register ourselves (server or client) if not already in the participants list
        // This handles scenes without GONetLocal player objects where we still want chat to work
        if (!participants.Any(p => p.AuthorityId == localAuthorityId))
        {
            var self = new ChatParticipant
            {
                AuthorityId = localAuthorityId,
                DisplayName = localDisplayName,
                IsServer = GONetMain.IsServer || IsServerAuthorityId(localAuthorityId),
                Status = ConnectionStatus.Connected
            };
            participants.Add(self);
            GONetLog.Info($"[CHAT-DEBUG] Auto-registered self: {localDisplayName} (authority {localAuthorityId}). Total participants: {participants.Count}");

            // If server, broadcast the updated list
            if (GONetMain.IsServer)
            {
                CallRpc(nameof(BroadcastParticipantUpdate), participants.ToArray());
            }
        }

        // If client, register with server after a short delay
        // Note: Persistent RPCs will automatically provide current state
        if (!GONetMain.IsServer)
        {
            StartCoroutine(RegisterAfterDelay());
        }
    }

    private void ScanForExistingParticipants()
    {
        GONetLog.Info($"[CHAT-DEBUG] Scanning for existing GONetLocal participants using GONet framework...");

        // Use GONet's internal tracking system instead of FindObjectsOfType
        // GONetLocal.LookupByAuthorityId provides access to all GONetLocal instances via static dictionary
        if (GONetLocal.LookupByAuthorityId != null)
        {
            int count = 0;
            foreach (var kvp in GONetLocal.LookupByAuthorityId)
            {
                ushort authorityId = kvp.Key;
                GONetLocal local = kvp.Value;
                count++;

                if (local != null && local.TryGetComponent(out GONetParticipant gnp) && gnp.enabled)
                {
                    GONetLog.Info($"[CHAT-DEBUG] Found GONetLocal for authority {authorityId} ('{local.name}')");
                    // Manually trigger the participant enabled logic
                    OnGONetParticipantEnabled(gnp);
                }
                else
                {
                    GONetLog.Info($"[CHAT-DEBUG] Skipping GONetLocal for authority {authorityId} - component null or disabled");
                }
            }
            GONetLog.Info($"[CHAT-DEBUG] Scanned {count} GONetLocal instances from framework");
        }
        else
        {
            GONetLog.Info($"[CHAT-DEBUG] GONetLocal.LookupByAuthorityId is null - no participants registered yet");
        }
    }

    private IEnumerator RegisterAfterDelay()
    {
        yield return new WaitForSeconds(0.5f); // Give time for all participants to be detected
        CallRpc(nameof(RegisterParticipant));
    }

    public override void OnGONetParticipantEnabled(GONetParticipant gonetParticipant)
    {
        base.OnGONetParticipantEnabled(gonetParticipant);

        GONetLog.Info($"[CHAT-DEBUG] OnGONetParticipantEnabled called for '{gonetParticipant.name}' (GONetId: {gonetParticipant.GONetId})");

        // Check if this participant has a GONetLocal component (represents a player)
        if (gonetParticipant.TryGetComponent(out GONetLocal gonetLocal))
        {
            ushort authorityId = gonetLocal.OwnerAuthorityId;

            GONetLog.Info($"[CHAT-DEBUG] Found GONetLocal with authorityId: {authorityId}, already tracked: {participants.Any(p => p.AuthorityId == authorityId)}");

            // Skip if already tracked
            if (participants.Any(p => p.AuthorityId == authorityId))
                return;

            string displayName = IsServerAuthorityId(authorityId) ? "Server" : $"Player_{authorityId}";

            var newParticipant = new ChatParticipant
            {
                AuthorityId = authorityId,
                DisplayName = displayName,
                IsServer = IsServerAuthorityId(authorityId),
                Status = ConnectionStatus.Connected
            };

            participants.Add(newParticipant);

            GONetLog.Info($"[CHAT-DEBUG] Added participant {authorityId} to list. Total participants: {participants.Count}");

            // Show join message (skip for ourselves)
            if (authorityId != localAuthorityId)
            {
                var sysMsg = new ChatMessage
                {
                    Type = ChatType.System,
                    Content = $"{displayName} joined",
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                allMessages.Add(sysMsg);
                TrimMessageHistory();
            }

            // Server broadcasts the updated list to all clients
            if (GONetMain.IsServer)
            {
                CallRpc(nameof(BroadcastParticipantUpdate), participants.ToArray());
            }
        }
        else
        {
            GONetLog.Info($"[CHAT-DEBUG] Participant '{gonetParticipant.name}' does not have GONetLocal component - skipping");
        }
    }

    public override void OnGONetParticipantDisabled(GONetParticipant gonetParticipant)
    {
        base.OnGONetParticipantDisabled(gonetParticipant);

        // Check if this participant has a GONetLocal component
        if (gonetParticipant.TryGetComponent(out GONetLocal gonetLocal))
        {
            ushort authorityId = gonetLocal.OwnerAuthorityId;
            var participant = participants.FirstOrDefault(p => p.AuthorityId == authorityId);

            if (participant.AuthorityId != 0) // Found (checking struct default)
            {
                participants.RemoveAll(p => p.AuthorityId == authorityId);
                selectedParticipants.Remove(authorityId);

                var sysMsg = new ChatMessage
                {
                    Type = ChatType.System,
                    Content = $"{participant.DisplayName} disconnected",
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                allMessages.Add(sysMsg);
                TrimMessageHistory();

                if (GONetMain.IsServer)
                {
                    CallRpc(nameof(BroadcastParticipantUpdate), participants.ToArray());
                }
            }
        }
    }

    void OnGUI()
    {
        if (isExpanded)
        {
            windowRect = GUI.Window(0, windowRect, DrawChatWindow, "GONet Chat System");
        }
        else
        {
            collapsedRect = GUI.Window(0, collapsedRect, DrawCollapsedWindow, "");
        }
    }

    #endregion

    #region GUI Drawing

    private void DrawChatWindow(int windowID)
    {
        GUILayout.BeginHorizontal();

        // Left Panel - Channels and Participants
        GUILayout.BeginVertical(GUILayout.Width(120));
        DrawChannelsSection();
        DrawParticipantsSection();
        GUILayout.EndVertical();

        // Right Panel - Chat Area
        GUILayout.BeginVertical();
        DrawChatHeader();
        DrawMessagesArea();
        DrawInputArea();
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

        // Window Controls
        if (GUI.Button(new Rect(windowRect.width - 25, 5, 20, 20), "X"))
        {
            isExpanded = false;
        }

        GUI.DragWindow(new Rect(0, 0, windowRect.width - 30, 20));
    }

    private void DrawCollapsedWindow(int windowID)
    {
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("💬 Chat", GUILayout.Height(25)))
        {
            isExpanded = true;
        }

        // Show unread indicator
        int unreadCount = GetUnreadMessageCount();
        if (unreadCount > 0)
        {
            GUILayout.Label($"({unreadCount})", GUILayout.Width(30));
        }

        GUILayout.EndHorizontal();

        GUI.DragWindow();
    }

    private void DrawChannelsSection()
    {
        GUILayout.Label("Channels", GUI.skin.box);

        foreach (var channel in channels)
        {
            bool isActive = currentChatMode == ChatType.Channel && activeChannel == channel.Name;
            GUI.backgroundColor = isActive ? Color.cyan : Color.white;

            if (GUILayout.Button($"# {channel.Name}", GUILayout.Height(20), GUILayout.MaxWidth(115)))
            {
                SwitchToChannel(channel.Name);
            }
        }

        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("+ New Channel", GUILayout.Height(20), GUILayout.MaxWidth(115)))
        {
            showNewChannelDialog = true;
        }

        if (showNewChannelDialog)
        {
            DrawNewChannelDialog();
        }
    }

    private void DrawParticipantsSection()
    {
        GUILayout.Label($"Online ({participants.Count})", GUI.skin.box);

        participantsScrollPosition = GUILayout.BeginScrollView(participantsScrollPosition, GUILayout.Height(200));

        foreach (var participant in participants)
        {
            GUILayout.BeginHorizontal();

            // Status indicator
            Color statusColor = GetStatusColor(participant.Status);
            GUI.color = statusColor;
            GUILayout.Label("●", GUILayout.Width(15));
            GUI.color = Color.white;

            // Selection checkbox for DM/Group (skip for self)
            if (participant.AuthorityId != localAuthorityId)
            {
                bool isSelected = selectedParticipants.Contains(participant.AuthorityId);
                bool newSelected = GUILayout.Toggle(isSelected, "", GUILayout.Width(20));

                if (newSelected != isSelected)
                {
                    if (newSelected)
                        selectedParticipants.Add(participant.AuthorityId);
                    else
                        selectedParticipants.Remove(participant.AuthorityId);
                }
            }
            else
            {
                GUILayout.Space(24); // Keep alignment
            }

            // Name button for quick DM
            string nameLabel = participant.AuthorityId == localAuthorityId
                ? $"{participant.DisplayName} (You)"
                : participant.DisplayName;

            if (GUILayout.Button(nameLabel, GUI.skin.label))
            {
                if (participant.AuthorityId != localAuthorityId)
                {
                    StartDirectMessage(participant.AuthorityId);
                }
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();

        // Direct/Group message button
        if (selectedParticipants.Count > 0)
        {
            string buttonText = selectedParticipants.Count == 1 ?
                $"Direct Message ({selectedParticipants.Count})" :
                $"Group Message ({selectedParticipants.Count})";

            if (GUILayout.Button(buttonText))
            {
                if (selectedParticipants.Count == 1)
                {
                    StartDirectMessage(selectedParticipants.First());
                }
                else
                {
                    StartGroupMessage();
                }
            }
        }
    }

    private void DrawChatHeader()
    {
        GUILayout.BeginHorizontal(GUI.skin.box);

        string headerText = "";
        switch (currentChatMode)
        {
            case ChatType.Channel:
                headerText = $"# {activeChannel}";
                break;
            case ChatType.DirectMessage:
                var dmTarget = participants.FirstOrDefault(p => selectedParticipants.Contains(p.AuthorityId));
                headerText = $"💬 DM: {dmTarget.DisplayName}";
                break;
            case ChatType.GroupMessage:
                var groupNames = participants.Where(p => selectedParticipants.Contains(p.AuthorityId))
                                           .Select(p => p.DisplayName)
                                           .Take(3); // Show up to 3 names
                string nameList = string.Join(", ", groupNames);
                if (selectedParticipants.Count > 3)
                {
                    nameList += $" +{selectedParticipants.Count - 3} more";
                }
                headerText = $"👥 Group: {nameList}";
                break;
        }

        GUILayout.Label(headerText);

        // Add "Back to Channels" button when in DM or Group mode
        if (currentChatMode != ChatType.Channel)
        {
            if (GUILayout.Button("← Back", GUILayout.Width(60)))
            {
                SwitchToChannel(activeChannel); // Return to current active channel
            }
        }

        GUILayout.EndHorizontal();
    }

    private void DrawMessagesArea()
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUI.skin.box, GUILayout.Height(300));

        // Filter messages based on current view mode
        var messagesToDisplay = GetFilteredMessages();

        foreach (var message in messagesToDisplay)
        {
            DrawMessage(message);
        }

        GUILayout.EndScrollView();
    }

    private List<ChatMessage> GetFilteredMessages()
    {
        var filtered = new List<ChatMessage>();

        switch (currentChatMode)
        {
            case ChatType.Channel:
                // Show all messages for the active channel
                filtered = allMessages.Where(m =>
                    (m.Type == ChatType.Channel && m.ChannelName == activeChannel) ||
                    m.Type == ChatType.System).ToList();
                break;

            case ChatType.DirectMessage:
                // Show messages between us and the selected participant
                if (selectedParticipants.Count > 0)
                {
                    var otherPartyId = selectedParticipants.First();
                    filtered = allMessages.Where(m =>
                        m.Type == ChatType.DirectMessage &&
                        ((m.SenderId == localAuthorityId && m.Recipients.Contains(otherPartyId)) ||
                         (m.SenderId == otherPartyId && m.Recipients.Contains(localAuthorityId)))).ToList();
                }
                break;

            case ChatType.GroupMessage:
                // Show messages for this specific group combination
                if (selectedParticipants.Count > 1)
                {
                    var groupMembers = new HashSet<ushort>(selectedParticipants);
                    groupMembers.Add(localAuthorityId);

                    filtered = allMessages.Where(m =>
                        m.Type == ChatType.GroupMessage &&
                        m.Recipients != null &&
                        new HashSet<ushort>(m.Recipients).SetEquals(groupMembers)).ToList();
                }
                break;
        }

        return filtered;
    }

    private void DrawMessage(ChatMessage message)
    {
        GUILayout.BeginHorizontal();

        // Format timestamp
        var time = DateTimeOffset.FromUnixTimeSeconds(message.Timestamp).ToLocalTime();
        string timeStr = time.ToString("HH:mm");

        // Different styling for own messages
        bool isOwnMessage = message.SenderId == localAuthorityId;
        GUI.color = isOwnMessage ? new Color(0.8f, 0.8f, 1f) : Color.white;

        // Message content
        string msgText = $"[{timeStr}] {message.SenderName}: {message.Content}";

        if (message.Type == ChatType.System)
        {
            GUI.color = Color.yellow;
            msgText = $"[System] {message.Content}";
        }

        GUILayout.Label(msgText);

        GUI.color = Color.white;
        GUILayout.EndHorizontal();
    }

    private void DrawInputArea()
    {
        GUILayout.BeginHorizontal();

        // Use TextArea for multi-line support and give it most of the available width
        currentInputText = GUILayout.TextArea(currentInputText,
            GUILayout.MinHeight(25),
            GUILayout.MaxHeight(100),
            GUILayout.ExpandWidth(true),
            GUILayout.MinWidth(200));

        // Keep the Send button at fixed width and height, aligned to bottom of text area
        GUILayout.BeginVertical();
        GUILayout.FlexibleSpace(); // Push button to bottom

        if (GUILayout.Button("Send", GUILayout.Width(60), GUILayout.Height(25)) ||
            (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return && !string.IsNullOrEmpty(currentInputText)))
        {
            SendCurrentMessage();
        }

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    private void DrawNewChannelDialog()
    {
        Rect dialogRect = new Rect(windowRect.width / 2 - 100, windowRect.height / 2 - 50, 200, 100);
        GUI.Box(dialogRect, "Create New Channel");

        GUILayout.BeginArea(new Rect(dialogRect.x + 10, dialogRect.y + 25, 180, 70));

        newChannelName = GUILayout.TextField(newChannelName);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Create"))
        {
            if (!string.IsNullOrEmpty(newChannelName))
            {
                CallRpc(nameof(CreateChannel), newChannelName);
                showNewChannelDialog = false;
                newChannelName = "";
            }
        }

        if (GUILayout.Button("Cancel"))
        {
            showNewChannelDialog = false;
            newChannelName = "";
        }
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    #endregion

    #region RPCs - State Synchronization

    /// <summary>
    /// Registers a newly connected client in the participant list.
    ///
    /// NOTE: With persistent RPCs (IsPersistent = true), state synchronization is now
    /// automatic! Late-joining clients receive:
    /// - BroadcastParticipantUpdate: Current participant list
    /// - OnChannelCreated: All existing channels
    /// - Previous chat messages with persistent delivery
    ///
    /// This RPC now only handles participant registration, making the system more efficient.
    /// </summary>
    [ServerRpc(IsMineRequired = false)]
    internal void RegisterParticipant()
    {
        GONetRpcContext rpcContext = GONetEventBus.GetCurrentRpcContext();
        ushort requestingAuthority = rpcContext.SourceAuthorityId;

        // Ensure we have the requesting client in our list
        if (!participants.Any(p => p.AuthorityId == requestingAuthority))
        {
            string displayName = IsServerAuthorityId(requestingAuthority) ? "Server" : $"Player_{requestingAuthority}";
            participants.Add(new ChatParticipant
            {
                AuthorityId = requestingAuthority,
                DisplayName = displayName,
                IsServer = IsServerAuthorityId(requestingAuthority),
                Status = ConnectionStatus.Connected
            });

            // This will now be persistent and automatically sent to late-joining clients
            CallRpc(nameof(BroadcastParticipantUpdate), participants.ToArray());
        }
    }


    /// <summary>
    /// Demonstrates ClientRpc usage for server-to-all-clients broadcasting.
    /// This is the most efficient way to update all connected clients simultaneously.
    ///
    /// ClientRpc Features Demonstrated:
    /// - Automatic delivery to all connected clients
    /// - Complex data structure serialization (ChatParticipant array)
    /// - State reconciliation on client side
    /// - Preservation of local client state during updates
    ///
    /// This pattern is ideal for authoritative updates where the server
    /// maintains the canonical state and all clients need to synchronize.
    /// </summary>
    [ClientRpc(IsPersistent = true)]
    internal void BroadcastParticipantUpdate(ChatParticipant[] allParticipants)
    {
        // Server is broadcasting the authoritative participant list
        var ourself = participants.FirstOrDefault(p => p.AuthorityId == localAuthorityId);
        participants = allParticipants.ToList();

        // Ensure we're still in the list
        if (!participants.Any(p => p.AuthorityId == localAuthorityId) && ourself.AuthorityId != 0)
        {
            participants.Add(ourself);
        }
    }

    #endregion

    #region RPCs - Channel Management

    [ServerRpc(IsMineRequired = false)]
    internal void CreateChannel(string channelName)
    {
        // Validate channel name
        if (string.IsNullOrEmpty(channelName) || channels.Any(c => c.Name == channelName))
            return;

        GONetRpcContext rpcContext = GONetEventBus.GetCurrentRpcContext();

        var newChannel = new ChatChannel
        {
            Name = channelName,
            Description = $"Channel created by {GetParticipantName(rpcContext.SourceAuthorityId)}",
            CreatorId = rpcContext.SourceAuthorityId,
            CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        channels.Add(newChannel);

        // Notify all clients
        CallRpc(nameof(OnChannelCreated), newChannel);
    }

    [ClientRpc(IsPersistent = true)]
    internal void OnChannelCreated(ChatChannel channel)
    {
        if (!channels.Any(c => c.Name == channel.Name))
        {
            channels.Add(channel);
        }

        var sysMsg = new ChatMessage
        {
            Type = ChatType.System,
            Content = $"Channel '{channel.Name}' created",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        allMessages.Add(sysMsg);
        TrimMessageHistory();
    }

    #endregion

    #region RPCs - Unified Messaging

    /// <summary>
    /// Demonstrates advanced TargetRpc usage with custom validation and async delivery reports.
    /// This method showcases GONet's new RPC validation system with server-side content filtering.
    ///
    /// IMPORTANT: This TargetRpc is intentionally NOT persistent because:
    /// 1. Chat messages are transient - late-joining clients don't need old messages
    /// 2. TargetRpc with specific authority lists wouldn't work for late-joiners anyway
    /// 3. Message history is handled locally with TrimMessageHistory()
    /// 4. Only structural state (channels, participants) uses persistent RPCs
    ///
    /// Key Features Demonstrated:
    /// - TargetRpc with dynamic target list (CurrentMessageTargets property)
    /// - Custom validation method (ValidateMessage) that can modify content before delivery
    /// - Multi-parameter RPC (4 parameters) showing complex data transfer
    /// - Async RPC with delivery reports for handling failed deliveries
    /// - Server-side profanity filtering with web API integration
    ///
    /// The validation method can:
    /// - Allow/deny specific targets based on connection status
    /// - Modify message content (profanity filtering) before delivery
    /// - Provide detailed denial reasons for debugging
    ///
    /// This pattern is ideal for chat systems, notifications, or any scenario requiring
    /// validated content delivery to specific clients.
    /// </summary>
    /// <param name="content">The message content to send (may be modified by validation)</param>
    /// <param name="channelName">Target channel name for routing</param>
    /// <param name="messageType">Type of message (Channel, DirectMessage, GroupMessage)</param>
    /// <param name="fromUserId">Original sender's authority ID for proper attribution</param>
    /// <param name="recipients">Array of recipient authority IDs for proper message filtering</param>
    /// <returns>Delivery report indicating successful/failed deliveries</returns>
    //[TargetRpc(nameof(CurrentMessageTargets), isMultipleTargets: true, validationMethod: nameof(ValidateMessage))] // if you want synchronous version
    [TargetRpc(nameof(CurrentMessageTargets), isMultipleTargets: true, validationMethod: nameof(ValidateMessageAsync))]
    internal async Task<RpcDeliveryReport> SendMessage(string content, string channelName, ChatType messageType, ushort fromUserId, ushort[] recipients)
    {
        await Task.CompletedTask; // Suppress CS1998 warning - method returns synchronously

        // Get context - this should always be available in an RPC
        GONetRpcContext context = GONetEventBus.GetCurrentRpcContext();

        var message = new ChatMessage
        {
            SenderId = fromUserId, // Use the explicit fromUserId instead of context.SourceAuthorityId
            SenderName = GetParticipantName(fromUserId),
            Content = content,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Type = messageType,
            ChannelName = channelName,
            Recipients = recipients // Use the passed recipients array instead of CurrentMessageTargets
        };

        OnReceiveMessage(message);

        return default;
    }

    [TargetRpc(nameof(CurrentMessageTargets), isMultipleTargets: true, validationMethod: nameof(ValidateMessage_TEST_EMPTY))]
    internal async Task<RpcDeliveryReport> SendMessage_TEST()
    {
        await Task.CompletedTask; // Suppress CS1998 warning - method returns synchronously
        return default;
    }

    internal RpcValidationResult ValidateMessage_TEST_EMPTY()
    {
        var context = GONetMain.EventBus.GetValidationContext();
        if (!context.HasValue)
        {
            var result = RpcValidationResult.CreatePreAllocated(1);
            result.AllowAll();
            return result;
        }
        var validationResult = context.Value.GetValidationResult();
        validationResult.AllowAll();
        return validationResult;
    }

    [TargetRpc(nameof(CurrentMessageTargets), isMultipleTargets: true, validationMethod: nameof(ValidateMessage_TEST_EMPTY_VOID))]
    internal void SendMessage_TEST_VOID()
    {
    }

    internal RpcValidationResult ValidateMessage_TEST_EMPTY_VOID()
    {
        var context = GONetMain.EventBus.GetValidationContext();
        if (!context.HasValue)
        {
            var result = RpcValidationResult.CreatePreAllocated(1);
            result.AllowAll();
            return result;
        }
        var validationResult = context.Value.GetValidationResult();
        validationResult.AllowAll();
        return validationResult;
    }

    void OnReceiveMessage(ChatMessage message)
    {
        // Store ALL messages regardless of current view - filtering happens during display
        allMessages.Add(message);
        TrimMessageHistory();

        // Auto-switch to direct message mode when receiving a DM
        if (message.Type == ChatType.DirectMessage && message.SenderId != localAuthorityId)
        {
            // Automatically select the sender and switch to DM mode
            selectedParticipants.Clear();
            selectedParticipants.Add(message.SenderId);
            currentChatMode = ChatType.DirectMessage;

            // This ensures the UI immediately shows the DM conversation
            RefreshCurrentMessages();
        }
        // Auto-switch to group message mode when receiving a group message
        else if (message.Type == ChatType.GroupMessage && message.SenderId != localAuthorityId)
        {
            // Automatically select all participants in the group (excluding ourselves)
            selectedParticipants.Clear();
            if (message.Recipients != null)
            {
                foreach (ushort recipientId in message.Recipients)
                {
                    if (recipientId != localAuthorityId)
                    {
                        selectedParticipants.Add(recipientId);
                    }
                }
            }
            currentChatMode = ChatType.GroupMessage;

            // This ensures the UI immediately shows the group conversation
            RefreshCurrentMessages();
        }
    }

    /// <summary>
    /// Custom RPC validation method demonstrating GONet's new validation system capabilities.
    /// This method shows how to implement server-side validation with content modification.
    /// Synchronous version of <see cref="ValidateMessageAsync(string, string, ChatType, ushort, ushort[])"/>
    ///
    /// Validation Features Demonstrated:
    /// - Target authorization based on connection status
    /// - Content modification (profanity filtering) with ref parameters
    /// - Integration with web APIs for enhanced filtering
    /// - Memory-efficient bool array results using ArrayPool
    /// - Comprehensive error handling and fallback mechanisms
    ///
    /// The validation method receives ref parameters allowing modification of RPC data
    /// before delivery. This enables server-side filtering, data sanitization, or
    /// parameter transformation while maintaining type safety.
    ///
    /// Performance Notes:
    /// - Uses ArrayPool for memory efficiency with large target lists
    /// - Implements aggressive timeouts (1-2 seconds) for web API calls
    /// - Falls back to local filtering if web APIs are unavailable
    /// - Validation context provides pre-allocated result structures
    /// </summary>
    /// <param name="content">Message content (can be modified for filtering)</param>
    /// <param name="channelName">Channel name (can be validated/modified)</param>
    /// <param name="messageType">Message type (can be validated/modified)</param>
    /// <param name="fromUserId">Sender ID (can be validated/modified)</param>
    /// <param name="recipients">Recipients array (can be validated/modified)</param>
    /// <returns>Validation result with allowed targets and optional modifications</returns>
    internal RpcValidationResult ValidateMessage(ref string content, ref string channelName, ref ChatType messageType, ref ushort fromUserId, ref ushort[] recipients)
    {
        GONetLog.Info($"[SYNC VALIDATION] ValidateMessage ENTERED - content: '{content}', channel: '{channelName}', type: {messageType}, fromUser: {fromUserId}, recipients: {recipients?.Length ?? 0}");

        // Get the pre-allocated validation result from the context
        var validationContext = GONetMain.EventBus.GetValidationContext();
        if (!validationContext.HasValue)
        {
            GONetLog.Warning($"[SYNC VALIDATION] NO VALIDATION CONTEXT - returning allow-all");
            // Fallback if no validation context (shouldn't happen in normal flow)
            var resultAllow = RpcValidationResult.CreatePreAllocated(1);
            resultAllow.AllowAll();
            return resultAllow;
        }

        GONetLog.Info($"[SYNC VALIDATION] Got validation context, targetCount={validationContext.Value.TargetCount}");
        var result = validationContext.Value.GetValidationResult();

        // Check which targets are connected and set the bool array accordingly
        bool hasAnyDenied = false;
        var targetAuthorityIds = validationContext.Value.TargetAuthorityIds;

        for (int i = 0; i < validationContext.Value.TargetCount; i++)
        {
            ushort target = targetAuthorityIds[i];
            if (participants.Any(p => p.AuthorityId == target && p.Status == ConnectionStatus.Connected))
            {
                result.AllowedTargets[i] = true;
            }
            else
            {
                result.AllowedTargets[i] = false;
                hasAnyDenied = true;
            }
        }

        // Set denial reason if any targets were denied
        if (hasAnyDenied)
        {
            result.DenialReason = "Some recipients are not connected";
        }

        // Filter profanity from the message - try web API with short timeouts, then fall back
        try
        {
            string originalContent = content;
            GONetLog.Info($"[SYNC VALIDATION] Starting profanity filter for content: '{originalContent}'");

            // Use web API filtering with aggressive timeouts (max 2 seconds total)
            string filteredContent = FilterProfanityWithShortTimeout(content);

            GONetLog.Info($"[SYNC VALIDATION] Profanity filter returned: '{filteredContent}' (original: '{originalContent}', modified: {filteredContent != originalContent})");

            if (filteredContent != originalContent)
            {
                GONetLog.Info($"[SYNC VALIDATION] CONTENT WAS MODIFIED - setting ref parameter");
                // Message was modified, need to serialize the modified data
                content = filteredContent;
                result.WasModified = true;

                // Note: In a real implementation, you would need to serialize the modified
                // RPC data structure with the filtered content and set result.ModifiedData
                // This requires knowing the exact RPC data structure format
            }
        }
        catch (Exception ex)
        {
            GONetLog.Error($"[SYNC VALIDATION] Failed to filter message content: {ex.Message}\nStack: {ex.StackTrace}");
        }

        GONetLog.Info($"[SYNC VALIDATION] ValidateMessage RETURNING - WasModified={result.WasModified}");
        return result;
    }

    /// <summary>
    /// ASYNC version of <see cref="ValidateMessage(ref string, ref string, ref ChatType, ref ushort, ref ushort[])"/> - 
    ///    demonstrates non-blocking RPC validation with async/await.
    /// This method performs the same validation as ValidateMessage but without blocking the Unity main thread.
    ///
    /// Key Differences from Sync Version:
    /// - Returns Task&lt;RpcValidationResult&gt; instead of RpcValidationResult
    /// - Parameters do NOT use 'ref' keyword (C# async limitation)
    /// - Uses SetValidatedOverride(index, value) API to modify parameters
    /// - Can perform async I/O without blocking (profanity API calls, database lookups, etc.)
    ///
    /// CRITICAL: When async validators modify parameters, they MUST use SetValidatedOverride()
    /// because 'ref' parameters are not allowed in async methods. The framework will serialize
    /// all parameters (original + overrides) after validation completes.
    ///
    /// Performance Comparison:
    /// - Sync validator with Thread.Sleep(2000): Blocks Unity for 2 seconds (FREEZES GAME)
    /// - Async validator with await Task.Delay(2000): Non-blocking, game runs smoothly
    ///
    /// Use Cases for Async Validators:
    /// - Web API calls (profanity filtering, content moderation)
    /// - Database lookups (permissions, ban lists, player data)
    /// - File I/O operations (logging, audit trails)
    /// - Cryptographic operations (signature verification, encryption)
    /// - Any operation that would block the main thread in sync version
    ///
    /// To use this async validator instead of the sync one, change the TargetRpc attribute:
    /// [TargetRpc(nameof(CurrentMessageTargets), isMultipleTargets: true,
    ///            validationMethod: nameof(ValidateMessageAsync))]
    /// </summary>
    /// <param name="content">Message content (index 0 - modify via SetValidatedOverride)</param>
    /// <param name="channelName">Channel name (index 1)</param>
    /// <param name="messageType">Message type (index 2)</param>
    /// <param name="fromUserId">Sender ID (index 3)</param>
    /// <param name="recipients">Recipients array (index 4)</param>
    /// <returns>Task that completes with validation result</returns>
    internal async Task<RpcValidationResult> ValidateMessageAsync(
        string content,        // [0] - No 'ref' keyword!
        string channelName,    // [1]
        ChatType messageType,  // [2]
        ushort fromUserId,     // [3]
        ushort[] recipients)   // [4]
    {
        GONetLog.Info($"[CHAT VALIDATION] ValidateMessageAsync ENTERED - content: '{content}', channel: '{channelName}', type: {messageType}, fromUser: {fromUserId}, recipients: {recipients?.Length ?? 0}");

        // Get the pre-allocated validation result from the context
        var validationContext = GONetMain.EventBus.GetValidationContext();
        if (!validationContext.HasValue)
        {
            GONetLog.Warning($"[CHAT VALIDATION] NO VALIDATION CONTEXT - returning allow-all");
            // Fallback if no validation context (shouldn't happen in normal flow)
            var resultAllow = RpcValidationResult.CreatePreAllocated(1);
            resultAllow.AllowAll();
            return resultAllow;
        }

        GONetLog.Info($"[CHAT VALIDATION] Got validation context, targetCount={validationContext.Value.TargetCount}");
        var result = validationContext.Value.GetValidationResult();

        // Check which targets are connected and set the bool array accordingly
        bool hasAnyDenied = false;
        var targetAuthorityIds = validationContext.Value.TargetAuthorityIds;

        for (int i = 0; i < validationContext.Value.TargetCount; i++)
        {
            ushort target = targetAuthorityIds[i];
            if (participants.Any(p => p.AuthorityId == target && p.Status == ConnectionStatus.Connected))
            {
                result.AllowedTargets[i] = true;
            }
            else
            {
                result.AllowedTargets[i] = false;
                hasAnyDenied = true;
            }
        }

        // Set denial reason if any targets were denied
        if (hasAnyDenied)
        {
            result.DenialReason = "Some recipients are not connected";
        }

        // ASYNC profanity filtering - THIS IS NON-BLOCKING!
        try
        {
            string originalContent = content;
            GONetLog.Info($"[CHAT VALIDATION] Starting async profanity filter for content: '{originalContent}'");

            // Perform async profanity filtering (non-blocking)
            string filteredContent = await FilterProfanityAsync(originalContent);

            GONetLog.Info($"[CHAT VALIDATION] Profanity filter returned: '{filteredContent}' (original: '{originalContent}', modified: {filteredContent != originalContent})");

            if (filteredContent != originalContent)
            {
                GONetLog.Info($"[CHAT VALIDATION] CONTENT WAS MODIFIED - using SetValidatedOverride");
                // Message was modified - use SetValidatedOverride API
                result.SetValidatedOverride(0, filteredContent);  // Parameter index 0 = content

                // Note: result.WasModified is automatically set to true by SetValidatedOverride()
                // The framework will serialize all params (original + overrides) into ModifiedData
            }
            else
            {
                GONetLog.Info($"[CHAT VALIDATION] Content was NOT modified (clean message)");
            }
        }
        catch (Exception ex)
        {
            GONetLog.Error($"[CHAT VALIDATION] Failed to filter message content (async): {ex.Message}");
        }

        GONetLog.Info($"[CHAT VALIDATION] ValidateMessageAsync RETURNING - WasModified={result.WasModified}");
        return result;
    }

    /// <summary>
    /// Async profanity filter using web APIs without blocking Unity main thread.
    /// This demonstrates proper async/await patterns for I/O-bound operations.
    /// </summary>
    private async Task<string> FilterProfanityAsync(string input)
    {
        if (!GONetMain.IsServer)
            return input;

        // Try PurgoMalum API first (with timeout)
        try
        {
            string result = await TryPurgoMalumAsync(input);
            if (result != null)
            {
                return result;
            }
        }
        catch (Exception ex)
        {
            GONetLog.Warning($"[Server] PurgoMalum async failed: {ex.Message}");
        }

        // Try profanity.dev as backup (with timeout)
        try
        {
            string result = await TryProfanityDevAsync(input);
            if (result != null)
            {
                return result;
            }
        }
        catch (Exception ex)
        {
            GONetLog.Warning($"[Server] Profanity.dev async failed: {ex.Message}");
        }

        // Fall back to local filtering
        return FilterProfanityLocal(input);
    }

    private async Task<string> TryPurgoMalumAsync(string input)
    {
        try
        {
            string url = $"https://www.purgomalum.com/service/plain?text={UnityWebRequest.EscapeURL(input)}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 2; // 2 second timeout

                // ASYNC web request - does NOT block Unity main thread!
                var operation = request.SendWebRequest();
                await operation; // Await the async operation

                if (request.result == UnityWebRequest.Result.Success)
                {
                    return request.downloadHandler.text;
                }
                else
                {
                    GONetLog.Warning($"[Server] PurgoMalum async request failed: {request.error}");
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            GONetLog.Warning($"[Server] PurgoMalum async exception: {ex.Message}");
            return null;
        }
    }

    private async Task<string> TryProfanityDevAsync(string input)
    {
        try
        {
            string jsonPayload = $"{{\"message\":\"{input.Replace("\"", "\\\"")}\"}}";

            using (UnityWebRequest request = new UnityWebRequest("https://vector.profanity.dev", "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 2; // 2 second timeout

                // ASYNC web request - does NOT block Unity main thread!
                var operation = request.SendWebRequest();
                await operation; // Await the async operation

                if (request.result == UnityWebRequest.Result.Success)
                {
                    // Parse JSON response - simple parsing for now
                    string response = request.downloadHandler.text;
                    if (response.Contains("\""))
                    {
                        var start = response.IndexOf("\"") + 1;
                        var end = response.LastIndexOf("\"");
                        if (end > start)
                        {
                            return response.Substring(start, end - start);
                        }
                    }
                    return input; // If we can't parse response, return original (assume clean)
                }
                else
                {
                    GONetLog.Warning($"[Server] Profanity.dev async request failed: {request.error}");
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            GONetLog.Warning($"[Server] Profanity.dev async exception: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region Helper Methods

    private void SendCurrentMessage()
    {
        if (string.IsNullOrEmpty(currentInputText))
            return;

        GONetLog.Info($"[CHAT-DEBUG] SendCurrentMessage called. Mode: {currentChatMode}, Participants count: {participants.Count}, GONetId: {gonetParticipant?.GONetId ?? 0}, IsMine: {gonetParticipant?.IsMine ?? false}");

        // Let the server-side validator handle ALL filtering (no client-side pre-filtering)
        string finalContent = currentInputText;

        // Set up targets based on mode
        HashSet<ushort> uniqueTargets = new HashSet<ushort>(); // Use HashSet to prevent duplicates

        switch (currentChatMode)
        {
            case ChatType.Channel:
                // For channels, target all participants
                foreach (var p in participants)
                {
                    uniqueTargets.Add(p.AuthorityId);
                    GONetLog.Info($"[CHAT-DEBUG] Adding participant {p.AuthorityId} to targets (Status: {p.Status})");
                }
                break;

            case ChatType.DirectMessage:
            case ChatType.GroupMessage:
                // For DM/Group, use selected participants plus ourselves
                foreach (var target in selectedParticipants)
                {
                    uniqueTargets.Add(target);
                }
                uniqueTargets.Add(localAuthorityId); // Add ourselves
                break;
        }

        CurrentMessageTargets = uniqueTargets.ToList();

        GONetLog.Info($"[CHAT-DEBUG] Calling SendMessage RPC with {uniqueTargets.Count} targets: [{string.Join(", ", uniqueTargets)}]");

        // Example of async RPC calling with delivery report handling
        // This demonstrates the new CallRpcAsync<TReturn, T1, T2, T3, T4, T5> pattern for complex RPCs
        CallRpcAsync<RpcDeliveryReport, string, string, ChatType, ushort, ushort[]>(
            nameof(SendMessage),
            finalContent,
            activeChannel,
            currentChatMode,
            localAuthorityId,
            uniqueTargets.ToArray()) // Pass the actual recipients array
            .ContinueWith(task =>
            {
                // Handle delivery failures gracefully - common in networked environments
                if (task.Result.FailedDelivery?.Length > 0)
                {
                    GONetLog.Warning($"[CHAT-DEBUG] Failed to deliver to some recipients: {task.Result.FailureReason}");
                }
                else
                {
                    GONetLog.Info($"[CHAT-DEBUG] SendMessage RPC completed successfully");
                }
            });

        currentInputText = "";
    }

    private void SwitchToChannel(string channelName)
    {
        activeChannel = channelName;
        currentChatMode = ChatType.Channel;
        selectedParticipants.Clear(); // Clear any selected participants when returning to channel view
        RefreshCurrentMessages();
    }

    private void StartDirectMessage(ushort participantId)
    {
        selectedParticipants.Clear();
        selectedParticipants.Add(participantId);
        currentChatMode = ChatType.DirectMessage;
        RefreshCurrentMessages();
    }

    private void StartGroupMessage()
    {
        // This should only be called for 2+ participants
        currentChatMode = ChatType.GroupMessage;
        RefreshCurrentMessages();
    }

    private void RefreshCurrentMessages()
    {
        // Messages are filtered dynamically in GetFilteredMessages() during display
        // No need to clear or modify allMessages here - we want to preserve all history
    }

    private bool IsServerAuthorityId(ushort authorityId)
    {
        // Use GONet's server authority ID constant
        return authorityId == GONetMain.OwnerAuthorityId_Server;
    }

    private string GetParticipantName(ushort authorityId)
    {
        if (IsServerAuthorityId(authorityId))
        {
            return "Server";
        }

        var participant = participants.FirstOrDefault(p => p.AuthorityId == authorityId);
        return string.IsNullOrEmpty(participant.DisplayName) ? $"Player_{authorityId}" : participant.DisplayName;
    }

    private Color GetStatusColor(ConnectionStatus status)
    {
        switch (status)
        {
            case ConnectionStatus.Connected: return Color.green;
            case ConnectionStatus.Away: return Color.yellow;
            case ConnectionStatus.Busy: return Color.red;
            case ConnectionStatus.Offline: return Color.gray;
            default: return Color.white;
        }
    }

    private int GetUnreadMessageCount()
    {
        // In a real implementation, track read/unread status
        return 0;
    }

    private void TrimMessageHistory()
    {
        // Keep only last 500 messages total (increased for persistence)
        if (allMessages.Count > 500)
        {
            allMessages.RemoveRange(0, allMessages.Count - 500);
        }
    }

    private string FilterProfanityLocal(string input)
    {
        if (!GONetMain.IsServer)
            return input;

        string filtered = input;
        foreach (var replacement in profanityReplacements)
        {
            string pattern = $@"\b{Regex.Escape(replacement.Key)}\b";
            filtered = Regex.Replace(filtered, pattern, replacement.Value, RegexOptions.IgnoreCase);
        }

        return filtered;
    }

    // Synchronous wrapper for backwards compatibility
    private string FilterProfanity(string input)
    {
        // For synchronous calls, use local filtering only
        return FilterProfanityLocal(input);
    }

    // Synchronous web API filtering with aggressive timeouts (max 2 seconds total)
    private string FilterProfanityWithShortTimeout(string input)
    {
        GONetLog.Info($"[PROFANITY FILTER] FilterProfanityWithShortTimeout called, IsServer={GONetMain.IsServer}, input='{input}'");

        if (!GONetMain.IsServer)
        {
            GONetLog.Info($"[PROFANITY FILTER] Not server - returning input unchanged");
            return input;
        }

        // Try PurgoMalum first with synchronous approach
        try
        {
            GONetLog.Info($"[PROFANITY FILTER] Attempting PurgoMalum sync...");
            string result = TryPurgoMalumSync(input);
            if (result != null)
            {
                GONetLog.Info($"[PROFANITY FILTER] PurgoMalum sync SUCCESS - result: '{result}'");
                return result;
            }
            GONetLog.Info($"[PROFANITY FILTER] PurgoMalum sync returned null - trying next service");
        }
        catch (Exception ex)
        {
            GONetLog.Warning($"[PROFANITY FILTER] PurgoMalum sync EXCEPTION: {ex.Message}");
        }

        // Try profanity.dev as backup
        try
        {
            GONetLog.Info($"[PROFANITY FILTER] Attempting Profanity.dev sync...");
            string result = TryProfanityDevSync(input);
            if (result != null)
            {
                GONetLog.Info($"[PROFANITY FILTER] Profanity.dev sync SUCCESS - result: '{result}'");
                return result;
            }
            GONetLog.Info($"[PROFANITY FILTER] Profanity.dev sync returned null - falling back to local");
        }
        catch (Exception ex)
        {
            GONetLog.Warning($"[PROFANITY FILTER] Profanity.dev sync EXCEPTION: {ex.Message}");
        }

        // Fall back to local filtering
        GONetLog.Info($"[PROFANITY FILTER] Using LOCAL filtering as fallback");
        string localResult = FilterProfanityLocal(input);
        GONetLog.Info($"[PROFANITY FILTER] Local filter result: '{localResult}'");
        return localResult;
    }

    private string TryPurgoMalumSync(string input)
    {
        try
        {
            string url = $"https://www.purgomalum.com/service/plain?text={UnityWebRequest.EscapeURL(input)}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 1; // 1 second timeout

                // Use synchronous approach - start the request and poll until done or timeout
                var operation = request.SendWebRequest();

                float startTime = Time.realtimeSinceStartup;
                while (!operation.isDone && (Time.realtimeSinceStartup - startTime) < 1.0f)
                {
                    // Busy wait with small delay to avoid spinning
                    System.Threading.Thread.Sleep(10);
                }

                if (operation.isDone && request.result == UnityWebRequest.Result.Success)
                {
                    return request.downloadHandler.text;
                }
                else
                {
                    GONetLog.Warning($"[Server] PurgoMalum sync request failed or timed out");
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            GONetLog.Warning($"[Server] PurgoMalum sync exception: {ex.Message}");
            return null;
        }
    }

    private string TryProfanityDevSync(string input)
    {
        try
        {
            string jsonPayload = $"{{\"message\":\"{input.Replace("\"", "\\\"")}\"}}";

            using (UnityWebRequest request = new UnityWebRequest("https://vector.profanity.dev", "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 1; // 1 second timeout

                // Use synchronous approach - start the request and poll until done or timeout
                var operation = request.SendWebRequest();

                float startTime = Time.realtimeSinceStartup;
                while (!operation.isDone && (Time.realtimeSinceStartup - startTime) < 1.0f)
                {
                    // Busy wait with small delay to avoid spinning
                    System.Threading.Thread.Sleep(10);
                }

                if (operation.isDone && request.result == UnityWebRequest.Result.Success)
                {
                    // Parse JSON response - simple parsing for now
                    string response = request.downloadHandler.text;
                    if (response.Contains("\""))
                    {
                        var start = response.IndexOf("\"") + 1;
                        var end = response.LastIndexOf("\"");
                        if (end > start)
                        {
                            return response.Substring(start, end - start);
                        }
                    }
                    return input; // If we can't parse response, return original (assume clean)
                }
                else
                {
                    GONetLog.Warning($"[Server] Profanity.dev sync request failed or timed out");
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            GONetLog.Warning($"[Server] Profanity.dev sync exception: {ex.Message}");
            return null;
        }
    }

    #endregion
}

/// <summary>
/// Data structures for the chat system demonstrating GONet RPC serialization best practices.
/// These structures showcase MemoryPack serialization for efficient network transfer.
///
/// Key Design Patterns:
/// - Structs for value semantics and performance
/// - MemoryPackable attribute for zero-allocation serialization
/// - Partial classes for source generator compatibility
/// - Namespace-level definitions for proper accessibility
/// - Minimal data footprint for network efficiency
///
/// These patterns ensure optimal performance for frequently transmitted RPC data.
/// </summary>
[MemoryPackable]
public partial struct ChatMessage
{
    public ushort SenderId { get; set; }
    public string SenderName { get; set; }
    public string Content { get; set; }
    public long Timestamp { get; set; }
    public GONetSampleChatSystem.ChatType Type { get; set; }
    public string ChannelName { get; set; }
    public ushort[] Recipients { get; set; }
}

// Extension methods for async UnityWebRequest
public static class UnityWebRequestExtensions
{
    public static TaskAwaiter<UnityWebRequest> GetAwaiter(this UnityWebRequestAsyncOperation asyncOp)
    {
        var tcs = new TaskCompletionSource<UnityWebRequest>();
        asyncOp.completed += operation => tcs.SetResult(asyncOp.webRequest);
        return tcs.Task.GetAwaiter();
    }
}

[MemoryPackable]
public partial struct ChatParticipant
{
    public ushort AuthorityId { get; set; }
    public string DisplayName { get; set; }
    public bool IsServer { get; set; }
    public GONetSampleChatSystem.ConnectionStatus Status { get; set; }
}

[MemoryPackable]
public partial struct ChatChannel
{
    public string Name { get; set; }
    public string Description { get; set; }
    public ushort CreatorId { get; set; }
    public long CreatedTimestamp { get; set; }
}