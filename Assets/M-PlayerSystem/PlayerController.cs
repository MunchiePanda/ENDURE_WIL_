using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ENDURE
{
    // This is like a special brain for our player in the game!
    public class PlayerController : MonoBehaviour
    {
        public enum PlayerState
        {
            Playing,
            UI
        }
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

        // Tile degradation system
        private Tile currentTile;
        private Tile previousTile;
        public bool isRunning = false; // Made public for tile degradation system
        private float lastTileInteractionTime = 0f;
        private float tileInteractionCooldown = 0.1f; // Minimum time between tile interactions

        [SerializeField]
        private float cameraYOffset = 0.4f; // This moves the camera a little bit up so we can see better
        private Camera playerCamera; // This is like our player's eyes, what we see in the game!

        [SerializeField]
        private UIManager uiManager; // This is like our player's UI manager, what we use to open and close the inventory panel!

        [Header("State")]
        public PlayerState state = PlayerState.Playing; // What mode the player is in

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
            if (uiManager == null) uiManager = GetComponentInChildren<UIManager>(true);
            SetState(PlayerState.Playing);
        }

        // This happens many, many times every second, like the player always thinking what to do next!
        void Update()
        {
            if (state == PlayerState.Playing)
            {
                UpdatePlaying();
            }
            else if (state == PlayerState.UI)
            {
                UpdateUI();
            }
            // Open/Close Inventory with I key using UIManager (D2, D6)
            if (Input.GetKeyDown(KeyCode.I))
            {
                OpenInventory();
            }
        }

        void UpdatePlaying()
        {
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

            // Detect tile interaction for degradation system
            DetectTileInteraction();

            // This makes the player and camera look around
            if (canMove && playerCamera != null)
            {
                rotationX += -Input.GetAxis("Mouse Y") * lookSpeed; // Look up and down with the mouse
                rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit); // Don't let the player look too far up or down
                playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0); // The camera looks up and down
                transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0); // The player turns left and right
            }
        }

        void UpdateUI()
        {
            if (Input.GetKeyDown(KeyCode.X) && state == PlayerState.UI) //X for now because escape is annoying and doesn't do shit right in editor ~S
            {
                //close all UI panels
                uiManager.EnableInventoryUI(false);
                uiManager.EnableCraftingUI(false);
                //unlock cursor
                uiManager.EnableUI(false);
            }
        }

        // Open the inventory panel ~S
        private void OpenInventory()
        {
            if (uiManager != null)
            {
                uiManager.EnableInventoryUI(true);
                SetState(PlayerState.UI);
                uiManager.EnableUI(true);
            }
            else
            {
                Debug.LogWarning("PlayerController OpenInventory(): UIManager not found under player for Inventory toggle (D2, D6)", this);
            }
        }

        public void SetState(PlayerState newState)
        {
            state = newState;
            switch (state)
            {
                case PlayerState.Playing:
                    canMove = true;
                    break;
                case PlayerState.UI:
                    canMove = false;
                    break;
            }
        }

         // Tile degradation system methods
         private void DetectTileInteraction()
         {
             // Cast ray downward to detect current tile
             RaycastHit hit;
             if (Physics.Raycast(transform.position, Vector3.down, out hit, 2f))
             {
                 Debug.Log($"Raycast hit: {hit.collider.name} at distance {hit.distance}");
                 // Look for Tile component on the parent object (since the script is on the parent, not the child Quad)
                 Tile tile = hit.collider.GetComponentInParent<Tile>();
                 if (tile != null)
                 {
                     Debug.Log($"Found Tile component on parent of {hit.collider.name}, previous tile: {(previousTile != null ? previousTile.name : "null")}");
                     if (tile != previousTile)
                     {
                         Debug.Log($"NEW TILE DETECTED: {tile.name} at coordinates {tile.Coordinates}");
                         OnTileEntered(tile);
                         previousTile = tile;
                     }
                     else
                     {
                         Debug.Log($"Same tile as before: {tile.name} - skipping");
                     }
                 }
                 else
                 {
                     Debug.Log($"No Tile component found on parent of {hit.collider.name}");
                 }
             }
             else
             {
                 Debug.Log("No raycast hit detected");
             }
         }

        private void OnTileEntered(Tile tile)
        {
            if (tile == null) 
            {
                Debug.LogError("OnTileEntered called with null tile!");
                return;
            }

            Debug.Log($"=== ON TILE ENTERED: {tile.name} at {tile.Coordinates} ===");
            Debug.Log($"Tile Instance ID: {tile.GetInstanceID()}");
            Debug.Log($"Tile degradation level: {tile.degradationLevel}");
            Debug.Log($"Tile is broken: {tile.isBroken}");

            // Don't degrade broken tiles
            if (tile.isBroken)
            {
                Debug.Log($"Player stepped on broken tile {tile.Coordinates} - skipping degradation");
                return;
            }

            // Check cooldown to prevent rapid-fire degradation
            if (Time.time - lastTileInteractionTime < tileInteractionCooldown)
            {
                Debug.Log($"Tile interaction cooldown active - skipping degradation (Time: {Time.time}, Last: {lastTileInteractionTime})");
                return;
            }

            float degradationAmount = isRunning ? tile.runDegradationAmount : tile.walkDegradationAmount;
            Debug.Log($"About to degrade tile {tile.Coordinates} by {degradationAmount} (Running: {isRunning})");
            
            tile.DegradeTile(degradationAmount);
            lastTileInteractionTime = Time.time;

            Debug.Log($"=== TILE DEGRADATION COMPLETE: {tile.Coordinates} ===");
        }
    }
}
