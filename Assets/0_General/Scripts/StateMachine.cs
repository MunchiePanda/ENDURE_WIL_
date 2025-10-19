using System;
using System.Collections.Generic;
using UnityEngine;

namespace ENDURE
{
    /// <summary>
    /// Generic state machine that can be used for any entity (tiles, AI creatures, etc.)
    /// Supports state transitions, events, and custom behaviors
    /// 
    /// HOW TO EXTEND FOR HORROR SYSTEMS (7 Horror):
    /// 
    /// 1. CREATE YOUR ENUM:
    ///    public enum AICreatureState { Patrolling, Alert, Chasing, Searching }
    /// 
    /// 2. CREATE YOUR STATE MACHINE CLASS:
    ///    public class AICreatureStateMachine : StateMachine<AICreatureState>
    ///    {
    ///        protected override void InitializeStates()
    ///        {
    ///            states = new Dictionary<AICreatureState, StateData>
    ///            {
    ///                { AICreatureState.Patrolling, new StateData("Patrolling", OnEnterPatrolling, OnExitPatrolling, OnUpdatePatrolling) },
    ///                { AICreatureState.Alert, new StateData("Alert", OnEnterAlert, OnExitAlert, OnUpdateAlert) },
    ///                { AICreatureState.Chasing, new StateData("Chasing", OnEnterChasing, OnExitChasing, OnUpdateChasing) },
    ///                { AICreatureState.Searching, new StateData("Searching", OnEnterSearching, OnExitSearching, OnUpdateSearching) }
    ///            };
    ///        }
    ///        
    ///        protected override bool CanTransitionTo(AICreatureState newState)
    ///        {
    ///            // Define your transition rules here
    ///            switch (currentState)
    ///            {
    ///                case AICreatureState.Patrolling: return newState == AICreatureState.Alert;
    ///                case AICreatureState.Alert: return newState == AICreatureState.Chasing || newState == AICreatureState.Patrolling;
    ///                case AICreatureState.Chasing: return newState == AICreatureState.Searching || newState == AICreatureState.Patrolling;
    ///                case AICreatureState.Searching: return newState == AICreatureState.Patrolling || newState == AICreatureState.Chasing;
    ///                default: return false;
    ///            }
    ///        }
    ///        
    ///        // Implement your state event handlers
    ///        private void OnEnterPatrolling() { /* AI starts patrolling */ }
    ///        private void OnUpdatePatrolling() { /* AI patrol logic */ }
    ///        private void OnEnterChasing() { /* AI starts chasing player */ }
    ///        private void OnUpdateChasing() { /* AI chase logic */ }
    ///        // ... etc for all states
    ///    }
    /// 
    /// 3. FOR SOUND DETECTION:
    ///    Create a simple sound system:
    ///    public class SoundDetectionSystem : MonoBehaviour
    ///    {
    ///        public void CreateSoundEvent(Vector3 position, SoundType soundType, float intensity)
    ///        {
    ///            // Find all AI creatures in range
    ///            AICreatureStateMachine[] creatures = FindObjectsOfType<AICreatureStateMachine>();
    ///            foreach (var creature in creatures)
    ///            {
    ///                float distance = Vector3.Distance(position, creature.transform.position);
    ///                if (distance <= GetSoundRange(soundType, intensity))
    ///                {
    ///                    creature.OnSoundDetected(position, intensity);
    ///                }
    ///            }
    ///        }
    ///    }
    ///    
    ///    // In your AI state machine, add:
    ///    public void OnSoundDetected(Vector3 soundPosition, float intensity)
    ///    {
    ///        if (currentState == AICreatureState.Patrolling)
    ///        {
    ///            SetState(AICreatureState.Alert);
    ///        }
    ///    }
    /// 
    /// 4. FOR SIGHT DETECTION:
    ///    Add to your OnUpdate methods:
    ///    private void CheckPlayerDetection()
    ///    {
    ///        if (player == null) return;
    ///        
    ///        float distance = Vector3.Distance(transform.position, player.position);
    ///        bool hasLineOfSight = Physics.Raycast(transform.position, 
    ///            (player.position - transform.position).normalized, 
    ///            distance, obstacleLayerMask);
    ///            
    ///        if (distance <= detectionRange && hasLineOfSight)
    ///        {
    ///            if (currentState == AICreatureState.Patrolling)
    ///                SetState(AICreatureState.Alert);
    ///        }
    ///    }
    /// 
    /// 5. FOR HIDING MECHANICS:
    ///    Check if player is hiding:
    ///    private bool IsPlayerHiding()
    ///    {
    ///        // Check if player is crouching
    ///        bool isCrouching = player.GetComponent<PlayerController>().isCrouching;
    ///        
    ///        // Check if player is in grass/cover
    ///        bool inCover = Physics.CheckSphere(player.position, 1f, grassLayerMask);
    ///        
    ///        return isCrouching && inCover;
    ///    }
    ///    
    ///    // Modify detection in your state updates:
    ///    if (IsPlayerHiding())
    ///    {
    ///        detectionRange *= 0.5f; // Reduce detection when hiding
    ///    }
    /// 
    /// 6. EXAMPLE USAGE:
    ///    AICreatureStateMachine ai = GetComponent<AICreatureStateMachine>();
    ///    ai.SetState(AICreatureState.Chasing); // Start chasing
    ///    ai.OnStateEnter += (state) => Debug.Log($"AI entered {state} state");
    ///    
    ///    // Listen for state changes
    ///    ai.OnStateTransition += (from, to) => {
    ///        if (to == AICreatureState.Chasing) {
    ///            // Play chase music, increase AI speed, etc.
    ///        }
    ///    };
    /// </summary>
    public abstract class StateMachine<T> : MonoBehaviour where T : Enum
    {
        [Header("State Machine Settings")]
        [SerializeField] protected T initialState;
        [SerializeField] protected bool debugLogging = true;
        
        protected T currentState;
        protected T previousState;
        protected Dictionary<T, StateData> states;
        protected bool isTransitioning = false;
        
        // Events for state changes
        public event Action<T> OnStateEnter;
        public event Action<T> OnStateExit;
        public event Action<T, T> OnStateTransition;
        
        protected virtual void Start()
        {
            InitializeStates();
            SetState(initialState);
        }
        
        protected virtual void Update()
        {
            if (!isTransitioning)
            {
                UpdateCurrentState();
            }
        }
        
        /// <summary>
        /// Initialize all states and their behaviors
        /// Override this in derived classes to define state-specific logic
        /// </summary>
        protected abstract void InitializeStates();
        
        /// <summary>
        /// Update logic for the current state
        /// Override this in derived classes for state-specific updates
        /// </summary>
        protected abstract void UpdateCurrentState();
        
        /// <summary>
        /// Set a new state with transition validation
        /// </summary>
        public virtual bool SetState(T newState)
        {
            if (isTransitioning)
            {
                if (debugLogging) Debug.LogWarning($"StateMachine: Cannot transition from {currentState} to {newState} - already transitioning");
                return false;
            }
            
            if (EqualityComparer<T>.Default.Equals(currentState, newState))
            {
                if (debugLogging) Debug.Log($"StateMachine: Already in state {currentState}");
                return true;
            }
            
            if (!CanTransitionTo(newState))
            {
                if (debugLogging) Debug.LogWarning($"StateMachine: Cannot transition from {currentState} to {newState} - transition not allowed");
                return false;
            }
            
            return PerformStateTransition(newState);
        }
        
        /// <summary>
        /// Check if transition to new state is allowed
        /// Override this in derived classes to define transition rules
        /// </summary>
        protected virtual bool CanTransitionTo(T newState)
        {
            return true; // Default: allow all transitions
        }
        
        /// <summary>
        /// Perform the actual state transition
        /// </summary>
        protected virtual bool PerformStateTransition(T newState)
        {
            isTransitioning = true;
            
            // Exit current state
            if (states.ContainsKey(currentState))
            {
                states[currentState].OnExit?.Invoke();
                OnStateExit?.Invoke(currentState);
            }
            
            previousState = currentState;
            currentState = newState;
            
            if (debugLogging)
            {
                Debug.Log($"StateMachine: Transitioned from {previousState} to {currentState}");
            }
            
            // Enter new state
            if (states.ContainsKey(currentState))
            {
                states[currentState].OnEnter?.Invoke();
                OnStateEnter?.Invoke(currentState);
            }
            
            OnStateTransition?.Invoke(previousState, currentState);
            
            isTransitioning = false;
            return true;
        }
        
        /// <summary>
        /// Get current state
        /// </summary>
        public T GetCurrentState()
        {
            return currentState;
        }
        
        /// <summary>
        /// Get previous state
        /// </summary>
        public T GetPreviousState()
        {
            return previousState;
        }
        
        /// <summary>
        /// Check if currently in a specific state
        /// </summary>
        public bool IsInState(T state)
        {
            return EqualityComparer<T>.Default.Equals(currentState, state);
        }
        
        /// <summary>
        /// Check if currently transitioning
        /// </summary>
        public bool IsTransitioning()
        {
            return isTransitioning;
        }
        
        /// <summary>
        /// Force set state without transition validation (use with caution)
        /// </summary>
        public void ForceSetState(T newState)
        {
            if (debugLogging)
            {
                Debug.LogWarning($"StateMachine: Force setting state to {newState} from {currentState}");
            }
            
            previousState = currentState;
            currentState = newState;
            isTransitioning = false;
            
            OnStateEnter?.Invoke(currentState);
            OnStateTransition?.Invoke(previousState, currentState);
        }
    }
    
    /// <summary>
    /// Data structure for state information
    /// </summary>
    [System.Serializable]
    public class StateData
    {
        public string name;
        public Action OnEnter;
        public Action OnExit;
        public Action OnUpdate;
        
        public StateData(string stateName, Action onEnter = null, Action onExit = null, Action onUpdate = null)
        {
            name = stateName;
            OnEnter = onEnter;
            OnExit = onExit;
            OnUpdate = onUpdate;
        }
    }
}