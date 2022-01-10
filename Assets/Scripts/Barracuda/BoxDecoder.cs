using System.Collections.Generic;
using UnityEngine;
using static Assets.Scripts.Barracuda.SsdAnchorGenerator;

namespace Assets.Scripts.Barracuda
{
    public class BoxDecoder : MonoBehaviour
    {
        public struct BoxDecoderParams
        {
            public float x_scale;
            public float y_scale;
            public float h_scale;
            public float w_scale;
            public int num_classes;
            public int num_boxes;
            public int num_coords;
            public bool reverse_output_order;
            public bool apply_exponential;
            public int box_coord_offset;
            public int num_keypoints;
            public int keypt_coord_offset;
            public int num_values_per_keypt;
            public bool apply_sigmoid;
            public bool apply_clipping_thresh;
            public float clipping_thresh;
            public int ignore_class_0;
            public float min_score_thresh;
        }

        public struct Detection
        {
            public float score;
            public int label_id;
            public LocationData location_data;
        }

        public enum LocationFormat
        {
            // The full image. This is a handy format when one needs to refer to the
            // full image, e.g. one uses global image labels. No other fields need to
            // be populated.
            GLOBAL = 0,

            // A rectangle aka bounding box of an object. The field bounding_box must be
            // used to store the location data.
            BOUNDING_BOX = 1,

            // A rectangle aka bounding box of an object, defined in coordinates
            // normalized by the image dimensions. The field relative_bounding_box must
            // be used to store the location data.
            RELATIVE_BOUNDING_BOX = 2,

            // A foreground mask. The field mask must be used to store the location
            // data.
            MASK = 3,
        }

        public struct LocationData
        {
            public LocationFormat format;
            public Rect bounding_box;
            public Rect relative_bounding_box;
            public Vector2[] keypoints;
        }

        [SerializeField] ComputeShader decode_box_detection_compute_shader;
        private int _decode_box_detection_kernal_id;
        private int _raw_boxes_id;
        private int _raw_anchors_id;
        private int _boxes_id;
        private int _scale_id;
        private int _num_coords_id;
        private int _reverse_output_order_id;
        private int _apply_exponential_id;
        private int _box_coord_offset_id;
        private int _num_keypoints_id;
        private int _keypt_coord_offset_id;
        private int _num_values_per_keypt_id;

        [SerializeField] ComputeShader score_detection_compute_shader;
        private int _score_detection_kernal_id;
        private int _raw_scores_id;
        private int _scored_boxes_id;
        private int _num_classes_id;
        private int _apply_sigmoid_id;
        private int _apply_clipping_thresh_id;
        private int _clipping_thresh_id;
        private int _ignore_class_0_id;

        private SsdAnchorGenerator _anchorGenerator = new SsdAnchorGenerator();

        private ComputeBuffer _anchorsBuffer;

        private List<SddAnchor> _anchors;

        private ComputeBuffer _boxesBuffer;

        private ComputeBuffer _scoresBuffer;

        private List<Detection> _detections = new List<Detection>();

        private void Awake()
        {
            _decode_box_detection_kernal_id = decode_box_detection_compute_shader.FindKernel("main");
            _raw_boxes_id = Shader.PropertyToID("raw_boxes");
            _raw_anchors_id = Shader.PropertyToID("raw_anchors");
            _boxes_id = Shader.PropertyToID("boxes");
            _scale_id = Shader.PropertyToID("scale");
            _num_coords_id = Shader.PropertyToID("num_coords");
            _reverse_output_order_id = Shader.PropertyToID("reverse_output_order");
            _apply_exponential_id = Shader.PropertyToID("apply_exponential");
            _box_coord_offset_id = Shader.PropertyToID("box_coord_offset");
            _num_keypoints_id = Shader.PropertyToID("num_keypoints");
            _keypt_coord_offset_id = Shader.PropertyToID("keypt_coord_offset");
            _num_values_per_keypt_id = Shader.PropertyToID("num_values_per_keypt");

            _score_detection_kernal_id = score_detection_compute_shader.FindKernel("main");
            _raw_scores_id = Shader.PropertyToID("raw_scores");
            _scored_boxes_id = Shader.PropertyToID("scored_boxes");
            _num_classes_id = Shader.PropertyToID("num_classes");
            _apply_sigmoid_id = Shader.PropertyToID("apply_sigmoid");
            _apply_clipping_thresh_id = Shader.PropertyToID("apply_clipping_thresh");
            _clipping_thresh_id = Shader.PropertyToID("clipping_thresh");
            _ignore_class_0_id = Shader.PropertyToID("ignore_class_0");
        }

        private void OnEnable()
        {
            if (_anchorsBuffer == null)
            {
                var param = new AnchorParams
                {
                    num_layers = 4,
                    min_scale = 0.1484375f,
                    max_scale = 0.75f,
                    input_size_height = 128,
                    input_size_width = 128,
                    anchor_offset_x = .5f,
                    anchor_offset_y = .5f,
                    strides = new int[] { 8, 16, 16, 16 },
                    aspect_ratios = new float[] { 1f },
                    fixed_anchor_size = true,
                    interpolated_scale_aspect_ratio = 1f,
                };

                _anchorsBuffer = new ComputeBuffer(896, 4 * 4); // num_boxes
                _anchorGenerator.GenerateAnchors(param, _anchorsBuffer);
                _anchors = _anchorGenerator.GenerateAnchors(param);
            }

            if (_boxesBuffer == null)
            {
                _boxesBuffer = new ComputeBuffer(896 * 16, 1 * 4); // num_boxes * num_coords
            }

            if (_scoresBuffer == null)
            {
                _scoresBuffer = new ComputeBuffer(896 * 2, 1 * 4); // num_boxes * 2
            }
        }

        private void OnDisable()
        {
            if (_anchorsBuffer != null)
            {
                _anchorsBuffer.Release();
                _anchorsBuffer = null;
            }

            if (_boxesBuffer != null)
            {
                _boxesBuffer.Release();
                _boxesBuffer = null;
            }

            if (_scoresBuffer != null)
            {
                _scoresBuffer.Release();
                _scoresBuffer = null;
            }
        }

        public List<Detection> DecodeGPU(ComputeBuffer raw_boxes, ComputeBuffer raw_scores, BoxDecoderParams param)
        {
            DecodeBoxesGPU(param, raw_boxes, _anchorsBuffer, _boxesBuffer);
            ScoreBoxesGPU(param, raw_scores, _scoresBuffer);

            var detection_scores = new float[param.num_boxes];
            var detection_classes = new int[param.num_boxes];


            var scoreClassIdPairs = new float[_scoresBuffer.count];
            _scoresBuffer.GetData(scoreClassIdPairs);

            for (int i = 0; i < param.num_boxes; i++)
            {
                detection_scores[i] = scoreClassIdPairs[i * 2];
                detection_classes[i] = (int)scoreClassIdPairs[i * 2 + 1];
            }

            var boxes = new float[_boxesBuffer.count];
            _boxesBuffer.GetData(boxes);

            return ConvertToDetections(param, boxes, detection_scores, detection_classes);
        }

        public List<Detection> DecodeCPU(float[] raw_boxes, float[] raw_scores, BoxDecoderParams param)
        {
            var boxes = DecodeBoxesCPU(param, raw_boxes, _anchors);
            var (detection_scores, detection_classes) = ScoreBoxesCPU(param, raw_scores);

            return ConvertToDetections(param, boxes, detection_scores, detection_classes);
        }

        private void DecodeBoxesGPU(BoxDecoderParams param, ComputeBuffer rawBoxes, ComputeBuffer rawAnchors, ComputeBuffer outputBuffer)
        {
            decode_box_detection_compute_shader.SetFloats(_scale_id, new[] { param.x_scale, param.y_scale, param.h_scale, param.w_scale });
            decode_box_detection_compute_shader.SetInt(_num_coords_id, param.num_coords);
            decode_box_detection_compute_shader.SetInt(_reverse_output_order_id, param.reverse_output_order ? 1 : 0);
            decode_box_detection_compute_shader.SetInt(_apply_exponential_id, param.apply_exponential ? 1 : 0);
            decode_box_detection_compute_shader.SetInt(_box_coord_offset_id, param.box_coord_offset);
            decode_box_detection_compute_shader.SetInt(_num_keypoints_id, param.num_keypoints);
            decode_box_detection_compute_shader.SetInt(_keypt_coord_offset_id, param.keypt_coord_offset);
            decode_box_detection_compute_shader.SetInt(_num_values_per_keypt_id, param.num_values_per_keypt);

            decode_box_detection_compute_shader.SetBuffer(_decode_box_detection_kernal_id, _raw_boxes_id, rawBoxes);
            decode_box_detection_compute_shader.SetBuffer(_decode_box_detection_kernal_id, _raw_anchors_id, rawAnchors);
            decode_box_detection_compute_shader.SetBuffer(_decode_box_detection_kernal_id, _boxes_id, outputBuffer);

            int groups = Mathf.CeilToInt(param.num_boxes);
            decode_box_detection_compute_shader.Dispatch(_decode_box_detection_kernal_id, groups, 1, 1);
        }

        private void ScoreBoxesGPU(BoxDecoderParams param, ComputeBuffer rawScores, ComputeBuffer outputBuffer)
        {
            score_detection_compute_shader.SetInt(_num_classes_id, param.num_classes);
            score_detection_compute_shader.SetInt(_apply_sigmoid_id, param.apply_sigmoid ? 0 : 1);
            score_detection_compute_shader.SetInt(_apply_clipping_thresh_id, param.apply_clipping_thresh ? 1 : 0);
            score_detection_compute_shader.SetFloat(_clipping_thresh_id, param.clipping_thresh);
            score_detection_compute_shader.SetInt(_ignore_class_0_id, param.ignore_class_0);

            score_detection_compute_shader.SetBuffer(_score_detection_kernal_id, _raw_scores_id, rawScores);
            score_detection_compute_shader.SetBuffer(_score_detection_kernal_id, _scored_boxes_id, outputBuffer);

            // 98304
            // int groups = Mathf.CeilToInt(param.num_boxes / 64f);
            score_detection_compute_shader.Dispatch(_score_detection_kernal_id, param.num_boxes, 1, 1);
        }

        private float[] DecodeBoxesCPU(BoxDecoderParams param, float[] raw_boxes, List<SddAnchor> anchors)
        {
            var boxes = new float[param.num_boxes * param.num_coords];

            for (int i = 0; i < param.num_boxes; i++)
            {
                var box_offset = i * param.num_coords + param.box_coord_offset;

                var y_center = raw_boxes[box_offset + 0];
                var x_center = raw_boxes[box_offset + 1];
                var h = raw_boxes[box_offset + 2];
                var w = raw_boxes[box_offset + 3];

                if (param.reverse_output_order)
                {
                    x_center = raw_boxes[box_offset + 0];
                    y_center = raw_boxes[box_offset + 1];
                    w = raw_boxes[box_offset + 2];
                    h = raw_boxes[box_offset + 3];
                }

                x_center = x_center / param.x_scale * anchors[i].w + anchors[i].x_center;
                y_center = y_center / param.y_scale * anchors[i].h + anchors[i].y_center;

                h = h / param.h_scale * anchors[i].h;
                w = w / param.w_scale * anchors[i].w;

                var ymin = y_center - h / 2f;
                var xmin = x_center - w / 2f;
                var ymax = y_center + h / 2f;
                var xmax = x_center + w / 2f;

                boxes[i * param.num_coords + 0] = ymin;
                boxes[i * param.num_coords + 1] = xmin;
                boxes[i * param.num_coords + 2] = ymax;
                boxes[i * param.num_coords + 3] = xmax;

                for (int k = 0; k < param.num_keypoints; k++)
                {
                    var offset = i * param.num_coords + param.keypt_coord_offset + k * param.num_values_per_keypt;
                    var keypoint_y = raw_boxes[offset + 0];
                    var keypoint_x = raw_boxes[offset + 1];

                    if (param.reverse_output_order)
                    {
                        keypoint_x = raw_boxes[offset + 0];
                        keypoint_y = raw_boxes[offset + 1];
                    }

                    boxes[offset + 0] = keypoint_x / param.x_scale * anchors[i].w + anchors[i].x_center;
                    boxes[offset + 1] = keypoint_y / param.y_scale * anchors[i].h + anchors[i].y_center;
                }
            }

            return boxes;
        }

        private (float[] detection_scores, int[] detection_classes) ScoreBoxesCPU(BoxDecoderParams param, float[] raw_scores)
        {
            var detection_scores = new float[param.num_boxes];
            var detection_classes = new int[param.num_boxes];

            for (int i = 0; i < param.num_boxes; i++)
            {
                var classId = -1;
                var maxScore = float.MinValue;

                for (int scoreId = 0; scoreId < param.num_classes; scoreId++)
                {
                    var score = raw_scores[i * param.num_classes + scoreId];
                    if (param.apply_sigmoid)
                    {
                        score = score < -param.clipping_thresh ? -param.clipping_thresh : score;
                        score = score > param.clipping_thresh ? param.clipping_thresh : score;
                    }

                    score = 1f / (1f + Mathf.Exp(-score));

                    if (maxScore < score)
                    {
                        maxScore = score;
                        classId = scoreId;
                    }
                }


                detection_scores[i] = maxScore;
                detection_classes[i] = classId;
            }

            return (detection_scores, detection_classes);
        }

        private List<Detection> ConvertToDetections(BoxDecoderParams param, float[] detection_boxes, float[] detection_scores, int[] detection_classes)
        {
            _detections.Clear();

            for (int i = 0; i < param.num_boxes; i++)
            {
                if (detection_scores[i] < param.min_score_thresh)
                {
                    continue;
                }

                var boxOffset = i * param.num_coords;
                var box_ymin = detection_boxes[boxOffset + 0];
                var box_xmin = detection_boxes[boxOffset + 1];
                var box_ymax = detection_boxes[boxOffset + 2];
                var box_xmax = detection_boxes[boxOffset + 3];
                var score = detection_scores[i];
                var class_id = detection_classes[i];
                var flip_vertically = false;

                var detection = ConvertToDetection(box_ymin, box_xmin, box_ymax, box_xmax, score, class_id, flip_vertically);
                var bbox = detection.location_data.relative_bounding_box;
                if (bbox.width < 0 || bbox.height < 0)
                {
                    continue;
                }

                detection.location_data.keypoints = new Vector2[param.num_keypoints * param.num_values_per_keypt];

                var index = 0;
                for (int kp_id = 0; kp_id < param.num_keypoints * param.num_values_per_keypt; kp_id += param.num_values_per_keypt)
                {
                    var keypoint_index = boxOffset + param.keypt_coord_offset + kp_id;
                    var kp = new Vector2();
                    kp.x = detection_boxes[keypoint_index + 0];
                    kp.y = flip_vertically ? 1f - detection_boxes[keypoint_index + 1] : detection_boxes[keypoint_index + 1];
                    detection.location_data.keypoints[index] = kp;
                    index++;
                }

                _detections.Add(detection);
            }

            return _detections;
        }

        private Detection ConvertToDetection(float box_ymin, float box_xmin, float box_ymax, float box_xmax, float score, int class_id, bool flip_vertically)
        {
            var relativeBbox = new Rect();
            relativeBbox.xMin = box_xmin;
            relativeBbox.yMin = flip_vertically ? 1f - box_ymax : box_ymin;
            relativeBbox.width = box_xmax - box_xmin;
            relativeBbox.height = box_ymax - box_ymin;

            var locationData = new LocationData();
            locationData.format = LocationFormat.RELATIVE_BOUNDING_BOX;
            locationData.relative_bounding_box = relativeBbox;

            var detection = new Detection();
            detection.score = score;
            detection.label_id = class_id;
            detection.location_data = locationData;

            return detection;
        }
    }
}
