using GONet;
using MemoryPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using static GONetSampleChatSystem;

/// <summary>
/// A comprehensive chat system for GONet that supports channels, direct messages, 
/// and group conversations with server-side profanity filtering and validation.
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
    private bool isDragging = false;

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
    public string CurrentChannelTarget { get; set; } = "general";

    // Local user info
    private string localDisplayName = "";
    private ushort localAuthorityId;

    #endregion

    #region Unity Lifecycle

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

    void Start()
    {
        // Generate a default display name
        localAuthorityId = GONetMain.MyAuthorityId;
        localDisplayName = GONetMain.IsServer ? "Server" : $"Player_{localAuthorityId}";

        // Announce our presence
        if (GONetMain.IsServer)
        {
            OnServerStarted();
        }
        else
        {
            CallRpc(nameof(ClientJoined), localDisplayName);
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
        GUILayout.Label("Participants", GUI.skin.box);

        participantsScrollPosition = GUILayout.BeginScrollView(participantsScrollPosition, GUILayout.Height(200));

        foreach (var participant in participants)
        {
            GUILayout.BeginHorizontal();

            // Status indicator
            Color statusColor = GetStatusColor(participant.Status);
            GUI.color = statusColor;
            GUILayout.Label("●", GUILayout.Width(15));
            GUI.color = Color.white;

            // Selection checkbox for DM/Group
            bool isSelected = selectedParticipants.Contains(participant.AuthorityId);
            bool newSelected = GUILayout.Toggle(isSelected, "", GUILayout.Width(20));

            if (newSelected != isSelected)
            {
                if (newSelected)
                    selectedParticipants.Add(participant.AuthorityId);
                else
                    selectedParticipants.Remove(participant.AuthorityId);
            }

            // Name button for quick DM
            if (GUILayout.Button(participant.DisplayName, GUI.skin.label))
            {
                StartDirectMessage(participant.AuthorityId);
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

    #region RPCs - Connection Management

    [ServerRpc(IsMineRequired = false)]
    internal void ClientJoined(string displayName)
    {
        GONetRpcContext rpcContext = GONetEventBus.GetCurrentRpcContext();

        var newParticipant = new ChatParticipant
        {
            AuthorityId = rpcContext.SourceAuthorityId,
            DisplayName = displayName,
            IsServer = false,
            Status = ConnectionStatus.Connected
        };

        participants.Add(newParticipant);

        // Notify all clients about the new participant
        CallRpc(nameof(BroadcastParticipantUpdate), participants.ToArray());

        // Send existing channels to the new client
        CallRpc(nameof(SyncChannels), channels.ToArray());

        // Send system message
        var sysMsg = new ChatMessage
        {
            Type = ChatType.System,
            Content = $"{displayName} joined the chat",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        CallRpc(nameof(BroadcastSystemMessage), sysMsg);
    }

    [ClientRpc]
    internal void BroadcastParticipantUpdate(ChatParticipant[] allParticipants)
    {
        participants = allParticipants.ToList();
    }

    [ClientRpc]
    internal void SyncChannels(ChatChannel[] allChannels)
    {
        channels = allChannels.ToList();
    }

    [ClientRpc]
    internal void BroadcastSystemMessage(ChatMessage message)
    {
        currentMessages.Add(message);
        TrimMessageHistory();
    }

    #endregion

    #region RPCs - Channel Management

    [ServerRpc(IsMineRequired = false, Relay = RelayMode.All)]
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
    }

    #endregion

    #region RPCs - Messaging

    // Channel message - goes to everyone
    [ServerRpc(IsMineRequired = false, Relay = RelayMode.All)]
    internal void SendChannelMessage(string channelName, string content)
    {
        // Server-side validation and filtering
        string filteredContent = FilterProfanity(content);

        GONetRpcContext rpcContext = GONetEventBus.GetCurrentRpcContext();

        var message = new ChatMessage
        {
            SenderId = rpcContext.SourceAuthorityId,
            SenderName = GetParticipantName(rpcContext.SourceAuthorityId),
            Content = filteredContent,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Type = ChatType.Channel,
            ChannelName = channelName
        };

        // The Relay = All means this automatically goes to everyone
        OnReceiveChannelMessage(message);
    }

    void OnReceiveChannelMessage(ChatMessage message)
    {
        if (currentChatMode == ChatType.Channel && activeChannel == message.ChannelName)
        {
            currentMessages.Add(message);
            TrimMessageHistory();
        }
    }

    // Direct/Group message using TargetRpc with validation
    [TargetRpc(nameof(CurrentMessageTargets), isMultipleTargets: true, validationMethod: nameof(ValidateDirectMessage))]
    internal async Task<RpcDeliveryReport> SendDirectMessage(string content, ChatType messageType)
    {
        // Get context from EventBus
        var context = GONetEventBus.CurrentRpcContext;
        if (!context.HasValue) // this check is unnecessary. There will be a context in every call, assuming that the call was made the right way.
        {
            // Handle error - shouldn't happen in RPC context
            return new RpcDeliveryReport { FailureReason = "No RPC context" };
        }

        var message = new ChatMessage
        {
            SenderId = context.Value.SourceAuthorityId,
            SenderName = GetParticipantName(context.Value.SourceAuthorityId),
            Content = content,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Type = messageType,
            Recipients = CurrentMessageTargets.ToArray()
        };

        OnReceiveDirectMessage(message);

        // Framework handles returning the report to the original caller, but something must be returned here to compile!
        return default;
    }

    void OnReceiveDirectMessage(ChatMessage message)
    {
        // Check if this message is relevant to current view
        bool shouldDisplay = false;

        if (message.Type == ChatType.DirectMessage && currentChatMode == ChatType.DirectMessage)
        {
            shouldDisplay = selectedParticipants.Contains(message.SenderId) ||
                          message.SenderId == localAuthorityId;
        }
        else if (message.Type == ChatType.GroupMessage && currentChatMode == ChatType.GroupMessage)
        {
            shouldDisplay = message.Recipients.Any(r => selectedParticipants.Contains(r));
        }

        if (shouldDisplay)
        {
            currentMessages.Add(message);
            TrimMessageHistory();
        }
    }

    // Server-side validation for direct messages
    internal RpcValidationResult ValidateDirectMessage(ushort sourceAuthority, ushort[] targets, int count, byte[] messageData)
    {
        // Only validate if we're the server
        if (!GONetMain.IsServer)
        {
            return RpcValidationResult.AllowAll(targets, count);
        }

        // Deserialize and filter the message content
        // Note: In production, you'd deserialize the messageData to get the actual content
        // For this example, we'll just validate targets

        var validTargets = new List<ushort>();
        var deniedTargets = new List<ushort>();

        foreach (var target in targets.Take(count))
        {
            // Check if target is a valid connected participant
            if (participants.Any(p => p.AuthorityId == target && p.Status == ConnectionStatus.Connected))
            {
                validTargets.Add(target);
            }
            else
            {
                deniedTargets.Add(target);
            }
        }

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

    private void OnServerStarted()
    {
        // Server adds itself as a participant
        participants.Add(new ChatParticipant
        {
            AuthorityId = 0,
            DisplayName = "Server",
            IsServer = true,
            Status = ConnectionStatus.Connected
        });
    }

    private void SendCurrentMessage()
    {
        if (string.IsNullOrEmpty(currentInputText))
            return;

        switch (currentChatMode)
        {
            case ChatType.Channel:
                CallRpc(nameof(SendChannelMessage), activeChannel, currentInputText);
                break;

            case ChatType.DirectMessage:
            case ChatType.GroupMessage:
                CurrentMessageTargets = selectedParticipants.ToList();
                CallRpcAsync<RpcDeliveryReport, string, ChatType>(nameof(SendDirectMessage), currentInputText, currentChatMode)
                    .ContinueWith(task =>
                    {
                        if (task.Result.FailedDelivery?.Length > 0)
                        {
                            Debug.LogWarning($"Failed to deliver to some recipients: {task.Result.FailureReason}");
                        }

                        if (task.Result.WasModified)
                        {
                            Debug.LogWarning($"Original message was modified by the server before being delivered to recipients.");
                        }
                    });
                break;
        }

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

[MemoryPackable]
public partial struct ChatMessage
{
    public ushort SenderId { get; set; }
    public string SenderName { get; set; }
    public string Content { get; set; }
    public long Timestamp { get; set; }
    public ChatType Type { get; set; }
    public string ChannelName { get; set; } // For channel messages
    public ushort[] Recipients { get; set; } // For DM/Group messages
}

[MemoryPackable]
public partial struct ChatParticipant
{
    public ushort AuthorityId { get; set; }
    public string DisplayName { get; set; }
    public bool IsServer { get; set; }
    public ConnectionStatus Status { get; set; }
}

[MemoryPackable]
public partial struct ChatChannel
{
    public string Name { get; set; }
    public string Description { get; set; }
    public ushort CreatorId { get; set; }
    public long CreatedTimestamp { get; set; }
}
