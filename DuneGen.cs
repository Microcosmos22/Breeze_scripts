using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DuneTerrainGenerator : MonoBehaviour
{
    // Reference to the terrain
    private Terrain terrain;
    private TerrainData terrainData;

    // Settings for dune generation
    public float duneHeight = 10f; // Maximum height of dunes
    public float duneScale = 0.1f; // Controls the frequency of dunes
    public float stretchX = 1.5f; // Controls stretching along X axis to create wind direction look
    public float detailScale = 0.02f; // Smaller scale for fine details (like smaller ripples)

    // Initialize and generate the terrain
    void Start()
    {
        // Get the terrain and its data
        terrain = GetComponent<Terrain>();
        terrainData = terrain.terrainData;

        // Generate the dune-like terrain
        GenerateDunes();
    }

    // Method to generate the dunes
    void GenerateDunes()
    {
        // Get the terrain heightmap dimensions
        int width = terrainData.heightmapResolution;
        int height = terrainData.heightmapResolution;

        // Create a new heightmap array
        float[,] heights = terrainData.GetHeights(0, 0, width, height); // Start with existing heights

        // Loop through each point in the heightmap
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                // Normalize x and z to range [0, 1]
                float normalizedX = (float)x / (float)width;
                float normalizedZ = (float)z / (float)height;

                // Apply Perlin noise to create base dune shape
                float baseDune = Mathf.PerlinNoise(normalizedX * duneScale * stretchX, normalizedZ * duneScale);

                // Add smaller noise for ripples or fine details
                float fineDetail = Mathf.PerlinNoise(normalizedX * detailScale, normalizedZ * detailScale);

                // Combine base dunes and fine detail, scale by max height
                float finalHeight = (baseDune + fineDetail * 0.2f) * duneHeight;

                // Set the height in the heightmap array
                heights[x, z] = Mathf.Clamp(finalHeight / terrainData.size.y, 0f, 1f); // Normalized by terrain height
            }
        }

        // Apply the heightmap to the terrain
        terrainData.SetHeights(0, 0, heights);

        Debug.Log("Dune landscape generated!");
    }
}
