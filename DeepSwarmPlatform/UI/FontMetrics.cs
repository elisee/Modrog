using DeepSwarmBasics.Math;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DeepSwarmPlatform.UI
{
    public class FontMetrics
    {
        public string Name;
        public int Size;
        public int Ascent;
        public int Descent;

        public readonly Dictionary<int, CharacterData> Characters = new Dictionary<int, CharacterData>();

        public class CharacterData
        {
            public int Advance;
            public Point Offset;
            public Rectangle SourceRectangle;
            public readonly Dictionary<int, int> Kerning = new Dictionary<int, int>();
        }

        // See http://pixel-fonts.com
        public static FontMetrics FromChevyRayJson(string path)
        {
            var json = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip }).RootElement;

            var metrics = new FontMetrics
            {
                Name = json.GetProperty("name").GetString(),
                Size = json.GetProperty("size").GetInt32(),
                Ascent = json.GetProperty("ascent").GetInt32(),
                Descent = json.GetProperty("descent").GetInt32(),
            };

            int charCount = json.GetProperty("char_count").GetInt32();
            int kerningCount = json.GetProperty("kerning_count").GetInt32();

            {
                var chars = json.GetProperty("chars");
                var advance = json.GetProperty("advance");
                var offset_x = json.GetProperty("offset_x");
                var offset_y = json.GetProperty("offset_y");
                var width = json.GetProperty("width");
                var height = json.GetProperty("height");
                var pack_x = json.GetProperty("pack_x");
                var pack_y = json.GetProperty("pack_y");
                var kerning = json.GetProperty("kerning");

                for (var i = 0; i < charCount; i++)
                {
                    int asciiIndex = chars[i].GetInt32();

                    metrics.Characters.Add(asciiIndex, new CharacterData
                    {
                        Advance = advance[i].GetInt32(),
                        Offset = new Point(offset_x[i].GetInt32(), offset_y[i].GetInt32()),
                        SourceRectangle = new Rectangle(pack_x[i].GetInt32(), pack_y[i].GetInt32(), width[i].GetInt32(), height[i].GetInt32())
                    });
                }

                for (var i = 0; i < kerningCount; i++)
                {
                    int leftAsciiIndex = kerning[i * 3 + 0].GetInt32();
                    int rightAsciiIndex = kerning[i * 3 + 1].GetInt32();
                    int offset = kerning[i * 3 + 2].GetInt32();

                    metrics.Characters[rightAsciiIndex].Kerning.Add(leftAsciiIndex, offset);
                }

                return metrics;
            }
        }
    }
}
