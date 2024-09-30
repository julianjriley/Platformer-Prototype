using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    Controls controls;
    InputAction movement;

    //Physics
    Rigidbody2D rb;

    //Movement
    [SerializeField] float playerSpeed = 8;
    [SerializeField] float playerDashSpeed = 20000f;

    //Jump stuff
    int jumpCounter = 2;
    [SerializeField] float jumpForce = 20f;
    [SerializeField] float wallJumpForce = 10f;
    float apexJumpPush;
    float coyoteTime = 0.2f;
    [SerializeField] float groundDetectionLength = 1.05f;
    bool inCoyoteTime = false;
    bool coyoteTimeDisabled = false;
    bool completedJump = false;
    bool isGrounded = false;
    bool jumpInputBuffered = false;
    bool jumpBuffered = false;

    //Wall Stuff
    [SerializeField] float sideDetectionLength;
    bool canWallJump = false;
    Vector2 lastWallDir;
    bool wallJumping = false;

    //Dash Stuff
    bool isDashing = false;
    bool canDash = true;
    bool dashCooldown;

    Coroutine coyoteTimeCoroutine;
    Coroutine wallJumpCoroutine;

    [SerializeField] Transform respawnPoint;

    Animator animator;
    SpriteRenderer spriteRenderer;


    Vector2 lastMovedVector;
    private void Awake()
    {
        controls = new Controls();
    }
    private void Start()
    {
        controls.Player.Movement.Enable();
        movement = controls.Player.Movement;
        controls.Player.Jump.Enable();
        controls.Player.Jump.started += Jump;
        controls.Player.Jump.performed += Jump;
        controls.Player.Jump.canceled += Jump;

        controls.Player.Dash.Enable();
        controls.Player.Dash.performed += Dash;
        
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        //Debug.Log(isGrounded);
    }

    private void FixedUpdate()
    {
        SetHorizontalVelocity();
        CheckWalls();
        CheckGrounded();
        AnimateCharacter();
    }

    void SetHorizontalVelocity()
    {
        if (isDashing || wallJumping)
            return;
        rb.velocity = new Vector2(movement.ReadValue<Vector2>().normalized.x * playerSpeed, rb.velocity.y);
        if(movement.ReadValue<Vector2>() != Vector2.zero )
            lastMovedVector = new Vector2(movement.ReadValue<Vector2>().normalized.x, 0);
    }

    void AnimateCharacter()
    {
        if(lastMovedVector.x < 0)
        {
            spriteRenderer.flipX = true;
        }
        else
        {
            spriteRenderer.flipX = false;
        }

        if(Mathf.Abs(rb.velocity.x) > 0.3f && isGrounded )
        {
            animator.SetBool("IsRunning", true);
        }
        else
        {
            animator.SetBool("IsRunning", false);
        }

        if(rb.velocity.y > 0.3f && !isGrounded)
        {
            animator.SetBool("IsRising", true);
        }
        else
        {
            animator.SetBool("IsRising", false);
        }

        if(rb.velocity.y < 0.3f && !isGrounded)
        {
            animator.SetBool("IsFalling", true);
        }
        else
        {
            animator.SetBool("IsFalling", false);
        }
    }

    void Jump(InputAction.CallbackContext context)
    {
        if(context.started)
        {
            if(isDashing) 
                return;
            if (!isGrounded && canWallJump)
            {
                Debug.Log("wJmp");
                if(wallJumpCoroutine != null )
                    StopCoroutine(wallJumpCoroutine);
                wallJumpCoroutine = StartCoroutine(WallJump());
                return;
            }

            if (!isGrounded && jumpCounter < 1)
                CheckJumpBuffer();
            if (jumpCounter >= 1 && !isGrounded)
                JumpFunctionality();
            else if (isGrounded)
            {
                Debug.Log("GOD HELP ME");
                JumpFunctionality();
            }

        }
        
        if(context.performed)
            completedJump = true;
        if(context.canceled && !completedJump)
            rb.AddForce(-Vector2.up * 6f, ForceMode2D.Impulse);



    }

    void JumpFunctionality()
    {
        isGrounded = false;
        if (jumpCounter < 1 && !jumpBuffered)
            return;
        
        rb.velocity = new Vector2(rb.velocity.x, 0);
        if (jumpBuffered)
        {
         
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
        else
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
        coyoteTimeDisabled = true;    
        jumpBuffered = false;

        jumpCounter -= 1;
        
    }

    void Dash(InputAction.CallbackContext context)
    {
        if(canDash && !wallJumping)
        {
            rb.velocity = new Vector2(rb.velocity.x, 0);
            isDashing = true;
            
            
            StartCoroutine(Dash());
        }

        
    }

    void CheckJumpBuffer()
    {
        jumpInputBuffered = true;
    }

    void CheckGrounded()
    {
        
        RaycastHit2D hit = Physics2D.Raycast(transform.position, -Vector2.up, groundDetectionLength);

        if(hit.collider)
        {
            if(isGrounded)
                jumpCounter = 2;
            isGrounded = true;
            completedJump = false;
            canDash = true;
            coyoteTimeDisabled = false;
            
            
            if (jumpBuffered)
            {
                Debug.Log("DONE");
                JumpFunctionality();

            }

        }
        else
        {
            isGrounded=false;
            if(!inCoyoteTime && jumpCounter > 0 && !coyoteTimeDisabled)
            {
                inCoyoteTime = true;
                coyoteTimeCoroutine = StartCoroutine("CoyoteTime");
            }
        }
        if(jumpInputBuffered)
        {
            RaycastHit2D hitBuffer = Physics2D.Raycast(transform.position, -Vector2.up, groundDetectionLength * 1.5f);
            if(hitBuffer)
            {
                jumpBuffered = true;
                jumpInputBuffered = false;
            }
            else
            {
                jumpInputBuffered = false;
            }

        }
    }

    void CheckWalls()
    {
        RaycastHit2D leftRay =  Physics2D.BoxCast(transform.position, new Vector2(0.75f,0.75f), 0, Vector2.left, sideDetectionLength);
        RaycastHit2D rightRay = Physics2D.BoxCast(transform.position, new Vector2(0.75f, 0.75f), 0, -Vector2.left, sideDetectionLength);
        //RaycastHit2D rightRay = Physics2D.Raycast(transform.position, -Vector2.left, sideDetectionLength);
        if (leftRay)
        {
            Debug.Log("leftWall");
            canWallJump = true;
            lastWallDir = -Vector2.left;
            animator.SetBool("IsClinging", true);
        }
        else if(rightRay)
        {
            Debug.Log("rightWall");
            canWallJump = true;
            lastWallDir = -Vector2.right;
            animator.SetBool("IsClinging", true);
        }
        else
        {
            canWallJump = false;
            animator.SetBool("IsClinging", false);
        }
    }

    
    void WallJumpFunctionality()
    {
        wallJumping = true;
        rb.AddForce((lastWallDir + Vector2.up) * 20, ForceMode2D.Impulse);

    }


    //Coyote Time ;)
    IEnumerator CoyoteTime()
    {
        isGrounded = false;
        yield return new WaitForSeconds(coyoteTime);
        inCoyoteTime = false;
       
    }

    IEnumerator Dash()
    {
        canDash = false;
        dashCooldown = true;
        rb.gravityScale = 0;
        rb.AddForce(lastMovedVector * playerDashSpeed, ForceMode2D.Impulse);
        animator.SetBool("IsDashing", true);
        yield return new WaitForSeconds(0.15f);
        animator.SetBool("IsDashing", false);
        rb.gravityScale = 3.5f;
        
        isDashing = false;
        //yield return new WaitForSeconds(0.3f);
        dashCooldown = false;

    }
    IEnumerator WallJump()
    {
        wallJumping = true;
        rb.velocity = Vector2.zero;
        //rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.AddForce((lastWallDir + Vector2.up * 1.4f) * wallJumpForce, ForceMode2D.Impulse);
        jumpCounter = 1;
        animator.SetBool("WallJumping", true);
        yield return new WaitForSeconds(0.3f);
        animator.SetBool("WallJumping", false);
        wallJumping = false;
        canDash = true;
    }

    public void RespawnSomewhereElse()
    {
        transform.position = respawnPoint.position;
    }
}
