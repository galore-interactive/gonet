using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using GONet;
using MemoryPack;

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
    private List<ChatMessage> currentMessages = new List<ChatMessage>();
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

    // Track our identity but DON'T add to participants yet
    public override void OnGONetClientVsServerStatusKnown(bool isClient, bool isServer, ushort myAuthorityId)
    {
        base.OnGONetClientVsServerStatusKnown(isClient, isServer, myAuthorityId);

        if (isClient)
        {
            StartCoroutine(RequestStateAfterDelay());
        }
    }

    private IEnumerator RequestStateAfterDelay()
    {
        yield return new WaitForSeconds(0.5f); // Give time for all participants to be detected
        CallRpc(nameof(RequestCurrentState));
    }

    // ALL participants get added here, including ourselves
    public override void OnGONetParticipantEnabled(GONetParticipant gonetParticipant)
    {
        base.OnGONetParticipantEnabled(gonetParticipant);

        if (!gonetParticipant.TryGetComponent(out GONetLocal gonetLocal))
            return;

        ushort authorityId = gonetLocal.OwnerAuthorityId;
        if  (gonetParticipant.IsMine)
        {
            localAuthorityId = gonetLocal.OwnerAuthorityId;
        }

        if (authorityId == GONetMain.OwnerAuthorityId_Unset)
        {
            Debug.LogError($"[GONetChatSystem] OnGONetParticipantEnabled with unset authority!");
            return;
        }

        if (participants.Any(p => p.AuthorityId == authorityId))
            return;

        // Determine display name based on whether this participant is the server
        bool isThisParticipantServer = (GONetMain.IsServer && authorityId == localAuthorityId);
        string displayName = isThisParticipantServer ? "Server" : $"Player_{authorityId}";

        var newParticipant = new ChatParticipant
        {
            AuthorityId = authorityId,
            DisplayName = displayName,
            IsServer = isThisParticipantServer,
            Status = ConnectionStatus.Connected
        };

        participants.Add(newParticipant);

        // Show join message (skip for ourselves on initial load)
        if (authorityId != localAuthorityId)
        {
            var sysMsg = new ChatMessage
            {
                Type = ChatType.System,
                Content = $"{displayName} joined",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            currentMessages.Add(sysMsg);
            TrimMessageHistory();
        }

        // Server broadcasts the updated list to all clients
        if (GONetMain.IsServer)
        {
            CallRpc(nameof(BroadcastParticipantUpdate), participants.ToArray());
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
                currentMessages.Add(sysMsg);
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

        foreach (var message in currentMessages)
        {
            DrawMessage(message);
        }

        GUILayout.EndScrollView();
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

        currentInputText = GUILayout.TextField(currentInputText, GUILayout.Height(25));

        if (GUILayout.Button("Send", GUILayout.Width(60), GUILayout.Height(25)) ||
            (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return && !string.IsNullOrEmpty(currentInputText)))
        {
            SendCurrentMessage();
        }

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

        // Send current state to the requesting client only
        CurrentSingleTarget = rpcContext.SourceAuthorityId;
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
        currentMessages.Add(sysMsg);
        TrimMessageHistory();
    }

    #endregion

    #region RPCs - Unified Messaging

    // Unified message sending using TargetRpc for all message types
    [TargetRpc(nameof(CurrentMessageTargets), isMultipleTargets: true, validationMethod: nameof(ValidateMessage))]
    internal async Task<RpcDeliveryReport> SendMessage(string content, string channelName, ChatType messageType)
    {
        // Get context
        var context = GONetEventBus.CurrentRpcContext;
        if (!context.HasValue)
        {
            return new RpcDeliveryReport { FailureReason = "No RPC context" };
        }

        var message = new ChatMessage
        {
            SenderId = context.Value.SourceAuthorityId,
            SenderName = GetParticipantName(context.Value.SourceAuthorityId),
            Content = content,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Type = messageType,
            ChannelName = channelName,
            Recipients = CurrentMessageTargets.ToArray()
        };

        OnReceiveMessage(message);

        return default;
    }

    void OnReceiveMessage(ChatMessage message)
    {
        GONetLog.Debug($"[{(GONetMain.IsServer ? "Server" : "Client")}] OnReceiveMessage called - From: {message.SenderId}, Type: {message.Type}, ShouldDisplay will be calculated...");

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
            GONetLog.Debug($"[{(GONetMain.IsServer ? "Server" : "Client")}] Adding message to display - From: {message.SenderName} ({message.SenderId}), Type: {message.Type}, Channel: {message.ChannelName}, Content: {message.Content}");
            currentMessages.Add(message);
            TrimMessageHistory();
        }
    }

    // Server-side validation for all messages
    internal RpcValidationResult ValidateMessage(ushort sourceAuthority, ushort[] targets, int count, byte[] messageData)
    {
        // Only validate if we're the server
        if (!GONetMain.IsServer)
        {
            return RpcValidationResult.AllowAll(targets, count);
        }

        // Validate targets are connected
        var validTargets = new List<ushort>();
        var deniedTargets = new List<ushort>();

        foreach (var target in targets.Take(count))
        {
            if (participants.Any(p => p.AuthorityId == target && p.Status == ConnectionStatus.Connected))
            {
                validTargets.Add(target);
            }
            else
            {
                deniedTargets.Add(target);
            }
        }

        // Here you could also deserialize messageData to filter profanity
        // For now, we'll just validate targets

        if (deniedTargets.Count > 0)
        {
            return new RpcValidationResult
            {
                AllowedTargets = validTargets.ToArray(),
                AllowedCount = validTargets.Count,
                DeniedTargets = deniedTargets.ToArray(),
                DeniedCount = deniedTargets.Count,
                DenialReason = "Some recipients are not connected"
            };
        }

        return RpcValidationResult.AllowAll(targets, count);
    }

    #endregion

    #region Helper Methods

    private void SendCurrentMessage()
    {
        if (string.IsNullOrEmpty(currentInputText))
            return;

        // Filter profanity locally if server
        string finalContent = GONetMain.IsServer ? FilterProfanity(currentInputText) : currentInputText;

        // Set up targets based on mode
        switch (currentChatMode)
        {
            case ChatType.Channel:
                // For channels, target all participants
                CurrentMessageTargets = participants.Select(p => p.AuthorityId).ToList();
                break;

            case ChatType.DirectMessage:
            case ChatType.GroupMessage:
                // For DM/Group, use selected participants plus ourselves
                CurrentMessageTargets = selectedParticipants.ToList();
                if (!CurrentMessageTargets.Contains(localAuthorityId))
                {
                    CurrentMessageTargets.Add(localAuthorityId);
                }
                break;
        }

        // Send using unified message system
        CallRpcAsync<RpcDeliveryReport, string, string, ChatType>(
            nameof(SendMessage),
            finalContent,
            activeChannel,
            currentChatMode)
            .ContinueWith(task =>
            {
                if (task.Result.FailedDelivery?.Length > 0)
                {
                    Debug.LogWarning($"Failed to deliver to some recipients: {task.Result.FailureReason}");
                }
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
        currentMessages.Clear();
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
        // Keep only last 100 messages
        if (currentMessages.Count > 100)
        {
            currentMessages.RemoveRange(0, currentMessages.Count - 100);
        }
    }

    private string FilterProfanity(string input)
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