namespace Jaket.World;

using Jaket.UI;

/// <summary> Class responsible for additions to control and local display of emotions. </summary>
public class Movement : MonoSingleton<Movement>
{
    /// <summary> Creates a singleton of movement. </summary>
    public static void Load()
    {
        // initialize the singleton
        Utils.Object("Movement", Plugin.Instance.transform).AddComponent<Movement>();
    }
}
