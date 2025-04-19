using UnityEngine;
using System;

public class WakePainter : MonoBehaviour
{
    public Terrain terrain;
    public int Amount_normallayers;
    public GameObject treePrefab; // Reference to the tree prefab
    public GameObject tree2Prefab; // Reference to the tree prefab
    public GameObject templePrefab; // Reference to the tree prefab
    public GameObject rockPrefab; // Reference to the tree prefab
    
    public float offsetX;
    public float offsetZ;

    void Start()
    {
        for (int i = 0; i < terrain.terrainData.detailPrototypes.Length; i++) terrain.terrainData.SetDetailLayer(0, 0, i, new int[terrain.terrainData.detailWidth, terrain.terrainData.detailHeight]);

        print("Terrain painter");
        print("amount details:"+CountAllDetails().ToString());
        //Console.Writeline($"The Money you have now are: {CountAllDetails()}");
        Amount_normallayers = 4;
        PaintTerrainLayers();
        print("amount details:"+CountAllDetails().ToString());
        
    }

    void PaintTerrainLayers()
    {
        if (terrain.terrainData == null)
        {
            Debug.LogError("Terrain reference is not set!");
            return;
        }
        
        

        TerrainData terrainData = terrain.terrainData;
        int width = terrainData.alphamapWidth;
        int height = terrainData.alphamapHeight;
        float[,] heights = terrainData.GetHeights(0, 0, width, height);
        
        int heightmapWidth = terrainData.heightmapResolution;  // Get heightmap resolution
        int heightmapHeight = terrainData.heightmapResolution;
        
        print("alphamapsize: "+width.ToString());

        // Initialize the splatmap data array
        float[,,] splatmapData = new float[width, height, terrainData.alphamapLayers];


        // Initialize detail layers
        int[,] detailLayerGreen = new int[width, height];
        int[,] detailLayerForest = new int[width, height];
        int[,] detailLayerFlower = new int[width, height];
        int[,] detailLayerDry = new int[width, height];

        int detailDensity = 5;
        
        // Define and assign tree prototypes
        TreePrototype[] treePrototypes = new TreePrototype[4];
        treePrototypes[0] = new TreePrototype();
        treePrototypes[0].prefab = treePrefab;
        treePrototypes[1] = new TreePrototype();
        treePrototypes[1].prefab = tree2Prefab;
        treePrototypes[2] = new TreePrototype();
        treePrototypes[2].prefab = templePrefab;
        treePrototypes[3] = new TreePrototype();
        treePrototypes[3].prefab = rockPrefab;
        terrainData.treePrototypes = treePrototypes;

        // Initialize tree instances list
        var treeInstances = new System.Collections.Generic.List<TreeInstance>();


        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float heightmapX = ((float)x + offsetX) / width * heightmapWidth;
                float heightmapZ = ((float)z + offsetZ) / height * heightmapHeight;
                
                float terrain_height = terrainData.GetHeight(Mathf.RoundToInt(heightmapX), Mathf.RoundToInt(heightmapZ));
                float normalizedHeight = terrain_height / terrain.terrainData.bounds.max.y;
                float ho = terrain.SampleHeight(new Vector3(x, 0f, z));
                
                /////////////// OWN STEEPNESS CALCULATION
                float heightL = terrainData.GetHeight(Mathf.Clamp(x - 1, 0, width), z); // Height of left neighbor
                float heightR = terrainData.GetHeight(Mathf.Clamp(x + 1, 0, width), z); // Height of right neighbor
                float heightD = terrainData.GetHeight(x, Mathf.Clamp(z - 1, 0, height)); // Height of down neighbor
                float heightU = terrainData.GetHeight(x, Mathf.Clamp(z + 1, 0, height)); // Height of up neighbor

                // Calculate differences in height along X and Z
                float slopeX = (heightR - heightL) / (2.0f * terrainData.size.x / width); // Approximate X-axis slope
                float slopeZ = (heightU - heightD) / (2.0f * terrainData.size.z / height); // Approximate Z-axis slope

                // Use Pythagoras theorem to combine the two slopes into one final steepness value
                float steep = Mathf.Sqrt(slopeX * slopeX + slopeZ * slopeZ);
                
                
                if (normalizedHeight <= 0.10f)                                   // SAND
                {   
                    
                    if(steep > 1f){                          // TOO STEEP
                        splatmapData[z, x, 3] = 1.0f;
                    }else{splatmapData[z, x, 0] = 1.0f;} // sand}
                }
                
                else if (normalizedHeight > 0.10f && normalizedHeight <= 0.35f)    // GRASS
                {   
                    if(steep > 1f){                          // TOO STEEP
                        splatmapData[z, x, 3] = 1.0f;
                    }else{
                        splatmapData[z, x, 1] = 1.0f; // grass

                        if (!((x > 280) && (x<440) &&(z>350) &&(z<410))){              // NOT IN AIRPORT
                            
                                                                                        // Spawn trees
                            if ((UnityEngine.Random.value < 0.005f)) // cherry blossom
                            {
                                //new Vector3(treeInstancePos.x * terrainData.size.x, treeInstancePos.y * terrainData.size.y, treeInstancePos.z * terrainData.size.z);
                                TreeInstance treeInstance = new TreeInstance
                                {
                                    position = new Vector3((float)x/terrainData.size.x, ho/ terrainData.size.y, (float)z/terrainData.size.z),
                                    widthScale = 1.0f,
                                    heightScale = 1.0f,
                                    prototypeIndex = 0};
                                treeInstances.Add(treeInstance);}else if ((UnityEngine.Random.value < 0.0000f)) // 2nd tree
                            {
                                //new Vector3(treeInstancePos.x * terrainData.size.x, treeInstancePos.y * terrainData.size.y, treeInstancePos.z * terrainData.size.z);
                                TreeInstance treeInstance = new TreeInstance
                                {
                                    position = new Vector3((float)x/terrainData.size.x, ho/ terrainData.size.y, (float)z/terrainData.size.z),
                                    widthScale = 2f,
                                    heightScale = 2f,
                                    prototypeIndex = 1};
                                treeInstances.Add(treeInstance);}else if ((UnityEngine.Random.value < 0.00000f)) // PAGODA TEMPLE
                            {
                                //new Vector3(treeInstancePos.x * terrainData.size.x, treeInstancePos.y * terrainData.size.y, treeInstancePos.z * terrainData.size.z);
                                TreeInstance treeInstance = new TreeInstance
                                {
                                    position = new Vector3((float)x/terrainData.size.x, ho/ terrainData.size.y, (float)z/terrainData.size.z),
                                    widthScale = 6f,
                                    heightScale = 6f,
                                    prototypeIndex = 2};
                                treeInstances.Add(treeInstance);} else if ((UnityEngine.Random.value < 0.000f)) // STONES
                            {
                                //new Vector3(treeInstancePos.x * terrainData.size.x, treeInstancePos.y * terrainData.size.y, treeInstancePos.z * terrainData.size.z);
                                TreeInstance treeInstance = new TreeInstance
                                {
                                    position = new Vector3((float)x/terrainData.size.x, ho/ terrainData.size.y, (float)z/terrainData.size.z),
                                    widthScale = 1.0f,
                                    heightScale = 1.0f,
                                    prototypeIndex = 3};
                                treeInstances.Add(treeInstance);}
                                
                            }else{
                                //detailLayerForest[z, x] = 2;                        // grass in airport
                            }
                    }
                }else{                                                              /// SNOWY VOLCANO
                    if(steep > 1.5f){                          // TOO STEEP
                            splatmapData[z, x, 3] = 1.0f;
                    }else{
                        splatmapData[z, x, 1] = 1.0f; // grass
                        //   SNOWY MOUNTAIN OVERRIDE

                        // splatmapData[z, x, 3] = 1.0f;  // steep

                        float nh = normalizedHeight-0.45f;
                        float prob = 10f*nh;

                        if ((UnityEngine.Random.value < prob)){
                            splatmapData[z, x, 4] = 1.0f;

                        }else{
                            splatmapData[z, x, 1] = 1.0f;
                        }
                    }}
                  
                
            }
        }
        print("splat length"+splatmapData.Length.ToString());
        
        // Apply splatmap data to the terrain
        terrainData.SetAlphamaps(0, 0, splatmapData);
        //terrainData.SetDetailLayer(0, 0, 0, detailLayerGreen);
        //terrainData.SetDetailLayer(0, 0, 1, detailLayerForest);
        //terrainData.SetDetailLayer(0, 0, 0, detailLayerGreen);
        
        terrainData.treeInstances = treeInstances.ToArray();
        terrain.Flush(); // Apply changes to the terrain
        terrain.terrainData.RefreshPrototypes();    
        
        
    }

    private float IsNeighborDetail(int[,] detailLayer, int x, int z, int width, int height)
    {
        float prob = 0.4f;

        // Check if any neighbor has details
        if (x > 0 && detailLayer[x - 1, z] > 0) // Left neighbor
        {
            prob += 0.2f;
        }
        else if (x < width - 1 && detailLayer[x + 1, z] > 0) // Right neighbor
        {
            prob += 0.2f;
        }
        else if (z > 0 && detailLayer[x, z - 1] > 0) // Bottom neighbor
        {
            prob += 0.2f;
        }
        else if (z < height - 1 && detailLayer[x, z + 1] > 0) // Top neighbor
        {
            prob += 0.2f;
        }

        return prob;
    }
    
    public int CountAllDetails()
    {
    int totalDetails = 0;
    TerrainData terrainData = terrain.terrainData;

    // Loop through each detail layer
    for (int layer = 0; layer < terrainData.detailPrototypes.Length; layer++)
    {
        // Get the detail layer data
        int[,] detailLayer = terrainData.GetDetailLayer(0, 0, terrainData.detailWidth, terrainData.detailHeight, layer);
        
        // Count the details in the layer
        for (int y = 0; y < terrainData.detailHeight; y++)
        {
            for (int x = 0; x < terrainData.detailWidth; x++)
            {
                totalDetails += detailLayer[x, y];
            }
        }
    }

    return totalDetails;
}

}
