using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using System.Collections;
using System.Collections.Generic;

namespace Project
{
    public class ClothSimulator
    {
        public ComputeBuffer<float3> _velocities;
        public ComputeBuffer<float3> _predictedPositions;
        public ComputeBuffer<float3> _masses;

        public static readonly float3 G = new float3(0, -9.81f, 0);
        public const int MAX_DIST_CONSTRAINT = 8;

        private ComputeBuffer<ConstraintType> _constraintTypes;
        private ComputeBuffer<DistanceConstraint> _distanceConstraints;
        private ComputeBuffer<BendConstraint> _bendConstraints;
        private ComputeBuffer<PinConstraint> _pinConstraints;
        private ComputeBuffer<CollisionConstraint> _collisionConstraints;
        private ComputeBuffer<ColliderGroup> _positionCorrect;

        private ColliderGroup _colliderDescGroup;
        private Dictionary<int, RigidbodyDesc> _rigidbodyProxies = new Dictionary<int, RigidbodyDesc>();
        private ComputeBuffer<RigidbodyForceApply> _rigidbodyForces;

        private float3 _forces;
        private ClothMeshModifier _meshModifier;
        private ClothSettings _settings;
    

        public ClothSimulator(ClothMeshModifier meshModifier, ClothSetting settings)
        {
            _meshModifier = meshModifier;
            _settings = settings;

            var vertexCount = meshModifier.vertexCount;
            _predictedPositions = new ComputeBuffer<float3>(vertexCount, ComputeBufferType.Default);
            _predictedPositions.SetData(meshModifier.vertices.ToArray());

            _velocities = new ComputeBuffer<float3>(vertexCount, sizeof(float) * 3);
            
            _masses = new ComputeBuffer<float3>(vertexCount, sizeof(float) * 3);
            InitializeBuffer<float>(_masses, vertexCount, 0);

            _constraintTypes = new ComputeBuffer<ConstraintType>(vertexCount, sizeof(int));
            InitializeBuffer<int>(_constraintTypes, vertexCount, 0);

            _distanceConstraints = new ComputeBuffer(vertexCount * MAX_DIST_CONSTRAINT, sizeof(float) * 4);
            _bendConstraints = new ComputeBuffer(vertexCount, sizeof(float) * 4);
            _collisionConstraints = new ComputeBuffer(vertexCount, sizeof(float) * 12);
            _pinConstraints = new ComputeBuffer(vertexCount, sizeof(float) * 4);

            _positionCorrect = new ComputeBuffer(vertexCount, sizeof(float) * 4);

            _rigidbodyForces = new ComputeBuffer<RigidbodyForceApply>(vertexCount, sizeof(float) * 4);

            _colliderDescGroup = new ColliderGroup(Allocator.Persistent);

            BuildMasses();
            BuildDistConstraints();
            BuildBendConstraints();
        }

        private void InitializeBuffer<T>(ComputeBuffer<T> buffer, int count, T value) where T : struct
        {
            T[] data = new T[count];
            for (var i = 0; i < count; i++)
            {
                data[i] = value;
            }
            buffer.SetData(data);
        }

        private void BuildMasses()
        {
            var indices = _meshModifier.indices;
            var vertices = _meshModifier.vertices;

            float[] masses = new float[_meshModifier.vertexCount];
            for (int i = 0; i < indices.Length; i += 3)
            {
                var i0 = indices[i];
                var i1 = indices[i + 1];
                var i2 = indices[i + 2];

                var v0 = vertices[i0];
                var v1 = vertices[i1];
                var v2 = vertices[i2];

                var area = Vector3.Cross(v1 - v0, v2 - v0).magnitude/2f;
                var mass = area * _settings.density/3f;
                masses[i0] += mass / 3;
                masses[i1] += mass / 3;
                masses[i2] += mass / 3;
            }

            _masses.SetData(masses);
        }

        private void BuildDistConstraints()
        {
            var edges = _meshModifier.edges;
            List<DistanceConstraintInfo> constraints = new List<DistanceConstraintInfo>();

            foreach (var edge in edges)
            {
                id0 = edge.vIndex0;
                id1 = edge.vIndex1;
                float restLength = Vector3.Distance(_meshModifier.vertices[id0], _meshModifier.vertices[id1]);
                constraints.Add(new DistanceConstraintInfo(id0, id1, restLength));
            }

            _distanceConstraints.SetData(constraints.ToArray());
        }

        private void BuildBendConstraints()
        {
            var indices = _meshModifier.indices;
            List<BendConstraintInfo> constraints = new List<BendConstraintInfo>();

            for (int i = 0; i < indices.Length; i += 3)
            {
                var i0 = indices[i];
                var i1 = indices[i + 1];
                var i2 = indices[i + 2];

                constraints.Add(new BendConstraintInfo(i0, i1, i2, i0, i2, i1));
                constraints.Add(new BendConstraintInfo(i1, i2, i0, i1, i0, i2));
                constraints.Add(new BendConstraintInfo(i2, i0, i1, i2, i1, i0));
            }

            _bendConstraints.SetData(constraints.ToArray());
        }

        public void Dipose()
        {
            _predictedPositions.Dispose();
            _velocities.Dispose();
            _masses.Dispose();
            _constraintTypes.Dispose();
            _distanceConstraints.Dispose();
            _bendConstraints.Dispose();
            _collisionConstraints.Dispose();
            _pinConstraints.Dispose();
            _positionCorrect.Dispose();
            _rigidbodyForces.Dispose();
            _colliderDescGroup.Dispose();
        }

        
    }
}