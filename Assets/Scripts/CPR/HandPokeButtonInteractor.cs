using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CPRSimulation
{
    public sealed class HandPokeButtonInteractor : MonoBehaviour
    {
        private const string RuntimeName = "HandPokeButtonInteractor";

        [SerializeField] private float pressDepthMeters = 0.16f;
        [SerializeField] private float releaseDepthMeters = 0.24f;
        [SerializeField] private float pressCooldownSeconds = 0.35f;
        [SerializeField] private bool showDebugFingerTips = true;

        private readonly Dictionary<Button, float> lastPressTimes = new Dictionary<Button, float>();
        private readonly HashSet<Button> pressedButtons = new HashSet<Button>();
        private readonly List<Button> buttonBuffer = new List<Button>(32);

        private OVRHand leftHand;
        private OVRHand rightHand;
        private OVRSkeleton leftSkeleton;
        private OVRSkeleton rightSkeleton;
        private Transform leftIndexTip;
        private Transform rightIndexTip;
        private OVRCameraRig cameraRig;
        private GameObject leftDebugTip;
        private GameObject rightDebugTip;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindObjectOfType<HandPokeButtonInteractor>() != null)
            {
                return;
            }

            if (FindObjectOfType<Canvas>() == null)
            {
                return;
            }

            var host = new GameObject(RuntimeName);
            DontDestroyOnLoad(host);
            host.AddComponent<HandPokeButtonInteractor>();
        }

        private void Awake()
        {
            EnsureEventSystem();
            ResolveHands();
            CreateDebugTips();
        }

        private void Update()
        {
            if (Time.frameCount % 60 == 0)
            {
                ResolveHands();
            }

            bool hasLeft = TryGetIndexTip(leftHand, leftSkeleton, ref leftIndexTip, out Vector3 leftTip);
            bool hasRight = TryGetIndexTip(rightHand, rightSkeleton, ref rightIndexTip, out Vector3 rightTip);

            UpdateDebugTip(leftDebugTip, hasLeft, leftTip);
            UpdateDebugTip(rightDebugTip, hasRight, rightTip);

            if (!hasLeft && !hasRight)
            {
                pressedButtons.Clear();
                return;
            }

            buttonBuffer.Clear();
            buttonBuffer.AddRange(FindObjectsOfType<Button>(false));

            var releasedButtons = ListPool<Button>.Get();
            foreach (Button button in pressedButtons)
            {
                if (!IsButtonPressedByAnyFinger(button, hasLeft, leftTip, hasRight, rightTip, releaseDepthMeters))
                {
                    releasedButtons.Add(button);
                }
            }

            foreach (Button releasedButton in releasedButtons)
            {
                pressedButtons.Remove(releasedButton);
            }

            ListPool<Button>.Release(releasedButtons);

            foreach (Button button in buttonBuffer)
            {
                if (button == null || !button.isActiveAndEnabled || !button.interactable)
                {
                    continue;
                }

                if (pressedButtons.Contains(button))
                {
                    continue;
                }

                if (!IsButtonPressedByAnyFinger(button, hasLeft, leftTip, hasRight, rightTip, pressDepthMeters))
                {
                    continue;
                }

                if (lastPressTimes.TryGetValue(button, out float lastPressTime) &&
                    Time.unscaledTime - lastPressTime < pressCooldownSeconds)
                {
                    continue;
                }

                pressedButtons.Add(button);
                lastPressTimes[button] = Time.unscaledTime;
                PressButton(button);
            }
        }

        private void ResolveHands()
        {
            if (cameraRig == null)
            {
                cameraRig = FindObjectOfType<OVRCameraRig>();
            }

            if (rightHand == null && cameraRig != null && cameraRig.rightHandAnchor != null)
            {
                rightHand = cameraRig.rightHandAnchor.GetComponentInChildren<OVRHand>(true);
            }

            if (leftHand == null && cameraRig != null && cameraRig.leftHandAnchor != null)
            {
                leftHand = cameraRig.leftHandAnchor.GetComponentInChildren<OVRHand>(true);
            }

            if (leftHand == null || rightHand == null)
            {
                var hands = FindObjectsOfType<OVRHand>(true);
                foreach (OVRHand hand in hands)
                {
                    var skeleton = hand.GetComponent<OVRSkeleton>();
                    if (skeleton == null)
                    {
                        continue;
                    }

                    string skeletonType = skeleton.GetSkeletonType().ToString();
                    if (leftHand == null && skeletonType.IndexOf("Left", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        leftHand = hand;
                    }
                    else if (rightHand == null && skeletonType.IndexOf("Right", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        rightHand = hand;
                    }
                }
            }

            if (leftHand != null && leftSkeleton == null)
            {
                leftSkeleton = leftHand.GetComponent<OVRSkeleton>();
            }

            if (rightHand != null && rightSkeleton == null)
            {
                rightSkeleton = rightHand.GetComponent<OVRSkeleton>();
            }
        }

        private bool TryGetIndexTip(OVRHand hand, OVRSkeleton skeleton, ref Transform cachedTip, out Vector3 position)
        {
            position = Vector3.zero;

            if (hand == null || !hand.IsTracked || skeleton == null)
            {
                return false;
            }

            if (cachedTip == null)
            {
                cachedTip = FindIndexTip(skeleton);
            }

            if (cachedTip == null)
            {
                return false;
            }

            position = cachedTip.position;
            return position.sqrMagnitude > 0.0001f;
        }

        private Transform FindIndexTip(OVRSkeleton skeleton)
        {
            if (skeleton == null || skeleton.Bones == null)
            {
                return null;
            }

            foreach (var bone in skeleton.Bones)
            {
                string boneName = bone.Id.ToString();
                if (boneName.IndexOf("IndexTip", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return bone.Transform;
                }
            }

            foreach (var bone in skeleton.Bones)
            {
                string boneName = bone.Id.ToString();
                if (boneName.IndexOf("Index3", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return bone.Transform;
                }
            }

            return null;
        }

        private bool IsButtonPressedByAnyFinger(
            Button button,
            bool hasLeft,
            Vector3 leftTip,
            bool hasRight,
            Vector3 rightTip,
            float depthMeters
        )
        {
            return (hasLeft && IsFingerPressingButton(leftTip, button, depthMeters)) ||
                   (hasRight && IsFingerPressingButton(rightTip, button, depthMeters));
        }

        private bool IsFingerPressingButton(Vector3 fingerPosition, Button button, float depthMeters)
        {
            var rectTransform = button.transform as RectTransform;
            if (rectTransform == null)
            {
                return false;
            }

            Vector3 localPoint = rectTransform.InverseTransformPoint(fingerPosition);
            Rect rect = rectTransform.rect;
            if (localPoint.x < rect.xMin || localPoint.x > rect.xMax ||
                localPoint.y < rect.yMin || localPoint.y > rect.yMax)
            {
                return false;
            }

            float planeDistance = Mathf.Abs(Vector3.Dot(fingerPosition - rectTransform.position, rectTransform.forward));
            return planeDistance <= depthMeters;
        }

        private void PressButton(Button button)
        {
            var pointerData = EventSystem.current != null
                ? new PointerEventData(EventSystem.current) { position = Vector2.zero }
                : null;

            if (pointerData != null)
            {
                ExecuteEvents.Execute(button.gameObject, pointerData, ExecuteEvents.pointerDownHandler);
                ExecuteEvents.Execute(button.gameObject, pointerData, ExecuteEvents.pointerUpHandler);
            }

            button.onClick.Invoke();
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventSystemObject);
        }

        private void CreateDebugTips()
        {
            if (!showDebugFingerTips)
            {
                return;
            }

            leftDebugTip = CreateDebugTip("LeftIndexPokeTip", new Color(0.2f, 0.75f, 1f, 0.95f));
            rightDebugTip = CreateDebugTip("RightIndexPokeTip", new Color(0.2f, 1f, 0.35f, 0.95f));
        }

        private GameObject CreateDebugTip(string name, Color color)
        {
            var tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tip.name = name;
            tip.transform.localScale = Vector3.one * 0.018f;
            DontDestroyOnLoad(tip);

            var collider = tip.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            var renderer = tip.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Sprites/Default"));
                material.color = color;
                renderer.sharedMaterial = material;
            }

            tip.SetActive(false);
            return tip;
        }

        private void UpdateDebugTip(GameObject tip, bool active, Vector3 position)
        {
            if (tip == null)
            {
                return;
            }

            tip.SetActive(showDebugFingerTips && active);
            if (active)
            {
                tip.transform.position = position;
            }
        }

        private static class ListPool<T>
        {
            private static readonly Stack<List<T>> Pool = new Stack<List<T>>();

            public static List<T> Get()
            {
                return Pool.Count > 0 ? Pool.Pop() : new List<T>();
            }

            public static void Release(List<T> list)
            {
                list.Clear();
                Pool.Push(list);
            }
        }
    }
}
