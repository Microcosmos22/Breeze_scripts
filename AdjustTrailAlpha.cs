using UnityEngine;

public class AdjustTrailAlpha : MonoBehaviour
{
    public TrailRenderer trailRenderer;
    public float alpha = 1.0f; // Alpha value from 0 to 1

    void Start()
    {
        if (trailRenderer == null)
        {
            trailRenderer = GetComponent<TrailRenderer>();
        }

        if (trailRenderer != null)
        {
            SetTrailAlpha(trailRenderer, alpha);
        }
    }

    void SetTrailAlpha(TrailRenderer trail, float alpha)
    {
        Gradient gradient = trail.colorGradient;
        GradientColorKey[] colorKeys = gradient.colorKeys;
        GradientAlphaKey[] alphaKeys = gradient.alphaKeys;

        for (int i = 0; i < alphaKeys.Length; i++)
        {
            alphaKeys[i].alpha = alpha;
        }

        Gradient newGradient = new Gradient();
        newGradient.SetKeys(colorKeys, alphaKeys);

        trail.colorGradient = newGradient;
    }
}
