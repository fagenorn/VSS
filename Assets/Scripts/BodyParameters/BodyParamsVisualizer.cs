using Live2D.Cubism.Core;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.BodyParameters
{
    internal class BodyParamsVisualizer : MonoBehaviour
    {
        [SerializeField] private TMP_Text _preFab;

        private List<TMP_Text> _labels = new List<TMP_Text>();

        [SerializeField] private BodyTracker _bodyParameterAnalyzer;

        [SerializeField] private CubismModel _model;

        private void Start()
        {
            if (_model == null) return;

            var currentY = 0f;

            for (int i = 0; i < 20; i++)
            {
                var c = Instantiate(_preFab);
                c.transform.parent = gameObject.transform;
                c.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, currentY, 0);
                _labels.Add(c);

                currentY += -15f;
            }

            //var obj = gameObject.AddComponent<Live2DParameterMapper>();
            //obj.SetModel(_model);
            //obj.SetBodyParameter(_bodyParameterAnalyzer.FaceAngleX);
            //obj.SetLive2DParameter("ParamAngleX");

            //obj = gameObject.AddComponent<Live2DParameterMapper>();
            //obj.SetModel(_model);
            //obj.SetBodyParameter(_bodyParameterAnalyzer.FaceAngleY);
            //obj.SetLive2DParameter("ParamAngleY");

            //obj = gameObject.AddComponent<Live2DParameterMapper>();
            //obj.SetModel(_model);
            //obj.SetBodyParameter(_bodyParameterAnalyzer.FaceAngleZ);
            //obj.SetLive2DParameter("ParamAngleZ");

            //obj = gameObject.AddComponent<Live2DParameterMapper>();
            //obj.SetModel(_model);
            //obj.SetBodyParameter(_bodyParameterAnalyzer.EyeLOpen);
            //obj.SetLive2DParameter("ParamEyeLOpen");
            //obj.SetSmoothing(0.05f);
            //obj.SetOutput(0, 2f);

            //obj = gameObject.AddComponent<Live2DParameterMapper>();
            //obj.SetModel(_model);
            //obj.SetBodyParameter(_bodyParameterAnalyzer.EyeROpen);
            //obj.SetLive2DParameter("ParamEyeROpen");
            //obj.SetSmoothing(0.05f);
            //obj.SetOutput(0, 2f);

            //obj = gameObject.AddComponent<Live2DParameterMapper>();
            //obj.SetModel(_model);
            //obj.SetBodyParameter(_bodyParameterAnalyzer.EyeLX);
            //obj.SetLive2DParameter("ParamEyeBallX");

            //obj = gameObject.AddComponent<Live2DParameterMapper>();
            //obj.SetModel(_model);
            //obj.SetBodyParameter(_bodyParameterAnalyzer.EyeLY);
            //obj.SetLive2DParameter("ParamEyeBallY");

            //obj = gameObject.AddComponent<Live2DParameterMapper>();
            //obj.SetModel(_model);
            //obj.SetBodyParameter(_bodyParameterAnalyzer.MouthOpen);
            //obj.SetLive2DParameter("ParamMouthOpen");
            //obj.SetSmoothing(0);
            //obj.SetOutput(0, 2f);

            //obj = gameObject.AddComponent<Live2DParameterMapper>();
            //obj.SetModel(_model);
            //obj.SetBodyParameter(_bodyParameterAnalyzer.MouthOpen);
            //obj.SetLive2DParameter("ParamMouthOpenY");
            //obj.SetSmoothing(0);
            //obj.SetOutput(0, 2f);

            ///// ###

            //obj = gameObject.AddComponent<Live2DParameterMapper>();
            //obj.SetModel(_model);
            //obj.SetBodyParameter(_bodyParameterAnalyzer.BodyAngleX);
            //obj.SetLive2DParameter("ParamBodyAngleX");
            //obj.SetInput(-60f, 60f);

            //obj = gameObject.AddComponent<Live2DParameterMapper>();
            //obj.SetModel(_model);
            //obj.SetBodyParameter(_bodyParameterAnalyzer.BodyAngleY);
            //obj.SetLive2DParameter("ParamBodyAngleY");
            //obj.SetInput(-60f, 60f);

            //obj = gameObject.AddComponent<Live2DParameterMapper>();
            //obj.SetModel(_model);
            //obj.SetBodyParameter(_bodyParameterAnalyzer.BodyAngleZ);
            //obj.SetLive2DParameter("ParamBodyAngleZ");
            //obj.SetInput(-60f, 60f);

            //obj = gameObject.AddComponent<Live2DParameterMapper>();
            //obj.SetModel(_model);
            //obj.SetBodyParameter(_bodyParameterAnalyzer.ArmLAngle1);
            //obj.SetLive2DParameter("ParamArmL1");

            //obj = gameObject.AddComponent<Live2DParameterMapper>();
            //obj.SetModel(_model);
            //obj.SetBodyParameter(_bodyParameterAnalyzer.ArmLAngle1);
            //obj.SetLive2DParameter("ParamArmLeftA");

            //obj = gameObject.AddComponent<Live2DParameterMapper>();
            //obj.SetModel(_model);
            //obj.SetBodyParameter(_bodyParameterAnalyzer.ArmLAngle2);
            //obj.SetLive2DParameter("ParamArmL2");

            //obj = gameObject.AddComponent<Live2DParameterMapper>();
            //obj.SetModel(_model);
            //obj.SetBodyParameter(_bodyParameterAnalyzer.ArmLAngle2);
            //obj.SetLive2DParameter("ParamArmLeftB");

            //obj = gameObject.AddComponent<Live2DParameterMapper>();
            //obj.SetModel(_model);
            //obj.SetBodyParameter(_bodyParameterAnalyzer.ArmRAngle1);
            //obj.SetLive2DParameter("ParamArmR1");

            //obj = gameObject.AddComponent<Live2DParameterMapper>();
            //obj.SetModel(_model);
            //obj.SetBodyParameter(_bodyParameterAnalyzer.ArmRAngle1);
            //obj.SetLive2DParameter("ParamArmRightA");

            //obj = gameObject.AddComponent<Live2DParameterMapper>();
            //obj.SetModel(_model);
            //obj.SetBodyParameter(_bodyParameterAnalyzer.ArmRAngle2);
            //obj.SetLive2DParameter("ParamArmR2");

            //obj = gameObject.AddComponent<Live2DParameterMapper>();
            //obj.SetModel(_model);
            //obj.SetBodyParameter(_bodyParameterAnalyzer.ArmRAngle2);
            //obj.SetLive2DParameter("ParamArmRightB");

            //obj = gameObject.AddComponent<Live2DParameterMapper>();
            //obj.SetModel(_model);
            //obj.SetBodyParameter(_bodyParameterAnalyzer.WristLAngle);
            //obj.SetLive2DParameter("ParamArmL3");

            //obj = gameObject.AddComponent<Live2DParameterMapper>();
            //obj.SetModel(_model);
            //obj.SetBodyParameter(_bodyParameterAnalyzer.WristRAngle);
            //obj.SetLive2DParameter("ParamArmR3");
        }

        private void Update()
        {
            _labels[0].text = $"Face Angle X = {_bodyParameterAnalyzer.FaceAngleX:00.00}";
            _labels[1].text = $"Face Angle Y = {_bodyParameterAnalyzer.FaceAngleY:00.00}";
            _labels[2].text = $"Face Angle Z = {_bodyParameterAnalyzer.FaceAngleZ:00.00}";

            _labels[3].text = $"Eye L Open = {_bodyParameterAnalyzer.EyeLOpen:00.00}";
            _labels[4].text = $"Eye R Open = {_bodyParameterAnalyzer.EyeROpen:00.00}";

            _labels[5].text = $"Eye L X = {_bodyParameterAnalyzer.EyeLX:00.00}";
            _labels[6].text = $"Eye L Y = {_bodyParameterAnalyzer.EyeLY:00.00}";
            _labels[7].text = $"Eye R X = {_bodyParameterAnalyzer.EyeRX:00.00}";
            _labels[8].text = $"Eye R Y = {_bodyParameterAnalyzer.EyeRY:00.00}";

            _labels[9].text = $"Mouth Open = {_bodyParameterAnalyzer.MouthOpen:00.00}";

            /// ########

            _labels[10].text = $"Body Angle X = {_bodyParameterAnalyzer.BodyAngleX:00.00}";
            _labels[11].text = $"Body Angle Y = {_bodyParameterAnalyzer.BodyAngleY:00.00}";
            _labels[12].text = $"Body Angle Z = {_bodyParameterAnalyzer.BodyAngleZ:00.00}";

            _labels[13].text = $"Arm L Angle 1 = {_bodyParameterAnalyzer.ArmLAngle1:00.00}";
            _labels[14].text = $"Arm L Angle 2 = {_bodyParameterAnalyzer.ArmLAngle2:00.00}";

            _labels[15].text = $"Arm R Angle 1 = {_bodyParameterAnalyzer.ArmRAngle1:00.00}";
            _labels[16].text = $"Arm R Angle 2 = {_bodyParameterAnalyzer.ArmRAngle2:00.00}";

            /// ########

            _labels[17].text = $"Wrist L Angle = {_bodyParameterAnalyzer.WristLAngle:00.00}";
            _labels[18].text = $"Wrist R Angle = {_bodyParameterAnalyzer.WristRAngle:00.00}";
        }
    }
}
