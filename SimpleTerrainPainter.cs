using UnityEngine;
using System;

public class SimpleTerrainPainter : MonoBehaviour
{
    public Terrain terrain;
    public int Amount_normallayers;
    public GameObject treePrefab0; // Reference to the tree prefab
    public GameObject treePrefab1;
    public GameObject treePrefab2;

    public float offsetX;
    public float offsetZ;

private float[,] treedensity = new float[3, 4]
{
    {0f, 0.001f, 0.005f, 0f},
    {0f, 0f,    0.001f, 0.001f},
    {0.0005f, 0.0005f, 0f,    0f}
};

    void Start(){
        for (int i = 0; i < terrain.terrainData.detailPrototypes.Length; i++)
        {terrain.terrainData.SetDetailLayer(0, 0, i, new int[terrain.terrainData.detailWidth, terrain.terrainData.detailHeight]);
        }

        Amount_normallayers = 4;
        PaintTerrainLayers();
        print($" Amount of details spawned: {CountAllDetails()}");
    }

    void PaintTerrainLayers(){
        if (terrain.terrainData == null){
            Debug.LogError("Terrain reference is not set!");
            return;
        }
        TerrainData terrainData = terrain.terrainData;

        int heightmapWidth = terrainData.heightmapResolution;  // Get heightmap resolution
        int heightmapHeight = terrainData.heightmapResolution;

        int width = terrainData.alphamapWidth;
        int height = terrainData.alphamapHeight;
        float[,] heights = terrainData.GetHeights(0, 0, width, height);


        // Initialize the splatmap data array
        float[,,] splatmapData = new float[width, height, terrainData.alphamapLayers];

        DetailPrototype[] detailPrototypes = terrainData.detailPrototypes;
        detailPrototypes[0].renderMode = DetailRenderMode.GrassBillboard;
        detailPrototypes[1].renderMode = DetailRenderMode.GrassBillboard;
        detailPrototypes[2].renderMode = DetailRenderMode.GrassBillboard;
        detailPrototypes[3].renderMode = DetailRenderMode.GrassBillboard;

        // Initialize detail layers
        int[,] detailLayerGreen = new int[width, height];
        int[,] detailLayerForest = new int[width, height];
        int[,] detailLayerFlower = new int[width, height];
        int[,] detailLayerDry = new int[width, height];

        int detailDensity = 5;

        // Define and assign tree prototypes
        TreePrototype[] treePrototypes = new TreePrototype[3];
        treePrototypes[0] = new TreePrototype();
        treePrototypes[1] = new TreePrototype();
        treePrototypes[2] = new TreePrototype();
        treePrototypes[0].prefab = treePrefab0;
        treePrototypes[1].prefab = treePrefab1;
        treePrototypes[2].prefab = treePrefab2;
        terrainData.treePrototypes = treePrototypes;

        // Initialize tree instances list
        var treeInstances = new System.Collections.Generic.List<TreeInstance>();


        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                // Map alphamap coordinates to heightmap coordinates
                float heightmapX = ((float)x + offsetX) / width * heightmapWidth;
                float heightmapZ = ((float)z + offsetZ) / height * heightmapHeight;

                float terrain_height = terrainData.GetHeight(Mathf.RoundToInt(heightmapX), Mathf.RoundToInt(heightmapZ));
                float normalizedHeight = terrain_height / terrain.terrainData.bounds.max.y;
                float ho = terrain.SampleHeight(new Vector3(x, 0f, z));
                //float steep = (float)Math.Sin(terrainData.GetSteepness((float)x/terrainData.size.x, (float)z/terrainData.size.z)/90f);

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
                ///////////////



                if (steep > 3.0f){                                                  //    CLIFF
                    splatmapData[z, x, 2] = 1.0f;
                }

                else if (normalizedHeight <= 0.2f){       // GRASS
                    if (steep < 0.3f){ // Farm fields
                        splatmapData[z, x, 5] = 1.0f;
                        //splatmapData[z, x, 1] = 1.0f;
                    } else if (steep > 1.5f){ // Hills with trees
                        if (normalizedHeight > 0.06f){   // Spawn fruit trees
                            if ((UnityEngine.Random.value < treedensity[2,0])){

                            TreeInstance treeInstance = new TreeInstance
                            {
                                position = new Vector3((float)x/terrainData.size.x, ho/ terrainData.size.y, (float)z/terrainData.size.z),
                                widthScale = 3.0f,
                                heightScale = 3.0f,
                                prototypeIndex = 2};
                            treeInstances.Add(treeInstance);}}
                      splatmapData[z, x, 0] = 1.0f;
                      detailLayerGreen[z, x] = detailDensity;

                    }else{              // Grass terrain
                        if (normalizedHeight > 0.06f){   // Spawn fruit trees
                    if ((UnityEngine.Random.value < treedensity[2,0])){
                            TreeInstance treeInstance = new TreeInstance
                            {
                                position = new Vector3((float)x/terrainData.size.x, ho/ terrainData.size.y, (float)z/terrainData.size.z),
                                widthScale = 3.0f,
                                heightScale = 3.0f,
                                prototypeIndex = 2};
                            treeInstances.Add(treeInstance);}}
                        splatmapData[z, x, 0] = 1.0f;
                    }
                    detailLayerGreen[z, x] = detailDensity;
                }else if (normalizedHeight > 0.2f && normalizedHeight <= 0.3f){  // NOT DENSE FOREST
                    float xnmh = normalizedHeight;

                    // Set splatmap for forests
                    splatmapData[z, x, 1] = 1.0f;
                    detailLayerGreen[z, x] = 2;
                    detailLayerFlower[z, x] = 2;


                    if ((UnityEngine.Random.value < treedensity[0,1])){
                        TreeInstance treeInstance = new TreeInstance
                        {
                            position = new Vector3((float)x/terrainData.size.x, ho/ terrainData.size.y, (float)z/terrainData.size.z),
                            widthScale = 3.0f,
                            heightScale = 3.0f,
                            prototypeIndex = 0};
                        treeInstances.Add(treeInstance);}

                        if ((UnityEngine.Random.value < treedensity[1,1])){
                        TreeInstance treeInstance = new TreeInstance
                        {
                            position = new Vector3((float)x/terrainData.size.x, ho/ terrainData.size.y, (float)z/terrainData.size.z),
                            widthScale = 3.0f,
                            heightScale = 3.0f,
                            prototypeIndex = 1};
                        treeInstances.Add(treeInstance);}

                    }
                else if (normalizedHeight > 0.3f && normalizedHeight <= 0.4f)    // YES DENSE FOREST
                {
                    float xnmh = normalizedHeight;

                    // Set splatmap for forests
                    splatmapData[z, x, 1] = 1.0f;
                    detailLayerForest[z, x] = 2;
                    detailLayerFlower[z, x] = 2;


                    if ((UnityEngine.Random.value < treedensity[0,2]*1.5f)){
                        TreeInstance treeInstance = new TreeInstance
                        {
                            position = new Vector3((float)x/terrainData.size.x, ho/ terrainData.size.y, (float)z/terrainData.size.z),
                            widthScale = 3.0f,
                            heightScale = 3.0f,
                            prototypeIndex = 0};
                        treeInstances.Add(treeInstance);}

                        if ((UnityEngine.Random.value < treedensity[1,2])){
                        TreeInstance treeInstance = new TreeInstance
                        {
                            position = new Vector3((float)x/terrainData.size.x, ho/ terrainData.size.y, (float)z/terrainData.size.z),
                            widthScale = 3.0f,
                            heightScale = 3.0f,
                            prototypeIndex = 1};
                        treeInstances.Add(treeInstance);}

                    }

                else                                                            //MOUNTAIN
                {
                    float nh = normalizedHeight-0.4f;
                    float prob = 5f*nh;

                    if ((UnityEngine.Random.value < prob)){
                        splatmapData[z, x, 3] = 1.0f;

                    }else{
                        splatmapData[z, x, 1] = 1.0f;
                    }

                    if ((UnityEngine.Random.value < treedensity[1,3])){
                        TreeInstance treeInstance = new TreeInstance{
                            position = new Vector3((float)x/terrainData.size.x, ho/ terrainData.size.y, (float)z/terrainData.size.z),
                            widthScale = 3.0f,
                            heightScale = 3.0f,
                            prototypeIndex = 1};
                        treeInstances.Add(treeInstance);}
                }


            }
        }
        print("splat length"+splatmapData.Length.ToString());

        // Apply splatmap data to the terrain
        terrainData.SetAlphamaps(0, 0, splatmapData);
        terrainData.SetDetailLayer(0, 0, 0, detailLayerGreen);
        terrainData.SetDetailLayer(0, 0, 1, detailLayerForest);
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
