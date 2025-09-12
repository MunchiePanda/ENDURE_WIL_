using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This is like a special brain for our player in the game!
public class PlayerController : MonoBehaviour
{
    // These are like the player's super powers and how fast they move!
    [Header("Base setup")]
    public float walkingSpeed = 7.5f; // How fast the player walks like a normal person
    public float runningSpeed = 11.5f; // How fast the player runs like a superhero!
    public float jumpSpeed = 8.0f; // How high the player can jump, *boing*!
    public float gravity = 20.0f; // This pulls the player down to the ground, so they don't float away!
    public float lookSpeed = 2.0f; // How fast the player can look around with their eyes
    public float lookXLimit = 45.0f; // How far up and down the player can look

    CharacterController characterController; // This helps the player walk and not go through walls
    Vector3 moveDirection = Vector3.zero; // This tells the player where to go
    float rotationX = 0; // This remembers where the player is looking up and down

    [HideInInspector]
    public bool canMove = true; // This is like a switch, true means the player can move, false means they can't!

    [SerializeField]
    private float cameraYOffset = 0.4f; // This moves the camera a little bit up so we can see better
    private Camera playerCamera; // This is like our player's eyes, what we see in the game!


    // This happens when the game first starts, like getting ready to play!
    void Start()
    {
        characterController = GetComponent<CharacterController>(); // We find the special helper for walking
        playerCamera = Camera.main; // We find our eyes (the camera)
        // We put the camera in the right spot, a little above the player's head
        playerCamera.transform.position = new Vector3(transform.position.x, transform.position.y + cameraYOffset, transform.position.z);
        playerCamera.transform.SetParent(transform); // The camera stays with the player, like a friend!
        // Lock cursor so it doesn't leave the game window, like a treasure hidden in a box!
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false; // We hide the mouse cursor so we can focus on the game!
    }

    // This happens many, many times every second, like the player always thinking what to do next!
    void Update()
    {
        bool isRunning = false; // We start by saying the player is not running

        // If we press the Left Shift key, the player starts to run super fast!
        isRunning = Input.GetKey(KeyCode.LeftShift);

        // If the player is on the ground, we figure out where they want to go
        Vector3 forward = transform.TransformDirection(Vector3.forward); // This is like pointing straight ahead
        Vector3 right = transform.TransformDirection(Vector3.right); // This is like pointing to the side

        // We figure out how fast the player should move, depending on if they are walking or running
        float curSpeedX = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Vertical") : 0; // How fast forward/backward
        float curSpeedY = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Horizontal") : 0; // How fast left/right
        float movementDirectionY = moveDirection.y; // We remember if the player was going up or down
        moveDirection = (forward * curSpeedX) + (right * curSpeedY); // This tells the player where to go on the ground

        // If we press the Jump button and the player can move and is on the ground, they jump!
        if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
        {
            moveDirection.y = jumpSpeed; // Boing! The player goes up!
        }
        else
        {
            moveDirection.y = movementDirectionY; // Otherwise, they keep going up or down as they were
        }

        // If the player is in the air, gravity pulls them down
        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime; // They fall down, whoosh!
        }

        // We tell the player to move!
        characterController.Move(moveDirection * Time.deltaTime);

        // This makes the player and camera look around
        if (canMove && playerCamera != null)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed; // Look up and down with the mouse
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit); // Don't let the player look too far up or down
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0); // The camera looks up and down
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0); // The player turns left and right
        }
    }
}
