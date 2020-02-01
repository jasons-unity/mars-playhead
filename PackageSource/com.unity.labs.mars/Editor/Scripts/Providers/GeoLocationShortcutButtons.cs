using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.MARS.Providers
{
    public static class GeoLocationShortcutButtons
    {
        struct ShortcutButton
        {
            public string name;
            public double latitude;
            public double longitude;
        }

        static ShortcutButton[] s_ShortcutButtons =
        {
            new ShortcutButton
            {
                name = "San Francisco",
                latitude = 37.787,
                longitude = -122.403
            },
            new ShortcutButton
            {
                name = "Redmond",
                latitude = 47.644,
                longitude = -122.139
            },
            new ShortcutButton
            {
                name = "Copenhagen",
                latitude = 55.68,
                longitude = 12.577
            },
            new ShortcutButton
            {
                name = "Tokyo",
                latitude = 35.669,
                longitude = 139.764
            }
        };

        public static void DrawShortcutButtons(string title, Action<double, double> shortcutAction)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

            using (new GUILayout.HorizontalScope())
            {
                foreach (var shortcutButton in s_ShortcutButtons)
                {
                    if (GUILayout.Button(shortcutButton.name))
                    {
                        shortcutAction(shortcutButton.latitude, shortcutButton.longitude);
                    }
                }
            }
        }
    }
}
