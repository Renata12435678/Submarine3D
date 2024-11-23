using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;

public class FlockUnit : MonoBehaviour
{
    [SerializeField] private float FOVAngle;
    [SerializeField] private float smoothDamp;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private Vector3[] directionsToCheckWhenAvoidingObstacles;
    static readonly ProfilerMarker s_PreparePerfMarker = new ProfilerMarker("FlockUnits");

    private List<FlockUnit> cohesionNeighbours = new List<FlockUnit>();
    private List<FlockUnit> avoidanceNeighbours = new List<FlockUnit>();
    private List<FlockUnit> aligementNeighbours = new List<FlockUnit>();
    private Flock assignedFlock;
    private Vector3 currentVelocity;
    private Vector3 currentObstacleAvoidanceVector;
    private float speed;

    private Vector3 initialRotation; // Сохраняем начальное вращение (X и Z)
    public Transform myTransform { get; set; }

    private void Awake()
    {
        myTransform = transform;
        // Сохраняем начальные значения поворота
        initialRotation = new Vector3(myTransform.eulerAngles.x, myTransform.eulerAngles.y, myTransform.eulerAngles.z);
    }

    public void AssignFlock(Flock flock)
    {
        assignedFlock = flock;
    }

    public void InitializeSpeed(float speed)
    {
        this.speed = speed;
    }

    public void MoveUnit()
    {
        s_PreparePerfMarker.Begin();
        FindNeighbours();
        CalculateSpeed();

        var cohesionVector = CalculateCohesionVector() * assignedFlock.cohesionWeight;
        var avoidanceVector = CalculateAvoidanceVector() * assignedFlock.avoidanceWeight;
        var aligementVector = CalculateAligementVector() * assignedFlock.aligementWeight;
        var boundsVector = CalculateBoundsVector() * assignedFlock.boundsWeight;
        var obstacleVector = CalculateObstacleVector() * assignedFlock.obstacleWeight;

        var moveVector = cohesionVector + avoidanceVector + aligementVector + boundsVector + obstacleVector;
        moveVector = Vector3.SmoothDamp(myTransform.forward, moveVector, ref currentVelocity, smoothDamp);
        moveVector = moveVector.normalized * speed;

        if (moveVector == Vector3.zero)
            moveVector = transform.forward;

        // Поворот по оси Y (X и Z сохраняются)
        myTransform.eulerAngles = new Vector3(initialRotation.x, Mathf.Atan2(moveVector.x, moveVector.z) * Mathf.Rad2Deg, initialRotation.z);

        // Перемещение: меняется X, Y остаётся фиксированным
        myTransform.position = new Vector3(myTransform.position.x + moveVector.x * Time.deltaTime,
                                            myTransform.position.y,
                                            myTransform.position.z + moveVector.z * Time.deltaTime);

        s_PreparePerfMarker.End();
    }

    private void FindNeighbours()
    {
        cohesionNeighbours.Clear();
        avoidanceNeighbours.Clear();
        aligementNeighbours.Clear();

        var allUnits = assignedFlock.allUnits;
        for (int i = 0; i < allUnits.Length; i++)
        {
            var currentUnit = allUnits[i];
            if (currentUnit != this)
            {
                float currentNeighbourDistanceSqr = Vector3.SqrMagnitude(currentUnit.myTransform.position - myTransform.position);
                if (currentNeighbourDistanceSqr <= assignedFlock.cohesionDistance * assignedFlock.cohesionDistance)
                {
                    cohesionNeighbours.Add(currentUnit);
                }
                if (currentNeighbourDistanceSqr <= assignedFlock.avoidanceDistance * assignedFlock.avoidanceDistance)
                {
                    avoidanceNeighbours.Add(currentUnit);
                }
                if (currentNeighbourDistanceSqr <= assignedFlock.aligementDistance * assignedFlock.aligementDistance)
                {
                    aligementNeighbours.Add(currentUnit);
                }
            }
        }
    }

    private void CalculateSpeed()
    {
        if (cohesionNeighbours.Count == 0)
            return;

        speed = 0;
        for (int i = 0; i < cohesionNeighbours.Count; i++)
        {
            speed += cohesionNeighbours[i].speed;
        }

        speed /= cohesionNeighbours.Count;
        speed = Mathf.Clamp(speed, assignedFlock.minSpeed, assignedFlock.maxSpeed);
    }

    private Vector3 CalculateCohesionVector()
    {
        var cohesionVector = Vector3.zero;
        if (cohesionNeighbours.Count == 0)
            return Vector3.zero;

        int neighboursInFOV = 0;
        for (int i = 0; i < cohesionNeighbours.Count; i++)
        {
            var neighbourPos = cohesionNeighbours[i].myTransform.position;
            if (IsInFOV(neighbourPos) && Vector3.Distance(neighbourPos, myTransform.position) > assignedFlock.avoidanceDistance * 0.5f)
            {
                neighboursInFOV++;
                cohesionVector += neighbourPos;
            }
        }

        if (neighboursInFOV == 0)
            return Vector3.zero;

        cohesionVector /= neighboursInFOV;
        cohesionVector -= myTransform.position;
        cohesionVector = cohesionVector.normalized;
        return cohesionVector;
    }

    private Vector3 CalculateAvoidanceVector()
    {
        var avoidanceVector = Vector3.zero;
        if (avoidanceNeighbours.Count == 0)
            return Vector3.zero;

        int neighboursInFOV = 0;
        for (int i = 0; i < avoidanceNeighbours.Count; i++)
        {
            var neighbourPos = avoidanceNeighbours[i].myTransform.position;
            if (IsInFOV(neighbourPos))
            {
                neighboursInFOV++;
                var directionAway = myTransform.position - neighbourPos;
                avoidanceVector += directionAway.normalized / directionAway.magnitude;
            }
        }

        if (neighboursInFOV == 0)
            return Vector3.zero;

        avoidanceVector /= neighboursInFOV;
        avoidanceVector = avoidanceVector.normalized;
        return avoidanceVector * 1.5f;
    }

    private Vector3 CalculateAligementVector()
    {
        var aligementVector = Vector3.zero;
        if (aligementNeighbours.Count == 0)
            return myTransform.forward;

        int neighboursInFOV = 0;
        for (int i = 0; i < aligementNeighbours.Count; i++)
        {
            if (IsInFOV(aligementNeighbours[i].myTransform.position))
            {
                neighboursInFOV++;
                aligementVector += aligementNeighbours[i].myTransform.forward;
            }
        }

        if (neighboursInFOV == 0)
            return myTransform.forward;

        aligementVector /= neighboursInFOV;
        aligementVector = aligementVector.normalized;
        return aligementVector;
    }

    private Vector3 CalculateBoundsVector()
    {
        var offsetToCenter = assignedFlock.transform.position - myTransform.position;
        var distanceToCenter = offsetToCenter.magnitude;

        if (distanceToCenter < assignedFlock.boundsDistance * 0.5f)
            return Vector3.zero;

        var boundsInfluence = Mathf.Clamp01((distanceToCenter - assignedFlock.boundsDistance * 0.5f) / (assignedFlock.boundsDistance * 0.5f));
        return offsetToCenter.normalized * boundsInfluence;
    }

    private Vector3 CalculateObstacleVector()
    {
        var obstacleVector = Vector3.zero;

        RaycastHit hit;
        if (Physics.Raycast(myTransform.position, myTransform.forward, out hit, assignedFlock.obstacleDistance, obstacleMask))
        {
            obstacleVector = FindBestDirectionToAvoidObstacle();
        }
        else
        {
            currentObstacleAvoidanceVector = Vector3.zero;
        }

        return obstacleVector;
    }

    private Vector3 FindBestDirectionToAvoidObstacle()
    {
        if (currentObstacleAvoidanceVector != Vector3.zero)
        {
            RaycastHit hit;
            if (!Physics.Raycast(myTransform.position, myTransform.forward, out hit, assignedFlock.obstacleDistance, obstacleMask))
            {
                return currentObstacleAvoidanceVector;
            }
        }

        float maxDistance = int.MinValue;
        Vector3 selectedDirection = Vector3.zero;

        for (int i = 0; i < directionsToCheckWhenAvoidingObstacles.Length; i++)
        {
            Vector3 currentDirection = myTransform.TransformDirection(directionsToCheckWhenAvoidingObstacles[i]);
            Ray ray = new Ray(myTransform.position, currentDirection);
            RaycastHit hit;

            if (!Physics.Raycast(ray, out hit, assignedFlock.obstacleDistance, obstacleMask))
            {
                float currentDistance = Vector3.Distance(ray.origin, hit.point);
                if (currentDistance > maxDistance)
                {
                    maxDistance = currentDistance;
                    selectedDirection = currentDirection;
                }
            }
        }

        currentObstacleAvoidanceVector = selectedDirection;
        return selectedDirection;
    }

    private bool IsInFOV(Vector3 position)
    {
        Vector3 toTarget = position - myTransform.position;
        float angle = Vector3.Angle(myTransform.forward, toTarget);
        return angle <= FOVAngle;
    }
}