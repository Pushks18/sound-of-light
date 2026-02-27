using UnityEngine;
using System;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 30f;

    [Header("Dash (Left Shift)")]
    public float dashSpeed = 50f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 1f;
    public float dashEnergyCost = 3f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 aimDirection = Vector2.up;
    private float dashTimer;
    private float dashCooldownTimer;
    private Vector2 dashDirection;
    private LightEnergy lightEnergy;

    /// <summary>Last non-zero movement direction (8-way, normalized).</summary>
    public Vector2 AimDirection => aimDirection;
    public bool IsDashing => dashTimer > 0f;
    public Vector2 DashDirection => dashDirection;
    public event Action OnDashStart;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        lightEnergy = GetComponent<LightEnergy>();
    }

    void Update()
    {
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized;

        // Update aim to last movement direction
        if (moveInput != Vector2.zero)
            aimDirection = moveInput;

        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        if (dashTimer > 0f)
            dashTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.LeftShift) && dashCooldownTimer <= 0f)
        {
            // Check dash count
            if (PlayerAmmo.Instance != null && !PlayerAmmo.Instance.TrySpendDash())
            {
                // no dashes left — don't proceed
            }
            else if (lightEnergy == null || lightEnergy.TrySpend(dashEnergyCost))
            {
                dashDirection = aimDirection;
                dashTimer = dashDuration;
                dashCooldownTimer = dashCooldown;
                OnDashStart?.Invoke();
            }
        }
    }

    void FixedUpdate()
    {
        if (dashTimer > 0f)
            rb.linearVelocity = dashDirection * dashSpeed;
        else
            rb.linearVelocity = moveInput * moveSpeed;
    }
}
