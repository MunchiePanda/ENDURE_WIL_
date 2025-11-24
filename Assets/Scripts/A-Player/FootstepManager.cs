using ENDURE;
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

        [Tooltip("In order: Walking, Running")]
        public AudioClip[] clips;       
    }

    [Header("Detection")]
    public float raycastDistance = 1.5f;

    [Tooltip("Minimum horizontal speed before footsteps start playing.")]
    public float minMoveSpeed = 0.2f;

    public LayerMask groundLayers = ~0;      //Set To Everything

    [Header("Audio")]
    [Tooltip("Footstep clips for each surface type here.")]
    public SurfaceAudio[] surfaceAudios;
    public SurfaceType defaultSurface = SurfaceType.Default;

    [Header("Movement")]
    private int audioIndex = 0;         //Default to 0 || AKA Walking
    private PlayerController player;

    public AudioSource audioSource;     //Will Default to Players Audio Sauce

    private Dictionary<SurfaceType, AudioClip[]> footsteps;
    private CharacterController controller;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        footsteps = new Dictionary<SurfaceType, AudioClip[]>();
      
        foreach (var audio in surfaceAudios)
        {
            if (audio == null || audio.clips == null)       //Keep from being rewritten
                continue;

            footsteps[audio.surfaceType] = audio.clips;
        }

        controller = GetComponent<CharacterController>();
        player = GetComponent<PlayerController>();

    }

    private void Update()
    {
        HandleFootsteps();

        //Shall the monster come for you???
        if(audioSource.isPlaying)
        {
            AlertMonstersNearby();
        }

    }

    private void AlertMonstersNearby()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        
        if(enemies.Length == 0 || enemies == null)
        {
            Debug.Log("enemies is empty or null");
            return;
        }

        EnemyBehaviour behaviour;
        
        foreach(var enemy in enemies)
        {
            behaviour = enemy.GetComponent<EnemyBehaviour>();
            behaviour.HearNoise(transform.position, audioSource.volume);
        }
    }



    private void HandleFootsteps()
    {
        float speed = controller.velocity.magnitude;

        if((player.sneakingSpeed + 0.3f) > speed)
        {
            //We Moving But
            //Not moving fast enough, Make no Sound (Sneaky Sneaky)
            audioSource.Stop();
            return; 
        }

        // Check if we are Running??
        if (player.isRunning == true)
        {
            // We are Running Boys
            audioIndex = 1;
            audioSource.volume = 1;             //Adjust Volume based on Speed
        }
        else
        {
            // We are not running, but we do be moving
            audioIndex = 0;
            audioSource.volume = 0.5f;
        }

            TryPlayFootsteps();
    }

    private void TryPlayFootsteps()
    {

        if(footsteps == null)
        {
            Debug.LogWarning("Footstep Dictionary is Null");
            return;
        }

        Ray ray = new Ray(transform.position, Vector3.down);

        if (!Physics.Raycast(ray, out RaycastHit hit, raycastDistance, groundLayers))
        {
            //Not Grounded (Don't Play)
            Debug.Log("No Ground");

            audioSource.Stop();
            return;
        }

        SurfaceType surfaceType = defaultSurface;   //Just in Case
        FootstepSurface surface = hit.collider.gameObject.GetComponent<FootstepSurface>();

        if(audioSource.isPlaying == true && audioSource.clip == footsteps[surface.surfaceType][audioIndex])
        {
            //Already doing the right things
            return;
        }

        //Switch to the Audio Clip from the Person Walking.
        audioSource.clip = footsteps[surface.surfaceType][audioIndex];

        //Audio source is set to Loop.
        audioSource.Play();

    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * raycastDistance);  
    }
}

