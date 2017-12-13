using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using UnityEngine;
using System;

public class Recorder {

    string filename;
    int H; int W; int skip;
    List<CloudFrame> container;

    [Serializable]
    public class CloudFrame
    {
        [SerializeField]
        public float t;
        [SerializeField]
        public Vector3[] p;
        [SerializeField]
        public Color[] c;

        public CloudFrame(Vector3[] somepc, Color[] someCC, float timestamp)
        {
            t = timestamp; 
            p = somepc;
            c = someCC;
        }
    }

    public static CloudFrame compileFrame(Vector3[] pc, Color[] cc, float timestamp)
    {
        List<Vector3> newPC = new List<Vector3>();
        List<Color> newCC = new List<Color>();

        for (int i = 0; i < pc.Length; i++)
        {
            newPC.Add(pc[i]);
            newCC.Add(cc[i]);
        }

        return new CloudFrame(newPC.ToArray(), newCC.ToArray(), timestamp);

    }

    public Recorder(string _filename, int _H, int _W, int _skip)
    {
        H = _H;
        W = _W;
        skip = _skip;
        filename = _filename;
        container = new List<CloudFrame>();
    }

    public void continueMovie(CloudFrame frame)
    {  //dt is time elapsed since the previous frame.... 
        container.Add(frame);
    }

    public void writeMovie()
    {
        filename = filename + ".pcm";
        if (File.Exists(filename))
        {
            File.Delete(filename);
        }

        using (StreamWriter sw = File.CreateText(filename))
        {
            sw.NewLine = "\n";
            sw.WriteLine(H.ToString() + " " + W.ToString() + " " + skip.ToString() + " ");

            foreach (CloudFrame frame in container)
            {
                //convert the frame into a json object and then write it to the file
                //sw.WriteLine(JsonUtility.ToJson(frame));
                String test = JsonUtility.ToJson(frame);
                sw.WriteLine(test);
            }
            sw.Flush();
            sw.Close();
        }
        container.Clear();  //clean up your mess
    }
}
