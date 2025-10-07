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

        if (beaconRenderer == null)
        {
            beaconRenderer = GetComponent<Renderer>();
        }
    }

    public override void OnGONetReady()
    {
        base.OnGONetReady();

        // This parameterless override is ONLY called for THIS beacon's participant (not catch-up calls)
        spawnTime = Time.time;
        //GONetLog.Info($"[TestBeacon] OnGONetReady - GONetId: {GONetParticipant.GONetId}, IsMine: {IsMine}, Owner: {GONetParticipant.OwnerAuthorityId}, Position: {transform.position}, SpawnTime: {spawnTime}");
    }

    void Update()
    {
        // Update visual age indicator on ALL clients (everyone sees age progression)
        float age = Time.time - spawnTime;
        float normalizedAge = Mathf.Clamp01(age / lifetime);

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
            //GONetLog.Info($"[TestBeacon] Despawning after {age:F2}s - GONetId: {GONetParticipant.GONetId}");
            Destroy(gameObject);
        }
    }

    /*
    protected override void OnDestroy()
    {
        if (GONetParticipant != null)
        {
            GONetLog.Info($"[TestBeacon] OnDestroy called - GONetId: {GONetParticipant.GONetId}, IsMine: {GONetParticipant.IsMine}");
        }
        base.OnDestroy();
    }
    */
}
