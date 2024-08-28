#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;
using UnityEditor.SceneManagement;
using Spine.Unity;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace ET
{
    public struct GameObjectInfo
    {
        public string prefabPath;
        public string showName;
        public GameObject obj;
    } 
    
    public class FindMissingReference : EditorWindow
    {
        [MenuItem("Tools/查找预制体丢失的引用")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(FindMissingReference)); // 创建并显示自定义Editor窗口
        }

        private void OnDisable()
        {
            _dictionary.Clear();
        }

        private void OnGUI()
        {
            if (GUILayout.Button("检测"))
            {
                _dictionary.Clear();
                CheckMissingAsset();
                int num = 0;
                foreach (var (key,value) in _dictionary)
                {
                    num += _dictionary[key].Count;
                    Debug.Log(key + " : " + value.Count);
                }
                Debug.Log(num);
            }
            

            if (_dictionary.Count > 0)
            {
                GUILayout.BeginHorizontal();
                
                _leftScrollPosition = EditorGUILayout.BeginScrollView(_leftScrollPosition,GUILayout.Width(320)); // 开始滚动视图
                
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                string[] keys = _dictionary.Keys.ToArray();
                _selectedTabIndex = GUILayout.SelectionGrid(_selectedTabIndex, keys, 1, GUILayout.Width(220));
                GUILayout.EndVertical();
                GUILayout.BeginVertical();
                for (int i = 0; i < keys.Length; i++)
                {
                    GUILayout.BeginHorizontal();
                    
                    if (GUILayout.Button("检测空对象"))
                    {
                        PrefabStageUtility.OpenPrefab(_dictionary[keys[i]][0].prefabPath);
                        var stage = PrefabStageUtility.GetCurrentPrefabStage();
                        var root = stage.prefabContentsRoot;
                        ReferenceCollector[] rcs = root.GetComponentsInChildren<ReferenceCollector>(true);
                        ParsePrefab.FindMissing(rcs);
                    }

                    // if (GUILayout.Button(("删除Image")))
                    // {
                    //     //GameObject prefab = Instantiate( AssetDatabase.LoadAssetAtPath<GameObject>(_dictionary[keys[i]][0].prefabPath));
                    //     PrefabStageUtility.OpenPrefab(_dictionary[keys[i]][0].prefabPath);
                    //     var stage = PrefabStageUtility.GetCurrentPrefabStage();
                    //     var prefab = stage.prefabContentsRoot;
                    //     ReferenceCollector[] rcs = prefab.GetComponentsInChildren<ReferenceCollector>(true);
                    //     ParsePrefab.DeleteMissObject(rcs, prefab, _dictionary[keys[i]][0].obj);
                    //     DestroyImmediate(prefab);
                    // }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                EditorGUILayout.EndScrollView(); // 结束滚动视图
                
                _rightScrollPosition = EditorGUILayout.BeginScrollView(_rightScrollPosition); // 开始滚动视图
                GUILayout.BeginVertical();
                var list = _dictionary[keys[_selectedTabIndex]];
                for (int i = 0; i < list.Count; i++)
                {
                    if (GUILayout.Button(list[i].showName,GetButtonStyle(i)))
                    {
                        selectedButtonIndex = i;
                        ShowInHierarchy(list[i].prefabPath, list[i].showName);
                    }

                }
                GUILayout.EndVertical();
                EditorGUILayout.EndScrollView(); // 结束滚动视图
                
                GUILayout.EndHorizontal();
            }
            
            
        }
        private int selectedButtonIndex = -1;
        
        GUIStyle GetButtonStyle(int buttonIndex)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button);

            // 如果是被选中的按钮，则应用高亮效果
            if (buttonIndex == selectedButtonIndex)
            {
                style.normal.background = MakeTex(2, 2, new Color(0.8f, 0.8f, 1f, 0.5f));
            }

            return style;
        }
        
        Texture2D MakeTex(int width, int height, Color color)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = color;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
        
        private int _selectedTabIndex ;
        private Vector2 _leftScrollPosition = Vector2.zero;
        private Vector2 _rightScrollPosition = Vector2.zero;

        //window名以及window下的所有的按钮信息
        private static Dictionary<string, List<GameObjectInfo>> _dictionary =
            new Dictionary<string, List<GameObjectInfo>>();
        

        private static void CheckMissingAsset()
        {
            
            string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { "Assets/Arts/Prefabs" });
            var load = 1f / guids.Length;
            float progress = 0;
            EditorUtility.DisplayProgressBar("检测预制体资源是否丢失", "", progress);
            foreach (string path in guids)
            {
                string asset = AssetDatabase.GUIDToAssetPath(path);
                string lastName = Path.GetFileNameWithoutExtension(asset);
                if (lastName.Contains("Window"))
                {
                    GameObject prefab =  AssetDatabase.LoadAssetAtPath<GameObject>(asset);
                    
                    //图片
                    Image[] images = prefab.transform.GetComponentsInChildren<Image>(true);
                    foreach (Image singleImage in images)
                    {
                        if (singleImage.sprite == null || singleImage.sprite.texture == null)
                        {
                            
                            if (singleImage.name != "ZD")
                            {
                                if (!_dictionary.ContainsKey(prefab.name))
                                {
                                    _dictionary.Add(prefab.name, new List<GameObjectInfo>());
                                }
                                _dictionary[prefab.name].Add(new GameObjectInfo()
                                {
                                    prefabPath = asset,
                                    showName = GetGameObjectPath(singleImage.gameObject),
                                    obj = singleImage.gameObject
                                });
                            }
                            
                            // EditorUtility.ClearProgressBar();
                            // return;
                        }
                    }

                    //特效
                    SkeletonAnimation[] skeletonAnimation =
                        prefab.transform.GetComponentsInChildren<SkeletonAnimation>(true);
                    foreach (SkeletonAnimation singleSpine in skeletonAnimation)
                    {
                        if (singleSpine.skeletonDataAsset == null)
                        {
                            //Debug.Log(GetGameObjectPath(singleSpine.gameObject) + "--" + "特效丢失");
                            if (!_dictionary.ContainsKey(prefab.name))
                            {
                                _dictionary.Add(prefab.name, new List<GameObjectInfo>());
                            }
                            _dictionary[prefab.name].Add(new GameObjectInfo()
                            {
                                prefabPath = asset,
                                showName = GetGameObjectPath(singleSpine.gameObject)
                            });
                        }
                    }

                    //字体
                    Text[] texts = prefab.transform.GetComponentsInChildren<Text>(true);
                    foreach (Text text in texts)
                    {
                        if (text.font == null)
                        {
                            // Font myFont = Resources.Load<Font>("Assets/Arts/Fonts/YOUYUAN.TTF"); // 替换"MyFontFile"为你的字体文件名
                            // // textField.font = myFont;
                            // Font font = Resources.GetBuiltinResource<Font>("Assets/Arts/Fonts/YOUYUAN");
                            // text.font = myFont;
                            // Debug.Log(GetGameObjectPath(text.gameObject) + "--" + "字体丢失");
                            if (!_dictionary.ContainsKey(prefab.name))
                            {
                                _dictionary.Add(prefab.name, new List<GameObjectInfo>());
                            }
                            _dictionary[prefab.name].Add(new GameObjectInfo()
                            {
                                prefabPath = asset,
                                showName = GetGameObjectPath(text.gameObject)
                            });
                        }
                    }

                    //rawimage
                    RawImage[] rawImages = prefab.transform.GetComponentsInChildren<RawImage>(true);
                    foreach (RawImage rawImage in rawImages)
                    {
                        if (rawImage.texture == null)
                        {
                            //Debug.Log(GetGameObjectPath(rawImage.gameObject) + "--" + "rawImage丢失");
                            if (!_dictionary.ContainsKey(prefab.name))
                            {
                                _dictionary.Add(prefab.name, new List<GameObjectInfo>());
                            }
                            _dictionary[prefab.name].Add(new GameObjectInfo()
                            {
                                prefabPath = asset,
                                showName = GetGameObjectPath(rawImage.gameObject)
                            });
                        }
                    }


                    progress += load;
                    EditorUtility.DisplayProgressBar("检测预制体资源是否丢失", asset, progress);
                    
                }
            }
            
            EditorUtility.ClearProgressBar();
            Debug.Log("检测完成!");
            
        }
        
        static string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;

            while (parent != null)
            {
                path = parent.name + "\\" + path;
                parent = parent.parent;
            }

            return path;
        }
        

        //找到对应的Prefab，然后找到某个miss的gameobject
        public static void ShowInHierarchy(string prefabPath,string showName)
        {
            PrefabStageUtility.OpenPrefab(prefabPath);
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            var root = stage.prefabContentsRoot;

            var path = showName.Split("\\");
            Selection.objects = FindGameObject(path, new[] { root }, 1);
        }

        private static GameObject[] FindGameObject(string[] path,GameObject[] gameObjects,int depth)
        {
            if (depth == path.Length)
            {
                return gameObjects;
            }

            return FindGameObject(path, gameObjects.GetChildren(path, depth), depth + 1);
        }
        
    }

    public static class GameObjectExtend
    {
        /// <summary>
        /// 仅在unity编辑器状态下可用，不建议在JumpToPrefab.cs之外使用
        /// </summary>
        public static GameObject[] GetChildren(this GameObject[] gameObjects, string[] path, int depth)
        {
            List<GameObject> children = new List<GameObject>();

            for (int i = 0; i < gameObjects.Length; i++)
            {
                for (int j = 0; j < gameObjects[i].transform.childCount; j++)
                {
                    if (gameObjects[i].transform.GetChild(j).name == path[depth])
                    {
                        children.Add(gameObjects[i].transform.GetChild(j).gameObject);
                    }
                }
            }

            return children.ToArray();
        }
    }
}
#endif
