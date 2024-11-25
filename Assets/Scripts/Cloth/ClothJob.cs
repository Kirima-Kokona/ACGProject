using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Project
{
    public class ClothSimulator : MonoBehaviour
    {
        public ComputeShader computeShader;

        private ComputeBuffer positionsBuffer;
        private ComputeBuffer velocitiesBuffer;
        private ComputeBuffer massesBuffer;
        private ComputeBuffer normalsBuffer;
        private ComputeBuffer predictPositionsBuffer;

        public Vector3[] positions;
        public Vector3[] velocities;
        public float[] masses;
        public Vector3[] normals;
        public Vector3[] predictPositions;

        public Vector3 fieldForce;
        public float damper;
        public float deltaTime;

        void Start()
        {
            int count = positions.Length;

            positionsBuffer = new ComputeBuffer(count, sizeof(float) * 3);
            velocitiesBuffer = new ComputeBuffer(count, sizeof(float) * 3);
            massesBuffer = new ComputeBuffer(count, sizeof(float));
            normalsBuffer = new ComputeBuffer(count, sizeof(float) * 3);
            predictPositionsBuffer = new ComputeBuffer(count, sizeof(float) * 3);

            positionsBuffer.SetData(positions);
            velocitiesBuffer.SetData(velocities);
            massesBuffer.SetData(masses);
            normalsBuffer.SetData(normals);
        }

        void Update()
        {
            PositionEstimate();
            predictPositionsBuffer.GetData(predictPositions);
        }

        void PositionEstimate()
        {
            int kernel = computeShader.FindKernel("CSPositionEstimate");
            computeShader.SetBuffer(kernel, "positions", positionsBuffer);
            computeShader.SetBuffer(kernel, "velocities", velocitiesBuffer);
            computeShader.SetBuffer(kernel, "masses", massesBuffer);
            computeShader.SetBuffer(kernel, "normals", normalsBuffer);
            computeShader.SetBuffer(kernel, "predictPositions", predictPositionsBuffer);
            computeShader.SetVector("fieldForce", fieldForce);
            computeShader.SetFloat("damper", damper);
            computeShader.SetFloat("deltaTime", deltaTime);
            computeShader.Dispatch(kernel, positions.Length / 64, 1, 1);
        }

        private void OnDestroy()
        {
            positionsBuffer.Release();
            velocitiesBuffer.Release();
            massesBuffer.Release();
            normalsBuffer.Release();
            predictPositionsBuffer.Release();
        }

        StructuredBuffer<float3> positions;
        StructuredBuffer<float3> velocities;
        StructuredBuffer<float> masses;
        StructuredBuffer<float3> normals;
        RWStructuredBuffer<float3> predictPositions;

        float3 fieldForce;
        float damper;
        float deltaTime;

        [numthreads(64, 1, 1)]
        void CSPositionEstimate(uint id : SV_DispatchThreadID)
        {
            float3 p = positions[id];
            float3 v = velocities[id];
            float m = masses[id];

            if (m > 0)
            {
                float3 normal = normals[id];
                float3 fieldForceAtNormal = dot(fieldForce, normal) * normal;
                float3 v1 = v + float3(0, -9.8f, 0) * deltaTime + fieldForceAtNormal * deltaTime / m;
                v1 *= max(0, (1 - damper * deltaTime / m));
                float3 p1 = p + v1 * deltaTime;
                predictPositions[id] = p1;
            }
            else
            {
                predictPositions[id] = p;
            }
        }

        ComputeBuffer distanceConstraintsBuffer;
        ComputeBuffer positionCorrectsBuffer;

        void Start()
        {
            distanceConstraintsBuffer = new ComputeBuffer(distanceConstraints.Length, sizeof(float) * 4 + sizeof(int) * 2);
            positionCorrectsBuffer = new ComputeBuffer(positions.Length, sizeof(float3));

            distanceConstraintsBuffer.SetData(distanceConstraints);
            positionCorrectsBuffer.SetData(new Vector3[positions.Length]);
        }

        void DistanceConstraintJob()
        {
            int kernel = computeShader.FindKernel("CSDistanceConstraintJob");

            computeShader.SetBuffer(kernel, "predictPositions", predictPositionsBuffer);
            computeShader.SetBuffer(kernel, "masses", massesBuffer);
            computeShader.SetBuffer(kernel, "distanceConstraints", distanceConstraintsBuffer);
            computeShader.SetBuffer(kernel, "positionCorrects", positionCorrectsBuffer);

            computeShader.SetFloat("compressStiffness", compressStiffness);
            computeShader.SetFloat("stretchStiffness", stretchStiffness);
            computeShader.SetFloat("di", 1.0f / iterations);

            computeShader.Dispatch(kernel, distanceConstraints.Length / 64, 1, 1);
        }

        StructuredBuffer<float3> predictPositions;
        StructuredBuffer<float> masses;
        StructuredBuffer<DistanceConstraintInfo> distanceConstraints;
        RWStructuredBuffer<float3> positionCorrects;

        float compressStiffness;
        float stretchStiffness;
        float di;

        [numthreads(64, 1, 1)]
        void CSDistanceConstraintJob(uint id : SV_DispatchThreadID)
        {
            DistanceConstraintInfo constraint = distanceConstraints[id];
            float3 p0 = predictPositions[constraint.vIndex0];
            float3 p1 = predictPositions[constraint.vIndex1];
            float m0 = masses[constraint.vIndex0];
            float m1 = masses[constraint.vIndex1];

            float3 distV = p1 - p0;
            float length = length(distV);
            float3 normal = distV / length;

            float err = length - constraint.restLength;
            float3 correct = err < 0 ? compressStiffness * normal * err : stretchStiffness * normal * err;

            float totalM = m0 + m1;
            positionCorrects[constraint.vIndex0] += correct * di * m1 / totalM;
            positionCorrects[constraint.vIndex1] -= correct * di * m0 / totalM;
        }
    }
}
