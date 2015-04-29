using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blamite.Serialization;
using Blamite.IO;

namespace Blamite.Blam.FourthGen
{
    class FourthGenIndexedStringIDSource : StringIDSource
    {
        private readonly FourthGenIndexedStringTable _strings;

        public FourthGenIndexedStringIDSource(FourthGenIndexedStringTable strings, IStringIDResolver resolver)
        {
            _strings = strings;
        }

        // These values were figured out through trial-and-error
        private static readonly int[] _setOffsets = { 0x90F, 0x1, 0x685, 0x720, 0x7C4, 0x778, 0x7D0, 0x8EA, 0x902 };
        private const int SetMin = 0x1;   // Mininum index that goes in a set
        private const int SetMax = 0xF1E; // Maximum index that goes in a set

        public int GetIndex(StringID strID)
        {
            var stringId = (int)strID.Value;

            // Get the set and index
            var set = stringId >> 16;      // Set = upper 16 bits
            var index = stringId & 0xFFFF; // Index = lower 16 bits

            int strIndex;
            if (set == 0 && (index < SetMin || index > SetMax))
            {
                // Value does not go into a set, so the index is the same as the ID
                strIndex = index;
            }
            else
            {
                if (set < 0 || set >= _setOffsets.Length)
                    return -1; // Invalid set number

                // Convert the index part of the ID into a string index based on its set
                if (set == 0)
                    index -= SetMin;
                strIndex = index + _setOffsets[set];
            }
            if (strIndex < 0 || strIndex >= _strings.Count)
                return -1;

            return strIndex;
        }

        public int GetStringId(int strIndex)
        {
            if (strIndex < 0 || strIndex >= _strings.Count)
                return 0;

            // If the value is outside of a set, just return it
            if (strIndex < SetMin || strIndex > SetMax)
                return strIndex;

            // Find the set which the index is closest to
            // TODO: This could probably be more optimized if the set offset list was sorted or something
            var set = 0;
            var minDistance = _strings.Count;
            for (var i = 0; i < _setOffsets.Length; i++)
            {
                if (strIndex < _setOffsets[i])
                    continue;
                var distance = strIndex - _setOffsets[i];
                if (distance >= minDistance)
                    continue;
                set = i;
                minDistance = distance;
            }

            // Compute the index within the set
            var index = strIndex - _setOffsets[set];
            if (set == 0)
                index += SetMin;

            // Set is the upper 16 bits, index is the lower 16
            return (set << 16) | (index & 0xFFFF);
        }

        public override int Count
        {
            get { return _strings.Count; }
        }

        public override StringIDLayout IDLayout
        {
            get { return null; }
        }

        public override int StringIDToIndex(StringID id)
        {
            return GetIndex(id);



            //if (_resolver != null)
            //    return _resolver.StringIDToIndex(id);
            //return -1;
        }

        public override StringID IndexToStringID(int index)
        {
            return new StringID((uint)GetStringId(index));
        }

        public override string GetString(int index)
        {
            if (index >= 0 && index < _strings.Count)
                return _strings[index];
            return null;
        }

        public override int FindStringIndex(string str)
        {
            return _strings.IndexOf(str);
        }

        public override IEnumerator<string> GetEnumerator()
        {
            return _strings.GetEnumerator();
        }

        public override StringID AddString(string str)
        {
            var index = _strings.Count;
            _strings.Add(str);
            return IndexToStringID(index);
        }

        public override void SetString(int index, string str)
        {
            _strings[index] = str;
        }

        internal void SaveChanges(IStream stream)
        {
            _strings.SaveChanges(stream);
        }
    }
}
