using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUFlock : MonoBehaviour
{

    private struct Boid
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 acceleration;
        public int prefabIndex; // Индекс префаба, материала и шейдера
    }

    private const int BOID_SIZE = 40; // 3*3*sizeof(float) + sizeof(int)
    private const int THREAD_GROUPS = 256;

    [Header("Boid Assets")]
    public Mesh[] meshes;
    public int submeshIndex = 0;  // For multi-part meshes
    public Material[] materials;
    public ComputeShader[] computeShaders;

    [Header("Simulation Settings")]
    public int count = 5000;
    public float boundaryRadius = 1000f;
    public float spawnRadius = 100f;

    [Header("Boid Settings")]
    public float maxVelocity = 1.75f;
    public float maxSteeringForce = 0.03f;
    public float seperationDistance = 35.0f;
    public float neighborDistance = 50.0f;
    [Range(0f, 360f)]
    public float fieldOfView = 300f;

    [Header("Force Weight Adjustments")]
    public float seperationScale = 1.5f;
    public float alignmentScale = 1.0f;
    public float cohesionScale = 1.0f;
    public float boundaryScale = 1.0f;

    private ComputeBuffer argsBuffer;
    private ComputeBuffer boidBuffer;

    private int computeKernel;
    private ComputeShader selectedComputeShader;
    private Material selectedMaterial;
    private Mesh selectedMesh;

    private Boid[] boids;

    void Start()
    {
        if (count < 1) count = 1;

        // Создаем массив аргументов для отрисовки
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

        // Создаем и заполняем массив боидов
        boids = new Boid[count];
        for (int i = 0; i < count; i++)
        {
            int prefabIndex = Random.Range(0, meshes.Length);
            boids[i].position = Random.insideUnitSphere * spawnRadius;
            boids[i].velocity = Random.insideUnitSphere;
            boids[i].acceleration = Vector3.zero;
            boids[i].prefabIndex = prefabIndex;
        }

        // Передаем данные в ComputeBuffer
        boidBuffer = new ComputeBuffer(count, BOID_SIZE);
        boidBuffer.SetData(boids);

        // Инициализируем шейдеры и материалы
        UpdateShaderSettings();

        // Передаем данные в выбранный ComputeShader и Material
        selectedComputeShader.SetBuffer(computeKernel, "boidBuffer", boidBuffer);
        selectedComputeShader.SetInt("count", count);
        selectedMaterial.SetBuffer("boidBuffer", boidBuffer);

        Shader.WarmupAllShaders();
    }

    void Update()
    {
        // Обновляем параметры шейдера при необходимости
        UpdateShaderSettings();

        // Запуск ComputeShader
        selectedComputeShader.Dispatch(computeKernel, count / THREAD_GROUPS + 1, 1, 1);

        // Рендеринг всех боидов с разными материалами и мешами
        for (int i = 0; i < meshes.Length; i++)
        {
            Material material = materials[i];
            Mesh mesh = meshes[i];

            // Обновляем аргументы для текущего меша
            uint[] args = new uint[5] {
                (uint)mesh.GetIndexCount(0),
                (uint)boids.Length,
                (uint)mesh.GetIndexStart(0),
                (uint)mesh.GetBaseVertex(0),
                0
            };
            argsBuffer.SetData(args);

            material.SetPass(0);
            Graphics.DrawMeshInstancedIndirect(mesh, submeshIndex, material,
                new Bounds(transform.position, Vector3.one * 2f * boundaryRadius), argsBuffer);
        }
    }

    private void UpdateShaderSettings()
    {
        // Обновляем настройки и данные для шейдера, если они изменились
        if (selectedComputeShader == null || selectedMaterial == null)
        {
            // Случайно выбираем шейдер, материал и меш
            int index = Random.Range(0, meshes.Length);
            selectedMesh = meshes[index];
            selectedMaterial = materials[index];
            selectedComputeShader = computeShaders[index];
            computeKernel = selectedComputeShader.FindKernel("CSMain");
        }

        // Передаем текущие параметры в ComputeShader
        selectedComputeShader.SetFloat("boundaryRadius", boundaryRadius);
        selectedComputeShader.SetFloat("maxVelocity", maxVelocity);
        selectedComputeShader.SetFloat("maxSteeringForce", maxSteeringForce);
        selectedComputeShader.SetFloat("seperationDistance", seperationDistance);
        selectedComputeShader.SetFloat("neighborDistance", neighborDistance);
        selectedComputeShader.SetFloat("fieldOfView", fieldOfView);
        selectedComputeShader.SetFloat("seperationScale", seperationScale);
        selectedComputeShader.SetFloat("alignmentScale", alignmentScale);
        selectedComputeShader.SetFloat("cohesionScale", cohesionScale);
        selectedComputeShader.SetFloat("boundaryScale", boundaryScale);
    }

    void OnDestroy()
    {
        if (argsBuffer != null) argsBuffer.Release();
        if (boidBuffer != null) boidBuffer.Release();
    }
}
