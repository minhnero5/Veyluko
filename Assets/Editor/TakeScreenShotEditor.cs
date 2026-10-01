using UnityEngine;
using UnityEditor;
using System;
using System.IO;

public class TakeScreenShotEditor
{
#if UNITY_EDITOR
    [MenuItem("Tools/TakeScreenShot %g")]
    public static void TakeScreenShot()
    {
        var path = Application.dataPath + "/../Screenshot";
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

        var fileName = path + "/" + Screen.width + "x" + Screen.height + "_" + DateTime.Now.Ticks + ".png";
        ScreenCapture.CaptureScreenshot(fileName);
        Debug.Log("TakeScreenShot " + fileName + " success!");
    }
#endif
}