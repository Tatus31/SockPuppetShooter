using System;
using UnityEngine;

namespace Utils {
    public class BetterSingleton<T> : MonoBehaviour where T : Component {
        protected static T instance;

        public static bool HasInstance => instance is not null;
        public static T TryGetInstance() => HasInstance ? instance : null;

        public static T Instance {
            get {
                if (instance is null) {
                    instance = FindAnyObjectByType<T>();
                    if (instance is null) {
                        var generatedGameObject = new GameObject(typeof(T).Name + "Generated");
                        instance = generatedGameObject.AddComponent<T>();
                    }
                }
                
                return instance;    
            }
        }

        void Awake() {
            InitSingleton();
        }

        protected virtual void InitSingleton() {
            if (!Application.isPlaying) 
                return;
            
            instance = this as T; 
        }
    }
}
