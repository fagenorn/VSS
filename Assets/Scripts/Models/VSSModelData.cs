using Assets.Scripts.Live2D;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace Assets.Scripts.Models
{
    using J = JsonPropertyAttribute;

    public partial class VSSModelData : BaseData
    {
        [J("_name")] public string Name { get; set; }

        [J("_id")] public Guid Id { get; set; }

        [J("_transform")] public TransformData Transform { get; set; }

        [J("_files")] public ModelFiles Files { get; set; }

        [J("_imported_date")] public DateTimeOffset ImportedDate { get; set; }

        [J("_tracker_parameters")] public ObservableCollection<BodyTrackerValueProvider> TrackerParameters { get; set; }

        [J("_animation_parameters")] public ObservableCollection<Live2DAnimationProvider> AnimationParameters { get; set; }
    }

    public partial class VSSModelData
    {
        private const int _saveIntervalMillisec = 2500;

        public static VSSModelData FromJson(string json) { return JsonConvert.DeserializeObject<VSSModelData>(json, JsonSettings.Default); }

        public string ToJson() { return JsonConvert.SerializeObject(this, JsonSettings.Default); }

        public async UniTaskVoid SaveAsync()
        {
            _saveTimer.Restart();

            lock (_saveTimer)
            {
                if (_saving) return;
                _saving = true;
            }

            await UniTask.WaitUntil(() => _saveTimer.ElapsedMilliseconds > _saveIntervalMillisec);
            _saveTimer.Reset();

            try
            {
                var path = Path.Combine(_currentDirectory, _currentFile);
                if (!File.Exists(path)) return;

                using (var fileStream = File.Open(path, FileMode.Create))
                using (var writer = new StreamWriter(fileStream))
                {
                    await writer.WriteAsync(ToJson());
                    UnityEngine.Debug.Log("SAVED!");
                }
            }
            finally
            {
                lock (_saveTimer)
                {
                    _saving = false;
                }
            }
        }

        private bool _saving = false;

        private Stopwatch _saveTimer = new Stopwatch();

        [JsonIgnore] private string _currentDirectory;

        [JsonIgnore] private string _currentFile;

        public void SetCurrentPath(string path)
        {
            _currentDirectory = Path.GetDirectoryName(path);
            _currentFile = Path.GetFileName(path);
        }

        public string GetFullPath(ModelFile file)
        {
            return Path.Combine(_currentDirectory, file.Name);
        }
    }

    public class ModelFiles
    {
        [J("_icon")] public ModelFile Icon { get; set; }

        [J("_model")] public ModelFile Model { get; set; }
    }

    public class ModelFile
    {
        [J("_name")] public string Name { get; set; }
    }

    public class TransformData
    {
        [J("_position")] public Vector3Json Position { get; set; }

        [J("_scale")] public Vector3Json Scale { get; set; }

        [J("_rotation")] public QuaternionJson Rotation { get; set; }
    }

    public class Vector3Json
    {
        public Vector3Json() { }

        public Vector3Json(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        [J("_x")] public float X { get; set; }

        [J("_y")] public float Y { get; set; }

        [J("_z")] public float Z { get; set; }

        public static implicit operator Vector3(Vector3Json data) => new Vector3(data.X, data.Y, data.Z);

        public static implicit operator Vector3Json(Vector3 data) => new Vector3Json(data.x, data.y, data.z);
    }

    public class QuaternionJson
    {
        public QuaternionJson() { }

        public QuaternionJson(float x, float y, float z, float w)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.W = w;
        }

        [J("_x")] public float X { get; set; }

        [J("_y")] public float Y { get; set; }

        [J("_z")] public float Z { get; set; }

        [J("_w")] public float W { get; set; }

        public static implicit operator Quaternion(QuaternionJson data) => new Quaternion(data.X, data.Y, data.Z, data.W);

        public static implicit operator QuaternionJson(Quaternion data) => new QuaternionJson(data.x, data.y, data.z, data.w);
    }
}
