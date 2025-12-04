// DoorController.cs
using UnityEngine;

public class DoorController : MonoBehaviour
{
    public float openAngle = 90f;
    public float openSpeed = 2f;
    public bool isOpen = false;

    private Quaternion initialRotation;
    private Quaternion targetRotation;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        initialRotation = transform.rotation;
        targetRotation = Quaternion.Euler(transform.eulerAngles + new Vector3(0, openAngle, 0));
    }

    void FixedUpdate() //FixedUpdate 處理 Rigidbody
    {
        if (isOpen)
        {
            Quaternion newRotation = Quaternion.RotateTowards(rb.rotation, targetRotation, openSpeed * Time.fixedDeltaTime * 100f);
            rb.MoveRotation(newRotation);
        }
    }

    public void OpenDoor()
    {
        isOpen = true;
    }
}