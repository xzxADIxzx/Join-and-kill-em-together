namespace Jaket.UI.Lib;

using UnityEngine;

/// <summary> Either horizontal or vertical bar that gets filled with interface elements. </summary>
public class Bar : MonoBehaviour
{
    /// <summary> Whether the bar is vertical or horizontal. </summary>
    private bool voh;
    /// <summary> Margin from the borders and padding between the elements. </summary>
    private float margin, padding;
    /// <summary> Amount of pixels claimed by the elements. </summary>
    private float filled;
    /// <summary> Action to be done in the update loop. </summary>
    private Runnable update;
    /// <summary> Rectangle that contains this element. </summary>
    private RectTransform rect;

    private void Start() => TryGetComponent(out rect);

    /// <summary> Sets up the basic options of the bar. </summary>
    public void Setup(bool voh, float margin = 8f, float padding = 8f)
    {
        this.voh = voh;
        this.margin = margin;
        this.padding = padding;
    }

    private void Update() => update?.Invoke();

    /// <summary> Schedules the given runnable to be done in the update loop. </summary>
    public void Update(Runnable update)
    {
        var previous = this.update;
        this.update = () =>
        {
            previous?.Invoke();
            update();
        };
    }

    /// <summary> Resolves the given size of an element and returns a rect to build the element in. </summary>
    public RectTransform Resolve(string name, float size)
    {
        float fill = (voh ? rect.sizeDelta.x : rect.sizeDelta.y) - margin * 2f;
        float incr = (filled != 0f ? padding : margin) + size / 2f;

        var result = Builder.Rect(name, rect, new(
            voh ? 0f : filled += incr,
            voh ? filled -= incr : 0f,
            voh ? fill : size,
            voh ? size : fill,
            voh ? new(.5f, 1f) : new(0f, .5f)
        ));

        filled += size / (voh ? -2f : 2f);
        return result;
    }
}
