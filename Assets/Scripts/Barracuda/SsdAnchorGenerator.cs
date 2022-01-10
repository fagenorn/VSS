using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Barracuda;
using UnityEngine;

namespace Assets.Scripts.Barracuda
{
    public class SsdAnchorGenerator
    {
        public struct AnchorParams
        {
            public int num_layers;
            public float min_scale;
            public float max_scale;
            public int input_size_height;
            public int input_size_width;
            public float anchor_offset_x;
            public float anchor_offset_y;
            public int[] strides;
            public float[] aspect_ratios;
            public bool fixed_anchor_size;
            public bool reduce_boxes_in_lowest_layer;
            public float interpolated_scale_aspect_ratio;
        }

        public struct SddAnchor
        {
            public float x_center;
            public float y_center;
            public float h;
            public float w;
        }

        public void GenerateAnchors(AnchorParams param, ComputeBuffer output)
        {
            var anchors = Flatten(GenerateAnchorsInternal(param));
            output.SetData(anchors);
        }

        public List<SddAnchor> GenerateAnchors(AnchorParams param)
        {
            return GenerateAnchorsInternal(param);
        }

        private float[] Flatten(List<SddAnchor> anchors)
        {
            var flattened = new float[anchors.Count * 4];
            var index = 0;
            for (int i = 0; i < anchors.Count; i++)
            {
                var anchor = anchors[i];
                flattened[index + 0] = anchor.y_center;
                flattened[index + 1] = anchor.x_center;
                flattened[index + 2] = anchor.h;
                flattened[index + 3] = anchor.w;
                index += 4;
            }

            return flattened;
        }

        private float CalculateScale(float min_scale, float max_scale, int stride_index, int num_strides)
        {
            if (num_strides == 1)
            {
                return (min_scale + max_scale) * .5f;
            }
            else
            {
                return min_scale + (max_scale - min_scale) * 1f * stride_index / (num_strides - 1f);
            }
        }

        private List<SddAnchor> GenerateAnchorsInternal(AnchorParams param)
        {
            List<SddAnchor> anchors = new List<SddAnchor>();

            int layer_id = 0;
            while (layer_id < param.num_layers)
            {
                List<float> anchor_height = new List<float>();
                List<float> anchor_width = new List<float>();
                List<float> aspect_ratios = new List<float>();
                List<float> scales = new List<float>();

                int last_same_stride_layer = layer_id;
                while (last_same_stride_layer < param.strides.Length && param.strides[last_same_stride_layer] == param.strides[layer_id])
                {
                    var scale = CalculateScale(param.min_scale, param.max_scale, last_same_stride_layer, param.strides.Length);
                    if (last_same_stride_layer == 0 && param.reduce_boxes_in_lowest_layer)
                    {
                        aspect_ratios.Add(1f);
                        aspect_ratios.Add(2f);
                        aspect_ratios.Add(.5f);
                        scales.Add(.1f);
                        scales.Add(scale);
                        scales.Add(scale);
                    }
                    else
                    {
                        for (int aspect_ratio_id = 0; aspect_ratio_id < param.aspect_ratios.Length; aspect_ratio_id++)
                        {
                            aspect_ratios.Add(param.aspect_ratios[aspect_ratio_id]);
                            scales.Add(scale);
                        }

                        if (param.interpolated_scale_aspect_ratio > 0)
                        {
                            var scale_next = last_same_stride_layer == param.strides.Length - 1 ? 1f : CalculateScale(param.min_scale, param.max_scale, last_same_stride_layer + 1, param.strides.Length);
                            scales.Add(Mathf.Sqrt(scale * scale_next));
                            aspect_ratios.Add(param.interpolated_scale_aspect_ratio);
                        }
                    }
                    last_same_stride_layer++;
                }

                for (int i = 0; i < aspect_ratios.Count; i++)
                {
                    var ratio_sqrts = Mathf.Sqrt(aspect_ratios[i]);
                    anchor_height.Add(scales[i] / ratio_sqrts);
                    anchor_width.Add(scales[i] * ratio_sqrts);
                }

                int stride = param.strides[layer_id];
                int feature_map_height = (int)Mathf.Ceil(1.0f * param.input_size_height / stride);
                int feature_map_width = (int)Mathf.Ceil(1.0f * param.input_size_width / stride);

                for (int y = 0; y < feature_map_height; y++)
                {
                    for (int x = 0; x < feature_map_width; x++)
                    {
                        for (int anchor_id = 0; anchor_id < anchor_height.Count; anchor_id++)
                        {
                            var x_center = (x + param.anchor_offset_x) * 1f / feature_map_width;
                            var y_center = (y + param.anchor_offset_y) * 1f / feature_map_height;

                            var anchor = new SddAnchor();
                            anchor.x_center = x_center;
                            anchor.y_center = y_center;

                            if (param.fixed_anchor_size)
                            {
                                anchor.w = 1f;
                                anchor.h = 1f;
                            }
                            else
                            {
                                anchor.w = anchor_width[anchor_id];
                                anchor.h = anchor_height[anchor_id];
                            }

                            anchors.Add(anchor);
                        }
                    }
                }

                layer_id = last_same_stride_layer;
            }

            return anchors;
        }
    }
}
