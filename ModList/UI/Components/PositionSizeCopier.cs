using UnityEngine;

namespace IPA.ModList.BeatSaber.UI.Components
{
    [RequireComponent(typeof(RectTransform))]
    public class PositionSizeCopier : MonoBehaviour
    {
        public RectTransform RectTransform => GetComponent<RectTransform>();

        public RectTransform CopyFrom { get; set; }

        public void Update()
        {
            var rt = RectTransform;
            rt.anchoredPosition = CopyFrom.anchoredPosition;
            rt.anchorMin = CopyFrom.anchorMin;
            rt.anchorMax = CopyFrom.anchorMax;
            rt.sizeDelta = CopyFrom.sizeDelta;
            rt.pivot = CopyFrom.pivot;
        }
    }
}