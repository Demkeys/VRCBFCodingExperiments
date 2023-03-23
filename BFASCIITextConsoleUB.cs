using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class BFASCIITextConsoleUB : UdonSharpBehaviour
{
    const int CHARMAPSIZE = 94;
    byte[] CharMapKeyArr = new byte[CHARMAPSIZE]{
        0x21,0x22,0x23,0x24,0x25,0x26,0x27,0x28,0x29,0x2a,0x2b,0x2c,0x2d,0x2e,0x2f,0x30,0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,0x39,0x3a,0x3b,0x3c,0x3d,0x3e,0x3f,0x40,0x41,0x42,0x43,0x44,0x45,0x46,0x47,0x48,0x49,0x4a,0x4b,0x4c,0x4d,0x4e,0x4f,0x50,0x51,0x52,0x53,0x54,0x55,0x56,0x57,0x58,0x59,0x5a,0x5b,0x5c,0x5d,0x5e,0x5f,0x60,0x61,0x62,0x63,0x64,0x65,0x66,0x67,0x68,0x69,0x6a,0x6b,0x6c,0x6d,0x6e,0x6f,0x70,0x71,0x72,0x73,0x74,0x75,0x76,0x77,0x78,0x79,0x7a,0x7b,0x7c,0x7d,0x7e
    };
    char[] CharMapValueArr = new char[CHARMAPSIZE]{
        '!','"','#','$','%','&','\'','(',')','*','+',',','-','.','/','0','1','2','3','4','5','6','7','8','9',':',';','<','=','>','?','@','A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z','[','\\',']','^','_','`','a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z','{','|','}','~'
    };

    char cursorChar = '█';
    
    // Determines where the next char will be printed. This value can 
    // be manually changed by the user, by moving the cursor forward 
    // or backward.
    int cursorPos = 0;

    // This value will always be 1+OutputTextUI.text, because cursorChar
    // is always shown in the console.
    int textSize = 0; 

    [HideInInspector] public byte OpCode;
    [HideInInspector] public byte Opr1;
    [HideInInspector] public byte Opr2;
    [HideInInspector] public byte Opr3;
    [SerializeField] Text OutputTextUI;

    // Puzzle
    byte[] puzzleIndicesArr = new byte[9]; // Indices of each required char.
    // Flags mentioning whether each char has been used atleast once.
    bool[] puzzleSolveFlagsArr = new bool[9]; 
    [SerializeField] PuzzleContUB puzzleCont;

    void Start()
    {
        // Initialize text
        ClearConsole();
        
        // Set up puzzle indices array.
        puzzleIndicesArr[0] = 0x62;
        puzzleIndicesArr[1] = 0x72;
        puzzleIndicesArr[2] = 0x61;
        puzzleIndicesArr[3] = 0x69;
        puzzleIndicesArr[4] = 0x6e;
        puzzleIndicesArr[5] = 0x66;
        puzzleIndicesArr[6] = 0x75;
        puzzleIndicesArr[7] = 0x63;
        puzzleIndicesArr[8] = 0x6b;
    }

    void Update()
    {
        #if UNITY_EDITOR
        if(Input.GetKeyDown(KeyCode.Space))
            // Debug.Log("█");
        {

            // OpCode = 0; 
            // for(int i = 0; i < 6; i++)
            // {
            //     Opr1 = CharMapKeyArr[i];
            //     InvokeDevice();
            // } 

            // for(int i = 0; i < puzzleIndicesArr.Length; i++)
            // {
            //     Opr1 = puzzleIndicesArr[i];
            //     InvokeDevice();
            // }
            
        }
        if(Input.GetKeyDown(KeyCode.LeftArrow))
        {
            // OpCode = 2; Opr1 = 0; InvokeDevice();
        }
        if(Input.GetKeyDown(KeyCode.RightArrow))
        {
            // OpCode = 2; Opr1 = 1; InvokeDevice();
        }
        if(Input.GetKeyDown(KeyCode.C))
        {
            // OpCode = 3; InvokeDevice();
        }
        if(Input.GetKeyDown(KeyCode.D))
        {
            // OpCode = 4; Opr1 = 200; Opr2 = 25; Opr3 = 127; InvokeDevice();
            // if(cursorPos > 0)
            //     OutputTextUI.text = OutputTextUI.text.Remove(cursorPos-1, 1);
            // cursorPos = OutputTextUI.text.IndexOf(cursorChar);
        }
        #endif
    }

    public void InvokeDevice()
    {
        // Debug.Log($"Text Console | OpCoded: {OpCode}, Operand1: {Opr1}, Operand2: {Opr2}, Operand3: {Opr3}");
        if(OpCode == 0) // Print char
        {
            char c = '-';

            // If this is -1, Opr1 is not a valid character.
            int charMapIndex = Array.IndexOf(CharMapKeyArr, Opr1);
            if(charMapIndex == -1) return;

            c = CharMapValueArr[charMapIndex];
            OutputTextUI.text = 
                OutputTextUI.text.Insert(cursorPos, c.ToString());
            cursorPos = OutputTextUI.text.IndexOf(cursorChar);

            CheckPuzzle(CharMapKeyArr[charMapIndex]);
 
            // for(int i = 0; i < CHARMAPSIZE; i++)
            // {
            //     if(Opr1 == CharMapKeyArr[i])
            //     {
            //         c = CharMapValueArr[i];
            //         OutputTextUI.text = 
            //             OutputTextUI.text.Insert(cursorPos, c.ToString());
            //         cursorPos = OutputTextUI.text.IndexOf(cursorChar);
            //         break;
            //     }
            // }
            
        }
        else if(OpCode == 1) // Delete at cursor position
        {
            if(cursorPos > 0)
                OutputTextUI.text = OutputTextUI.text.Remove(cursorPos-1, 1);
            cursorPos = OutputTextUI.text.IndexOf(cursorChar);
        }
        else if(OpCode == 2) // Move Cursor
        {
            if(Opr1 == 0) // Left
            {
                OutputTextUI.text = OutputTextUI.text.Remove(cursorPos,1);
                cursorPos = cursorPos > 0 ? cursorPos-- : cursorPos;
                OutputTextUI.text = OutputTextUI.text.Insert(cursorPos, cursorChar.ToString());
            }
            else if(Opr1 == 1) // Right
            {
                OutputTextUI.text = OutputTextUI.text.Remove(cursorPos,1);
                // At this point the text and text size don't include cursorChar.
                cursorPos = cursorPos < OutputTextUI.text.Length ? cursorPos++ : cursorPos;
                OutputTextUI.text = OutputTextUI.text.Insert(cursorPos, cursorChar.ToString());
            }
        }
        else if(OpCode == 3) // Clear Console
        {
            ClearConsole();
        }
        else if(OpCode == 4) // Change Text Color
        {
            Color32 textCol = new Color32(Opr1,Opr2,Opr3,(byte)0xff);
            OutputTextUI.color = textCol;
        }
    }

    void ClearConsole()
    {
        // Set text to cursorChar instead of completely empty.
        OutputTextUI.text = cursorChar.ToString();
        cursorPos = 0;
    }

    public void InitDevice()
    {
        OutputTextUI.text = cursorChar.ToString();
        cursorPos = 0;
        OpCode = 0; Opr1 = 0; Opr2 = 0; Opr3 = 0; 
    }

    void CheckPuzzle(byte asciiCode)
    {
        // Iterate over puzzleIndices to see if asciiCode matches any cells,
        // and if it does, set respective puzzleSolveFlagsArr item to true.
        // for(int i = 0; i < puzzleIndicesArr.Length; i++)
        // {
        //     if(asciiCode == puzzleIndicesArr[i]) 
        //         puzzleSolveFlagsArr[i] = true;
        // }

        // Check if asciiCode value exists in puzzleIndicesArr. If it does,
        // set the corresponding flag to true in puzzleSolveFlagsArr.
        int charIndex = Array.IndexOf(puzzleIndicesArr, asciiCode);
        if(charIndex != -1) puzzleSolveFlagsArr[charIndex] = true;

        // Finally check if all flags in puzzleSolveFlagsArr are true. If they
        // are, puzzle is solved.
        for(int i = 0; i < puzzleSolveFlagsArr.Length; i++)
        {
            if(puzzleSolveFlagsArr[i] != true) return;
        }

        // If code gets to this point, all flags are true, and puzzle has been
        // solved. So called respective method in puzzle controller.
        // Debug.Log("ASCII puzzle solved");
        puzzleCont.ASCIIConsolePhase1Solved();
    }
}


// █