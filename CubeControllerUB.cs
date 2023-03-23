
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class CubeControllerUB : UdonSharpBehaviour
{
    [HideInInspector] public byte OpCode;
    [HideInInspector] public byte Opr1;
    [HideInInspector] public byte Opr2;
    [HideInInspector] public byte Opr3;
    [SerializeField] Transform[] cubeTransArr;
    [SerializeField] Rigidbody[] cubeRBArr;
    [SerializeField] MeshRenderer[] cubeMeshRendArr;
    [SerializeField] Material cubeMat;
    [SerializeField] Color[] cubeEmissionColorArr;
    
    int currentCube;

    int dbgInsCounter;
    byte[][] dbgInsArr;
    [SerializeField] string dbgIns;
    
    void Start()
    {
        OpCode = 0; Opr1 = 0; Opr2 = 0; Opr3 = 0;
        SelectCube();
        // Debug.Log(Mathf.Clamp(4,0,4));
        dbgInsCounter = 0;
        dbgInsArr = new byte[4][]{
            new byte[4] {0x0,0x0,0x0,0x0},
            new byte[4] {0x0,0x1,0x0,0x0},
            new byte[4] {0x0,0x2,0x0,0x0},
            new byte[4] {0x0,0x3,0x0,0x0}
        };

    }

    void Update()
    {
        #if UNITY_EDITOR
        if(Input.GetKeyDown(KeyCode.A))
        {
            // dbgInsCounter = dbgInsCounter < dbgInsArr.Length-1 ? dbgInsCounter++ : 0;
            // OpCode = dbgInsArr[dbgInsCounter][0]; 
            // Opr1 = dbgInsArr[dbgInsCounter][1]; 
            // Opr2 = dbgInsArr[dbgInsCounter][2]; 
            // Opr3 = dbgInsArr[dbgInsCounter][3]; 

            // string[] s = dbgIns.Split(',');
            // OpCode = byte.Parse(s[0]);
            // Opr1 = byte.Parse(s[1]);
            // Opr2 = byte.Parse(s[2]);
            // Opr3 = byte.Parse(s[3]);
            // InvokeDevice();

            // OpCode = 1; Opr1 = 5; Opr2 = 1; InvokeDevice();

            ///////////////////////////////////////////
            // Debug, delete later
            // cubeTransArr[0].localScale += (cubeTransArr[0].right*1);
            cubeTransArr[0].localScale += new Vector3(1,0,0);
            ///////////////////////////////////////////
        }
            // Vector3 cube01Pos = cubeTransArr[0].position + cubeTransArr[0].forward;
            // cubeTransArr[1].position = cube01Pos;
        #endif
    }

    public void InvokeDevice()
    {
        // Debug.Log($"Cube Controller | OpCoded: {OpCode}, Operand1: {Opr1}, Operand2: {Opr2}, Operand3: {Opr3}");

        if(OpCode == 0x0) // Select Cube
        {
            currentCube = Mathf.Clamp(Opr1, 0, 3);
            SelectCube();
            // Debug.Log($"Current Cube: {currentCube}");
        }
        else if(OpCode == 0x1) // Translate
        {
            Vector3 units = SetAxis(cubeTransArr[currentCube], Opr1) * (float)Opr2;
            cubeTransArr[currentCube].Translate(units, Space.Self);
        }
        else if(OpCode == 0x2) // Rotate
        {
            Vector3 units = SetAxis(cubeTransArr[currentCube], Opr1) * (float)Opr2;
            cubeTransArr[currentCube].Rotate(units, Space.Self);
        }
        // TO DO: Need to work on scaling logic. Buggy.
        else if(OpCode == 0x3) // Scale
        {
            Vector3 units = Vector3.zero;
            switch(Opr1)
            {
                // case 0: units = cubeTransArr[currentCube].right * Opr2; break;
                // case 1: units = cubeTransArr[currentCube].up * Opr2; break;
                // case 2: units = cubeTransArr[currentCube].forward * Opr2; break;
                // case 3: units = cubeTransArr[currentCube].lossyScale * Opr2; break;

                case 0: units = new Vector3(1,0,0) * Opr2; break;
                case 1: units = new Vector3(0,1,0) * Opr2; break;
                case 2: units = new Vector3(0,0,1) * Opr2; break;
                case 3: units = new Vector3(-1,0,0) * Opr2; break;
                case 4: units = new Vector3(0,-1,0) * Opr2; break;
                case 5: units = new Vector3(0,0,-1) * Opr2; break;
                case 6: units = new Vector3(1,1,1) * Opr2; break;
            }
            
            cubeTransArr[currentCube].localScale += units;
                
        }
        else if(OpCode == 0x4) // AddForce
        {
            Vector3 force = SetAxis(cubeTransArr[currentCube], Opr1) * (float)Opr2;
            cubeRBArr[currentCube].AddRelativeForce(force);
            // Debug.Log($"Add Force | Force: {force} | Current Cube: {currentCube}");
            // cubeRBArr[0].AddRelativeForce(new Vector3(0f,(float)Opr2,0f));
            
            
        }
        else if(OpCode == 0x5) // AddTorque
        {
            Vector3 force = SetAxis(cubeTransArr[currentCube], Opr1) * (float)Opr2;
            cubeRBArr[currentCube].AddRelativeTorque(force);
        }
        else if(OpCode == 0x6) // Set IsKinematic
            cubeRBArr[currentCube].isKinematic = Opr1 == 1;
        else if(OpCode == 0x7) // Set UseGravity
            cubeRBArr[currentCube].useGravity = Opr1 == 1;

    }

    Vector3 SetAxis(Transform trans, int opr)
    {
        Vector3 axis = Vector3.zero;
        switch(opr)
        {
            case 0: axis = trans.right; break;
            case 1: axis = trans.up; break;
            case 2: axis = trans.forward; break;
            case 3: axis = -trans.right; break;
            case 4: axis = -trans.up; break;
            case 5: axis = -trans.forward; break;
            default: break;
        }
        return axis;
    }

    public void InitDevice()
    {
        currentCube = 0;
        SelectCube();
    }

    void SelectCube()
    {
        
        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

        // Set color 
        for(int i = 0; i < cubeMeshRendArr.Length; i++)
        {
            Color col = cubeEmissionColorArr[i];
            float H = 0; float S = 0; float V = 0;
            Color.RGBToHSV(col, out H, out S, out V);
            // Debug.Log($"{H},{S},{V}");
            V = i == currentCube ? V : 0.25f;
            col = Color.HSVToRGB(H,S,V);
            // Debug.Log(col);
            cubeMeshRendArr[i].GetPropertyBlock(propBlock);
            propBlock.SetColor("_EmissionColor", col);
            cubeMeshRendArr[i].SetPropertyBlock(propBlock);
        }

        // Set V or HSV


    } 
}
