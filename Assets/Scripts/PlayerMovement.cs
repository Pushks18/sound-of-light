using UnityEngine;
using UnityEngine.Rendering.Universal;
using System;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 8f;

    [Header("Dash (Left Shift)")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 1f;
    public float dashEnergyCost = 3f;

    [Header("Aim Indicator")]
    [Tooltip("Distance from center to the pip (should match half the player's visual size).")]
    public float pipOffset = 0.45f;
    [Tooltip("Light radius of the aim pip.")]
    public float pipRadius = 0.25f;
    [Tooltip("Light intensity of the aim pip.")]
    public float pipIntensity = 0.15f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 aimDirection = Vector2.up;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector2 dashDirection;
    private LightEnergy lightEnergy;

    private int playerLayer;
    private int enemyLayer;

    private Transform aimPivot;

    /// <summary>Last non-zero movement direction (8-way, normalized).</summary>
    public Vector2 AimDirection => aimDirection;
    public bool IsDashing => dashTimer > 0f;
    public Vector2 DashDirection => dashDirection;
    public event Action OnDashStart;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        lightEnergy = GetComponent<LightEnergy>();

        playerLayer = gameObject.layer;
        enemyLayer = LayerMask.NameToLayer("Enemy");

        // Ensure clean collision state on scene load (persists across scenes)
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);

        CreateAimPip();
    }

    void CreateAimPip()
    {
        // Pivot sits at player center, rotating to orbit the pip around the edge
        var pivotObj = new GameObject("AimPivot");
        pivotObj.transform.SetParent(transform);
        pivotObj.transform.localPosition = Vector3.zero;
        pivotObj.transform.localRotation = Quaternion.identity;
        aimPivot = pivotObj.transform;

        // Pip is offset from center — a tiny point light at the player's front edge
        var pipObj = new GameObject("AimPip");
        pipObj.transform.SetParent(aimPivot);
        pipObj.transform.localPosition = new Vector3(0f, pipOffset, 0f);
        pipObj.transform.localScale = Vector3.one;

        var light = pipObj.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.pointLightOuterRadius = pipRadius;
        light.pointLightInnerRadius = pipRadius * 0.3f;
        light.intensity = pipIntensity;
        light.color = new Color(0.85f, 0.9f, 1f); // cool white — distinct from warm flashlight
        light.shadowsEnabled = false;
        light.falloffIntensity = 0.8f;
    }

    void Update()
    {
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized;

        // Update aim to last movement direction
        if (moveInput != Vector2.zero)
            aimDirection = moveInput;

        UpdateAimPip();

        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.unscaledDeltaTime;

        if (dashTimer > 0f)
        {
            dashTimer -= Time.unscaledDeltaTime;

            // Restore collision as soon as the timer runs out
            if (dashTimer <= 0f)
                Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
        }

        // Block dashing when game has ended (victory/death pause)
        if (GameManager.Instance != null && GameManager.Instance.gameEnded)
            return;

        if (Input.GetKeyDown(KeyCode.LeftShift) && dashCooldownTimer <= 0f)
        {
            // Check both resources before spending either
            if (PlayerAmmo.Instance != null && PlayerAmmo.Instance.Dashes <= 0)
            {
                // no dashes left — don't proceed
            }
            else if (lightEnergy != null && !lightEnergy.CanSpend(dashEnergyCost))
            {
                // not enough energy — don't proceed
            }
            else
            {
                // Both checks passed — now spend
                PlayerAmmo.Instance?.TrySpendDash();
                lightEnergy?.TrySpend(dashEnergyCost);

                dashDirection = aimDirection;
                dashTimer = dashDuration;
                dashCooldownTimer = dashCooldown;

                // Let the player pass through enemies while dashing
                Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);

                OnDashStart?.Invoke();
            }
        }
    }

    void OnDisable()
    {
        // Ensure collision is restored if the player dies or is disabled mid-dash
        if (enemyLayer >= 0)
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
    }

    void FixedUpdate()
    {
        if (dashTimer > 0f)
            rb.linearVelocity = dashDirection * dashSpeed;
        else
            rb.linearVelocity = moveInput * moveSpeed;
    }

    void UpdateAimPip()
    {
        if (aimPivot == null) return;

        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        aimPivot.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }
}
