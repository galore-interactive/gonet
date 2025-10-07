using GONet;
using MemoryPack;
using System.Collections;
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
/// 4. Persistent ClientRpc: BroadcastClaimChanged(IsPersistent = true) automatically informs late-joining clients
/// 5. TargetRpc with SpecificAuthority: NotifyClaimerOfAttemptedTheft() uses a property name
///    to dynamically target the RPC to whoever has claimed the object
/// 6. RPC Context: Access via GONetEventBus.GetCurrentRpcContext() during RPC execution
/// 7. Persistent RPC State: Eliminates need for manual GetStatus() calls - state is automatic!
/// 
/// HOW TO CALL RPCs:
/// - Use CallRpc(nameof(MethodName), args...) for fire-and-forget calls
/// - Use await CallRpcAsync<TResult>(nameof(MethodName), args...) for calls with return values
/// - The CallRpc methods are inherited from GONetParticipantCompanionBehaviour
/// - RPCs are routed at runtime based on generated metadata
/// 
/// VISUAL FEEDBACK:
/// - Green: Available to claim
/// - Cyan: Hovering over available object
/// - Yellow: Claimed by you
/// - Red: Claimed by another player
/// - Gray: Hovering over unavailable object
/// - Flash + Scale Bounce: Someone tried to steal your claimed object!
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

    [Header("Theft Attempt Feedback")]
    public Color attemptedTheftFlashColor = new Color(1f, 0.5f, 0f); // Orange
    public float flashDuration = 0.3f;
    public float bounceScale = 1.3f; // Max scale during bounce
    public float bounceDuration = 0.6f;
    public AnimationCurve bounceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Customizable in Inspector

    // NOTE: Have to let it know not to use the default profile because the default profile will force the
    //       blending of this value when we don't want it to blend because it is a use short. For the time
    //       being, you have to tell it to use this profile that allows the use of the attribute properties
    //       to be the ones that are applied, which in this case says that we do not want to blend a use short
    //       because it's not really blendable. Whatever we've got to figure out how to make this a little bit
    //       better, but for now, this is what we do.
    [GONetAutoMagicalSync(
        GONetAutoMagicalSyncAttribute.PROFILE_TEMPLATE_NAME___EMPTY_USE_ATTRIBUTE_PROPERTIES_DIRECTLY,
        ShouldBlendBetweenValuesReceived = false)]
    public ushort ClaimedByAuthorityId { get; set; } = GONetMain.OwnerAuthorityId_Unset;

    // NOTE: Have to let it know not to use the default profile because the default profile will force the
    //       blending of this value when we don't want it to blend because it is a use short. For the time
    //       being, you have to tell it to use this profile that allows the use of the attribute properties
    //       to be the ones that are applied, which in this case says that we do not want to blend a use short
    //       because it's not really blendable. Whatever we've got to figure out how to make this a little bit
    //       better, but for now, this is what we do.
    [GONetAutoMagicalSync(
        GONetAutoMagicalSyncAttribute.PROFILE_TEMPLATE_NAME___EMPTY_USE_ATTRIBUTE_PROPERTIES_DIRECTLY,
        ShouldBlendBetweenValuesReceived = false)]
    public int TotalClaimCount { get; set; }

    private Renderer objectRenderer;
    private Color targetColor;
    private Color originalColor;
    private bool isMouseOver;
    private bool isClaimedByMe;
    private bool isFlashing;
    private Vector3 originalScale;
    private Coroutine bounceCoroutine;

    protected override void Awake()
    {
        base.Awake();
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer == null)
        {
            objectRenderer = GetComponentInChildren<Renderer>();
        }
        targetColor = availableColor;
        originalColor = availableColor;
        originalScale = transform.localScale;

        // Initialize bounce curve if not set in Inspector
        if (bounceCurve == null || bounceCurve.length == 0)
        {
            bounceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            bounceCurve.AddKey(0.5f, 1.2f); // Peak in the middle
        }
    }

    void Update()
    {
        if (objectRenderer != null && !isFlashing)
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
            // Use CallRpcAsync for RPCs with return values
            bool releaseSuccess = await CallRpcAsync<bool>(nameof(RequestRelease));

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
            // Use CallRpcAsync for RPCs with return values
            ClaimResult result = await CallRpcAsync<ClaimResult>(nameof(RequestClaim));

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
            // Use CallRpc for fire-and-forget (void RPCs)
            // This will notify the server about the attempt
            CallRpc(nameof(NotifyAttemptedClaimWhileOwned));
        }
    }

    [ServerRpc(IsMineRequired = false)]
    internal async Task<ClaimResult> RequestClaim()
    {
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

        if (GONetMain.IsServer)
        {
            CallRpc(nameof(BroadcastClaimChanged), ClaimedByAuthorityId, true);
        }

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
    internal async Task<bool> RequestRelease()
    {
        var context = GONetEventBus.GetCurrentRpcContext();

        if (ClaimedByAuthorityId != context.SourceAuthorityId)
        {
            GONetLog.Warning($"Authority {context.SourceAuthorityId} tried to release but doesn't own it");
            return false;
        }

        ClaimedByAuthorityId = GONetMain.OwnerAuthorityId_Unset;

        GONetLog.Debug($"Authority {context.SourceAuthorityId} released {name}");

        if (GONetMain.IsServer)
        {
            CallRpc(nameof(BroadcastClaimChanged), ClaimedByAuthorityId, false);
        }

        return true;
    }

    [ServerRpc(IsMineRequired = false)]
    internal void NotifyAttemptedClaimWhileOwned()
    {
        var context = GONetEventBus.CurrentRpcContext;
        if (context.HasValue)
        {
            GONetLog.Debug($"Authority {context.Value.SourceAuthorityId} tried to claim object owned by Authority {ClaimedByAuthorityId}");

            // Server routes this to the player who claimed it using TargetRpc
            if (GONetMain.IsServer)
            {
                // The TargetRpc will be sent to the authority specified by ClaimedByAuthorityId property
                CallRpc(nameof(NotifyClaimerOfAttemptedTheft), context.Value.SourceAuthorityId);
            }
        }
    }

    /// <summary>
    /// TargetRpc sent only to the player who has claimed this object when someone else tries to steal it.
    /// Uses the ClaimedByAuthorityId property to determine the target recipient.
    /// This provides direct feedback to the claiming player that someone attempted to "steal" their object.
    ///
    /// NOTE: This TargetRpc is NOT persistent because:
    /// 1. Theft attempt notifications are transient events that don't need to persist
    /// 2. If it were persistent, late-joining clients might not receive it properly
    ///    since their authority ID wouldn't be in ClaimedByAuthorityId when the theft occurred
    /// 3. Late-joiners don't need to know about historical theft attempts
    /// </summary>
    [TargetRpc(nameof(ClaimedByAuthorityId))]
    internal void NotifyClaimerOfAttemptedTheft(ushort attemptingAuthorityId)
    {
        // This runs only on the client that has claimed this object
        GONetLog.Warning($"⚠️ Authority {attemptingAuthorityId} tried to steal YOUR claimed object: {name}!");

        // Trigger visual feedback
        StartCoroutine(FlashWarning());

        if (bounceCoroutine != null)
        {
            StopCoroutine(bounceCoroutine);
        }
        bounceCoroutine = StartCoroutine(BounceScale());

        // You could also trigger UI notifications, sound effects, etc.
        // UIManager.ShowNotification($"Player {attemptingAuthorityId} tried to steal {name}!");
        // AudioManager.PlaySound("theft_attempt_warning");
    }

    /// <summary>
    /// Persistent ClientRpc that informs all clients about claim state changes.
    /// Uses ClientRpc (not TargetRpc) to ensure late-joining clients receive this state.
    /// This is the recommended pattern for persistent state that all clients need.
    /// </summary>
    [ClientRpc(IsPersistent = true)]
    internal void BroadcastClaimChanged(ushort authorityId, bool wasClaimed)
    {
        if (wasClaimed)
        {
            ClaimedByAuthorityId = authorityId;
            GONetLog.Debug($"{name} was claimed by Authority {authorityId}");

            if (authorityId == GONetMain.MyAuthorityId)
            {
                // AudioManager.PlaySound("claim_success");
            }
        }
        else
        {
            ClaimedByAuthorityId = GONetMain.OwnerAuthorityId_Unset;
            GONetLog.Debug($"{name} was released by Authority {authorityId}");
            // AudioManager.PlaySound("claim_released");
        }

        UpdateVisualState();
    }

    /// <summary>
    /// NOTE: GetStatus() method removed!
    /// With persistent RPCs (IsPersistent = true), late-joining clients automatically receive:
    /// - BroadcastClaimChanged: Current claim status and owner
    /// - Initial object state via GONetAutoMagicalSync properties
    ///
    /// This eliminates the need for manual status requests.
    /// </summary>

    void UpdateVisualState()
    {
        if (isMouseOver || isFlashing) return;

        if (ClaimedByAuthorityId == GONetMain.OwnerAuthorityId_Unset)
        {
            targetColor = availableColor;
            originalColor = availableColor;
        }
        else if (isClaimedByMe)
        {
            targetColor = claimedByMeColor;
            originalColor = claimedByMeColor;
        }
        else
        {
            targetColor = claimedColor;
            originalColor = claimedColor;
        }
    }

    IEnumerator FlashWarning()
    {
        isFlashing = true;
        Color currentColor = objectRenderer.material.color;

        // Flash to warning color
        float elapsed = 0;
        while (elapsed < flashDuration)
        {
            objectRenderer.material.color = Color.Lerp(currentColor, attemptedTheftFlashColor, elapsed / flashDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Flash back to original
        elapsed = 0;
        while (elapsed < flashDuration)
        {
            objectRenderer.material.color = Color.Lerp(attemptedTheftFlashColor, originalColor, elapsed / flashDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        objectRenderer.material.color = originalColor;
        isFlashing = false;
    }

    IEnumerator BounceScale()
    {
        float elapsed = 0;

        while (elapsed < bounceDuration)
        {
            float normalizedTime = elapsed / bounceDuration;

            // Use the animation curve for non-linear bounce
            float curveValue = bounceCurve.Evaluate(normalizedTime);

            // Apply elastic/bounce effect
            float scaleMultiplier = 1f + (bounceScale - 1f) * curveValue;

            // For extra bounce effect, add a secondary oscillation
            float secondaryBounce = Mathf.Sin(normalizedTime * Mathf.PI * 4) * 0.05f * (1f - normalizedTime);
            scaleMultiplier += secondaryBounce;

            transform.localScale = originalScale * scaleMultiplier;

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
        bounceCoroutine = null;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (bounceCoroutine != null)
        {
            StopCoroutine(bounceCoroutine);
        }
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

// ClaimStatus struct removed - no longer needed with persistent RPCs