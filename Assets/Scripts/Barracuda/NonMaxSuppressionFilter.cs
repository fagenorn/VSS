using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Assets.Scripts.Barracuda
{
    public class NonMaxSuppressionFilter
    {
        public enum OverlapType
        {
            JACCARD,
            MODIFIED_JACCARD,
            INTERSECTION_OVER_UNION
        }

        public enum AlgorithmType
        {
            WEIGHTED,
            OTHER
        }

        public struct NonMaxSuppressionParams
        {
            public float min_suppression_threshold;
            public int max_num_detections;
            public float min_score_threshold;
            public OverlapType overlap_type;
            public AlgorithmType algorithm;
        }

        private List<BoxDecoder.Detection> _detections = new List<BoxDecoder.Detection>();

        private List<BoxDecoder.LocationData> _retained_locations = new List<BoxDecoder.LocationData>();

        public List<BoxDecoder.Detection> Filter(NonMaxSuppressionParams param, List<BoxDecoder.Detection> detections)
        {
            _detections.Clear();

            if (detections == null || detections.Count == 0)
            {
                return _detections;
            }

            IEnumerable<BoxDecoder.Detection> query = detections.OrderByDescending(x => x.score);

            if (param.max_num_detections > 0)
            {
                query = query.Take(param.max_num_detections);
            }

            if (param.algorithm == AlgorithmType.WEIGHTED)
            {
                WeightedNonMaxSuppression(param, query);
            }
            else
            {
                throw new Exception("Not implemented");
            }

            return _detections;
        }

        private void WeightedNonMaxSuppression(NonMaxSuppressionParams param, IEnumerable<BoxDecoder.Detection> query)
        {
            _retained_locations.Clear();

            if (param.min_score_threshold > 0.001f)
            {
                query = query.Where(x => x.score > param.min_score_threshold);
            }

            foreach (var detection in query)
            {
                var location = detection.location_data;

                bool suppressed = false;
                foreach (var retainedLocation in _retained_locations)
                {
                    var similarity = OverlapSimilarity(param.overlap_type, retainedLocation.relative_bounding_box, location.relative_bounding_box);

                    if (similarity > param.min_suppression_threshold)
                    {
                        suppressed = true;
                        break;
                    }
                }

                if (!suppressed)
                {
                    _detections.Add(detection);
                    _retained_locations.Add(location);
                }

                if (_detections.Count >= param.max_num_detections)
                {
                    break;
                }
            }
        }

        private float OverlapSimilarity(OverlapType overlapType, UnityEngine.Rect rect1, UnityEngine.Rect rect2)
        {
            var r1 = new RectangleF(rect1.x, rect1.y, rect1.width, rect1.height);
            var r2 = new RectangleF(rect2.x, rect2.y, rect2.width, rect2.height);

            if (!r1.IntersectsWith(r2)) return 0f;
            var intersection = RectangleF.Intersect(r1, r2);
            var intersectionArea = intersection.Width * intersection.Height;

            float normalization;

            switch (overlapType)
            {
                case OverlapType.JACCARD:
                    var union = RectangleF.Union(r1, r2);
                    normalization = union.Width * union.Height;
                    break;
                case OverlapType.MODIFIED_JACCARD:
                    normalization = r2.Width * r2.Height;
                    break;
                case OverlapType.INTERSECTION_OVER_UNION:
                    normalization = (r1.Width * r1.Height) + (r2.Width * r2.Height) - intersectionArea;
                    break;
                default:
                    throw new Exception("Unrecognized overlap type");
            }

            return normalization > 0f ? intersectionArea / normalization : 0f;
        }
    }
}
