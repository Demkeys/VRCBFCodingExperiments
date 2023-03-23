using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using System.Text;
using VirtualFileSystem;

public class BFRuntime01UB : UdonSharpBehaviour
{
    // Runtime
    const int MEMSIZE = 30000;
    const int INSBUFFERSIZE = 10000;
    byte[] memory;
    byte[] insBuffer;
    int insPointer = 0;
    int dataPointer = 0;
    bool isExecuting = false;
    bool isAcceptingInput = false;

    // Time variables
    public int ipc = 1; // Instructions per cycle
    float cf = 1; // Cycles per second
    float cf_TimeStamp = 0f;
    
    // Stall instruction variables
    float stall_TimeStamp = 0f;
    bool isStalling = false; 

    // UI
    [SerializeField] InputField insInput;
    [SerializeField] InputField userInput;
    [SerializeField] Button userInputBtn;
    [SerializeField] Text userOutput;
    [SerializeField] Image isExecutingImg;
    [SerializeField] Image isAcceptingInputImg;
    [SerializeField] Color TrueColor;
    [SerializeField] Color FalseColor;
    [SerializeField] Slider speedSlider;
    [SerializeField] Text insPointerText;
    [SerializeField] Text dataPointerText;
    [SerializeField] Text memDumpText;

    [Header("Device Controller")]
    [SerializeField] BFDeviceControllerUB deviceContUB;

    [Header("Debugging")]
    [SerializeField][Multiline(5)] string dbgCodeStr = "";

    // This flag is necessary to allow use to allow user to write BF code
    // outside of debugMode without using debug BF code.
    [SerializeField] bool debugMode = false;
    [Header("VFS")]
    [SerializeField] FileManager vfsFileManager;
    [Header("Puzzle")]
    [SerializeField] PuzzleContUB puzzleController;


    // Start is called before the first frame update
    void Start()
    {
        userInput.interactable = false;
        userInputBtn.interactable = false;
        UpdateImgUI();
        // CreateMemDump();
        // DebugStuff();
    }

    // Update is called once per frame
    void Update()
    {
        // For debugging
        #if UNITY_EDITOR
        if(debugMode)
        {
            // if(Input.GetKeyDown(KeyCode.Space))
                // DbgExecuteCode();
            if(Input.GetKeyDown(KeyCode.RightControl))
                userInput.text = "255";
            if(Input.GetKeyDown(KeyCode.RightShift) && userInputBtn.interactable)
                AcceptInput();
            if(Input.GetKeyDown(KeyCode.P))
                puzzleController.ActivatePuzzle();

        }
        #endif
        if(Input.GetKeyDown(KeyCode.F10))
            StartRuntime();

        // This is a bad way to fix the issue, but if it works, leave it
        // in for now.
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            
        }

        if(!isExecuting) return;

         
        

        if(!isAcceptingInput)
        {
            if(Time.time > cf_TimeStamp)
            {
                for(int i = 0; i < ipc; i++)
                {
                    // If accepting input from user, break out of loop so that 
                    // instructions don't continue executing.
                    if(isAcceptingInput) break;

                    // If stall instruction was executed and runtime is 
                    // currently stalling, don't execute any of the other code.
                    // Instead, stall execution until stall period is over.
                    // Keep this logic within for loop so that if a stall ins
                    // was executed in the previous cycle and there are more
                    // cycles left in this second, execution doesn't continue.                    
                    if(isStalling)
                    {
                        if(Time.time < stall_TimeStamp) return;
                        else isStalling = false;
                    }

                    insPointerText.text = $"0x{Convert.ToString(insPointer, 16).PadLeft(4, '0')}";

                    // Iterate over instruction buffer.
                    ExecuteInstruction();

                    // If interating over last instruction, stop execution.
                    if(insPointer == INSBUFFERSIZE-1)
                    {
                        isExecuting = false;
                        speedSlider.interactable = true;
                        userInputBtn.interactable = false;
                        UpdateImgUI();
                    } 

                }
                cf_TimeStamp = Time.time + cf;
            }
        }
    }

    void ExecuteInstruction()
    {
        if(!isExecuting) return;

        switch(insBuffer[insPointer])
        {
            case 0x3E: // '>' Increment Data pointer
                // dataPointer = dataPointer < 255 ? dataPointer+1 : 255;
                // If dataPointer is less than max index of memory array, increment,
                // else, set to max index of memory array. This will make sure that 
                // dataPointer never goes out of bounds of memory array.
                dataPointer = dataPointer < memory.Length-1 ? dataPointer+1 : memory.Length-1;
                dataPointerText.text = $"0x{Convert.ToString(dataPointer, 16).PadLeft(4, '0')}";
                CreateMemDump();
                break;
            case 0x3C: // '<' Decrement Data pointer
                dataPointer = dataPointer > 0 ? dataPointer-1 : 0;
                dataPointerText.text = $"0x{Convert.ToString(dataPointer, 16).PadLeft(4, '0')}";
                CreateMemDump();
                break;
            case 0x2B: // '+' Incrememnt memory[dataPointer]
                memory[dataPointer] = 
                    memory[dataPointer] < 255 ? (byte)(memory[dataPointer]+1) : (byte)255;
                // Debug.Log(memory[dataPointer]);
                CreateMemDump();
                break;
            case 0x2D: // '-' Decrememnt memory[dataPointer]
                memory[dataPointer] = 
                    memory[dataPointer] > 0 ? (byte)(memory[dataPointer]-1) : (byte)0;
                CreateMemDump();
                break;
            case 0x2E: // '.' Output
                OutputOp();
                CreateMemDump();
                // userOutput.text += $"Pointer: {dataPointer}, Value: {Convert.ToString(memory[dataPointer], 10).PadLeft(3, '0')}\n";
                // Debug.Log($"{Convert.ToString(memory[dataPointer], 16).PadLeft(2, '0')} ");
                break;
            case 0x2C: // ',' Input
                isAcceptingInput = true;
                userInput.interactable = true;
                userInputBtn.interactable = true;
                UpdateImgUI();
                CreateMemDump();
                break;
            case 0x5B: // '[' - Jump forward to next ']' if memory[dataPointer] is 0
                // If dataPointer points to 0 value
                if(memory[dataPointer] == 0x0)
                {
                    // Start reading ins right after '['
                    for(int i = insPointer+1; i < INSBUFFERSIZE; i++)
                    {
                        // If ']' is found
                        if(insBuffer[i] == 0x5D)
                        { 
                            // Set insPointer to ']' location. After switch exits,
                            // insPointer gets incremented.
                            insPointer = i; break; 
                        }
                        // If ']' is not found, keep iterating to the end.
                    }
                }
                break;
            case 0x5D: // ']' - Jump backward to previous '[' if memory[dataPointer] is not 0
                // If dataPointer points to non-zero value
                if(memory[dataPointer] != 0)
                {
                    // Start reading backwards for a '[' instruction.
                    for(int i = insPointer-1; i > -1; i--)
                    {
                        // If '[' is found
                        if(insBuffer[i] == 0x5B)
                        {
                            insPointer = i; break;
                        }
                        // If '[' is not found, keep iterating to the start.
                    }
                }
                break;
            /* '=' - Store constant value at current memory cell. Uses value 
            at current memory as index to choose a value from valArr, then 
            stores that value at current memory cell.
            */
            case 0x3D: 
                byte[] valArr = {8,16,32,64,128,255}; // Constants

                // If provided index is with valArr range, choose value,
                // otherwise store 0 instead.
                byte memVal = 
                    memory[dataPointer] >= 0 && memory[dataPointer] < valArr.Length ? 
                    valArr[(int)memory[dataPointer]] : (byte)0;
 
                memory[dataPointer] = memVal;
                CreateMemDump();
                break;
            case 0x0: // Invalid instruction, halt execution.
                isExecuting = false;
                speedSlider.interactable = true;
                userInputBtn.interactable = false;
                UpdateImgUI();
                break;
            default: break;
        }
        insPointer++;

    }

    void OutputOp()
    {
        // Output data to Debug Console
        userOutput.text += $"Pointer: {dataPointer}, Value: {Convert.ToString(memory[dataPointer], 10).PadLeft(3, '0')}\n";

        if(memory[dataPointer] == 0xff)
        {
            byte opcode = dataPointer > 3 ? memory[dataPointer-4] : (byte)0;
            byte opr1 = dataPointer > 3 ? memory[dataPointer-3] : (byte)0;
            byte opr2 = dataPointer > 3 ? memory[dataPointer-2] : (byte)0;
            byte opr3 = dataPointer > 3 ? memory[dataPointer-1] : (byte)0;
            deviceContUB.OpCode = opcode;
            deviceContUB.Opr1 = opr1;
            deviceContUB.Opr2 = opr2;
            deviceContUB.Opr3 = opr3;
            deviceContUB.InvokeDeviceController();

            // Debug.Log($"Runtime | OpCode:{opcode}, Opr1:{opr1}, Opr2:{opr2}, Opr3:{opr3}");

            // Device 0 ops are runtime related ops. So if Device 0 is 
            // active in the Device Controller, the data (opcode and oprs) 
            // is meant for runtime related ops.
            // if(deviceContUB.Device == 0)
            //     ExecuteDevice0Ops(opcode, opr1, opr2, opr3);    

            // Call ExecuteDevice0Ops. Device 0 Ops are mainly meant to execute
            // when Device 0 is selected, but because of OpCode 1 (Select Device)
            // we need to break that rule and call ExecuteDevice0Ops regardless 
            // of which Device is selected.
            ExecuteDevice0Ops(opcode, opr1, opr2, opr3);

            // Puzzle Code
            byte[] hexKey = puzzleController.hexKeyArr;
            byte[] puzzleMsg = new byte[]{
                0x70,0x75,0x7a,0x7a,0x6c,0x65,0x20,0x61,0x63,0x74,0x69,0x76,
                0x61,0x74,0x65,0x64
            };
            // If hex key found in Opr1, 2 and 3, activate puzzle.
            if(opcode == hexKey[0] && opr1 == hexKey[1] && 
                opr2 == hexKey[2] && opr3 == hexKey[3])
            {
                // Store "puzzle activated" ascii string in memory.
                for(int i = 0; i < puzzleMsg.Length; i++)
                {
                    // Inc data pointer and write to memory
                    dataPointer = dataPointer < memory.Length-1 ? dataPointer+1 : memory.Length-1;
                    memory[dataPointer] = puzzleMsg[i];
                }
                // Update UI
                dataPointerText.text = $"0x{Convert.ToString(dataPointer, 16).PadLeft(4, '0')}";
                CreateMemDump();

                // Activate puzzle
                puzzleController.ActivatePuzzle();

            }
        }
        
    }

    // Device 0 ops are runtime related ops. These are various ops that BF 
    // doesn't offer out of the box. Most of the Device 0 Ops should only 
    // be executed if Device 0 is selected. OpCode 1 is an exception to this
    // rule. OpCode 1 lets you set the current device, so it needs to be 
    // executed regardless of which device is active. 
    void ExecuteDevice0Ops(byte OpCode, byte Opr1, byte Opr2, byte Opr3)
    {

        // OpCode 1 (Select Device) is a special case which should execute 
        // regardless of which device is currently selected.
        // if(OpCode == 0x1) deviceContUB.SetDeviceFromRuntime((int)Opr1);

        // If Device 0 is not selected, Device 0 Ops shouldn't be executed,
        // so return.
        if(deviceContUB.Device != 0) return;

        // Debug.Log("Device0 Op");
        switch(OpCode)
        {
            case 0x0: break; // ???
            case 0x1: // (Deprecated) Set Device (already handled above) 
                break;
            case 0x2: // Stall
                float t = 0f; // Stall time value.
                switch (Opr2)
                {
                    case 0x0:
                        t = (float)Opr1; // Value in seconds.
                        break;
                    case 0x1:
                        t = (float)Opr1/1000; // Value in milliseconds. Convert to seconds.
                        break;
                    default: break;
                }
                stall_TimeStamp = Time.time + t; // Set new stall timestamp
                isStalling = true; 
                // Debug.Log("Stall ins");
                break;
            case 0x3: // (Deprecated) Auto-input a value into memory 
                // byte val = 0x0;
                // switch (Opr1)
                // {
                //     case 0x0: val = 8; break;
                //     case 0x1: val = 16; break;
                //     case 0x2: val = 32; break;
                //     case 0x3: val = 64; break;
                //     case 0x4: val = 128; break;
                //     case 0x5: val = 255; break;
                //     default: break;
                // }
                // // Write value to location at dataPointer-2, which should be 
                // // the location of Opr2.
                // memory[dataPointer-2] = val;
                // CreateMemDump();
                break;
            default: break;
        }
    }

    void SetIPC()
    {
        int sliderVal = (int)speedSlider.value;
        int speedVal = 1;
        switch(sliderVal)
        {
            case 1: speedVal = 1; break;
            case 2: speedVal = 4; break;
            case 3: speedVal = 16; break;
            case 4: speedVal = 64; break;
            case 5: speedVal = 256; break;
            case 6: speedVal = 1024; break;
            case 7: speedVal = 2048; break;
            case 8: speedVal = 4096; break;
            default: break;
        }
        ipc = speedVal;
    }

    void Init()
    {
        SetIPC();
        memory = new byte[MEMSIZE];
        insBuffer = new byte[INSBUFFERSIZE];
        insPointer = 0;
        dataPointer = 0;
        isExecuting = false;

        // char[] insCharArr = insInput.text.ToCharArray();
        
        // ValidateInsCharInput returns an array containing only valid ins chars. 
        char[] insCharArr = ValidateInsCharInput();
        // string s = new String(insCharArr); Debug.Log(s);

        byte[] tempByteArr = new byte[insCharArr.Length];
        // Use this method of converting ascii char array to byte array because
        // ASCIIEncoding.ASCII.GetBytes() is not available in Udon.
        for(int i = 0; i < insCharArr.Length; i++)
        {
            tempByteArr[i] = (byte)insCharArr[i];
        }
        
        // byte[] tempByteArr = ASCIIEncoding.ASCII.GetBytes(insCharArr, 0, insCharArr.Length);
        Array.Copy(tempByteArr, insBuffer, tempByteArr.Length);
        // insBuffer = ASCIIEncoding.ASCII.GetBytes(insCharArr, 0, insCharArr.Length);

        userInput.text = "";
        userOutput.text = "";
        userInput.interactable = false;
        speedSlider.interactable = false;
        userInputBtn.interactable = false;
        insPointerText.text = "0x0000";
        dataPointerText.text = "0x0000";

        isExecuting = true;
        isAcceptingInput = false;
        UpdateImgUI();
        CreateMemDump();

    }

    char[] ValidateInsCharInput()
    {
        // Instructions
        char[] validInsChars = new char[]{'<','>','+','-','.',',','[',']','='};
        
        // 0 = Newline, 1 = Comment
        char[] validOtherChars = new char[]{'\n','#'};

        string insInputText = insInput.text;
        
        // Strip comments
        for(int i = 0; i < insInputText.Length; i++)
        {
            // If # char is found
            if(insInputText[i] == validOtherChars[1])
            {
                // Make a new iteration and iterate till \n is found
                for(int j = i; j < insInputText.Length; j++)
                {
                    // If \n is found, this is a comment
                    if(insInputText[j] == validOtherChars[0])
                    {
                        // Remove chars from # to \n, basically removing the comment
                        insInputText = insInputText.Remove(i, (j-i)+1);
                        break;
                    }
                }
            }
        }

        // Strip invalid chars
        for(int i = 0; i < insInputText.Length; i++)
        {
            // If this flag never gets set to false, the char is invalid.
            bool invalidCharFound = true;

            for(int j = 0; j < validInsChars.Length; j++)
            {
                if(insInputText[i] == validInsChars[j]) invalidCharFound = false;
            }

            // If a char gets removed, the next char replaces the char at the current i 
            // index, so decrement i, so that the char doesn't get skipped over.
            if(invalidCharFound) { insInputText = insInputText.Remove(i,1); i--; }
        }

        // Strip whitespaces
        insInputText = insInputText.Replace(" ", "");
        
        // Debug.Log(insInputText);

        return insInputText.ToCharArray();
    }

    public void StartRuntime()
    {
        Init();
        deviceContUB.InitDeviceController();
    }

    void UpdateImgUI()
    {
        isExecutingImg.color = isExecuting ? TrueColor : FalseColor;
        isAcceptingInputImg.color = isAcceptingInput ? TrueColor : FalseColor;
    }

    /* 
    Memory dump shows the data of a range of cells (frame) in memory. The cell that the
    mem ptr points to must be placed at the center of the frame.
    */
    void CreateMemDump()
    {
        int memPtr = dataPointer;

        const int columns = 8;
        const int rows = 5;
        const int rowSize = 8;
        const int frameSize = 40;

        // Location of memPtr in frame. memPtr must be centered in the frame.
        // Range: 0 to [frameSize-1]. 
        const int ptrLocInFrame = 16; 

        string memDump = "";

        // int frameStart = memPtr < ptrLocInFrame ? 0 : memPtr-ptrLocInFrame;
        // int frameEnd = memPtr > memory.Length-(frameSize-ptrLocInFrame) ? 
        //     memory.Length-1 : frameStart+(frameSize-1);

        int frameStart = 0;
        int frameEnd = 0;

        if(memPtr < ptrLocInFrame)
        {
            frameStart = 0;
            frameEnd = frameSize-1; 
        }
        else if(memPtr > memory.Length-(frameSize-ptrLocInFrame))
        {
            frameStart = memory.Length-frameSize;
            frameEnd = memory.Length-1;
        }
        else
        {
            frameStart = memPtr-ptrLocInFrame;
            frameEnd = memPtr+(frameSize-ptrLocInFrame);
        }

        string debugStr = $"memPtr: {memPtr}, frameStart: {frameStart}, frameEnd: {frameEnd}";
        // Debug.Log(debugStr);
        
        for(int i = 0; i < rows; i++)
        {
            memDump += $"{Convert.ToString(frameStart+(i*rowSize),16).PadLeft(4,'0')}\t|\t";
            for(int j = 0; j < columns; j++)
            {
                memDump += $"{Convert.ToString(memory[frameStart+((i*rowSize)+j)], 16).PadLeft(2,'0')}\t";
            }
            memDump += "\n";
        }

        memDumpText.text = memDump;
    }

    public void AcceptInput()
    {
        // byte[] temp = ASCIIEncoding.ASCII.GetBytes(userInput.text.ToCharArray(), 0, 1);
        byte val = 0;
        Byte.TryParse(
            userInput.text, System.Globalization.NumberStyles.Integer, null, out val);
        memory[dataPointer] = val;
        // memory[dataPointer] = temp.Length > 0 ? temp[0] : (byte)0;
        isAcceptingInput = false;
        userInput.interactable = false;
        userInputBtn.interactable = false;

        UpdateImgUI();
    }

    // Method for scratch coding. Not meant for prod.
    void DebugStuff()
    {
        // 0-7 = Instructions
        char[] validInsChars = new char[]{'<','>','+','-','.',',','[',']'};
        
        // 0 = Newline, 1 = Comment
        char[] validOtherChars = new char[]{'\n','#'};

        string s = $"This is a sentence. # This is a comment.\nThis is a sentence on a new line. # This is another comment.\nThis is a sentence on another new line. # This is yet another comment.\n";
        s = "++[>-sdfsdf+.,sfdsf]#This is a comment.\n[ +   ]";
        // ++[>-+.,][+]
        
        Debug.Log(s);

        // Strip comments
        for(int i = 0; i < s.Length; i++)
        {
            if(s[i] == validOtherChars[1])
            {
                for(int j = i; j < s.Length; j++)
                {
                    if(s[j] == validOtherChars[0])
                    {
                        s = s.Remove(i, (j-i)+1);
                        break;
                    }
                }
            }
        }

        // Stripg invalid chars
        // s = s.Trim(validInsChars);
        for(int i = 0; i < s.Length; i++)
        {
            bool invalidCharFound = true;
            for(int j = 0; j < validInsChars.Length; j++)
            {
                if(s[i] == validInsChars[j]) invalidCharFound = false;
            }
            if(invalidCharFound) { s = s.Remove(i,1); i--; }
        }

        // Strip newlines
        s = s.Replace(validOtherChars[0].ToString(), "");

        // Strip whitespaces
        s = s.Replace(" ", "");
    }

    // Note: Maybe add a validation check to see if byte array 
    // being saved is zero. I don't remember if VFS files can have
    // zero bytes, so if there is a zero size array, create one 
    // element with a newline converted to byte.
    public void SaveDataToFile()
    {
        // if(isExecuting) return;
        byte[] dataBuffer = new byte[insInput.text.Length];
        if(dataBuffer.Length == 0)
        {
            dataBuffer = new byte[1];
            dataBuffer[0] = Convert.ToByte('\n');
        }
        else
        {
            for(int i = 0; i < dataBuffer.Length; i++)
            {
                dataBuffer[i] = Convert.ToByte(insInput.text[i]);
            }
        }
        vfsFileManager.SetProgramVariable("fileDataBuffer", dataBuffer);
    }

    public void LoadDataFromFile()
    {
        if(isExecuting) return;
        byte[] dataBuffer = 
            (byte[])vfsFileManager.GetProgramVariable("fileDataBuffer");
        char[] insCharArr = new char[dataBuffer.Length];
        for(int i = 0; i < insCharArr.Length; i++)
        {
            insCharArr[i] = Convert.ToChar(dataBuffer[i]);
        }
        insInput.text = new string(insCharArr);
    }

    #if UNITY_EDITOR
    void DbgExecuteCode()
    {
        insInput.text = dbgCodeStr;
        StartRuntime();
    }

    #endif
}
 