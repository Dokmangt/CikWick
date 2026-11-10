using UnityEngine;

public class TPS_Camera : MonoBehaviour
{
    [Header("References")]
   [SerializeField] private Transform playertransform;
   [SerializeField] private Transform orientationtransform;
   [SerializeField] private Transform playervisualtransform;
   [Header("Camera Settings")]
    [SerializeField] private float rotationSpeed;
    void Start()
    {
        
    }

    void Update()
    {
        
        Vector3 viewdirection = playertransform.position - new Vector3(transform.position.x, playertransform.position.y, transform.position.z);
        orientationtransform.forward = viewdirection.normalized;
        float horizontalinput = Input.GetAxis("Horizontal");
        float verticalinput = Input.GetAxis("Vertical");
        Vector3 inputdirection = orientationtransform.forward * verticalinput + orientationtransform.right * horizontalinput;
        if(inputdirection.magnitude != 0.1f)
        {
                    playervisualtransform.forward = Vector3.Slerp(playervisualtransform.forward, inputdirection.normalized, rotationSpeed * Time.deltaTime);

        }
        
    }
}
