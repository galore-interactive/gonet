/* GONet (TM, serial number 88592370), Copyright (c) 2019-2023 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@galoreinteractive.com
 */

using UnityEngine;
using GONet;

/// <summary>
/// Test beacon for debugging spawn/despawn propagation issues.
/// Spawns at random position in camera view, shows age visually, then despawns after lifetime.
/// Server owns and despawns. Stationary (no movement) to isolate spawn/despawn bugs.
/// </summary>
[DefaultExecutionOrder(10)]
public class SpawnTestBeacon : GONetParticipantCompanionBehaviour
{
    [Header("Lifecycle")]
    [Tooltip("How long before server despawns this beacon")]
    public float lifetime = 35f;
    private float spawnTime;

    [Header("Visual Feedback")]
    public Renderer beaconRenderer;

    // Visual age progression: Green → Yellow → Red
    private Color startColor = new Color(0f, 1f, 0f, 0.8f); // Green with some transparency
    private Color midColor = new Color(1f, 1f, 0f, 0.9f);   // Yellow
    private Color endColor = new Color(1f, 0f, 0f, 1f);     // Red, fully opaque

    private Vector3 startScale = Vector3.one * 0.5f;
    private Vector3 endScale = Vector3.one * 0.35f;

    protected override void Awake()
    {
        base.Awake();

        GONetLog.Info($"[TestBeacon] Awake() called - GameObject: {gameObject.name}, InstanceID: {GetInstanceID()}");

        if (beaconRenderer == null)
        {
            beaconRenderer = GetComponent<Renderer>();
        }
    }

    protected override void Start()
    {
        base.Start();
        GONetLog.Info($"[TestBeacon] Start() called - GameObject: {gameObject.name}, GONetId: {(GONetParticipant != null ? GONetParticipant.GONetId.ToString() : "NULL")}, IsMine: {(GONetParticipant != null ? IsMine.ToString() : "NULL")}");
    }

    public override void OnGONetReady()
    {
        base.OnGONetReady();

        // CRITICAL: Use GONet synchronized time instead of Unity Time.time!
        // Unity Time.time is local and differs between server/client due to network delays.
        // GONetMain.Time.ElapsedSeconds is synchronized across all peers by the server.
        // This ensures all machines calculate the same age and display the same color.
        spawnTime = (float)GONetMain.Time.ElapsedSeconds;
        GONetLog.Info($"[TestBeacon] ✅ OnGONetReady FIRED - GONetId: {GONetParticipant.GONetId}, IsMine: {IsMine}, Owner: {GONetParticipant.OwnerAuthorityId}, Position: {transform.position}, SpawnTime: {spawnTime} (GONet synced time)");
    }

    /// <summary>
    /// Standard Unity Update() with defensive checks - demonstrates the DEFENSIVE UPDATE PATTERN.
    ///
    /// PATTERN CHOICE: This example uses defensive checks in Update() instead of UpdateAfterGONetReady():
    /// - Checks if spawnTime == 0 before processing (defensive check for race condition)
    /// - Runs at SpawnTestBeacon's script execution order (10, configured via [DefaultExecutionOrder(10)])
    /// - More familiar to Unity developers (standard Update() pattern)
    /// - Gives precise control over when this runs relative to other scripts
    ///
    /// SCRIPT EXECUTION ORDER: This class uses [DefaultExecutionOrder(10)] to control timing.
    /// - Unity allows configuring when Update() runs (Edit → Project Settings → Script Execution Order)
    /// - This attribute achieves the same result programmatically
    /// - UpdateAfterGONetReady() would bypass this and run at -32000 (GONetGlobal's priority)
    /// - If you need execution order control, use defensive Update() pattern like this example
    ///
    /// ALTERNATIVE PATTERN: See Projectile.cs for UpdateAfterGONetReady() pattern:
    /// - NO defensive checks needed (framework guarantees OnGONetReady fired)
    /// - Runs at -32000 (early frame, before most Update() methods)
    /// - Zero overhead if not overridden (static per-type caching)
    /// - ⭐ HIGHLY PREFERRED for performance (constant overhead vs linear with N objects)
    ///
    /// TRADE-OFFS:
    /// ✅ Defensive Update() advantages:
    ///    - Full control over script execution order (via Unity's Script Execution Order settings)
    ///    - Familiar Unity pattern - runs at expected time in frame
    ///    - Works well when you need precise ordering relative to other scripts
    ///
    /// ⚠️ Defensive Update() disadvantages:
    ///    - Requires manual defensive checks (easy to forget)
    ///    - Check runs every frame (small overhead, even after initialized)
    ///    - Each MonoBehaviour with Update() adds to Unity's update loop overhead (N Update() calls)
    ///    - Less performant than UpdateAfterGONetReady() (constant overhead, just 1 Update() call)
    ///
    /// WHEN TO CHOOSE defensive Update() vs UpdateAfterGONetReady():
    /// - Use defensive Update(): When you need precise script execution order control
    /// - Use UpdateAfterGONetReady(): When you don't need execution order control (HIGHLY PREFERRED for performance)
    ///
    /// See ONGONETREADY_LIFECYCLE_DESIGN.md for detailed pattern comparison and frame timeline.
    /// See Projectile.UpdateAfterGONetReady() for the alternative pattern in action.
    /// </summary>
    private int updateCallCount = 0;
    void Update()
    {
        updateCallCount++;

        // CRITICAL DEFENSIVE CHECK: Don't process until OnGONetReady has fired and initialized spawnTime!
        // GONetParticipant.Awake() is a COROUTINE that yields, so it can complete AFTER Start() and Update() run.
        // This race condition means spawnTime can be 0 on the first few Update() calls.
        //
        // ALTERNATIVE PATTERN: Use UpdateAfterGONetReady() to eliminate this defensive check entirely.
        // See ProjectileSpawner.cs for example of UpdateAfterGONetReady() pattern.
        if (spawnTime == 0)
        {
            // DIAGNOSTIC: Log waiting state (first 5 calls only to avoid spam)
            if (updateCallCount <= 5)
            {
                GONetLog.Info($"[TestBeacon] Update #{updateCallCount} - ⏳ WAITING for OnGONetReady to initialize spawnTime - GONetId: {(GONetParticipant != null ? GONetParticipant.GONetId.ToString() : "NULL")}, IsMine: {(GONetParticipant != null ? IsMine.ToString() : "NULL")}");
            }
            return; // Skip this frame - not ready yet
        }

        // Update visual age indicator on ALL clients (everyone sees age progression)
        // CRITICAL: Use GONet synchronized time for age calculation to ensure consistent colors across all peers
        float age = (float)GONetMain.Time.ElapsedSeconds - spawnTime;
        float normalizedAge = Mathf.Clamp01(age / lifetime);

        // DIAGNOSTIC: Log first 5 Update() calls and then every 60 frames
        if (updateCallCount <= 5 || updateCallCount % 60 == 0)
        {
            GONetLog.Info($"[TestBeacon] Update #{updateCallCount} - spawnTime: {spawnTime}, age: {age:F2}s, normalizedAge: {normalizedAge:F2}, GONetId: {(GONetParticipant != null ? GONetParticipant.GONetId.ToString() : "NULL")}, IsMine: {(GONetParticipant != null ? IsMine.ToString() : "NULL")}");
        }

        // Three-stage color: Green → Yellow → Red
        Color currentColor;
        if (normalizedAge < 0.5f)
        {
            currentColor = Color.Lerp(startColor, midColor, normalizedAge * 2f);
        }
        else
        {
            currentColor = Color.Lerp(midColor, endColor, (normalizedAge - 0.5f) * 2f);
        }

        // Pulsing alpha for visibility
        float pulseAlpha = Mathf.Lerp(0.7f, 1f, Mathf.Sin(Time.time * 3f) * 0.5f + 0.5f);
        currentColor.a = pulseAlpha;

        if (beaconRenderer != null && beaconRenderer.material != null)
        {
            beaconRenderer.material.color = currentColor;
        }

        // Scale changes based on age:
        // - Normal lifetime: Gentle shrink from startScale to endScale
        // - Over-aged (past lifetime): Shrink to 50% of endScale to indicate "should be despawned"
        Vector3 targetScale;
        if (normalizedAge <= 1.0f)
        {
            // Normal aging
            targetScale = Vector3.Lerp(startScale, endScale, normalizedAge);
        }
        else
        {
            // Over-aged! Shrink to 50% to indicate it should have despawned
            targetScale = endScale * 0.5f;
        }
        transform.localScale = targetScale;

        // Server despawns when lifetime expires (only server owns these)
        if (GONetMain.IsServer && IsMine && age >= lifetime)
        {
            GONetLog.Info($"[TestBeacon] ⚠️ Server despawning beacon after {age:F2}s (lifetime: {lifetime}s) - GONetId: {GONetParticipant.GONetId}");
            Destroy(gameObject);
        }
    }

    protected override void OnDestroy()
    {
        if (GONetParticipant != null)
        {
            GONetLog.Info($"[TestBeacon] ❌ OnDestroy called - GONetId: {GONetParticipant.GONetId}, IsMine: {GONetParticipant.IsMine}, spawnTime: {spawnTime}, age: {Time.time - spawnTime:F2}s");
        }
        else
        {
            GONetLog.Info($"[TestBeacon] ❌ OnDestroy called - GONetParticipant is NULL (object was destroyed before GONet initialized)");
        }
        base.OnDestroy();
    }
}
