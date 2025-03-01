using APlayer.StartPage;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;
using static APlayer.StartPage.SavedData.Group;

namespace APlayer.SaveData
{
    public static class SaveData
    {
        public static async Task<Contents?> LoadContents(StorageFolder folder,string file_name = "index.json")
        {
            try
            {
                var file = await   folder.GetFileAsync(file_name);
                var json = await FileIO.ReadTextAsync(file);
                return JsonSerializer.Deserialize(json, ContentsContext.Default.Contents);
            }
            catch (Exception)
            {
                return null;
            }
        }
        public static async Task<bool> SaveContents(Contents contents, StorageFolder folder, string file_name = "index.json")
        {
            try
            {
                var json = JsonSerializer.Serialize(contents, ContentsContext.Default.Contents);

                var item = await folder.TryGetItemAsync(file_name);
                if (item == null)
                {
                    var f = await folder.CreateFileAsync(file_name);
                    await FileIO.WriteTextAsync(f, json);
                    return true;
                }
                if (item is StorageFile file)
                {
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

        public static async Task<List?> LoadList(StorageFolder folder, string file_name)
        {
            try
            {
                var file = await folder.GetFileAsync(file_name);
                var json = await FileIO.ReadTextAsync(file);
                return JsonSerializer.Deserialize(json, ListContext.Default.List);
            }
            catch (Exception)
            {
                return null;
            }
        }
        public static async Task<bool> SaveList(List list,StorageFolder folder, string file_name)
        {
            try
            {
                var json = JsonSerializer.Serialize(list, ListContext.Default.List);

                var item = await folder.TryGetItemAsync(file_name);
                if (item == null)
                {
                    var f = await folder.CreateFileAsync(file_name);
                    await FileIO.WriteTextAsync(f, json);
                    return true;
                }
                if (item is StorageFile file)
                {
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
        public static async Task DeleteList(StorageFolder folder, List<string> file_name)
        {
            foreach (var fn in file_name)
            {
                var item = await folder.TryGetItemAsync(fn);
                if (item is StorageFile)
                {
                    await item.DeleteAsync();
                }
            }
        }
    }

    [JsonSerializable(typeof(Contents))]
    internal partial class ContentsContext : JsonSerializerContext { }

    [JsonSerializable(typeof(List))]
    internal partial class ListContext : JsonSerializerContext { }


    public class Contents(List<ListIndex> indexes)
    {
        public List<ListIndex> Indexes { get; set; } = indexes;
    }
    public class ListIndex(string name, string file_name, int order)
    {
        public string Name { get; set; } = name;
        public string FileName { get; set; } = file_name;
        public int Order { get; set; } = order;
    }

    public class List(string name, IEnumerable<Folder> folders)
    {
        public string Name { get; set; } = name;
        public IEnumerable<Folder> Folders { get; set; } = folders;
    }
    public class Folder(string name, string path)
    {
        public string Name { get; set; } = name;
        public string Path { get; set; } = path;
    }
}
