
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

public class BFDeviceControllerUB : UdonSharpBehaviour
{
    [HideInInspector] public byte OpCode;
    [HideInInspector] public byte Opr1;
    [HideInInspector] public byte Opr2;
    [HideInInspector] public byte Opr3;
    [HideInInspector] public int Device; // 0 = None, 1 = Text Console, 2 = Color Grid, 3 = Cube Controller
    string[] DeviceNameArr = new string[4]{
        "None","Text Console","Color Grid","Cube Controller"
    };
    [Header("Devices")]
    [SerializeField] BFASCIITextConsoleUB asciiConsoleUB;
    [SerializeField] BFColorGridUB colorGridUB;
    [SerializeField] CubeControllerUB cubeContUB;
    [Header("UI")]
    [SerializeField] Toggle[] deviceToggleArr;
    [SerializeField] Text deviceNameText;
    [Header("Puzzle")]
    [SerializeField] PuzzleContUB puzzleContUB;

    void Start()
    {
        
        
    }

    void Update()
    {
        #if UNITY_EDITOR
            if(Input.GetKeyDown(KeyCode.Alpha0)) deviceToggleArr[0].isOn = true;
            else if(Input.GetKeyDown(KeyCode.Alpha1)) deviceToggleArr[1].isOn = true;
            else if(Input.GetKeyDown(KeyCode.Alpha2)) deviceToggleArr[2].isOn = true;
            else if(Input.GetKeyDown(KeyCode.Alpha3)) deviceToggleArr[3].isOn = true;
            else if(Input.GetKeyDown(KeyCode.Alpha4)) OnDeviceToggleChanged();
        #endif
    }

    void DebugSetDeviceToggleOn(int toggleIndex)
    {
        
    }

    public void InvokeDeviceController()
    {
        // Debug.Log($"Device Controller | Device: {Device}, OpCoded: {OpCode}, Operand1: {Opr1}, Operand2: {Opr2}, Operand3: {Opr3}");
        if(Device == 0)
        {
            // Device 0 ops are runtime related ops. They are handled 
            // in the BF Runtime.
        }
        else if(Device == 1)
        {
            
            // asciiConsoleUB.Mode = Mode;
            asciiConsoleUB.OpCode = OpCode;
            asciiConsoleUB.Opr1 = Opr1;
            asciiConsoleUB.Opr2 = Opr2;
            asciiConsoleUB.Opr3 = Opr3;
            asciiConsoleUB.InvokeDevice();

            // Puzzle Code
            byte[] hexKey = puzzleContUB.asciiConsoleHexKeyArr;
            if(OpCode == hexKey[0] && Opr1 == hexKey[1] && 
                Opr2 == hexKey[2] && Opr3 == hexKey[3])
            {
                // puzzleContUB.ASCIIConsoleSolved();
            }
        }
        else if(Device == 2)
        {
            colorGridUB.OpCode = OpCode;
            colorGridUB.Opr1 = Opr1;
            colorGridUB.Opr2 = Opr2;
            colorGridUB.Opr3 = Opr3;
            colorGridUB.InvokeDevice();

            // Puzzle Code
            byte[] hexKey = puzzleContUB.colorGridHexKeyArr;
            if(OpCode == hexKey[0] && Opr1 == hexKey[1] && 
                Opr2 == hexKey[2] && Opr3 == hexKey[3])
            {
                // puzzleContUB.ColorGridSolved();
            }
        }
        else if(Device == 3)
        {
            cubeContUB.OpCode = OpCode;
            cubeContUB.Opr1 = Opr1;
            cubeContUB.Opr2 = Opr2;
            cubeContUB.Opr3 = Opr3;
            cubeContUB.InvokeDevice();

            // Puzzle Code
            byte[] hexKey = puzzleContUB.cubeContHexKeyArr;
            if(OpCode == hexKey[0] && Opr1 == hexKey[1] && 
                Opr2 == hexKey[2] && Opr3 == hexKey[3])
            {
                // puzzleContUB.CubeContSolved();
            }
        }
        
    }

    // Called from BFRuntime when Device0 OpCode 0x1 is executed.
    public void SetDeviceFromRuntime(int device)
    {
        // If device is not within 0-3 range, return.
        if(device < 0 || device > 3) return;

        // Set all toggle values based on 
        for(int i = 0; i < deviceToggleArr.Length; i++)
        {
            // deviceToggleArr[i].
            if(i == device)
            {
                deviceToggleArr[i].isOn = true;
                // Device = i;
                // deviceNameText.text = DeviceNameArr[i];
            }
            // else deviceToggleArr[i].isOn = false;
        }

        /*
        This block is necessary due to a Unity bug that causes UI Toggle
        OnValueChanged event to not fire in the Editor.
        */
        #if UNITY_EDITOR
        OnDeviceToggleChanged();
        #endif
        // Debug.Log($"Device set to: {Device}");
    }

    public void OnDeviceToggleChanged()
    {
        for(int i = 0; i < deviceToggleArr.Length; i++)
        {
            if(deviceToggleArr[i].isOn)
            {
                Device = i;
                deviceNameText.text = DeviceNameArr[i];
            }

        }
        // Debug.Log($"Device set to: {Device}");
    }

    // Called when BF Runtime starts executing. Initializes other devices. This 
    // way all device get reset each time the BF Runtime starts executing.
    public void InitDeviceController()
    {
        asciiConsoleUB.InitDevice();
        colorGridUB.InitDevice();
        cubeContUB.InitDevice();
    }
}
