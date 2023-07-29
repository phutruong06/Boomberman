using UnityEngine;
using Unity.Netcode;

public class MovementController : NetworkBehaviour
{
    public new Rigidbody2D rigidbody { get; private set; }
    [SerializeField] private NetworkVariable<Vector2> direction = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public float speed = 5f;

    public KeyCode inputUp = KeyCode.W;
    public KeyCode inputDown = KeyCode.S;
    public KeyCode inputLeft = KeyCode.A;
    public KeyCode inputRight = KeyCode.D;

    public AnimatedSpriteRenderer spriteRendererUp;
    public AnimatedSpriteRenderer spriteRendererDown;
    public AnimatedSpriteRenderer spriteRendererLeft;
    public AnimatedSpriteRenderer spriteRendererRight;
    public AnimatedSpriteRenderer spriteRendererDeath;
    private AnimatedSpriteRenderer activeSpriteRenderer;


    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        activeSpriteRenderer = spriteRendererDown;
    }
        
    private void Update()
    {
        SetSpriteC();

        //if (!IsOwner)
        //    return;

        if (Input.GetKey(inputUp))
        {
            SetDirectionServerRpc(Vector2.up);
        }
        else if (Input.GetKey(inputDown))
        {
            SetDirectionServerRpc(Vector2.down);
        }
        else if (Input.GetKey(inputLeft))
        {
            SetDirectionServerRpc(Vector2.left);
        }
        else if (Input.GetKey(inputRight))
        {
            SetDirectionServerRpc(Vector2.right);
        }
        else
        {
            SetDirectionServerRpc(Vector2.zero);
        }
  

    }

    private void FixedUpdate()
    {
        if (!IsOwnedByServer)
            return;

        MoveRigidbodyServerRpc();
    }

    [ServerRpc]
    private void MoveRigidbodyServerRpc()
    {
        Vector2 position = rigidbody.position;
        Vector2 translation = direction.Value * speed * Time.fixedDeltaTime;

        rigidbody.MovePosition(position + translation);
    }

    [ServerRpc]
    private void SetDirectionServerRpc(Vector2 newDirection)
    {
        direction.Value = newDirection;

    }

    private void SetSpriteC()
    {

        spriteRendererUp.enabled = activeSpriteRenderer == spriteRendererUp;
        spriteRendererDown.enabled = activeSpriteRenderer == spriteRendererDown;
        spriteRendererLeft.enabled = activeSpriteRenderer == spriteRendererLeft;
        spriteRendererRight.enabled = activeSpriteRenderer == spriteRendererRight;
        Debug.Log(direction.Value);
        activeSpriteRenderer.idle = direction.Value == Vector2.zero;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.gameObject.layer == LayerMask.NameToLayer("Explosion")) {
            DeathSequence();
        }

    }

    private void DeathSequence()
    {
        enabled = false;
        GetComponent<BombController>().enabled = false;

        spriteRendererUp.enabled= false;
        spriteRendererDown.enabled= false;
        spriteRendererLeft.enabled= false;
        spriteRendererRight.enabled= false;
        spriteRendererDeath.enabled = true;

        Invoke(nameof(OnDeathSequenceEnded), 1.25f);
    }

    private void OnDeathSequenceEnded()
    {
        gameObject.SetActive(false);
        FindObjectOfType<GameManager>().ChechWinState();
    }




}
