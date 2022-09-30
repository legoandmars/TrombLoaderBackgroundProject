using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

[Serializable]
public struct SongEvent {
    public int EventID;
    public UnityEvent unityEvent;
}
