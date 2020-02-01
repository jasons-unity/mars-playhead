using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Editor window for interacting with the EntityFromDataModule
    /// </summary>
    public class CreateEntityFromDataWindow : EditorWindow
    {
        static readonly GUIContent k_CreateWindowTitle = new GUIContent("Create Proxy with Conditions");
        static readonly Vector2 k_WindowSize = new Vector2(500, 200);

        public EntityFromDataModule module { get; set; }

        public Action create { private get; set; }

        public Action cancel { private get; set; }

        bool m_Canceled;
        Vector2 m_ScrollPosition;

        void OnEnable()
        {
            titleContent = k_CreateWindowTitle;
            m_Canceled = true; // Set this true so that closing the window via standard X will be considered a cancel

            var windowPosition = new Vector2(Screen.width / 2, Screen.height / 2);
            position = new Rect(windowPosition, k_WindowSize);
            minSize = k_WindowSize;

            EditorEvents.WindowUsed.Send(new UiComponentArgs { label = k_CreateWindowTitle.text, active = true });
        }

        void OnGUI()
        {
            if (module == null)
            {
                Close();
                return;
            }

            EditorGUILayout.BeginVertical();
            using (var scrollScope = new EditorGUILayout.ScrollViewScope(m_ScrollPosition))
            {
                m_ScrollPosition = scrollScope.scrollPosition;
                foreach (var kvp in module.potentialEntities)
                {
                    if (module.potentialEntities.Count > 1)
                        GUILayout.Label(kvp.Key.name, EditorStyles.boldLabel);

                    var potentialConditions = kvp.Value;

                    for (var i = 0; i < potentialConditions.Count; i++)
                    {
                        var potentialCondition = potentialConditions[i];
                        var conditionName = potentialCondition.conditionCreator.ConditionName;
                        try
                        {
                            var valueString = potentialCondition.conditionCreator.ValueString;
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                potentialCondition.use = EditorGUILayout.ToggleLeft(conditionName, potentialCondition.use, EditorStyles.label);
                                GUILayout.FlexibleSpace();

                                using (new EditorGUI.DisabledScope(!potentialCondition.use))
                                {
                                    GUILayout.Label(valueString);
                                }

                                potentialConditions[i] = potentialCondition;
                            }
                        }
                        catch (Exception e)
                        {
                            potentialCondition.use = false;
                            GUILayout.Label(string.Format("Error: {0} unable to determine value, cannot add {1} condition.",
                                ((Component)potentialCondition.conditionCreator).GetType().Name,
                                conditionName));

                            Debug.LogErrorFormat("{0}\n{1}", e.Message, e.StackTrace);
                        }
                    }

                    if (module.potentialEntities.Count > 1)
                        EditorGUILayout.Separator();
                }
            }
            GUILayout.FlexibleSpace();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Cancel"))
                    Close();

                if (GUILayout.Button("Create"))
                {
                    if (create != null)
                        create();

                    m_Canceled = false; // Track this so that cancel event is not fired when window is destroyed.
                    Close();
                }
            }
            EditorGUILayout.EndVertical();
        }

        void OnDestroy()
        {
            if (m_Canceled && cancel != null)
                cancel();

            EditorEvents.WindowUsed.Send(new UiComponentArgs { label = k_CreateWindowTitle.text, active = false });
        }
    }
}
