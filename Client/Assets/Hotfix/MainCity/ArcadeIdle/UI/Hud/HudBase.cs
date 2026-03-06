using UnityEngine;

public class HudBase : MonoBehaviour
{

    [SerializeField, Tooltip("The offset from the displayer's position to place the order info on screen.")]
    protected Vector3 displayOffset = new Vector3(0f, 2.5f, 0f);

    public Transform Target { get; set; } // Transform of the object (e.g., a customer) the order info is attached to
    protected Camera mainCamera; // Main camera reference to convert world position to screen position

    protected virtual void Awake()
    {
        mainCamera = Camera.main;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected virtual void Start()
    {

    }

    protected virtual void OnEnable()
    {
        mainCamera = Camera.main;
    }

    protected virtual void OnDisable()
    {

    }

    // Update is called once per frame
    protected virtual void LateUpdate()
    {
        if (Target == null) return; // If no displayer is set, do nothing

        // Calculate the display position of the order info in screen space
        var displayPosition = Target.position + displayOffset;
        transform.position = mainCamera.WorldToScreenPoint(displayPosition); // Set the position of the UI element on screen
    }
}
