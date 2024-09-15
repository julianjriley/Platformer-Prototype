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
    float apexJumpPush;
    float coyoteTime = 0.2f;
    float groundDetectionLength = 1.05f;
    bool inCoyoteTime = false;
    bool completedJump = false;
    bool isGrounded = false;
    bool jumpInputBuffered = false;
    bool jumpBuffered = false;

    //Dash Stuff
    bool isDashing = false;
    bool canDash = true;

    Coroutine coyoteTimeCoroutine;


    bool isTouchingWall = false;
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
    }

    private void Update()
    {
        //Debug.Log(isGrounded);
    }

    private void FixedUpdate()
    {
        SetHorizontalVelocity();
        CheckGrounded();

    }

    void SetHorizontalVelocity()
    {
        if (isDashing)
            return;
        rb.velocity = new Vector2(movement.ReadValue<Vector2>().normalized.x * playerSpeed, rb.velocity.y);
        if(movement.ReadValue<Vector2>() != Vector2.zero )
            lastMovedVector = new Vector2(movement.ReadValue<Vector2>().normalized.x, 0);
    }

    void Jump(InputAction.CallbackContext context)
    {
        if(context.started)
        {
            if(isDashing) 
                return;
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
        //isGrounded = false;
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
            
        jumpBuffered = false;
        jumpCounter -= 1;
        Debug.Log(jumpCounter);
    }

    void Dash(InputAction.CallbackContext context)
    {
        if(canDash)
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

        if(hit)
        {
            isGrounded = true;
            completedJump = false;
            canDash = true;
            jumpCounter = 2;
            if (jumpBuffered)
            {
                Debug.Log("DONE");
                JumpFunctionality();

            }

        }
        else
        {
            if(!inCoyoteTime && jumpCounter > 0)
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
                Debug.Log("made it");
                jumpBuffered = true;
                jumpInputBuffered = false;
            }
            else
            {
                jumpInputBuffered = false;
            }

        }
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
        rb.gravityScale = 0;
        rb.AddForce(lastMovedVector * playerDashSpeed, ForceMode2D.Impulse);
        yield return new WaitForSeconds(0.15f);
        
        rb.gravityScale = 5;

        isDashing = false;
    }
}
