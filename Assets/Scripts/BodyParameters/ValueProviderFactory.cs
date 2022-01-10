using Assets.Scripts.Live2D;
using Assets.Scripts.Models;
using Live2D.Cubism.Core;
using System.Collections.Generic;

namespace Assets.Scripts.BodyParameters
{
    internal static class ValueProviderFactory
    {
        private struct BodyTrackerDefaultData
        {
            public BodyParam BodyParam;
            public string Live2DId;
            public string Name;
            public float? InputMin;
            public float? InputMax;
            public float? OutputMin;
            public float? OutputMax;
            public float? SmoothTime;
            public float? Sensitivity;
        }

        private static readonly List<BodyTrackerDefaultData> _bodyParamToLive2DMap = new List<BodyTrackerDefaultData>
        {
            new BodyTrackerDefaultData()
            {
                Name = "Head Yaw",
                BodyParam = BodyParam.FaceAngleX,
                Live2DId = "ParamAngleX"
            },
            new BodyTrackerDefaultData()
            {
                Name = "Head Yaw",
                BodyParam = BodyParam.FaceAngleX,
                Live2DId = "PARAM_ANGLE_X"
            },
            new BodyTrackerDefaultData()
            {
                Name = "Head Pitch",
                BodyParam = BodyParam.FaceAngleY,
                Live2DId = "ParamAngleY"
            },
            new BodyTrackerDefaultData()
            {
                Name = "Head Pitch",
                BodyParam = BodyParam.FaceAngleY,
                Live2DId = "PARAM_ANGLE_Y"
            },
            new BodyTrackerDefaultData()
            {
                Name = "Head Roll",
                BodyParam = BodyParam.FaceAngleZ,
                Live2DId = "ParamAngleZ"
            },
            new BodyTrackerDefaultData()
            {
                Name = "Head Roll",
                BodyParam = BodyParam.FaceAngleZ,
                Live2DId = "PARAM_ANGLE_Z"
            },
            new BodyTrackerDefaultData()
            {
                Name = "Left Eye Open",
                BodyParam = BodyParam.EyeLOpen,
                Live2DId = "ParamEyeLOpen",
                SmoothTime = .05f,
                Sensitivity = 0.75f,
            },
            new BodyTrackerDefaultData()
            {
                Name = "Left Eye Open",
                BodyParam = BodyParam.EyeLOpen,
                Live2DId = "PARAM_EYE_L_OPEN",
                SmoothTime = .05f,
                Sensitivity = 0.75f,
            },
            new BodyTrackerDefaultData()
            {
                Name = "Right Eye Open",
                BodyParam = BodyParam.EyeROpen,
                Live2DId = "ParamEyeROpen",
                SmoothTime = .05f,
                Sensitivity = 0.75f,
            },
            new BodyTrackerDefaultData()
            {
                Name = "Right Eye Open",
                BodyParam = BodyParam.EyeROpen,
                Live2DId = "PARAM_EYE_R_OPEN",
                SmoothTime = .05f,
                Sensitivity = 0.75f,
            },
            new BodyTrackerDefaultData()
            {
                Name = "Iris X Position",
                BodyParam = BodyParam.EyeLX,
                Live2DId = "ParamEyeBallX"
            },
            new BodyTrackerDefaultData()
            {
                Name = "Iris X Position",
                BodyParam = BodyParam.EyeLX,
                Live2DId = "PARAM_EYE_BALL_X"
            },
            new BodyTrackerDefaultData()
            {
                Name = "Iris Y Position",
                BodyParam = BodyParam.EyeLY,
                Live2DId = "ParamEyeBallY"
            },
            new BodyTrackerDefaultData()
            {
                Name = "Iris Y Position",
                BodyParam = BodyParam.EyeLY,
                Live2DId = "PARAM_EYE_BALL_Y"
            },
            new BodyTrackerDefaultData()
            {
                Name = "Brow Left Y Position",
                BodyParam = BodyParam.BrowLY,
                Live2DId = "ParamBrowLY"
            },
            new BodyTrackerDefaultData()
            {
                Name = "Brow Left Y Position",
                BodyParam = BodyParam.BrowLY,
                Live2DId = "PARAM_BROW_L_Y"
            },
            new BodyTrackerDefaultData()
            {
                Name = "Brow Right Y Position",
                BodyParam = BodyParam.BrowRY,
                Live2DId = "ParamBrowRY"
            },
            new BodyTrackerDefaultData()
            {
                Name = "Brow Right Y Position",
                BodyParam = BodyParam.BrowRY,
                Live2DId = "PARAM_BROW_R_Y"
            },
            new BodyTrackerDefaultData()
            {
                Name = "Mouth Open",
                BodyParam = BodyParam.MouthOpen,
                Live2DId = "ParamMouthOpen",
                SmoothTime = 0.03f,
                Sensitivity = 0.2f,
            },
            new BodyTrackerDefaultData()
            {
                Name = "Mouth Open",
                BodyParam = BodyParam.MouthOpen,
                Live2DId = "ParamMouthOpenY",
                SmoothTime = 0.03f,
                Sensitivity = 0.2f,
            },
            new BodyTrackerDefaultData()
            {
                Name = "Mouth Open",
                BodyParam = BodyParam.MouthOpen,
                Live2DId = "PARAM_MOUTH_OPEN_Y",
                SmoothTime = 0.03f,
                Sensitivity = 0.2f,
            },
            new BodyTrackerDefaultData()
            {
                Name = "Mouth Smile",
                BodyParam = BodyParam.HappyEmotion,
                Live2DId = "ParamMouthForm",
                SmoothTime = 0.05f,
                OutputMin = 0f
            },
            new BodyTrackerDefaultData()
            {
                Name = "Mouth Smile",
                BodyParam = BodyParam.HappyEmotion,
                Live2DId = "PARAM_MOUTH_FORM",
                SmoothTime = 0.05f,
                OutputMin = 0f
            },
            new BodyTrackerDefaultData()
            {
                Name = "Mouth Smile",
                BodyParam = BodyParam.HappyEmotion,
                Live2DId = "ParamEyeLSmile",
            },
            new BodyTrackerDefaultData()
            {
                Name = "Mouth Smile",
                BodyParam = BodyParam.HappyEmotion,
                Live2DId = "PARAM_EYE_L_SMILE",
            },
            new BodyTrackerDefaultData()
            {
                Name = "Mouth Smile",
                BodyParam = BodyParam.HappyEmotion,
                Live2DId = "ParamEyeRSmile",
            },
            new BodyTrackerDefaultData()
            {
                Name = "Mouth Smile",
                BodyParam = BodyParam.HappyEmotion,
                Live2DId = "PARAM_EYE_R_SMILE",
            },

            new BodyTrackerDefaultData()
            {
                Name = "Body Yaw",
                BodyParam = BodyParam.BodyAngleX,
                Live2DId = "ParamBodyAngleX",
                SmoothTime = 0.2f,
                Sensitivity = 0.7f,
            },
            new BodyTrackerDefaultData()
            {
                Name = "Body Yaw",
                BodyParam = BodyParam.BodyAngleX,
                Live2DId = "PARAM_BODY_ANGLE_X",
                SmoothTime = 0.2f,
                Sensitivity = 0.7f,
            },
            new BodyTrackerDefaultData()
            {
                Name = "Body Pitch",
                BodyParam = BodyParam.BodyAngleY,
                Live2DId = "ParamBodyAngleY",
                SmoothTime = 0.2f,
            },
            new BodyTrackerDefaultData()
            {
                Name = "Body Pitch",
                BodyParam = BodyParam.BodyAngleY,
                Live2DId = "PARAM_BODY_ANGLE_Y",
                SmoothTime = 0.2f,
            },
            new BodyTrackerDefaultData()
            {
                Name = "Body Roll",
                BodyParam = BodyParam.BodyAngleZ,
                Live2DId = "ParamBodyAngleZ",
                SmoothTime = 0.2f,
                Sensitivity = 0.55f,
            },
            new BodyTrackerDefaultData()
            {
                Name = "Body Roll",
                BodyParam = BodyParam.BodyAngleZ,
                Live2DId = "PARAM_BODY_ANGLE_Z",
                SmoothTime = 0.2f,
                Sensitivity = 0.55f,
            },

            new BodyTrackerDefaultData()
            {
                Name = "Left Shoulder to Elbow Angle",
                BodyParam = BodyParam.ArmLAngle1,
                Live2DId = "ParamArmL1",
                SmoothTime = 0.2f,
            },
            new BodyTrackerDefaultData()
            {
                Name = "Left Shoulder to Elbow Angle",
                BodyParam = BodyParam.ArmLAngle1,
                Live2DId = "ParamArmLeftA",
                SmoothTime = 0.2f,
            },
            new BodyTrackerDefaultData()
            {
                Name = "Left Elbow to Wrist Angle",
                BodyParam = BodyParam.ArmLAngle2,
                Live2DId = "ParamArmL2",
                SmoothTime = 0.2f,
            },
            new BodyTrackerDefaultData()
            {
                Name = "Left Elbow to Wrist Angle",
                BodyParam = BodyParam.ArmLAngle2,
                Live2DId = "ParamArmLeftB",
                SmoothTime = 0.2f,
            },
            new BodyTrackerDefaultData()
            {
                Name = "Right Shoulder to Elbow Angle",
                BodyParam = BodyParam.ArmRAngle1,
                Live2DId = "ParamArmR1",
                SmoothTime = 0.2f,
            },
            new BodyTrackerDefaultData()
            {
                Name = "Right Shoulder to Elbow Angle",
                BodyParam = BodyParam.ArmRAngle1,
                Live2DId = "ParamArmRightA",
                SmoothTime = 0.2f,
            },
            new BodyTrackerDefaultData()
            {
                Name = "Right Elbow to Wrist Angle",
                BodyParam = BodyParam.ArmRAngle2,
                Live2DId = "ParamArmR2",
                SmoothTime = 0.2f,
            },
            new BodyTrackerDefaultData()
            {
                Name = "Right Elbow to Wrist Angle",
                BodyParam = BodyParam.ArmRAngle2,
                Live2DId = "ParamArmRightB",
                SmoothTime = 0.2f,
            },
            new BodyTrackerDefaultData()
            {
                Name = "Left Wrist Angle",
                BodyParam = BodyParam.WristLAngle,
                Live2DId = "ParamArmL3",
                SmoothTime = 0.2f,
            },
            new BodyTrackerDefaultData()
            {
                Name = "Right Wrist Angle",
                BodyParam = BodyParam.WristRAngle,
                Live2DId = "ParamArmR3",
                SmoothTime = 0.2f,
            },
        };

        public static List<BodyTrackerValueProvider> DefaultBodyTracker(BodyTracker bodyTracker, VSSModel model)
        {
            var parameters = new List<BodyTrackerValueProvider>();

            foreach (var data in _bodyParamToLive2DMap)
            {
                if (TryCreateBodyTrackerProvider(out var provider, data, bodyTracker, model))
                {
                    parameters.Add(provider);
                }
            }

            return parameters;
        }

        public static List<Live2DAnimationProvider> DefaultAnimation(VSSModel model)
        {
            var parameters = new List<Live2DAnimationProvider>();

            foreach (var key in model.VSSMotionDataDict.Keys)
            {
                if (key.ToLower().Contains("idle") && TryCreateAnimationProvider(out var provider, model, model.VSSMotionDataDict[key]))
                {
                    parameters.Add(provider);
                }
            }

            return parameters;
        }

        private static bool TryCreateAnimationProvider(out Live2DAnimationProvider provider, VSSModel model, VSSMotionData motionData)
        {
            provider = new Live2DAnimationProvider();

            provider.Priority = (int)Priorities.IdleAnimation;
            provider.Enabled = true;
            provider.HasHotkey = false;
            provider.Name = "Idle Animation";
            provider.MotionId = motionData.Name;
            provider.AnimationType = AnimationType.Looping;

            provider.Initialize(model);

            return true;
        }

        private static bool TryCreateBodyTrackerProvider(out BodyTrackerValueProvider provider, BodyTrackerDefaultData data, BodyTracker bodyTracker, VSSModel model)
        {
            provider = new BodyTrackerValueProvider();

            var bodyParameter = bodyTracker.GetBodyParameter(data.BodyParam);

            if (!model.Live2DParamDict.TryGetValue(data.Live2DId, out var live2DParameter) || bodyParameter == null) return false;

            provider.Live2DParameterId = data.Live2DId;
            provider.BodyParameterType = data.BodyParam;
            provider.Name = data.Name ?? "Body Parameter";
            provider.InputMin = data.InputMin ?? bodyParameter.DefaultMin;
            provider.InputMax = data.InputMax ?? bodyParameter.DefaultMax;
            provider.OutputMin = data.OutputMin ?? live2DParameter.MinimumValue;
            provider.OutputMax = data.OutputMax ?? live2DParameter.MaximumValue;
            provider.SmoothTime = data.SmoothTime ?? .1f;
            provider.Sensitivity = data.Sensitivity ?? 1f;

            provider.Initialize(bodyTracker, model);

            return true;
        }
    }
}
