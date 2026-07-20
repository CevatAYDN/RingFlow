using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RingFlow.Gameplay.UI
{
    /// <summary>
    /// Manages the runtime Canvas, CanvasScaler, and related UI infrastructure.
    /// Extracted from UIRoot to reduce its responsibilities.
    /// </summary>
    public class CanvasManager
    {
        private Canvas _canvas;
        private CanvasScaler _scaler;
        private CanvasGroup _canvasGroup;

        /// <summary>
        /// Reference resolution used when BigButtons mode is off.
        /// </summary>
        public static readonly Vector2 DefaultResolution = new Vector2(1080, 1920);

        /// <summary>
        /// Reference resolution used when BigButtons accessibility mode is on.
        /// </summary>
        public static readonly Vector2 BigButtonsResolution = new Vector2(810, 1440);

        public Canvas Canvas => _canvas;
        public CanvasScaler Scaler => _scaler;
        public CanvasGroup CanvasGroup => _canvasGroup;
        public Transform Transform => _canvas != null ? _canvas.transform : null;

        /// <summary>
        /// Ensures a Canvas with scaler exists under the given parent.
        /// If a child named "UICanvas" already exists, it is reused.
        /// </summary>
        public void EnsureCanvas(Transform parent)
        {
            if (_canvas != null) return;

            var canvasGo = parent.Find("UICanvas")?.gameObject;
            if (canvasGo == null)
            {
                canvasGo = new GameObject("UICanvas");
                canvasGo.transform.SetParent(parent, false);
            }

            _canvas = canvasGo.GetComponent<Canvas>();
            if (_canvas == null) _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            _canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Tangent;

            _scaler = canvasGo.GetComponent<CanvasScaler>();
            if (_scaler == null) _scaler = canvasGo.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = DefaultResolution;
            _scaler.matchWidthOrHeight = 0.5f;
            _scaler.referencePixelsPerUnit = 100;

            if (canvasGo.GetComponent<GraphicRaycaster>() == null)
                canvasGo.AddComponent<GraphicRaycaster>();

            _canvasGroup = canvasGo.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = canvasGo.AddComponent<CanvasGroup>();
        }

        /// <summary>
        /// Switches between default and BigButtons reference resolutions.
        /// Called when the accessibility setting changes.
        /// </summary>
        public void SetBigButtons(bool big)
        {
            if (_scaler == null) return;
            _scaler.referenceResolution = big ? BigButtonsResolution : DefaultResolution;
        }

        /// <summary>
        /// Updates all cameras in the scene to have a PhysicsRaycaster component.
        /// </summary>
        public static void EnsureCameraRaycasters()
        {
            foreach (var cam in Camera.allCameras)
            {
                if (cam != null && cam.GetComponent<PhysicsRaycaster>() == null)
                    cam.gameObject.AddComponent<PhysicsRaycaster>();
            }
        }

        /// <summary>
        /// Adds a CanvasGroup to a GameObject if it doesn't already have one.
        /// </summary>
        public static CanvasGroup EnsureCanvasGroup(GameObject go)
        {
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            return cg;
        }

        /// <summary>
        /// Destroys the Canvas and all its children.
        /// </summary>
        public void Destroy()
        {
            if (_canvas != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(_canvas.gameObject);
                else
                    Object.DestroyImmediate(_canvas.gameObject);
                _canvas = null;
            }
            _scaler = null;
            _canvasGroup = null;
        }
    }
}
