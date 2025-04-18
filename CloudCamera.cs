using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CloudCamera : MonoBehaviour
{
    public Material cloudMaterial; // Assign your cloud material in the Inspector

    //protected Material _clearColorMaterial;



    void Start()
    {
        /*if (_clearColorMaterial == null)
        {
            _clearColorMaterial = Resources.Load<Material>("CloudMaterial"); // Make sure the material is in "Assets/Resources/"
            if (_clearColorMaterial == null)
            {
                Debug.LogError("CloudCamera3D: _clearColorMaterial is missing! Please assign it in the Inspector or place a material named 'ClearColorMaterial' in Resources.");
            }
        }*/
        // Ensure the main camera has depth texture enabled for depth information
        Camera.main.depthTextureMode |= DepthTextureMode.Depth;
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (cloudMaterial != null)
        {
            // Render the clouds to the destination using the assigned cloud material
            Graphics.Blit(source, destination, cloudMaterial);
        }
        else
        {
            // Fallback if cloudMaterial is not assigned
            Graphics.Blit(source, destination);
        }
    }
}
