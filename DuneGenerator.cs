using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DuneGenerator : MonoBehaviour
{
    // Reference to the terrain
    public Terrain terrain;
    private TerrainData terrainData;

    // Settings for dune generation
    public float duneHeight = 10f; // Maximum height of dunes
    public float duneScale = 0.1f; // Controls the frequency of dunes
    public float stretchX = 1.5f; // Controls stretching along X axis to create wind direction look
    public float detailScale = 0.02f; // Smaller scale for fine details (like smaller ripples)
    
    public int initial_trees = 20;
    private List<Vector2> init_spawnpos;
    
    public GameObject Airportprefab;

    // Prefabs to spawn
    public GameObject stonePrefab;
    public GameObject treePrefab;
    public GameObject housePrefab;
    public GameObject otherPrefab; // Another object, e.g., cactus

    // Probabilities for spawning prefabs
    [Range(0, 1)] public float stoneProbability = 0.25f;
    [Range(0, 1)] public float treeProbability = 0.25f;
    [Range(0, 1)] public float houseProbability = 0.2f;
    [Range(0, 1)] public float otherProbability = 0.3f;

    // Initialize and generate the terrain
    void Start()
    {
        // Get the terrain and its data
        terrainData = terrain.terrainData;

        // Generate the dune-like terrain
        if (terrainData == null)
        {
            print("Could not find terrain");
        }
        Airport();
    }
    

    
    void Airport(){
        Vector3 position = new Vector3(800f, 15f, 200f);
        Vector3 eulerAngles = new Vector3(0f, 90f, 0f); // Example angles
        Quaternion rotation = Quaternion.Euler(eulerAngles);

        Instantiate(Airportprefab, position, rotation);
        
        
    }

    // Method to generate the dunes
    public void GenerateDunes()
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
                heights[x, z] = finalHeight / terrainData.size.y; // Normalize by terrain height
                
                
                // Airport plane
                if ((x > 10) && (x < 200) && (z > 450) && (z < 500)){
                    
                    heights[x, z] = 20f / terrainData.size.y; // Normalize by terrain height
                }}}
        
        // Apply the heightmap to the terrain
        terrainData.SetHeights(0, 0, heights);
        int range = 20;
        int treeAreas = 20;
        
        for (int i = 0; i< treeAreas; i++){
            
            int rand_x = (int)Random.Range(0, width*2);
            int rand_z = (int)Random.Range(0, height*2);
            
           
            
            for (int x = rand_x-range; x < rand_x+range; x++)
            {
                for (int z = rand_z-range; z < rand_z+range; z++)
                {
                    //float terrainHeight = terrain.SampleHeight(spawnPosition);
                    //Vector3 worldPosition = new Vector3(spawnPosition.x, terrainHeight, spawnPosition.z);
                    // Spawn a prefab based on probabilities

                    SpawnPrefabAt(x, z);

                }}}

        

        Debug.Log("Dune landscape generated!");
    }


    void SpawnPrefabAt(int x, int z)
    {
        // Grouping is a coefficient, 1 = objects independent. != 1 : objects favour having neighbours
        float ho = terrain.SampleHeight(new Vector3(x, 0f, z));
        Vector3 worldPosition = new Vector3((float)x, ho, (float)z); 
        
        
        // Randomly determine which prefab to spawn based on probabilities
        float randomValue = Random.value; // Returns a random float between 0 and 1

        if (randomValue < stoneProbability)
        {
            GameObject stone = Instantiate(stonePrefab, worldPosition, Quaternion.identity);
            stone.transform.localScale = new Vector3(5f, 5f, 5f);
            stone.transform.position += new Vector3(0, -5f, 0);
        }
        else if (randomValue <  treeProbability)
        {
            Instantiate(treePrefab, worldPosition, Quaternion.identity);
        }
        else if (randomValue <  houseProbability)
        {
            Instantiate(housePrefab, worldPosition, Quaternion.identity);
        }
        else if (randomValue <  otherProbability)
        {
            Instantiate(otherPrefab, worldPosition, Quaternion.identity);
        }
    }
    /*
    public bool IsTreeAtPosition(Vector3 worldPosition, float checkRadius)
    {
        // Convert the world position to normalized terrain coordinates
        Vector3 terrainPos = terrain.GetPosition();
        
        // Loop through all trees and check the distance
        foreach (Vector2 treepos in init_spawnpos)
        {
            // Check the distance between the tree and the target position
            float distance = Vector2.Distance(treepos,
                                              new Vector2(worldPosition.x, worldPosition.z));
                                              
            print($"distance is {distance}");

            // If the distance is smaller than the check radius, return true (tree found)
            if (distance < checkRadius / terrainData.size.x)
            {
                return true;
            }
        }
        // No trees found within the check radius
        return false;
    }*/
}