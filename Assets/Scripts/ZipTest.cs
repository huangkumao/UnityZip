using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using UnityEngine;

public class ZipTest : MonoBehaviour
{
    public static float sTime = 0;

	void Start ()
	{

    }

    public ZipHelper.ZipCallback ZipCB { get; set; }
    public ZipHelper.UnzipCallback UnZipCB { get; set; }

    public void OnClickZip()
    {
#if !UNITY_EDITOR
        return;
#endif

        ZipCB = new ZipResult();

        sTime = Time.realtimeSinceStartup;
        string[] paths = { Path.Combine(Application.dataPath, "UI/") };
        ZipHelper.Zip(paths, Path.Combine(Application.dataPath, "StreamingAssets/UI.zip"), null, ZipCB);
    }

    public void OnClickUnZip()
    {
        UnZipCB = new UnZipResult();

        sTime = Time.realtimeSinceStartup;
#if UNITY_EDITOR
        ZipHelper.UnzipFile(Path.Combine(Application.dataPath, "StreamingAssets/UI.zip"),
                            Path.Combine(Application.dataPath, "StreamingAssets/"), null, UnZipCB);
#else
        StartCoroutine(AndroidUnZip());
#endif
    }

    IEnumerator AndroidUnZip()
    {
        WWW www = new WWW(Application.streamingAssetsPath + "/UI.zip");
        yield return www;
        if (www.isDone)
        {
            ZipHelper.UnzipFile(www.bytes, Application.temporaryCachePath, null, UnZipCB);
        }
        www.Dispose();
    }
}

public class ZipResult : ZipHelper.ZipCallback
{
    public bool OnPreZip(ZipEntry _entry)
    {
        if (_entry.IsFile)
        {
            //Debug.Log(_entry.Name);
            if (GetFileSuffix(_entry.Name) == "meta")
                return false;
        }
        
        return true;
    }

    public void OnPostZip(ZipEntry _entry)
    {

    }

    public void OnFinished(bool _result)
    {
        Debug.Log("Finished : " + (Time.realtimeSinceStartup - ZipTest.sTime));
    }

    public string GetFileSuffix(string path)
    {
        int _index = path.LastIndexOf(".", StringComparison.Ordinal) + 1;
        return path.Substring(_index, path.Length - _index);
    }
}

public class UnZipResult : ZipHelper.UnzipCallback
{
    public bool OnPreUnzip(ZipEntry _entry)
    {
        return true;
    }

    public void OnPostUnzip(ZipEntry _entry)
    {
        //Debug.Log(_entry.Name);
    }

    public void OnFinished(bool _result)
    {
        Debug.Log("Finished : " + (Time.realtimeSinceStartup - ZipTest.sTime));
    }
}
