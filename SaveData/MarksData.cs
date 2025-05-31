using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Storage;
using static APlayer.StartPage.SavedData.Group;

namespace APlayer.SaveData
{
    public class MarksData
    {
        public StorageFolder? Folder { get; set; }

        private readonly Dictionary<string, MarksFile> Data = [];

        public async Task<MarksFile?> GetMarksFile(string name)
        {
            if (Data.TryGetValue(name, out var marksFile))
                return marksFile;

            return Folder is null ? null : await MarksFile.Load(name + ".json", Folder);
        }

        public void SetMarksFile(MarksFile marksFile)
        {
            Data[marksFile.Name] = marksFile;
        }

        public async Task Save()
        {
            if (Folder is null)
                return;
            foreach (var item in Data.Values)
            {
                if (item.Update)
                {
                    await item.Save(item.Name + ".json",Folder);
                }
            }
        }
        public async Task Save(string name)
        {
            if (Folder is null)
                return;
            if (Data.TryGetValue(name, out var marksFile))
                await marksFile.Save(name + ".json", Folder);
        }

    }

    public class MarksFile
    {
        public bool Update = false;
        public string Name { get; set; } = "";
        public Dictionary<string, int[]> Marks { get; set; } = [];

        public void SetMarks(string path, int[] marks)
        {
            Marks[path] = marks;
        }
        public void SetMarks(string path, TimeSpan[] marks)
        {
            Marks[path] = [.. marks.Select(e => (int)e.TotalSeconds)];
        }

        public TimeSpan[] GetMarks(string path)
        {
            var marks = Marks.GetValueOrDefault(path);
            if (marks is null)
                return [];
            return [.. marks.Select(e=> TimeSpan.FromSeconds(e))];
        }

        public static async Task<MarksFile?> Load(string file_name, StorageFolder folder)
        {
            try
            {
                var file = await folder.GetFileAsync(file_name);
                var json = await FileIO.ReadTextAsync(file);
                return JsonSerializer.Deserialize(json, MarksFileContext.Default.MarksFile);
            }
            catch (Exception)
            {
                return null;
            }
        }
        public async Task<bool> Save(string file_name, StorageFolder folder)
        {
            Update = false;
            if (Marks.Count == 0)
                return true;

            try
            {
                var json = JsonSerializer.Serialize(this, MarksFileContext.Default.MarksFile);
                var item = await folder.TryGetItemAsync(file_name);
                if (item == null)
                {
                    if (Marks.Count == 0)
                        return true;
                    var f = await folder.CreateFileAsync(file_name);
                    await FileIO.WriteTextAsync(f, json);
                    return true;
                }
                if (item is StorageFile file)
                {
                    if (Marks.Count == 0)
                    {
                        await file.DeleteAsync();
                        return true;
                    }

                    var file_json = await FileIO.ReadTextAsync(file);
                    if (json == file_json)
                    {
                        return true;
                    }
                    await FileIO.WriteTextAsync(file, json);
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }


    [JsonSerializable(typeof(MarksFile))]
    internal partial class MarksFileContext : JsonSerializerContext { }

}
