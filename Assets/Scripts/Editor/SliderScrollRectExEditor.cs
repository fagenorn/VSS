using UnityEditor;
using UnityEngine.UI;

namespace Assets.Scripts.UI.Extensions
{
    [CustomEditor(typeof(SliderScrollRectEx))]
    public class SliderScrollRectExEditor : UnityEditor.UI.ScrollRectEditor
    {
        public override void OnInspectorGUI()
        {

            SliderScrollRectEx component = (SliderScrollRectEx)target;

            base.OnInspectorGUI();

            component.Slider = (Slider)EditorGUILayout.ObjectField("Slider", component.Slider, typeof(Slider), true);
        }
    }
}
