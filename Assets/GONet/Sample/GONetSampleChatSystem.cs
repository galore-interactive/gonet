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
/// A comprehensive chat system for GONet that supports channels, direct messages, 
/// and group conversations with server-side profanity filtering and validation.
/// Designed to be placed on the GONet Global GameObject and tracks GONetLocal objects.
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
    private Rect windowRect = new Rect(10, 10, 400, 500);
    private Rect collapsedRect = new Rect(10, 10, 200, 30);

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

    public override void OnGONetParticipantStarted()
    {
        base.OnGONetParticipantStarted();

        // Now we have a valid GONetId and OwnerAuthorityId
        localAuthorityId = GONetMain.MyAuthorityId;
        localDisplayName = GONetMain.IsServer ? "Server" : $"Player_{localAuthorityId}";

        // Don't add ourselves here - let OnGONetParticipantEnabled handle it

        // If client, request current state from server after a short delay
        if (!GONetMain.IsServer)
        {
            StartCoroutine(RequestStateAfterDelay());
        }
    }

    private IEnumerator RequestStateAfterDelay()
    {
        yield return new WaitForSeconds(0.5f); // Give time for all participants to be detected
        CallRpc(nameof(RequestCurrentState));
    }

    public override void OnGONetParticipantEnabled(GONetParticipant gonetParticipant)
    {
        base.OnGONetParticipantEnabled(gonetParticipant);

        // Check if this participant has a GONetLocal component (represents a player)
        if (gonetParticipant.TryGetComponent(out GONetLocal gonetLocal))
        {
            ushort authorityId = gonetLocal.OwnerAuthorityId;

            // Skip if already tracked
            if (participants.Any(p => p.AuthorityId == authorityId))
                return;

            string displayName = authorityId == 0 ? "Server" : $"Player_{authorityId}";

            var newParticipant = new ChatParticipant
            {
                AuthorityId = authorityId,
                DisplayName = displayName,
                IsServer = authorityId == 0,
                Status = ConnectionStatus.Connected
            };

            participants.Add(newParticipant);

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

            if (GUILayout.Button($"# {channel.Name}", GUILayout.Height(20)))
            {
                SwitchToChannel(channel.Name);
            }
        }

        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("+ New Channel", GUILayout.Height(20)))
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

        // Group message button
        if (selectedParticipants.Count > 0)
        {
            if (GUILayout.Button($"Message Selected ({selectedParticipants.Count})"))
            {
                StartGroupMessage();
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
                headerText = $"DM: {dmTarget.DisplayName}";
                break;
            case ChatType.GroupMessage:
                headerText = $"Group ({selectedParticipants.Count} members)";
                break;
        }

        GUILayout.Label(headerText);
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

    [ServerRpc(IsMineRequired = false)]
    internal void RequestCurrentState()
    {
        GONetRpcContext rpcContext = GONetEventBus.GetCurrentRpcContext();
        ushort requestingAuthority = rpcContext.SourceAuthorityId;

        // Ensure we have the requesting client in our list
        if (!participants.Any(p => p.AuthorityId == requestingAuthority))
        {
            string displayName = requestingAuthority == 0 ? "Server" : $"Player_{requestingAuthority}";
            participants.Add(new ChatParticipant
            {
                AuthorityId = requestingAuthority,
                DisplayName = displayName,
                IsServer = requestingAuthority == 0,
                Status = ConnectionStatus.Connected
            });

            // Broadcast the updated list to ALL clients
            CallRpc(nameof(BroadcastParticipantUpdate), participants.ToArray());
        }

        // Send current state to the requesting client only
        CurrentSingleTarget = requestingAuthority;
        CallRpc(nameof(ReceiveCurrentState), participants.ToArray(), channels.ToArray());
    }

    [TargetRpc(nameof(CurrentSingleTarget), isMultipleTargets: false)]
    internal void ReceiveCurrentState(ChatParticipant[] allParticipants, ChatChannel[] allChannels)
    {
        // Update our local state with server's state
        // Keep ourselves in the list but update everyone else
        var ourself = participants.FirstOrDefault(p => p.AuthorityId == localAuthorityId);

        participants = allParticipants.ToList();

        // Make sure we're in the list (in case server doesn't have us yet)
        if (!participants.Any(p => p.AuthorityId == localAuthorityId) && ourself.AuthorityId != 0)
        {
            participants.Add(ourself);
        }

        channels = allChannels.ToList();
    }

    [ClientRpc]
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

    [ClientRpc]
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

    // Unified message sending using TargetRpc for all message types
    [TargetRpc(nameof(CurrentMessageTargets), isMultipleTargets: true, validationMethod: nameof(ValidateMessage))]
    internal async Task<RpcDeliveryReport> SendMessage(string content, string channelName, ChatType messageType, ushort fromUserId)
    {
        // Get context - this should always be available in an RPC
        GONetRpcContext context = GONetEventBus.GetCurrentRpcContext();

        Debug.Log($"[{(GONetMain.IsServer ? "Server" : "Client")}] Received SendMessage RPC from {context.SourceAuthorityId}, originally from user {fromUserId}");

        var message = new ChatMessage
        {
            SenderId = fromUserId, // Use the explicit fromUserId instead of context.SourceAuthorityId
            SenderName = GetParticipantName(fromUserId),
            Content = content,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Type = messageType,
            ChannelName = channelName,
            Recipients = CurrentMessageTargets.ToArray()
        };

        OnReceiveMessage(message);

        return default;
    }

    [TargetRpc(nameof(CurrentMessageTargets), isMultipleTargets: true, validationMethod: nameof(ValidateMessage_TEST_EMPTY))]
    internal async Task<RpcDeliveryReport> SendMessage_TEST()
    {
        return default;
    }

    internal RpcValidationResult ValidateMessage_TEST_EMPTY()
    {
        // Get the pre-allocated validation result and allow all targets
        var validationContext = GONetEventBus.GetCurrentRpcContext().ValidationContext;
        var result = validationContext.GetValidationResult();
        result.AllowAll();
        return result;
    }

    [TargetRpc(nameof(CurrentMessageTargets), isMultipleTargets: true, validationMethod: nameof(ValidateMessage_TEST_EMPTY_VOID))]
    internal void SendMessage_TEST_VOID()
    {
    }

    internal RpcValidationResult ValidateMessage_TEST_EMPTY_VOID()
    {
        // Get the pre-allocated validation result and allow all targets
        var validationContext = GONetEventBus.GetCurrentRpcContext().ValidationContext;
        var result = validationContext.GetValidationResult();
        result.AllowAll();
        return result;
    }

    void OnReceiveMessage(ChatMessage message)
    {
        // Determine if we should display this message based on current view
        bool shouldDisplay = false;

        switch (message.Type)
        {
            case ChatType.Channel:
                shouldDisplay = (currentChatMode == ChatType.Channel && activeChannel == message.ChannelName);
                break;

            case ChatType.DirectMessage:
                if (currentChatMode == ChatType.DirectMessage)
                {
                    shouldDisplay = selectedParticipants.Contains(message.SenderId) ||
                                  message.SenderId == localAuthorityId;
                }
                break;

            case ChatType.GroupMessage:
                if (currentChatMode == ChatType.GroupMessage)
                {
                    shouldDisplay = message.Recipients.Any(r => selectedParticipants.Contains(r)) ||
                                  message.SenderId == localAuthorityId;
                }
                break;
        }

        if (shouldDisplay)
        {
            allMessages.Add(message);
            TrimMessageHistory();
        }
    }

    // Server-side validation for all messages
    internal RpcValidationResult ValidateMessage(ref string content, ref string channelName, ref ChatType messageType, ref ushort fromUserId)
    {
        // Get the pre-allocated validation result from the context
        var validationContext = GONetMain.EventBus.GetValidationContext();
        if (!validationContext.HasValue)
        {
            // Fallback if no validation context (shouldn't happen in normal flow)
            var resultAllow = RpcValidationResult.CreatePreAllocated(1);
            resultAllow.AllowAll();
            return resultAllow;
        }

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
            // Use web API filtering with aggressive timeouts (max 2 seconds total)
            string filteredContent = FilterProfanityWithShortTimeout(content);

            if (filteredContent != originalContent)
            {
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
            Debug.LogError($"Failed to filter message content: {ex.Message}");
        }

        return result;
    }

    #endregion

    #region Helper Methods

    private void SendCurrentMessage()
    {
        if (string.IsNullOrEmpty(currentInputText))
            return;

        // Filter profanity locally if server (client-side preview only - real filtering happens server-side)
        string finalContent = GONetMain.IsServer ? FilterProfanity(currentInputText) : currentInputText;

        // Set up targets based on mode
        HashSet<ushort> uniqueTargets = new HashSet<ushort>(); // Use HashSet to prevent duplicates

        switch (currentChatMode)
        {
            case ChatType.Channel:
                // For channels, target all participants
                foreach (var p in participants)
                {
                    uniqueTargets.Add(p.AuthorityId);
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

        // Debug logging to help diagnose the issue
        Debug.Log($"[{(GONetMain.IsServer ? "Server" : "Client")}] Sending message to {CurrentMessageTargets.Count} targets: {string.Join(", ", CurrentMessageTargets)}");

        // Send using unified message system
        CallRpcAsync<RpcDeliveryReport, string, string, ChatType, ushort>(
            nameof(SendMessage),
            finalContent,
            activeChannel,
            currentChatMode,
            localAuthorityId)
            .ContinueWith(task =>
            {
                if (task.Result.FailedDelivery?.Length > 0)
                {
                    Debug.LogWarning($"Failed to deliver to some recipients: {task.Result.FailureReason}");
                }

                Debug.Log($"[{(GONetMain.IsServer ? "Server" : "Client")}] Message delivery report - Delivered to: {string.Join(", ", task.Result.DeliveredTo ?? new ushort[0])}");
            });

        currentInputText = "";
    }

    private void SwitchToChannel(string channelName)
    {
        activeChannel = channelName;
        currentChatMode = ChatType.Channel;
        selectedParticipants.Clear();
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
        if (selectedParticipants.Count > 1)
        {
            currentChatMode = ChatType.GroupMessage;
            RefreshCurrentMessages();
        }
    }

    private void RefreshCurrentMessages()
    {
        // In a real implementation, you'd filter messages based on current view
        // For now, we'll just clear for demonstration
        allMessages.Clear();
    }

    private string GetParticipantName(ushort authorityId)
    {
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

    private async Task<string> FilterProfanityAsync(string input)
    {
        if (!GONetMain.IsServer)
            return input;

        // Try web API first, then fall back to local filtering
        string filtered = await TryWebProfanityFilter(input);
        if (filtered != null)
        {
            Debug.Log($"[Server] Used web API profanity filter");
            return filtered;
        }

        // Fallback to local profanity filtering
        Debug.Log($"[Server] Falling back to local profanity filter");
        return FilterProfanityLocal(input);
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

    private async Task<string> TryWebProfanityFilter(string input)
    {
        try
        {
            // First try PurgoMalum API (free, established service)
            string result = await TryPurgoMalumFilter(input);
            if (result != null) return result;

            // If PurgoMalum fails, try profanity.dev as backup
            result = await TryProfanityDevFilter(input);
            if (result != null) return result;

            return null; // All web APIs failed
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Server] Web profanity filter failed: {ex.Message}");
            return null;
        }
    }

    private async Task<string> TryPurgoMalumFilter(string input)
    {
        try
        {
            string url = $"https://www.purgomalum.com/service/plain?text={UnityWebRequest.EscapeURL(input)}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 1; // 1 second timeout
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    return request.downloadHandler.text;
                }
                else
                {
                    Debug.LogWarning($"[Server] PurgoMalum API failed: {request.error}");
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Server] PurgoMalum API exception: {ex.Message}");
            return null;
        }
    }

    private async Task<string> TryProfanityDevFilter(string input)
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

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    // Parse JSON response - assuming it returns filtered text
                    string response = request.downloadHandler.text;
                    // Simple JSON parsing for now - in production you'd want proper JSON parsing
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
                    Debug.LogWarning($"[Server] Profanity.dev API failed: {request.error}");
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Server] Profanity.dev API exception: {ex.Message}");
            return null;
        }
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
        if (!GONetMain.IsServer)
            return input;

        // Try PurgoMalum first with synchronous approach
        try
        {
            string result = TryPurgoMalumSync(input);
            if (result != null)
            {
                Debug.Log($"[Server] Used PurgoMalum API for profanity filtering");
                return result;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Server] PurgoMalum sync failed: {ex.Message}");
        }

        // Try profanity.dev as backup
        try
        {
            string result = TryProfanityDevSync(input);
            if (result != null)
            {
                Debug.Log($"[Server] Used Profanity.dev API for profanity filtering");
                return result;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Server] Profanity.dev sync failed: {ex.Message}");
        }

        // Fall back to local filtering
        Debug.Log($"[Server] Web APIs failed, using local profanity filter");
        return FilterProfanityLocal(input);
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
                    Debug.LogWarning($"[Server] PurgoMalum sync request failed or timed out");
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Server] PurgoMalum sync exception: {ex.Message}");
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
                    Debug.LogWarning($"[Server] Profanity.dev sync request failed or timed out");
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Server] Profanity.dev sync exception: {ex.Message}");
            return null;
        }
    }

    #endregion
}

// Data structures need to be at namespace level for MemoryPack
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