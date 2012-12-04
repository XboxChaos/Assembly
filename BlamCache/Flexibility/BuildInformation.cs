using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtryzeDLL.Blam.Scripting;
using ExtryzeDLL.Blam.Util;
using ExtryzeDLL.Flexibility;

namespace ExtryzeDLL.Flexibility
{
    public class BuildInformation
    {
        private string _gameName;
        private AESKey _localeKey;
        private AESKey _stringidKey;
        private AESKey _filenameKey;
        private string _shortName;
        private string _pluginFolder;
        private int _headerSize;
        private bool _loadStrings;
        private string _layoutFile;
        private string _scriptDefsFile;
        private Dictionary<string, StructureLayout> _layouts = new Dictionary<string, StructureLayout>();
        private LocaleSymbolCollection _localeSymbols = new LocaleSymbolCollection();
        private IList<StringIDModifier> _stringidModifiers = new List<StringIDModifier>();

        public class StructureLocaleSymbol
        {
            public char Code { get; set; }
            public string CodeAsString { get; set; }
            public string Display { get; set; }
        }
        public class StringIDModifier
        {
            public int Identifier { get; set; }
            public int Modifier { get; set; }
            public bool isGreaterThan { get; set; }
            public bool isAddition { get; set; }
        }

        public BuildInformation(string game, string localeKey, string stringidKey, IList<StringIDModifier> stringidModifier, string filenameKey, int headerSize, bool loadStrings, string layoutFile, string shortName, string pluginFolder, string scriptDefsFile)
        {
            _gameName = game;
            if (localeKey != null)
                _localeKey = new AESKey(localeKey);
            if (stringidKey != null)
                _stringidKey = new AESKey(stringidKey);
            _stringidModifiers = stringidModifier;
            if (filenameKey != null)
                _filenameKey = new AESKey(filenameKey);
            _headerSize = headerSize;
            _loadStrings = loadStrings;
            _layoutFile = layoutFile;
            _shortName = shortName;
            _pluginFolder = pluginFolder;
            _scriptDefsFile = scriptDefsFile;
        }

        public LocaleSymbolCollection LocaleSymbols
        {
            get { return _localeSymbols; }
        }

        public string GameName
        {
            get { return _gameName; }
        }

        public string ShortName
        {
            get { return _shortName; }
        }

        public string PluginFolder
        {
            get { return _pluginFolder; }
        }

        public AESKey LocaleKey
        {
            get { return _localeKey; }
        }

        public AESKey StringIDKey
        {
            get { return _stringidKey; }
        }

        public IList<StringIDModifier> StringIDModifiers
        {
            get { return _stringidModifiers; }
        }

        public AESKey FileNameKey
        {
            get { return _filenameKey; }
        }

        public int HeaderSize
        {
            get { return _headerSize; }
        }

        public bool LoadStrings
        {
            get { return _loadStrings; }
        }

        public string LayoutFilename
        {
            get { return _layoutFile; }
        }

        public string ScriptDefinitionsFilename
        {
            get { return _scriptDefsFile; }
        }
    }
}