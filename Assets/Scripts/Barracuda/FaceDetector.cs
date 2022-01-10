using Mediapipe;
using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;
using static Assets.Scripts.Barracuda.BoxDecoder;
using static Assets.Scripts.Barracuda.NonMaxSuppressionFilter;

namespace Assets.Scripts.Barracuda
{
    public class FaceDetector : MonoBehaviour
    {
        [SerializeField] public SpriteRenderer _spriteRenderer;

        [SerializeField] public NNModel _faceDetectionModel;

        private Model _runtimeFaceDetectionModel;

        private IWorker _workerFaceDetection;

        [SerializeField] public NNModel _faceLandmarkModel;

        private Model _runtimeFaceLandmarkModel;

        private IWorker _workerFaceLandmark;

        private int main_zero_border_id;

        private int main_repeat_id;

        [SerializeField] ComputeShader image_to_tensor_compute_shader;

        [SerializeField] BoxDecoder boxDecoder;

        NonMaxSuppressionFilter _nonMaxSuppressionFilter = new NonMaxSuppressionFilter();

        private int input_data_id;
        private int output_data_id;
        private int transform_matrix_id;
        private int alpha_id;
        private int beta_id;
        private int out_size_id;

        private struct ValueTransformation
        {
            public float scale;
            public float offset;
        }

        private struct Size
        {
            public Size(int height, int width) : this()
            {
                this.height = height;
                this.width = width;
            }

            public int height;

            public int width;
        }

        private struct RotatedRect
        {
            public float center_x;
            public float center_y;
            public float width;
            public float height;
            public float rotation;
        };

        private RenderTexture _rt;

        private BoxDecoder.Detection _latest;

        private void Start()
        {
            _runtimeFaceDetectionModel = ModelLoader.Load(_faceDetectionModel, verbose: false);
            _workerFaceDetection = WorkerFactory.CreateWorker(WorkerFactory.Type.Compute, _runtimeFaceDetectionModel, verbose: false);

            _runtimeFaceLandmarkModel = ModelLoader.Load(_faceLandmarkModel, verbose: false);
            _workerFaceLandmark = WorkerFactory.CreateWorker(WorkerFactory.Type.Compute, _runtimeFaceLandmarkModel, verbose: true);

            main_zero_border_id = image_to_tensor_compute_shader.FindKernel("mainZeroBorder");
            main_repeat_id = image_to_tensor_compute_shader.FindKernel("mainRepeat");

            input_data_id = Shader.PropertyToID("input_data");
            output_data_id = Shader.PropertyToID("output_data");
            transform_matrix_id = Shader.PropertyToID("transform_matrix");
            alpha_id = Shader.PropertyToID("alpha");
            beta_id = Shader.PropertyToID("beta");
            out_size_id = Shader.PropertyToID("out_size");

            _rt = new RenderTexture(650, 650, 32, RenderTextureFormat.ARGB32);
            _rt.enableRandomWrite = true;
            _rt.Create();
        }

        private void OnDestroy()
        {
            _workerFaceDetection?.Dispose();
        }

        public void Run(RenderTexture texture)
        {
            // RGBA32 or ARGB32

            var roi = GetRoi(texture);
            var _ = PadRoi(128, 128, true, ref roi);
            var matrix = GetRotatedSubRectToRectTransformMatrix(roi, texture.width, texture.height, false);
            var input = TextureToTensor(texture, roi, new Size() { width = 128, height = 128 }, -1, 1, true);

            _workerFaceDetection.Execute(input);

            var output1 = _workerFaceDetection.PeekOutput("regressors");
            var output2 = _workerFaceDetection.PeekOutput("classificators");

            var boxes = (output1.data as ComputeTensorData).buffer;
            var scores = (output2.data as ComputeTensorData).buffer;

            var boxParam = new BoxDecoderParams
            {
                x_scale = 128f,
                y_scale = 128f,
                h_scale = 128f,
                w_scale = 128f,
                num_classes = 1,
                num_boxes = 896,
                num_coords = 16,
                reverse_output_order = true,
                apply_exponential = false,
                box_coord_offset = 0,
                num_keypoints = 6,
                keypt_coord_offset = 4,
                num_values_per_keypt = 2,
                apply_sigmoid = true,
                apply_clipping_thresh = true,
                clipping_thresh = 100f,
                ignore_class_0 = 0,
                min_score_thresh = .5f,
            };
            // var detections = boxDecoder.DecodeGPU(boxes, scores, boxParam);
            var detections = boxDecoder.DecodeCPU(output1.ToReadOnlyArray(), output2.ToReadOnlyArray(), boxParam);

            var filterParam = new NonMaxSuppressionParams
            {
                min_suppression_threshold = 0.3f,
                overlap_type = OverlapType.INTERSECTION_OVER_UNION,
                algorithm = AlgorithmType.WEIGHTED,
            };
            var filteredDetections = _nonMaxSuppressionFilter.Filter(filterParam, detections);
            filteredDetections = ProjectDetections(matrix, filteredDetections);

            input.Dispose();
            output1.Dispose();
            output2.Dispose();

            if (filteredDetections.Count > 0)
            {
                var normalizedRect = DetectionsToRect(texture, filteredDetections);
                TransformNormalizedRect(texture, normalizedRect, 1.5f, 1.5f);

                _latest = filteredDetections[0];
                UpdateFaceLandmarks(texture, normalizedRect);
            }
        }

        private void UpdateFaceLandmarks(RenderTexture texture, NormalizedRect rect)
        {
            var roi = GetRoi(texture, rect);
            var _ = PadRoi(192, 192, false, ref roi); // Can be removed
            var input = TextureToTensor(texture, roi, new Size() { width = 192, height = 192 }, 0, 1, false);

            var tex = new Texture2D(192, 192, TextureFormat.RGB24, false);
            RenderTexture.active = _rt;
            tex.ReadPixels(new UnityEngine.Rect(0, 0, _rt.width, _rt.height), 0, 0);
            tex.Apply();

            _spriteRenderer.sprite = Sprite.Create(tex, new UnityEngine.Rect(0, 0, tex.width, tex.height), new Vector2(0, 0), 1);

            //_workerFaceLandmark.Execute(input);

            //var output = _workerFaceDetection.PeekOutput("conv2d_21");
            //var arr = output.ToReadOnlyArray();

            input.Dispose();
        }

        private RotatedRect GetRoi(Texture input, NormalizedRect norm_rect = null)
        {
            if (norm_rect != null)
            {
                return new RotatedRect()
                {
                    center_x = norm_rect.XCenter * input.width,
                    center_y = norm_rect.YCenter * input.height,
                    width = norm_rect.Width * input.width,
                    height = norm_rect.Height * input.height,
                    rotation = norm_rect.Rotation,
                };
            }

            return new RotatedRect()
            {
                center_x = .5f * input.width,
                center_y = .5f * input.height,
                width = input.width,
                height = input.height,
                rotation = 0,
            };
        }

        private float[] PadRoi(int input_tensor_width, int input_tensor_height, bool keep_aspect_ratio, ref RotatedRect roi)
        {
            if (!keep_aspect_ratio)
            {
                return new float[] { 0.0f, 0.0f, 0.0f, 0.0f };
            }

            float tensor_aspect_ratio = (float)input_tensor_height / input_tensor_width;
            float roi_aspect_ratio = roi.height / roi.width;

            float vertical_padding = 0.0f;
            float horizontal_padding = 0.0f;
            float new_width;
            float new_height;

            if (tensor_aspect_ratio > roi_aspect_ratio)
            {
                new_width = roi.width;
                new_height = roi.width * tensor_aspect_ratio;
                vertical_padding = (1.0f - roi_aspect_ratio / tensor_aspect_ratio) / 2.0f;
            }
            else
            {
                new_width = roi.height / tensor_aspect_ratio;
                new_height = roi.height;
                horizontal_padding = (1.0f - tensor_aspect_ratio / roi_aspect_ratio) / 2.0f;
            }

            roi.width = new_width;
            roi.height = new_height;

            return new float[] { horizontal_padding, vertical_padding, horizontal_padding, vertical_padding };
        }

        private Tensor TextureToTensor(Texture input, RotatedRect roi, Size output_dims, float range_min, float range_max, bool zeroBorder)
        {
            const int kNumChannels = 3;
            var tensor = new Tensor(1, output_dims.height, output_dims.width, kNumChannels);
            var tensorData = new ComputeTensorData(tensor.shape, "_textureToTensor", ComputeInfo.channelsOrder, false);

            const float kInputImageRangeMin = 0.0f;
            const float kInputImageRangeMax = 1.0f;

            var transform = GetValueRangeTransformation(kInputImageRangeMin, kInputImageRangeMax, range_min, range_max);
            ExtractSubRectToBuffer(input, new Size(input.height, input.width), roi, false, transform.scale, transform.offset, new Size(output_dims.height, output_dims.width), zeroBorder, tensorData.buffer);

            tensor.AttachToDevice(tensorData);

            return tensor;
        }

        private ValueTransformation GetValueRangeTransformation(float from_range_min, float from_range_max, float to_range_min, float to_range_max)
        {
            float scale = (to_range_max - to_range_min) / (from_range_max - from_range_min);
            float offset = to_range_min - from_range_min * scale;
            return new ValueTransformation { scale = scale, offset = offset };
        }

        private void ExtractSubRectToBuffer(Texture texture, Size texture_size, RotatedRect texture_sub_rect, bool flip_horizontaly, float alpha, float beta, Size destination_size, bool zeroBorder, ComputeBuffer buffer)
        {
            var transform_mat = GetRotatedSubRectToRectTransformMatrix(texture_sub_rect, texture_size.width, texture_size.height, flip_horizontaly);
            var kernalId = zeroBorder ? main_zero_border_id : main_repeat_id;

            image_to_tensor_compute_shader.SetTexture(kernalId, input_data_id, texture);
            image_to_tensor_compute_shader.SetBuffer(kernalId, output_data_id, buffer);
            image_to_tensor_compute_shader.SetMatrix(transform_matrix_id, transform_mat);
            image_to_tensor_compute_shader.SetInts(out_size_id, new[] { destination_size.width, destination_size.height });
            image_to_tensor_compute_shader.SetFloat(alpha_id, alpha);
            image_to_tensor_compute_shader.SetFloat(beta_id, beta);

            image_to_tensor_compute_shader.SetTexture(kernalId, Shader.PropertyToID("output_texture"), _rt);

            int groups = Mathf.CeilToInt(destination_size.width / 8f);
            image_to_tensor_compute_shader.Dispatch(kernalId, groups, groups, 1);
        }

        private Matrix4x4 GetRotatedSubRectToRectTransformMatrix(RotatedRect sub_rect, int rect_width, int rect_height, bool flip_horizontaly)
        {
            var matrix = new Matrix4x4();

            float a = sub_rect.width;
            float b = sub_rect.height;

            float flip = flip_horizontaly ? -1 : 1;

            float c = Mathf.Cos(sub_rect.rotation);
            float d = Mathf.Sin(sub_rect.rotation);

            float e = sub_rect.center_x;
            float f = sub_rect.center_y;

            float g = 1.0f / rect_width;
            float h = 1.0f / rect_height;

            matrix[0, 0] = a * c * flip * g;
            matrix[0, 1] = -b * d * g;
            matrix[0, 2] = 0.0f;
            matrix[0, 3] = (-0.5f * a * c * flip + 0.5f * b * d + e) * g;

            matrix[1, 0] = a * d * flip * h;
            matrix[1, 1] = b * c * h;
            matrix[1, 2] = 0.0f;
            matrix[1, 3] = (-0.5f * b * c - 0.5f * a * d * flip + f) * h;

            matrix[2, 0] = 0.0f;
            matrix[2, 1] = 0.0f;
            matrix[2, 2] = a * g;
            matrix[2, 3] = 0.0f;

            matrix[3, 0] = 0.0f;
            matrix[3, 1] = 0.0f;
            matrix[3, 2] = 0.0f;
            matrix[3, 3] = 1.0f;

            return matrix;
        }

        private List<BoxDecoder.Detection> ProjectDetections(Matrix4x4 transform_matrix, List<BoxDecoder.Detection> detections)
        {
            Vector2 Project(Vector2 p)
            {
                var a = transform_matrix[1, 0];
                var b = transform_matrix[1, 1];
                var c = transform_matrix[1, 3];

                return new Vector2(
                    p.x * transform_matrix[0, 0] + p.y * transform_matrix[0, 1] + transform_matrix[0, 3],
                    p.x * transform_matrix[1, 0] + p.y * transform_matrix[1, 1] + transform_matrix[1, 3]);
            }

            foreach (var detection in detections)
            {
                var locationData = detection.location_data;

                // Project keypoints
                for (int i = 0; i < locationData.keypoints.Length; i++)
                {
                    var kpProject = Project(locationData.keypoints[i]);
                    locationData.keypoints[i].x = kpProject.x;
                    locationData.keypoints[i].y = kpProject.y;
                }

                // Project bounding box.
                // a) Define and project box points.
                var bbox = locationData.relative_bounding_box;
                var boxCoords = new[] {
                    Project(new Vector2(bbox.xMin, bbox.yMin)),
                    Project(new Vector2(bbox.xMin + bbox.width, bbox.yMin)),
                    Project(new Vector2(bbox.xMin + bbox.width, bbox.yMin + bbox.height)),
                    Project(new Vector2(bbox.xMin, bbox.width + bbox.height)),
                };

                // b) Find new left top and right bottom points for a box which encompases non-projected (rotated) box.
                var kFloatMax = float.MaxValue;
                var kFloatMin = float.MinValue;
                var leftTop = new Vector2(kFloatMax, kFloatMax);
                var rightBottom = new Vector2(kFloatMin, kFloatMin);
                foreach (var coor in boxCoords)
                {
                    leftTop.x = Mathf.Min(leftTop.x, coor.x);
                    leftTop.y = Mathf.Min(leftTop.y, coor.y);
                    rightBottom.x = Mathf.Min(rightBottom.x, coor.x);
                    rightBottom.y = Mathf.Min(rightBottom.x, coor.y);
                }

                locationData.relative_bounding_box.xMin = leftTop.x;
                locationData.relative_bounding_box.yMin = leftTop.y;
                locationData.relative_bounding_box.width = rightBottom.x - leftTop.x;
                locationData.relative_bounding_box.height = rightBottom.y - leftTop.y;
            }

            return detections;
        }

        private NormalizedRect DetectionsToRect(Texture input, List<BoxDecoder.Detection> detections)
        {
            var detection = detections[0];
            var location_data = detection.location_data;
            var bbox = location_data.relative_bounding_box;
            var output_rect = new NormalizedRect();
            output_rect.XCenter = bbox.xMin + bbox.width / 2;
            output_rect.YCenter = bbox.yMin + bbox.height / 2;
            output_rect.Width = bbox.width;
            output_rect.Height = bbox.height;
            var rotation = ComputeRotation(input, detection);
            output_rect.Rotation = rotation;

            return output_rect;
        }

        private float NormalizeRadians(float angle)
        {
            return angle - 2 * Mathf.PI * Mathf.Floor((angle - (-Mathf.PI)) / (2 * Mathf.PI));
        }

        private float ComputeRotation(Texture input, BoxDecoder.Detection detection)
        {
            const int rotation_vector_start_keypoint_index = 0;  // Left eye.
            const int rotation_vector_end_keypoint_index = 1;  // Right eye.
            const int rotation_vector_target_angle_degrees = 0;

            var location_data = detection.location_data;
            var x0 = location_data.keypoints[rotation_vector_start_keypoint_index].x * input.width;
            var y0 = location_data.keypoints[rotation_vector_start_keypoint_index].y * input.height;
            var x1 = location_data.keypoints[rotation_vector_end_keypoint_index].x * input.width;
            var y1 = location_data.keypoints[rotation_vector_end_keypoint_index].y * input.height;

            return NormalizeRadians(rotation_vector_target_angle_degrees - Mathf.Atan2(-(y1 - y0), x1 - x0));
        }

        private void TransformNormalizedRect(Texture input, NormalizedRect rect, float scale_x, float scale_y)
        {
            var long_side = Mathf.Max(rect.Width * input.width, rect.Height * input.height);
            rect.Width = (long_side / input.width) * scale_x;
            rect.Height = (long_side / input.height) * scale_y;
        }
    }
}


