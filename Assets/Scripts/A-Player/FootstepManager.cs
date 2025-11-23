using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class FootstepManager : MonoBehaviour
{
    public enum SurfaceType {Default, Grass, Gravel, Concrete, Metal}
    
    [System.Serializable]
    public class SurfaceAudio
    {
        public SurfaceType surfaceType;
        public AudioClip clips;       
    }

    [Header("Detection")]
    public float raycastDistance = 1.5f;

    [Tooltip("Minimum horizontal speed before footsteps start playing.")]
    public float minMoveSpeed = 0.2f;

    public LayerMask groundLayers = ~0;      //Set To Everything

    [Tooltip("Distance the player must move between footsteps.")]
    public float stepDistance = 2.0f;

    [Header("Audio")]
    [Tooltip("Footstep clips for each surface type here.")]
    public SurfaceAudio[] surfaceAudios;
    public SurfaceType defaultSurface = SurfaceType.Default;

    public AudioSource audioSource; //Will Default to Players Audio Sauce

    private Dictionary<SurfaceType, AudioClip> footsteps;
    private CharacterController characterController;
    private Vector3 lastPosition;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        footsteps = new Dictionary<SurfaceType, AudioClip>();
      
        foreach (var audio in surfaceAudios)
        {
            if (audio == null || audio.clips == null)       //Keep from being rewritten
                continue;

            footsteps[audio.surfaceType] = audio.clips;
        }

        lastPosition = transform.position;
        characterController = GetComponent<CharacterController>();

    }

    private void Update()
    {
        HandleFootsteps();

        Debug.Log($"Foot Step noises are currently playing: {audioSource.isPlaying}");
    }

    private void HandleFootsteps()
    {
        float speed = characterController.velocity.magnitude;
        //Debug.Log($"Player Speed is {speed}");

        if(minMoveSpeed > speed)
        {
            //Not moving fast enough, Make no Sound (Sneaky Sneaky)
            audioSource.Stop();
            return; 
        }

        TryPlayFootsteps();

    }

    private void TryPlayFootsteps()
    {
        if(footsteps == null)
        {
            Debug.LogWarning("Footstep Disctionary is Null");
            return;
        }

        Ray ray = new Ray(transform.position, Vector3.down);

        if (!Physics.Raycast(ray, out RaycastHit hit, raycastDistance, groundLayers))
        {
            //Not Grounded (Don't Play)
            audioSource.Stop();
            return;
        }

        SurfaceType surfaceType = defaultSurface;   //Just in Case
        FootstepSurface surface = hit.collider.gameObject.GetComponent<FootstepSurface>();

        if(audioSource.isPlaying == true && audioSource.clip == footsteps[surface.surfaceType])
        {
            //Already doing the right things
            return;
        }

        //Switch to the Audio Clip from the Person Walking.
        audioSource.clip = footsteps[surface.surfaceType];

        //Audio source is set to Loop.
        audioSource.Play();


    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * raycastDistance);  
    }
}

