using System;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord.Net;
using Discord;
using Discord.WebSocket;
using System.Diagnostics;
using FreneticUtilities.FreneticExtensions;
using FreneticUtilities.FreneticDataSyntax;
using DenizenBot.MetaObjects;

namespace DenizenBot
{
    /// <summary>
    /// Helper class to contain the full set of meta documentation, and the logic to load it in.
    /// </summary>
    public class MetaDocs
    {
        /// <summary>
        /// Primary Denizen official sources.
        /// </summary>
        public static readonly string[] DENIZEN_SOURCES = new string[]
        {
            "https://github.com/DenizenScript/Denizen-For-Bukkit/archive/dev.zip",
            "https://github.com/DenizenScript/Denizen-Core/archive/master.zip"
        };

        /// <summary>
        /// Denizen secondary addon sources.
        /// </summary>
        public static readonly string[] DENIZEN_ADDON_SOURCES = new string[]
        {
            "https://github.com/DenizenScript/Depenizen/archive/master.zip",
            "https://github.com/DenizenScript/dDiscordBot/archive/master.zip",
            "https://github.com/DenizenScript/Webizen/archive/master.zip",
            "https://github.com/DenizenScript/dIRCBot/archive/master.zip"
        };

        /// <summary>
        /// All known commands.
        /// </summary>
        public Dictionary<string, MetaCommand> Commands = new Dictionary<string, MetaCommand>(512);

        /// <summary>
        /// Getters for standard meta object types.
        /// </summary>
        public static Dictionary<string, Func<MetaObject>> MetaObjectGetters = new Dictionary<string, Func<MetaObject>>()
        {
            { "command", () => new MetaCommand() }
        };

        /// <summary>
        /// Download all docs.
        /// </summary>
        public void DownloadAll()
        {
            foreach (string src in DENIZEN_SOURCES)
            {
                Download(src);
            }
            foreach (string src in DENIZEN_ADDON_SOURCES)
            {
                Download(src);
            }
        }

        /// <summary>
        /// Download a zip file from a URL.
        /// </summary>
        public static ZipArchive DownloadZip(string url)
        {
            HttpClient client = new HttpClient();
            client.Timeout = new TimeSpan(0, 2, 0);
            byte[] zipDataBytes = client.GetByteArrayAsync(url).Result;
            MemoryStream zipDataStream = new MemoryStream(zipDataBytes);
            return new ZipArchive(zipDataStream);
        }

        /// <summary>
        /// End of file marker.
        /// </summary>
        public const string END_OF_FILE_MARK = "\0END_OF_FILE";

        /// <summary>
        /// Start of file marker prefix.
        /// </summary>
        public const string START_OF_FILE_PREFIX = "\0START_OF_FILE ";

        /// <summary>
        /// Read lines of meta docs from Java files in a zip.
        /// </summary>
        public static string[] ReadLines(ZipArchive zip, string folderLimit = null)
        {
            List<string> lines = new List<string>();
            foreach (ZipArchiveEntry entry in zip.Entries)
            {
                if (folderLimit != null && !entry.FullName.StartsWith(folderLimit))
                {
                    continue;
                }
                if (!entry.FullName.EndsWith(".java"))
                {
                    continue;
                }
                using (Stream entryStream = entry.Open())
                {
                    lines.Add(START_OF_FILE_PREFIX + entry.FullName);
                    lines.AddRange(entryStream.AllLinesOfText().Where((s) => s.TrimStart().StartsWith("// ")).Select((s) => s.Trim().Substring("// ".Length)));
                    lines.Add(END_OF_FILE_MARK);
                }
            }
            return lines.ToArray();
        }

        /// <summary>
        /// Load the meta doc data from lines.
        /// </summary>
        public void LoadDataFromLines(string[] lines)
        {
            string file = "<unknown>";
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.StartsWith(START_OF_FILE_PREFIX))
                {
                    file = line.Substring(START_OF_FILE_PREFIX.Length);
                }
                else if (line.StartsWith("<--[") && line.EndsWith("]"))
                {
                    string objectType = line.Substring("<--[".Length, lines[i].Length - "<--[]".Length);
                    List<string> objectData = new List<string>();
                    for (i++; i < lines.Length; i++)
                    {
                        if (lines[i] == "-->")
                        {
                            break;
                        }
                        else if (lines[i] == END_OF_FILE_MARK || lines[i].StartsWith(START_OF_FILE_PREFIX))
                        {
                            LoadErrors.Add("While processing " + file + " was not able to find the end of an object's documentation!");
                            objectData = null;
                            break;
                        }
                        objectData.Add(lines[i]);
                    }
                    if (objectData == null)
                    {
                        continue;
                    }
                    objectData.Add("@end_meta");
                    LoadInObject(objectType, file, objectData.ToArray());
                }
            }
        }

        /// <summary>
        /// Load an object into the meta docs from the object's text definition.
        /// </summary>
        public void LoadInObject(string objectType, string file, string[] objectData)
        {
            if (!MetaObjectGetters.TryGetValue(objectType.ToLowerFast(), out Func<MetaObject> getter))
            {
                LoadErrors.Add("While processing " + file + " found unknown meta type '" + objectType + "'.");
                return;
            }
            MetaObject obj = getter();
            string curKey = null;
            string curValue = null;
            foreach (string line in objectData)
            {
                if (line.StartsWith("@"))
                {
                    if (curKey != null && curValue != null)
                    {
                        if (!obj.ApplyValue(curKey, curValue))
                        {
                            LoadErrors.Add("While processing " + file + " in object type '" + objectType + "' for '"
                                + obj.Name + "' could not apply key '" + curKey + "' with value '" + curValue + "'.");
                        }
                        curKey = null;
                        curValue = null;
                    }
                    int space = line.IndexOf(' ');
                    if (space == -1)
                    {
                        curKey = line.Substring(1);
                        if (curKey == "end_meta")
                        {
                            break;
                        }
                        continue;
                    }
                    curKey = line.Substring(1, space - 1);
                    curValue = line.Substring(space + 1);
                }
                else
                {
                    curValue += "\n" + line;
                }
            }
            obj.AddTo(this);
        }

        /// <summary>
        /// Download and load a source.
        /// </summary>
        public void Download(string source, string folderLimit = null)
        {
            try
            {
                ZipArchive zip = DownloadZip(source);
                string[] fullLines = ReadLines(zip, folderLimit);
                LoadDataFromLines(fullLines);
            }
            catch (Exception ex)
            {
                LoadErrors.Add("Internal exception - " + ex.GetType().FullName + " ... see bot console for details.");
                Console.WriteLine("Error: " + ex.ToString());
            }
        }

        /// <summary>
        /// A list of load-time errors, if any.
        /// </summary>
        public List<string> LoadErrors = new List<string>();
    }
}
