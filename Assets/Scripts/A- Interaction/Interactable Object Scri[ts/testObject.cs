using UnityEngine;

public class testObject : MonoBehaviour, IInteractable
{

    [SerializeField] string message = "Press E to Change Colour";

    bool isRed;
    Renderer renderer;
    Material material;
    Color color;

    public string prompt => message;

    private void Start()
    {
        renderer = GetComponent<Renderer>();
        material = renderer.material;
        color = material.color;
        isRed = false;
    }


    public void Interact(Interactor interactor)
    {
        
        if (isRed)
        {
           material.color = color;
           isRed = false;
        }
        else
        {
            material.color = Color.red;
            isRed = true;
        }
    }

    public void OnHoverEnter(Interactor interactor)
    {
        material.color= Color.yellow;
    }

    public void OnHoverExit(Interactor interactor)
    {

        if(isRed)
        {
            material.color = Color.red;
        }
        else
        {
            material.color = color;
        }
    }
}
