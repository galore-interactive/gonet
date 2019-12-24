using UnityEngine;

public class ProjectileSceneUI : MonoBehaviour
{
    public TMPro.TextMeshProUGUI time;

    private void Update()
    {
        if (time)
        {
            time.text = GONet.GONetMain.Time.ElapsedSeconds.ToString();
        }
    }
}
