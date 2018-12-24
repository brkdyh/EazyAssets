using UnityEngine;

public class VersionObj : ScriptableObject
{
    [SerializeField]
    public bool Open_Version;

    [SerializeField]
    public string Main_Version_Number;

    [SerializeField]
    public string Asset_Version_Number;

    public VersionObj()
    {
        Open_Version = false;
        Main_Version_Number = "0.0.0";
        Asset_Version_Number = "0.0.0";
    }
}