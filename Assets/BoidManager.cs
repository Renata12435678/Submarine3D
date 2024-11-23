using System.Collections.Generic;
using UnityEngine;

public class BoidManager : MonoBehaviour
{
    public List<GameObject> boidPrefabs;
    public int flockSize;
    public Vector3 spawnBounds;

    private List<GameObject> boids = new List<GameObject>();

    void Start()
    {
        GenerateUnits();
    }

    void GenerateUnits()
    {
        for (int i = 0; i < flockSize; i++)
        {
            GameObject randomPrefab = boidPrefabs[Random.Range(0, boidPrefabs.Count)];

            // √енерируем случайную позицию внутри зоны спавна
            var randomVector = UnityEngine.Random.insideUnitSphere;
            randomVector = new Vector3(randomVector.x * spawnBounds.x, randomVector.y * spawnBounds.y, randomVector.z * spawnBounds.z);
            var spawnPosition = transform.position + randomVector;

            Quaternion prefabRotation = randomPrefab.transform.rotation;
            var rotation = Quaternion.Euler(prefabRotation.eulerAngles.x, Random.Range(0f, 360f), 0f);

            GameObject boid = Instantiate(randomPrefab, spawnPosition, rotation);
        }
    }

}
