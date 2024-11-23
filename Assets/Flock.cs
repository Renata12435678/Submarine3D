using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

public class Flock : MonoBehaviour
{
    [Header("Spawn Setup")]
    [SerializeField] private FlockUnit[] flockUnitPrefabs; // Массив префабов
    [SerializeField] private int flockSize;
    [SerializeField] private Vector3 spawnBounds;
    static readonly ProfilerMarker s1_PreparePerfMarker = new ProfilerMarker("MoveUnit_in_Flock");

    [Header("Speed Setup")]
    [Range(0, 10)]
    [SerializeField] private float _minSpeed;
    public float minSpeed { get { return _minSpeed; } }
    [Range(0, 10)]
    [SerializeField] private float _maxSpeed;
    public float maxSpeed { get { return _maxSpeed; } }

    [Header("Detection Distances")]
    [Range(0, 10)]
    [SerializeField] private float _cohesionDistance;
    public float cohesionDistance { get { return _cohesionDistance; } }
    [Range(0, 10)]
    [SerializeField] private float _avoidanceDistance;
    public float avoidanceDistance { get { return _avoidanceDistance; } }
    [Range(0, 10)]
    [SerializeField] private float _aligementDistance;
    public float aligementDistance { get { return _aligementDistance; } }
    [Range(0, 10)]
    [SerializeField] private float _obstacleDistance;
    public float obstacleDistance { get { return _obstacleDistance; } }
    [Range(0, 100)]
    [SerializeField] private float _boundsDistance;
    public float boundsDistance { get { return _boundsDistance; } }

    [Header("Behaviour Weights")]
    [Range(0, 10)]
    [SerializeField] private float _cohesionWeight;
    public float cohesionWeight { get { return _cohesionWeight; } }
    [Range(0, 10)]
    [SerializeField] private float _avoidanceWeight;
    public float avoidanceWeight { get { return _avoidanceWeight; } }
    [Range(0, 10)]
    [SerializeField] private float _aligementWeight;
    public float aligementWeight { get { return _aligementWeight; } }
    [Range(0, 10)]
    [SerializeField] private float _boundsWeight;
    public float boundsWeight { get { return _boundsWeight; } }
    [Range(0, 100)]
    [SerializeField] private float _obstacleWeight;
    public float obstacleWeight { get { return _obstacleWeight; } }

    public FlockUnit[] allUnits { get; set; }

    private void Start()
    {
        GenerateUnits();

        // Добавляем случайное разлётывание юнитов после генерации
        foreach (var unit in allUnits)
        {
            unit.myTransform.position += UnityEngine.Random.onUnitSphere * 2f;
        }
    }

    private void Update()
    {
        s1_PreparePerfMarker.Begin();
        for (int i = 0; i < allUnits.Length; i++)
        {
            allUnits[i].MoveUnit();
        }
        s1_PreparePerfMarker.End();
    }

    private void GenerateUnits()
    {
        allUnits = new FlockUnit[flockSize];
        for (int i = 0; i < flockSize; i++)
        {
            // Генерация случайной позиции в пределах границ
            Vector3 randomVector = new Vector3(
                UnityEngine.Random.Range(-spawnBounds.x, spawnBounds.x),
                UnityEngine.Random.Range(-spawnBounds.y, spawnBounds.y),
                UnityEngine.Random.Range(-spawnBounds.z, spawnBounds.z)
            );
            randomVector += new Vector3(i % 10, (i / 10) % 10, i / 100); // Для более равномерного распределения

            var spawnPosition = transform.position + randomVector;
            var rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);

            // Выбираем случайный префаб
            var randomPrefab = flockUnitPrefabs[UnityEngine.Random.Range(0, flockUnitPrefabs.Length)];

            allUnits[i] = Instantiate(randomPrefab, spawnPosition, rotation);
            allUnits[i].AssignFlock(this);
            allUnits[i].InitializeSpeed(UnityEngine.Random.Range(minSpeed, maxSpeed));

            // Поворот рыбы с сохранением её исходных осей X и Y
            var prefabRotation = randomPrefab.transform.rotation;
            allUnits[i].myTransform.rotation = Quaternion.Euler(prefabRotation.eulerAngles.x, prefabRotation.eulerAngles.y, prefabRotation.eulerAngles.z);
        }
    }
}
