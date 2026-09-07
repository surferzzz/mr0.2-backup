using UnityEngine;

namespace CPRSimulation
{
    [DefaultExecutionOrder(10000)]
    [DisallowMultipleComponent]
    public sealed class NoFallBoundaryClamp : MonoBehaviour
    {
        [Header("World Bounds")]
        public float minX = -3.75f;
        public float maxX = 3.75f;
        public float minZ = -2.8f;
        public float maxZ = 3.3f;

        [Header("Vertical Guard")]
        public bool clampY = true;
        public float minY = 0f;
        public bool useMaxY;
        public float maxY = 3f;

        [Header("Reference")]
        public Transform referenceTransform;
        public bool clampReferenceTransform;

        [Header("Physics")]
        public bool clearVelocityOnClamp = true;

        private Rigidbody _rigidbody;
        private CharacterController _characterController;

        private void Awake()
        {
            CacheComponents();
        }

        private void LateUpdate()
        {
            CacheComponents();

            if (clampReferenceTransform && referenceTransform != null)
            {
                ClampUsingReferenceTransform();
                return;
            }

            Vector3 current = transform.position;
            Vector3 clamped = ClampPosition(current);

            if ((clamped - current).sqrMagnitude <= 0.000001f)
            {
                return;
            }

            ApplyClampedPosition(current, clamped);
        }

        private void ClampUsingReferenceTransform()
        {
            Vector3 referenceCurrent = referenceTransform.position;
            Vector3 referenceClamped = ClampPosition(referenceCurrent);

            if ((referenceClamped - referenceCurrent).sqrMagnitude <= 0.000001f)
            {
                return;
            }

            Vector3 current = transform.position;
            Vector3 clamped = current + (referenceClamped - referenceCurrent);
            ApplyClampedPosition(current, clamped);
        }

        private Vector3 ClampPosition(Vector3 position)
        {
            position.x = Mathf.Clamp(position.x, minX, maxX);
            position.z = Mathf.Clamp(position.z, minZ, maxZ);

            if (clampY)
            {
                position.y = Mathf.Max(position.y, minY);
                if (useMaxY)
                {
                    position.y = Mathf.Min(position.y, maxY);
                }
            }

            return position;
        }

        private void CacheComponents()
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }

            if (_characterController == null)
            {
                _characterController = GetComponent<CharacterController>();
            }
        }

        private void ApplyClampedPosition(Vector3 current, Vector3 clamped)
        {
            if (_characterController != null)
            {
                bool wasEnabled = _characterController.enabled;
                if (wasEnabled)
                {
                    _characterController.enabled = false;
                }

                transform.position = clamped;

                if (wasEnabled)
                {
                    _characterController.enabled = true;
                }

                return;
            }

            if (_rigidbody != null && !_rigidbody.isKinematic)
            {
                _rigidbody.position = clamped;

                if (clearVelocityOnClamp)
                {
                    Vector3 velocity = _rigidbody.velocity;
                    if (!Mathf.Approximately(current.x, clamped.x)) velocity.x = 0f;
                    if (!Mathf.Approximately(current.y, clamped.y)) velocity.y = 0f;
                    if (!Mathf.Approximately(current.z, clamped.z)) velocity.z = 0f;
                    _rigidbody.velocity = velocity;
                }

                return;
            }

            transform.position = clamped;
        }

        private void OnValidate()
        {
            if (maxX < minX)
            {
                (minX, maxX) = (maxX, minX);
            }

            if (maxZ < minZ)
            {
                (minZ, maxZ) = (maxZ, minZ);
            }

            if (maxY < minY)
            {
                (minY, maxY) = (maxY, minY);
            }
        }
    }
}
