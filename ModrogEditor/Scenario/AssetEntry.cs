using System.Collections.Generic;

namespace ModrogEditor.Scenario
{
    class AssetEntry
    {
        public AssetType AssetType;
        public string Name;
        public string Path;
        public readonly List<AssetEntry> Children = new List<AssetEntry>();
    }
}
