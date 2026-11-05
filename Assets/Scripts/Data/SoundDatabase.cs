using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Sound/Sound Database")]
public class SoundDatabase : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public SoundId Id;
        public AudioClip Clip;
    }

    [SerializeField] private List<Entry> _entries = new List<Entry>();
    private Dictionary<SoundId, Entry> _lookup;

    public bool TryGet(SoundId id, out Entry entry)
    {
        if(_lookup == null)
        {
            _lookup = new Dictionary<SoundId, Entry>();
            foreach(Entry e in _entries)
            {
                if(e == null || e.Id == SoundId.None)
                {
                    continue;
                }

                if (_lookup.ContainsKey(e.Id))
                {
                    continue;
                }
                _lookup.Add(e.Id, e);
            }
        }
        return _lookup.TryGetValue(id, out entry);
    }
}