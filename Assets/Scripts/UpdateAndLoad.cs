using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpdateAndLoad : MonoBehaviour
{
    public Text TxtInfo;

    public void SetTxtInfo(string txt)
    {
        TxtInfo.text = txt;
    }
}
