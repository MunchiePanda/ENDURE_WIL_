using UnityEngine;
using ENDURE;
using System.Collections.Generic;

namespace ENDURE
{
    /// <summary>
    /// State machine implementation for Tile degradation system
    /// Handles Intact -> Degrading -> Broken transitions
    /// </summary>
    public class TileStateMachine : StateMachine<TileState>
    {
        [Header("Tile State Machine")]
        [SerializeField] private Tile tileComponent;
        [SerializeField] private float degradationThreshold = 1f;
        [SerializeField] private float visualUpdateInterval = 0.1f;
        
        private float lastVisualUpdate = 0f;
        
        protected override void Start()
        {
            // Get the tile component if not assigned
            if (tileComponent == null)
                tileComponent = GetComponent<Tile>();
                
            base.Start();
        }
        
        protected override void InitializeStates()
        {
            states = new Dictionary<TileState, StateData>
            {
                {
                    TileState.Intact,
                    new StateData(
                        "Intact",
                        OnEnterIntact,
                        OnExitIntact,
                        OnUpdateIntact
                    )
                },
                {
                    TileState.Degrading,
                    new StateData(
                        "Degrading",
                        OnEnterDegrading,
                        OnExitDegrading,
                        OnUpdateDegrading
                    )
                },
                {
                    TileState.Broken,
                    new StateData(
                        "Broken",
                        OnEnterBroken,
                        OnExitBroken,
                        OnUpdateBroken
                    )
                }
            };
        }
        
        protected override void UpdateCurrentState()
        {
            if (states.ContainsKey(currentState))
            {
                states[currentState].OnUpdate?.Invoke();
            }
        }
        
        protected override bool CanTransitionTo(TileState newState)
        {
            // Define allowed transitions
            switch (currentState)
            {
                case TileState.Intact:
                    return newState == TileState.Degrading || newState == TileState.Broken;
                    
                case TileState.Degrading:
                    return newState == TileState.Broken || newState == TileState.Intact;
                    
                case TileState.Broken:
                    return false; // Broken tiles cannot transition to other states
                    
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Degrade the tile and potentially trigger state transition
        /// </summary>
        public void DegradeTile(float amount)
        {
            if (tileComponent == null) return;
            
            tileComponent.degradationLevel += amount;
            tileComponent.degradationLevel = Mathf.Clamp01(tileComponent.degradationLevel);
            
            // Check for state transitions based on degradation level
            if (tileComponent.degradationLevel >= degradationThreshold && currentState != TileState.Broken)
            {
                SetState(TileState.Broken);
            }
            else if (tileComponent.degradationLevel > 0f && currentState == TileState.Intact)
            {
                SetState(TileState.Degrading);
            }
        }
        
        /// <summary>
        /// Reset tile to intact state (for regeneration)
        /// </summary>
        public void ResetTile()
        {
            if (tileComponent != null)
            {
                tileComponent.degradationLevel = 0f;
                tileComponent.isBroken = false;
            }
            SetState(TileState.Intact);
        }
        
        #region State Event Handlers
        
        private void OnEnterIntact()
        {
            if (debugLogging) Debug.Log($"Tile {GetComponent<Tile>()?.Coordinates} entered Intact state");
            if (tileComponent != null)
            {
                tileComponent.isBroken = false;
                tileComponent.currentState = TileState.Intact;
            }
        }
        
        private void OnExitIntact()
        {
            if (debugLogging) Debug.Log($"Tile {GetComponent<Tile>()?.Coordinates} exited Intact state");
        }
        
        private void OnUpdateIntact()
        {
            // Intact tiles don't need special update logic
        }
        
        private void OnEnterDegrading()
        {
            if (debugLogging) Debug.Log($"Tile {GetComponent<Tile>()?.Coordinates} entered Degrading state");
            if (tileComponent != null)
            {
                tileComponent.currentState = TileState.Degrading;
            }
        }
        
        private void OnExitDegrading()
        {
            if (debugLogging) Debug.Log($"Tile {GetComponent<Tile>()?.Coordinates} exited Degrading state");
        }
        
        private void OnUpdateDegrading()
        {
            // Update visual appearance during degradation
            if (Time.time - lastVisualUpdate >= visualUpdateInterval)
            {
                if (tileComponent != null)
                {
                    // Trigger visual update in tile component
                    tileComponent.SendMessage("UpdateVisualState", SendMessageOptions.DontRequireReceiver);
                }
                lastVisualUpdate = Time.time;
            }
        }
        
        private void OnEnterBroken()
        {
            if (debugLogging) Debug.Log($"Tile {GetComponent<Tile>()?.Coordinates} entered Broken state");
            if (tileComponent != null)
            {
                tileComponent.isBroken = true;
                tileComponent.currentState = TileState.Broken;
                // Trigger tile breaking logic
                tileComponent.SendMessage("BreakTile", SendMessageOptions.DontRequireReceiver);
            }
        }
        
        private void OnExitBroken()
        {
            if (debugLogging) Debug.Log($"Tile {GetComponent<Tile>()?.Coordinates} exited Broken state");
        }
        
        private void OnUpdateBroken()
        {
            // Broken tiles don't need update logic
        }
        
        #endregion
    }
}
