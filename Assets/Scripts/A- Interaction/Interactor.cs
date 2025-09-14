using NUnit.Framework;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class Interactor : MonoBehaviour
{
    //Placed on player/Main Cam
    [Header("Casting")]
    private Camera cam;
    [SerializeField, Min(0.1f)] float maxDist = 3f;
    [SerializeField] LayerMask interactMask;        //The Layer that all interactable objecst will be on.

    [Header("UI")]
    public TextMeshProUGUI promptText;

    private RaycastHit[] hits = new RaycastHit[5];      //Reads 5 closest colliders
    private IInteractable interactable;

    private void Start()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        int count = Physics.RaycastNonAlloc(ray, hits, maxDist, interactMask, QueryTriggerInteraction.Ignore);
        IInteractable found = null;     //Need to check if Raycast hit inherits from IInteractable;

        float best = float.MaxValue;

        for(int i = 0; i < count; i++)
        {
            var hit = hits[i];
            if (!hit.collider) continue;        //No need to run rest of loop

            var candidate = hit.collider.GetComponentInParent<IInteractable>();
            if (candidate == null) continue;

            if (hit.distance < best)            //We want to get the closest object to the Player/ Camera if the ray cast hits multiple objects
            {
                best = hit.distance;
                found = candidate;
            }

            hits[i] = default;      //want to clear it out
        }

        if(!ReferenceEquals(interactable, found))   //Only if they are not referring to the same instance, meaning that we have looked at a different object
        {
            if(interactable != null)            //if exist, tell old target -> "I'm not looking at you"
            {
                interactable.OnHoverExit(this);
                promptText.text = string.Empty;
            }

            interactable = found;           //new current target

            if (interactable != null)           //We are now looking at you
            {
                interactable.OnHoverEnter(this);
                promptText.text = interactable.prompt;
            }
        }


        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) TryInteract();

    }//end update

    private void TryInteract()
    {
        if (interactable != null)
        {
            interactable.Interact(this);
        }
        else { Debug.LogWarning("Not looking at anything interactable"); }


    }


}

public interface IInteractable
{
    public string prompt {  get;}     //text that pops up on HUD/Canvas that shows the player how to interact with the object

    void OnHoverEnter(Interactor interactor);       //when raycast hits but no message has been made.
    void OnHoverExit(Interactor interactor);        //when raycast leaves unprompted

    void Interact(Interactor interactor);
}
