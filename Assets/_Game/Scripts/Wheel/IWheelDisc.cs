using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.WheelOfFortune.Wheel
{
    /// <summary>A rotating wheel disc that can display one zone's slices.</summary>
    public interface IWheelDisc
    {
        RectTransform Rect { get; }
        Image Base { get; }
        WheelLayout Layout { get; }
    }
}
