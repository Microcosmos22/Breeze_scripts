using UnityEngine;
using System;

public class GrassTerrainPainter : MonoBehaviour
{
    public Terrain terrain;
    public int Amount_normallayers;
    
    void Start()
    {
        for (int i = 0; i < terrain.terrainData.detailPrototypes.Length; i++) terrain.terrainData.SetDetailLayer(0, 0, i, new int[terrain.terrainData.detailWidth, terrain.terrainData.detailHeight]);

        print("Terrain painter");
        print("amount details:"+CountAllDetails().ToString());
        //Console.Writeline($"The Money you have now are: {CountAllDetails()}");
        Amount_normallayers = 4;
        PaintTerrainLayers();
        
        
    }

    void PaintTerrainLayers()
    {
        if (terrain.terrainData == null)
        {
            Debug.LogError("Terrain reference is not set!");
            return;
        }
        
        

        TerrainData terrainData = terrain.terrainData;
        int width = 1024; //terrainData.alphamapWidth;
        int height = 1024; //terrainData.alphamapHeight;
        float[,] heights = terrainData.GetHeights(0, 0, width, height);
        
        print("alphamapsize: "+width.ToString());

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
        TreePrototype[] treePrototypes = terrainData.treePrototypes;
        /*
        Shader newShader = Shader.Find("");
        
        foreach (TreePrototype tp in treePrototypes){
            Renderer[] renderers = treePrototypes.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.materials)
                {
                    material.shader = newShader;
                }
            }
            treePrototypes.GetComponentsInChildren<Renderer>();
        }
        
        */

        // Initialize tree instances list
        var treeInstances = new System.Collections.Generic.List<TreeInstance>();
        //var treeInstances = Terrain.activeTerrain.terrainData.treeInstances;


        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float terrain_height = terrainData.GetHeight(x, z);
                float normalizedHeight = terrain_height / 1000f; //terrain.terrainData.bounds.max.y;
                float ho = terrain.SampleHeight(new Vector3(x, 0f, z));
                float steep = (float)Math.Sin(terrainData.GetSteepness((float)x/terrainData.size.x, (float)z/terrainData.size.z)/90f);
                
                
                if (normalizedHeight <= 0.2f)                                   // GRASS
                {   // TOO STEEP
                    if(steep > 0.6f){
                        splatmapData[z, x, 1] = 1.0f;
                    }else{
                    
                    // Set splatmap for green terrain
                    splatmapData[z, x, 0] = 1.0f;
                    
                    Vector3 posvec = new Vector3((float)x/terrainData.size.x, ho/ terrainData.size.y, (float)z/terrainData.size.z);
                    
                    print(posvec);
                    
                    if (!((x<850) && (x>750) && (z<250) && (z>190))){ // AIRPORT
                    
                    if ((UnityEngine.Random.value < 0.2f)) // Adjust the probability as needed
                    {TreeInstance treeInstance = new TreeInstance
                        {position = posvec, prototypeIndex = 0};
                        treeInstances.Add(treeInstance);
                        
                        }
                        
                    if ((UnityEngine.Random.value < 0.0005f)) // Adjust the probability as needed
                    {TreeInstance treeInstance = new TreeInstance
                        {position = posvec, prototypeIndex = 0};
                        treeInstances.Add(treeInstance);}
                        
                    if ((UnityEngine.Random.value < 0.0005f)) // Adjust the probability as needed
                    {TreeInstance treeInstance = new TreeInstance
                        {position = posvec, prototypeIndex = 0};
                        treeInstances.Add(treeInstance);}
                        
                    /*if ((UnityEngine.Random.value < 0.0005f)) // Adjust the probability as needed
                    {TreeInstance treeInstance = new TreeInstance
                        {position = new Vector3((float)x/terrainData.size.x, ho/ terrainData.size.y, (float)z/terrainData.size.z), prototypeIndex = 3};
                        treeInstances.Add(treeInstance);}
                        
                    if ((UnityEngine.Random.value < 0.0005f)) // Adjust the probability as needed
                    {TreeInstance treeInstance = new TreeInstance
                        {position = new Vector3((float)x/terrainData.size.x, ho/ terrainData.size.y, (float)z/terrainData.size.z), prototypeIndex = 4};
                        treeInstances.Add(treeInstance);}
                        
                    if ((UnityEngine.Random.value < 0.0005f)) // Adjust the probability as needed
                    {TreeInstance treeInstance = new TreeInstance
                        {position = new Vector3((float)x/terrainData.size.x, ho/ terrainData.size.y, (float)z/terrainData.size.z), prototypeIndex = 5};
                        treeInstances.Add(treeInstance);}
                        
                    if ((UnityEngine.Random.value < 0.0005f)) // Adjust the probability as needed
                    {TreeInstance treeInstance = new TreeInstance
                        {position = new Vector3((float)x/terrainData.size.x, ho/ terrainData.size.y, (float)z/terrainData.size.z), prototypeIndex = 6};
                        treeInstances.Add(treeInstance);}
                        
                        */
                    // Set detail density for yellow grass
                    detailLayerGreen[z, x] = detailDensity;

                }}}
            }
        }
        print("splat length"+splatmapData.Length.ToString());
        print("amount Trees:"+treeInstances.Count.ToString());
        
        // Apply splatmap data to the terrain
        terrainData.SetAlphamaps(0, 0, splatmapData);
        terrainData.SetDetailLayer(0, 0, 0, detailLayerGreen);
        terrainData.SetDetailLayer(0, 0, 1, detailLayerForest);
        terrainData.SetDetailLayer(0, 0, 0, detailLayerGreen);
        
        
        
        //terrainData.treeInstances = treeInstances.ToArray();
        terrainData.SetTreeInstances(treeInstances.ToArray(), false);
        terrain.Flush(); // Apply changes to the terrain
        //terrain.terrainData.RefreshPrototypes();    
        
        
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
