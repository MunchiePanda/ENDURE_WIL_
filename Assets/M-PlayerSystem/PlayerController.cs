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
        public float sneakingSpeed = 3.75f; // How fast the player sneaks around *Sneaky Sneaky*
        public float walkingSpeed = 7.5f;   // How fast the player walks like a normal person
        public float runningSpeed = 11.5f;  // How fast the player runs like a superhero!
        public float jumpSpeed = 8.0f;      // How high the player can jump, *boing*!
        public float gravity = 20.0f;       // This pulls the player down to the ground, so they don't float away!
        public float lookSpeed = 2.0f;      // How fast the player can look around with their eyes
        public float lookXLimit = 45.0f;    // How far up and down the player can look
        
        [SerializeField] private float staminaDrainPerSecond = 10f;
        [SerializeField] private float staminaRegenPerSecond = 5f;

        CharacterController characterController; // This helps the player walk and not go through walls
        Vector3 moveDirection = Vector3.zero; // This tells the player where to go
        float rotationX = 0; // This remembers where the player is looking up and down
        private PlayerManager playerManager;

        [HideInInspector]
        public bool canMove = true; // This is like a switch, true means the player can move, false means they can't!

		// New tile degradation system - track all tiles in scene
		private Tile[] allTiles;
		private Tile currentTile;
		private Tile previousTile;
		public bool isRunning = false;
        public bool isSneaking = false;
		private float lastTileInteractionTime = 0f;
		private float tileInteractionCooldown = 0.1f;

        [SerializeField]
        private float cameraYOffset = 0.4f; // This moves the camera a little bit up so we can see better
        private Camera playerCamera; // This is like our player's eyes, what we see in the game!
        
        [Header("Camera Culling Fix")]
        [Tooltip("Fix camera culling so floor remains visible when falling through broken tiles")]
        public bool fixCameraCulling = true;

        [SerializeField]
        private UIManager uiManager; // This is like our player's UI manager, what we use to open and close the inventory panel!

        [Header("Torch Settings")]
        public bool enableTorch = true;
        public KeyCode torchToggleKey = KeyCode.F;
        public bool torchStartsOn = true;
        public float torchIntensity = 3f;
        public float torchRange = 18f;
        [Range(1f, 120f)]
        public float torchSpotAngle = 60f;
        public Color torchColor = new Color(1f, 0.95f, 0.85f);
        public Vector3 torchLocalPosition = new Vector3(0.2f, -0.15f, 0.4f);
        public Vector3 torchLocalRotation = new Vector3(0f, 0f, 0f);

        private Light torchLight;
        private bool torchIsOn;

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
            
            // Apply camera culling fix if enabled
            if (fixCameraCulling && playerCamera != null)
            {
                ApplyCameraCullingFix();
            }

            SetupTorch();
                                                         // Lock cursor so it doesn't leave the game window, like a treasure hidden in a box!
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false; // We hide the mouse cursor so we can focus on the game!
            if (uiManager == null) uiManager = GetComponentInChildren<UIManager>(true);
            SetState(PlayerState.Playing);
        
        playerManager = GetComponent<PlayerManager>();
        if (playerManager == null)
        {
            Debug.LogWarning("PlayerController Start(): PlayerManager component missing on player.");
        }
            
            // Find all tiles in the scene
            FindAllTiles();
        }
        
        private void FindAllTiles()
        {
            allTiles = FindObjectsOfType<Tile>();
            Debug.Log($"Found {allTiles.Length} tiles in the scene");
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
            if (Input.GetKeyDown(KeyCode.Q))
            {
                ToggleQuestOverview();
            }

            if (enableTorch && Input.GetKeyDown(torchToggleKey))
            {
                ToggleTorch();
            }
        }

        void UpdatePlaying()
        {
            //Running Logic (needs stamina)
            bool wantsToRun = Input.GetKey(KeyCode.LeftShift);
            bool staminaAvailable = playerManager != null ? playerManager.Stamina.current > playerManager.Stamina.min : true;
            isRunning = wantsToRun && staminaAvailable;

            //Sneaking Logic (does not need stamina)
            isSneaking = Input.GetKey(KeyCode.LeftControl);

            // If the player is on the ground, we figure out where they want to go
            Vector3 forward = transform.TransformDirection(Vector3.forward); // This is like pointing straight ahead
            Vector3 right = transform.TransformDirection(Vector3.right); // This is like pointing to the side

            // We figure out how fast the player should move, depending on if they are walking or running
            float targetSpeed; //= isRunning ? runningSpeed : walkingSpeed;

            if (isRunning)
            {
                targetSpeed = runningSpeed;
            }
            else if(isSneaking)
            {
                targetSpeed = sneakingSpeed;
            }
            else
            {
                targetSpeed = walkingSpeed;
            }

                float curSpeedX = canMove ? targetSpeed * Input.GetAxis("Vertical") : 0; // How fast forward/backward
            float curSpeedY = canMove ? targetSpeed * Input.GetAxis("Horizontal") : 0; // How fast left/right
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

            if (playerManager != null)
            {
                if (isRunning)
                {
                    playerManager.DrainStamina(staminaDrainPerSecond * Time.deltaTime);
                }
                else if (playerManager.Stamina.current < playerManager.Stamina.max)
                {
                    playerManager.RegainStamina(staminaRegenPerSecond * Time.deltaTime);
                }
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

        private void ToggleQuestOverview()
        {
            if (uiManager != null)
            {
                uiManager.ToggleQuestOverviewUI();
            }
            else
            {
                Debug.LogWarning("PlayerController ToggleQuestOverview(): UIManager not found under player for Quest Overview toggle (D2, D9)", this);
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
             // Find the closest tile to the player
             Tile closestTile = FindClosestTile();
             
             if (closestTile != null && closestTile != previousTile)
             {
                 Debug.Log($"NEW TILE DETECTED: {closestTile.name} at coordinates {closestTile.Coordinates}");
                 OnTileEntered(closestTile);
                 previousTile = closestTile;
             }
         }
         
         private Tile FindClosestTile()
         {
             if (allTiles == null || allTiles.Length == 0) return null;
             
             Tile closestTile = null;
             float closestDistance = float.MaxValue;
             
             foreach (Tile tile in allTiles)
             {
                 if (tile == null) continue; // Skip null tiles (destroyed tiles)
                 
                 float distance = Vector3.Distance(transform.position, tile.transform.position);
                 if (distance < closestDistance && distance < 2f) // Only consider tiles within 2 units
                 {
                     closestDistance = distance;
                     closestTile = tile;
                 }
             }
             
             return closestTile;
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
            Debug.Log($"Tile GameObject: {tile.gameObject.name}");
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
        
        private void ApplyCameraCullingFix()
        {
            if (playerCamera == null) return;
            
            // Adjust camera settings to prevent floor culling when falling
            playerCamera.nearClipPlane = 0.1f; // Closer near plane
            playerCamera.farClipPlane = 2000f; // Further far plane
            playerCamera.fieldOfView = 75f; // Wider field of view
            
            Debug.Log("Camera culling fix applied - floor should remain visible when falling through broken tiles");
        }

        private void SetupTorch()
        {
            if (!enableTorch)
            {
                return;
            }

            if (playerCamera == null)
            {
                Debug.LogWarning("PlayerController: Cannot set up torch because playerCamera is null.");
                return;
            }

            GameObject torchObject = new GameObject("PlayerTorch");
            torchObject.transform.SetParent(playerCamera.transform);
            torchObject.transform.localPosition = torchLocalPosition;
            torchObject.transform.localRotation = Quaternion.Euler(torchLocalRotation);

            torchLight = torchObject.AddComponent<Light>();
            torchLight.type = LightType.Spot;
            torchLight.intensity = torchIntensity;
            torchLight.range = torchRange;
            torchLight.spotAngle = torchSpotAngle;
            torchLight.color = torchColor;
            torchLight.shadows = LightShadows.Soft;

            torchIsOn = torchStartsOn;
            torchLight.enabled = torchIsOn;
        }

        private void ToggleTorch()
        {
            if (torchLight == null)
            {
                return;
            }

            torchIsOn = !torchIsOn;
            torchLight.enabled = torchIsOn;
        }
    }
}
