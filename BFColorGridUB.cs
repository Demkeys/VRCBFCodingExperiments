/*
Notes on working mechanism (incase I forget in the future):
How this is supposed to work is, ColorGrid device gets initialized when the runtime is initialized. During init process the instruction array gets reset so all instructions are (-1,-1,-1,-1). Then throughout the duration of the runtime running, more instructions get added to the instruction array whenever needed. Ins Count keeps track of number of instructions. So basically the array keep gathering more and more instructions each time, in addition to the instructions it already has, and each time it passes all the instructions to the ColorGrid shader. This is how the ColorGrid maintains state.
*/

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

public class BFColorGridUB : UdonSharpBehaviour
{
    [SerializeField] Material colorGridMat;
    [SerializeField] Slider gridSizeSliderUI;
    [HideInInspector] public byte OpCode;
    [HideInInspector] public byte Opr1;
    [HideInInspector] public byte Opr2;
    [HideInInspector] public byte Opr3;
    Vector4[] ins_Arr = new Vector4[100];

    // Total number of instructions to be executed. This number only ever increases, so that the instruction array can be populated until it's full.
    int ins_Count = 0; 
    
    // Vector4 with -1 value. This is mainly used to set the instruction array's cells to (-1,-1,-1,-1) so that they can then be populated with instructions.
    Vector4 negOneVec4 = new Vector4(-1,-1,-1,-1); 

    // Puzzle stuff
    
    // List of instructions needed to solve the puzzle. Each instruction needs
    // to be executed atleast once for it to count.
    byte[][] puzzleSolveInsArr = new byte[6][];
    bool[] puzzleSolveFlagsArr = new bool[6];
    [SerializeField] PuzzleContUB puzzleContUB;

    int someInt = 0;

    void Start()
    {
        // Init puzzle stuff
        puzzleSolveInsArr[0] = new byte[4]{0x0,0xe4,0x03,0x03};
        puzzleSolveInsArr[1] = new byte[4]{0x0,0xff,0x8c,0x0};
        puzzleSolveInsArr[2] = new byte[4]{0x0,0xff,0xed,0x0};
        puzzleSolveInsArr[3] = new byte[4]{0x0,0x0,0x80,0x26};
        puzzleSolveInsArr[4] = new byte[4]{0x0,0x24,0x40,0x8e};
        puzzleSolveInsArr[5] = new byte[4]{0x0,0x73,0x29,0x82};

        for(int i = 0; i < ins_Arr.Length; i++) ins_Arr[i] = negOneVec4;
        
        // Give some instructions for first start, so Color Grid isn't empty.
        ins_Arr[0] = new Vector4(4,Random.Range(0,256),0,0);
        // ins_Arr[1] = new Vector4(5,127,0,127);
        ins_Arr[1] = new Vector4(2,6,0,0);
        ins_Arr[2] = new Vector4(2,12,0,0);
        // InvokeDevice();
        
        ins_Count = 3;

        colorGridMat.SetInt("ins_count", ins_Count);
        colorGridMat.SetVectorArray("ins_arr", ins_Arr);

        SetGridSize();
    }

    void Update()
    {
        #if UNITY_EDITOR
        if(Input.GetKeyDown(KeyCode.S))
        {
            OpCode = 0; Opr1 = 0; Opr2 = 0; Opr3 = 0; InvokeDevice();
            OpCode = 5; InvokeDevice();
            OpCode = 0; Opr1 = 0xe4; Opr2 = 0x03; Opr3 = 0x03; InvokeDevice();
        }
        if(Input.GetKeyDown(KeyCode.Space))
        {
            // OpCode = 0; Opr1 = 0; Opr2 = 0; Opr3 = 0; InvokeDevice();

            // OpCode = 5; InvokeDevice();

            // Set red
            OpCode = 0; Opr1 = 0xe4; Opr2 = 0x03; Opr3 = 0x03; InvokeDevice();
            // Draw 
            OpCode = 2; Opr1 = 10; Opr2 = 0; InvokeDevice();

            // Set orange
            OpCode = 0; Opr1 = 0xff; Opr2 = 0x8c; Opr3 = 0x0; InvokeDevice();
            // Draw 
            OpCode = 2; Opr1 = 11; Opr2 = 0; InvokeDevice();

            // Set yellow
            OpCode = 0; Opr1 = 0xff; Opr2 = 0xed; Opr3 = 0x0; InvokeDevice();
            // Draw 
            OpCode = 2; Opr1 = 12; Opr2 = 0; InvokeDevice();
            
            // Set green
            OpCode = 0; Opr1 = 0x0; Opr2 = 0x80; Opr3 = 0x26; InvokeDevice();
            // Draw 
            OpCode = 2; Opr1 = 12; Opr2 = 0; InvokeDevice();
            
            // Set blue
            OpCode = 0; Opr1 = 0x24; Opr2 = 0x40; Opr3 = 0x8e; InvokeDevice();
            // Draw 
            OpCode = 2; Opr1 = 12; Opr2 = 0; InvokeDevice();
            
            // Set purple
            OpCode = 0; Opr1 = 0x73; Opr2 = 0x29; Opr3 = 0x82; InvokeDevice();
            // Draw 
            OpCode = 2; Opr1 = 12; Opr2 = 0; InvokeDevice();
            
            
            // OpCode = 0; Opr1 = 255; Opr2 = 0; Opr3 = 0; InvokeDevice();

            // OpCode = 1; Opr2 = 10; Opr3 = 0;
            // for(int i = 0; i < 10; i++)
            // {
            //     Opr1 = (byte)i;
            //     InvokeDevice();
            // }
        }
        #endif
    }

    public void InvokeDevice()
    {
        // Debug.Log($"Color Grid | OpCoded: {OpCode}, Operand1: {Opr1}, Operand2: {Opr2}, Operand3: {Opr3}");
        
        for(int i = 0; i < ins_Arr.Length; i++)
        {
            if(ins_Arr[i] == negOneVec4)
            {
                // Special case: Random number needed, so generate in script and pass to shader.
                if(OpCode == 4) // Op: randomize_grid_op
                    Opr1 = (byte)Random.Range(0,256);
                ins_Arr[i] = new Vector4(OpCode, Opr1, Opr2, Opr3);
                ins_Count++;
                break;
            }
        }
        CheckPuzzle(new byte[]{OpCode,Opr1,Opr2,Opr3});
        colorGridMat.SetVectorArray("ins_arr", ins_Arr);
        colorGridMat.SetInt("ins_count", ins_Count);
    }

    public void InitDevice()
    {
        // Reset ins_Arr and ins_Count, then pass those values to the shader.
        for(int i = 0; i < ins_Arr.Length; i++) ins_Arr[i] = negOneVec4;
        ins_Count = 0;
        colorGridMat.SetVectorArray("ins_arr", ins_Arr);
        colorGridMat.SetInt("ins_count", ins_Count);
        SetGridSize();
    }

    public void OnGridSizeSliderValueChanged()
    {
        // Debug.Log("T");
        SetGridSize();
    }

    void SetGridSize()
    {
        float sizeN = 0;
        switch(gridSizeSliderUI.value)
        {
            case 1: sizeN = 4; break;
            case 2: sizeN = 8; break;
            case 3: sizeN = 16; break;
            case 4: sizeN = 32; break;
            case 5: sizeN = 64; break;
            case 6: sizeN = 128; break;
            default: break;
        }
        // Debug.Log("Test");
        // colorGridMat.SetVector("_GridSize", new Vector4(sizeN, sizeN, 0, 0)); 
        colorGridMat.SetVector("_MainTex_ST", new Vector4(sizeN, sizeN, 0, 0));
    }

    void CheckPuzzle(byte[] ins)
    {
        // Iterate over puzzleSolveInsArr and check if ins array matches any of
        // puzzleSolveInsArr's items. If it does, set the corresponding flag to
        // true in puzzleSolveFlagsArr.
        for(int i = 0; i < puzzleSolveInsArr.Length; i++)
        {
            bool matchFound = true;
            for(int j = 0; j < ins.Length; j++)
            {
                if(ins[j] != puzzleSolveInsArr[i][j]) matchFound = false;
            }
            if(matchFound)
            {
                puzzleSolveFlagsArr[i] = true; break;
            }
        }

        // Iterate over puzzleSolveFlagsArr. If all flags are true, puzzle solved.
        bool allFlagsTrue = true;
        for(int i = 0; i < puzzleSolveFlagsArr.Length; i++)
        {
            allFlagsTrue = puzzleSolveFlagsArr[i] == true;
        }

        if(allFlagsTrue)
        {
            puzzleContUB.ColorGridPhase1Solved();
        }
        
    }
}
