using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Snapshot : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        referenceCam = Camera.main;
        sh = Shader.Find("Unlit/testShader");
        initTest();
        myCam = Instantiate<Camera>(referenceCam);
        myCam.transform.parent = transform;
        referenceCam.tag = "Untagged";
        var audioListener =referenceCam.GetComponent<AudioListener>();
        if (audioListener) {
            GameObject.Destroy(audioListener);
        }
    }

    public RenderTexture rt;
    public Shader sh;
    Camera referenceCam;
    private Camera myCam;

    void init() {

    }


    // Update is called once per frame
    void Update()
    {
        test();
    }

    public Texture2D testOutput;
    void initTest(){
        
        rt = new RenderTexture(referenceCam.pixelWidth, referenceCam.pixelHeight, 16, RenderTextureFormat.ARGBFloat);
        testOutput = new Texture2D(referenceCam.pixelWidth, referenceCam.pixelHeight, TextureFormat.RGBAFloat, false);
    
    }

    void test() {
        takeSnapshot(ref testOutput);
    }

    void takeSnapshot(ref Texture2D outp) {
        myCam.CopyFrom(referenceCam);

        referenceCam.enabled = false;
        myCam.enabled = true;

        myCam.clearFlags = CameraClearFlags.Color;
        myCam.backgroundColor =Color.black;

        myCam.targetTexture = rt;
        RenderTexture.active = rt;
        myCam.RenderWithShader(sh,"");
        
        
        
        Rect regionToReadFrom = new Rect(0, 0, outp.width, outp.height);
        outp.ReadPixels(regionToReadFrom,0,0,false);
        outp.Apply();
        referenceCam.enabled = true;
        myCam.enabled = false;
    }

}
