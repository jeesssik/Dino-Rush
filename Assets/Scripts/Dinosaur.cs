using UnityEngine;

public class Dinosaur : MonoBehaviour
{
    [SerializeField] private float upForce;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask ground;
    [SerializeField] private float radius;

    private Rigidbody2D dinoRb;
    private Animator dinoAnimator;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer shieldVisual;
    private Vector3 startPosition;
    private float coyoteTimer;
    private float jumpBuffer;
    private int airJumpsLeft = 1;
    private bool dead;

    void Awake()
    {
        GameManager.Ensure();
    }

    void Start()
    {
        dinoRb = GetComponent<Rigidbody2D>();
        dinoAnimator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        startPosition = transform.position;
        CreateShieldVisual();
        if (PsychedelicFX.Instance != null)
            PsychedelicFX.Instance.BindDinosaur(transform);
    }

    void Update()
    {
        if (dead)
        {
            if (shieldVisual != null)
                shieldVisual.enabled = false;
            if (spriteRenderer != null)
                spriteRenderer.color = Color.white;
            return;
        }

        bool playing = GameManager.Instance == null || GameManager.Instance.IsPlaying;
        bool isGrounded = false;
        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, radius, ground);

        if (dinoAnimator != null)
        {
            dinoAnimator.SetBool("IsGrounded", isGrounded);
            float speed = 1f;
            if (playing && isGrounded && GameManager.Instance != null)
                speed = GameManager.Instance.SpeedMultiplier;
            dinoAnimator.speed = speed;
        }

        if (!playing || (GameManager.Instance != null && GameManager.Instance.JumpLocked))
        {
            UpdateVisuals();
            return;
        }

        if (isGrounded)
        {
            coyoteTimer = 0.12f;
            airJumpsLeft = 1;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Space))
            jumpBuffer = 0.12f;
        else
            jumpBuffer -= Time.deltaTime;

        if (jumpBuffer > 0f)
        {
            if (coyoteTimer > 0f)
            {
                Jump(upForce);
                coyoteTimer = 0f;
                jumpBuffer = 0f;
                airJumpsLeft = 1;
            }
            else if (airJumpsLeft > 0)
            {
                Jump(upForce * 0.72f);
                airJumpsLeft = 0;
                jumpBuffer = 0f;
            }
        }

        // Soltar ESPACIO en el aire acorta el salto y da más control.
        if (Input.GetKeyUp(KeyCode.Space) && dinoRb.velocity.y > 0f)
            dinoRb.velocity = new Vector2(dinoRb.velocity.x, dinoRb.velocity.y * 0.45f);

        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, Time.deltaTime * 12f);
        UpdateVisuals();
    }

    void Jump(float force)
    {
        dinoRb.velocity = new Vector2(dinoRb.velocity.x, 0f);
        dinoRb.AddForce(Vector2.up * force);
        transform.localScale = new Vector3(0.84f, 1.16f, 1f);
        if (PsychedelicFX.Instance != null)
            PsychedelicFX.Instance.JumpTrail(transform.position);
    }

    void UpdateVisuals()
    {
        bool shielded = GameManager.Instance != null && GameManager.Instance.HasShield;
        if (shieldVisual != null)
        {
            shieldVisual.enabled = shielded;
            if (shielded)
            {
                shieldVisual.transform.Rotate(0f, 0f, -140f * Time.deltaTime);
                float pulse = 1.85f + Mathf.Sin(Time.time * 10f) * 0.12f;
                shieldVisual.transform.localScale = Vector3.one * pulse;
                Color ring = Color.HSVToRGB(Mathf.Repeat(Time.time * 0.9f, 1f), 0.7f, 1f);
                ring.a = 0.95f;
                shieldVisual.color = ring;
            }
        }

        if (spriteRenderer == null || GameManager.Instance == null)
            return;

        if (GameManager.Instance.IsInvulnerable)
        {
            float alpha = Mathf.PingPong(Time.time * 16f, 1f) > 0.5f ? 0.35f : 1f;
            Color flash = Color.HSVToRGB(Mathf.Repeat(Time.time * 4f, 1f), 0.85f, 1f);
            flash.a = alpha;
            spriteRenderer.color = flash;
        }
        else if (GameManager.Instance.IsPlaying && GameManager.Instance.SpeedMultiplier > 1.35f)
        {
            float trip = Mathf.Clamp01((GameManager.Instance.SpeedMultiplier - 1.35f) / 0.8f);
            Color neon = Color.HSVToRGB(Mathf.Repeat(Time.time * 1.1f, 1f), 0.55f * trip, 1f);
            spriteRenderer.color = Color.Lerp(Color.white, neon, trip * 0.65f);
        }
        else
        {
            spriteRenderer.color = Color.white;
        }
    }

    public void Revive()
    {
        dead = false;
        transform.position = startPosition;
        transform.localScale = Vector3.one;
        airJumpsLeft = 1;
        coyoteTimer = 0f;
        jumpBuffer = 0f;

        if (dinoRb != null)
        {
            dinoRb.simulated = true;
            dinoRb.velocity = Vector2.zero;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        if (dinoAnimator != null)
        {
            dinoAnimator.speed = 1f;
            dinoAnimator.ResetTrigger("Die");
            dinoAnimator.SetBool("IsGrounded", true);
            dinoAnimator.Play("RUn", 0, 0f);
            dinoAnimator.Update(0f);
        }
    }

    private void OnDrawGizmos()
    {
        if (groundCheck == null)
            return;

        Gizmos.DrawWireSphere(groundCheck.position, radius);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (dead || !collision.gameObject.CompareTag("Obstacle"))
            return;

        if (GameManager.Instance != null && GameManager.Instance.TryConsumeHit(transform.position))
        {
            Destroy(collision.gameObject);
            if (dinoRb != null)
                dinoRb.velocity = new Vector2(dinoRb.velocity.x, 4.5f);
            return;
        }

        dead = true;
        transform.localScale = Vector3.one;
        if (dinoAnimator != null)
        {
            dinoAnimator.speed = 1f;
            dinoAnimator.SetTrigger("Die");
        }

        if (dinoRb != null)
        {
            dinoRb.velocity = Vector2.zero;
            dinoRb.simulated = false;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.GameOver();
    }

    void CreateShieldVisual()
    {
        GameObject bubble = new GameObject("ShieldBubble");
        bubble.transform.SetParent(transform, false);
        bubble.transform.localPosition = new Vector3(0.02f, 0.05f, 0f);
        bubble.transform.localScale = Vector3.one * 1.85f;

        shieldVisual = bubble.AddComponent<SpriteRenderer>();
        shieldVisual.sprite = GameSprites.Ring;
        shieldVisual.sortingOrder = 3;
        shieldVisual.enabled = false;
    }
}
