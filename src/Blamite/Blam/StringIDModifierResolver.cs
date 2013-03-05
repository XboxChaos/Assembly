using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Blam
{
    /// <summary>
    /// Implementation of IStringIDResolver that uses modifier values to translate stringIDs into array indices.
    /// NOTE: This class is deprecated because it is inaccurate. See <seealso cref="StringIDSetResolver"/> instead.
    /// </summary>
    public class StringIDModifierResolver : IStringIDResolver
    {
        private List<StringIDModifier> _modifiers = new List<StringIDModifier>();

        /// <summary>
        /// Adds a new modifier value that can be applied to stringIDs.
        /// </summary>
        /// <param name="identifier">The value that the ID must compare to for the modification to take place.</param>
        /// <param name="modifier">The modifier value to add or subtract from the ID.</param>
        /// <param name="greaterThan">true if the modifier should be applied when the stringID is greater than the identifier, or false if a less-than comparison should be used.</param>
        /// <param name="addition">true if the modifier should be added to stringIDs when converting them to array indices, or false if subtraction should be used.</param>
        public void AddModifier(int identifier, int modifier, bool greaterThan, bool addition)
        {
            StringIDModifier newModifier = new StringIDModifier();
            newModifier.Identifier = identifier;
            newModifier.Modifier = modifier;
            newModifier.IsGreaterThan = greaterThan;
            newModifier.IsAddition = addition;
            _modifiers.Add(newModifier);
        }

        /// <summary>
        /// Translates a stringID into an index into the global debug strings array.
        /// </summary>
        /// <param name="id">The StringID to translate.</param>
        /// <returns>The index of the string in the global debug strings array.</returns>
        public int StringIDToIndex(StringID id)
        {
            foreach (StringIDModifier modifier in _modifiers)
            {
                bool modify = false;

                if (modifier.IsGreaterThan)
                    modify = (id.Value > modifier.Identifier);
                else
                    modify = (id.Value < modifier.Identifier);

                if (modify)
                {
                    if (modifier.IsAddition)
                        return id.Value + modifier.Modifier;
                    else
                        return id.Value - modifier.Modifier;
                }
            }
            return id.Value;
        }

        /// <summary>
        /// Translates a string index into a stringID which can be written to the file.
        /// </summary>
        /// <param name="index">The index of the string in the global strings array.</param>
        /// <returns>The stringID associated with the index.</returns>
        public StringID IndexToStringID(int index)
        {
            foreach (StringIDModifier modifier in _modifiers)
            {
                bool modify = false;

                if (modifier.IsGreaterThan)
                    modify = (index > modifier.Identifier);
                else
                    modify = (index < modifier.Identifier);

                if (modify)
                {
                    if (modifier.IsAddition)
                        return new StringID(index - modifier.Modifier);
                    else
                        return new StringID(index + modifier.Modifier);
                }
            }
            return new StringID(index);
        }

        private class StringIDModifier
        {
            public int Identifier { get; set; }
            public int Modifier { get; set; }
            public bool IsGreaterThan { get; set; }
            public bool IsAddition { get; set; }
        }
    }
}
