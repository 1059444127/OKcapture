using UnityEngine;
using System.Collections;
using Windows.Kinect;
using System;
using System.IO;
using System.Threading;

//public enum DepthViewMode
//{
 //   SeparateSourceReaders,
  //  MultiSourceReader,
//}


public class pcViewer : MonoBehaviour
{
    private float currentTime;

    public DepthViewMode ViewMode = DepthViewMode.SeparateSourceReaders;
    public int skip;  //this is just a resolution
    public float psize; //how big do you want the points to be 
    public int pCloudSize;  //how big is my pcloud? 

    //Managers
    public GameObject MultiSourceManager;
    private MultiSourceManager _MultiManager;

    private KinectSensor _Sensor;
    private CoordinateMapper _Mapper;

    private Vector3[] pointCloud;
    private Color[] colorCloud;
    private Boolean pCloudReady;
    GameObject structure;
    private Boolean structureReady;
    Shader myShader;

    //Recording CONTROLS
    Recorder myRecorder;
    public string fileName;
    private float startTime;
    public bool recordMovie;
    public bool started;
    public float recordLength;
    private float elapsedTime;
    Thread recordingThread;

    //locally store camera intrinsics -- these are set by the factory
    private float PrincipalPointX;
    private float PrincipalPointY;
    private float FocalLengthX;
    private float FocalLengthY;

    //room scale
    public float maxD;
    public float maxH;
    public float maxW;
    public float scale; //this should be = 0.001 to convert from mm to m.
    public bool project;
    public float rotate;

    //local variables - the frame
    private int W;
    private int H;

    //local variables - we are going to recalculate for use
    private int scaledH;
    private int scaledW;

    // Use this for initialization
    private void Start()
    {
        myShader = Shader.Find("Unlit/Texture_color");
        _Sensor = KinectSensor.GetDefault();
        if (_Sensor != null)
        {
            //get coordinate mapper
            _Mapper = _Sensor.CoordinateMapper;
            var frameDesc = _Sensor.DepthFrameSource.FrameDescription;

            //set the camera intrinsics
            PrincipalPointX = _Mapper.GetDepthCameraIntrinsics().PrincipalPointX;
            PrincipalPointY = _Mapper.GetDepthCameraIntrinsics().PrincipalPointY;
            FocalLengthX = _Mapper.GetDepthCameraIntrinsics().FocalLengthX;
            FocalLengthY = _Mapper.GetDepthCameraIntrinsics().FocalLengthY;

            //get frame Width and Height
            W = frameDesc.Width;
            H = frameDesc.Height;

            //calculate local scaled variables
            scaledW = (int)Math.Floor((double)W / skip + 1);
            scaledH = (int)Math.Floor((double)H / skip + 1);

            //for GUI controls... movie is not recording at start
            //started = false;

            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }
        }

        structure = new GameObject("Point Cloud");
        structure.transform.parent = gameObject.transform;
        structure.transform.localPosition = new Vector3(0, 0, 0);

        _MultiManager = MultiSourceManager.GetComponent<MultiSourceManager>();
        if (_MultiManager == null)
            return;

        elapsedTime = 0;
    }


    // Update is called once per frame
    void Update()
    {
        if (_Sensor == null)
            return;

        _MultiManager = MultiSourceManager.GetComponent<MultiSourceManager>();
        if (_MultiManager == null)
            return;

        //Generates a new point cloud every 30 fps
        if (_MultiManager.GetDepthData() != null)
            CreatePointCloud(_MultiManager.GetDepthData(), _MultiManager.GetColorTexture());

        //DebugScript(pointCloud);
        if (pCloudReady && !structureReady)
        {
            generateStructure();
            structure.transform.Rotate(new Vector3(-1 * rotate, 0, 180));
        }
        else if (pCloudReady && structureReady)
        {
            RefreshData();
            //takeDepthSnap();
        }

        if (recordMovie)
        {
            if (!started)
            {
                startTime = Time.time;
                started = true;
                if (fileName.Equals(""))
                    fileName = "myMovie";

                myRecorder = new Recorder(fileName, H, W, skip);
            }

            recordFrame();
        }

        if (structure != null)
        {
            //structure.transform.position = new Vector3(0, 100, -50);
            structure.transform.localScale = new Vector3(50, 50, 50);
        }

    }

    private void recordFrame()
    {
        float elapsedTime = Time.time - startTime;
        //dont record more than how long the user choose
        if (elapsedTime > recordLength)
        {
            recordMovie = false;
            started = false;
            elapsedTime = 0;
            //now we are done with recording so lets record to file
            myRecorder.writeMovie();
            return;
        }
        else
        {
            myRecorder.continueMovie(Recorder.compileFrame(pointCloud, colorCloud, elapsedTime));
            Debug.Log("I recorded a Frame :)");
        }
    }

    private void RefreshData()
    {
        //first find all the children of the structure (these are the gameObjs from the previous pointCloud
        GameObject[] pointObjArray = GameObject.FindGameObjectsWithTag("Player");

        for (int i = 0; i < pointCloud.Length; i++) //cycle through the new pointCloud and update the location of each pointObj
        {
            try
            {
                if (pointCloud[i] != Vector3.zero)
                {
                    pointObjArray[i].transform.localPosition = pointCloud[i];
                    Renderer renderer = pointObjArray[i].GetComponent<Renderer>();
                    renderer.enabled = true;
                    //if (renderer != null)
                    //renderer.material.color = colorCloud[i];
                    //renderer.material.SetColor("_Color", colorCloud[i]);
                }
                else
                    pointObjArray[i].GetComponent<Renderer>().enabled = false;
            } catch(Exception e)
            {
                Debug.Log("oops");
            }

        }

    }

    private void generateStructure()
    {
        //so the idea here is that this method creates a game object of what the camera sees
        if ((pointCloud != null))  //if the point cloud isn't null
        {
            foreach (Vector3 point in pointCloud)
            {
                GameObject jointObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                //Debug.Log(pointCloud[j].x.ToString());
                jointObj.transform.parent = structure.transform;
                jointObj.transform.localPosition = point;  //move the center point of the sphere to the location of the point
                jointObj.tag = "Player";
                jointObj.transform.localScale = new Vector3(psize, psize, psize);  //make it a really tiny shphere
                Renderer renderer = jointObj.GetComponent<Renderer>();
                renderer.enabled = true;
                //renderer.material.shader = myShader;
            }
            structureReady = true;
        }
    }

    private void CreatePointCloud(ushort[] depthData, Texture2D colorData)
    {
        int pointCloudSize = scaledH * scaledW;
        Vector3[] _pointCloud = new Vector3[pointCloudSize]; //(int)(frameDesc.Width/skip + frameDesc.Height/skip)];
        Color[] _colorCloud = new Color[pointCloudSize];  //same size as the point cloud

        //now we cycle through the depth frame
        int i = 0;  //this is the index that tracks both the _pointCloud and the _colorCloud.
        for (int x = 0; x < W; x += skip)
        {
            for (int y = 0; y < H; y += skip)
            {
                int offset = x + y * W;
                ushort depth = depthData[offset];
                DepthSpacePoint Pd = new DepthSpacePoint();
                Pd.X = x;
                Pd.Y = y;

                //now that i have my depth spacepoint lets map it to the color space
                ColorSpacePoint Cp = _Mapper.MapDepthPointToColorSpace(Pd, depth);

                if (depth != 0)
                    pCloudReady = true;

                Vector3 temp = depthToPointCloudPos(x, y, depth);
                _colorCloud[i] = colorData.GetPixel((int)Cp.X, (int)Cp.Y);  //we always save the color

                /*
                 * So the point of this is that we want to reduce draw calls by not rendering things
                 * that are being culled by the window the user picked.  But we dont wanna loose the data
                 */
                if ((Mathf.Abs(temp.x) <= maxW) && (Mathf.Abs(temp.y) <= maxH) && (Mathf.Abs(temp.z) <= maxD))
                {  //impliment depth narrowing -- only the points within the window
                    _pointCloud[i] = temp;
                }
                else if ((Mathf.Abs(temp.x) <= maxW) && (Mathf.Abs(temp.y) <= maxH) && (Mathf.Abs(temp.z) > maxD))
                {
                    if (project)
                    {
                        Vector3 test = projectToBack(temp);
                        _pointCloud[i] = test;
                    } 
                } else
                    _pointCloud[i] = Vector3.zero; ;
                i++;
            }
        }

        pointCloudSize = i;
        pointCloud = _pointCloud;//pointCloud = tools.ChopAt(_pointCloud,i);
        colorCloud = _colorCloud;//colorCloud = tools.ChopAt(_colorCloud,i);
        pCloudSize = pointCloudSize;
    }

    private Vector3 projectToBack(Vector3 vectorA)
    {
        Vector3 tmp = Vector3.ProjectOnPlane(vectorA, Vector3.forward);
        tmp.z = maxD;
        return tmp;
    }

    void OnGUI()
    {
        GUI.BeginGroup(new Rect(0, 0, Screen.width, Screen.height));
        GUI.TextField(new Rect(Screen.width - 250, 10, 250, 20), "FrameRate: " + ((int)(1/Time.deltaTime)).ToString());
        GUI.TextField(new Rect(Screen.width - 250, 10, 250, 40), "\n Elapsed Time: " + ((int)(elapsedTime)).ToString());
        GUI.EndGroup();
    }

    private Vector3 depthToPointCloudPos(int x, int y, float depth)
    {
        //map from depth to real space using camera parameters - in m
        Vector3 point = new Vector3();
        point.x = (float)((x - PrincipalPointX) / FocalLengthX * depth) * scale;
        point.y = (float)((y - PrincipalPointY) / FocalLengthY * depth) * scale;
        point.z = (float)depth * scale;

        return point;
    }

    void OnApplicationQuit()
    {
        if (_Mapper != null)
        {
            _Mapper = null;
        }

        if (_Sensor != null)
        {
            if (_Sensor.IsOpen)
            {
                _Sensor.Close();
            }

            _Sensor = null;
        }
    }

}

