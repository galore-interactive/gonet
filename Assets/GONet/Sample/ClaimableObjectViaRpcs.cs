using GONet;
using MemoryPack;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Demonstrates GONet RPC usage through a claiming/selection system where objects can be 
/// exclusively "claimed" by different players in a multiplayer session.
/// 
/// KEY ASSUMPTIONS:
/// - These objects are SERVER-OWNED (spawned by server or placed in scene)
/// - Only the server has authority to modify GONetAutoMagicalSync properties
/// - Clients request changes via RPCs, server validates and applies them
/// 
/// RPC PATTERNS DEMONSTRATED:
/// 1. Task<T> Returns: RequestClaim() returns detailed result data that clients use
/// 2. Task<bool> Returns: RequestRelease() returns success/failure that clients check
/// 3. Void RPCs: NotifyAttemptedClaimWhileOwned() for fire-and-forget notifications
/// 4. ClientRpc: BroadcastClaimChanged() for server-to-all-clients notifications
/// 5. RPC Context: Access via GONetEventBus.CurrentRpcContext during RPC execution
/// 
/// VISUAL FEEDBACK:
/// - Green: Available to claim
/// - Cyan: Hovering over available object
/// - Yellow: Claimed by you
/// - Red: Claimed by another player
/// - Gray: Hovering over unavailable object
/// </summary>
public class ClaimableObjectViaRpcs : GONetParticipantCompanionBehaviour
{
    [Header("Visual Settings")]
    public Color availableColor = Color.green;
    public Color hoverAvailableColor = Color.cyan;
    public Color claimedColor = Color.red;
    public Color claimedByMeColor = Color.yellow;
    public Color hoverUnavailableColor = Color.gray;
    public float colorTransitionSpeed = 5f;

    [GONetAutoMagicalSync]
    public ushort ClaimedByAuthorityId { get; set; } = GONetMain.OwnerAuthorityId_Unset;

    [GONetAutoMagicalSync]
    public int TotalClaimCount { get; set; }

    private Renderer objectRenderer;
    private Color targetColor;
    private bool isMouseOver;
    private bool isClaimedByMe;

    protected override void Awake()
    {
        base.Awake();
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer == null)
        {
            objectRenderer = GetComponentInChildren<Renderer>();
        }
        targetColor = availableColor;
    }

    void Update()
    {
        if (objectRenderer != null)
        {
            objectRenderer.material.color = Color.Lerp(
                objectRenderer.material.color,
                targetColor,
                Time.deltaTime * colorTransitionSpeed
            );
        }

        isClaimedByMe = ClaimedByAuthorityId == GONetMain.MyAuthorityId;
        UpdateVisualState();
    }

    void OnMouseEnter()
    {
        isMouseOver = true;

        bool isAvailable = ClaimedByAuthorityId == GONetMain.OwnerAuthorityId_Unset;

        if (isAvailable)
        {
            targetColor = hoverAvailableColor;
            GONetLog.Debug($"{name} is available - click to claim!");
        }
        else if (isClaimedByMe)
        {
            GONetLog.Debug($"{name} is yours - click to release!");
        }
        else
        {
            targetColor = hoverUnavailableColor;
            GONetLog.Debug($"{name} is claimed by Authority {ClaimedByAuthorityId}");
        }
    }

    void OnMouseExit()
    {
        isMouseOver = false;
        UpdateVisualState();
    }

    async void OnMouseDown()
    {
        if (isClaimedByMe)
        {
            bool releaseSuccess = await this.RequestRelease();

            if (releaseSuccess)
            {
                GONetLog.Debug("Successfully released the object!");
            }
            else
            {
                GONetLog.Warning("Failed to release - server rejected");
            }
        }
        else if (ClaimedByAuthorityId == GONetMain.OwnerAuthorityId_Unset)
        {
            ClaimResult result = await this.RequestClaim();

            if (result.Success)
            {
                GONetLog.Debug($"Claim successful! You are claim #{result.ClaimNumber}");

                if (result.IsSpecialClaim)
                {
                    GONetLog.Debug("🎉 Special claim! You get a bonus!");
                }
            }
            else
            {
                GONetLog.Warning($"Claim failed: {result.Message}");

                if (result.CurrentOwnerAuthorityId != GONetMain.OwnerAuthorityId_Unset)
                {
                    GONetLog.Debug($"Authority {result.CurrentOwnerAuthorityId} claimed it first");
                }
            }
        }
        else
        {
            this.NotifyAttemptedClaimWhileOwned(ClaimedByAuthorityId);
        }
    }

    [ServerRpc(IsMineRequired = false)]
    async Task<ClaimResult> RequestClaim()
    {
        // Get context when needed
        var context = GONetEventBus.GetCurrentRpcContext();

        if (ClaimedByAuthorityId != GONetMain.OwnerAuthorityId_Unset)
        {
            return new ClaimResult
            {
                Success = false,
                Message = $"Already claimed by Authority {ClaimedByAuthorityId}",
                CurrentOwnerAuthorityId = ClaimedByAuthorityId,
                ClaimNumber = 0,
                IsSpecialClaim = false
            };
        }

        ClaimedByAuthorityId = context.SourceAuthorityId;
        TotalClaimCount++;

        bool isSpecial = TotalClaimCount % 5 == 0;

        GONetLog.Debug($"Authority {context.SourceAuthorityId} claimed {name} (Remote: {context.IsSourceRemote})");

        this.BroadcastClaimChanged(context.SourceAuthorityId, true);

        return new ClaimResult
        {
            Success = true,
            Message = isSpecial ? "Special claim!" : "Successfully claimed!",
            CurrentOwnerAuthorityId = context.SourceAuthorityId,
            ClaimNumber = TotalClaimCount,
            IsSpecialClaim = isSpecial
        };
    }

    [ServerRpc(IsMineRequired = false)]
    async Task<bool> RequestRelease()
    {
        var context = GONetEventBus.GetCurrentRpcContext();

        if (ClaimedByAuthorityId != context.SourceAuthorityId)
        {
            GONetLog.Warning($"Authority {context.SourceAuthorityId} tried to release but doesn't own it");
            return false;
        }

        ClaimedByAuthorityId = GONetMain.OwnerAuthorityId_Unset;

        GONetLog.Debug($"Authority {context.SourceAuthorityId} released {name}");

        this.BroadcastClaimChanged(context.SourceAuthorityId, false);

        return true;
    }

    [ServerRpc(IsMineRequired = false)]
    void NotifyAttemptedClaimWhileOwned(ushort currentOwnerAuthorityId)
    {
        // Could access context if needed
        var context = GONetEventBus.CurrentRpcContext;
        if (context.HasValue)
        {
            GONetLog.Debug($"Authority {context.Value.SourceAuthorityId} tried to claim object owned by Authority {currentOwnerAuthorityId}");
        }
    }

    [ClientRpc]
    void BroadcastClaimChanged(ushort authorityId, bool wasClaimed)
    {
        if (wasClaimed)
        {
            GONetLog.Debug($"{name} was claimed by Authority {authorityId}");

            if (authorityId == GONetMain.MyAuthorityId)
            {
                // AudioManager.PlaySound("claim_success");
            }
        }
        else
        {
            GONetLog.Debug($"{name} was released by Authority {authorityId}");
            // AudioManager.PlaySound("claim_released");
        }

        UpdateVisualState();
    }

    [ServerRpc(IsMineRequired = false)]
    async Task<ClaimStatus> GetStatus()
    {
        return new ClaimStatus
        {
            IsAvailable = ClaimedByAuthorityId == GONetMain.OwnerAuthorityId_Unset,
            CurrentOwnerAuthorityId = ClaimedByAuthorityId,
            TotalClaims = TotalClaimCount
        };
    }

    void UpdateVisualState()
    {
        if (isMouseOver) return;

        if (ClaimedByAuthorityId == GONetMain.OwnerAuthorityId_Unset)
            targetColor = availableColor;
        else if (isClaimedByMe)
            targetColor = claimedByMeColor;
        else
            targetColor = claimedColor;
    }
}


[MemoryPackable]
public partial struct ClaimResult
{
    public bool Success;
    public string Message;
    public ushort CurrentOwnerAuthorityId;
    public int ClaimNumber;
    public bool IsSpecialClaim;
}

[MemoryPackable]
public partial struct ClaimStatus
{
    public bool IsAvailable;
    public ushort CurrentOwnerAuthorityId;
    public int TotalClaims;
}